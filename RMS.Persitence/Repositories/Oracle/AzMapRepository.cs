using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class AzMapRepository : OracleRepositoryBase, IAzMapRepository
    {
        private static List<string>? _cachedCities;
        private static DateTime _citiesExpiry = DateTime.MinValue;

        private static List<string>? _cachedRegions;
        private static DateTime _regionsExpiry = DateTime.MinValue;

        private static List<string>? _cachedBanks;
        private static DateTime _banksExpiry = DateTime.MinValue;

        public AzMapRepository(IConfiguration configuration) : base(configuration) { }

        private FilterBuilder BuildFilter(AzMapTransaction f) =>
            new FilterBuilder()
                .AddString("m.BANK_NAME            = :BankName", "BankName", f.BankName)
                .AddString("m.SOURCE_CITY          = :SourceCity", "SourceCity", f.SourceCity)
                .AddString("m.SOURCE_CITY_CLEAN    = :SourceCityClean", "SourceCityClean", f.SourceCityClean)
                .AddString("m.SOURCE_CITY_CATEGORY = :CityCategory", "CityCategory", f.SourceCityCategory)
                .AddMonth("m.REPORT_MONTH", "Month",
                    f.ReportMonth == default ? null : f.ReportMonth);

        public async Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapTransaction f,
            PageRequest pageReq,
            CancellationToken ct = default)
        {
            var filter = BuildFilter(f);

            var countSql = $"SELECT COUNT(*) FROM pg_mv_req4_az_map m {filter.WhereClause}";

            // Əvvəlcə yalnız səhifələnmiş (məs. 20-50) sətri seçirik, sonra cities ilə JOIN edirik
            var pagedSql = $"""
                WITH paged_m AS (
                    SELECT
                        m.REPORT_MONTH,
                        m.BANK_NAME,
                        m.SOURCE_CITY,
                        m.SOURCE_CITY_CLEAN,
                        m.SOURCE_CITY_CATEGORY,
                        m.TOTAL_AMOUNT,
                        m.TOTAL_COUNT
                    FROM pg_mv_req4_az_map m
                    {filter.WhereClause}
                    ORDER BY m.REPORT_MONTH DESC
                    OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
                ),
                norm_cities AS (
                    SELECT
                        latitude,
                        longitude,
                        REGEXP_REPLACE(UPPER(TRIM(city_name)), '[^A-ZƏĞIÖŞÜÇ]', '') AS norm_name
                    FROM cities
                )
                SELECT
                    pm.REPORT_MONTH         AS ReportMonth,
                    pm.BANK_NAME            AS BankName,
                    pm.SOURCE_CITY          AS SourceCity,
                    pm.SOURCE_CITY_CLEAN    AS SourceCityClean,
                    pm.SOURCE_CITY_CATEGORY AS SourceCityCategory,
                    pm.TOTAL_AMOUNT         AS TotalAmount,
                    pm.TOTAL_COUNT          AS TotalCount,
                    c.latitude              AS Latitude,
                    c.longitude             AS Longitude
                FROM paged_m pm
                LEFT JOIN norm_cities c
                       ON pm.SOURCE_CITY_CLEAN IS NOT NULL
                      AND c.norm_name = REGEXP_REPLACE(UPPER(TRIM(pm.SOURCE_CITY_CLEAN)), '[^A-ZƏĞIÖŞÜÇ]', '')
                ORDER BY pm.REPORT_MONTH DESC
                """;

            var dynParam = new DynamicParameters(filter.Parameters);
            dynParam.Add("Offset", (pageReq.Page - 1) * pageReq.PageSize);
            dynParam.Add("PageSize", pageReq.PageSize);

            using var conn = CreateConnection();

            var totalCount = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, filter.Parameters, cancellationToken: ct));

            var items = await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(pagedSql, dynParam, cancellationToken: ct));

            return new PagedResult<AzMapTransaction>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pageReq.Page,
                PageSize = pageReq.PageSize
            };
        }

        public async Task<IEnumerable<AzMapTransaction>> GetHeatmapDataAsync(
            string? bankName,
            string? sourceCity,
            DateTime? month,
            CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .AddString("m.BANK_NAME   = :BankName", "BankName", bankName)
                .AddString("m.SOURCE_CITY = :SourceCity", "SourceCity", sourceCity)
                .AddMonth("m.REPORT_MONTH", "Month", month);

            // Əvvəlcə MV daxilində şəhər üzrə qruplaşdırılır (~70 sətir), yalnız ondan sonra cities ilə JOIN edilir
            var sql = $"""
                WITH city_agg AS (
                    SELECT
                        m.SOURCE_CITY_CLEAN,
                        SUM(m.TOTAL_AMOUNT) AS TOTAL_AMOUNT,
                        SUM(m.TOTAL_COUNT)  AS TOTAL_COUNT
                    FROM pg_mv_req4_az_map m
                    {filter.WhereClause}
                    GROUP BY m.SOURCE_CITY_CLEAN
                ),
                norm_cities AS (
                    SELECT
                        latitude,
                        longitude,
                        REGEXP_REPLACE(UPPER(TRIM(city_name)), '[^A-ZƏĞIÖŞÜÇ]', '') AS norm_name
                    FROM cities
                )
                SELECT
                    ca.SOURCE_CITY_CLEAN AS SourceCityClean,
                    ca.TOTAL_AMOUNT      AS TotalAmount,
                    ca.TOTAL_COUNT       AS TotalCount,
                    c.latitude           AS Latitude,
                    c.longitude          AS Longitude
                FROM city_agg ca
                LEFT JOIN norm_cities c
                       ON ca.SOURCE_CITY_CLEAN IS NOT NULL
                      AND c.norm_name = REGEXP_REPLACE(UPPER(TRIM(ca.SOURCE_CITY_CLEAN)), '[^A-ZƏĞIÖŞÜÇ]', '')
                WHERE c.latitude IS NOT NULL AND c.longitude IS NOT NULL
                """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, filter.Parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<string>> GetDistinctCitiesAsync(
            CancellationToken ct = default)
        {
            if (_cachedCities is not null && DateTime.UtcNow < _citiesExpiry)
                return _cachedCities;

            const string sql = """
                SELECT DISTINCT SOURCE_CITY
                FROM pg_mv_req4_az_map
                WHERE SOURCE_CITY IS NOT NULL
                  AND SOURCE_CITY != 'NAMƏLUM'
                ORDER BY SOURCE_CITY
                """;
            using var conn = CreateConnection();
            var list = (await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct))).ToList();

            _cachedCities = list;
            _citiesExpiry = DateTime.UtcNow.AddMinutes(30);
            return list;
        }

        public async Task<IEnumerable<string>> GetDistinctRegionsAsync(
            CancellationToken ct = default)
        {
            if (_cachedRegions is not null && DateTime.UtcNow < _regionsExpiry)
                return _cachedRegions;

            const string sql = """
                SELECT DISTINCT SOURCE_CITY_CLEAN
                FROM pg_mv_req4_az_map
                WHERE SOURCE_CITY_CLEAN IS NOT NULL
                  AND SOURCE_CITY_CLEAN != 'NAMƏLUM'
                ORDER BY SOURCE_CITY_CLEAN
                """;
            using var conn = CreateConnection();
            var list = (await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct))).ToList();

            _cachedRegions = list;
            _regionsExpiry = DateTime.UtcNow.AddMinutes(30);
            return list;
        }

        public async Task<IEnumerable<string>> GetDistinctBanksAsync(
            CancellationToken ct = default)
        {
            if (_cachedBanks is not null && DateTime.UtcNow < _banksExpiry)
                return _cachedBanks;

            const string sql = """
                SELECT DISTINCT BANK_NAME
                FROM pg_mv_req4_az_map
                WHERE BANK_NAME IS NOT NULL
                ORDER BY BANK_NAME
                """;
            using var conn = CreateConnection();
            var list = (await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct))).ToList();

            _cachedBanks = list;
            _banksExpiry = DateTime.UtcNow.AddMinutes(30);
            return list;
        }
    }
}
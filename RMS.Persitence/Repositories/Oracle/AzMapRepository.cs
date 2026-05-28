using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class AzMapRepository : OracleRepositoryBase, IAzMapRepository
    {
        public AzMapRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
            SELECT
                m.REPORT_MONTH          AS ReportMonth,
                m.BANK_NAME             AS BankName,
                m.SOURCE_CITY           AS SourceCity,
                m.SOURCE_CITY_CLEAN     AS SourceCityClean,
                m.SOURCE_CITY_CATEGORY  AS SourceCityCategory,
                m.TOTAL_AMOUNT          AS TotalAmount,
                m.TOTAL_COUNT           AS TotalCount,
                c.latitude              AS Latitude,
                c.longitude             AS Longitude
            FROM   pg_mv_req4_az_map m
            LEFT JOIN public.cities c
                   ON UPPER(TRIM(c.city_name)) = UPPER(TRIM(m.SOURCE_CITY_CLEAN))
            """;

        private FilterBuilder BuildFilter(AzMapTransaction f) =>
            new FilterBuilder()
                .Add("m.BANK_NAME            = @BankName", "BankName", f.BankName)
                .Add("m.SOURCE_CITY          = @SourceCity", "SourceCity", f.SourceCity)
                .Add("m.SOURCE_CITY_CLEAN    = @SourceCityClean", "SourceCityClean", f.SourceCityClean)
                .Add("m.SOURCE_CITY_CATEGORY = @CityCategory", "CityCategory", f.SourceCityCategory)
                .AddMonth("m.REPORT_MONTH", "Month",
                    f.ReportMonth == default ? null : f.ReportMonth);

        public Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapTransaction f,
            PageRequest pageReq,
            CancellationToken ct = default)
        {
            var filter = BuildFilter(f);
            return QueryPagedAsync<AzMapTransaction>(
                SelectColumns + $" {filter.WhereClause}",
                "ReportMonth DESC",
                pageReq,
                filter.Parameters,
                ct);
        }

        public async Task<IEnumerable<AzMapTransaction>> GetHeatmapDataAsync(
            string? bankName,
            string? sourceCity,
            DateTime? month,
            CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .Add("m.BANK_NAME   = @BankName", "BankName", bankName)
                .Add("m.SOURCE_CITY = @SourceCity", "SourceCity", sourceCity)
                .AddMonth("m.REPORT_MONTH", "Month", month);

            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(
                    SelectColumns + $" {filter.WhereClause}",
                    filter.Parameters,
                    cancellationToken: ct));
        }

        public async Task<IEnumerable<string>> GetDistinctCitiesAsync(
            CancellationToken ct = default)
        {
            const string sql = """
                SELECT DISTINCT SOURCE_CITY
                FROM pg_mv_req4_az_map
                WHERE SOURCE_CITY IS NOT NULL
                  AND TRIM(SOURCE_CITY) != ''
                  AND SOURCE_CITY != 'NAMƏLUM'
                """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<string>> GetDistinctRegionsAsync(
            CancellationToken ct = default)
        {
            const string sql = """
                SELECT DISTINCT SOURCE_CITY_CLEAN
                FROM pg_mv_req4_az_map
                WHERE SOURCE_CITY_CLEAN IS NOT NULL
                  AND TRIM(SOURCE_CITY_CLEAN) != ''
                  AND SOURCE_CITY_CLEAN != 'NAMƏLUM'
                """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<string>> GetDistinctBanksAsync(
            CancellationToken ct = default)
        {
            const string sql = """
                SELECT DISTINCT BANK_NAME
                FROM pg_mv_req4_az_map
                WHERE BANK_NAME IS NOT NULL
                """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct));
        }
    }
}
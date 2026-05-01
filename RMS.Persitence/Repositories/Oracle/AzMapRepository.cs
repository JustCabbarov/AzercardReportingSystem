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
                REPORT_MONTH          AS ReportMonth,
                BANK_NAME             AS BankName,
                SOURCE_CITY           AS SourceCity,
                SOURCE_CITY_CLEAN     AS SourceCityClean,
                SOURCE_CITY_CATEGORY  AS SourceCityCategory,
                TOTAL_AMOUNT          AS TotalAmount,
                TOTAL_COUNT           AS TotalCount
            FROM ALI_JABBAROV.PG_MV_REQ4_AZ_MAP
            """;

        private FilterBuilder BuildFilter(AzMapTransaction f) =>
            new FilterBuilder()
                .Add("BANK_NAME            = :BankName", "BankName", f.BankName)
                .Add("SOURCE_CITY          = :SourceCity", "SourceCity", f.SourceCity)
                .Add("SOURCE_CITY_CLEAN    = :SourceCityClean", "SourceCityClean", f.SourceCityClean)
                .Add("SOURCE_CITY_CATEGORY = :CityCategory", "CityCategory", f.SourceCityCategory)
                .AddMonth("REPORT_MONTH", "Month",
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
                .Add("BANK_NAME    = :BankName", "BankName", bankName)
                .Add("SOURCE_CITY  = :SourceCity", "SourceCity", sourceCity)
                .AddMonth("REPORT_MONTH", "Month", month);

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
                FROM ALI_JABBAROV.PG_MV_REQ4_AZ_MAP
                WHERE SOURCE_CITY IS NOT NULL
                  AND LENGTH(TRIM(SOURCE_CITY)) > 0
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
                FROM ALI_JABBAROV.PG_MV_REQ4_AZ_MAP
                WHERE SOURCE_CITY_CLEAN IS NOT NULL
                  AND LENGTH(TRIM(SOURCE_CITY_CLEAN)) > 0
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
                FROM ALI_JABBAROV.PG_MV_REQ4_AZ_MAP
                WHERE BANK_NAME IS NOT NULL
                """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct));
        }
    }
}
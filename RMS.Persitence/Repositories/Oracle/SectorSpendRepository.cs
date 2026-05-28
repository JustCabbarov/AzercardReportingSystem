using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class SectorSpendRepository : OracleRepositoryBase, ISectorSpendRepository
    {
        public SectorSpendRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH                        AS ReportMonth,
            BANK_NAME                           AS BankName,
            MCC_GROUP                           AS MccGroup,
            MCC                                 AS Mcc,
            SOURCE_CITY                         AS SourceCity,
            SOURCE_CITY_CATEGORY                AS SourceCityCategory,
            SOURCE_CHANNEL                      AS SourceChannel,
            OPERATION_TYPE                      AS OperationType,
            SOURCE_COUNTRY_CATEGORY             AS SourceCountryCategory,
            IS_ACQUIRING                        AS IsAcquiring,
            IS_ISSUING                          AS IsIssuing,
            TOTAL_AMOUNT::float8                AS TotalAmount,
            TOTAL_COUNT::bigint                 AS TotalCount,
            TOTAL_LOCAL_AMOUNT::float8          AS TotalLocalAmount,
            AVG_TICKET::float8                  AS AvgTicket,
            LOCAL_RATIO_PCT::float8             AS LocalRatioPct,
            SECTOR_MARKET_SHARE_PCT::float8     AS SectorMarketSharePct
        FROM pg_mv_req8_sector_spend
        """;

        private Task<PagedResult<SectorSpend>> QueryAsync(
            string? bankName,
            DateTime? month,
            string? mccGroup,
            string? sourceChannel,
            string? operationType,
            string? sourceCityCategory,
            string? sourceCountryCategory,
            int? isAcquiring,
            int? isIssuing,
            PageRequest pageReq,
            CancellationToken ct)
        {
            var filter = new FilterBuilder()
                .AddString("BANK_NAME = @BankName", "BankName", bankName)                          // ← AddString
                .AddString("MCC_GROUP = @MccGroup", "MccGroup", mccGroup)                          // ← AddString
                .AddString("SOURCE_CHANNEL = @SourceChannel", "SourceChannel", sourceChannel)      // ← AddString
                .AddString("OPERATION_TYPE = @OperationType", "OperationType", operationType)      // ← AddString
                .AddString("SOURCE_CITY_CATEGORY = @CityCategory", "CityCategory", sourceCityCategory)           // ← AddString
                .AddString("SOURCE_COUNTRY_CATEGORY = @CountryCategory", "CountryCategory", sourceCountryCategory) // ← AddString
                .Add("IS_ACQUIRING = @IsAcquiring", "IsAcquiring", isAcquiring)                   // int? — Add qalır
                .Add("IS_ISSUING = @IsIssuing", "IsIssuing", isIssuing)                            // int? — Add qalır
                .AddMonth("REPORT_MONTH", "Month", month);

            return QueryPagedAsync<SectorSpend>(
                SelectColumns + $" {filter.WhereClause}",
                "ReportMonth DESC, MccGroup",
                pageReq,
                filter.Parameters,
                ct);
        }

        public Task<PagedResult<SectorSpend>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(null, null, null, null, null, null, null, null, null, pageReq, ct);

        public Task<PagedResult<SectorSpend>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(bankName, null, null, null, null, null, null, null, null, pageReq, ct);

        public Task<PagedResult<SectorSpend>> GetByBankAndMonthAsync(
            string bankName, DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(bankName, month, null, null, null, null, null, null, null, pageReq, ct);

        public Task<PagedResult<SectorSpend>> GetByMccGroupAsync(
            string mccGroup, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(null, null, mccGroup, null, null, null, null, null, null, pageReq, ct);

        public Task<PagedResult<SectorSpend>> FilterAsync(
            SectorSpend f, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(
                f.BankName,
                f.ReportMonth == default ? null : f.ReportMonth,
                f.MccGroup,
                f.SourceChannel,
                f.OperationType,
                f.SourceCityCategory,
                f.SourceCountryCategory,
                f.IsAcquiring,
                f.IsIssuing,
                pageReq,
                ct);

        public async Task<IEnumerable<SectorSpendTrend>> GetTrendAsync(
            string bankName, string mccGroup, CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .Add("BANK_NAME = @BankName", "BankName", bankName)
                .Add("MCC_GROUP = @MccGroup", "MccGroup", mccGroup);

            var sql = $"""
            SELECT
                REPORT_MONTH                                                AS ReportMonth,
                SUM(TOTAL_AMOUNT)::float8                                   AS TotalAmount,
                SUM(TOTAL_COUNT)::bigint                                    AS TotalCount,
                (SUM(TOTAL_AMOUNT) / NULLIF(SUM(TOTAL_COUNT), 0))::float8  AS AvgTicket,
                AVG(SECTOR_MARKET_SHARE_PCT)::float8                       AS SectorMarketSharePct
            FROM pg_mv_req8_sector_spend
            {filter.WhereClause}
            GROUP BY REPORT_MONTH
            ORDER BY REPORT_MONTH
            """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<SectorSpendTrend>(
                new CommandDefinition(sql, filter.Parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<(string Hash, string Name)>> GetDistinctBanksAsync(CancellationToken ct = default)
        {
            var sql = """
        SELECT DISTINCT 
            s.bank_name AS Hash,
            COALESCE(b.bank_name, s.bank_name) AS Name
        FROM (
            SELECT DISTINCT bank_name FROM pg_mv_req1_forecast_input
            UNION
            SELECT DISTINCT bank_name FROM pg_mv_req3_world_map
            UNION
            SELECT DISTINCT bank_name FROM pg_mv_req4_az_map
            UNION
            SELECT DISTINCT bank_name FROM pg_mv_req7_benchmark
            UNION
            SELECT DISTINCT bank_name FROM pg_mv_req8_sector_spend
        ) s
        LEFT JOIN banks b ON b.bank_hash = s.bank_name
        WHERE s.bank_name IS NOT NULL
        ORDER BY Name
        """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<(string Hash, string Name)>(
                new CommandDefinition(sql, cancellationToken: ct));
        }
        public async Task<IEnumerable<(string Hash, string Name)>> GetDistinctMccGroupsAsync(CancellationToken ct = default)
        {
            var sql = """
        SELECT DISTINCT 
            s.mcc_group AS Hash,
            COALESCE(m.mcc_name, s.mcc_group) AS Name
        FROM (
            SELECT DISTINCT mcc_group FROM pg_mv_req1_forecast_input WHERE mcc_group IS NOT NULL
            UNION
            SELECT DISTINCT mcc_group FROM pg_mv_req8_sector_spend WHERE mcc_group IS NOT NULL
        ) s
        LEFT JOIN mcc_groups m ON m.mcc_hash = s.mcc_group
        ORDER BY Name
        """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<(string Hash, string Name)>(
                new CommandDefinition(sql, cancellationToken: ct));
        }
    }
}
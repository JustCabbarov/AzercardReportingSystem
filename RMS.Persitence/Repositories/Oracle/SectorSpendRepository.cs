using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class SectorSpendRepository : OracleRepositoryBase, ISectorSpendRepository
    {
        private static bool _banksTableMissing;
        private static bool _mccGroupsTableMissing;

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
            TOTAL_AMOUNT                        AS TotalAmount,
            TOTAL_COUNT                         AS TotalCount,
            TOTAL_LOCAL_AMOUNT                  AS TotalLocalAmount,
            AVG_TICKET                          AS AvgTicket,
            LOCAL_RATIO_PCT                     AS LocalRatioPct,
            SECTOR_MARKET_SHARE_PCT             AS SectorMarketSharePct
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
                .AddString("BANK_NAME = :BankName", "BankName", bankName)
                .AddString("MCC_GROUP = :MccGroup", "MccGroup", mccGroup)
                .AddString("SOURCE_CHANNEL = :SourceChannel", "SourceChannel", sourceChannel)
                .AddString("OPERATION_TYPE = :OperationType", "OperationType", operationType)
                .AddString("SOURCE_CITY_CATEGORY = :CityCategory", "CityCategory", sourceCityCategory)
                .AddString("SOURCE_COUNTRY_CATEGORY = :CountryCategory", "CountryCategory", sourceCountryCategory)
                .Add("IS_ACQUIRING = :IsAcquiring", "IsAcquiring", isAcquiring)
                .Add("IS_ISSUING = :IsIssuing", "IsIssuing", isIssuing)
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
                .Add("BANK_NAME = :BankName", "BankName", bankName)
                .Add("MCC_GROUP = :MccGroup", "MccGroup", mccGroup);

            var sql = $"""
            SELECT
                REPORT_MONTH                                                AS ReportMonth,
                SUM(TOTAL_AMOUNT)                                           AS TotalAmount,
                SUM(TOTAL_COUNT)                                            AS TotalCount,
                ROUND(SUM(TOTAL_AMOUNT) / NULLIF(SUM(TOTAL_COUNT), 0), 2)   AS AvgTicket,
                AVG(SECTOR_MARKET_SHARE_PCT)                                AS SectorMarketSharePct
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
            const string sqlWithJoin = """
            SELECT DISTINCT 
                s.bank_name AS Hash,
                COALESCE(b.bank_name, s.bank_name) AS Name
            FROM (
                SELECT DISTINCT bank_name FROM pg_mv_req1_forecast_input
                UNION
                SELECT DISTINCT bank_name FROM pg_mv_req5_world_map
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

            const string fallbackSql = """
            SELECT DISTINCT 
                s.bank_name AS Hash,
                s.bank_name AS Name
            FROM (
                SELECT DISTINCT bank_name FROM pg_mv_req1_forecast_input
                UNION
                SELECT DISTINCT bank_name FROM pg_mv_req5_world_map
                UNION
                SELECT DISTINCT bank_name FROM pg_mv_req4_az_map
                UNION
                SELECT DISTINCT bank_name FROM pg_mv_req7_benchmark
                UNION
                SELECT DISTINCT bank_name FROM pg_mv_req8_sector_spend
            ) s
            WHERE s.bank_name IS NOT NULL
            ORDER BY Name
            """;

            using var conn = CreateConnection();

            if (_banksTableMissing)
            {
                return await conn.QueryAsync<(string Hash, string Name)>(
                    new CommandDefinition(fallbackSql, cancellationToken: ct));
            }

            try
            {
                return await conn.QueryAsync<(string Hash, string Name)>(
                    new CommandDefinition(sqlWithJoin, cancellationToken: ct));
            }
            catch (OracleException ex) when (ex.Number == 942)
            {
                _banksTableMissing = true;
                return await conn.QueryAsync<(string Hash, string Name)>(
                    new CommandDefinition(fallbackSql, cancellationToken: ct));
            }
        }

        public async Task<IEnumerable<(string Hash, string Name)>> GetDistinctMccGroupsAsync(CancellationToken ct = default)
        {
            const string sqlWithJoin = """
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

            const string fallbackSql = """
            SELECT DISTINCT 
                s.mcc_group AS Hash,
                s.mcc_group AS Name
            FROM (
                SELECT DISTINCT mcc_group FROM pg_mv_req1_forecast_input WHERE mcc_group IS NOT NULL
                UNION
                SELECT DISTINCT mcc_group FROM pg_mv_req8_sector_spend WHERE mcc_group IS NOT NULL
            ) s
            ORDER BY Name
            """;

            using var conn = CreateConnection();

            if (_mccGroupsTableMissing)
            {
                return await conn.QueryAsync<(string Hash, string Name)>(
                    new CommandDefinition(fallbackSql, cancellationToken: ct));
            }

            try
            {
                return await conn.QueryAsync<(string Hash, string Name)>(
                    new CommandDefinition(sqlWithJoin, cancellationToken: ct));
            }
            catch (OracleException ex) when (ex.Number == 942)
            {
                _mccGroupsTableMissing = true;
                return await conn.QueryAsync<(string Hash, string Name)>(
                    new CommandDefinition(fallbackSql, cancellationToken: ct));
            }
        }
    }
}
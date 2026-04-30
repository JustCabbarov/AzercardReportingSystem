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
            REPORT_MONTH                                    AS ReportMonth,
            BANK_NAME                                       AS BankName,
            MCC_GROUP                                       AS MccGroup,
            MCC                                             AS Mcc,
            SOURCE_CITY                                     AS SourceCity,
            SOURCE_CITY_CATEGORY                            AS SourceCityCategory,
            SOURCE_CHANNEL                                  AS SourceChannel,
            OPERATION_TYPE                                  AS OperationType,
            SOURCE_COUNTRY_CATEGORY                         AS SourceCountryCategory,
            IS_ACQUIRING                                    AS IsAcquiring,
            IS_ISSUING                                      AS IsIssuing,
            CAST(TOTAL_AMOUNT AS BINARY_DOUBLE)             AS TotalAmount,
            CAST(TOTAL_COUNT AS NUMBER(18,0))               AS TotalCount,
            CAST(TOTAL_LOCAL_AMOUNT AS BINARY_DOUBLE)       AS TotalLocalAmount,
            CAST(AVG_TICKET AS BINARY_DOUBLE)               AS AvgTicket,
            CAST(LOCAL_RATIO_PCT AS BINARY_DOUBLE)          AS LocalRatioPct,
            CAST(SECTOR_MARKET_SHARE_PCT AS BINARY_DOUBLE)  AS SectorMarketSharePct
        FROM ALI_JABBAROV.PG_MV_REQ8_SECTOR_SPEND
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
                .Add("BANK_NAME = :BankName", "BankName", bankName)
                .Add("MCC_GROUP = :MccGroup", "MccGroup", mccGroup)
                .Add("SOURCE_CHANNEL = :SourceChannel", "SourceChannel", sourceChannel)
                .Add("OPERATION_TYPE = :OperationType", "OperationType", operationType)
                .Add("SOURCE_CITY_CATEGORY = :CityCategory", "CityCategory", sourceCityCategory)
                .Add("SOURCE_COUNTRY_CATEGORY = :CountryCategory", "CountryCategory", sourceCountryCategory)
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
                REPORT_MONTH                                                        AS ReportMonth,
                CAST(SUM(TOTAL_AMOUNT) AS BINARY_DOUBLE)                            AS TotalAmount,
                CAST(SUM(TOTAL_COUNT) AS NUMBER(18,0))                              AS TotalCount,
                CAST(SUM(TOTAL_AMOUNT) / NULLIF(SUM(TOTAL_COUNT), 0) AS BINARY_DOUBLE) AS AvgTicket,
                CAST(AVG(SECTOR_MARKET_SHARE_PCT) AS BINARY_DOUBLE)                AS SectorMarketSharePct
            FROM ALI_JABBAROV.PG_MV_REQ8_SECTOR_SPEND
            {filter.WhereClause}
            GROUP BY REPORT_MONTH
            ORDER BY REPORT_MONTH
            """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<SectorSpendTrend>(
                new CommandDefinition(sql, filter.Parameters, cancellationToken: ct));
        }
    }
}
using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class WorldMapRepository : OracleRepositoryBase, IWorldMapRepository
    {
        public WorldMapRepository(IConfiguration configuration) : base(configuration) { }

        private FilterBuilder BuildFilter(WorldMapTransaction f) =>
            new FilterBuilder()
                .AddString("w.BANK_NAME = @BankName", "BankName", f.BankName)
                .AddString("UPPER(w.SOURCE_COUNTRY) = UPPER(@SourceCountry)", "SourceCountry", f.SourceCountry)
                .AddString("w.SOURCE_COUNTRY_CATEGORY = @SourceCountryCategory", "SourceCountryCategory", f.SourceCountryCategory)
                .AddString("UPPER(w.TARGET_COUNTRY) = UPPER(@TargetCountry)", "TargetCountry", f.TargetCountry)
                .AddString("w.TARGET_COUNTRY_CATEGORY = @TargetCountryCategory", "TargetCountryCategory", f.TargetCountryCategory)
                .AddString("w.PAYMENT_SYSTEM = @PaymentSystem", "PaymentSystem", f.PaymentSystem)
                .AddBool("w.IS_ISSUING", "IsIssuing", f.IsIssuing)
                .AddBool("w.IS_ACQUIRING", "IsAcquiring", f.IsAcquiring)
                .AddMonth("w.REPORT_MONTH", "Month",
                    f.ReportMonth == default ? null : f.ReportMonth);

        public Task<PagedResult<WorldMapTransaction>> FilterAsync(
    WorldMapTransaction f,
    PageRequest pageReq,
    CancellationToken ct = default)
        {
            var filter = BuildFilter(f);
            var sql = $"""
        SELECT
            REPORT_MONTH                    AS ReportMonth,
            BANK_NAME                       AS BankName,
            SourceCountry,
            SOURCE_COUNTRY_CATEGORY         AS SourceCountryCategory,
            TargetCountry,
            TARGET_COUNTRY_CATEGORY         AS TargetCountryCategory,
            PAYMENT_SYSTEM                  AS PaymentSystem,
            IS_ISSUING                      AS IsIssuing,
            IS_ACQUIRING                    AS IsAcquiring,
            TOTAL_AMOUNT                    AS TotalAmount,
            TOTAL_COUNT                     AS TotalCount,
            SourceLatitude,
            SourceLongitude,
            TargetLatitude,
            TargetLongitude
        FROM (
            SELECT
                w.REPORT_MONTH,
                w.BANK_NAME,
                INITCAP(w.SOURCE_COUNTRY)         AS SourceCountry,
                w.SOURCE_COUNTRY_CATEGORY,
                INITCAP(w.TARGET_COUNTRY)         AS TargetCountry,
                w.TARGET_COUNTRY_CATEGORY,
                w.PAYMENT_SYSTEM,
                w.IS_ISSUING,
                w.IS_ACQUIRING,
                w.TOTAL_AMOUNT,
                w.TOTAL_COUNT,
                sc.latitude                       AS SourceLatitude,
                sc.longitude                      AS SourceLongitude,
                tc.latitude                       AS TargetLatitude,
                tc.longitude                      AS TargetLongitude
            FROM pg_mv_req3_world_map w
            LEFT JOIN countries sc ON UPPER(sc.name) = UPPER(w.SOURCE_COUNTRY)
            LEFT JOIN countries tc ON UPPER(tc.name) = UPPER(w.TARGET_COUNTRY)
            {filter.WhereClause}
        ) AS _base
        """;

            return QueryPagedAsync<WorldMapTransaction>(
                sql,
                "ReportMonth DESC",
                pageReq,
                filter.Parameters,
                ct);
        }

        private async Task<IEnumerable<WorldMapTransaction>> QueryAggregatedAsync(
            string? where, object? param, CancellationToken ct)
        {
            var sql = $"""
                SELECT
                    INITCAP(w.SOURCE_COUNTRY)         AS SourceCountry,
                    w.SOURCE_COUNTRY_CATEGORY         AS SourceCountryCategory,
                    INITCAP(w.TARGET_COUNTRY)         AS TargetCountry,
                    w.TARGET_COUNTRY_CATEGORY         AS TargetCountryCategory,
                    w.PAYMENT_SYSTEM                  AS PaymentSystem,
                    BOOL_OR(w.IS_ISSUING)             AS IsIssuing,
                    BOOL_OR(w.IS_ACQUIRING)           AS IsAcquiring,
                    SUM(w.TOTAL_AMOUNT)               AS TotalAmount,
                    SUM(w.TOTAL_COUNT)                AS TotalCount,
                    sc.latitude                       AS SourceLatitude,
                    sc.longitude                      AS SourceLongitude,
                    tc.latitude                       AS TargetLatitude,
                    tc.longitude                      AS TargetLongitude
                FROM pg_mv_req3_world_map w
                LEFT JOIN countries sc ON UPPER(sc.name) = UPPER(w.SOURCE_COUNTRY)
                LEFT JOIN countries tc ON UPPER(tc.name) = UPPER(w.TARGET_COUNTRY)
                {(where is null ? "" : $"WHERE {where}")}
                GROUP BY
                    INITCAP(w.SOURCE_COUNTRY),
                    w.SOURCE_COUNTRY_CATEGORY,
                    INITCAP(w.TARGET_COUNTRY),
                    w.TARGET_COUNTRY_CATEGORY,
                    w.PAYMENT_SYSTEM,
                    sc.latitude,
                    sc.longitude,
                    tc.latitude,
                    tc.longitude
                ORDER BY SUM(w.TOTAL_AMOUNT) DESC
                """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, param, cancellationToken: ct));
        }

        private async Task<IEnumerable<WorldMapTransaction>> QueryRawAsync(
            string? where, object? param, CancellationToken ct)
        {
            var sql = $"""
                SELECT
                    w.REPORT_MONTH                    AS ReportMonth,
                    w.BANK_NAME                       AS BankName,
                    INITCAP(w.SOURCE_COUNTRY)         AS SourceCountry,
                    w.SOURCE_COUNTRY_CATEGORY         AS SourceCountryCategory,
                    INITCAP(w.TARGET_COUNTRY)         AS TargetCountry,
                    w.TARGET_COUNTRY_CATEGORY         AS TargetCountryCategory,
                    w.PAYMENT_SYSTEM                  AS PaymentSystem,
                    w.IS_ISSUING                      AS IsIssuing,
                    w.IS_ACQUIRING                    AS IsAcquiring,
                    w.TOTAL_AMOUNT                    AS TotalAmount,
                    w.TOTAL_COUNT                     AS TotalCount,
                    sc.latitude                       AS SourceLatitude,
                    sc.longitude                      AS SourceLongitude,
                    tc.latitude                       AS TargetLatitude,
                    tc.longitude                      AS TargetLongitude
                FROM pg_mv_req3_world_map w
                LEFT JOIN countries sc ON UPPER(sc.name) = UPPER(w.SOURCE_COUNTRY)
                LEFT JOIN countries tc ON UPPER(tc.name) = UPPER(w.TARGET_COUNTRY)
                {(where is null ? "" : $"WHERE {where}")}
                ORDER BY w.REPORT_MONTH DESC
                """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, param, cancellationToken: ct));
        }

        public Task<IEnumerable<WorldMapTransaction>> GetAllAsync(
            CancellationToken ct = default)
            => QueryAggregatedAsync(null, null, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "w.BANK_NAME = @BankName",
                new { BankName = bankName },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => QueryRawAsync(
                "DATE_TRUNC('month', w.REPORT_MONTH) = DATE_TRUNC('month', @Month::timestamp)",
                new { Month = month.Date },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(
            string country, CancellationToken ct = default)
            => QueryRawAsync(
                "UPPER(w.SOURCE_COUNTRY) = UPPER(@Country)",
                new { Country = country },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "w.BANK_NAME = @BankName AND w.IS_ISSUING = TRUE",
                new { BankName = bankName },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "w.BANK_NAME = @BankName AND w.IS_ACQUIRING = TRUE",
                new { BankName = bankName },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByBankAndMonthAsync(
            string bankName, DateTime month, CancellationToken ct = default)
            => QueryRawAsync(
                "w.BANK_NAME = @BankName AND DATE_TRUNC('month', w.REPORT_MONTH) = DATE_TRUNC('month', @Month::timestamp)",
                new { BankName = bankName, Month = month.Date },
                ct);
    }
}
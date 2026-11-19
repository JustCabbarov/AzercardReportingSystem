using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class WorldMapRepository : OracleRepositoryBase, IWorldMapRepository
    {
        private static readonly ConcurrentDictionary<string, (List<string> Values, DateTime Expiry)> _columnCache = new();
        private static readonly HashSet<string> AllowedFilterColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "IS_ISSUING_CATEGORY",
            "CARD_PRODUCT_TYPE_CATEGORY",
            "PAYMENT_SYSTEM",
            "CARD_ACCOUNT_CURRENCY",
            "SOURCE_BANK_NAME",
            "BANK_NAME",
            "ACQUIRING_DEVICE_TYPE",
            "TRANS_GROUP",
            "OPERATION_TYPE",
            "TOKEN_STATUS",
        };

        public WorldMapRepository(IConfiguration configuration) : base(configuration) { }

        private FilterBuilder BuildFilter(WorldMapTransaction f)
        {
            var fb = new FilterBuilder()
                .AddString("w.BANK_NAME = :BankName", "BankName", f.BankName)
                .AddString("UPPER(w.SOURCE_COUNTRY) = UPPER(:SourceCountry)", "SourceCountry", f.SourceCountry)
                .AddString("w.SOURCE_COUNTRY_CATEGORY = :SourceCountryCategory", "SourceCountryCategory", f.SourceCountryCategory)
                .AddString("UPPER(w.TARGET_COUNTRY) = UPPER(:TargetCountry)", "TargetCountry", f.TargetCountry)
                .AddString("w.TARGET_COUNTRY_CATEGORY = :TargetCountryCategory", "TargetCountryCategory", f.TargetCountryCategory)
                .AddString("w.PAYMENT_SYSTEM = :PaymentSystem", "PaymentSystem", f.PaymentSystem)
                .AddString("w.IS_ISSUING_CATEGORY = :IsIssuingCategory", "IsIssuingCategory", f.IsIssuingCategory)
                .AddString("w.CARD_PRODUCT_TYPE_CATEGORY = :CardProductTypeCategory", "CardProductTypeCategory", f.CardProductTypeCategory)
                .AddString("w.CARD_ACCOUNT_CURRENCY = :CardAccountCurrency", "CardAccountCurrency", f.CardAccountCurrency)
                .AddString("w.SOURCE_BANK_NAME = :SourceBankName", "SourceBankName", f.SourceBankName)
                .AddString("w.ACQUIRING_DEVICE_TYPE = :AcquiringDeviceType", "AcquiringDeviceType", f.AcquiringDeviceType)
                .AddString("w.TRANS_GROUP = :TransGroup", "TransGroup", f.TransGroup)
                .AddString("w.OPERATION_TYPE = :OperationType", "OperationType", f.OperationType)
                .AddString("w.TOKEN_STATUS = :TokenStatus", "TokenStatus", f.TokenStatus)
                .AddBool("w.IS_ISSUING", "IsIssuing", f.IsIssuing)
                .AddBool("w.IS_ACQUIRING", "IsAcquiring", f.IsAcquiring)
                .AddMonth("w.REPORT_MONTH", "Month",
                    f.ReportMonth == default ? null : f.ReportMonth);

            if (f.StartDate is not null)
                fb.Add("w.REPORT_MONTH >= TRUNC(:StartDate, 'MM')", "StartDate",
                    new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1));

            if (f.EndDate is not null)
                fb.Add("w.REPORT_MONTH <= TRUNC(:EndDate, 'MM')", "EndDate",
                    new DateTime(f.EndDate.Value.Year, f.EndDate.Value.Month, 1));

            return fb;
        }

        public async Task<PagedResult<WorldMapTransaction>> FilterAsync(
            WorldMapTransaction f,
            PageRequest? pageReq = null,
            CancellationToken ct = default)
        {
            var filter = BuildFilter(f);
            var sql = $"""
        SELECT
            w.REPORT_MONTH                                                                                      AS ReportMonth,
            w.BANK_NAME                                                                                         AS BankName,
            INITCAP(w.SOURCE_COUNTRY)                                                                           AS SourceCountry,
            w.SOURCE_COUNTRY_CATEGORY                                                                           AS SourceCountryCategory,
            INITCAP(w.TARGET_COUNTRY)                                                                           AS TargetCountry,
            w.TARGET_COUNTRY_CATEGORY                                                                           AS TargetCountryCategory,
            w.PAYMENT_SYSTEM                                                                                    AS PaymentSystem,
            CASE WHEN TO_CHAR(w.IS_ISSUING) = '1' THEN 1 WHEN TO_CHAR(w.IS_ISSUING) = '0' THEN 0 ELSE NULL END   AS IsIssuing,
            CASE WHEN TO_CHAR(w.IS_ACQUIRING) = '1' THEN 1 WHEN TO_CHAR(w.IS_ACQUIRING) = '0' THEN 0 ELSE NULL END AS IsAcquiring,
            SUM(w.TOTAL_AMOUNT)                                                                                 AS TotalAmount,
            SUM(w.TOTAL_COUNT)                                                                                  AS TotalCount,
            sc.latitude                                                                                         AS SourceLatitude,
            sc.longitude                                                                                        AS SourceLongitude,
            tc.latitude                                                                                         AS TargetLatitude,
            tc.longitude                                                                                        AS TargetLongitude
        FROM pg_mv_req5_world_map w
        LEFT JOIN countries sc ON UPPER(sc.name) = UPPER(w.SOURCE_COUNTRY)
        LEFT JOIN countries tc ON UPPER(tc.name) = UPPER(w.TARGET_COUNTRY)
        {filter.WhereClause}
        GROUP BY
            w.REPORT_MONTH,
            w.BANK_NAME,
            INITCAP(w.SOURCE_COUNTRY),
            w.SOURCE_COUNTRY_CATEGORY,
            INITCAP(w.TARGET_COUNTRY),
            w.TARGET_COUNTRY_CATEGORY,
            w.PAYMENT_SYSTEM,
            CASE WHEN TO_CHAR(w.IS_ISSUING) = '1' THEN 1 WHEN TO_CHAR(w.IS_ISSUING) = '0' THEN 0 ELSE NULL END,
            CASE WHEN TO_CHAR(w.IS_ACQUIRING) = '1' THEN 1 WHEN TO_CHAR(w.IS_ACQUIRING) = '0' THEN 0 ELSE NULL END,
            sc.latitude,
            sc.longitude,
            tc.latitude,
            tc.longitude
        """;

            if (pageReq is not null)
            {
                return await QueryPagedAsync<WorldMapTransaction>(
                    sql,
                    "ReportMonth DESC, TotalAmount DESC",
                    pageReq,
                    filter.Parameters,
                    ct);
            }

            var fullSql = $"""
                SELECT
                    MAX(w.REPORT_MONTH)         AS ReportMonth,
                    w.BANK_NAME                 AS BankName,
                    INITCAP(w.SOURCE_COUNTRY)   AS SourceCountry,
                    w.SOURCE_COUNTRY_CATEGORY   AS SourceCountryCategory,
                    INITCAP(w.TARGET_COUNTRY)   AS TargetCountry,
                    w.TARGET_COUNTRY_CATEGORY   AS TargetCountryCategory,
                    w.PAYMENT_SYSTEM            AS PaymentSystem,
                    SUM(w.TOTAL_AMOUNT)         AS TotalAmount,
                    SUM(w.TOTAL_COUNT)          AS TotalCount,
                    sc.latitude                 AS SourceLatitude,
                    sc.longitude                AS SourceLongitude,
                    tc.latitude                 AS TargetLatitude,
                    tc.longitude                AS TargetLongitude
                FROM pg_mv_req5_world_map w
                LEFT JOIN countries sc ON UPPER(sc.name) = UPPER(w.SOURCE_COUNTRY)
                LEFT JOIN countries tc ON UPPER(tc.name) = UPPER(w.TARGET_COUNTRY)
                {filter.WhereClause}
                GROUP BY
                    w.BANK_NAME,
                    INITCAP(w.SOURCE_COUNTRY),
                    w.SOURCE_COUNTRY_CATEGORY,
                    INITCAP(w.TARGET_COUNTRY),
                    w.TARGET_COUNTRY_CATEGORY,
                    w.PAYMENT_SYSTEM,
                    sc.latitude,
                    sc.longitude,
                    tc.latitude,
                    tc.longitude
                ORDER BY TotalAmount DESC
                """;
            using var conn = CreateConnection();
            var items = (await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(fullSql, filter.Parameters, cancellationToken: ct))).ToList();

            return new PagedResult<WorldMapTransaction>
            {
                Items = items,
                TotalCount = items.Count,
                Page = 1,
                PageSize = items.Count > 0 ? items.Count : 1
            };
        }

        private async Task<IEnumerable<WorldMapTransaction>> QueryAggregatedAsync(
            string? where, object? param, CancellationToken ct)
        {
            var sql = $"""
                SELECT
                    INITCAP(w.SOURCE_COUNTRY)                                               AS SourceCountry,
                    w.SOURCE_COUNTRY_CATEGORY                                               AS SourceCountryCategory,
                    INITCAP(w.TARGET_COUNTRY)                                               AS TargetCountry,
                    w.TARGET_COUNTRY_CATEGORY                                               AS TargetCountryCategory,
                    w.PAYMENT_SYSTEM                                                        AS PaymentSystem,
                    CASE WHEN MAX(TO_CHAR(w.IS_ISSUING)) = '1' THEN 1 ELSE 0 END            AS IsIssuing,
                    CASE WHEN MAX(TO_CHAR(w.IS_ACQUIRING)) = '1' THEN 1 ELSE 0 END          AS IsAcquiring,
                    SUM(w.TOTAL_AMOUNT)                                                     AS TotalAmount,
                    SUM(w.TOTAL_COUNT)                                                      AS TotalCount,
                    sc.latitude                                                             AS SourceLatitude,
                    sc.longitude                                                            AS SourceLongitude,
                    tc.latitude                                                             AS TargetLatitude,
                    tc.longitude                                                            AS TargetLongitude
                FROM pg_mv_req5_world_map w
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
                    CASE WHEN TO_CHAR(w.IS_ISSUING) = '1' THEN 1 WHEN TO_CHAR(w.IS_ISSUING) = '0' THEN 0 ELSE NULL END   AS IsIssuing,
                    CASE WHEN TO_CHAR(w.IS_ACQUIRING) = '1' THEN 1 WHEN TO_CHAR(w.IS_ACQUIRING) = '0' THEN 0 ELSE NULL END AS IsAcquiring,
                    w.TOTAL_AMOUNT                    AS TotalAmount,
                    w.TOTAL_COUNT                     AS TotalCount,
                    sc.latitude                       AS SourceLatitude,
                    sc.longitude                      AS SourceLongitude,
                    tc.latitude                       AS TargetLatitude,
                    tc.longitude                      AS TargetLongitude
                FROM pg_mv_req5_world_map w
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
            => QueryAggregatedAsync(
                "w.BANK_NAME = :BankName",
                new { BankName = bankName },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => QueryAggregatedAsync(
                "w.REPORT_MONTH = TRUNC(:Month, 'MM')",
                new { Month = month.Date },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(
            string country, CancellationToken ct = default)
            => QueryRawAsync(
                "UPPER(w.SOURCE_COUNTRY) = UPPER(:Country)",
                new { Country = country },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "w.BANK_NAME = :BankName AND w.IS_ISSUING = '1'",
                new { BankName = bankName },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "w.BANK_NAME = :BankName AND w.IS_ACQUIRING = '1'",
                new { BankName = bankName },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByBankAndMonthAsync(
            string bankName, DateTime month, CancellationToken ct = default)
            => QueryAggregatedAsync(
                "w.BANK_NAME = :BankName AND w.REPORT_MONTH = TRUNC(:Month, 'MM')",
                new { BankName = bankName, Month = month.Date },
                ct);

        public async Task<IEnumerable<string>> GetDistinctColumnValuesAsync(
            string columnName,
            CancellationToken ct = default)
        {
            if (!AllowedFilterColumns.Contains(columnName))
                return Array.Empty<string>();

            if (_columnCache.TryGetValue(columnName, out var cached) && DateTime.UtcNow < cached.Expiry)
                return cached.Values;

            var sql = $"""
                SELECT DISTINCT TO_CHAR({columnName}) AS VAL
                FROM pg_mv_req5_world_map
                WHERE {columnName} IS NOT NULL
                ORDER BY VAL
                """;

            try
            {
                using var conn = CreateConnection();
                var list = (await conn.QueryAsync<string>(
                    new CommandDefinition(sql, cancellationToken: ct)))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                _columnCache[columnName] = (list, DateTime.UtcNow.AddMinutes(30));
                return list;
            }
            catch (OracleException ex) when (ex.Number == 904)
            {
                return Array.Empty<string>();
            }
        }
    }
}
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class DevicesRepository : IDevicesRepository
    {
        private readonly string _connStr;

        public DevicesRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("PostgreSqlConnection")!;
        }

        private NpgsqlConnection Connect() => new(_connStr);

        // ─────────────────────────────────────────────
        // FILTER BUILDER (alias-safe)
        // ─────────────────────────────────────────────
        private static FilterBuilder BuildCommonFilter(
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory,
            string tableAlias = "b")
            => new FilterBuilder()
                .AddString($"{tableAlias}.bank_name = @BankName", "BankName", bankName)
                .AddString($"{tableAlias}.region_name = @RegionName", "RegionName", regionName)
                .AddString($"{tableAlias}.mcc_name = @MccName", "MccName", mccName)
                .AddString($"{tableAlias}.retail_category = @RetailCategory", "RetailCategory", retailCategory);

        // ─────────────────────────────────────────────
        // PAGINATION
        // ─────────────────────────────────────────────
        private async Task<PagedResult<T>> QueryPagedAsync<T>(
            string baseSql,
            string orderBy,
            PageRequest pageReq,
            object? parameters,
            CancellationToken ct = default)
        {
            var countSql = $"SELECT COUNT(*) FROM ({baseSql}) _cnt";
            var dataSql = $"""
                {baseSql}
                ORDER BY {orderBy}
                LIMIT @PageSize OFFSET @Offset
                """;

            var dynParams = new DynamicParameters(parameters);
            dynParams.Add("PageSize", pageReq.PageSize);
            dynParams.Add("Offset", (pageReq.Page - 1) * pageReq.PageSize);

            using var db = Connect();

            var total = await db.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: ct));

            var items = await db.QueryAsync<T>(
                new CommandDefinition(dataSql, dynParams, cancellationToken: ct));

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = total,
                Page = pageReq.Page,
                PageSize = pageReq.PageSize
            };
        }

        // ─────────────────────────────────────────────
        // FILTERS
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension)
        {
            var allowedColumns = new HashSet<string>
            {
                "bank_name", "region_name", "mcc_name", "retail_category"
            };

            var col = dimension.ToLower();
            if (!allowedColumns.Contains(col))
                throw new ArgumentException($"Unknown dimension: {dimension}");

            var sql = $"""
                SELECT DISTINCT {col} AS dim_value
                FROM bi_market_devices_masked
                WHERE {col} IS NOT NULL
                ORDER BY {col}
                """;

            using var db = Connect();
            return await db.QueryAsync<FilterValue>(sql);
        }

        // ─────────────────────────────────────────────
        // XY ANALYSIS (FIXED)
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime reportMonth,
            string xDimension,
            string yDimension,
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory)
        {
            var allowedColumns = new HashSet<string>
            {
                "bank_name", "region_name", "mcc_name", "retail_category"
            };

            var x = xDimension.ToLower();
            var y = yDimension.ToLower();

            if (!allowedColumns.Contains(x) || !allowedColumns.Contains(y))
                throw new ArgumentException("Invalid dimension");

            if (x == y)
                throw new ArgumentException("X and Y cannot be same");

            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory, "b")
                .AddMonth("b.report_month", "ReportMonth", reportMonth);

            var sql = $"""
                SELECT
                    b.{x} AS x_value,
                    b.{y} AS y_value,
                    SUM(b.total_devices) AS device_count,
                    ROUND(
                        SUM(b.total_devices) * 100.0 /
                        NULLIF(SUM(SUM(b.total_devices)) OVER (PARTITION BY b.{x}), 0),
                    2) AS share_pct,
                    SUM(t.mom_diff) AS mom_diff,
                    ROUND(AVG(t.mom_pct_change), 2) AS mom_pct_change
                FROM mv_devices_base b
                LEFT JOIN mv_devices_trend t
                ON  b.report_month      = t.report_month
                AND b.bank_name         = t.bank_name
                AND b.region_name       = t.region_name
                AND b.mcc_name          = t.mcc_name
                AND b.retail_category   = t.retail_category
                AND b.transaction_class = t.transaction_class
                AND b.ctls_status       = t.ctls_status
                {filter.WhereClause}
                GROUP BY b.{x}, b.{y}
                ORDER BY b.{x}, device_count DESC
                """;

            using var db = Connect();
            return await db.QueryAsync<XyItem>(sql, filter.Parameters);
        }

        // ─────────────────────────────────────────────
        // LATEST MONTH (FIXED COLUMN)
        // ─────────────────────────────────────────────
        public async Task<DateTime> GetLatestReportMonthAsync()
        {
            // Düzəliş:
            var sql = "SELECT MAX(report_month) FROM mv_devices_base";

            using var db = Connect();
            return await db.ExecuteScalarAsync<DateTime>(sql);
        }

        // ─────────────────────────────────────────────
        // TOTAL DEVICES
        // ─────────────────────────────────────────────
        public async Task<long> GetTotalDevicesAsync(
            DateTime reportMonth,
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory, "b")
                .AddMonth("b.report_month", "ReportMonth", reportMonth);

            var sql = $"""
                SELECT COALESCE(SUM(total_devices), 0)
                FROM mv_devices_base b
                {filter.WhereClause}
                """;

            using var db = Connect();
            return await db.ExecuteScalarAsync<long>(sql, filter.Parameters);
        }

        // ─────────────────────────────────────────────
        // SUMMARY
        // ─────────────────────────────────────────────
        public Task<PagedResult<SummaryItem>> GetSummaryPagedAsync(
            DateTime reportMonth,
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory, "b")
                .AddMonth("b.report_month", "ReportMonth", reportMonth);

            var baseSql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, grand_total_month
                FROM mv_devices_base b
                {filter.WhereClause}
                """;

            return QueryPagedAsync<SummaryItem>(baseSql, "total_devices DESC", pageReq, filter.Parameters, ct);
        }

        // ─────────────────────────────────────────────
        // SHARE
        // ─────────────────────────────────────────────
        public Task<PagedResult<ShareItem>> GetSharePagedAsync(
            DateTime reportMonth,
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory, "b")
                .AddMonth("b.report_month", "ReportMonth", reportMonth);

            var baseSql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, grand_total_month,
                    share_pct_by_bank, share_pct_by_region,
                    share_pct_by_mcc, share_pct_by_retail_cat
                FROM mv_devices_base b
                {filter.WhereClause}
                """;

            return QueryPagedAsync<ShareItem>(baseSql, "total_devices DESC", pageReq, filter.Parameters, ct);
        }

        // ─────────────────────────────────────────────
        // MOM
        // ─────────────────────────────────────────────
        public Task<PagedResult<MomItem>> GetMomComparisonPagedAsync(
            DateTime reportMonth,
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory, "t")
                .AddMonth("t.report_month", "ReportMonth", reportMonth);

            var baseSql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, prev_month_devices, mom_diff, mom_pct_change
                FROM mv_devices_trend t
                {filter.WhereClause}
                """;

            return QueryPagedAsync<MomItem>(baseSql, "total_devices DESC", pageReq, filter.Parameters, ct);
        }

        // ─────────────────────────────────────────────
        // TREND
        // ─────────────────────────────────────────────
        public Task<PagedResult<TrendItem>> GetTrendPagedAsync(
            DateTime dateFrom,
            DateTime dateTo,
            string? bankName,
            string? regionName,
            string? mccName,
            string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory, "t")
                .AddRange("t.report_month", "DateFrom", "DateTo", dateFrom, dateTo);

            var baseSql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, prev_month_devices, mom_diff, mom_pct_change
                FROM mv_devices_trend t
                {filter.WhereClause}
                """;

            return QueryPagedAsync<TrendItem>(baseSql, "report_month", pageReq, filter.Parameters, ct);
        }
    }
}
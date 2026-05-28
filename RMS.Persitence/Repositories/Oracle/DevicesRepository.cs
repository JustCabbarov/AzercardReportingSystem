using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RMS.Domain.Entities.Oracle.DeviceModel;
using RMS.Domain.Repositories.Oracle;
namespace RMS.Persitence.Repositories.Oracle
{
    public class DevicesRepository : IDevicesRepository
    {
        private readonly string _connStr;

        public DevicesRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("Postgres")!;
        }

        private NpgsqlConnection Connect() => new(_connStr);

        // ── shared filter builder ─────────────────────────────────────
        private static FilterBuilder BuildCommonFilter(
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => new FilterBuilder()
                .AddString("bank_name = @BankName", "BankName", bankName)
                .AddString("region_name = @RegionName", "RegionName", regionName)
                .AddString("mcc_name = @MccName", "MccName", mccName)
                .AddString("retail_category = @RetailCategory", "RetailCategory", retailCategory);

        // ── /api/filters ─────────────────────────────────────────────
        public async Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension)
        {
            var allowedColumns = new HashSet<string>
            {
                "bank_name", "region_name", "mcc_name", "retail_category"
            };

            var col = dimension.ToLower();
            if (!allowedColumns.Contains(col))
                throw new ArgumentException($"Namelum dimension: {dimension}");

            var sql = $"SELECT DISTINCT {col} AS dim_value FROM bi_market_devices_masked WHERE {col} IS NOT NULL ORDER BY {col}";

            using var db = Connect();
            return await db.QueryAsync<FilterValue>(sql);
        }

        // ── /api/summary ─────────────────────────────────────────────
        public async Task<IEnumerable<SummaryItem>> GetSummaryAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory)
                .AddMonth("report_month", "ReportMonth", reportMonth);

            var sql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, grand_total_month
                FROM mv_devices_base
                {filter.WhereClause}
                ORDER BY total_devices DESC
                """;

            using var db = Connect();
            return await db.QueryAsync<SummaryItem>(sql, filter.Parameters);
        }

        // ── /api/share ───────────────────────────────────────────────
        public async Task<IEnumerable<ShareItem>> GetShareAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory)
                .AddMonth("report_month", "ReportMonth", reportMonth);

            var sql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, grand_total_month,
                    share_pct_by_bank, share_pct_by_region,
                    share_pct_by_mcc, share_pct_by_retail_cat
                FROM mv_devices_base
                {filter.WhereClause}
                ORDER BY total_devices DESC
                """;

            using var db = Connect();
            return await db.QueryAsync<ShareItem>(sql, filter.Parameters);
        }

        // ── /api/mom-comparison ──────────────────────────────────────
        public async Task<IEnumerable<MomItem>> GetMomComparisonAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory)
                .AddMonth("report_month", "ReportMonth", reportMonth);

            var sql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, prev_month_devices, mom_diff, mom_pct_change
                FROM mv_devices_trend
                {filter.WhereClause}
                ORDER BY total_devices DESC
                """;

            using var db = Connect();
            return await db.QueryAsync<MomItem>(sql, filter.Parameters);
        }

        // ── /api/trend ───────────────────────────────────────────────
        public async Task<IEnumerable<TrendItem>> GetTrendAsync(
            DateTime dateFrom, DateTime dateTo,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
        {
            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory)
                .AddRange("report_month", "DateFrom", "DateTo", dateFrom, dateTo);

            var sql = $"""
                SELECT
                    report_month, bank_name, region_name, mcc_name,
                    retail_category, transaction_class, ctls_status,
                    total_devices, prev_month_devices, mom_diff, mom_pct_change
                FROM mv_devices_trend
                {filter.WhereClause}
                ORDER BY report_month
                """;

            using var db = Connect();
            return await db.QueryAsync<TrendItem>(sql, filter.Parameters);
        }

        // ── /api/xy-analysis ─────────────────────────────────────────
        public async Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime reportMonth,
            string xDimension, string yDimension,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
        {
            var allowedColumns = new HashSet<string>
            {
                "bank_name", "region_name", "mcc_name", "retail_category"
            };

            var x = xDimension.ToLower();
            var y = yDimension.ToLower();

            if (!allowedColumns.Contains(x) || !allowedColumns.Contains(y))
                throw new ArgumentException("Namelum dimension adlari");

            if (x == y)
                throw new ArgumentException("X ve Y eyni ola bilmez");

            var filter = BuildCommonFilter(bankName, regionName, mccName, retailCategory)
                .AddMonth("report_month", "ReportMonth", reportMonth);

            var sql = $"""
                SELECT
                    {x} AS x_value,
                    {y} AS y_value,
                    SUM(total_devices) AS device_count,
                    ROUND(SUM(total_devices) * 100.0 /
                        NULLIF(SUM(SUM(total_devices)) OVER (PARTITION BY {x}), 0), 2) AS share_pct
                FROM mv_devices_base
                {filter.WhereClause}
                GROUP BY {x}, {y}
                ORDER BY {x}, device_count DESC
                """;

            using var db = Connect();
            return await db.QueryAsync<XyItem>(sql, filter.Parameters);
        }
    }
}
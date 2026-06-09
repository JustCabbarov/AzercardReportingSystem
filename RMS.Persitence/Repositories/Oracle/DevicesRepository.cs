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
            _connStr = config.GetConnectionString("PostgreSqlConnection")!;
        }

        private NpgsqlConnection Connect() => new(_connStr);

        // ── shared filter builder ─────────────────────────────────────
        private static FilterBuilder BuildCommonFilter(
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories,
            string tableAlias = "")
        {
            var p = string.IsNullOrEmpty(tableAlias) ? "" : tableAlias + ".";

            return new FilterBuilder()
                .AddList($"{p}bank_name", "BankName", bankNames)
                .AddList($"{p}region_name", "RegionName", regionNames)
                .AddList($"{p}mcc_name", "MccName", mccNames)
                .AddList($"{p}retail_category", "RetailCategory", retailCategories);
        }

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
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
        {
            var filter = BuildCommonFilter(bankNames, regionNames, mccNames, retailCategories)
                .AddRange("report_month", "DateFrom", "DateTo", dateFrom, dateTo);

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
     DateTime dateFrom, DateTime dateTo,
     string dimension,
     List<string>? bankNames = null,
     List<string>? regionNames = null,
     List<string>? mccNames = null,
     List<string>? retailCategories = null)
        {
            var allowedColumns = new HashSet<string>
    {
        "bank_name", "region_name", "mcc_name", "retail_category"
    };

            var dim = string.IsNullOrWhiteSpace(dimension)
                ? "retail_category"
                : dimension.ToLower();

            if (!allowedColumns.Contains(dim))
                throw new ArgumentException($"Namelum dimension: {dimension}");

            // Filter-Dimension conflict yoxlanışı
            var filterConflicts = new Dictionary<string, bool>
    {
        { "bank_name",       bankNames?.Any()        == true },
        { "region_name",     regionNames?.Any()      == true },
        { "mcc_name",        mccNames?.Any()         == true },
        { "retail_category", retailCategories?.Any() == true }
    };

            if (filterConflicts.TryGetValue(dim, out var hasConflict) && hasConflict)
                throw new ArgumentException(
                    $"'{dim}' həm filter həm dimension kimi verilə bilməz");

            var filter = new FilterBuilder()
                .AddRange("report_month", "DateFrom", "DateTo", dateFrom, dateTo)
                .AddList("bank_name", "BankNames", bankNames)
                .AddList("region_name", "RegionNames", regionNames)
                .AddList("mcc_name", "MccNames", mccNames)
                .AddList("retail_category", "RetailCategories", retailCategories);

            var sql = $"""
        WITH filtered_data AS (
            SELECT *
            FROM mv_devices_base
            {filter.WhereClause}
        ),
        grouped_data AS (
            SELECT
                {dim}              AS dimension_value,
                SUM(total_devices) AS total_devices
            FROM filtered_data
            GROUP BY {dim}
        )
        SELECT
            dimension_value,
            total_devices,
            ROUND(
                total_devices * 100.0 / SUM(total_devices) OVER (),
                2
            ) AS share_pct
        FROM grouped_data
        ORDER BY total_devices DESC
        """;

            using var db = Connect();
            return await db.QueryAsync<ShareItem>(sql, filter.Parameters);
        }

        // ── /api/mom-comparison ──────────────────────────────────────
        public async Task<IEnumerable<MomItem>> GetMomComparisonAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
        {
            var filter = BuildCommonFilter(bankNames, regionNames, mccNames, retailCategories)
                .AddRange("report_month", "DateFrom", "DateTo", dateFrom, dateTo);

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
        public async Task<DeviceTrendResponse> GetTrendAsync(DeviceTrendRequest r)
        {
            var allowedColumns = new HashSet<string>
            {
                "bank_name", "region_name", "mcc_name", "retail_category"
            };

            var dim = string.IsNullOrWhiteSpace(r.Dimension) ? "retail_category" : r.Dimension.ToLower();
            if (!allowedColumns.Contains(dim))
                throw new ArgumentException($"Namelum dimension: {r.Dimension}");

            var filter = new FilterBuilder()
                .AddRange("report_month", "DateFrom", "DateTo", r.DateFrom, r.DateTo)
                .AddList(dim, "DimensionValues", r.DimensionValues);

            var sql = $"""
                SELECT
                    {dim}                         AS series_label,
                    report_month                  AS period,
                    SUM(total_devices)            AS cnt,
                    SUM(prev_month_devices)       AS prev_month_devices,
                    SUM(mom_diff)                 AS mom_diff,
                    ROUND(AVG(mom_pct_change), 2) AS mom_pct_change
                FROM mv_devices_trend
                {filter.WhereClause}
                GROUP BY {dim}, report_month
                ORDER BY {dim}, report_month
                """;

            using var db = Connect();
            var rows = await db.QueryAsync(sql, filter.Parameters);

            var series = rows
                .GroupBy(x => (string)x.series_label)
                .Select(g => new DeviceTrendSeries
                {
                    Label = g.Key,
                    Points = g.Select(x => new DeviceTrendPoint
                    {
                        Period = (DateTime)x.period,
                        Count = (long)x.cnt,
                        PrevMonthDevices = (long?)x.prev_month_devices,
                        MomDiff = (long?)x.mom_diff,
                        MomPctChange = (decimal?)x.mom_pct_change
                    }).ToList()
                }).ToList();

            return new DeviceTrendResponse { Series = series };
        }

        // ── /api/xy-analysis ─────────────────────────────────────────
        public async Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime dateFrom, DateTime dateTo,
            string xDimension, string yDimension,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
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

            var filter = BuildCommonFilter(bankNames, regionNames, mccNames, retailCategories, tableAlias: "b")
                .AddRange("b.report_month", "DateFrom", "DateTo", dateFrom, dateTo);

            var sql = $"""
                WITH grouped AS (
                    SELECT
                        b.{x} AS x_value,
                        b.{y} AS y_value,
                        SUM(b.total_devices) AS device_count,
                        SUM(t.mom_diff)      AS mom_diff,
                        ROUND(AVG(t.mom_pct_change), 2) AS mom_pct_change
                    FROM mv_devices_base b
                    LEFT JOIN mv_devices_trend t
                        ON  b.report_month    = t.report_month
                        AND b.bank_name       = t.bank_name
                        AND b.region_name     = t.region_name
                        AND b.mcc_name        = t.mcc_name
                        AND b.retail_category = t.retail_category
                    {filter.WhereClause}
                    GROUP BY b.{x}, b.{y}
                ),
                totals AS (
                    SELECT x_value, SUM(device_count) AS x_total
                    FROM grouped
                    GROUP BY x_value
                )
                SELECT
                    g.x_value,
                    g.y_value,
                    g.device_count,
                    ROUND(g.device_count * 100.0 / NULLIF(t.x_total, 0), 2) AS share_pct,
                    g.mom_diff,
                    g.mom_pct_change
                FROM grouped g
                JOIN totals t ON g.x_value = t.x_value
                ORDER BY g.x_value, g.device_count DESC
                """;

            using var db = Connect();
            return await db.QueryAsync<XyItem>(sql, filter.Parameters);
        }

        // ── /api/latest-month ─────────────────────────────────────────
        public async Task<DateTime> GetLatestReportMonthAsync()
        {
            var sql = "SELECT MAX(report_month) FROM mv_devices_base";
            using var db = Connect();
            return await db.ExecuteScalarAsync<DateTime>(sql);
        }

        // ── /api/total-devices ────────────────────────────────────────
        public async Task<object> GetTotalDevicesAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
        {
            var filter = BuildCommonFilter(bankNames, regionNames, mccNames, retailCategories)
                .AddRange("report_month", "DateFrom", "DateTo", dateFrom, dateTo);

            var sql = $"""
                SELECT
                    retail_category,
                    SUM(total_devices) AS total_devices
                FROM mv_devices_base
                {filter.WhereClause}
                GROUP BY retail_category
                ORDER BY total_devices DESC
                """;

            using var db = Connect();
            var items = (await db.QueryAsync<RetailCategoryTotalItem>(sql, filter.Parameters)).ToList();

            return new
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Total = items.Sum(x => x.TotalDevices),
                Categories = items
            };
        }
    }
}
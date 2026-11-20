using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities.Oracle.AcquiringTransaction;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.Oracle
{

    public class AcquiringDeviceRepository : IAcquiringDeviceRepository
    {
        private readonly string _connStr;

        public AcquiringDeviceRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("OracleConnection")
                ?? config.GetConnectionString("PostgreSqlConnection")!;
        }

        private OracleConnection Connect() => new(_connStr);

        private static readonly HashSet<string> AllowedDimensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "acquiring_device_type_calc", "trans_group", "payment_system",
            "source_bank_name", "target_bank_name", "target_city",
            "operation_type", "is_contactless", "mcc", "is_acquiring_category"
            };

        private static string SafeDim(string dim) =>
            AllowedDimensions.Contains(dim)
                ? dim.ToLower()
                : throw new ArgumentException($"Invalid dimension: {dim}");

        private static decimal GetDivisor(string unit) => unit switch
        {
            // Frontend (Mln/Mlrd) artıq client tərəfdə bölür — ikiqat bölmənin qarşısını almaq üçün
            // yalnız kiçik hərfli birbaşa API çağırışlarında divisor tətbiq olunur.
            "mlrd" => 1_000_000_000m,
            "mln" => 1_000_000m,
            _ => 1m
        };

        private static string GetValueExpr(string? category) => (category?.Trim().ToLowerInvariant()) switch
        {
            "count" => "SUM(transaction_count)",
            "avg" or "average" => "CASE WHEN SUM(transaction_count) = 0 THEN 0 " +
                                  "ELSE SUM(local_amount) / NULLIF(SUM(transaction_count), 0) END",
            _ => "SUM(local_amount)"
        };

        private static bool IsCountCategory(string? category) =>
            string.Equals(category?.Trim(), "count", StringComparison.OrdinalIgnoreCase);

        private static FilterBuilder BuildCommonFilter(AcquiringDeviceFilter f, string alias = "m")
            => new FilterBuilder()
                .AddTimestampRange($"{alias}.report_date", "FromDate", "ToDate", f.FromDate, f.ToDate)
                .AddList($"{alias}.acquiring_device_type_calc", "DeviceTypes", f.AcquiringDeviceTypes)
                .AddList($"{alias}.trans_group", "TransGroups", f.TransGroups)
                .AddList($"{alias}.operation_type", "OpTypes", f.OperationTypes)
                .AddList($"{alias}.token_status", "TokenStat", f.TokenStatuses)
                .AddList($"{alias}.mcc", "Mcc", f.Mccs)
                .AddList($"{alias}.is_acquiring_category", "AcqCat", f.AcquiringCategories)
                .AddList($"{alias}.target_bank_name", "TgtBank", f.TargetBankNames)
                .AddList($"{alias}.source_bank_name", "SrcBank", f.SourceBankNames)
                .AddList($"{alias}.target_city", "TgtCity", f.TargetCities)
                .AddList($"{alias}.payment_system", "PaySys", f.PaymentSystems)
                .AddList($"{alias}.is_contactless", "Contactless", f.ContactlessStatuses);

        private static AcquiringFilterOptionsResponse? _cachedFilterOptions;
        private static DateTime _filterOptionsCacheExpiry = DateTime.MinValue;

        private sealed class FilterOptionRow
        {
            public string Col_Key { get; set; } = "";
            public string? Col_Val { get; set; }
            public DateTime? Min_Date { get; set; }
            public DateTime? Max_Date { get; set; }
        }

        // ─────────────────────────────────────────────
        // FILTER OPTIONS
        // ─────────────────────────────────────────────
        public async Task<AcquiringFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken ct = default)
        {
            if (_cachedFilterOptions is not null && DateTime.UtcNow < _filterOptionsCacheExpiry)
                return _cachedFilterOptions;

            const string sql = """
                SELECT
                    CASE
                        WHEN GROUPING(acquiring_device_type_calc) = 0 THEN 'device'
                        WHEN GROUPING(trans_group) = 0                THEN 'trans'
                        WHEN GROUPING(operation_type) = 0             THEN 'op'
                        WHEN GROUPING(token_status) = 0               THEN 'token'
                        WHEN GROUPING(mcc) = 0                        THEN 'mcc'
                        WHEN GROUPING(is_acquiring_category) = 0      THEN 'acq_cat'
                        WHEN GROUPING(target_bank_name) = 0           THEN 'tgt_bank'
                        WHEN GROUPING(source_bank_name) = 0           THEN 'src_bank'
                        WHEN GROUPING(target_city) = 0                THEN 'tgt_city'
                        WHEN GROUPING(payment_system) = 0             THEN 'pay_sys'
                        WHEN GROUPING(is_contactless) = 0             THEN 'contactless'
                        ELSE 'minmax'
                    END AS col_key,
                    CASE
                        WHEN GROUPING(acquiring_device_type_calc) = 0 THEN TO_CHAR(acquiring_device_type_calc)
                        WHEN GROUPING(trans_group) = 0                THEN TO_CHAR(trans_group)
                        WHEN GROUPING(operation_type) = 0             THEN TO_CHAR(operation_type)
                        WHEN GROUPING(token_status) = 0               THEN TO_CHAR(token_status)
                        WHEN GROUPING(mcc) = 0                        THEN TO_CHAR(mcc)
                        WHEN GROUPING(is_acquiring_category) = 0      THEN TO_CHAR(is_acquiring_category)
                        WHEN GROUPING(target_bank_name) = 0           THEN TO_CHAR(target_bank_name)
                        WHEN GROUPING(source_bank_name) = 0           THEN TO_CHAR(source_bank_name)
                        WHEN GROUPING(target_city) = 0                THEN TO_CHAR(target_city)
                        WHEN GROUPING(payment_system) = 0             THEN TO_CHAR(payment_system)
                        WHEN GROUPING(is_contactless) = 0             THEN TO_CHAR(is_contactless)
                        ELSE NULL
                    END AS col_val,
                    CASE WHEN GROUPING_ID(
                        acquiring_device_type_calc, trans_group, operation_type, token_status,
                        mcc, is_acquiring_category, target_bank_name, source_bank_name,
                        target_city, payment_system, is_contactless) = 2047 THEN MIN(report_date) END AS min_date,
                    CASE WHEN GROUPING_ID(
                        acquiring_device_type_calc, trans_group, operation_type, token_status,
                        mcc, is_acquiring_category, target_bank_name, source_bank_name,
                        target_city, payment_system, is_contactless) = 2047 THEN MAX(report_date) END AS max_date
                FROM mv_acquiring_device_transaction
                GROUP BY GROUPING SETS (
                    (acquiring_device_type_calc),
                    (trans_group),
                    (operation_type),
                    (token_status),
                    (mcc),
                    (is_acquiring_category),
                    (target_bank_name),
                    (source_bank_name),
                    (target_city),
                    (payment_system),
                    (is_contactless),
                    ()
                )
                """;

            using var db = Connect();
            await db.OpenAsync(ct);

            var rows = (await db.QueryAsync<FilterOptionRow>(new CommandDefinition(
                sql,
                commandTimeout: 60,
                cancellationToken: ct))).ToList();

            List<string> Pick(string key) => rows
                .Where(r => r.Col_Key == key && !string.IsNullOrEmpty(r.Col_Val))
                .Select(r => r.Col_Val!)
                .Distinct()
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();

            var minmax = rows.FirstOrDefault(r => r.Col_Key == "minmax");

            var response = new AcquiringFilterOptionsResponse
            {
                AcquiringDeviceTypes = Pick("device"),
                TransGroups          = Pick("trans"),
                OperationTypes       = Pick("op"),
                TokenStatuses        = Pick("token"),
                Mccs                 = Pick("mcc"),
                AcquiringCategories  = Pick("acq_cat"),
                TargetBanks          = Pick("tgt_bank"),
                SourceBanks          = Pick("src_bank"),
                TargetCities         = Pick("tgt_city"),
                PaymentSystems       = Pick("pay_sys"),
                ContactlessStatuses  = Pick("contactless"),
                MinDate              = minmax?.Min_Date ?? DateTime.MinValue,
                MaxDate              = minmax?.Max_Date ?? DateTime.MinValue,
            };

            _cachedFilterOptions = response;
            _filterOptionsCacheExpiry = DateTime.UtcNow.AddMinutes(30);
            return response;
        }

        // ─────────────────────────────────────────────
        // LATEST REPORT DATE
        // ─────────────────────────────────────────────
        public async Task<DateTime> GetLatestReportDateAsync()
        {
            const string sql = "SELECT MAX(report_date) FROM mv_acquiring_device_transaction";
            using var db = Connect();
            return await db.ExecuteScalarAsync<DateTime>(sql);
        }

        // ─────────────────────────────────────────────
        // DEVICE SUMMARY
        // ─────────────────────────────────────────────
        public async Task<List<DeviceSummaryRow>> GetDeviceSummaryAsync(AcquiringDeviceFilter f)
        {
            var filter = BuildCommonFilter(f);
            var divisor = GetDivisor(f.AmountUnit);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            SELECT
                acquiring_device_type_calc                                    AS DeviceType,
                ROUND(SUM(local_amount) / :Divisor, 2)                        AS Volume,
                SUM(transaction_count)                                        AS Count,
                ROUND(
                    CASE WHEN SUM(transaction_count) = 0 THEN 0
                         ELSE SUM(local_amount) / NULLIF(SUM(transaction_count), 0)
                    END, 2)                                                   AS AvgAmount
            FROM mv_acquiring_device_transaction m
            {filter.WhereClause}
            GROUP BY acquiring_device_type_calc
            ORDER BY Volume DESC
            """;

            using var db = Connect();
            return (await db.QueryAsync<DeviceSummaryRow>(sql, dp)).ToList();
        }

        // ─────────────────────────────────────────────
        // PIE CHART
        // ─────────────────────────────────────────────
        public async Task<List<AcqPieItem>> GetPieChartAsync(AcquiringDeviceFilter f)
        {
            var filter = BuildCommonFilter(f);
            var divisor = IsCountCategory(f.Category) ? 1m : GetDivisor(f.AmountUnit);
            var valExpr = GetValueExpr(f.Category);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            WITH totals AS (
                SELECT
                    acquiring_device_type_calc AS label,
                    {valExpr}                  AS val,
                    SUM(transaction_count)     AS cnt
                FROM mv_acquiring_device_transaction m
                {filter.WhereClause}
                GROUP BY acquiring_device_type_calc
            ),
            grand AS (SELECT SUM(val) AS g FROM totals)
            SELECT
                label                                    AS Label,
                ROUND(val / :Divisor, 2)                 AS Value,
                cnt                                      AS Count,
                ROUND(val * 100.0 / NULLIF(g, 0), 2)     AS Percent
            FROM totals CROSS JOIN grand
            ORDER BY val DESC
            """;

            using var db = Connect();
            return (await db.QueryAsync<AcqPieItem>(sql, dp)).ToList();
        }

        // ─────────────────────────────────────────────
        // TRANS GROUP CHART
        // ─────────────────────────────────────────────
        public async Task<List<TransGroupItem>> GetTransGroupChartAsync(AcquiringDeviceFilter f)
        {
            var filter = BuildCommonFilter(f);
            var divisor = IsCountCategory(f.Category) ? 1m : GetDivisor(f.AmountUnit);
            var valExpr = GetValueExpr(f.Category);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            WITH totals AS (
                SELECT
                    trans_group            AS label,
                    {valExpr}              AS val,
                    SUM(transaction_count) AS cnt
                FROM mv_acquiring_device_transaction m
                {filter.WhereClause}
                GROUP BY trans_group
            ),
            grand AS (SELECT SUM(val) AS g FROM totals)
            SELECT
                label                                    AS TransGroup,
                ROUND(val / :Divisor, 2)                 AS Value,
                cnt                                      AS Count,
                ROUND(val * 100.0 / NULLIF(g, 0), 2)     AS Percent
            FROM totals CROSS JOIN grand
            ORDER BY val DESC
            """;

            using var db = Connect();
            return (await db.QueryAsync<TransGroupItem>(sql, dp)).ToList();
        }

        // ─────────────────────────────────────────────
        // PAYMENT SYSTEM CHART
        // ─────────────────────────────────────────────
        public async Task<List<PaymentSystemItem>> GetPaymentSystemChartAsync(AcquiringDeviceFilter f)
        {
            var filter = BuildCommonFilter(f);
            var divisor = IsCountCategory(f.Category) ? 1m : GetDivisor(f.AmountUnit);
            var valExpr = GetValueExpr(f.Category);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            WITH totals AS (
                SELECT
                    payment_system         AS label,
                    {valExpr}              AS val,
                    SUM(transaction_count) AS cnt
                FROM mv_acquiring_device_transaction m
                {filter.WhereClause}
                GROUP BY payment_system
            ),
            grand AS (SELECT SUM(val) AS g FROM totals)
            SELECT
                label                                    AS PaymentSystem,
                ROUND(val / :Divisor, 2)                 AS Value,
                cnt                                      AS Count,
                ROUND(val * 100.0 / NULLIF(g, 0), 2)     AS Percent
            FROM totals CROSS JOIN grand
            ORDER BY val DESC
            """;

            using var db = Connect();
            return (await db.QueryAsync<PaymentSystemItem>(sql, dp)).ToList();
        }

        // ─────────────────────────────────────────────
        // TREND CHART + PROQNOZ
        // ─────────────────────────────────────────────
        public async Task<AcqTrendResponse> GetTrendAsync(AcquiringTrendRequest r)
        {
            var dim = SafeDim(r.Dimension);
            var filter = BuildCommonFilter(r);

            if (r.DimValues?.Count > 0)
                filter.AddList($"m.{dim}", "DimValues", r.DimValues);

            var truncExpr = (r.Granularity?.Trim().ToLowerInvariant()) switch
            {
                "year"    => "TRUNC(period_month, 'YYYY')",
                "quarter" => "TRUNC(period_month, 'Q')",
                "day"     => "TRUNC(report_date, 'DD')",
                _         => "period_month"
            };

            var divisor = IsCountCategory(r.Category) ? 1m : GetDivisor(r.AmountUnit);
            var valExpr = GetValueExpr(r.Category);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            SELECT
                {dim}                              AS series_label,
                {truncExpr}                        AS period,
                ROUND({valExpr} / :Divisor, 2)     AS actual
            FROM mv_acquiring_device_transaction m
            {filter.WhereClause}
            GROUP BY {dim}, {truncExpr}
            ORDER BY {dim}, period
            """;

            using var db = Connect();
            var rows = (await db.QueryAsync<AcqTrendDbRow>(sql, dp)).ToList();

            var series = rows
                .GroupBy(x => x.Series_Label ?? "")
                .Select(g =>
                {
                    var points = g.Select(x => new AcqTrendPoint
                    {
                        Period = x.Period,
                        Actual = x.Actual
                    }).ToList();

                    return new AcqTrendSeries { Label = g.Key, Points = points };
                }).ToList();

            return new AcqTrendResponse { Series = series };
        }

        private record AcqTrendDbRow
        {
            public string? Series_Label { get; set; }
            public DateTime Period { get; set; }
            public decimal Actual { get; set; }
        }

     

        // ─────────────────────────────────────────────
        // BANK CHARTS
        // ─────────────────────────────────────────────
        public async Task<List<BankItem>> GetSourceBankChartAsync(AcquiringDeviceFilter f)
            => await GetBankChartInternalAsync(f, "source_bank_name");

        public async Task<List<BankItem>> GetTargetBankChartAsync(AcquiringDeviceFilter f)
            => await GetBankChartInternalAsync(f, "target_bank_name");

        private async Task<List<BankItem>> GetBankChartInternalAsync(
            AcquiringDeviceFilter f, string bankColumn)
        {
            var filter = BuildCommonFilter(f).AddRaw($"{bankColumn} IS NOT NULL");
            var divisor = IsCountCategory(f.Category) ? 1m : GetDivisor(f.AmountUnit);
            var valExpr = GetValueExpr(f.Category);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            WITH totals AS (
                SELECT
                    {bankColumn}           AS label,
                    {valExpr}              AS val,
                    SUM(transaction_count) AS cnt
                FROM mv_acquiring_device_transaction m
                {filter.WhereClause}
                GROUP BY {bankColumn}
            ),
            grand AS (SELECT SUM(val) AS g FROM totals)
            SELECT
                label                                    AS BankName,
                ROUND(val / :Divisor, 2)                 AS Value,
                cnt                                      AS Count,
                ROUND(val * 100.0 / NULLIF(g, 0), 2)     AS Percent
            FROM totals CROSS JOIN grand
            ORDER BY val DESC
            FETCH FIRST 20 ROWS ONLY
            """;

            using var db = Connect();
            return (await db.QueryAsync<BankItem>(sql, dp)).ToList();
        }

        // ─────────────────────────────────────────────
        // DASHBOARD — bütün qrafiklər paralel
        // ─────────────────────────────────────────────
        public async Task<AcquiringDashboardResponse> GetDashboardAsync(AcquiringDeviceFilter f)
        {
            var t1 = GetDeviceSummaryAsync(f);
            var t2 = GetPieChartAsync(f);
            var t3 = GetTransGroupChartAsync(f);
            var t4 = GetPaymentSystemChartAsync(f);
            var t5 = GetSourceBankChartAsync(f);
            var t6 = GetTargetBankChartAsync(f);

            await Task.WhenAll(t1, t2, t3, t4, t5, t6);

            return new AcquiringDashboardResponse
            {
                DeviceSummary = await t1,
                PieChart = await t2,
                TransGroupChart = await t3,
                PaymentSysChart = await t4,
                SourceBankChart = await t5,
                TargetBankChart = await t6,
            };
        }
    }
}


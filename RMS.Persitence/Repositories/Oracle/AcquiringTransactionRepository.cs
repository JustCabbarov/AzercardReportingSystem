using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
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
            _connStr = config.GetConnectionString("PostgreSqlConnection")!;
        }

        private NpgsqlConnection Connect() => new(_connStr);

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
            "mlrd" => 1_000_000_000m,
            "mln" => 1_000_000m,
            _ => 1m
        };

        private static string GetValueExpr(string category) => category switch
        {
            "count" => "SUM(transaction_count)",
            "avg" => "CASE WHEN SUM(transaction_count) = 0 THEN 0 " +
                       "ELSE SUM(local_amount) / NULLIF(SUM(transaction_count), 0) END",
            _ => "SUM(local_amount)"
        };

        private static FilterBuilder BuildCommonFilter(AcquiringDeviceFilter f, string alias = "m")
            => new FilterBuilder()
                .AddRange($"{alias}.report_date", "FromDate", "ToDate", f.FromDate, f.ToDate)
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

        // ─────────────────────────────────────────────
        // FILTER OPTIONS
        // ─────────────────────────────────────────────
        public async Task<AcquiringFilterOptionsResponse> GetFilterOptionsAsync()
        {
            const string sql = """
            SELECT
                ARRAY_AGG(DISTINCT acquiring_device_type_calc ORDER BY acquiring_device_type_calc)
                    FILTER (WHERE acquiring_device_type_calc IS NOT NULL) AS device_types,
                ARRAY_AGG(DISTINCT trans_group       ORDER BY trans_group)
                    FILTER (WHERE trans_group IS NOT NULL)                AS trans_groups,
                ARRAY_AGG(DISTINCT operation_type    ORDER BY operation_type)
                    FILTER (WHERE operation_type IS NOT NULL)             AS op_types,
                ARRAY_AGG(DISTINCT token_status      ORDER BY token_status)
                    FILTER (WHERE token_status IS NOT NULL)               AS token_statuses,
                ARRAY_AGG(DISTINCT mcc               ORDER BY mcc)
                    FILTER (WHERE mcc IS NOT NULL)                        AS mccs,
                ARRAY_AGG(DISTINCT is_acquiring_category ORDER BY is_acquiring_category)
                    FILTER (WHERE is_acquiring_category IS NOT NULL)      AS acq_categories,
                ARRAY_AGG(DISTINCT target_bank_name  ORDER BY target_bank_name)
                    FILTER (WHERE target_bank_name IS NOT NULL)           AS target_banks,
                ARRAY_AGG(DISTINCT source_bank_name  ORDER BY source_bank_name)
                    FILTER (WHERE source_bank_name IS NOT NULL)           AS source_banks,
                ARRAY_AGG(DISTINCT target_city       ORDER BY target_city)
                    FILTER (WHERE target_city IS NOT NULL)                AS target_cities,
                ARRAY_AGG(DISTINCT payment_system    ORDER BY payment_system)
                    FILTER (WHERE payment_system IS NOT NULL)             AS payment_systems,
                ARRAY_AGG(DISTINCT is_contactless::text ORDER BY is_contactless::text)
                    FILTER (WHERE is_contactless IS NOT NULL)             AS contactless,
                MIN(report_date) AS min_date,
                MAX(report_date) AS max_date
            FROM mv_acquiring_device_transaction
            """;

            using var db = Connect();
            var row = await db.QuerySingleAsync(sql);

            return new AcquiringFilterOptionsResponse
            {
                AcquiringDeviceTypes = ((string[])row.device_types ?? []).ToList(),
                TransGroups = ((string[])row.trans_groups ?? []).ToList(),
                OperationTypes = ((string[])row.op_types ?? []).ToList(),
                TokenStatuses = ((string[])row.token_statuses ?? []).ToList(),
                Mccs = ((string[])row.mccs ?? []).ToList(),
                AcquiringCategories = ((string[])row.acq_categories ?? []).ToList(),
                TargetBanks = ((string[])row.target_banks ?? []).ToList(),
                SourceBanks = ((string[])row.source_banks ?? []).ToList(),
                TargetCities = ((string[])row.target_cities ?? []).ToList(),
                PaymentSystems = ((string[])row.payment_systems ?? []).ToList(),
                ContactlessStatuses = ((string[])row.contactless ?? []).ToList(),
                MinDate = (DateTime)row.min_date,
                MaxDate = (DateTime)row.max_date,
            };
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
                ROUND(SUM(local_amount) / @Divisor, 2)                        AS Volume,
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
            var divisor = GetDivisor(f.AmountUnit);
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
                ROUND(val / @Divisor, 2)                 AS Value,
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
            var divisor = GetDivisor(f.AmountUnit);
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
                ROUND(val / @Divisor, 2)                 AS Value,
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
            var divisor = GetDivisor(f.AmountUnit);
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
                ROUND(val / @Divisor, 2)                 AS Value,
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

            var truncExpr = r.Granularity == "year"
                ? "DATE_TRUNC('year', report_date)::DATE"
                : "DATE_TRUNC('month', report_date)::DATE";

            var divisor = GetDivisor(r.AmountUnit);
            var valExpr = GetValueExpr(r.Category);
            var dp = new DynamicParameters(filter.Parameters);
            dp.Add("Divisor", divisor);

            var sql = $"""
            SELECT
                {dim}                              AS series_label,
                {truncExpr}                        AS period,
                ROUND({valExpr} / @Divisor, 2)     AS actual
            FROM mv_acquiring_device_transaction m
            {filter.WhereClause}
            GROUP BY {dim}, {truncExpr}
            ORDER BY {dim}, period
            """;

            using var db = Connect();
            var rows = (await db.QueryAsync(sql, dp)).ToList();

            var series = rows
                .GroupBy(x => (string)(x.series_label ?? ""))
                .Select(g =>
                {
                    var points = g.Select(x => new AcqTrendPoint
                    {
                        Period = (DateTime)x.period,
                        Actual = (decimal)x.actual
                    }).ToList();

                 

                    return new AcqTrendSeries { Label = g.Key, Points = points };
                }).ToList();

            return new AcqTrendResponse { Series = series };
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
            var divisor = GetDivisor(f.AmountUnit);
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
                ROUND(val / @Divisor, 2)                 AS Value,
                cnt                                      AS Count,
                ROUND(val * 100.0 / NULLIF(g, 0), 2)     AS Percent
            FROM totals CROSS JOIN grand
            ORDER BY val DESC
            LIMIT 20
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


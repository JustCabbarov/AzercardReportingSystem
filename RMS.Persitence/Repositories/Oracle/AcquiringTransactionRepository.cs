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
            using var db = Connect();

            var minMax = await db.QuerySingleAsync<(DateTime min_date, DateTime max_date)>(
                "SELECT MIN(report_date) AS min_date, MAX(report_date) AS max_date FROM mv_acquiring_device_transaction");

            var deviceTypes = (await db.QueryAsync<string>(
                "SELECT DISTINCT acquiring_device_type_calc FROM mv_acquiring_device_transaction WHERE acquiring_device_type_calc IS NOT NULL ORDER BY acquiring_device_type_calc")).ToList();
            var transGroups = (await db.QueryAsync<string>(
                "SELECT DISTINCT trans_group FROM mv_acquiring_device_transaction WHERE trans_group IS NOT NULL ORDER BY trans_group")).ToList();
            var opTypes = (await db.QueryAsync<string>(
                "SELECT DISTINCT operation_type FROM mv_acquiring_device_transaction WHERE operation_type IS NOT NULL ORDER BY operation_type")).ToList();
            var tokenStatuses = (await db.QueryAsync<string>(
                "SELECT DISTINCT token_status FROM mv_acquiring_device_transaction WHERE token_status IS NOT NULL ORDER BY token_status")).ToList();
            var mccs = (await db.QueryAsync<string>(
                "SELECT DISTINCT mcc FROM mv_acquiring_device_transaction WHERE mcc IS NOT NULL ORDER BY mcc")).ToList();
            var acqCategories = (await db.QueryAsync<string>(
                "SELECT DISTINCT is_acquiring_category FROM mv_acquiring_device_transaction WHERE is_acquiring_category IS NOT NULL ORDER BY is_acquiring_category")).ToList();
            var targetBanks = (await db.QueryAsync<string>(
                "SELECT DISTINCT target_bank_name FROM mv_acquiring_device_transaction WHERE target_bank_name IS NOT NULL ORDER BY target_bank_name")).ToList();
            var sourceBanks = (await db.QueryAsync<string>(
                "SELECT DISTINCT source_bank_name FROM mv_acquiring_device_transaction WHERE source_bank_name IS NOT NULL ORDER BY source_bank_name")).ToList();
            var targetCities = (await db.QueryAsync<string>(
                "SELECT DISTINCT target_city FROM mv_acquiring_device_transaction WHERE target_city IS NOT NULL ORDER BY target_city")).ToList();
            var paymentSystems = (await db.QueryAsync<string>(
                "SELECT DISTINCT payment_system FROM mv_acquiring_device_transaction WHERE payment_system IS NOT NULL ORDER BY payment_system")).ToList();
            var contactless = (await db.QueryAsync<string>(
                "SELECT DISTINCT TO_CHAR(is_contactless) FROM mv_acquiring_device_transaction WHERE is_contactless IS NOT NULL ORDER BY TO_CHAR(is_contactless)")).ToList();

            return new AcquiringFilterOptionsResponse
            {
                AcquiringDeviceTypes = deviceTypes,
                TransGroups = transGroups,
                OperationTypes = opTypes,
                TokenStatuses = tokenStatuses,
                Mccs = mccs,
                AcquiringCategories = acqCategories,
                TargetBanks = targetBanks,
                SourceBanks = sourceBanks,
                TargetCities = targetCities,
                PaymentSystems = paymentSystems,
                ContactlessStatuses = contactless,
                MinDate = minMax.min_date,
                MaxDate = minMax.max_date,
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

            var truncExpr = r.Granularity == "year"
                ? "TRUNC(period_month, 'YYYY')"
                : "period_month";

            var divisor = GetDivisor(r.AmountUnit);
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


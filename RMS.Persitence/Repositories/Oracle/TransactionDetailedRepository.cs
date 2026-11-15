using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities.Oracle.TransactionDeteiled;
using RMS.Domain.Repositories;

namespace RMS.Persitence.Repositories.Oracle
{
    public class TransactionDetailedRepository : ITransactionDetailedRepository
    {
        private readonly string _connStr;

        public TransactionDetailedRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("OracleConnection")
                ?? config.GetConnectionString("PostgreSqlConnection")!;
        }

        private OracleConnection Connect() => new(_connStr);

        // =============================================
        // 1. KPI + Issuing Category + Target Bank bölgüsü
        // =============================================
        public async Task<TransactionSummaryResponse> GetSummaryAsync(TransactionFilterRequest filter)
        {
            var builder = BuildFilters(filter);

            var measureCol = GetMeasureColumn(filter.Measure);
            var where = builder.WhereClause;
            var param = builder.Parameters;

            var catSql = $@"
                SELECT
                    issuing_category                        AS Label,
                    SUM(total_local_amount)                 AS Amount,
                    SUM(total_count)                        AS Count,
                    ROUND(
                        SUM({measureCol}) * 100.0 /
                        NULLIF(SUM(SUM({measureCol})) OVER (), 0)
                    , 2)                                    AS Percent
                FROM mv_transaction_summary
                {where}
                GROUP BY issuing_category
                ORDER BY Amount DESC";

            var targetBuilder = BuildFilters(filter, includeTargetBank: false);
            var targetSql = $@"
                SELECT
                    target_bank_name                        AS Label,
                    SUM(total_local_amount)                 AS Amount,
                    SUM(total_count)                        AS Count,
                    ROUND(
                        SUM({measureCol}) * 100.0 /
                        NULLIF(SUM(SUM({measureCol})) OVER (), 0)
                    , 2)                                    AS Percent
                FROM mv_transaction_summary
                {targetBuilder.WhereClause}
                GROUP BY target_bank_name
                ORDER BY Amount DESC";

            using var con1 = Connect();
            using var con2 = Connect();
            var catTask = con1.QueryAsync<DistributionRaw>(catSql, param);
            var targetTask = con2.QueryAsync<DistributionRaw>(targetSql, targetBuilder.Parameters);
            await Task.WhenAll(catTask, targetTask);

            var categories = (await catTask).Select(r => new CategoryDistributionDto
            {
                Label = r.Label,
                Amount = r.Amount,
                Count = r.Count,
                Percent = r.Percent
            }).ToList();

            var targetBanks = (await targetTask).Select(r => new BankDistributionDto
            {
                BankName = r.Label,
                Amount = r.Amount,
                Count = r.Count,
                Percent = r.Percent
            }).ToList();

            return new TransactionSummaryResponse
            {
                Kpi = new KpiDto
                {
                    TotalAmount = categories.Sum(c => c.Amount),
                    TotalCount = categories.Sum(c => c.Count)
                },
                IssuingCategories = categories,
                TargetBanks = targetBanks
            };
        }

        // =============================================
        // 2. Source Bank bölgüsü (kaskad — Bank sütunu SOURCE_BANK_NAME göstərir)
        // =============================================
        public async Task<List<BankDistributionDto>> GetTargetBanksAsync(TransactionFilterRequest filter)
        {
            var builder = BuildFilters(filter, includeSourceBank: false);

            var measureCol = GetMeasureColumn(filter.Measure);
            var where = builder.WhereClause;
            var param = builder.Parameters;

            var sql = $@"
                SELECT
                    source_bank_name                        AS Label,
                    SUM(total_local_amount)                 AS Amount,
                    SUM(total_count)                        AS Count,
                    ROUND(
                        SUM({measureCol}) * 100.0 /
                        NULLIF(SUM(SUM({measureCol})) OVER (), 0)
                    , 2)                                    AS Percent
                FROM mv_transaction_summary
                {where}
                GROUP BY source_bank_name
                ORDER BY Amount DESC";

            using var con = Connect();
            var rows = await con.QueryAsync<DistributionRaw>(sql, param);

            return rows.Select(r => new BankDistributionDto
            {
                BankName = r.Label,
                Amount = r.Amount,
                Count = r.Count,
                Percent = r.Percent
            }).ToList();
        }

        // =============================================
        // 3. Acquiring Device bölgüsü
        // =============================================
        public async Task<List<DeviceDistributionDto>> GetAcquiringDevicesAsync(TransactionFilterRequest filter)
        {
            var builder = BuildFilters(filter);

            var measureCol = GetMeasureColumn(filter.Measure);
            var where = builder.WhereClause;
            var param = builder.Parameters;

            var sql = $@"
                SELECT
                    acquiring_device_type                   AS Label,
                    SUM(total_local_amount)                 AS Amount,
                    SUM(total_count)                        AS Count,
                    ROUND(
                        SUM({measureCol}) * 100.0 /
                        NULLIF(SUM(SUM({measureCol})) OVER (), 0)
                    , 2)                                    AS Percent
                FROM mv_transaction_summary
                {where}
                GROUP BY acquiring_device_type
                ORDER BY Amount DESC";

            using var con = Connect();
            var rows = await con.QueryAsync<DistributionRaw>(sql, param);

            return rows.Select(r => new DeviceDistributionDto
            {
                DeviceType = r.Label,
                Amount = r.Amount,
                Count = r.Count,
                Percent = r.Percent
            }).ToList();
        }

        // =============================================
        // Köməkçi metodlar
        // =============================================
        private static FilterBuilder BuildFilters(
            TransactionFilterRequest filter,
            bool includeTargetBank = true,
            bool includeSourceBank = true)
        {
            var builder = new FilterBuilder();

            builder
                .AddTimestampRange("report_day", "dateFrom", "dateTo", filter.DateFrom, filter.DateTo)
                .AddString("issuing_category = :issuingCategory", "issuingCategory", filter.IssuingCategory)
                .AddString("payment_system = :paymentSystem", "paymentSystem", filter.PaymentSystem)
                .AddString("acquiring_device_type = :device", "device", filter.AcquiringDevice)
                .AddString("card_product_type_category = :cardType", "cardType", filter.CardType)
                .AddString("transaction_currency = :currency", "currency", filter.Currency)
                .AddString("operation_type = :operationType", "operationType", filter.OperationType)
                .AddString("trans_group = :transGroup", "transGroup", filter.TransGroup)
                .AddString("token_status = :tokenStatus", "tokenStatus", filter.TokenStatus);

            if (includeSourceBank)
                builder.AddString("source_bank_name = :sourceBankName", "sourceBankName", filter.SourceBankName);

            if (includeTargetBank)
                builder.AddString("target_bank_name = :targetBank", "targetBank", filter.TargetBankName);

            return builder;
        }

        private static string GetMeasureColumn(MeasureType measure) => measure switch
        {
            MeasureType.AZN => "total_local_amount",
            MeasureType.SAY => "total_count",
            MeasureType.R => "total_settlement_amount",
            MeasureType.PCT => "total_local_amount",
            _ => "total_local_amount"
        };

        private class DistributionRaw
        {
            public string Label { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public long Count { get; set; }
            public decimal Percent { get; set; }
        }
    }
}
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RMS.Domain.Entities.Oracle.SummaryTransaction;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.Oracle
{


    public class SummaryTransactionRepository : ISummaryTransactionRepository
    {
        private readonly string _connStr;

        // Brifdəki sabit device sırası
        private static readonly string[] DeviceOrder = ["POS", "ATM", "ECO", "C2C", "UFX"];

        public SummaryTransactionRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("PostgreSqlConnection")!;
        }

        private NpgsqlConnection Connect() => new(_connStr);

        public async Task<SummaryTransactionResponse> GetSummaryAsync(SummaryFilterRequest filter)
        {
            var builder = BuildFilters(filter);
            var scale = (int)filter.Scale;

            var sql = $@"
            SELECT
                is_issuing                              AS IsIssuing,
                is_acquiring                            AS IsAcquiring,
                acquiring_device_type                   AS AcquiringDeviceType,
                SUM(total_local_amount) / {scale}       AS TotalLocalAmount,
                SUM(total_count)                        AS TotalCount
            FROM public.mv_summary_transaction
            {builder.WhereClause}
            GROUP BY is_issuing, is_acquiring, acquiring_device_type";

            using var con = Connect();
            var rows = (await con.QueryAsync<SummaryRaw>(sql, builder.Parameters)).ToList();

            return new SummaryTransactionResponse
            {
                // 1. Total — bütün sətirlər
                Total = new KpiBlockDto
                {
                    Amount = rows.Sum(r => r.TotalLocalAmount),
                    Count = rows.Sum(r => r.TotalCount),
                    Devices = []
                },

                // 2. Issuing (IS_ISSUING=1, IS_ACQUIRING=0)
                Issuing = BuildBlock(rows, isIssuing: "1", isAcquiring: "0"),

                // 3. Inner (IS_ISSUING=1, IS_ACQUIRING=1)
                Inner = BuildBlock(rows, isIssuing: "1", isAcquiring: "1"),

                // 4. Acquiring (IS_ISSUING=0, IS_ACQUIRING=1)
                Acquiring = BuildBlock(rows, isIssuing: "0", isAcquiring: "1"),
            };
        }

        // ─── Blok qur: KPI + sabit 5 device ──────────────────────────────────────
        private static KpiBlockDto BuildBlock(
            List<SummaryRaw> rows, string isIssuing, string isAcquiring)
        {
            var filtered = rows
                .Where(r => r.IsIssuing == isIssuing && r.IsAcquiring == isAcquiring)
                .ToList();

            // Device-ları lookup-a çevir
            var lookup = filtered
                .Where(r => !string.IsNullOrWhiteSpace(r.AcquiringDeviceType))
                .GroupBy(r => r.AcquiringDeviceType!)
                .ToDictionary(g => g.Key, g => new
                {
                    Amount = g.Sum(x => x.TotalLocalAmount),
                    Count = g.Sum(x => x.TotalCount)
                });

            // Brifdəki sabit sıra ilə 5 device — datada yoxdursa 0 gəlir
            var devices = DeviceOrder.Select(d => new DeviceBreakdownDto
            {
                DeviceType = d,
                Amount = lookup.TryGetValue(d, out var v) ? v.Amount : 0,
                Count = lookup.TryGetValue(d, out var v2) ? v2.Count : 0
            }).ToList();

            return new KpiBlockDto
            {
                Amount = filtered.Sum(r => r.TotalLocalAmount),
                Count = filtered.Sum(r => r.TotalCount),
                Devices = devices
            };
        }

        // ─── Filter builder ───────────────────────────────────────────────────────
        private static FilterBuilder BuildFilters(SummaryFilterRequest f)
        {
            return new FilterBuilder()
                .AddRange("report_day", "dateFrom", "dateTo", f.DateFrom, f.DateTo)
                .AddString("target_bank_name = @targetBank", "targetBank", f.TargetBankName)
                .AddString("source_bank_name = @sourceBank", "sourceBank", f.SourceBankName)
                .AddString("card_product_type_category = @cardType", "cardType", f.CardProductType)
                .AddString("payment_system = @paymentSystem", "paymentSystem", f.PaymentSystem)
                .AddString("trans_group = @transGroup", "transGroup", f.TransGroup)
                .AddString("operation_type = @operationType", "operationType", f.OperationType)
                .AddString("token_status = @tokenStatus", "tokenStatus", f.TokenStatus)
                .AddString("is_contactless = @isContactless", "isContactless", f.IsContactless)
                .AddString("transaction_currency = @currency", "currency", f.TransactionCurrency);
        }
    }
}

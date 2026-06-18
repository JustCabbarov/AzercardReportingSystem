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

            var response = new SummaryTransactionResponse();

            // 1. Total AZC — bütün sətirlər
            response.Total = new KpiBlockDto
            {
                Amount = rows.Sum(r => r.TotalLocalAmount),
                Count = rows.Sum(r => r.TotalCount)
            };

            // 2. Issuing only (IS_ISSUING=1, IS_ACQUIRING=0)
            var issuingRows = rows.Where(r => r.IsIssuing == "1" && r.IsAcquiring == "0").ToList();
            response.Issuing = new KpiBlockDto
            {
                Amount = issuingRows.Sum(r => r.TotalLocalAmount),
                Count = issuingRows.Sum(r => r.TotalCount)
            };
            response.IssuingByDevice = issuingRows
                .Where(r => r.AcquiringDeviceType is not null)
                .Select(r => new DeviceBreakdownDto
                {
                    DeviceType = r.AcquiringDeviceType!,
                    Amount = r.TotalLocalAmount,
                    Count = r.TotalCount
                })
                .OrderByDescending(r => r.Amount)
                .ToList();

            // 3. Inner (IS_ISSUING=1, IS_ACQUIRING=1)
            var innerRows = rows.Where(r => r.IsIssuing == "1" && r.IsAcquiring == "1").ToList();
            response.Inner = new KpiBlockDto
            {
                Amount = innerRows.Sum(r => r.TotalLocalAmount),
                Count = innerRows.Sum(r => r.TotalCount)
            };
            response.InnerByDevice = innerRows
                .Where(r => r.AcquiringDeviceType is not null)
                .Select(r => new DeviceBreakdownDto
                {
                    DeviceType = r.AcquiringDeviceType!,
                    Amount = r.TotalLocalAmount,
                    Count = r.TotalCount
                })
                .OrderByDescending(r => r.Amount)
                .ToList();

            // 4. Acquiring only (IS_ISSUING=0, IS_ACQUIRING=1)
            var acquiringRows = rows.Where(r => r.IsIssuing == "0" && r.IsAcquiring == "1").ToList();
            response.Acquiring = new KpiBlockDto
            {
                Amount = acquiringRows.Sum(r => r.TotalLocalAmount),
                Count = acquiringRows.Sum(r => r.TotalCount)
            };
            response.AcquiringByDevice = acquiringRows
                .Where(r => r.AcquiringDeviceType is not null)
                .Select(r => new DeviceBreakdownDto
                {
                    DeviceType = r.AcquiringDeviceType!,
                    Amount = r.TotalLocalAmount,
                    Count = r.TotalCount
                })
                .OrderByDescending(r => r.Amount)
                .ToList();

            return response;
        }

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

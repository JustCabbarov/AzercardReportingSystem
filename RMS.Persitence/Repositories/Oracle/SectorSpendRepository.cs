using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.Oracle
{
    public class SectorSpendRepository : OracleRepositoryBase, ISectorSpendRepository
    {
        public SectorSpendRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH       AS ReportMonth,
            BANK_NAME          AS BankName,
            REGION_NAME_CLEAN  AS RegionNameClean,
            MCC_GROUP          AS MccGroup,
            MCC                AS Mcc,
            MCC_NAME           AS MccName,
            RETAIL_CATEGORY    AS RetailCategory,
            TRANSACTION_CLASS  AS TransactionClass,
            SOURCE_CHANNEL     AS SourceChannel,
            OPERATION_TYPE     AS OperationType,
            TOTAL_AMOUNT       AS TotalAmount,
            TOTAL_COUNT        AS TotalCount,
            TOTAL_LOCAL_AMOUNT AS TotalLocalAmount
        FROM MV_REQ8_SECTOR_SPEND
        """;

        public async Task<IEnumerable<SectorSpend>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<SectorSpend>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<SectorSpend>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY REPORT_MONTH DESC, MCC_GROUP";
            using var conn = CreateConnection();
            return await conn.QueryAsync<SectorSpend>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<SectorSpend>> GetByBankAndMonthAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE BANK_NAME = :BankName
               AND TRUNC(REPORT_MONTH,'MM') = TRUNC(:Month,'MM')
             ORDER BY TOTAL_AMOUNT DESC
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<SectorSpend>(
                new CommandDefinition(sql,
                    new { BankName = bankName, Month = month.Date },
                    cancellationToken: ct));
        }

        public async Task<IEnumerable<SectorSpend>> GetByMccGroupAsync(
            string mccGroup, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE MCC_GROUP = :MccGroup ORDER BY REPORT_MONTH DESC, BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<SectorSpend>(
                new CommandDefinition(sql, new { MccGroup = mccGroup }, cancellationToken: ct));
        }

        /// <summary>
        /// Bank daxilində hər MCC-nin ümumi xərcdəki payını hesablayır.
        /// ShareOfWalletPct = MCC amount / bank total amount * 100
        /// </summary>
        public async Task<IEnumerable<SectorSpend>> GetWithShareOfWalletAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var rows = (await GetByBankAndMonthAsync(bankName, month, ct)).ToList();
            var totalAmount = rows.Sum(r => r.TotalAmount);

            foreach (var row in rows)
                row.ShareOfWalletPct = totalAmount > 0
                    ? Math.Round(row.TotalAmount / totalAmount * 100, 2)
                    : 0;

            return rows;
        }
    }

}

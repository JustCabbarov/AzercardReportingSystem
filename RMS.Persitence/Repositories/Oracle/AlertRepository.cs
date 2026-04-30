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
    public class AlertRepository : OracleRepositoryBase, IAlertRepository
    {
        public AlertRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH      AS ReportMonth,
            BANK_NAME         AS BankName,
            MCC_GROUP         AS MccGroup,
            REGION_NAME_CLEAN AS RegionNameClean,
            CUR_AMOUNT        AS CurAmount,
            CUR_COUNT         AS CurCount,
            CUR_CARDS         AS CurCards,
            PREV_AMOUNT       AS PrevAmount,
            PREV_COUNT        AS PrevCount,
            PREV_CARDS        AS PrevCards,
            GENERATED_AT      AS GeneratedAt
        FROM ALI_JABBAROV.PG_MV_REQ2_ALERT
        """;

        public async Task<IEnumerable<AlertSignal>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY REPORT_MONTH DESC, BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<AlertSignal>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<AlertSignal>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY REPORT_MONTH DESC, MCC_GROUP";
            using var conn = CreateConnection();
            return await conn.QueryAsync<AlertSignal>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<AlertSignal>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE TRUNC(REPORT_MONTH,'MM') = TRUNC(:Month,'MM') ORDER BY BANK_NAME, MCC_GROUP";
            using var conn = CreateConnection();
            return await conn.QueryAsync<AlertSignal>(
                new CommandDefinition(sql, new { Month = month.Date }, cancellationToken: ct));
        }

        /// <summary>
        /// Yalnız |dəyişim| >= 10% olan sətirləri qaytarır.
        /// Filter Oracle tərəfdə tətbiq edilir — lazımsız sətir gəlmir.
        /// </summary>
        public async Task<IEnumerable<AlertSignal>> GetAlertsOnlyAsync(
            string? bankName = null, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE PREV_AMOUNT IS NOT NULL
               AND PREV_AMOUNT > 0
               AND ABS((CUR_AMOUNT - PREV_AMOUNT) / PREV_AMOUNT * 100) >= 10
            """;

            if (!string.IsNullOrWhiteSpace(bankName))
                sql += " AND BANK_NAME = :BankName";

            sql += " ORDER BY REPORT_MONTH DESC, BANK_NAME";

            using var conn = CreateConnection();
            return await conn.QueryAsync<AlertSignal>(
                new CommandDefinition(sql,
                    string.IsNullOrWhiteSpace(bankName) ? null : new { BankName = bankName },
                    cancellationToken: ct));
        }

        /// <summary>
        /// severity: "Critical" (>=50%), "High" (25-49%), "Medium" (10-24%)
        /// </summary>
        public async Task<IEnumerable<AlertSignal>> GetBySeverityAsync(
            string severity, CancellationToken ct = default)
        {
            var (minPct, maxPct) = severity.ToUpperInvariant() switch
            {
                "CRITICAL" => (50m, 9999m),
                "HIGH" => (25m, 49.99m),
                "MEDIUM" => (10m, 24.99m),
                _ => throw new ArgumentException($"Bilinməyən severity: {severity}")
            };

            var sql = SelectColumns + """
             WHERE PREV_AMOUNT IS NOT NULL
               AND PREV_AMOUNT > 0
               AND ABS((CUR_AMOUNT - PREV_AMOUNT) / PREV_AMOUNT * 100) >= :MinPct
               AND ABS((CUR_AMOUNT - PREV_AMOUNT) / PREV_AMOUNT * 100) <= :MaxPct
             ORDER BY REPORT_MONTH DESC
            """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<AlertSignal>(
                new CommandDefinition(sql,
                    new { MinPct = minPct, MaxPct = maxPct },
                    cancellationToken: ct));
        }
    }
}

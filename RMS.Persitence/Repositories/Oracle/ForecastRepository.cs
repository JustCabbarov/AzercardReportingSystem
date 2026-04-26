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
    public class ForecastRepository : OracleRepositoryBase, IForecastRepository
    {
        public ForecastRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH   AS ReportMonth,
            BANK_NAME      AS BankName,
            MCC_GROUP      AS MccGroup,
            TOTAL_AMOUNT   AS TotalAmount,
            TOTAL_COUNT    AS TotalCount,
            TOTAL_CARDS    AS TotalCards,
            AMOUNT_LAG1    AS AmountLag1,
            AMOUNT_LAG3    AS AmountLag3,
            AMOUNT_LAG12   AS AmountLag12,
            ROLLING_AVG_3M AS RollingAvg3M,
            ROLLING_AVG_6M AS RollingAvg6M,
            SEASON_MONTH   AS SeasonMonth,
            TIME_INDEX     AS TimeIndex
        FROM MV_REQ1_FORECAST_INPUT
        """;

        public async Task<IEnumerable<ForecastInput>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY BANK_NAME, MCC_GROUP, REPORT_MONTH";
            using var conn = CreateConnection();
            return await conn.QueryAsync<ForecastInput>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<ForecastInput>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY MCC_GROUP, REPORT_MONTH";
            using var conn = CreateConnection();
            return await conn.QueryAsync<ForecastInput>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<ForecastInput>> GetByBankAndMccAsync(
            string bankName, string mccGroup, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE BANK_NAME = :BankName
               AND MCC_GROUP = :MccGroup
             ORDER BY REPORT_MONTH
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<ForecastInput>(
                new CommandDefinition(sql,
                    new { BankName = bankName, MccGroup = mccGroup },
                    cancellationToken: ct));
        }

        public async Task<IEnumerable<ForecastInput>> GetByDateRangeAsync(
            DateTime from, DateTime to, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE REPORT_MONTH >= :From
               AND REPORT_MONTH <= :To
             ORDER BY BANK_NAME, MCC_GROUP, REPORT_MONTH
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<ForecastInput>(
                new CommandDefinition(sql,
                    new { From = from.Date, To = to.Date },
                    cancellationToken: ct));
        }
    }
}

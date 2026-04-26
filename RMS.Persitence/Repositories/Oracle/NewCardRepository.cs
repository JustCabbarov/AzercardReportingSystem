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
    public class NewCardRepository : OracleRepositoryBase, INewCardRepository
    {
        public NewCardRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            BANK_NAME                                      AS BankName,
            CARD_PRODUCT_NAME                              AS CardProductName,
            CARD_BRAND_NAME                                AS CardBrandName,
            PRODUCT_TYPE                                   AS ProductType,
            REGION_NAME_CLEAN                              AS RegionNameClean,
            FIRST_MONTH                                    AS FirstMonth,
            M1_CARDS                                       AS M1Cards,
            M1_TRANS                                       AS M1Trans,
            M1_AMOUNT                                      AS M1Amount,
            CASE WHEN M1_ACTIVE = 1 THEN 1 ELSE 0 END     AS M1Active,
            M2_TRANS                                       AS M2Trans,
            M2_AMOUNT                                      AS M2Amount,
            CASE WHEN M2_ACTIVE = 1 THEN 1 ELSE 0 END     AS M2Active,
            M3_TRANS                                       AS M3Trans,
            M3_AMOUNT                                      AS M3Amount,
            CASE WHEN M3_ACTIVE = 1 THEN 1 ELSE 0 END     AS M3Active
        FROM MV_REQ9_NEW_CARD
        """;

        public async Task<IEnumerable<NewCardActivation>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY FIRST_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<NewCardActivation>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<NewCardActivation>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY FIRST_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<NewCardActivation>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<NewCardActivation>> GetByFirstMonthAsync(
            DateTime firstMonth, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE TRUNC(FIRST_MONTH,'MM') = TRUNC(:FirstMonth,'MM') ORDER BY BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<NewCardActivation>(
                new CommandDefinition(sql, new { FirstMonth = firstMonth.Date }, cancellationToken: ct));
        }

        /// <summary>
        /// segment: "EarlyActive" | "DelayedActive" | "SlowActive" | "Inactive"
        /// Oracle-da M1/M2/M3 trans hədlərinə görə filter edilir.
        /// </summary>
        public async Task<IEnumerable<NewCardActivation>> GetBySegmentAsync(
            string bankName, string segment, CancellationToken ct = default)
        {
            var filter = segment switch
            {
                "EarlyActive" => "M1_TRANS > 5",
                "DelayedActive" => "M1_TRANS <= 5 AND M2_TRANS > 0",
                "SlowActive" => "M1_TRANS = 0 AND M2_TRANS = 0 AND M3_TRANS > 0",
                "Inactive" => "M1_TRANS = 0 AND M2_TRANS = 0 AND M3_TRANS = 0",
                _ => throw new ArgumentException($"Bilinməyən segment: {segment}")
            };

            var sql = SelectColumns + $" WHERE BANK_NAME = :BankName AND {filter} ORDER BY FIRST_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<NewCardActivation>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<NewCardActivation>> GetInactiveCardsAsync(
            string bankName, CancellationToken ct = default)
            => await GetBySegmentAsync(bankName, "Inactive", ct);
    }
}

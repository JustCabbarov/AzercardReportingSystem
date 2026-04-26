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
    public class CardActivityRepository : OracleRepositoryBase, ICardActivityRepository
    {
        public CardActivityRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH         AS ReportMonth,
            BANK_NAME            AS BankName,
            REGION_NAME          AS RegionName,
            REGION_NAME_CLEAN    AS RegionNameClean,
            CARD_BRAND_NAME      AS CardBrandName,
            CARD_PRODUCT_NAME    AS CardProductName,
            PRODUCT_TYPE         AS ProductType,
            PAYMENT_SCHEME       AS PaymentScheme,
            CONTACTLESS_STATUS   AS ContactlessStatus,
            EXP_STATUS           AS ExpStatus,
            STATUS_3D            AS Status3D,
            CARD_TYPE            AS CardType,
            TOTAL_CARDS          AS TotalCards,
            TOTAL_TRANS_COUNT    AS TotalTransCount,
            TOTAL_TRANS_AMOUNT   AS TotalTransAmount,
            TOTAL_LOCAL_AMOUNT   AS TotalLocalAmount,
            CONTACTLESS_COUNT    AS ContactlessCount,
            CHIP_COUNT           AS ChipCount,
            TOKEN_COUNT          AS TokenCount,
            ACTIVE_CARD_RATE_PCT AS ActiveCardRatePct
        FROM MV_REQ6_CARD_ACTIVITY
        """;

        public async Task<IEnumerable<CardActivity>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<CardActivity>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<CardActivity>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY REPORT_MONTH DESC, CARD_PRODUCT_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<CardActivity>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<CardActivity>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE TRUNC(REPORT_MONTH,'MM') = TRUNC(:Month,'MM') ORDER BY BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<CardActivity>(
                new CommandDefinition(sql, new { Month = month.Date }, cancellationToken: ct));
        }

        public async Task<IEnumerable<CardActivity>> GetByProductTypeAsync(
            string productType, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE PRODUCT_TYPE = :ProductType ORDER BY REPORT_MONTH DESC, BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<CardActivity>(
                new CommandDefinition(sql, new { ProductType = productType }, cancellationToken: ct));
        }

        /// <summary>
        /// ActivitySegment C# entitydə hesablanır.
        /// Oracle-da ACTIVE_CARD_RATE_PCT aralığı ilə filter edilir.
        /// </summary>
        public async Task<IEnumerable<CardActivity>> GetByActivitySegmentAsync(
            string bankName, string segment, CancellationToken ct = default)
        {
            var (min, max) = segment switch
            {
                "HighlyActive" => (80m, 100m),
                "ModeratelyActive" => (50m, 79.99m),
                "LowActive" => (20m, 49.99m),
                "Passive" => (0m, 19.99m),
                _ => throw new ArgumentException($"Bilinməyən segment: {segment}")
            };

            var sql = SelectColumns + """
             WHERE BANK_NAME = :BankName
               AND ACTIVE_CARD_RATE_PCT >= :Min
               AND ACTIVE_CARD_RATE_PCT <= :Max
             ORDER BY REPORT_MONTH DESC
            """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<CardActivity>(
                new CommandDefinition(sql,
                    new { BankName = bankName, Min = min, Max = max },
                    cancellationToken: ct));
        }
    }
}

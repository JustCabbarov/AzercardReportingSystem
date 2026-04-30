using Dapper;
using Microsoft.Extensions.Configuration;

using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;


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
            TOTAL_CARDS          AS TotalCards,
            TOTAL_TRANS_COUNT    AS TotalTransCount,
            TOTAL_TRANS_AMOUNT   AS TotalTransAmount,
            TOTAL_LOCAL_AMOUNT   AS TotalLocalAmount,
            CONTACTLESS_COUNT    AS ContactlessCount,
            CHIP_COUNT           AS ChipCount,
            TOKEN_COUNT          AS TokenCount,
            ACTIVE_CARD_RATE_PCT AS ActiveCardRatePct
        FROM ALI_JABBAROV.PG_MV_REQ6_CARD_ACTIVITY
        """;

        private Task<PagedResult<CardActivity>> QueryAsync(
            string? bankName,
            DateTime? month,
            string? productType,
            string? cardBrandName,
            decimal? minRate,
            decimal? maxRate,
            PageRequest pageReq,
            CancellationToken ct)
        {
            var filter = new FilterBuilder()
                .Add("BANK_NAME = :BankName", "BankName", bankName)
                .Add("PRODUCT_TYPE = :ProductType", "ProductType", productType)
                .Add("CARD_BRAND_NAME = :CardBrand", "CardBrand", cardBrandName)
                .Add("ACTIVE_CARD_RATE_PCT >= :Min", "Min", minRate)
                .Add("ACTIVE_CARD_RATE_PCT <= :Max", "Max", maxRate)
                .AddMonth("REPORT_MONTH", "Month", month);

            return QueryPagedAsync<CardActivity>(
                SelectColumns + $" {filter.WhereClause}",
                "ReportMonth DESC",
                pageReq,
                filter.Parameters,
                ct);
        }

        public Task<PagedResult<CardActivity>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(bankName, null, null, null, null, null, pageReq, ct);

        public Task<PagedResult<CardActivity>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(null, month, null, null, null, null, pageReq, ct);

        public Task<PagedResult<CardActivity>> GetByProductTypeAsync(
            string productType, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(null, null, productType, null, null, null, pageReq, ct);

        public Task<PagedResult<CardActivity>> FilterAsync(
            CardActivity f, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(
                f.BankName,
                f.ReportMonth == default ? null : f.ReportMonth,
                f.ProductType,
                f.CardBrandName,
                null, null,
                pageReq, ct);

        /// <summary>
        /// ActivitySegment C# entitydə hesablanır.
        /// Oracle-da ACTIVE_CARD_RATE_PCT aralığı ilə filter edilir.
        /// </summary>
        public Task<PagedResult<CardActivity>> GetByActivitySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default)
        {
            var (min, max) = segment switch
            {
                "HighlyActive" => (80m, 100m),
                "ModeratelyActive" => (50m, 79.99m),
                "LowActive" => (20m, 49.99m),
                "Passive" => (0m, 19.99m),
                _ => throw new ArgumentException($"Bilinməyən segment: {segment}")
            };

            return QueryAsync(bankName, null, null, null, min, max, pageReq, ct);
        }
    }
}
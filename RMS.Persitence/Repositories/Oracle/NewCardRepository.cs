using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using RMS.Persitence.Repositories.Oracle;

namespace RMS.Persistence.Repositories.Oracle
{
    public class NewCardRepository : OracleRepositoryBase, INewCardRepository
    {
        public NewCardRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            BANK_NAME                                                   AS BankName,
            CARD_PRODUCT_NAME                                           AS CardProductName,
            CARD_BRAND_NAME                                             AS CardBrandName,
            PRODUCT_TYPE                                                AS ProductType,
            REGION_NAME_CLEAN                                           AS RegionNameClean,
            FIRST_MONTH                                                 AS FirstMonth,
            M1_CARDS                                                    AS M1Cards,
            M1_TRANS                                                    AS M1Trans,
            M1_AMOUNT                                                   AS M1Amount,
            CASE WHEN TO_CHAR(M1_ACTIVE) = '1' THEN 1 ELSE 0 END        AS M1Active,
            M2_TRANS                                                    AS M2Trans,
            M2_AMOUNT                                                   AS M2Amount,
            CASE WHEN TO_CHAR(M2_ACTIVE) = '1' THEN 1 ELSE 0 END        AS M2Active,
            M3_TRANS                                                    AS M3Trans,
            M3_AMOUNT                                                   AS M3Amount,
            CASE WHEN TO_CHAR(M3_ACTIVE) = '1' THEN 1 ELSE 0 END        AS M3Active
        FROM PG_MV_REQ9_NEW_CARD
        """;

        public Task<PagedResult<NewCardActivation>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default)
        {
            return QueryPagedAsync<NewCardActivation>(
                SelectColumns, "FirstMonth DESC", pageReq, null, ct);
        }

        public Task<PagedResult<NewCardActivation>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .Add("BANK_NAME = :BankName", "BankName", bankName);

            return QueryPagedAsync<NewCardActivation>(
                SelectColumns + $" {filter.WhereClause}",
                "FirstMonth DESC", pageReq, filter.Parameters, ct);
        }

        public Task<PagedResult<NewCardActivation>> GetByFirstMonthAsync(
            DateTime firstMonth, PageRequest pageReq, CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .AddMonth("FIRST_MONTH", "FirstMonth", firstMonth);

            return QueryPagedAsync<NewCardActivation>(
                SelectColumns + $" {filter.WhereClause}",
                "FirstMonth DESC", pageReq, filter.Parameters, ct);
        }

        public Task<PagedResult<NewCardActivation>> GetBySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default)
        {
            // Segment DB-də yoxdur, C# tərəfində hesablanır —
            // buna görə bütün bank data-sını çəkib filtirliyirik
            var filter = new FilterBuilder()
                .Add("BANK_NAME = :BankName", "BankName", bankName);

            // Segment şərtini SQL-ə çeviririk
            var segmentSql = segment switch
            {
                "EarlyActive" => " AND M1_TRANS > 5",
                "DelayedActive" => " AND M1_TRANS <= 5 AND M2_TRANS > 0",
                "SlowActive" => " AND M1_TRANS = 0 AND M2_TRANS = 0 AND M3_TRANS > 0",
                "Inactive" => " AND M1_TRANS = 0 AND M2_TRANS = 0 AND M3_TRANS = 0",
                _ => ""
            };

            return QueryPagedAsync<NewCardActivation>(
                SelectColumns + $" {filter.WhereClause}{segmentSql}",
                "FirstMonth DESC", pageReq, filter.Parameters, ct);
        }

        public Task<PagedResult<NewCardActivation>> GetInactiveCardsAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .Add("BANK_NAME = :BankName", "BankName", bankName);

            return QueryPagedAsync<NewCardActivation>(
                SelectColumns + $" {filter.WhereClause} AND M1_TRANS = 0 AND M2_TRANS = 0 AND M3_TRANS = 0",
                "FirstMonth DESC", pageReq, filter.Parameters, ct);
        }

        /// <summary>
        /// Benchmark-dakı FilterAsync ilə eyni pattern —
        /// null olan field-lar WHERE-ə əlavə edilmir.
        /// </summary>
        public Task<PagedResult<NewCardActivation>> FilterAsync(
            NewCardActivation f, PageRequest pageReq, CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .AddString("BANK_NAME = :BankName", "BankName", f.BankName)
                .AddString("CARD_PRODUCT_NAME = :CardProduct", "CardProduct", f.CardProductName)
                .AddString("CARD_BRAND_NAME = :CardBrand", "CardBrand", f.CardBrandName)
                .AddString("PRODUCT_TYPE = :ProductType", "ProductType", f.ProductType)
                .AddString("REGION_NAME_CLEAN = :Region", "Region", f.RegionNameClean)
                .AddMonth("FIRST_MONTH", "FirstMonth",
                    f.FirstMonth == default ? null : f.FirstMonth);

            return QueryPagedAsync<NewCardActivation>(
                SelectColumns + $" {filter.WhereClause}",
                "FirstMonth DESC", pageReq, filter.Parameters, ct);
        }

        public async Task<IEnumerable<NewCardActivation>> GetTrendAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = """
    SELECT
        FIRST_MONTH                                                 AS FirstMonth,
        M1_CARDS                                                    AS M1Cards,
        M1_TRANS                                                    AS M1Trans,
        M1_AMOUNT                                                   AS M1Amount,
        CASE WHEN TO_CHAR(M1_ACTIVE) = '1' THEN 1 ELSE 0 END        AS M1Active,
        M2_TRANS                                                    AS M2Trans,
        M2_AMOUNT                                                   AS M2Amount,
        CASE WHEN TO_CHAR(M2_ACTIVE) = '1' THEN 1 ELSE 0 END        AS M2Active,
        M3_TRANS                                                    AS M3Trans,
        M3_AMOUNT                                                   AS M3Amount,
        CASE WHEN TO_CHAR(M3_ACTIVE) = '1' THEN 1 ELSE 0 END        AS M3Active
    FROM PG_MV_REQ9_NEW_CARD
    WHERE BANK_NAME = :BankName
    ORDER BY FIRST_MONTH
    """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<NewCardActivation>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }
    }
}
using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class MarketBenchmarkRepository : OracleRepositoryBase, IMarketBenchmarkRepository
    {
        public MarketBenchmarkRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH        AS ReportMonth,
            BANK_NAME           AS BankName,
            REGION_NAME_CLEAN   AS RegionNameClean,
            CARD_BRAND_NAME     AS CardBrandName,
            PRODUCT_TYPE        AS ProductType,
            BANK_TRANS_AMOUNT   AS BankTransAmount,
            BANK_TRANS_COUNT    AS BankTransCount,
            BANK_CARD_COUNT     AS BankCardCount,
            MARKET_TOTAL_AMOUNT AS MarketTotalAmount,
            MARKET_TOTAL_CARDS  AS MarketTotalCards
        FROM public.pg_mv_req7_benchmark
        """;

        private Task<PagedResult<MarketBenchmark>> QueryAsync(
            string? bankName,
            DateTime? month,
            string? regionNameClean,
            PageRequest pageReq,
            CancellationToken ct)
        {
            var filter = new FilterBuilder()
                .Add("BANK_NAME = @BankName", "BankName", bankName)
                .Add("REGION_NAME_CLEAN = @Region", "Region", regionNameClean)
                .AddMonth("REPORT_MONTH", "Month", month);

            return QueryPagedAsync<MarketBenchmark>(
                SelectColumns + $" {filter.WhereClause}",
                "ReportMonth DESC",
                pageReq,
                filter.Parameters,
                ct);
        }

        public Task<PagedResult<MarketBenchmark>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(null, null, null, pageReq, ct);

        public Task<PagedResult<MarketBenchmark>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(bankName, null, null, pageReq, ct);

        public Task<PagedResult<MarketBenchmark>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(null, month, null, pageReq, ct);

        public Task<PagedResult<MarketBenchmark>> GetByBankAndMonthAsync(
            string bankName, DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => QueryAsync(bankName, month, null, pageReq, ct);

        public async Task<PagedResult<MarketBenchmark>> GetRankedByMonthAsync(
            DateTime month,
            PageRequest pageReq,
            string? regionNameClean = null,
            CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .Add("REGION_NAME_CLEAN = @Region", "Region", regionNameClean)
                .AddMonth("REPORT_MONTH", "Month", month);

            var allSql = SelectColumns + $" {filter.WhereClause}";

            using var conn = CreateConnection();
            var rows = (await conn.QueryAsync<MarketBenchmark>(
                new CommandDefinition(allSql, filter.Parameters, cancellationToken: ct)))
                .GroupBy(r => r.BankName)
                .Select(g => new
                {
                    BankTransAmount = g.Sum(x => x.BankTransAmount),
                    Rows = g.ToList()
                })
                .OrderByDescending(g => g.BankTransAmount)
                .SelectMany((g, i) => g.Rows.Select(r =>
                {
                    r.BankRank = i + 1;
                    return r;
                }))
                .ToList();

            return new PagedResult<MarketBenchmark>
            {
                Items = rows.Skip((pageReq.Page - 1) * pageReq.PageSize)
                                 .Take(pageReq.PageSize),
                TotalCount = rows.Count,
                Page = pageReq.Page,
                PageSize = pageReq.PageSize
            };
        }

        public Task<PagedResult<MarketBenchmark>> FilterAsync(
    MarketBenchmark f,
    PageRequest pageReq,
    CancellationToken ct = default)
        {
            var filter = new FilterBuilder()
                .AddString("BANK_NAME = @BankName", "BankName", f.BankName)           // ← Add → AddString
                .AddString("REGION_NAME_CLEAN = @Region", "Region", f.RegionNameClean)
                .AddString("CARD_BRAND_NAME = @CardBrand", "CardBrand", f.CardBrandName)
                .AddString("PRODUCT_TYPE = @ProductType", "ProductType", f.ProductType)
                .AddMonth("REPORT_MONTH", "Month",
                    f.ReportMonth == default ? null : (DateTime?)f.ReportMonth);       // ← null cast

            return QueryPagedAsync<MarketBenchmark>(
                SelectColumns + $" {filter.WhereClause}",
                "ReportMonth DESC",
                pageReq,
                filter.Parameters,
                ct);
        }
        public async Task<IEnumerable<MarketBenchmarkTrend>> GetTrendAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = """
    SELECT
        REPORT_MONTH        AS ReportMonth,
        BANK_TRANS_AMOUNT   AS BankTransAmount,
        MARKET_TOTAL_AMOUNT AS MarketTotalAmount,
        BANK_CARD_COUNT     AS BankCardCount,
        MARKET_TOTAL_CARDS  AS MarketTotalCards
    FROM pg_mv_req7_benchmark
    WHERE BANK_NAME = @BankName
    ORDER BY REPORT_MONTH
    """;

            using var conn = CreateConnection();
            var rows = await conn.QueryAsync<MarketBenchmark>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));

            return rows
                .GroupBy(r => r.ReportMonth)
                .Select(g => new MarketBenchmarkTrend
                {
                    Month = g.Key,
                    MarketSharePct = g.First().MarketTotalAmount > 0
                        ? Math.Round(g.First().BankTransAmount / g.First().MarketTotalAmount * 100, 2)
                        : 0,
                    CardSharePct = g.First().MarketTotalCards > 0
                        ? Math.Round((decimal)g.Sum(x => x.BankCardCount) / g.First().MarketTotalCards * 100, 2)
                        : 0
                })
                .OrderBy(x => x.Month)
                .ToList();
        }
    }
}
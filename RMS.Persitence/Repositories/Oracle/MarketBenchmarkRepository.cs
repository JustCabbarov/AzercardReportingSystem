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
        FROM MV_REQ7_BENCHMARK
        """;

        public async Task<IEnumerable<MarketBenchmark>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<MarketBenchmark>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<MarketBenchmark>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<MarketBenchmark>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<MarketBenchmark>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE TRUNC(REPORT_MONTH,'MM') = TRUNC(:Month,'MM') ORDER BY BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<MarketBenchmark>(
                new CommandDefinition(sql, new { Month = month.Date }, cancellationToken: ct));
        }

        /// <summary>
        /// Həmin ay üçün bankları BANK_TRANS_AMOUNT-a görə sıralayır.
        /// BankRank C# tərəfdə Select ilə təyin edilir.
        /// </summary>
        public async Task<IEnumerable<MarketBenchmark>> GetRankedByMonthAsync(
            DateTime month,
            string? regionNameClean = null,
            CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE TRUNC(REPORT_MONTH,'MM') = TRUNC(:Month,'MM')";

            if (!string.IsNullOrWhiteSpace(regionNameClean))
                sql += " AND REGION_NAME_CLEAN = :Region";

            using var conn = CreateConnection();
            var rows = await conn.QueryAsync<MarketBenchmark>(
                new CommandDefinition(sql,
                    new { Month = month.Date, Region = regionNameClean },
                    cancellationToken: ct));

            return rows
                .OrderByDescending(r => r.BankTransAmount)
                .Select((r, i) => { r.BankRank = i + 1; return r; });
        }
    }

}

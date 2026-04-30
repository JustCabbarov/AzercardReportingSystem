
using RMS.Domain.Entities.Oracle;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IMarketBenchmarkRepository
    {
        Task<PagedResult<MarketBenchmark>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<MarketBenchmark>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<MarketBenchmark>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<MarketBenchmark>> GetByBankAndMonthAsync(
            string bankName, DateTime month, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<MarketBenchmark>> GetRankedByMonthAsync(
            DateTime month, PageRequest pageReq, string? regionNameClean = null, CancellationToken ct = default);

        Task<PagedResult<MarketBenchmark>> FilterAsync(
            MarketBenchmark f, PageRequest pageReq, CancellationToken ct = default);
        Task<IEnumerable<MarketBenchmarkTrend>> GetTrendAsync(
    string bankName, CancellationToken ct = default);
    }
}
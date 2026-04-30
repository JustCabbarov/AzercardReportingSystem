using RMS.Contract.Services.Oracle;

using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class MarketBenchmarkService : IMarketBenchmarkService
    {
        private readonly IMarketBenchmarkRepository _repo;

        public MarketBenchmarkService(IMarketBenchmarkRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<MarketBenchmark>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetAllAsync(pageReq, ct);

        public Task<PagedResult<MarketBenchmark>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, pageReq, ct);
        public Task<IEnumerable<MarketBenchmarkTrend>> GetTrendAsync(
    string bankName, CancellationToken ct = default)
    => _repo.GetTrendAsync(bankName, ct);
        public Task<PagedResult<MarketBenchmark>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, pageReq, ct);

        public Task<PagedResult<MarketBenchmark>> GetRankedAsync(
            DateTime month, PageRequest pageReq, string? regionNameClean = null, CancellationToken ct = default)
            => _repo.GetRankedByMonthAsync(month, pageReq, regionNameClean, ct);

        public Task<PagedResult<MarketBenchmark>> FilterAsync(
            MarketBenchmark f, PageRequest pageReq, CancellationToken ct = default)
            => _repo.FilterAsync(f, pageReq, ct);

       
    }
}
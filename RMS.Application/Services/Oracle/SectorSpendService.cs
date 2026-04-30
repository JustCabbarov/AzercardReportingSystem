using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class SectorSpendService : ISectorSpendService
    {
        private readonly ISectorSpendRepository _repo;

        public SectorSpendService(ISectorSpendRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<SectorSpend>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetAllAsync(pageReq, ct);

        public Task<PagedResult<SectorSpend>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, pageReq, ct);

        public Task<PagedResult<SectorSpend>> GetByBankAndMonthAsync(
            string bankName, DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByBankAndMonthAsync(bankName, month, pageReq, ct);

        public Task<PagedResult<SectorSpend>> GetByMccGroupAsync(
            string mccGroup, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByMccGroupAsync(mccGroup, pageReq, ct);

        public Task<PagedResult<SectorSpend>> FilterAsync(
            SectorSpend f, PageRequest pageReq, CancellationToken ct = default)
            => _repo.FilterAsync(f, pageReq, ct);

        public Task<IEnumerable<SectorSpendTrend>> GetTrendAsync(
            string bankName, string mccGroup, CancellationToken ct = default)
            => _repo.GetTrendAsync(bankName, mccGroup, ct);

        /// <summary>
        /// Bank + ay üzrə ən yüksək xərcli top N MCC qrupunu qaytarır.
        /// </summary>
        public async Task<IEnumerable<SectorSpend>> GetTopMccGroupsAsync(
            string bankName, DateTime month, int topN = 10, CancellationToken ct = default)
        {
            var result = await _repo.GetByBankAndMonthAsync(
                bankName, month,
                new PageRequest { Page = 1, PageSize = 1000 },
                ct);

            return result.Items
                .OrderByDescending(x => x.TotalAmount)
                .Take(topN);
        }

        /// <summary>
        /// Onlayn vs oflayn kanal paylanması — hər kanalın ümumi xərcdəki faizi.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetChannelDistributionAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var result = await _repo.GetByBankAndMonthAsync(
                bankName, month,
                new PageRequest { Page = 1, PageSize = 1000 },
                ct);

            var data = result.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceChannel))
                .ToList();

            var totalAmount = data.Sum(x => x.TotalAmount);
            if (totalAmount == 0) return [];

            return data
                .GroupBy(x => x.SourceChannel!)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(x => x.TotalAmount) / totalAmount * 100, 2));
        }
    }
}
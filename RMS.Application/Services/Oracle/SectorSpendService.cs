using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.Oracle
{
    public class SectorSpendService : ISectorSpendService
    {
        private readonly ISectorSpendRepository _repo;

        public SectorSpendService(ISectorSpendRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<SectorSpend>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<SectorSpend>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<SectorSpend>> GetByBankAndMonthAsync(
            string bankName, DateTime month, CancellationToken ct = default)
            => _repo.GetByBankAndMonthAsync(bankName, month, ct);

        public Task<IEnumerable<SectorSpend>> GetByMccGroupAsync(
            string mccGroup, CancellationToken ct = default)
            => _repo.GetByMccGroupAsync(mccGroup, ct);

        public Task<IEnumerable<SectorSpend>> GetWithShareOfWalletAsync(
            string bankName, DateTime month, CancellationToken ct = default)
            => _repo.GetWithShareOfWalletAsync(bankName, month, ct);

        /// <summary>
        /// Bank + ay üzrə ən yüksək xərcli top N MCC qrupunu qaytarır.
        /// </summary>
        public async Task<IEnumerable<SectorSpend>> GetTopMccGroupsAsync(
            string bankName, DateTime month, int topN = 10, CancellationToken ct = default)
        {
            var data = await _repo.GetWithShareOfWalletAsync(bankName, month, ct);
            return data
                .OrderByDescending(x => x.TotalAmount)
                .Take(topN);
        }

        /// <summary>
        /// Onlayn (SOURCE_CHANNEL) vs oflayn kanal paylanması.
        /// Hər kanalın ümumi xərcdəki faizi hesablanır.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetChannelDistributionAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAndMonthAsync(bankName, month, ct))
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceChannel))
                .ToList();

            var totalAmount = data.Sum(x => x.TotalAmount);
            if (totalAmount == 0) return new Dictionary<string, decimal>();

            return data
                .GroupBy(x => x.SourceChannel!)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(x => x.TotalAmount) / totalAmount * 100, 2));
        }
    }
}

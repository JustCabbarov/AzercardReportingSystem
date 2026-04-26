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
    public class MarketBenchmarkService : IMarketBenchmarkService
    {
        private readonly IMarketBenchmarkRepository _repo;

        public MarketBenchmarkService(IMarketBenchmarkRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<MarketBenchmark>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<MarketBenchmark>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<MarketBenchmark>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, ct);

        public Task<IEnumerable<MarketBenchmark>> GetRankedAsync(
            DateTime month, string? regionNameClean = null, CancellationToken ct = default)
            => _repo.GetRankedByMonthAsync(month, regionNameClean, ct);

        /// <summary>
        /// Müəyyən bankın bazar payı — MarketSharePct entity property-sindən oxunur.
        /// Əgər bir neçə region varsa, weighted average hesablanır.
        /// </summary>
        public async Task<decimal> GetMarketShareAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct))
                .Where(x => x.ReportMonth.Year == month.Year
                         && x.ReportMonth.Month == month.Month)
                .ToList();

            if (!data.Any()) return 0;

            var bankTotal = data.Sum(x => x.BankTransAmount);
            var marketTotal = data.Sum(x => x.MarketTotalAmount);

            return marketTotal > 0
                ? Math.Round(bankTotal / marketTotal * 100, 2)
                : 0;
        }

        /// <summary>
        /// Müəyyən bankın kart payı — kart sayına görə.
        /// </summary>
        public async Task<decimal> GetCardShareAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct))
                .Where(x => x.ReportMonth.Year == month.Year
                         && x.ReportMonth.Month == month.Month)
                .ToList();

            if (!data.Any()) return 0;

            var bankCards = data.Sum(x => x.BankCardCount);
            var marketCards = data.Sum(x => x.MarketTotalCards);

            return marketCards > 0
                ? Math.Round((decimal)bankCards / marketCards * 100, 2)
                : 0;
        }

        /// <summary>
        /// Bankın həmin aydakı sırasını qaytarır (1 = ən böyük).
        /// </summary>
        public async Task<int> GetBankRankAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var ranked = await _repo.GetRankedByMonthAsync(month, null, ct);
            var bank = ranked.FirstOrDefault(x => x.BankName == bankName);
            return bank?.BankRank ?? -1;
        }
    }
}

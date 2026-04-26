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
    public class CardActivityService : ICardActivityService
    {
        private readonly ICardActivityRepository _repo;

        public CardActivityService(ICardActivityRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<CardActivity>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<CardActivity>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<CardActivity>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, ct);

        public Task<IEnumerable<CardActivity>> GetByProductTypeAsync(
            string productType, CancellationToken ct = default)
            => _repo.GetByProductTypeAsync(productType, ct);

        public Task<IEnumerable<CardActivity>> GetByActivitySegmentAsync(
            string bankName, string segment, CancellationToken ct = default)
            => _repo.GetByActivitySegmentAsync(bankName, segment, ct);

        /// <summary>
        /// Bank + ay üzrə ortalama contactless istifadə faizi.
        /// ContactlessRatePct entity-də hesablanır, burada weighted avg götürülür.
        /// </summary>
        public async Task<decimal> GetAvgContactlessRateAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct))
                .Where(x => x.ReportMonth.Year == month.Year
                         && x.ReportMonth.Month == month.Month
                         && x.TotalTransCount > 0)
                .ToList();

            if (!data.Any()) return 0;

            var totalTrans = data.Sum(x => x.TotalTransCount);
            var totalContactless = data.Sum(x => x.ContactlessCount);

            return totalTrans > 0
                ? Math.Round((decimal)totalContactless / totalTrans * 100, 2)
                : 0;
        }

        /// <summary>
        /// Seqment payı — hər seqmentin ümumi kartlara nisbəti (%).
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct))
                .Where(x => x.ReportMonth.Year == month.Year
                         && x.ReportMonth.Month == month.Month)
                .ToList();

            var totalCards = data.Sum(x => x.TotalCards);
            if (totalCards == 0) return new Dictionary<string, decimal>();

            return data
                .GroupBy(x => x.ActivitySegment)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round((decimal)g.Sum(x => x.TotalCards) / totalCards * 100, 2));
        }
    }

}

using RMS.Contract.Services.Oracle;

using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class CardActivityService : ICardActivityService
    {
        private readonly ICardActivityRepository _repo;

        public CardActivityService(ICardActivityRepository repo)
        {
            _repo = repo;
        }


        public Task<PagedResult<CardActivity>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, pageReq, ct);

        public Task<PagedResult<CardActivity>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, pageReq, ct);

        public Task<PagedResult<CardActivity>> GetByProductTypeAsync(
            string productType, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByProductTypeAsync(productType, pageReq, ct);

        public Task<PagedResult<CardActivity>> GetByActivitySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByActivitySegmentAsync(bankName, segment, pageReq, ct);

        public Task<PagedResult<CardActivity>> FilterAsync(
            CardActivity f, PageRequest pageReq, CancellationToken ct = default)
            => _repo.FilterAsync(f, pageReq, ct);

        /// <summary>
        /// Bank + ay üzrə ortalama contactless istifadə faizi.
        /// </summary>
        public async Task<decimal> GetAvgContactlessRateAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var pageReq = new PageRequest { Page = 1, PageSize = int.MaxValue };
            var data = (await _repo.GetByBankAsync(bankName, pageReq, ct)).Items
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
            var pageReq = new PageRequest { Page = 1, PageSize = int.MaxValue };
            var data = (await _repo.GetByBankAsync(bankName, pageReq, ct)).Items
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
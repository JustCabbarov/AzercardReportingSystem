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
    public class NewCardService : INewCardService
    {
        private readonly INewCardRepository _repo;

        public NewCardService(INewCardRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<NewCardActivation>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<NewCardActivation>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<NewCardActivation>> GetByFirstMonthAsync(
            DateTime firstMonth, CancellationToken ct = default)
            => _repo.GetByFirstMonthAsync(firstMonth, ct);

        public Task<IEnumerable<NewCardActivation>> GetBySegmentAsync(
            string bankName, string segment, CancellationToken ct = default)
            => _repo.GetBySegmentAsync(bankName, segment, ct);

        public Task<IEnumerable<NewCardActivation>> GetInactiveCardsAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetInactiveCardsAsync(bankName, ct);

        /// <summary>
        /// Bank üzrə ortalama 3 aylıq aktivasyon faizi.
        /// Avg3MActiveRate entity property-sindən oxunur.
        /// </summary>
        public async Task<decimal> GetAvgActivationRateAsync(
            string bankName, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct)).ToList();
            if (!data.Any()) return 0;

            return Math.Round(data.Average(x => x.Avg3MActiveRate), 2);
        }

        /// <summary>
        /// Seqment paylanması — hər seqmentin kart sayına görə faizi.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(
            string bankName, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct)).ToList();
            var total = data.Sum(x => x.M1Cards);
            if (total == 0) return new Dictionary<string, decimal>();

            return data
                .GroupBy(x => x.ActivationSegment)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round((decimal)g.Sum(x => x.M1Cards) / total * 100, 2));
        }

        /// <summary>
        /// İlk istifadəyə qədər ortalama ay sayı.
        /// Yalnız aktivləşmiş kartlar nəzərə alınır (MonthsToFirstUse != null).
        /// </summary>
        public async Task<double> GetAvgMonthsToFirstUseAsync(
            string bankName, CancellationToken ct = default)
        {
            var data = (await _repo.GetByBankAsync(bankName, ct))
                .Where(x => x.MonthsToFirstUse.HasValue)
                .ToList();

            if (!data.Any()) return 0;

            return Math.Round(data.Average(x => (double)x.MonthsToFirstUse!.Value), 2);
        }
    }

}

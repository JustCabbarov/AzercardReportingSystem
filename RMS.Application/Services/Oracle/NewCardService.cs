using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class NewCardService : INewCardService
    {
        private readonly INewCardRepository _repo;

        public NewCardService(INewCardRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<NewCardActivation>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetAllAsync(pageReq, ct);

        public Task<PagedResult<NewCardActivation>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, pageReq, ct);

        public Task<PagedResult<NewCardActivation>> GetByFirstMonthAsync(
            DateTime firstMonth, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetByFirstMonthAsync(firstMonth, pageReq, ct);

        public Task<PagedResult<NewCardActivation>> GetBySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetBySegmentAsync(bankName, segment, pageReq, ct);

        public Task<PagedResult<NewCardActivation>> GetInactiveCardsAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default)
            => _repo.GetInactiveCardsAsync(bankName, pageReq, ct);

        public Task<PagedResult<NewCardActivation>> FilterAsync(
            NewCardActivation f, PageRequest pageReq, CancellationToken ct = default)
            => _repo.FilterAsync(f, pageReq, ct);

        public Task<IEnumerable<NewCardActivation>> GetTrendAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetTrendAsync(bankName, ct);

        /// <summary>
        /// Bank üzrə bütün kartların ortalama 3 aylıq aktivasiya faizini qaytarır.
        /// Avg3MActiveRate entity-də bool əsaslı hesablanır, biz sadəcə ortalama alırıq.
        /// </summary>
        public async Task<decimal> GetAvgActivationRateAsync(
            string bankName, CancellationToken ct = default)
        {
            var data = (await _repo.GetTrendAsync(bankName, ct)).ToList();

            if (!data.Any()) return 0;

            // M1Cards > 0 olan sətirlər üzərində Avg3MActiveRate ortalaması
            var valid = data.Where(x => x.M1Cards > 0).ToList();

            if (!valid.Any()) return 0;

            return Math.Round(valid.Average(x => x.Avg3MActiveRate), 2);
        }

        /// <summary>
        /// ActivationSegment entity property-sindən istifadə edir —
        /// hər sətrin öz segmenti var, biz sadəcə qruplayıb M1Cards-ı toplayırıq.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(
            string bankName, CancellationToken ct = default)
        {
            var data = (await _repo.GetTrendAsync(bankName, ct)).ToList();

            var total = data.Sum(x => x.M1Cards);
            if (total == 0) return new Dictionary<string, decimal>();

            var segments = new[] { "EarlyActive", "DelayedActive", "SlowActive", "Inactive" };

            return segments.ToDictionary(
                seg => seg,
                seg => Math.Round(
                    (decimal)data
                        .Where(x => x.ActivationSegment == seg)
                        .Sum(x => x.M1Cards) / total * 100,
                    2));
        }
    }
}
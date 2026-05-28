using RMS.Contract.DTOs;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class WorldMapService : IWorldMapService
    {
        private readonly IWorldMapRepository _repo;

        public WorldMapService(IWorldMapRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<WorldMapTransaction>> FilterAsync(
            WorldMapTransaction f,
            PageRequest pageReq,
            CancellationToken ct = default)
            => _repo.FilterAsync(f, pageReq, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(
            string country, CancellationToken ct = default)
            => _repo.GetBySourceCountryAsync(country, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetIssuingAsync(bankName, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetAcquiringAsync(bankName, ct);

        public async Task<IEnumerable<CountryAmountDto>> GetAmountByCountryAsync(
     string? bankName,
     DateTime? month,
     CancellationToken ct = default)
        {
            IEnumerable<WorldMapTransaction> data;

            if (bankName is not null && month is not null)
                data = await _repo.GetByBankAndMonthAsync(bankName, month.Value, ct);
            else if (bankName is not null)
                data = await _repo.GetByBankAsync(bankName, ct);
            else if (month is not null)
                data = await _repo.GetByMonthAsync(month.Value, ct);
            else
                data = await _repo.GetAllAsync(ct);

            return data
                .Where(x => x.TargetLatitude.HasValue && x.TargetLongitude.HasValue
                         && !string.IsNullOrWhiteSpace(x.TargetCountry))
                .GroupBy(x => x.TargetCountry!)
                .Select(g => new CountryAmountDto
                {
                    Country = g.Key,
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    Latitude = g.First(x => x.TargetLatitude.HasValue).TargetLatitude,
                    Longitude = g.First(x => x.TargetLatitude.HasValue).TargetLongitude
                });
        }
    }
}
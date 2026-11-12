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
            bool hasBank = !string.IsNullOrWhiteSpace(bankName);

            if (hasBank && month is not null)
                data = await _repo.GetByBankAndMonthAsync(bankName!.Trim(), month.Value, ct);
            else if (hasBank)
                data = await _repo.GetByBankAsync(bankName!.Trim(), ct);
            else if (month is not null)
                data = await _repo.GetByMonthAsync(month.Value, ct);
            else
                data = await _repo.GetAllAsync(ct);

            return data
                .Select(x =>
                {
                    bool useSource = !string.IsNullOrWhiteSpace(x.SourceCountry)
                        && !string.Equals(x.SourceCountry, "Azerbaijan", StringComparison.OrdinalIgnoreCase)
                        && x.SourceLatitude.HasValue && x.SourceLongitude.HasValue;

                    if (useSource)
                    {
                        return new
                        {
                            Country = x.SourceCountry!,
                            x.TotalAmount,
                            Latitude = x.SourceLatitude,
                            Longitude = x.SourceLongitude
                        };
                    }

                    if (!string.IsNullOrWhiteSpace(x.TargetCountry)
                        && x.TargetLatitude.HasValue && x.TargetLongitude.HasValue)
                    {
                        return new
                        {
                            Country = x.TargetCountry!,
                            x.TotalAmount,
                            Latitude = x.TargetLatitude,
                            Longitude = x.TargetLongitude
                        };
                    }

                    return null;
                })
                .Where(x => x is not null)
                .GroupBy(x => x!.Country)
                .Select(g => new CountryAmountDto
                {
                    Country = g.Key,
                    TotalAmount = g.Sum(x => x!.TotalAmount),
                    Latitude = g.First()!.Latitude,
                    Longitude = g.First()!.Longitude
                });
        }
    }
}
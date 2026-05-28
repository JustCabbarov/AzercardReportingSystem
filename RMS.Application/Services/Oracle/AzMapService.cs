using RMS.Contract.DTOs;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class AzMapService : IAzMapService
    {
        private readonly IAzMapRepository _repo;

        public AzMapService(IAzMapRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapFilterRequest f, PageRequest pageReq, CancellationToken ct = default)
        {
            var transaction = new AzMapTransaction
            {
                BankName = f.BankName,
                SourceCity = f.SourceCity,
                SourceCityClean = f.SourceCityClean,
                SourceCityCategory = f.SourceCityCategory,
                ReportMonth = f.ReportMonth ?? default
            };

            return _repo.FilterAsync(transaction, pageReq, ct);
        }

        public async Task<IEnumerable<CityAmountDto>> GetHeatmapDataAsync(
            string? bankName, string? sourceCity, DateTime? month, CancellationToken ct = default)
        {
            var data = await _repo.GetHeatmapDataAsync(bankName, sourceCity, month, ct);

            return data
                .Where(x => x.Latitude.HasValue && x.Longitude.HasValue)
                .GroupBy(x => x.SourceCityClean ?? x.SourceCity ?? "")
                .Select(g => new CityAmountDto
                {
                    City = g.Key,
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    TotalCount = g.Sum(x => x.TotalCount),
                    Latitude = g.First().Latitude!.Value,
                    Longitude = g.First().Longitude!.Value
                });
        }

        public Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken ct = default)
            => _repo.GetDistinctCitiesAsync(ct);

        public Task<IEnumerable<string>> GetDistinctRegionsAsync(CancellationToken ct = default)
            => _repo.GetDistinctRegionsAsync(ct);

        public Task<IEnumerable<string>> GetDistinctBanksAsync(CancellationToken ct = default)
            => _repo.GetDistinctBanksAsync(ct);
    }
}
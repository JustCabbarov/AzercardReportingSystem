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
        public Task<IEnumerable<AzMapTransaction>> GetHeatmapDataAsync(
            string? bankName, string? sourceCity, DateTime? month, CancellationToken ct = default)
            => _repo.GetHeatmapDataAsync(bankName, sourceCity, month, ct);

        public Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken ct = default)
            => _repo.GetDistinctCitiesAsync(ct);

        public Task<IEnumerable<string>> GetDistinctRegionsAsync(CancellationToken ct = default)
            => _repo.GetDistinctRegionsAsync(ct);

        public Task<IEnumerable<string>> GetDistinctBanksAsync(CancellationToken ct = default)
            => _repo.GetDistinctBanksAsync(ct);
    }
}
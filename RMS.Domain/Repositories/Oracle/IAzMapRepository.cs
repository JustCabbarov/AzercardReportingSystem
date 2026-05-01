using RMS.Domain.Entities.Oracle;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IAzMapRepository
    {
        Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapTransaction f, PageRequest pageReq, CancellationToken ct = default);

        Task<IEnumerable<AzMapTransaction>> GetHeatmapDataAsync(
            string? bankName, string? sourceCity, DateTime? month, CancellationToken ct = default);

        Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetDistinctRegionsAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetDistinctBanksAsync(CancellationToken ct = default);
    }
}
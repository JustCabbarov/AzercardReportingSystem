using RMS.Contract.DTOs;
using RMS.Domain.Entities.Oracle;

namespace RMS.Contract.Services.Oracle
{
    public interface IAzMapService
    {
        Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapFilterRequest f, PageRequest pageReq, CancellationToken ct = default);

        Task<IEnumerable<CityAmountDto>> GetHeatmapDataAsync(
            string? bankName, string? sourceCity, DateTime? month, CancellationToken ct = default);

        Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetDistinctRegionsAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetDistinctBanksAsync(CancellationToken ct = default);
    }
}
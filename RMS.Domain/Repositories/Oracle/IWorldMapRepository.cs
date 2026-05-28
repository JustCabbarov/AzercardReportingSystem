using RMS.Domain.Entities.Oracle;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IWorldMapRepository
    {
        Task<IEnumerable<WorldMapTransaction>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(DateTime month, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetByBankAndMonthAsync(string bankName, DateTime month, CancellationToken ct = default); // ← əlavə edildi
        Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(string country, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(string bankName, CancellationToken ct = default);
        Task<PagedResult<WorldMapTransaction>> FilterAsync(
    WorldMapTransaction f,
    PageRequest pageReq,
    CancellationToken ct = default);
    }
}
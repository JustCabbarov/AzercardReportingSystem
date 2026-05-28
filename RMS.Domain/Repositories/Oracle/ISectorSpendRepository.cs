using RMS.Domain.Entities.Oracle;

public interface ISectorSpendRepository
{
    Task<PagedResult<SectorSpend>> GetAllAsync(PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> GetByBankAsync(string bankName, PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> GetByBankAndMonthAsync(string bankName, DateTime month, PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> GetByMccGroupAsync(string mccGroup, PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> FilterAsync(SectorSpend f, PageRequest pageReq, CancellationToken ct = default);
    Task<IEnumerable<SectorSpendTrend>> GetTrendAsync(string bankName, string mccGroup, CancellationToken ct = default);
    Task<IEnumerable<(string Hash, string Name)>> GetDistinctBanksAsync(CancellationToken ct = default);
    Task<IEnumerable<(string Hash, string Name)>> GetDistinctMccGroupsAsync(CancellationToken ct = default);
}
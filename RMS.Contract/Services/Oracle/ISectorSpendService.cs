using RMS.Domain.Entities.Oracle;

public interface ISectorSpendService
{
    Task<PagedResult<SectorSpend>> GetAllAsync(PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> GetByBankAsync(string bankName, PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> GetByBankAndMonthAsync(string bankName, DateTime month, PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> GetByMccGroupAsync(string mccGroup, PageRequest pageReq, CancellationToken ct = default);
    Task<PagedResult<SectorSpend>> FilterAsync(SectorSpend f, PageRequest pageReq, CancellationToken ct = default);
    Task<IEnumerable<SectorSpendTrend>> GetTrendAsync(string bankName, string mccGroup, CancellationToken ct = default);
    Task<IEnumerable<SectorSpend>> GetTopMccGroupsAsync(string bankName, DateTime month, int topN = 10, CancellationToken ct = default);
    Task<Dictionary<string, decimal>> GetChannelDistributionAsync(string bankName, DateTime month, CancellationToken ct = default);
}
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IDevicesRepository
    {
        Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension);


        Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime reportMonth,
            string xDimension, string yDimension,
            string? bankName, string? regionName, string? mccName, string? retailCategory);

        Task<DateTime> GetLatestReportMonthAsync();

        Task<long> GetTotalDevicesAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory);

        // ── Paged ────────────────────────────────────────────────────────────

        Task<PagedResult<SummaryItem>> GetSummaryPagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default);

        Task<PagedResult<ShareItem>> GetSharePagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default);

        Task<PagedResult<MomItem>> GetMomComparisonPagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default);

        Task<PagedResult<TrendItem>> GetTrendPagedAsync(
            DateTime dateFrom, DateTime dateTo,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default);
    }
}
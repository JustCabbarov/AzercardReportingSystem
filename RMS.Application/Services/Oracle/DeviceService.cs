using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class DevicesService : IDevicesService
    {
        private readonly IDevicesRepository _repo;

        public DevicesService(IDevicesRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension)
            => _repo.GetFiltersAsync(dimension);

       
        public Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime reportMonth,
            string xDimension, string yDimension,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => _repo.GetXyAnalysisAsync(reportMonth, xDimension, yDimension, bankName, regionName, mccName, retailCategory);

        public Task<DateTime> GetLatestReportMonthAsync()
            => _repo.GetLatestReportMonthAsync();

        public Task<long> GetTotalDevicesAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => _repo.GetTotalDevicesAsync(reportMonth, bankName, regionName, mccName, retailCategory);

        // ── Paged ────────────────────────────────────────────────────────────

        public Task<PagedResult<SummaryItem>> GetSummaryPagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
            => _repo.GetSummaryPagedAsync(reportMonth, bankName, regionName, mccName, retailCategory, pageReq, ct);

        public Task<PagedResult<ShareItem>> GetSharePagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
            => _repo.GetSharePagedAsync(reportMonth, bankName, regionName, mccName, retailCategory, pageReq, ct);

        public Task<PagedResult<MomItem>> GetMomComparisonPagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
            => _repo.GetMomComparisonPagedAsync(reportMonth, bankName, regionName, mccName, retailCategory, pageReq, ct);

        public Task<PagedResult<TrendItem>> GetTrendPagedAsync(
            DateTime dateFrom, DateTime dateTo,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default)
            => _repo.GetTrendPagedAsync(dateFrom, dateTo, bankName, regionName, mccName, retailCategory, pageReq, ct);
    }
}
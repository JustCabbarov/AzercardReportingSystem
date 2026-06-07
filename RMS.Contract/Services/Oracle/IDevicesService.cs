using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface IDevicesService
    {
        Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension);
        public Task<PagedResult<SummaryItem>> GetSummaryPagedAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory,
            PageRequest pageReq,
            CancellationToken ct = default);
        public Task<PagedResult<ShareItem>> GetSharePagedAsync(
       DateTime reportMonth,
       string? bankName, string? regionName, string? mccName, string? retailCategory,
       PageRequest pageReq,
       CancellationToken ct = default);
        public Task<PagedResult<MomItem>> GetMomComparisonPagedAsync(
     DateTime reportMonth,
     string? bankName, string? regionName, string? mccName, string? retailCategory,
     PageRequest pageReq,
     CancellationToken ct = default);
        public Task<PagedResult<TrendItem>> GetTrendPagedAsync(
     DateTime dateFrom, DateTime dateTo,
     string? bankName, string? regionName, string? mccName, string? retailCategory,
     PageRequest pageReq,
     CancellationToken ct = default);
        Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime reportMonth,
            string xDimension, string yDimension,
            string? bankName, string? regionName, string? mccName, string? retailCategory);
        Task<DateTime> GetLatestReportMonthAsync();
        Task<long> GetTotalDevicesAsync(DateTime reportMonth, string? bankName, string? regionName, string? mccName, string? retailCategory);
    }
}

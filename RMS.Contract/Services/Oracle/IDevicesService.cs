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
        Task<IEnumerable<SummaryItem>> GetSummaryAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory);
        Task<IEnumerable<ShareItem>> GetShareAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory);
        Task<IEnumerable<MomItem>> GetMomComparisonAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory);
        Task<IEnumerable<TrendItem>> GetTrendAsync(
            DateTime dateFrom, DateTime dateTo,
            string? bankName, string? regionName, string? mccName, string? retailCategory);
        Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime reportMonth,
            string xDimension, string yDimension,
            string? bankName, string? regionName, string? mccName, string? retailCategory);
        Task<DateTime> GetLatestReportMonthAsync();
        Task<long> GetTotalDevicesAsync(DateTime reportMonth, string? bankName, string? regionName, string? mccName, string? retailCategory);
    }
}

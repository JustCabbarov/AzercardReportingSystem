using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IDevicesRepository
    {
        Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension);

        Task<IEnumerable<SummaryItem>> GetSummaryAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories);

        Task<IEnumerable<ShareItem>> GetShareAsync(
            DateTime dateFrom, DateTime dateTo,
            string? dimension,
            List<string>? dimensionValues);

        Task<IEnumerable<MomItem>> GetMomComparisonAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories);

        Task<DeviceTrendResponse> GetTrendAsync(DeviceTrendRequest r);

        Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime dateFrom, DateTime dateTo,
            string xDimension, string yDimension,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories);

        Task<DateTime> GetLatestReportMonthAsync();

        Task<object> GetTotalDevicesAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories);
    }
}
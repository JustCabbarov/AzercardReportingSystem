using RMS.Contract.Services.Oracle;
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

        public Task<IEnumerable<SummaryItem>> GetSummaryAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
            => _repo.GetSummaryAsync(dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);

        public Task<IEnumerable<ShareItem>> GetShareAsync(
      DateTime dateFrom, DateTime dateTo,
      string? dimension,
      List<string>? bankNames,
      List<string>? regionNames,
      List<string>? mccNames,
      List<string>? retailCategories)
      => _repo.GetShareAsync(dateFrom, dateTo, dimension,
          bankNames, regionNames, mccNames, retailCategories);

        public Task<IEnumerable<MomItem>> GetMomComparisonAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
            => _repo.GetMomComparisonAsync(dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);

        public Task<DeviceTrendResponse> GetTrendAsync(DeviceTrendRequest r)
            => _repo.GetTrendAsync(r);

        public Task<IEnumerable<XyItem>> GetXyAnalysisAsync(
            DateTime dateFrom, DateTime dateTo,
            string xDimension, string yDimension,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
            => _repo.GetXyAnalysisAsync(dateFrom, dateTo, xDimension, yDimension, bankNames, regionNames, mccNames, retailCategories);

        public Task<DateTime> GetLatestReportMonthAsync()
            => _repo.GetLatestReportMonthAsync();

        public Task<object> GetTotalDevicesAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
            => _repo.GetTotalDevicesAsync(dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);
    }
}
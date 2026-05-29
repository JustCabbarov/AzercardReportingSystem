using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => _repo.GetSummaryAsync(reportMonth, bankName, regionName, mccName, retailCategory);

        public Task<IEnumerable<ShareItem>> GetShareAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => _repo.GetShareAsync(reportMonth, bankName, regionName, mccName, retailCategory);

        public Task<IEnumerable<MomItem>> GetMomComparisonAsync(
            DateTime reportMonth,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => _repo.GetMomComparisonAsync(reportMonth, bankName, regionName, mccName, retailCategory);

        public Task<IEnumerable<TrendItem>> GetTrendAsync(
            DateTime dateFrom, DateTime dateTo,
            string? bankName, string? regionName, string? mccName, string? retailCategory)
            => _repo.GetTrendAsync(dateFrom, dateTo, bankName, regionName, mccName, retailCategory);

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
    }
}

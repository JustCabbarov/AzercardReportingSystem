using RMS.Application.Forecasting;
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

        // ── TREND + GROWTH-RATE FORECAST ─────────────────────────────────────────

        public async Task<DeviceTrendResponse> GetTrendAsync(DeviceTrendRequest r)
        {
            var response = await _repo.GetTrendAsync(r);

            // EndDate-dən sonrakı 12 ayı proqnozlaşdır
            var endDate = r.DateTo;

            foreach (var series in response.Series)
                AppendGrowthRateForecast(series, endDate);

            return response;
        }

        /// <summary>
        /// Terminal aktivliyi trend seriyasına Growth-Rate əsaslı proqnoz nöqtələri əlavə edir.
        /// </summary>
        private static void AppendGrowthRateForecast(DeviceTrendSeries series, DateTime endDate)
        {
            // Yalnız real nöqtələri götür
            var actualPoints = series.Points.Where(p => !p.IsForecast).ToList();
            if (actualPoints.Count < 2) return;

            var actuals = actualPoints
                .Select(p => (p.Period, (decimal)p.Count))
                .ToList();

            var forecasts = GrowthRateForecastEngine.Compute(actuals, endDate, horizonMonths: 12);

            foreach (var f in forecasts)
            {
                series.Points.Add(new DeviceTrendPoint
                {
                    Period             = f.Month,
                    Count              = 0,
                    IsForecast         = true,
                    Forecast           = f.Forecast,
                    ForecastGrowthRate = f.GrowthRate
                });
            }
        }

        // ── DİGƏR METODLAR ───────────────────────────────────────────────────────

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
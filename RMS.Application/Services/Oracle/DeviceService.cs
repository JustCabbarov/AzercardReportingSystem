using RMS.Application.Caching;
using RMS.Application.Forecasting;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class DevicesService : IDevicesService
    {
        private static readonly TimeSpan LookupTtl = TimeSpan.FromHours(2);
        private readonly IDevicesRepository _repo;
        private readonly IInMemoryCacheService? _cache;

        public DevicesService(IDevicesRepository repo, IInMemoryCacheService? cache = null)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<IEnumerable<FilterValue>> GetFiltersAsync(string dimension)
        {
            if (_cache is null)
                return await _repo.GetFiltersAsync(dimension);

            var key = $"oracle:lookup:devices:filters:{dimension.ToLowerInvariant()}";
            return await _cache.GetOrCreateAsync(
                key,
                async _ => (await _repo.GetFiltersAsync(dimension)).ToList(),
                LookupTtl);
        }

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

            // EndDate-dən sonrakı 12 ayı proqnozlaşdır; illik artım faizi üçün 24 aylıq tarixçə lazımdır
            var endDate = r.DateTo;
            var historyStart = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-24);

            DeviceTrendResponse? historyResponse = null;
            if (r.DateFrom > historyStart)
            {
                historyResponse = await _repo.GetTrendAsync(new DeviceTrendRequest
                {
                    DateFrom        = historyStart,
                    DateTo          = endDate,
                    Dimension       = r.Dimension,
                    DimensionValues = r.DimensionValues
                });
            }

            var historyByLabel = historyResponse?.Series
                .ToDictionary(s => s.Label, s => s.Points.Where(p => !p.IsForecast).ToList());

            foreach (var series in response.Series)
            {
                List<DeviceTrendPoint>? historyPoints = null;
                historyByLabel?.TryGetValue(series.Label, out historyPoints);
                AppendGrowthRateForecast(series, endDate, historyPoints);
            }

            return response;
        }

        /// <summary>
        /// Terminal aktivliyi trend seriyasına Growth-Rate əsaslı proqnoz nöqtələri əlavə edir.
        /// </summary>
        private static void AppendGrowthRateForecast(
            DeviceTrendSeries series,
            DateTime endDate,
            List<DeviceTrendPoint>? historyPoints = null)
        {
            // Yalnız real nöqtələri götür
            var sourcePoints = (historyPoints is { Count: >= 2 } ? historyPoints : series.Points)
                .Where(p => !p.IsForecast)
                .ToList();
            if (sourcePoints.Count < 2) return;

            var actuals = sourcePoints
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
            => _cache is null
                ? _repo.GetLatestReportMonthAsync()
                : _cache.GetOrCreateAsync("oracle:lookup:devices:latest-month", _ => _repo.GetLatestReportMonthAsync(), LookupTtl);

        public Task<object> GetTotalDevicesAsync(
            DateTime dateFrom, DateTime dateTo,
            List<string>? bankNames, List<string>? regionNames,
            List<string>? mccNames, List<string>? retailCategories)
            => _repo.GetTotalDevicesAsync(dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);
    }
}
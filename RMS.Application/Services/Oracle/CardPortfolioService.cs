using RMS.Application.Caching;
using RMS.Application.Forecasting;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class CardPortfolioService : ICardPortfolioService
    {
        private static readonly TimeSpan LookupTtl = TimeSpan.FromHours(2);
        private readonly ICardPortfolioRepository _repo;
        private readonly IInMemoryCacheService? _cache;

        public CardPortfolioService(ICardPortfolioRepository repo, IInMemoryCacheService? cache = null)
        {
            _repo = repo;
            _cache = cache;
        }

        public Task<FilterOptionsResponse> GetFilterOptionsAsync(CancellationToken ct = default)
            => _cache is null
                ? _repo.GetFilterOptionsAsync(ct)
                : _cache.GetOrCreateAsync(
                    "oracle:lookup:cards:filter-options",
                    token => _repo.GetFilterOptionsAsync(token),
                    LookupTtl,
                    ct);

        public Task<DateTime> GetLatestReportMonthAsync()
            => _cache is null
                ? _repo.GetLatestReportMonthAsync()
                : _cache.GetOrCreateAsync("oracle:lookup:cards:latest-month", _ => _repo.GetLatestReportMonthAsync(), LookupTtl);

        public Task<IEnumerable<TopSchemeCardDto>> GetTopCardsAsync(CardPortfolioFilter f, string? dimension)
            => _repo.GetTopCardsAsync(f, dimension);

        public Task<CrossTableResponse> GetCrossTableAsync(CrossTableRequest r) => _repo.GetCrossTableAsync(r);
        public Task<PayChartResponse> GetPayChartAsync(PayChartRequest r) => _repo.GetPayChartAsync(r);

        // ── TREND + GROWTH-RATE FORECAST ─────────────────────────────────────────

        public async Task<TrendChartResponse> GetTrendAsync(TrendChartRequest r)
        {
            var response = await _repo.GetTrendAsync(r);

            var isMonthly = string.IsNullOrWhiteSpace(r.Granularity)
                || r.Granularity.Trim().Equals("month", StringComparison.OrdinalIgnoreCase);
            if (!isMonthly)
                return response;

            // EndDate-dən sonrakı 12 ayı proqnozlaşdır; illik artım faizi üçün 24 aylıq tarixçə lazımdır
            var endDate = r.ToMonth ?? DateTime.Today;
            var historyStart = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-24);

            TrendChartResponse? historyResponse = null;
            if (r.FromMonth is null || r.FromMonth.Value > historyStart)
            {
                historyResponse = await _repo.GetTrendAsync(new TrendChartRequest
                {
                    FromMonth         = historyStart,
                    ToMonth           = endDate,
                    BankNames         = r.BankNames,
                    RegionNames       = r.RegionNames,
                    PaymentSchemes    = r.PaymentSchemes,
                    ProductTypes      = r.ProductTypes,
                    ContactlessStatus = r.ContactlessStatus,
                    ExpStatus         = r.ExpStatus,
                    Status3Ds         = r.Status3Ds,
                    BaseCurrencies    = r.BaseCurrencies,
                    CardProductNames  = r.CardProductNames,
                    Dimension         = r.Dimension,
                    DimValues         = r.DimValues,
                    Granularity       = "month"
                });
            }

            var historyByLabel = historyResponse?.Series
                .ToDictionary(s => s.Label, s => s.Points.Where(p => !p.IsForecast).ToList());

            foreach (var series in response.Series)
            {
                List<TrendPoint>? historyPoints = null;
                historyByLabel?.TryGetValue(series.Label, out historyPoints);
                AppendGrowthRateForecast(series, endDate, historyPoints);
            }

            return response;
        }

        /// <summary>
        /// Kart portfoliosu trend seriyasına Growth-Rate əsaslı proqnoz nöqtələri əlavə edir.
        /// </summary>
        private static void AppendGrowthRateForecast(
            TrendSeries series,
            DateTime endDate,
            List<TrendPoint>? historyPoints = null)
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
                series.Points.Add(new TrendPoint
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

        public Task<XyChartResponse> GetXyChartAsync(XyChartRequest r) => _repo.GetXyChartAsync(r);
    }
}
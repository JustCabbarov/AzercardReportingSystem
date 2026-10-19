using RMS.Application.Forecasting;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class CardPortfolioService : ICardPortfolioService
    {
        private readonly ICardPortfolioRepository _repo;

        public CardPortfolioService(ICardPortfolioRepository repo)
        {
            _repo = repo;
        }

        public Task<FilterOptionsResponse> GetFilterOptionsAsync() => _repo.GetFilterOptionsAsync();
        public Task<DateTime> GetLatestReportMonthAsync() => _repo.GetLatestReportMonthAsync();

        public Task<IEnumerable<TopSchemeCardDto>> GetTopCardsAsync(CardPortfolioFilter f, string? dimension)
            => _repo.GetTopCardsAsync(f, dimension);

        public Task<CrossTableResponse> GetCrossTableAsync(CrossTableRequest r) => _repo.GetCrossTableAsync(r);
        public Task<PayChartResponse> GetPayChartAsync(PayChartRequest r) => _repo.GetPayChartAsync(r);

        // ── TREND + GROWTH-RATE FORECAST ─────────────────────────────────────────

        public async Task<TrendChartResponse> GetTrendAsync(TrendChartRequest r)
        {
            var response = await _repo.GetTrendAsync(r);

            // EndDate-dən sonrakı 12 ayı proqnozlaşdır
            var endDate = r.ToMonth ?? DateTime.Today;

            foreach (var series in response.Series)
                AppendGrowthRateForecast(series, endDate);

            return response;
        }

        /// <summary>
        /// Kart portfoliosu trend seriyasına Growth-Rate əsaslı proqnoz nöqtələri əlavə edir.
        /// </summary>
        private static void AppendGrowthRateForecast(TrendSeries series, DateTime endDate)
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
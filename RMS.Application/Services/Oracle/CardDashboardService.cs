using Microsoft.Extensions.Logging;
using RMS.Application.Forecasting;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardTransaction;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class CardDashboardService : ICardDashboardService
    {
        private readonly ICardDashboardRepository _repo;
        private readonly ILogger<CardDashboardService> _logger;

        public CardDashboardService(
            ICardDashboardRepository repo,
            ILogger<CardDashboardService> logger)
        {
            _repo   = repo;
            _logger = logger;
        }

        // ── KPI ──────────────────────────────────────────────────────────────────

        public Task<KpiSummaryResponse> GetKpiAsync(DashboardFilterRequest filter)
            => _repo.GetKpiSummaryAsync(filter);

        // ── TREND + GROWTH-RATE FORECAST ─────────────────────────────────────────

        public async Task<List<TrendPoint>> GetTrendWithForecastAsync(DashboardFilterRequest filter)
        {
            // 1. Real trend datasını repo-dan al
            var trend = await _repo.GetTrendAsync(filter);

            // 2. Proqnoz yalnız aylıq rejimdə və ən azı 2 nöqtə varsa hesablanır
            if (filter.DateGrouping != DateGrouping.Monthly || trend.Count < 2)
            {
                _logger.LogInformation(
                    "GrowthRate forecast atlandı: DateGrouping={G}, TrendCount={C}",
                    filter.DateGrouping, trend.Count);
                return trend;
            }

            var endDate = filter.DateTo ?? DateTime.Today;
            var historyStart = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-24);

            // İllik artım faizi üçün seçilmiş aralıq 24 aydan qısadırsa tarixçəni genişləndir
            var historyTrend = (filter.DateFrom is null || filter.DateFrom.Value > historyStart)
                ? await _repo.GetTrendAsync(new DashboardFilterRequest
                {
                    DateFrom         = historyStart,
                    DateTo           = endDate,
                    DateGrouping     = filter.DateGrouping,
                    RoundTo          = filter.RoundTo,
                    TargetBankName   = filter.TargetBankName,
                    TransGroup       = filter.TransGroup,
                    CardTypeCategory = filter.CardTypeCategory,
                    PaymentSystem    = filter.PaymentSystem,
                    PaymentType      = filter.PaymentType,
                    OperationType    = filter.OperationType,
                    TokenStatus      = filter.TokenStatus,
                    IsContactless    = filter.IsContactless
                })
                : trend;

            var realHistory = historyTrend.Where(t => !t.IsForecast).ToList();
            if (realHistory.Count < 2)
                realHistory = trend.Where(t => !t.IsForecast).ToList();

            // 3. Əvvəlki ilin artım faizinə əsasən həm Dövriyyə, həm Əməliyyat sayı üçün proqnoz hesabla
            var amountActuals = realHistory
                .Select(t => (t.PeriodDate, t.TotalAmount))
                .ToList();
            var countActuals = realHistory
                .Select(t => (t.PeriodDate, (decimal)t.TotalTxCount))
                .ToList();

            var amountForecasts = GrowthRateForecastEngine.Compute(amountActuals, endDate, horizonMonths: 12);
            var countForecastByMonth = GrowthRateForecastEngine
                .Compute(countActuals, endDate, horizonMonths: 12)
                .ToDictionary(c => c.Month, c => Math.Round(c.Forecast, 0));

            // 4. Forecast nöqtələrini trend siyahısına əlavə et
            foreach (var f in amountForecasts)
            {
                countForecastByMonth.TryGetValue(f.Month, out var txForecast);
                trend.Add(new TrendPoint
                {
                    PeriodDate      = f.Month,
                    PeriodLabel     = f.Month.ToString("MM/yyyy"),
                    IsForecast      = true,

                    // Proqnoz dəyərləri
                    ForecastAmount  = f.Forecast,
                    ForecastTxCount = txForecast,

                    // Real dəyərlər yoxdur (gələcək)
                    TotalAmount     = 0,
                    TotalTxCount    = 0,
                });
            }

            _logger.LogInformation(
                "GrowthRate forecast əlavə edildi: EndDate={D}, ForecastCount={C}",
                endDate, amountForecasts.Count);

            return trend;
        }

        // ── BREAKDOWN ────────────────────────────────────────────────────────────

        public Task<BreakdownResponse> GetBreakdownAsync(
            DashboardFilterRequest filter, BreakdownType type)
            => _repo.GetBreakdownAsync(filter, type);

        // ── FULL DASHBOARD (bütün widgetlər paralel) ─────────────────────────────

        public async Task<DashboardResponse> GetFullDashboardAsync(DashboardFilterRequest filter)
        {
            var kpiTask   = _repo.GetKpiSummaryAsync(filter);
            var prodTask  = _repo.GetBreakdownAsync(filter, BreakdownType.ProductType);
            var psTask    = _repo.GetBreakdownAsync(filter, BreakdownType.PaymentSystem);
            var ptTask    = _repo.GetBreakdownAsync(filter, BreakdownType.PaymentType);
            var cnTask    = _repo.GetBreakdownAsync(filter, BreakdownType.CashNonCash);
            var trendTask = GetTrendWithForecastAsync(filter);

            await Task.WhenAll(kpiTask, prodTask, psTask, ptTask, cnTask, trendTask);

            return new DashboardResponse
            {
                Kpi              = await kpiTask,
                Trend            = await trendTask,
                ProductBreakdown = await prodTask,
                PaymentSystem    = await psTask,
                PaymentType      = await ptTask,
                CashNonCash      = await cnTask,
            };
        }

        // ── FILTER LOOKUPS ───────────────────────────────────────────────────────

        public Task<FilterLookupResponse> GetFilterLookupsAsync()
            => _repo.GetFilterLookupsAsync();
    }
}

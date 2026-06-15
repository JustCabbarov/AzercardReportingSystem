using Microsoft.Extensions.Logging;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardTransaction;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.Oracle
{

    public class CardDashboardService : ICardDashboardService
    {
        private readonly ICardDashboardRepository _repo;
        private readonly IForecastingService _forecast;
        private readonly ILogger<CardDashboardService> _logger;

        // SSA üçün minimum data tələbi (12 ay)
        private const int MinPointsForForecast = 12;

        public CardDashboardService(
            ICardDashboardRepository repo,
            IForecastingService forecast,
            ILogger<CardDashboardService> logger)
        {
            _repo = repo;
            _forecast = forecast;
            _logger = logger;
        }

        // ── KPI ──────────────────────────────────────────────────────────────────

        public Task<KpiSummaryResponse> GetKpiAsync(DashboardFilterRequest filter)
            => _repo.GetKpiSummaryAsync(filter);

        // ── TREND + SSA FORECAST ─────────────────────────────────────────────────

        public async Task<List<TrendPoint>> GetTrendWithForecastAsync(DashboardFilterRequest filter)
        {
            // 1. Real trend datasını repo-dan al
            var trend = await _repo.GetTrendAsync(filter);

            // 2. Forecast yalnız aylıq rejimdə və yetərli data varsa hesablanır
            if (filter.DateGrouping != DateGrouping.Monthly || trend.Count < MinPointsForForecast)
            {
                _logger.LogInformation(
                    "Forecast atlandı: DateGrouping={G}, TrendCount={C}",
                    filter.DateGrouping, trend.Count);
                return trend;
            }

            // 3. İlin sonuna neçə ay qaldığını hesabla
            int horizonMonths = 12 - DateTime.Today.Month;
            if (horizonMonths <= 0)
            {
                _logger.LogInformation("Forecast atlandı: ilin sonu çatıb (ay={M})", DateTime.Today.Month);
                return trend;
            }

            // 4. Filter kombinasiyasına görə unikal model açarı
            var key = BuildForecastKey(filter);

            try
            {
                // 5. Model mövcud deyilsə train et
                if (!_forecast.ModelExists(key))
                {
                    _logger.LogInformation("SSA train başladı: Key={Key}", key);

                    var inputs = trend.Select((t, i) => new ForecastInput
                    {
                        TimeIndex = i + 1,                  // int — sıra nömrəsi (1, 2, 3...)
                        TotalAmount = (float)t.TotalAmount,
                        TotalCount = (float)t.TotalTxCount,
                    }).ToList();

                    await _forecast.TrainFromSeriesAsync(key, inputs);
                    _logger.LogInformation("SSA train tamamlandı: Key={Key}", key);
                }

                // 6. Proqnoz al
                var result = await _forecast.ForecastFromSeriesAsync(key, horizonMonths);

                if (result.Forecasts.Count == 0)
                {
                    _logger.LogWarning("SSA boş proqnoz qaytardı: Key={Key}", key);
                    return trend;
                }

                // 7. Forecast nöqtələrini trend siyahısına əlavə et
                foreach (var fc in result.Forecasts)
                {
                    trend.Add(new TrendPoint
                    {
                        PeriodDate = fc.Month,
                        PeriodLabel = fc.Month.ToString("MM/yyyy"),
                        IsForecast = true,

                        // Forecast dəyərləri
                        ForecastAmount = (decimal)fc.PredictedAmount,
                        ForecastAmountLow = (decimal)fc.AmountLowerBound,
                        ForecastAmountHigh = (decimal)fc.AmountUpperBound,
                        ForecastTxCount = (decimal)fc.PredictedCount,
                        ForecastTxCountLow = (decimal)fc.CountLowerBound,
                        ForecastTxCountHigh = (decimal)fc.CountUpperBound,
                        ForecastAccuracy = result.Accuracy,

                        // Real dəyərlər yoxdur (gələcək)
                        TotalAmount = 0,
                        TotalTxCount = 0,
                    });
                }

                _logger.LogInformation(
                    "SSA forecast əlavə edildi: Key={Key}, Horizon={H}, Accuracy={A}%",
                    key, horizonMonths, result.Accuracy);
            }
            catch (Exception ex)
            {
                // Forecast xətası bütün response-u bloklamamalıdır
                _logger.LogError(ex, "SSA forecast xətası: Key={Key}", key);
            }

            return trend;
        }

        // ── BREAKDOWN ────────────────────────────────────────────────────────────

        public Task<BreakdownResponse> GetBreakdownAsync(
            DashboardFilterRequest filter, BreakdownType type)
            => _repo.GetBreakdownAsync(filter, type);

        // ── FULL DASHBOARD (bütün widgetlər paralel) ─────────────────────────────

        public async Task<DashboardResponse> GetFullDashboardAsync(DashboardFilterRequest filter)
        {
            // KPI + Breakdown-lar paralel işləyir
            var kpiTask = _repo.GetKpiSummaryAsync(filter);
            var prodTask = _repo.GetBreakdownAsync(filter, BreakdownType.ProductType);
            var psTask = _repo.GetBreakdownAsync(filter, BreakdownType.PaymentSystem);
            var ptTask = _repo.GetBreakdownAsync(filter, BreakdownType.PaymentType);
            var cnTask = _repo.GetBreakdownAsync(filter, BreakdownType.CashNonCash);

            // Trend + Forecast ayrıca (SSA train sürətini nəzərə alaraq)
            var trendTask = GetTrendWithForecastAsync(filter);

            await Task.WhenAll(kpiTask, prodTask, psTask, ptTask, cnTask, trendTask);

            return new DashboardResponse
            {
                Kpi = await kpiTask,
                Trend = await trendTask,
                ProductBreakdown = await prodTask,
                PaymentSystem = await psTask,
                PaymentType = await ptTask,
                CashNonCash = await cnTask,
            };
        }

        // ── FILTER LOOKUPS ───────────────────────────────────────────────────────

        public Task<FilterLookupResponse> GetFilterLookupsAsync()
            => _repo.GetFilterLookupsAsync();

        // ── HELPERS ──────────────────────────────────────────────────────────────

        /// Filter kombinasiyasına görə SSA model açarı qurur.
        /// Eyni filterlər → eyni model → yenidən train lazım deyil.
        private static string BuildForecastKey(DashboardFilterRequest f)
        {
            var parts = new[]
            {
            "CARD",
            f.PaymentSystem    ?? "ALL",
            f.CardTypeCategory ?? "ALL",
            f.OperationType    ?? "ALL",
            f.PaymentType      ?? "ALL",
            f.TargetBankName   ?? "ALL",
            f.TransGroup       ?? "ALL",
            f.TokenStatus      ?? "ALL",
            f.IsContactless.HasValue ? (f.IsContactless.Value ? "CTLS" : "NO_CTLS") : "ALL",
        };

            return string.Join("_", parts)
                         .Replace(" ", "_")
                         .ToUpperInvariant();
        }
    }
}

using RMS.Application.Forecasting;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.AcquiringTransaction;
using RMS.Domain.Repositories.Oracle;

public class AcquiringDeviceService : IAcquiringDeviceService
{
    private readonly IAcquiringDeviceRepository _repo;

    public AcquiringDeviceService(IAcquiringDeviceRepository repo)
    {
        _repo = repo;
    }

    public Task<AcquiringFilterOptionsResponse> GetFilterOptionsAsync()
        => _repo.GetFilterOptionsAsync();

    public Task<AcquiringDashboardResponse> GetDashboardAsync(AcquiringDeviceFilter f)
        => _repo.GetDashboardAsync(f);

    public Task<DateTime> GetLatestReportDateAsync()
        => _repo.GetLatestReportDateAsync();

    public async Task<AcqTrendResponse> GetTrendAsync(AcquiringTrendRequest r)
    {
        var trendResponse = await _repo.GetTrendAsync(r);

        // EndDate-dən sonrakı 12 ayı proqnozlaşdır
        var endDate = r.ToDate ?? DateTime.Today;

        foreach (var series in trendResponse.Series)
            AppendGrowthRateForecast(series, endDate);

        return trendResponse;
    }

    // ── Growth-Rate Proqnoz ──────────────────────────────────────────────────────

    /// <summary>
    /// Seriyaya Growth-Rate əsaslı proqnoz nöqtələri əlavə edir.
    /// GrowthRate(M) = actual(M, prev_year) / actual(M-1, prev_year)
    /// Forecast(M)   = Forecast_or_Actual(M-1) × GrowthRate(M)
    /// </summary>
    private static void AppendGrowthRateForecast(AcqTrendSeries series, DateTime endDate)
    {
        // Yalnız real (actual) nöqtələri götür
        var actualPoints = series.Points.Where(p => !p.IsForecast).ToList();
        if (actualPoints.Count < 2) return;

        var actuals = actualPoints
            .Select(p => (p.Period, p.Actual))
            .ToList();

        var forecasts = GrowthRateForecastEngine.Compute(actuals, endDate, horizonMonths: 12);

        foreach (var f in forecasts)
        {
            series.Points.Add(new AcqTrendPoint
            {
                Period        = f.Month,
                Actual        = 0,
                Forecast      = f.Forecast,
                IsForecast    = true,
                ForecastModel = "GrowthRate"
            });
        }
    }
}
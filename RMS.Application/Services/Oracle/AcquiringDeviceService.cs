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

        // Proqnoz yalnız aylıq rejimdə tətbiq olunur
        var isMonthly = string.IsNullOrWhiteSpace(r.Granularity)
            || r.Granularity.Trim().Equals("month", StringComparison.OrdinalIgnoreCase);
        if (!isMonthly)
            return trendResponse;

        // EndDate-dən sonrakı 12 ayı proqnozlaşdır; illik artım faizi üçün 24 aylıq tarixçə lazımdır
        var endDate = r.ToDate ?? DateTime.Today;
        var historyStart = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-24);

        AcqTrendResponse? historyResponse = null;
        if (r.FromDate is null || r.FromDate.Value > historyStart)
        {
            var historyRequest = new AcquiringTrendRequest
            {
                FromDate             = historyStart,
                ToDate               = endDate,
                Category             = r.Category,
                AmountUnit           = r.AmountUnit,
                AcquiringDeviceTypes = r.AcquiringDeviceTypes,
                TransGroups          = r.TransGroups,
                OperationTypes       = r.OperationTypes,
                TokenStatuses        = r.TokenStatuses,
                Mccs                 = r.Mccs,
                AcquiringCategories  = r.AcquiringCategories,
                TargetBankNames      = r.TargetBankNames,
                SourceBankNames      = r.SourceBankNames,
                TargetCities         = r.TargetCities,
                PaymentSystems       = r.PaymentSystems,
                ContactlessStatuses  = r.ContactlessStatuses,
                Dimension            = r.Dimension,
                DimValues            = r.DimValues,
                Granularity          = "month"
            };
            historyResponse = await _repo.GetTrendAsync(historyRequest);
        }

        var historyByLabel = historyResponse?.Series
            .ToDictionary(s => s.Label, s => s.Points.Where(p => !p.IsForecast).ToList());

        foreach (var series in trendResponse.Series)
        {
            List<AcqTrendPoint>? historyPoints = null;
            historyByLabel?.TryGetValue(series.Label, out historyPoints);
            AppendGrowthRateForecast(series, endDate, historyPoints);
        }

        return trendResponse;
    }

    // ── Growth-Rate Proqnoz ──────────────────────────────────────────────────────

    /// <summary>
    /// Seriyaya Growth-Rate əsaslı proqnoz nöqtələri əlavə edir.
    /// GrowthRate(M) = actual(M, prev_year) / actual(M-1, prev_year)
    /// Forecast(M)   = Forecast_or_Actual(M-1) × GrowthRate(M)
    /// </summary>
    private static void AppendGrowthRateForecast(
        AcqTrendSeries series,
        DateTime endDate,
        List<AcqTrendPoint>? historyPoints = null)
    {
        var sourcePoints = (historyPoints is { Count: >= 2 } ? historyPoints : series.Points)
            .Where(p => !p.IsForecast)
            .ToList();
        if (sourcePoints.Count < 2) return;

        var actuals = sourcePoints
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
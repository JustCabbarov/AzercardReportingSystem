using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.AcquiringTransaction;
using RMS.Domain.Repositories.Oracle;

public class AcquiringDeviceService : IAcquiringDeviceService
{
    private readonly IAcquiringDeviceRepository _repo;
    private readonly IForecastingService _forecasting;

    public AcquiringDeviceService(
        IAcquiringDeviceRepository repo,
        IForecastingService forecasting)
    {
        _repo = repo;
        _forecasting = forecasting;
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

        int horizon = MonthsUntilYearEnd();
        if (horizon <= 0) return trendResponse;

        foreach (var series in trendResponse.Series)
            await AppendSsaForecastAsync(series, BuildSeriesKey(series.Label, r), horizon);

        return trendResponse;
    }

    private async Task AppendSsaForecastAsync(
        AcqTrendSeries series, string seriesKey, int horizon)
    {
        series.Points.RemoveAll(p => p.IsForecast);

        var actualPoints = series.Points.Where(p => !p.IsForecast).ToList();
        if (actualPoints.Count < 3) return;

        try
        {
            if (!_forecasting.ModelExists(seriesKey))
            {
                var historicalData = actualPoints
                    .Select((p, i) => new ForecastInput
                    {
                        TimeIndex = i,
                        TotalAmount = (float)p.Actual,
                        TotalCount = (float)p.Actual
                    })
                    .ToList();

                await _forecasting.TrainFromSeriesAsync(seriesKey, historicalData);
            }

            var result = await _forecasting.ForecastFromSeriesAsync(seriesKey, horizon);

            if (result?.Forecasts?.Count > 0)
            {
                foreach (var f in result.Forecasts.Take(horizon))
                {
                    series.Points.Add(new AcqTrendPoint
                    {
                        Period = f.Month,
                        Actual = 0,
                        Forecast = (decimal)f.PredictedAmount,
                        ForecastLower = (decimal)f.AmountLowerBound,
                        ForecastUpper = (decimal)f.AmountUpperBound,
                        IsForecast = true,
                        ForecastAccuracy = result.Accuracy,
                        ForecastModel = result.ModelUsed
                    });
                }
                return;
            }
        }
        catch (Exception)
        {
            // SSA uğursuz olarsa moving average-ə düş
        }

        AppendMovingAverageForecast(series.Points, horizon);
    }

    private static void AppendMovingAverageForecast(
        List<AcqTrendPoint> points, int horizon)
    {
        if (points.Count < 3) return;

        var avg = points.TakeLast(3).Average(p => p.Actual);
        var lastPeriod = points.Last().Period;

        for (int i = 1; i <= horizon; i++)
        {
            points.Add(new AcqTrendPoint
            {
                Period = lastPeriod.AddMonths(i),
                Actual = 0,
                Forecast = Math.Round(avg, 2),
                IsForecast = true,
                ForecastModel = "MovingAverage"
            });
        }
    }

    private static int MonthsUntilYearEnd()
    {
        var today = DateTime.Today;
        var yearEnd = new DateTime(today.Year, 12, 1);
        return Math.Max(0, (yearEnd.Year - today.Year) * 12
                           + yearEnd.Month - today.Month);
    }

    private static string BuildSeriesKey(string label, AcquiringTrendRequest r)
    {
        var parts = new List<string> { label };

        if (r.AcquiringDeviceTypes?.Count > 0)
            parts.Add(string.Join("-", r.AcquiringDeviceTypes.Order()));
        if (r.TransGroups?.Count > 0)
            parts.Add(string.Join("-", r.TransGroups.Order()));
        if (r.OperationTypes?.Count > 0)
            parts.Add(string.Join("-", r.OperationTypes.Order()));
        if (r.TokenStatuses?.Count > 0)
            parts.Add(string.Join("-", r.TokenStatuses.Order()));
        if (r.Mccs?.Count > 0)
            parts.Add(string.Join("-", r.Mccs.Order()));
        if (r.AcquiringCategories?.Count > 0)
            parts.Add(string.Join("-", r.AcquiringCategories.Order()));
        if (r.TargetBankNames?.Count > 0)
            parts.Add(string.Join("-", r.TargetBankNames.Order()));
        if (r.SourceBankNames?.Count > 0)
            parts.Add(string.Join("-", r.SourceBankNames.Order()));
        if (r.TargetCities?.Count > 0)
            parts.Add(string.Join("-", r.TargetCities.Order()));
        if (r.PaymentSystems?.Count > 0)
            parts.Add(string.Join("-", r.PaymentSystems.Order()));
        if (r.ContactlessStatuses?.Count > 0)
            parts.Add(string.Join("-", r.ContactlessStatuses.Order()));

        var raw = string.Join("_", parts);
        if (raw.Length > 100)
        {
            var hash = Convert.ToHexString(
                System.Security.Cryptography.MD5.HashData(
                    System.Text.Encoding.UTF8.GetBytes(raw)))[..8];
            return $"{label}_{hash}";
        }

        return raw;
    }
}
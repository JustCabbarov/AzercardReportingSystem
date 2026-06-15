using RMS.Domain.Entities.Oracle;

namespace RMS.Contract.Services.Oracle;

public interface IForecastingService
{
    Task TrainGlobalAsync(CancellationToken ct = default);
    Task TrainAllAsync(CancellationToken ct = default);
    Task TrainAsync(string bankName, string mccGroup, CancellationToken ct = default);

    Task<ForecastResult> ForecastAsync(
        string bankName,
        string mccGroup,
        int horizonMonths,
        CancellationToken ct = default);

    Task<ForecastResult> ForecastAllAsync(
        int horizonMonths,
        CancellationToken ct = default);

    // ── Acquiring Device Trend üçün ──────────────────────────────────
    bool ModelExists(string seriesKey);

    Task TrainFromSeriesAsync(
        string seriesKey,
        IList<ForecastInput> historicalData,
        CancellationToken ct = default);

    Task<ForecastResult> ForecastFromSeriesAsync(
        string seriesKey,
        int horizonMonths,
        CancellationToken ct = default);
}
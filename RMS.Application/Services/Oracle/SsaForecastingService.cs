// RMS.Application/Services/Oracle/SsaForecastingService.cs
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle;

public sealed class SsaForecastingService : IForecastingService
{
    private readonly IForecastRepository _repo;
    private readonly IModelStore _store;
    private readonly ILogger<SsaForecastingService> _logger;

    private const int MinSeriesLength = 12;
    private const int MinWindowSize = 2;
    private const int SeasonalPeriod = 12;
    private const int TrainHorizon = 24;
    private const float ConfidenceLevel = 0.95f;
    private const string GlobalKey = "GLOBAL";

    public SsaForecastingService(
        IForecastRepository repo,
        IModelStore store,
        ILogger<SsaForecastingService> logger)
    {
        _repo = repo;
        _store = store;
        _logger = logger;
    }

    // ── TRAIN ─────────────────────────────────────────────────────────

    public async Task TrainGlobalAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Global model train başladı.");

        var data = (await _repo.GetAllAsync(ct))
                       .OrderBy(x => x.TimeIndex)
                       .ToList();

        if (data.Count < MinSeriesLength)
        {
            _logger.LogWarning("Global train üçün data azdır. Count={Count}", data.Count);
            return;
        }

        // ── Train/Test split ────────────────────────────────────────
        int testSize = Math.Max(1, Math.Min(6, data.Count / 5));
        var trainData = data.Take(data.Count - testSize).ToList();
        var testData = data.Skip(data.Count - testSize).ToList();

        await FitAndSaveAsync(GlobalKey, trainData, ct);

        // Real MAPE accuracy hesabla və saxla
        float accuracy = await ComputeRealAccuracyAsync(GlobalKey, testData, ct);
        await _store.SaveAccuracyAsync(GlobalKey, accuracy, ct);

        _logger.LogInformation(
            "Global model train tamamlandı. Count={Count}, Accuracy={Accuracy}%",
            data.Count, accuracy);
    }

    public async Task TrainAllAsync(CancellationToken ct = default)
    {
        await TrainGlobalAsync(ct);

        var combinations = await _repo.GetAllBankMccGroupsAsync(ct);
        foreach (var (bank, mcc) in combinations)
        {
            ct.ThrowIfCancellationRequested();
            await TrainAsync(bank, mcc, ct);
        }
    }

    public async Task TrainAsync(
        string bankName, string mccGroup, CancellationToken ct = default)
    {
        var series = (await _repo.GetByBankAndMccAsync(bankName, mccGroup, ct))
                         .OrderBy(x => x.TimeIndex)
                         .ToList();

        if (series.Count < MinSeriesLength)
        {
            _logger.LogWarning(
                "Train atlandı (data az): Bank={Bank}, Mcc={Mcc}, Count={Count}",
                bankName, mccGroup, series.Count);
            return;
        }

        // ── FIX: Train/Test split — son 6 ayı test üçün saxla ──────
        int testSize = Math.Max(1, Math.Min(6, series.Count / 5));
        var trainData = series.Take(series.Count - testSize).ToList();
        var testData = series.Skip(series.Count - testSize).ToList();

        var key = BankKey(bankName, mccGroup);

        // Yalnız train data ilə fit et
        await FitAndSaveAsync(key, trainData, ct);

        // Real MAPE accuracy hesabla
        float accuracy = await ComputeRealAccuracyAsync(key, testData, ct);

        _logger.LogInformation(
            "Model train edildi: Bank={Bank}, Mcc={Mcc}, Count={Count}, Accuracy={Accuracy}%",
            bankName, mccGroup, series.Count, accuracy);

        // Accuracy-ni saxla ki forecast zamanı istifadə edək
        await _store.SaveAccuracyAsync(key, accuracy, ct);
    }

    // ── FORECAST ──────────────────────────────────────────────────────

    public async Task<ForecastResult> ForecastAllAsync(
        int horizonMonths, CancellationToken ct = default)
    {
        if (!_store.Exists(GlobalKey))
        {
            _logger.LogWarning("Global model tapılmadı. Əvvəlcə /train/global çağırın.");
            return EmptyResult("GLOBAL", "ALL");
        }

        using var amountStream = await _store.LoadAsync(GlobalKey + "_AMOUNT", ct);
        using var countStream = await _store.LoadAsync(GlobalKey + "_COUNT", ct);

        if (amountStream is null || countStream is null)
        {
            _logger.LogWarning("Global model stream null.");
            return EmptyResult("GLOBAL", "ALL");
        }

        var amountForecast = Predict(amountStream);
        var countForecast = Predict(countStream);

        var baseMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        int safeHorizon = Math.Min(horizonMonths,
                              Math.Min(amountForecast.Forecast.Length,
                                       countForecast.Forecast.Length));

        var forecasts = Enumerable.Range(0, safeHorizon)
            .Select(i => new MonthlyForecast
            {
                Month = baseMonth.AddMonths(i),
                PredictedAmount = MathF.Max(0, amountForecast.Forecast[i]),
                AmountLowerBound = MathF.Max(0, amountForecast.LowerBound[i]),
                AmountUpperBound = MathF.Max(0, amountForecast.UpperBound[i]),
                PredictedCount = MathF.Max(0, countForecast.Forecast[i]),
                CountLowerBound = MathF.Max(0, countForecast.LowerBound[i]),
                CountUpperBound = MathF.Max(0, countForecast.UpperBound[i]),
            })
            .ToList();

        float accuracy = await _store.LoadAccuracyAsync(GlobalKey, ct);

        return new ForecastResult
        {
            BankName = "GLOBAL",
            MccGroup = "ALL",
            ModelUsed = "Global",
            ConfidenceLevel = ConfidenceLevel * 100f,
            Accuracy = accuracy,
            Forecasts = forecasts
        };
    }

    public async Task<ForecastResult> ForecastAsync(
        string bankName, string mccGroup, int horizonMonths,
        CancellationToken ct = default)
    {
        var bankKey = BankKey(bankName, mccGroup);

        if (_store.Exists(bankKey))
        {
            _logger.LogInformation(
                "Bank-specific model: Bank={Bank}, Mcc={Mcc}", bankName, mccGroup);
            var result = await PredictAsync(bankName, mccGroup, horizonMonths, bankKey, ct);
            result.ModelUsed = "Bank-Specific";
            return result;
        }

        if (_store.Exists(GlobalKey))
        {
            _logger.LogInformation(
                "Global model (fallback): Bank={Bank}, Mcc={Mcc}", bankName, mccGroup);
            var result = await PredictAsync(bankName, mccGroup, horizonMonths, GlobalKey, ct);
            result.ModelUsed = "Global";
            return result;
        }

        _logger.LogWarning(
            "Heç bir model tapılmadı: Bank={Bank}, Mcc={Mcc}.", bankName, mccGroup);

        return EmptyResult(bankName, mccGroup);
    }

    // ── CORE: Train → Amount + Count ──────────────────────────────────

    private async Task FitAndSaveAsync(
        string key, IList<ForecastInput> series, CancellationToken ct)
    {
        var mlContext = new MLContext(seed: 42);
        int seriesLength = series.Count;

        // ── FIX: windowSize — seriesLength/3 daha optimal ──────────
        int windowSize = Math.Max(MinWindowSize,
                             Math.Min(seriesLength / 3, SeasonalPeriod));

        _logger.LogInformation(
            "SSA train: Key={Key}, Len={Len}, Win={Win}",
            key, seriesLength, windowSize);

        await FitSingleAsync(mlContext, key + "_AMOUNT",
            series.Select(x => x.TotalAmount).ToList(),
            seriesLength, windowSize, ct);

        await FitSingleAsync(mlContext, key + "_COUNT",
            series.Select(x => x.TotalCount).ToList(),
            seriesLength, windowSize, ct);

        _logger.LogInformation("SSA train tamamlandı: Key={Key}", key);
    }

    private async Task FitSingleAsync(
        MLContext mlContext, string storeKey,
        IList<float> values, int seriesLength, int windowSize,
        CancellationToken ct)
    {
        var dataView = mlContext.Data.LoadFromEnumerable(
            values.Select(v => new SsaInput { Value = v }));

        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(SsaOutput.Forecast),
            inputColumnName: nameof(SsaInput.Value),
            windowSize: windowSize,
            seriesLength: seriesLength,
            trainSize: seriesLength,
            horizon: TrainHorizon,
            confidenceLevel: ConfidenceLevel,
            confidenceLowerBoundColumn: nameof(SsaOutput.LowerBound),
            confidenceUpperBoundColumn: nameof(SsaOutput.UpperBound));

        var model = pipeline.Fit(dataView);

        using var ms = new MemoryStream();
        mlContext.Model.Save(model, dataView.Schema, ms);
        ms.Position = 0;

        await _store.SaveAsync(storeKey, ms, ct);
    }

    // ── REAL MAPE ACCURACY ────────────────────────────────────────────

    /// <summary>
    /// Test datasını modeldən keçirib real MAPE hesablayır.
    /// Real dəyər vs proqnoz müqayisəsi — interval-based deyil.
    /// </summary>
    private async Task<float> ComputeRealAccuracyAsync(
        string key, IList<ForecastInput> testData, CancellationToken ct)
    {
        if (testData.Count == 0) return 0f;

        using var amountStream = await _store.LoadAsync(key + "_AMOUNT", ct);
        if (amountStream is null) return 0f;

        var forecast = Predict(amountStream);
        int compareCount = Math.Min(testData.Count, forecast.Forecast.Length);

        var mapeValues = Enumerable.Range(0, compareCount)
            .Where(i => MathF.Abs(testData[i].TotalAmount) > 1f) // sıfıra bölməni önlə
            .Select(i =>
            {
                float real = testData[i].TotalAmount;
                float predicted = MathF.Max(0, forecast.Forecast[i]);
                return MathF.Abs(real - predicted) / MathF.Abs(real);
            })
            .ToList();

        if (mapeValues.Count == 0) return 0f;

        float mape = mapeValues.Average();
        float accuracy = MathF.Max(0f, (1f - mape) * 100f);

        return MathF.Round(accuracy, 1);
    }

    // ── CORE: Predict ─────────────────────────────────────────────────

    private async Task<ForecastResult> PredictAsync(
        string bankName, string mccGroup, int horizonMonths,
        string key, CancellationToken ct)
    {
        using var amountStream = await _store.LoadAsync(key + "_AMOUNT", ct);
        using var countStream = await _store.LoadAsync(key + "_COUNT", ct);

        if (amountStream is null || countStream is null)
        {
            _logger.LogWarning("Model stream null: Key={Key}", key);
            return EmptyResult(bankName, mccGroup);
        }

        // ── FIX: həmişə bugünkü tarixdən başla ─────────────────────
        var baseMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);

        var amountForecast = Predict(amountStream);
        var countForecast = Predict(countStream);

        int safeHorizon = Math.Min(horizonMonths,
                              Math.Min(amountForecast.Forecast.Length,
                                       countForecast.Forecast.Length));

        var forecasts = Enumerable.Range(0, safeHorizon)
            .Select(i => new MonthlyForecast
            {
                Month = baseMonth.AddMonths(i),
                PredictedAmount = MathF.Max(0, amountForecast.Forecast[i]),
                AmountLowerBound = MathF.Max(0, amountForecast.LowerBound[i]),
                AmountUpperBound = MathF.Max(0, amountForecast.UpperBound[i]),
                PredictedCount = MathF.Max(0, countForecast.Forecast[i]),
                CountLowerBound = MathF.Max(0, countForecast.LowerBound[i]),
                CountUpperBound = MathF.Max(0, countForecast.UpperBound[i]),
            })
            .ToList();

        // Real accuracy-ni store-dan oxu (train zamanı saxlanılıb)
        float accuracy = await _store.LoadAccuracyAsync(key, ct);

        return new ForecastResult
        {
            BankName = bankName,
            MccGroup = mccGroup,
            ConfidenceLevel = ConfidenceLevel * 100f,
            Accuracy = accuracy,
            Forecasts = forecasts
        };
    }



    private static SsaOutput Predict(Stream modelStream)
    {
        var mlContext = new MLContext(seed: 42);
        var model = mlContext.Model.Load(modelStream, out _);
        var engine = model.CreateTimeSeriesEngine<SsaInput, SsaOutput>(mlContext);
        return engine.Predict();
    }

    private static ForecastResult EmptyResult(string bankName, string mccGroup) =>
        new() { BankName = bankName, MccGroup = mccGroup, Forecasts = [] };

    private static string BankKey(string bank, string mcc) => $"{bank}_{mcc}";

    private sealed class SsaInput { public float Value { get; set; } }

    private sealed class SsaOutput
    {
        public float[] Forecast { get; set; } = [];
        public float[] LowerBound { get; set; } = [];
        public float[] UpperBound { get; set; } = [];
    }
}
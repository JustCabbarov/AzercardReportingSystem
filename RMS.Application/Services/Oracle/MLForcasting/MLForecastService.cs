//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.ML;
//using Microsoft.ML.Transforms.TimeSeries;
//using RMS.Contract.Services.Oracle;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RMS.Application.Services.Oracle.MLForcasting
//{
//    public sealed class MLForecastService : IMLForecastService
//    {
//        // ── Asılılıqlar ────────────────────────────────────────────────────────
//        private readonly IForecastService _forecastService;
//        private readonly ModelStore _store;
//        private readonly MLForecastOptions _opts;
//        private readonly ILogger<MLForecastService> _logger;

//        // ── RAM Cache: key = "BANK::MCC::horizon" ──────────────────────────────
//        // ConcurrentDictionary — çox thread eyni anda oxuya bilər, yazma lock-lanır
//        private static readonly ConcurrentDictionary<string, CachedModel> _cache = new();

//        // ── Hər key üçün ayrı lock — fərqli bank/MCC cütləri bir-birini gözləmir
//        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

//        public MLForecastService(
//            IForecastService forecastService,
//            ModelStore store,
//            IOptions<MLForecastOptions> opts,
//            ILogger<MLForecastService> logger)
//        {
//            _forecastService = forecastService;
//            _store = store;
//            _opts = opts.Value;
//            _logger = logger;
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // ANA METOD — ForecastAsync
//        // ══════════════════════════════════════════════════════════════════════
//        public async Task<ForecastResult> ForecastAsync(
//            string bankName,
//            string mccGroup,
//            int horizon = 12,
//            CancellationToken ct = default)
//        {
//            if (horizon <= 0)
//                throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon müsbət olmalıdır.");

//            var cacheKey = BuildKey(bankName, mccGroup, horizon);
//            var modelPath = _store.GetModelPath(bankName, mccGroup);

//            // ── 1. RAM cache-də mövcuddursa və köhnəlməyibsə → birbaşa istifadə et
//            if (_cache.TryGetValue(cacheKey, out var cached) && !IsStale(cached))
//            {
//                _logger.LogDebug("RAM cache-dən götürüldü. Key={Key}", cacheKey);
//                return Predict(cached, bankName, mccGroup);
//            }

//            // ── 2. Eyni key üçün paralel train-i önlə (yalnız biri işləsin)
//            var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
//            await semaphore.WaitAsync(ct);

//            try
//            {
//                // Double-check: biz gözləyərkən başqa thread artıq train etmiş ola bilər
//                if (_cache.TryGetValue(cacheKey, out cached) && !IsStale(cached))
//                    return Predict(cached, bankName, mccGroup);

//                // ── 3. Disk-də mövcud model varmı?
//                var mlContext = new MLContext(seed: 42);
//                ITransformer? model = null;

//                if (!_store.IsStale(modelPath))
//                {
//                    model = _store.Load(mlContext, modelPath);
//                }

//                // ── 4. Model yoxdursa və ya köhnədirsə → yenidən öyrən
//                if (model is null)
//                {
//                    _logger.LogInformation(
//                        "Model öyrənilir. Bank={Bank}, MCC={Mcc}", bankName, mccGroup);

//                    model = await TrainAsync(mlContext, bankName, mccGroup, horizon, ct);
//                    _store.Save(mlContext, model, modelPath);
//                }

//                // ── 5. Engine yarat və RAM-a yaz
//                var engine = model.CreateTimeSeriesEngine<ForecastModelInput, ForecastModelOutput>(mlContext);

//                cached = new CachedModel
//                {
//                    FilePath = modelPath,
//                    TrainedAt = DateTime.UtcNow,
//                    Engine = engine,
//                    Horizon = horizon
//                };

//                _cache[cacheKey] = cached;
//                return Predict(cached, bankName, mccGroup);
//            }
//            finally
//            {
//                semaphore.Release();
//            }
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // BÜTÜN MCC QRUPLARI — paralel
//        // ══════════════════════════════════════════════════════════════════════
//        public async Task<IEnumerable<ForecastResult>> ForecastAllMccAsync(
//            string bankName,
//            int horizon = 12,
//            CancellationToken ct = default)
//        {
//            var mccGroups = (await _forecastService.GetMccGroupsAsync(bankName, ct)).ToList();

//            _logger.LogInformation(
//                "Toplu forecast başladı. Bank={Bank}, MCC sayı={Count}", bankName, mccGroups.Count);

//            var tasks = mccGroups.Select(async mcc =>
//            {
//                try { return await ForecastAsync(bankName, mcc, horizon, ct); }
//                catch (Exception ex)
//                {
//                    _logger.LogWarning("MCC={Mcc} atlandı: {Msg}", mcc, ex.Message);
//                    return null;
//                }
//            });

//            var results = await Task.WhenAll(tasks);
//            return results.Where(r => r is not null)!;
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // YENİDƏN ÖYRƏNMƏYİ ZORLA — köhnə cacheı sil, yenidən train et
//        // ══════════════════════════════════════════════════════════════════════
//        public async Task RetrainAsync(
//            string bankName,
//            string mccGroup,
//            int horizon = 12,
//            CancellationToken ct = default)
//        {
//            var cacheKey = BuildKey(bankName, mccGroup, horizon);
//            var modelPath = _store.GetModelPath(bankName, mccGroup);

//            _cache.TryRemove(cacheKey, out _);

//            var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
//            await semaphore.WaitAsync(ct);

//            try
//            {
//                _logger.LogInformation(
//                    "Yenidən öyrənmə başladı. Bank={Bank}, MCC={Mcc}", bankName, mccGroup);

//                var mlContext = new MLContext(seed: 42);
//                var model = await TrainAsync(mlContext, bankName, mccGroup, horizon, ct);

//                _store.Save(mlContext, model, modelPath);

//                var engine = model.CreateTimeSeriesEngine<ForecastModelInput, ForecastModelOutput>(mlContext);

//                _cache[cacheKey] = new CachedModel
//                {
//                    FilePath = modelPath,
//                    TrainedAt = DateTime.UtcNow,
//                    Engine = engine,
//                    Horizon = horizon
//                };

//                _logger.LogInformation(
//                    "Yenidən öyrənmə tamamlandı. Bank={Bank}, MCC={Mcc}", bankName, mccGroup);
//            }
//            finally
//            {
//                semaphore.Release();
//            }
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // KÖMƏKÇI — SSA öyrənməsi (thread pool-da işləyir, UI-ı bloklamır)
//        // ══════════════════════════════════════════════════════════════════════
//        private async Task<ITransformer> TrainAsync(
//            MLContext mlContext,
//            string bankName,
//            string mccGroup,
//            int horizon,
//            CancellationToken ct)
//        {
//            var series = (await _forecastService.GetTimeSeriesAsync(bankName, mccGroup, ct)).ToList();

//            int minRequired = _opts.WindowSize * 2;
//            if (series.Count < minRequired)
//                throw new InvalidOperationException(
//                    $"SSA üçün ən azı {minRequired} nöqtə lazımdır, {series.Count} var.");

//            var inputData = series.Select(x => new ForecastModelInput { Value = (float)x.Amount });
//            var dataView = mlContext.Data.LoadFromEnumerable(inputData);

//            var pipeline = mlContext.Forecasting.ForecastBySsa(
//                outputColumnName: nameof(ForecastModelOutput.ForecastedValues),
//                inputColumnName: nameof(ForecastModelInput.Value),
//                windowSize: _opts.WindowSize,
//                seriesLength: Math.Min(_opts.SeriesLength, series.Count),
//                trainSize: series.Count,
//                horizon: horizon,
//                confidenceLevel: _opts.ConfidenceLevel,
//                confidenceLowerBoundColumn: nameof(ForecastModelOutput.LowerBoundValues),
//                confidenceUpperBoundColumn: nameof(ForecastModelOutput.UpperBoundValues)
//            );

//            // ML.NET Fit() sinxrondur — böyük data üçün thread pool-da işləsin
//            return await Task.Run(() => pipeline.Fit(dataView), ct);
//        }

//        // ── Predict ────────────────────────────────────────────────────────────
//        private static ForecastResult Predict(CachedModel cached, string bankName, string mccGroup)
//        {
//            var engine = (TimeSeriesPredictionEngine<ForecastModelInput, ForecastModelOutput>)cached.Engine;
//            var output = engine.Predict();

//            return new ForecastResult
//            {
//                BankName = bankName,
//                MccGroup = mccGroup,
//                ForecastStartDate = DateTime.UtcNow,
//                Horizon = cached.Horizon,
//                Points = BuildPoints(output, cached.Horizon)
//            };
//        }

//        // ── Köhnəlik yoxlaması ─────────────────────────────────────────────────
//        private bool IsStale(CachedModel model) =>
//            DateTime.UtcNow - model.TrainedAt > _opts.ModelTtl;

//        // ── Cache key ──────────────────────────────────────────────────────────
//        private static string BuildKey(string bank, string mcc, int horizon) =>
//            $"{bank}::{mcc}::{horizon}";

//        // ── Proqnoz nöqtələri ──────────────────────────────────────────────────
//        private static List<ForecastPoint> BuildPoints(ForecastModelOutput output, int horizon)
//        {
//            var points = new List<ForecastPoint>(horizon);
//            for (int i = 0; i < horizon; i++)
//            {
//                float f = i < output.ForecastedValues.Length ? output.ForecastedValues[i] : 0f;
//                float l = i < output.LowerBoundValues.Length ? output.LowerBoundValues[i] : f;
//                float u = i < output.UpperBoundValues.Length ? output.UpperBoundValues[i] : f;

//                points.Add(new ForecastPoint
//                {
//                    PeriodIndex = i + 1,
//                    Forecast = MathF.Max(0f, f),
//                    LowerBound = MathF.Max(0f, l),
//                    UpperBound = MathF.Max(0f, u)
//                });
//            }
//            return points;
//        }
//    }
//}
 



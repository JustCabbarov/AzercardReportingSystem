//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.ML;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RMS.Application.Services.Oracle.MLForcasting
//{
//    public sealed class ModelStore
//    {
//        private readonly MLForecastOptions _opts;
//        private readonly ILogger<ModelStore> _logger;

//        public ModelStore(IOptions<MLForecastOptions> opts, ILogger<ModelStore> logger)
//        {
//            _opts = opts.Value;
//            _logger = logger;
//            Directory.CreateDirectory(_opts.ModelRootPath);
//        }

//        // ── Disk yolu ──────────────────────────────────────────────────────────
//        public string GetModelPath(string bankName, string mccGroup)
//        {
//            // fayl sistemi üçün təhlükəli simvolları sil
//            var safeBank = Sanitize(bankName);
//            var safeMcc = Sanitize(mccGroup);

//            var dir = Path.Combine(_opts.ModelRootPath, safeBank);
//            Directory.CreateDirectory(dir);
//            return Path.Combine(dir, $"{safeMcc}__model.zip");
//        }

//        // ── Modelin yaşı ───────────────────────────────────────────────────────
//        public bool IsStale(string modelPath)
//        {
//            if (!File.Exists(modelPath)) return true;

//            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(modelPath);
//            return age > _opts.ModelTtl;
//        }

//        // ── Diske yaz ──────────────────────────────────────────────────────────
//        public void Save(
//            MLContext mlContext,
//            ITransformer model,
//            string modelPath)
//        {
//            // Mövcud faylı .bak kimi yedəklə
//            if (File.Exists(modelPath))
//                File.Copy(modelPath, modelPath + ".bak", overwrite: true);

//            mlContext.Model.Save(model, inputSchema: null, filePath: modelPath);

//            _logger.LogInformation("Model saxlandı: {Path}", modelPath);
//        }

//        // ── Diskdən yüklə ──────────────────────────────────────────────────────
//        public ITransformer? Load(MLContext mlContext, string modelPath)
//        {
//            if (!File.Exists(modelPath))
//            {
//                _logger.LogDebug("Model faylı tapılmadı: {Path}", modelPath);
//                return null;
//            }

//            try
//            {
//                var model = mlContext.Model.Load(modelPath, out _);
//                _logger.LogInformation("Model diskdən yükləndi: {Path}", modelPath);
//                return model;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Model yüklənərkən xəta: {Path}", modelPath);

//                // Korlanmış fayl varsa yedəyi bərpa et
//                var backup = modelPath + ".bak";
//                if (File.Exists(backup))
//                {
//                    _logger.LogWarning("Yedək fayl bərpa edilir: {Bak}", backup);
//                    File.Copy(backup, modelPath, overwrite: true);
//                    return mlContext.Model.Load(modelPath, out _);
//                }

//                return null;
//            }
//        }

//        // ── Köməkçi ───────────────────────────────────────────────────────────
//        private static string Sanitize(string name) =>
//            string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
//    }
//}
//}

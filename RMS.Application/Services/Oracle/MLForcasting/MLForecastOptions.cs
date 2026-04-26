//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RMS.Application.Services.Oracle.MLForcasting
//{
//    public sealed class MLForecastOptions
//    {
//        public const string Section = "MLForecast";

//        /// <summary>Model .zip fayllarının saxlandığı kök qovluq.</summary>
//        public string ModelRootPath { get; set; } = Path.Combine(
//            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//            "RMS", "Models");

//        /// <summary>Modelin neçə saatdan sonra köhnə sayılıb yenidən öyrənilsin.</summary>
//        public int ModelTtlHours { get; set; } = 24;

//        /// <summary>Hesablanmış TTL TimeSpan-i.</summary>
//        public TimeSpan ModelTtl => TimeSpan.FromHours(ModelTtlHours);

//        // ── SSA parametrləri ───────────────────────────────────────────────────
//        public int WindowSize { get; set; } = 12;
//        public int SeriesLength { get; set; } = 36;
//        public float ConfidenceLevel { get; set; } = 0.95f;

//        /// <summary>
//        /// Fon tapşırığı üçün Cron ifadəsi (Hangfire / Quartz ilə istifadə üçün).
//        /// Default: hər gün saat 02:00.
//        /// </summary>
//        public string RetrainCronExpression { get; set; } = "0 2 * * *";
//    }
//}
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RMS.Application.Services.Oracle.MLForcasting
//{
//    public interface IMLForecastService
//    {
//        /// <summary>
//        /// Verilmiş bank + MCC qrupu üçün SSA modeli ilə proqnoz qurur.
//        /// Model RAM-da cache-lənir, diskdə saxlanılır.
//        /// </summary>
//        Task<ForecastResult> ForecastAsync(
//            string bankName,
//            string mccGroup,
//            int horizon = 12,
//            CancellationToken ct = default);

//        /// <summary>
//        /// Bankın bütün MCC qrupları üzrə paralel proqnoz qurur.
//        /// </summary>
//        Task<IEnumerable<ForecastResult>> ForecastAllMccAsync(
//            string bankName,
//            int horizon = 12,
//            CancellationToken ct = default);

//        /// <summary>
//        /// Mövcud modeli silib yenidən öyrənir.
//        /// Fon tapşırığından (BackgroundService / Hangfire) çağrılması tövsiyə olunur.
//        /// </summary>
//        Task RetrainAsync(
//            string bankName,
//            string mccGroup,
//            int horizon = 12,
//            CancellationToken ct = default);
//    }
//}


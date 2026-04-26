using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface IForecastService
    {
        /// <summary>Bütün banklar üzrə bütün forecast məlumatlarını qaytarır.</summary>
        Task<IEnumerable<ForecastInput>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Müəyyən bank üzrə forecast məlumatlarını qaytarır.</summary>
        Task<IEnumerable<ForecastInput>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Bank + MCC kombinasiyası üzrə sıralanmış zaman seriyasını qaytarır (ML.NET SSA üçün).</summary>
        Task<IEnumerable<ForecastInput>> GetTimeSeriesAsync(string bankName, string mccGroup, CancellationToken ct = default);

        /// <summary>Müəyyən tarix aralığı üzrə forecast məlumatlarını qaytarır.</summary>
        Task<IEnumerable<ForecastInput>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);

        /// <summary>Bankın mövcud olan bütün MCC qruplarını qaytarır.</summary>
        Task<IEnumerable<string>> GetMccGroupsAsync(string bankName, CancellationToken ct = default);
    }
}

using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface IAlertService
    {
        /// <summary>Bütün sətirləri qaytarır (alert olan və olmayan).</summary>
        Task<IEnumerable<AlertSignal>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə bütün sətirləri qaytarır.</summary>
        Task<IEnumerable<AlertSignal>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Yalnız |dəyişim| >= 10% olan siqnalları qaytarır.</summary>
        Task<IEnumerable<AlertSignal>> GetActiveAlertsAsync(string? bankName = null, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə siqnalları qaytarır.</summary>
        Task<IEnumerable<AlertSignal>> GetByMonthAsync(DateTime month, CancellationToken ct = default);

        /// <summary>Severity üzrə filter: "Critical" | "High" | "Medium".</summary>
        Task<IEnumerable<AlertSignal>> GetBySeverityAsync(string severity, CancellationToken ct = default);

        /// <summary>Spike (artım) olan siqnalları qaytarır.</summary>
        Task<IEnumerable<AlertSignal>> GetSpikesAsync(string? bankName = null, CancellationToken ct = default);

        /// <summary>Drop (azalma) olan siqnalları qaytarır.</summary>
        Task<IEnumerable<AlertSignal>> GetDropsAsync(string? bankName = null, CancellationToken ct = default);
    }
}

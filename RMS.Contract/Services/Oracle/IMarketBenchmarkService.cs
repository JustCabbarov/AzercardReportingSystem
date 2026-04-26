using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface IMarketBenchmarkService
    {
        /// <summary>Bütün benchmark məlumatlarını qaytarır.</summary>
        Task<IEnumerable<MarketBenchmark>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə benchmark məlumatlarını qaytarır.</summary>
        Task<IEnumerable<MarketBenchmark>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə benchmark məlumatlarını qaytarır.</summary>
        Task<IEnumerable<MarketBenchmark>> GetByMonthAsync(DateTime month, CancellationToken ct = default);

        /// <summary>Bankları həcmə görə sıralayır, BankRank təyin edir.</summary>
        Task<IEnumerable<MarketBenchmark>> GetRankedAsync(DateTime month, string? regionNameClean = null, CancellationToken ct = default);

        /// <summary>Müəyyən bankın bazar payını (%) qaytarır.</summary>
        Task<decimal> GetMarketShareAsync(string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>Müəyyən bankın kart payını (%) qaytarır.</summary>
        Task<decimal> GetCardShareAsync(string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>Bankın həmin aydakı sırasını qaytarır.</summary>
        Task<int> GetBankRankAsync(string bankName, DateTime month, CancellationToken ct = default);
    }
}

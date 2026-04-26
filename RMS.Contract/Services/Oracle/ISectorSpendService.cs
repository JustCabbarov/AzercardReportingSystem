using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface ISectorSpendService
    {
        /// <summary>Bütün sektor xərc məlumatlarını qaytarır.</summary>
        Task<IEnumerable<SectorSpend>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə sektor xərc məlumatlarını qaytarır.</summary>
        Task<IEnumerable<SectorSpend>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Bank + ay üzrə sektor xərc məlumatlarını qaytarır.</summary>
        Task<IEnumerable<SectorSpend>> GetByBankAndMonthAsync(string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>MCC qrupu üzrə filter.</summary>
        Task<IEnumerable<SectorSpend>> GetByMccGroupAsync(string mccGroup, CancellationToken ct = default);

        /// <summary>ShareOfWalletPct hesablanmış halda qaytarır.</summary>
        Task<IEnumerable<SectorSpend>> GetWithShareOfWalletAsync(string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>Bank üzrə ən yüksək xərc olan top N MCC qrupunu qaytarır.</summary>
        Task<IEnumerable<SectorSpend>> GetTopMccGroupsAsync(string bankName, DateTime month, int topN = 10, CancellationToken ct = default);

        /// <summary>Onlayn vs oflayn kanal payını hesablayır.</summary>
        Task<Dictionary<string, decimal>> GetChannelDistributionAsync(string bankName, DateTime month, CancellationToken ct = default);
    }

}

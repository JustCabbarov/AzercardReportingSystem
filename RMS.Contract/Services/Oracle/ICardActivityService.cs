using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface ICardActivityService
    {
        /// <summary>Bütün kart aktivlik məlumatlarını qaytarır.</summary>
        Task<IEnumerable<CardActivity>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə kart aktivlik məlumatlarını qaytarır.</summary>
        Task<IEnumerable<CardActivity>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə kart aktivlik məlumatlarını qaytarır.</summary>
        Task<IEnumerable<CardActivity>> GetByMonthAsync(DateTime month, CancellationToken ct = default);

        /// <summary>Məhsul tipi üzrə filter.</summary>
        Task<IEnumerable<CardActivity>> GetByProductTypeAsync(string productType, CancellationToken ct = default);

        /// <summary>Aktivlik seqmenti üzrə filter: "HighlyActive" | "ModeratelyActive" | "LowActive" | "Passive".</summary>
        Task<IEnumerable<CardActivity>> GetByActivitySegmentAsync(string bankName, string segment, CancellationToken ct = default);

        /// <summary>Bank üzrə ortalama contactless istifadə faizini hesablayır.</summary>
        Task<decimal> GetAvgContactlessRateAsync(string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>Bank üzrə seqment payını hesablayır (HighlyActive, Moderate və s. neçə %).</summary>
        Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(string bankName, DateTime month, CancellationToken ct = default);
    }

}

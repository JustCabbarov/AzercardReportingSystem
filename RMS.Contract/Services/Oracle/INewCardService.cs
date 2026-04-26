using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface INewCardService
    {
        /// <summary>Bütün yeni kart aktivlik məlumatlarını qaytarır.</summary>
        Task<IEnumerable<NewCardActivation>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə yeni kart aktivlik məlumatlarını qaytarır.</summary>
        Task<IEnumerable<NewCardActivation>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>İlk aktivasyon ayı üzrə filter.</summary>
        Task<IEnumerable<NewCardActivation>> GetByFirstMonthAsync(DateTime firstMonth, CancellationToken ct = default);

        /// <summary>Aktivasyon seqmenti üzrə filter: "EarlyActive" | "DelayedActive" | "SlowActive" | "Inactive".</summary>
        Task<IEnumerable<NewCardActivation>> GetBySegmentAsync(string bankName, string segment, CancellationToken ct = default);

        /// <summary>3 ayda heç istifadə edilməmiş kartları qaytarır.</summary>
        Task<IEnumerable<NewCardActivation>> GetInactiveCardsAsync(string bankName, CancellationToken ct = default);

        /// <summary>Bank üzrə ortalama 3 aylıq aktivasyon faizini hesablayır.</summary>
        Task<decimal> GetAvgActivationRateAsync(string bankName, CancellationToken ct = default);

        /// <summary>Seqment payı paylanmasını hesablayır (neçə % EarlyActive, Inactive və s.).</summary>
        Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(string bankName, CancellationToken ct = default);

        /// <summary>İlk istifadəyə qədər ortalama ay sayını hesablayır.</summary>
        Task<double> GetAvgMonthsToFirstUseAsync(string bankName, CancellationToken ct = default);
    }
}

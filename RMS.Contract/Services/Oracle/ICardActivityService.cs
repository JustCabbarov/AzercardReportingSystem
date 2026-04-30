
using RMS.Domain.Entities.Oracle;

namespace RMS.Contract.Services.Oracle
{
    public interface ICardActivityService
    {
        

        /// <summary>Bank üzrə kart aktivlik məlumatlarını qaytarır.</summary>
        Task<PagedResult<CardActivity>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə kart aktivlik məlumatlarını qaytarır.</summary>
        Task<PagedResult<CardActivity>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Məhsul tipi üzrə filter.</summary>
        Task<PagedResult<CardActivity>> GetByProductTypeAsync(
            string productType, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Aktivlik seqmenti üzrə filter: "HighlyActive" | "ModeratelyActive" | "LowActive" | "Passive".</summary>
        Task<PagedResult<CardActivity>> GetByActivitySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Universal filter.</summary>
        Task<PagedResult<CardActivity>> FilterAsync(
            CardActivity f, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Bank üzrə ortalama contactless istifadə faizini hesablayır.</summary>
        Task<decimal> GetAvgContactlessRateAsync(
            string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>Bank üzrə seqment payını hesablayır (HighlyActive, Moderate və s. neçə %).</summary>
        Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(
            string bankName, DateTime month, CancellationToken ct = default);
    }
}

using RMS.Domain.Entities.Oracle;

namespace RMS.Contract.Services.Oracle
{
    public interface IMarketBenchmarkService
    {
        /// <summary>Bütün benchmark məlumatlarını qaytarır.</summary>
        Task<PagedResult<MarketBenchmark>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Bank üzrə benchmark məlumatlarını qaytarır.</summary>
        Task<PagedResult<MarketBenchmark>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə benchmark məlumatlarını qaytarır.</summary>
        Task<PagedResult<MarketBenchmark>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default);

        /// <summary>Bankları həcmə görə sıralayır, BankRank təyin edir.</summary>
        Task<PagedResult<MarketBenchmark>> GetRankedAsync(
            DateTime month, PageRequest pageReq, string? regionNameClean = null, CancellationToken ct = default);

        /// <summary>Universal filter — bütün parametrlər opsionaldır.</summary>
        Task<PagedResult<MarketBenchmark>> FilterAsync(
            MarketBenchmark f, PageRequest pageReq, CancellationToken ct = default);

       
       

        Task<IEnumerable<MarketBenchmarkTrend>> GetTrendAsync(
    string bankName, CancellationToken ct = default);
    }
}
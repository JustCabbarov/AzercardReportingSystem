using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface IAzMapService
    {
        /// <summary>Bütün AZ xəritə tranzaksiyalarını qaytarır.</summary>
        Task<IEnumerable<AzMapTransaction>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə AZ xəritə tranzaksiyalarını qaytarır.</summary>
        Task<IEnumerable<AzMapTransaction>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə AZ xəritə tranzaksiyalarını qaytarır.</summary>
        Task<IEnumerable<AzMapTransaction>> GetByMonthAsync(DateTime month, CancellationToken ct = default);

        /// <summary>Region üzrə filter.</summary>
        Task<IEnumerable<AzMapTransaction>> GetByRegionAsync(string regionNameClean, CancellationToken ct = default);

        /// <summary>Terminal tipi üzrə filter (POS, ATM və s.).</summary>
        Task<IEnumerable<AzMapTransaction>> GetByDeviceTypeAsync(string deviceType, CancellationToken ct = default);

        /// <summary>Şəhər üzrə filter.</summary>
        Task<IEnumerable<AzMapTransaction>> GetByCityAsync(string cityClean, CancellationToken ct = default);

        /// <summary>Region üzrə ümumi məbləği hesablayır (xəritə üçün).</summary>
        Task<Dictionary<string, decimal>> GetAmountByRegionAsync(string bankName, DateTime month, CancellationToken ct = default);

        /// <summary>Terminal tipi üzrə cihaz sayını qaytarır.</summary>
        Task<Dictionary<string, long>> GetDeviceCountByTypeAsync(string bankName, DateTime month, CancellationToken ct = default);
    }

}

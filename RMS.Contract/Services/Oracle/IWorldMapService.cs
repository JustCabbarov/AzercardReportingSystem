using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface IWorldMapService
    {
        /// <summary>Bütün dünya tranzaksiyalarını qaytarır.</summary>
        Task<IEnumerable<WorldMapTransaction>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Bank üzrə dünya tranzaksiyalarını qaytarır.</summary>
        Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(string bankName, CancellationToken ct = default);

        /// <summary>Müəyyən ay üzrə dünya tranzaksiyalarını qaytarır.</summary>
        Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(DateTime month, CancellationToken ct = default);

        /// <summary>Mənbə ölkə üzrə filter.</summary>
        Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(string country, CancellationToken ct = default);

        /// <summary>Bankın issuing tranzaksiyalarını qaytarır (kart sahibi xaricdə xərcləyir).</summary>
        Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(string bankName, CancellationToken ct = default);

        /// <summary>Bankın acquiring tranzaksiyalarını qaytarır (xarici kart Azərbaycanda xərcləyir).</summary>
        Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(string bankName, CancellationToken ct = default);

        /// <summary>Ölkə üzrə ümumi məbləği hesablayır (xəritə üçün heat map dəyəri).</summary>
        Task<Dictionary<string, decimal>> GetAmountByCountryAsync(DateTime month, CancellationToken ct = default);
    }
}

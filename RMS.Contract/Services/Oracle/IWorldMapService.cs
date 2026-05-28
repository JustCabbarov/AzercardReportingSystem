using RMS.Contract.DTOs;
using RMS.Domain.Entities.Oracle;

namespace RMS.Contract.Services.Oracle
{
    public interface IWorldMapService
    {
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

        /// <summary>Ölkə üzrə ümumi məbləği və koordinatları qaytarır (xəritə üçün).</summary>
        Task<IEnumerable<CountryAmountDto>> GetAmountByCountryAsync(string? bankName, DateTime? month, CancellationToken ct = default);

        Task<PagedResult<WorldMapTransaction>> FilterAsync(
            WorldMapTransaction f,
            PageRequest pageReq,
            CancellationToken ct = default);
    }
}
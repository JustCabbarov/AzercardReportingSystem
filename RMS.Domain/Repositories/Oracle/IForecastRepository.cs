using RMS.Domain.Entities.Oracle;

namespace RMS.Domain.Repositories.Oracle;

public interface IForecastRepository
{
    Task<IEnumerable<ForecastInput>> GetAllAsync(
        CancellationToken ct = default);

    Task<IEnumerable<ForecastInput>> GetByBankAsync(
        string bankName,
        CancellationToken ct = default);

    Task<IEnumerable<ForecastInput>> GetByBankAndMccAsync(
        string bankName,
        string mccGroup,
        CancellationToken ct = default);

    Task<IEnumerable<ForecastInput>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    Task<IEnumerable<(string BankName, string MccGroup)>> GetAllBankMccGroupsAsync(
        CancellationToken ct = default);

    Task<IEnumerable<string>> GetMccGroupsAsync(
        string bankName,
        CancellationToken ct = default);

    Task<DateTime?> GetLastReportMonthAsync(
        string bankName,
        string mccGroup,
        CancellationToken ct = default);
}
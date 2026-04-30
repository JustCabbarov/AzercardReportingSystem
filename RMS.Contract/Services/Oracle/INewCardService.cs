using RMS.Domain.Entities.Oracle;

namespace RMS.Contract.Services.Oracle
{
    public interface INewCardService
    {
        Task<PagedResult<NewCardActivation>> GetAllAsync(
            PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<NewCardActivation>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<NewCardActivation>> GetByFirstMonthAsync(
            DateTime firstMonth, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<NewCardActivation>> GetBySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<NewCardActivation>> GetInactiveCardsAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<NewCardActivation>> FilterAsync(
            NewCardActivation f, PageRequest pageReq, CancellationToken ct = default);

        Task<IEnumerable<NewCardActivation>> GetTrendAsync(
            string bankName, CancellationToken ct = default);

        Task<decimal> GetAvgActivationRateAsync(
            string bankName, CancellationToken ct = default);

        Task<Dictionary<string, decimal>> GetSegmentDistributionAsync(
            string bankName, CancellationToken ct = default);
    }
}
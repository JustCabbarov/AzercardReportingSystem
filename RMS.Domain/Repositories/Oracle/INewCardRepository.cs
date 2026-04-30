using RMS.Domain.Entities.Oracle;

namespace RMS.Domain.Repositories.Oracle
{
    public interface INewCardRepository
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
    }
}
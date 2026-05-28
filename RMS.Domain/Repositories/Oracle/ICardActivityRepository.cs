
using RMS.Domain.Entities.Oracle;

namespace RMS.Domain.Repositories.Oracle
{
    public interface ICardActivityRepository
    {

        Task<PagedResult<CardActivity>> GetByBankAsync(
            string bankName, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<CardActivity>> GetByMonthAsync(
            DateTime month, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<CardActivity>> GetByProductTypeAsync(
            string productType, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<CardActivity>> GetByActivitySegmentAsync(
            string bankName, string segment, PageRequest pageReq, CancellationToken ct = default);

        Task<PagedResult<CardActivity>> FilterAsync(
            CardActivity f, PageRequest pageReq, CancellationToken ct = default);
    }
}
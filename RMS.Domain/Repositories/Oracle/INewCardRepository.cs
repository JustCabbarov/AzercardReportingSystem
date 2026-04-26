using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface INewCardRepository
    {
        Task<IEnumerable<NewCardActivation>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<NewCardActivation>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<NewCardActivation>> GetByFirstMonthAsync(DateTime firstMonth, CancellationToken ct = default);
        Task<IEnumerable<NewCardActivation>> GetBySegmentAsync(string bankName, string segment, CancellationToken ct = default);
        Task<IEnumerable<NewCardActivation>> GetInactiveCardsAsync(string bankName, CancellationToken ct = default);
    }
}

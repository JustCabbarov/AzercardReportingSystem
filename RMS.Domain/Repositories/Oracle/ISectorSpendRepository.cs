using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface ISectorSpendRepository
    {
        Task<IEnumerable<SectorSpend>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<SectorSpend>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<SectorSpend>> GetByBankAndMonthAsync(string bankName, DateTime month, CancellationToken ct = default);
        Task<IEnumerable<SectorSpend>> GetByMccGroupAsync(string mccGroup, CancellationToken ct = default);
        Task<IEnumerable<SectorSpend>> GetWithShareOfWalletAsync(string bankName, DateTime month, CancellationToken ct = default);
    }
}

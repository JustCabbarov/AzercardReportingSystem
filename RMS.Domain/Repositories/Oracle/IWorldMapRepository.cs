using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IWorldMapRepository
    {
        Task<IEnumerable<WorldMapTransaction>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(DateTime month, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(string country, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(string bankName, CancellationToken ct = default);
    }
}

using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IAzMapRepository
    {
        Task<IEnumerable<AzMapTransaction>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<AzMapTransaction>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<AzMapTransaction>> GetByMonthAsync(DateTime month, CancellationToken ct = default);
        Task<IEnumerable<AzMapTransaction>> GetByRegionAsync(string regionNameClean, CancellationToken ct = default);
        Task<IEnumerable<AzMapTransaction>> GetByDeviceTypeAsync(string deviceType, CancellationToken ct = default);
        Task<IEnumerable<AzMapTransaction>> GetByCityAsync(string cityClean, CancellationToken ct = default);
    }
}

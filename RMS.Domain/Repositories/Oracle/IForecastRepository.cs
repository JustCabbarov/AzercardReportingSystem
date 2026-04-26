using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IForecastRepository
    {
        Task<IEnumerable<ForecastInput>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<ForecastInput>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<ForecastInput>> GetByBankAndMccAsync(string bankName, string mccGroup, CancellationToken ct = default);
        Task<IEnumerable<ForecastInput>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }

}

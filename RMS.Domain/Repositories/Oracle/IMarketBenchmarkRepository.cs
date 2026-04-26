using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IMarketBenchmarkRepository
    {
        Task<IEnumerable<MarketBenchmark>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<MarketBenchmark>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<MarketBenchmark>> GetByMonthAsync(DateTime month, CancellationToken ct = default);
        Task<IEnumerable<MarketBenchmark>> GetRankedByMonthAsync(DateTime month, string? regionNameClean = null, CancellationToken ct = default);
    }

}

using RMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services
{
    public interface ISyncService
    {
        Task SyncTrendAsync();
        Task SyncPeriodComparisonAsync();
        Task SyncGlobalMapAsync();
        Task SyncAzerbaijanMapAsync();
        Task SyncCardActivityAsync();
        Task SyncBenchmarkAsync();
        Task SyncSectorSpendingAsync();
        Task SyncNewCardActivationAsync();
        Task<IEnumerable<SyncLog>> GetSyncLogsAsync();
        Task<SyncLog?> GetLastSyncLogAsync();
    }
}

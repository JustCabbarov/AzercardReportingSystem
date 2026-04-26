using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IAlertRepository
    {
        Task<IEnumerable<AlertSignal>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<AlertSignal>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<AlertSignal>> GetByMonthAsync(DateTime month, CancellationToken ct = default);
        Task<IEnumerable<AlertSignal>> GetAlertsOnlyAsync(string? bankName = null, CancellationToken ct = default);
        Task<IEnumerable<AlertSignal>> GetBySeverityAsync(string severity, CancellationToken ct = default);
    }
}

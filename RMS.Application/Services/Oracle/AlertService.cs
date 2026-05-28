using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.Oracle
{
    public class AlertService : IAlertService
    {
        private readonly IAlertRepository _repo;

        public AlertService(IAlertRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<AlertSignal>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<AlertSignal>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<AlertSignal>> GetActiveAlertsAsync(
            string? bankName = null, CancellationToken ct = default)
            => _repo.GetAlertsOnlyAsync(bankName, ct);

        public Task<IEnumerable<AlertSignal>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, ct);

        public Task<IEnumerable<AlertSignal>> GetBySeverityAsync(
            string severity, CancellationToken ct = default)
            => _repo.GetBySeverityAsync(severity, ct);

        /// <summary>
        /// SignalType == "Spike" — yəni əvvəlki aya nisbətən artım olan siqnallar.
        /// </summary>
        public async Task<IEnumerable<AlertSignal>> GetSpikesAsync(
            string? bankName = null, CancellationToken ct = default)
        {
            var alerts = await _repo.GetAlertsOnlyAsync(bankName, ct);
            return alerts.Where(a => a.SignalType == "Spike");
        }

        /// <summary>
        /// SignalType == "Drop" — yəni əvvəlki aya nisbətən azalma olan siqnallar.
        /// </summary>
        public async Task<IEnumerable<AlertSignal>> GetDropsAsync(
            string? bankName = null, CancellationToken ct = default)
        {
            var alerts = await _repo.GetAlertsOnlyAsync(bankName, ct);
            return alerts.Where(a => a.SignalType == "Drop");
        }
    }

}

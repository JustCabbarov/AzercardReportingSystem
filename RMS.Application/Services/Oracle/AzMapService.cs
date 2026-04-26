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
    public class AzMapService : IAzMapService
    {
        private readonly IAzMapRepository _repo;

        public AzMapService(IAzMapRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<AzMapTransaction>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<AzMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<AzMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, ct);

        public Task<IEnumerable<AzMapTransaction>> GetByRegionAsync(
            string regionNameClean, CancellationToken ct = default)
            => _repo.GetByRegionAsync(regionNameClean, ct);

        public Task<IEnumerable<AzMapTransaction>> GetByDeviceTypeAsync(
            string deviceType, CancellationToken ct = default)
            => _repo.GetByDeviceTypeAsync(deviceType, ct);

        public Task<IEnumerable<AzMapTransaction>> GetByCityAsync(
            string cityClean, CancellationToken ct = default)
            => _repo.GetByCityAsync(cityClean, ct);

        /// <summary>
        /// Azərbaycan xəritəsi üçün — hər regionun ümumi tranzaksiya məbləği.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetAmountByRegionAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = await _repo.GetByBankAsync(bankName, ct);

            return data
                .Where(x => x.ReportMonth.Year == month.Year
                         && x.ReportMonth.Month == month.Month
                         && !string.IsNullOrWhiteSpace(x.RegionNameClean))
                .GroupBy(x => x.RegionNameClean!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.TotalAmount));
        }

        /// <summary>
        /// Terminal tipi üzrə cihaz sayı — POS, ATM, mPOS və s.
        /// </summary>
        public async Task<Dictionary<string, long>> GetDeviceCountByTypeAsync(
            string bankName, DateTime month, CancellationToken ct = default)
        {
            var data = await _repo.GetByBankAsync(bankName, ct);

            return data
                .Where(x => x.ReportMonth.Year == month.Year
                         && x.ReportMonth.Month == month.Month
                         && !string.IsNullOrWhiteSpace(x.AcquiringDeviceType))
                .GroupBy(x => x.AcquiringDeviceType!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.DeviceCount ?? 0));
        }
    }
}

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
    public class ForecastService : IForecastService
    {
        private readonly IForecastRepository _repo;

        public ForecastService(IForecastRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<ForecastInput>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<ForecastInput>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        /// <summary>
        /// ML.NET SSA üçün — TIME_INDEX-ə görə sıralanmış, fasiləsiz zaman seriyası.
        /// </summary>
        public async Task<IEnumerable<ForecastInput>> GetTimeSeriesAsync(
            string bankName, string mccGroup, CancellationToken ct = default)
        {
            var data = await _repo.GetByBankAndMccAsync(bankName, mccGroup, ct);
            return data.OrderBy(x => x.TimeIndex);
        }

        public Task<IEnumerable<ForecastInput>> GetByDateRangeAsync(
            DateTime from, DateTime to, CancellationToken ct = default)
        {
            if (from > to)
                throw new ArgumentException("'from' tarixi 'to'-dan böyük ola bilməz.");

            return _repo.GetByDateRangeAsync(from, to, ct);
        }

        /// <summary>
        /// Bankın məlumat bazasında olan bütün unikal MCC qruplarını qaytarır.
        /// </summary>
        public async Task<IEnumerable<string>> GetMccGroupsAsync(
            string bankName, CancellationToken ct = default)
        {
            var data = await _repo.GetByBankAsync(bankName, ct);
            return data
                .Select(x => x.MccGroup)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x);
        }
    }
}

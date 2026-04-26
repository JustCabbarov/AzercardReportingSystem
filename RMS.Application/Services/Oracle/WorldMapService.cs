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
    public class WorldMapService : IWorldMapService
    {
        private readonly IWorldMapRepository _repo;

        public WorldMapService(IWorldMapRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<WorldMapTransaction>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetByBankAsync(bankName, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => _repo.GetByMonthAsync(month, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(
            string country, CancellationToken ct = default)
            => _repo.GetBySourceCountryAsync(country, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetIssuingAsync(bankName, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(
            string bankName, CancellationToken ct = default)
            => _repo.GetAcquiringAsync(bankName, ct);

        /// <summary>
        /// Xəritə üçün heat map dəyəri — hər ölkənin ümumi tranzaksiya məbləği.
        /// SOURCE_COUNTRY null olanlar nəzərə alınmır.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetAmountByCountryAsync(
            DateTime month, CancellationToken ct = default)
        {
            var data = await _repo.GetByMonthAsync(month, ct);

            return data
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceCountry))
                .GroupBy(x => x.SourceCountry!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.TotalAmount));
        }
    }
}

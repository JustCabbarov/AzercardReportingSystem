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
     string? bankName,
     DateTime? month,
     CancellationToken ct = default)
        {
            IEnumerable<WorldMapTransaction> data;

            if (bankName is not null && month is not null)
                data = await _repo.GetByBankAndMonthAsync(bankName, month.Value, ct);
            else if (bankName is not null)
                data = await _repo.GetByBankAsync(bankName, ct);
            else if (month is not null)
                data = await _repo.GetByMonthAsync(month.Value, ct);
            else
                data = await _repo.GetAllAsync(ct);

            return data
                .Where(x => !string.IsNullOrWhiteSpace(x.TargetCountry)) 
                .GroupBy(x => x.TargetCountry!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.TotalAmount));
        }
    }
}

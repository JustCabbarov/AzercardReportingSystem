using RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface ICardActivityRepository
    {
        Task<IEnumerable<CardActivity>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<CardActivity>> GetByBankAsync(string bankName, CancellationToken ct = default);
        Task<IEnumerable<CardActivity>> GetByMonthAsync(DateTime month, CancellationToken ct = default);
        Task<IEnumerable<CardActivity>> GetByProductTypeAsync(string productType, CancellationToken ct = default);
        Task<IEnumerable<CardActivity>> GetByActivitySegmentAsync(string bankName, string segment, CancellationToken ct = default);
    }
}

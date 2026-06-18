using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.SummaryTransaction;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.Oracle
{
    public class SummaryTransactionService : ISummaryTransactionService
    {
        private readonly ISummaryTransactionRepository _repo;

        public SummaryTransactionService(ISummaryTransactionRepository repo)
        {
            _repo = repo;
        }

        public async Task<SummaryTransactionResponse> GetSummaryAsync(SummaryFilterRequest filter)
        {
            return await _repo.GetSummaryAsync(filter);
        }
    }
}

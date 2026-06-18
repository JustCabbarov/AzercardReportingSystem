using RMS.Domain.Entities.Oracle.SummaryTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{

    public interface ISummaryTransactionService
    {
        Task<SummaryTransactionResponse> GetSummaryAsync(SummaryFilterRequest filter);
    }
}

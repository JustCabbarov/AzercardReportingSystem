using RMS.Domain.Entities.Oracle.TransactionDeteiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface ITransactionDetailedService
    {
        Task<TransactionSummaryResponse> GetSummaryAsync(TransactionFilterRequest filter);
        Task<List<BankDistributionDto>> GetTargetBanksAsync(TransactionFilterRequest filter);
        Task<List<DeviceDistributionDto>> GetAcquiringDevicesAsync(TransactionFilterRequest filter);
    }
}

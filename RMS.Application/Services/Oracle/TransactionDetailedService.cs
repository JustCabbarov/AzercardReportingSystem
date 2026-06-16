using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.TransactionDeteiled;
using RMS.Domain.Repositories;

namespace RMS.Application.Services.Oracle;

public class TransactionDetailedService : ITransactionDetailedService
{
    private readonly ITransactionDetailedRepository _repo;

    public TransactionDetailedService(ITransactionDetailedRepository repo)
    {
        _repo = repo;
    }

    public async Task<TransactionSummaryResponse> GetSummaryAsync(TransactionFilterRequest filter)
    {
        return await _repo.GetSummaryAsync(filter);
    }

    public async Task<List<BankDistributionDto>> GetTargetBanksAsync(TransactionFilterRequest filter)
    {
        return await _repo.GetTargetBanksAsync(filter);
    }

    public async Task<List<DeviceDistributionDto>> GetAcquiringDevicesAsync(TransactionFilterRequest filter)
    {
        return await _repo.GetAcquiringDevicesAsync(filter);
    }
}
using RMS.Contract.DTOs;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class AzMapService : IAzMapService
    {
        private readonly IAzMapRepository _repo;

        public AzMapService(IAzMapRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapFilterRequest f, PageRequest? pageReq = null, CancellationToken ct = default)
        {
            var transaction = new AzMapTransaction
            {
                BankName = f.BankName,
                SourceCity = f.SourceCity,
                SourceCityClean = f.SourceCityClean,
                SourceCityCategory = f.SourceCityCategory,
                AcquiringDeviceType = f.AcquiringDeviceType,
                TransGroup = f.TransGroup,
                OperationType = f.OperationType,
                TokenStatus = f.TokenStatus,
                Mcc = f.Mcc,
                IsAcquiringCategory = f.IsAcquiringCategory,
                TargetBankName = f.TargetBankName,
                SourceBankName = f.SourceBankName,
                TargetCity = f.TargetCity,
                PaymentSystem = f.PaymentSystem,
                ReportMonth = f.ReportMonth ?? default,
                StartDate = f.StartDate,
                EndDate = f.EndDate
            };

            return _repo.FilterAsync(transaction, pageReq, ct);
        }

        public async Task<IEnumerable<CityAmountDto>> GetHeatmapDataAsync(
            string? bankName, string? sourceCity, DateTime? month, CancellationToken ct = default)
        {
            var data = await _repo.GetHeatmapDataAsync(bankName, sourceCity, month, ct);

            return data
                .Where(x => x.Latitude.HasValue && x.Longitude.HasValue)
                .GroupBy(x => x.SourceCityClean ?? x.SourceCity ?? "")
                .Select(g => new CityAmountDto
                {
                    City = g.Key,
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    TotalCount = g.Sum(x => x.TotalCount),
                    Latitude = g.First().Latitude!.Value,
                    Longitude = g.First().Longitude!.Value
                });
        }

        public Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken ct = default)
            => _repo.GetDistinctCitiesAsync(ct);

        public Task<IEnumerable<string>> GetDistinctRegionsAsync(CancellationToken ct = default)
            => _repo.GetDistinctRegionsAsync(ct);

        public Task<IEnumerable<string>> GetDistinctBanksAsync(CancellationToken ct = default)
            => _repo.GetDistinctBanksAsync(ct);

        public async Task<AzMapFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default)
        {
            var acquiringDeviceTypes = await _repo.GetDistinctColumnValuesAsync("ACQUIRING_DEVICE_TYPE", false, ct);
            var transGroups = await _repo.GetDistinctColumnValuesAsync("TRANS_GROUP", false, ct);
            var operationTypes = await _repo.GetDistinctColumnValuesAsync("OPERATION_TYPE", false, ct);
            var tokenStatuses = await _repo.GetDistinctColumnValuesAsync("TOKEN_STATUS", false, ct);
            var mccs = await _repo.GetDistinctColumnValuesAsync("MCC", false, ct);
            var isAcquiringCategories = await _repo.GetDistinctColumnValuesAsync("IS_ACQUIRING_CATEGORY", false, ct);
            var targetBankNames = await _repo.GetDistinctColumnValuesAsync("TARGET_BANK_NAME", false, ct);
            var sourceBankNames = await _repo.GetDistinctColumnValuesAsync("SOURCE_BANK_NAME", false, ct);
            var targetCities = await _repo.GetDistinctColumnValuesAsync("TARGET_CITY", true, ct);
            var paymentSystems = await _repo.GetDistinctColumnValuesAsync("PAYMENT_SYSTEM", false, ct);

            var targetCityList = targetCities.ToList();
            if (targetCityList.Count == 0)
                targetCityList = (await _repo.GetDistinctCitiesAsync(ct)).ToList();

            var sourceBankList = sourceBankNames.ToList();
            if (sourceBankList.Count == 0)
                sourceBankList = (await _repo.GetDistinctBanksAsync(ct)).ToList();

            return new AzMapFilterOptionsDto
            {
                AcquiringDeviceTypes = acquiringDeviceTypes.ToList(),
                TransGroups = transGroups.ToList(),
                OperationTypes = operationTypes.ToList(),
                TokenStatuses = tokenStatuses.ToList(),
                Mccs = mccs.ToList(),
                IsAcquiringCategories = isAcquiringCategories.ToList(),
                TargetBankNames = targetBankList(targetBankNames),
                SourceBankNames = sourceBankList,
                TargetCities = targetCityList,
                PaymentSystems = paymentSystems.ToList(),
            };
        }

        private static List<string> targetBankList(IEnumerable<string> values) => values.ToList();
    }
}
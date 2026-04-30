using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class AzMapRepository : OracleRepositoryBase, IAzMapRepository
    {
        public AzMapRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_DATE           AS ReportDate,
            REPORT_MONTH          AS ReportMonth,
            BANK_NAME             AS BankName,
            REGION_NAME_CLEAN     AS RegionNameClean,
            SOURCE_CITY           AS SourceCity,
            SOURCE_CITY_CLEAN     AS SourceCityClean,
            SOURCE_CITY_CATEGORY  AS SourceCityCategory,
            ACQUIRING_DEVICE_TYPE AS AcquiringDeviceType,
            MCC_GROUP             AS MccGroup,
            RETAIL_CATEGORY       AS RetailCategory,
            TRANSACTION_CLASS     AS TransactionClass,
            CTLS_STATUS           AS CtlsStatus,
            TOTAL_AMOUNT          AS TotalAmount,
            TOTAL_COUNT           AS TotalCount,
            DEVICE_COUNT          AS DeviceCount
        FROM ALI_JABBAROV.PG_MV_REQ4_AZ_MAP
        """;

        public async Task<IEnumerable<AzMapTransaction>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<AzMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE BANK_NAME = :BankName
             ORDER BY REPORT_MONTH DESC, REGION_NAME_CLEAN
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<AzMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE TRUNC(REPORT_MONTH, 'MM') = TRUNC(:Month, 'MM')
             ORDER BY BANK_NAME, REGION_NAME_CLEAN
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, new { Month = month.Date }, cancellationToken: ct));
        }

        public async Task<IEnumerable<AzMapTransaction>> GetByRegionAsync(
            string regionNameClean, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE REGION_NAME_CLEAN = :Region
             ORDER BY REPORT_MONTH DESC, BANK_NAME
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, new { Region = regionNameClean }, cancellationToken: ct));
        }

        public async Task<IEnumerable<AzMapTransaction>> GetByDeviceTypeAsync(
            string deviceType, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE ACQUIRING_DEVICE_TYPE = :DeviceType
             ORDER BY REPORT_MONTH DESC
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, new { DeviceType = deviceType }, cancellationToken: ct));
        }

        public async Task<IEnumerable<AzMapTransaction>> GetByCityAsync(
            string cityClean, CancellationToken ct = default)
        {
            var sql = SelectColumns + """
             WHERE SOURCE_CITY_CLEAN = :City
             ORDER BY REPORT_MONTH DESC
            """;
            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, new { City = cityClean }, cancellationToken: ct));
        }
    }
}
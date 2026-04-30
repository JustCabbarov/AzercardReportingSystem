using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class WorldMapRepository : OracleRepositoryBase, IWorldMapRepository
    {
        public WorldMapRepository(IConfiguration configuration) : base(configuration) { }

        private async Task<IEnumerable<WorldMapTransaction>> QueryAggregatedAsync(
            string? where, object? param, CancellationToken ct)
        {
            var sql = $"""
            SELECT
                INITCAP(SOURCE_COUNTRY)         AS SourceCountry,
                SOURCE_COUNTRY_CATEGORY         AS SourceCountryCategory,
                INITCAP(TARGET_COUNTRY)         AS TargetCountry,
                TARGET_COUNTRY_CATEGORY         AS TargetCountryCategory,
                PAYMENT_SYSTEM                  AS PaymentSystem,
                MAX(IS_ISSUING)                 AS IsIssuing,
                MAX(IS_ACQUIRING)               AS IsAcquiring,
                SUM(TOTAL_AMOUNT)               AS TotalAmount,
                SUM(TOTAL_COUNT)                AS TotalCount
            FROM ALI_JABBAROV.PG_MV_REQ3_WORLD_MAP
            {(where is null ? "" : $"WHERE {where}")}
            GROUP BY
                INITCAP(SOURCE_COUNTRY),
                SOURCE_COUNTRY_CATEGORY,
                INITCAP(TARGET_COUNTRY),
                TARGET_COUNTRY_CATEGORY,
                PAYMENT_SYSTEM
            ORDER BY SUM(TOTAL_AMOUNT) DESC
            """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, param, cancellationToken: ct));
        }

        private async Task<IEnumerable<WorldMapTransaction>> QueryRawAsync(
            string? where, object? param, CancellationToken ct)
        {
            var sql = $"""
            SELECT
                REPORT_MONTH                    AS ReportMonth,
                BANK_NAME                       AS BankName,
                INITCAP(SOURCE_COUNTRY)         AS SourceCountry,
                SOURCE_COUNTRY_CATEGORY         AS SourceCountryCategory,
                INITCAP(TARGET_COUNTRY)         AS TargetCountry,
                TARGET_COUNTRY_CATEGORY         AS TargetCountryCategory,
                PAYMENT_SYSTEM                  AS PaymentSystem,
                IS_ISSUING                      AS IsIssuing,
                IS_ACQUIRING                    AS IsAcquiring,
                TOTAL_AMOUNT                    AS TotalAmount,
                TOTAL_COUNT                     AS TotalCount
            FROM ALI_JABBAROV.PG_MV_REQ3_WORLD_MAP
            {(where is null ? "" : $"WHERE {where}")}
            ORDER BY REPORT_MONTH DESC
            """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, param, cancellationToken: ct));
        }

        public Task<IEnumerable<WorldMapTransaction>> GetAllAsync(
            CancellationToken ct = default)
            => QueryAggregatedAsync(null, null, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync("BANK_NAME = :BankName", new { BankName = bankName }, ct);

        public Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
            => QueryRawAsync(
                "TRUNC(REPORT_MONTH, 'MM') = TRUNC(:Month, 'MM')",
                new { Month = month.Date },
                ct);

        public Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(
     string country, CancellationToken ct = default)
     => QueryRawAsync(
         "UPPER(SOURCE_COUNTRY) = UPPER(:Country)",
         new { Country = country },
         ct);
        public Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "BANK_NAME = :BankName AND IS_ACQUIRING = 1",  
                new { BankName = bankName },
                ct);

        
        public Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(
            string bankName, CancellationToken ct = default)
            => QueryRawAsync(
                "BANK_NAME = :BankName AND IS_ISSUING = 1",    
                new { BankName = bankName },
                ct);


        public Task<IEnumerable<WorldMapTransaction>> GetByBankAndMonthAsync(
    string bankName, DateTime month, CancellationToken ct = default)
    => QueryRawAsync(
        "BANK_NAME = :BankName AND TRUNC(REPORT_MONTH, 'MM') = TRUNC(:Month, 'MM')",
        new { BankName = bankName, Month = month.Date },
        ct);
    }
}
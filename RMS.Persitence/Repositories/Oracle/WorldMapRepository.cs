using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.Oracle
{
    public class WorldMapRepository : OracleRepositoryBase, IWorldMapRepository
    {
        public WorldMapRepository(IConfiguration configuration) : base(configuration) { }

        private const string SelectColumns = """
        SELECT
            REPORT_MONTH            AS ReportMonth,
            BANK_NAME               AS BankName,
            SOURCE_COUNTRY          AS SourceCountry,
            SOURCE_COUNTRY_CATEGORY AS SourceCountryCategory,
            TARGET_COUNTRY          AS TargetCountry,
            TARGET_COUNTRY_CATEGORY AS TargetCountryCategory,
            PAYMENT_SYSTEM          AS PaymentSystem,
            IS_ISSUING              AS IsIssuing,
            IS_ACQUIRING            AS IsAcquiring,
            TOTAL_AMOUNT            AS TotalAmount,
            TOTAL_COUNT             AS TotalCount
        FROM MV_REQ3_WORLD_MAP
        """;

        public async Task<IEnumerable<WorldMapTransaction>> GetAllAsync(CancellationToken ct = default)
        {
            var sql = SelectColumns + " ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<IEnumerable<WorldMapTransaction>> GetByBankAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<WorldMapTransaction>> GetByMonthAsync(
            DateTime month, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE TRUNC(REPORT_MONTH,'MM') = TRUNC(:Month,'MM') ORDER BY BANK_NAME";
            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, new { Month = month.Date }, cancellationToken: ct));
        }

        public async Task<IEnumerable<WorldMapTransaction>> GetBySourceCountryAsync(
            string country, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE SOURCE_COUNTRY = :Country ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, new { Country = country }, cancellationToken: ct));
        }

        public async Task<IEnumerable<WorldMapTransaction>> GetIssuingAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName AND IS_ISSUING = 1 ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }

        public async Task<IEnumerable<WorldMapTransaction>> GetAcquiringAsync(
            string bankName, CancellationToken ct = default)
        {
            var sql = SelectColumns + " WHERE BANK_NAME = :BankName AND IS_ACQUIRING = 1 ORDER BY REPORT_MONTH DESC";
            using var conn = CreateConnection();
            return await conn.QueryAsync<WorldMapTransaction>(
                new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
        }
    }

}

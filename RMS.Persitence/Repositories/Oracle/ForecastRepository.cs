using Dapper;
using Microsoft.Extensions.Configuration;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using RMS.Persitence.Repositories.Oracle;

namespace RMS.Persistence.Repositories.Oracle;

public sealed class ForecastRepository : OracleRepositoryBase, IForecastRepository
{
    private const string SelectSql = """
        SELECT
            REPORT_MONTH   AS ReportMonth,
            BANK_NAME      AS BankName,
            MCC_GROUP      AS MccGroup,
            TOTAL_AMOUNT   AS TotalAmount,
            TOTAL_COUNT    AS TotalCount,
            TOTAL_CARDS    AS TotalCards,
            AMOUNT_LAG1    AS AmountLag1,
            AMOUNT_LAG3    AS AmountLag3,
            AMOUNT_LAG12   AS AmountLag12,
            ROLLING_AVG_3M AS RollingAvg3M,
            ROLLING_AVG_6M AS RollingAvg6M,
            SEASON_MONTH   AS SeasonMonth,
            TIME_INDEX     AS TimeIndex
        FROM pg_mv_req1_forecast_input
        """;

    public ForecastRepository(IConfiguration configuration) : base(configuration) { }

    public async Task<IEnumerable<ForecastInput>> GetAllAsync(
        CancellationToken ct = default)
    {
        var sql = SelectSql + " ORDER BY BANK_NAME, MCC_GROUP, REPORT_MONTH";
        using var conn = CreateConnection();
        return await conn.QueryAsync<ForecastInput>(
            new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<IEnumerable<ForecastInput>> GetByBankAsync(
        string bankName, CancellationToken ct = default)
    {
        var sql = SelectSql + """
             WHERE BANK_NAME = @BankName
             ORDER BY MCC_GROUP, REPORT_MONTH
            """;
        using var conn = CreateConnection();
        return await conn.QueryAsync<ForecastInput>(
            new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
    }

    public async Task<IEnumerable<ForecastInput>> GetByBankAndMccAsync(
        string bankName, string mccGroup, CancellationToken ct = default)
    {
        var sql = SelectSql + """
             WHERE BANK_NAME = @BankName
               AND MCC_GROUP = @MccGroup
             ORDER BY REPORT_MONTH
            """;
        using var conn = CreateConnection();
        return await conn.QueryAsync<ForecastInput>(
            new CommandDefinition(sql,
                new { BankName = bankName, MccGroup = mccGroup },
                cancellationToken: ct));
    }

    public async Task<IEnumerable<ForecastInput>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var sql = SelectSql + """
             WHERE REPORT_MONTH >= @From
               AND REPORT_MONTH <= @To
             ORDER BY BANK_NAME, MCC_GROUP, REPORT_MONTH
            """;
        using var conn = CreateConnection();
        return await conn.QueryAsync<ForecastInput>(
            new CommandDefinition(sql,
                new { From = from.Date, To = to.Date },
                cancellationToken: ct));
    }

    public async Task<IEnumerable<(string BankName, string MccGroup)>> GetAllBankMccGroupsAsync(
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT
                BANK_NAME AS BankName,
                MCC_GROUP AS MccGroup
            FROM pg_mv_req1_forecast_input
            ORDER BY BANK_NAME, MCC_GROUP
            """;

        using var conn = CreateConnection();
        var rows = await conn.QueryAsync<BankMccRow>(
            new CommandDefinition(sql, cancellationToken: ct));

        return rows.Select(r => (r.BankName, r.MccGroup));
    }

    public async Task<IEnumerable<string>> GetMccGroupsAsync(
        string bankName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT MCC_GROUP
            FROM pg_mv_req1_forecast_input
            WHERE BANK_NAME = @BankName
            ORDER BY MCC_GROUP
            """;
        using var conn = CreateConnection();
        return await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { BankName = bankName }, cancellationToken: ct));
    }

    public async Task<DateTime?> GetLastReportMonthAsync(
        string bankName, string mccGroup, CancellationToken ct = default)
    {
        const string sql = """
            SELECT MAX(REPORT_MONTH)
            FROM pg_mv_req1_forecast_input
            WHERE BANK_NAME = @BankName
              AND MCC_GROUP = @MccGroup
            """;
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<DateTime?>(
            new CommandDefinition(sql,
                new { BankName = bankName, MccGroup = mccGroup },
                cancellationToken: ct));
    }

    private sealed record BankMccRow(string BankName, string MccGroup);
}
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RMS.Domain.Entities.Oracle.CardTransaction;
using RMS.Domain.Repositories.Oracle;


namespace RMS.Persitence.Repositories.Oracle;

public class CardDashboardRepository : ICardDashboardRepository
{
    private readonly string _connStr;

    public CardDashboardRepository(IConfiguration config)
    {
        _connStr = config.GetConnectionString("PostgreSqlConnection")!;
    }

    private NpgsqlConnection Connection => new NpgsqlConnection(_connStr);

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private static string PeriodColumn(DateGrouping g) => g switch
    {
        DateGrouping.Quarterly => "REPORT_QUARTER",
        DateGrouping.Yearly => "REPORT_YEAR",
        _ => "REPORT_MONTH"
    };

    private static decimal Round(decimal v, RoundTo r) => r switch
    {
        RoundTo.Thousands => Math.Round(v / 1_000M, 2),
        RoundTo.Millions => Math.Round(v / 1_000_000M, 2),
        RoundTo.Billions => Math.Round(v / 1_000_000_000M, 2),
        _ => Math.Round(v, 2)
    };

    private static decimal? Pct(decimal cur, decimal? prev) =>
        prev is null or 0 ? null
        : Math.Round((cur - prev.Value) / Math.Abs(prev.Value) * 100, 2);

    private static FilterBuilder BuildFilter(DashboardFilterRequest f)
    {
        var col = PeriodColumn(f.DateGrouping);

        return new FilterBuilder()
            .AddRange(col, "DateFrom", "DateTo", f.DateFrom, f.DateTo)
            .AddString("PAYMENT_SYSTEM = :PS", "PS", f.PaymentSystem)
            .AddString("IS_ISSUING_CATEGORY = :PT", "PT", f.PaymentType)
            .AddString("OPERATION_TYPE = :OT", "OT", f.OperationType)
            .AddString("CARD_PRODUCT_TYPE_CATEGORY = :CT", "CT", f.CardTypeCategory)
            .AddString("TOKEN_STATUS = :TS", "TS", f.TokenStatus)
            .AddString("TRANS_GROUP = :TG", "TG", f.TransGroup)
            .AddString("TARGET_BANK_NAME = :TB", "TB", f.TargetBankName)
            .AddBool("IS_CONTACTLESS", "IC", f.IsContactless);
    }

    // ── 1. KPI SUMMARY  (Widget 1-4) ─────────────────────────────────────────

    public async Task<KpiSummaryResponse> GetKpiSummaryAsync(DashboardFilterRequest filter)
    {
        var fb = BuildFilter(filter);

        var sql = $@"
            SELECT
                SUM(TOTAL_AMOUNT)   AS TotalAmount,
                SUM(TOTAL_TX_COUNT) AS TotalTxCount,
                MAX(MAX_AMOUNT)     AS MaxAmount,
                CASE WHEN SUM(TOTAL_TX_COUNT) > 0
                     THEN ROUND(SUM(TOTAL_AMOUNT) / SUM(TOTAL_TX_COUNT), 2)
                     ELSE 0 END     AS AvgAmount
            FROM MV_CARD_DASHBOARD
            {fb.WhereClause}";

        using var con = Connection;
        var r = await con.QuerySingleOrDefaultAsync<dynamic>(sql, fb.Parameters);
        if (r is null) return new();

        return new KpiSummaryResponse
        {
            TotalAmount = Round((decimal)(r.totalamount ?? 0m), filter.RoundTo),
            TotalTxCount = (long)(r.totaltxcount ?? 0L),
            MaxAmount = Round((decimal)(r.maxamount ?? 0m), filter.RoundTo),
            AvgAmount = Round((decimal)(r.avgamount ?? 0m), filter.RoundTo),
        };
    }

    // ── 2. TREND  (Widget 5) ──────────────────────────────────────────────────

    public async Task<List<TrendPoint>> GetTrendAsync(DashboardFilterRequest filter)
    {
        var fb = BuildFilter(filter);
        var col = PeriodColumn(filter.DateGrouping);

        var sql = $@"
            SELECT
                {col}                                           AS PeriodDate,
                SUM(TOTAL_AMOUNT)                               AS TotalAmount,
                SUM(TOTAL_TX_COUNT)                             AS TotalTxCount,
                LAG(SUM(TOTAL_AMOUNT))   OVER (ORDER BY {col}) AS PrevAmount,
                LAG(SUM(TOTAL_TX_COUNT)) OVER (ORDER BY {col}) AS PrevTxCount
            FROM MV_CARD_DASHBOARD
            {fb.WhereClause}
            GROUP BY {col}
            ORDER BY {col}";

        using var con = Connection;
        var rows = await con.QueryAsync<dynamic>(sql, fb.Parameters);

        return rows.Select(r =>
        {
            decimal total = (decimal)(r.totalamount ?? 0m);
            decimal totalTx = (decimal)(r.totaltxcount ?? 0m);
            decimal? prev = r.prevamount == null ? null : (decimal?)r.prevamount;
            decimal? prevTx = r.prevtxcount == null ? null : (decimal?)r.prevtxcount;
            DateTime dt = (DateTime)r.perioddate;

            return new TrendPoint
            {
                PeriodDate = dt,
                PeriodLabel = filter.DateGrouping switch
                {
                    DateGrouping.Quarterly => $"Q{(dt.Month - 1) / 3 + 1}/{dt.Year}",
                    DateGrouping.Yearly => dt.ToString("yyyy"),
                    _ => dt.ToString("MM/yyyy")
                },
                TotalAmount = Round(total, filter.RoundTo),
                TotalTxCount = (long)totalTx,
                PrevAmount = prev == null ? null : Round(prev.Value, filter.RoundTo),
                PrevTxCount = prevTx,
                AmountChangePct = Pct(total, prev),
                TxChangePct = Pct(totalTx, prevTx),
            };
        }).ToList();
    }

    // ── 3-6. BREAKDOWN  (Widget 6,7,8,9) ─────────────────────────────────────

    public async Task<BreakdownResponse> GetBreakdownAsync(
        DashboardFilterRequest filter, BreakdownType type)
    {
        var categoryCol = type switch
        {
            BreakdownType.ProductType => "CARD_PRODUCT_TYPE_CATEGORY",
            BreakdownType.PaymentSystem => "PAYMENT_SYSTEM",
            BreakdownType.PaymentType => "IS_ISSUING_CATEGORY",
            BreakdownType.CashNonCash => "OPERATION_TYPE",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        var fb = BuildFilter(filter);

        var sql = $@"
            SELECT
                {categoryCol}                                               AS Category,
                SUM(TOTAL_AMOUNT)                                           AS TotalAmount,
                SUM(TOTAL_TX_COUNT)                                         AS TotalTxCount,
                ROUND(
                    SUM(TOTAL_AMOUNT) * 100.0
                    / NULLIF(SUM(SUM(TOTAL_AMOUNT)) OVER (), 0), 2)         AS AmountPct,
                ROUND(
                    SUM(TOTAL_TX_COUNT) * 100.0
                    / NULLIF(SUM(SUM(TOTAL_TX_COUNT)) OVER (), 0), 2)       AS TxCountPct
            FROM MV_CARD_DASHBOARD
            {fb.WhereClause}
            GROUP BY {categoryCol}
            ORDER BY TotalAmount DESC";

        using var con = Connection;
        var rows = await con.QueryAsync<dynamic>(sql, fb.Parameters);

        return new BreakdownResponse
        {
            GroupBy = categoryCol,
            Items = rows.Select(r => new BreakdownItem
            {
                Category = (string)(r.category ?? ""),
                TotalAmount = Round((decimal)(r.totalamount ?? 0m), filter.RoundTo),
                TotalTxCount = (long)(r.totaltxcount ?? 0L),
                AmountPct = (decimal)(r.amountpct ?? 0m),
                TxCountPct = (decimal)(r.txcountpct ?? 0m),
            }).ToList()
        };
    }

    // ── FILTER LOOKUPS ───────────────────────────────────────────────────────

    public async Task<FilterLookupResponse> GetFilterLookupsAsync()
    {
        using var con = Connection;

        var q = async (string col) => (await con.QueryAsync<string>(
            $"SELECT DISTINCT {col} FROM MV_CARD_DASHBOARD WHERE {col} IS NOT NULL ORDER BY 1"
        )).ToList();

        return new FilterLookupResponse
        {
            PaymentSystems = await q("PAYMENT_SYSTEM"),
            PaymentTypes = await q("IS_ISSUING_CATEGORY"),
            OperationTypes = await q("OPERATION_TYPE"),
            CardTypeCategories = await q("CARD_PRODUCT_TYPE_CATEGORY"),
            TokenStatuses = await q("TOKEN_STATUS"),
            TransGroups = await q("TRANS_GROUP"),
            TargetBankNames = await q("TARGET_BANK_NAME"),
        };
    }
}
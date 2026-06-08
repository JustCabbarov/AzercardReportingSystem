using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;
using RMS.Persitence.Repositories.Oracle;

namespace RMS.Persistence.Repositories.Cards
{
    public class CardPortfolioRepository : ICardPortfolioRepository
    {
        private readonly string _connStr;

        public CardPortfolioRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("PostgreSqlConnection")!;
        }

        private NpgsqlConnection Connect() => new(_connStr);

        private static readonly HashSet<string> AllowedDimensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "bank_name", "region_name", "payment_scheme", "base_currency_name",
            "product_type", "card_product_name", "status_3d", "contactless_status",
            "exp_status", "card_category_name", "card_commercial_name", "card_brand_name"
        };

        private static string SafeDim(string dim) =>
            AllowedDimensions.Contains(dim)
                ? dim.ToLower()
                : throw new ArgumentException($"Invalid dimension: {dim}");

        private static FilterBuilder BuildCommonFilter(CardPortfolioFilter f, string alias = "m")
     => new FilterBuilder()
         .AddList($"{alias}.bank_name", "BankName", f.BankNames)
         .AddList($"{alias}.region_name", "RegionName", f.RegionNames)
         .AddList($"{alias}.payment_scheme", "PaymentScheme", f.PaymentSchemes)
         .AddList($"{alias}.product_type", "ProductType", f.ProductTypes)
         .AddList($"{alias}.contactless_status", "Contactless", f.ContactlessStatuses)
         .AddList($"{alias}.exp_status", "ExpStatus", f.ExpStatuses)
         .AddList($"{alias}.status_3d", "Status3d", f.Status3Ds)
         .AddList($"{alias}.base_currency_name", "Currency", f.BaseCurrencies)
         .AddList($"{alias}.card_product_name", "CardProduct", f.CardProductNames)
         .AddRange($"{alias}.report_month", "FromMonth", "ToMonth", f.FromMonth, f.ToMonth);

        private async Task<PagedResult<T>> QueryPagedAsync<T>(
            string baseSql,
            string orderBy,
            PageRequest pageReq,
            object? parameters,
            CancellationToken ct = default)
        {
            var countSql = $"SELECT COUNT(*) FROM ({baseSql}) _cnt";
            var dataSql = $"""
                {baseSql}
                ORDER BY {orderBy}
                LIMIT @PageSize OFFSET @Offset
                """;

            var dynParams = new DynamicParameters(parameters);
            dynParams.Add("PageSize", pageReq.PageSize);
            dynParams.Add("Offset", (pageReq.Page - 1) * pageReq.PageSize);

            using var db = Connect();
            var total = await db.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: ct));
            var items = await db.QueryAsync<T>(
                new CommandDefinition(dataSql, dynParams, cancellationToken: ct));

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = total,
                Page = pageReq.Page,
                PageSize = pageReq.PageSize
            };
        }

        // ─────────────────────────────────────────────
        // FILTER OPTIONS
        // ─────────────────────────────────────────────
        public async Task<FilterOptionsResponse> GetFilterOptionsAsync()
        {
            const string sql = """
                SELECT
                    ARRAY_AGG(DISTINCT bank_name            ORDER BY bank_name)            AS banks,
                    ARRAY_AGG(DISTINCT region_name          ORDER BY region_name)          AS regions,
                    ARRAY_AGG(DISTINCT payment_scheme       ORDER BY payment_scheme)       AS schemes,
                    ARRAY_AGG(DISTINCT product_type         ORDER BY product_type)         AS product_types,
                    ARRAY_AGG(DISTINCT contactless_status   ORDER BY contactless_status)   AS contactless,
                    ARRAY_AGG(DISTINCT exp_status           ORDER BY exp_status)           AS exp_statuses,
                    ARRAY_AGG(DISTINCT status_3d            ORDER BY status_3d)            AS status_3ds,
                    ARRAY_AGG(DISTINCT base_currency_name   ORDER BY base_currency_name)   AS currencies,
                    ARRAY_AGG(DISTINCT card_product_name    ORDER BY card_product_name)    AS card_products,
                    MIN(report_month) AS min_month,
                    MAX(report_month) AS max_month
                FROM mv_card_portfolio
                """;

            using var db = Connect();
            var row = await db.QuerySingleAsync(sql);

            return new FilterOptionsResponse
            {
                Banks = ((string[])row.banks).ToList(),
                Regions = ((string[])row.regions).ToList(),
                PaymentSchemes = ((string[])row.schemes).ToList(),
                ProductTypes = ((string[])row.product_types).ToList(),
                ContactlessStatuses = ((string[])row.contactless).ToList(),
                ExpStatuses = ((string[])row.exp_statuses).ToList(),
                Status3Ds = ((string[])row.status_3ds).ToList(),
                Currencies = ((string[])row.currencies).ToList(),
                CardProducts = ((string[])row.card_products).ToList(),
                MinMonth = (DateTime)row.min_month,
                MaxMonth = (DateTime)row.max_month
            };
        }

        // ─────────────────────────────────────────────
        // LATEST MONTH
        // ─────────────────────────────────────────────
        public async Task<DateTime> GetLatestReportMonthAsync()
        {
            const string sql = "SELECT MAX(report_month) FROM mv_card_portfolio";
            using var db = Connect();
            return await db.ExecuteScalarAsync<DateTime>(sql);
        }

        // ─────────────────────────────────────────────
        // TOP SCHEMES
        // ─────────────────────────────────────────────
        public async Task<TopCardsResponse> GetTopCardsAsync(CardPortfolioFilter f)
        {
            var filter = BuildCommonFilter(f, "m");

            var sql = $"""
                WITH current_data AS (
                    SELECT payment_scheme, product_type,
                           SUM(total_cards)      AS cnt,
                           SUM(prev_total_cards) AS prev_cnt
                    FROM mv_card_portfolio m
                    {filter.WhereClause}
                    GROUP BY payment_scheme, product_type
                ),
                scheme_totals AS (
                    SELECT payment_scheme,
                           SUM(cnt)      AS total,
                           SUM(prev_cnt) AS prev_total
                    FROM current_data
                    GROUP BY payment_scheme
                ),
                grand AS (SELECT SUM(total) AS grand_total FROM scheme_totals)
                SELECT
                    st.payment_scheme,
                    st.total                                                                  AS total_cards,
                    ROUND(st.total * 100.0 / NULLIF(g.grand_total, 0), 2)                    AS share_percent,
                    COALESCE(st.prev_total, 0)                                                AS prev_total,
                    COALESCE(ROUND((st.total - st.prev_total) * 100.0 / NULLIF(st.prev_total, 0), 2), 0) AS mom_change,
                    COALESCE(MAX(CASE WHEN cd.product_type = 'SALARY' THEN cd.cnt END), 0)   AS salary,
                    COALESCE(MAX(CASE WHEN cd.product_type = 'CREDIT' THEN cd.cnt END), 0)   AS credit,
                    COALESCE(MAX(CASE WHEN cd.product_type = 'SOCIAL' THEN cd.cnt END), 0)   AS social,
                    COALESCE(MAX(CASE WHEN cd.product_type = 'OTHER'  THEN cd.cnt END), 0)   AS other,
                    g.grand_total
                FROM scheme_totals st
                CROSS JOIN grand g
                LEFT JOIN current_data cd USING (payment_scheme)
                GROUP BY st.payment_scheme, st.total, st.prev_total, g.grand_total
                ORDER BY st.total DESC
                """;

            using var db = Connect();
            var rows = (await db.QueryAsync(sql, filter.Parameters)).ToList();

            var grandTotal = rows.Count > 0 ? (long)rows[0].grand_total : 0L;
            var schemes = rows
                .Where(r => r.payment_scheme != null)
                .Select(r => new TopSchemeCardDto
                {
                    PaymentScheme = r.payment_scheme,
                    TotalCards = (long)r.total_cards,
                    SharePercent = (double)r.share_percent,
                    MomChange = (double)r.mom_change,
                    MomIsUp = (double)r.mom_change >= 0,
                    ShareIsUp = (double)r.share_percent >= 0,
                    Salary = (long)r.salary,
                    Credit = (long)r.credit,
                    Social = (long)r.social,
                    Other = (long)r.other
                }).ToList();

            return new TopCardsResponse { GrandTotal = grandTotal, Schemes = schemes };
        }

        // ─────────────────────────────────────────────
        // CROSS TABLE
        // ─────────────────────────────────────────────
        public async Task<CrossTableResponse> GetCrossTableAsync(CrossTableRequest r)
        {
            var dim = SafeDim(r.RowDimension);
            var filter = BuildCommonFilter(r, "m");

            var sql = $"""
                WITH cur AS (
                    SELECT {dim} AS label,
                           product_type,
                           SUM(total_cards)      AS cnt,
                           SUM(prev_total_cards) AS prev_cnt
                    FROM mv_card_portfolio m
                    {filter.WhereClause}
                    GROUP BY {dim}, product_type
                )
                SELECT
                    label,
                    product_type,
                    cnt                                                                        AS cur_cnt,
                    COALESCE(prev_cnt, 0)                                                      AS prev_cnt,
                    COALESCE(ROUND((cnt - prev_cnt) * 100.0 / NULLIF(prev_cnt, 0), 2), 0)     AS mom_pct
                FROM cur
                ORDER BY label, product_type
                """;

            using var db = Connect();
            var rows = (await db.QueryAsync(sql, filter.Parameters)).ToList();

            var colHeaders = rows.Select(x => (string)x.product_type).Distinct().OrderBy(x => x).ToList();
            var tableRows = rows
                .GroupBy(x => (string)x.label)
                .Select(g => new CrossTableRow
                {
                    Label = g.Key,
                    Total = g.Sum(x => (long)x.cur_cnt),
                    Columns = g.Select(x => new CrossTableCell
                    {
                        ProductType = (string)x.product_type,
                        Count = (long)x.cur_cnt,
                        MomPct = (double)x.mom_pct,
                        MomUp = (double)x.mom_pct >= 0
                    }).ToList()
                }).ToList();

            return new CrossTableResponse { ColumnHeaders = colHeaders, Rows = tableRows };
        }

        // ─────────────────────────────────────────────
        // PAY CHART
        // ─────────────────────────────────────────────
        public async Task<PayChartResponse> GetPayChartAsync(PayChartRequest r)
        {
            var dim = SafeDim(r.Dimension);
            var filter = BuildCommonFilter(r, "m");

            var sql = $"""
                WITH totals AS (
                    SELECT {dim} AS label, SUM(total_cards) AS cnt
                    FROM mv_card_portfolio m
                    {filter.WhereClause}
                    GROUP BY {dim}
                ),
                grand AS (SELECT SUM(cnt) AS g FROM totals)
                SELECT label, cnt, ROUND(cnt * 100.0 / NULLIF(g, 0), 2) AS pct
                FROM totals CROSS JOIN grand
                ORDER BY cnt DESC
                """;

            using var db = Connect();
            var rows = await db.QueryAsync(sql, filter.Parameters);

            return new PayChartResponse
            {
                Items = rows.Select(x => new PayChartItem
                {
                    Label = (string)x.label ?? "",
                    Count = (long)x.cnt,
                    Percent = (double)x.pct
                }).ToList()
            };
        }

        // ─────────────────────────────────────────────
        // TREND
        // ─────────────────────────────────────────────
        public async Task<TrendChartResponse> GetTrendAsync(TrendChartRequest r)
        {
            var dim = SafeDim(r.Dimension);
            var filter = BuildCommonFilter(r, "m");

            // DimValue yerinə DimValues list
            if (r.DimValues != null && r.DimValues.Count > 0)
                filter.AddList($"m.{dim}", "DimValues", r.DimValues);

            var truncExpr = r.Granularity == "year"
                ? "DATE_TRUNC('year', report_month)::DATE"
                : "report_month";

            var sql = $"""
        SELECT
            {dim}            AS series_label,
            {truncExpr}      AS period,
            SUM(total_cards) AS cnt
        FROM mv_card_portfolio m
        {filter.WhereClause}
        GROUP BY {dim}, {truncExpr}
        ORDER BY {dim}, period
        """;

            using var db = Connect();
            var rows = await db.QueryAsync(sql, filter.Parameters);

            var series = rows
                .GroupBy(x => (string)x.series_label)
                .Select(g => new TrendSeries
                {
                    Label = g.Key,
                    Points = g.Select(x => new TrendPoint
                    {
                        Period = (DateTime)x.period,
                        Count = (long)x.cnt
                    }).ToList()
                }).ToList();

            return new TrendChartResponse { Series = series };
        }

        // ─────────────────────────────────────────────
        // XY CHART
        // ─────────────────────────────────────────────
        public async Task<XyChartResponse> GetXyChartAsync(XyChartRequest r)
        {
            var xDim = SafeDim(r.XDimension);
            var yDim = SafeDim(r.YDimension);

            if (xDim == yDim)
                throw new ArgumentException("X and Y cannot be same");

            var filter = BuildCommonFilter(r, "m");

            var sql = $"""
                SELECT
                    {xDim}                AS x_label,
                    {yDim}                AS y_label,
                    SUM(total_cards)      AS cnt,
                    SUM(prev_total_cards) AS prev_cnt,
                    ROUND(
                        SUM(total_cards) * 100.0 /
                        NULLIF(SUM(SUM(total_cards)) OVER (PARTITION BY {xDim}), 0),
                    2) AS share_pct,
                    COALESCE(ROUND(
                        (SUM(total_cards) - SUM(prev_total_cards)) * 100.0 /
                        NULLIF(SUM(prev_total_cards), 0), 2), 0) AS mom_pct,
                    SUM(total_cards) >= COALESCE(SUM(prev_total_cards), 0) AS mom_is_up
                FROM mv_card_portfolio m
                {filter.WhereClause}
                GROUP BY {xDim}, {yDim}
                ORDER BY {xDim}, cnt DESC
                """;

            using var db = Connect();
            var rows = (await db.QueryAsync(sql, filter.Parameters)).ToList();

            return new XyChartResponse
            {
                XLabels = rows.Select(x => (string)x.x_label).Distinct().OrderBy(x => x).ToList(),
                YLabels = rows.Select(x => (string)x.y_label).Distinct().OrderBy(x => x).ToList(),
                Cells = rows.Select(x => new XyCell
                {
                    XLabel = (string)x.x_label,
                    YLabel = (string)x.y_label,
                    Count = (long)x.cnt,
                    SharePct = (double)x.share_pct,
                    MomPct = (double)x.mom_pct,
                    MomIsUp = (bool)x.mom_is_up
                }).ToList()
            };
        }
    }
}
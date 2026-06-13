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
            "product_type", "card_product_name", "contactless_status",
            "exp_status", "card_category_name", "card_commercial_name", "card_brand_name"
        };

        private static string SafeDim(string dim) =>
            AllowedDimensions.Contains(dim)
                ? dim.ToLower()
                : throw new ArgumentException($"Invalid dimension: {dim}");

        private static FilterBuilder BuildCommonFilter(CardPortfolioFilter f, string alias = "m")
        {
            var fb = new FilterBuilder()
                .AddList($"{alias}.bank_name", "BankName", f.BankNames)
                .AddList($"{alias}.region_name", "RegionName", f.RegionNames)
                .AddList($"{alias}.payment_scheme", "PaymentScheme", f.PaymentSchemes)
                .AddList($"{alias}.product_type", "ProductType", f.ProductTypes)
                .AddList($"{alias}.status_3d", "Status3d", f.Status3Ds)
                .AddList($"{alias}.base_currency_name", "Currency", f.BaseCurrencies)
                .AddList($"{alias}.card_product_name", "CardProduct", f.CardProductNames)
                .AddRange($"{alias}.report_month", "FromMonth", "ToMonth", f.FromMonth, f.ToMonth);

            if (!string.IsNullOrEmpty(f.ContactlessStatus))
            {
                fb.AddRaw($"{alias}.contactless_status = @ContactlessStatus");
                fb.Parameters.Add("ContactlessStatus", f.ContactlessStatus);
            }

            if (!string.IsNullOrEmpty(f.ExpStatus))
            {
                fb.AddRaw($"{alias}.exp_status = @ExpStatus");
                fb.Parameters.Add("ExpStatus", f.ExpStatus);
            }

            return fb;
        }

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
        public async Task<IEnumerable<TopSchemeCardDto>> GetTopCardsAsync(
            CardPortfolioFilter f,
            string? dimension)
        {
            var allowedColumns = new HashSet<string>
            {
                "payment_scheme", "product_type", "bank_name",
                "region_name", "contactless_status", "exp_status", "card_product_name"
            };

            var filter = BuildCommonFilter(f, "m");

            string sql;

            if (string.IsNullOrWhiteSpace(dimension))
            {
                sql = $"""
                    SELECT
                        'All'            AS payment_scheme,
                        SUM(total_cards) AS total_cards,
                        100.00           AS share_percent
                    FROM mv_card_portfolio m
                    {filter.WhereClause}
                    """;
            }
            else
            {
                var dim = dimension.ToLower();

                if (!allowedColumns.Contains(dim))
                    throw new ArgumentException($"Namelum dimension: {dimension}");

                var filterConflicts = new Dictionary<string, bool>
                {
                    { "payment_scheme",     f.PaymentSchemes?.Any()      == true },
                    { "product_type",       f.ProductTypes?.Any()        == true },
                    { "bank_name",          f.BankNames?.Any()           == true },
                    { "region_name",        f.RegionNames?.Any()         == true },
                    { "contactless_status", !string.IsNullOrEmpty(f.ContactlessStatus) },
                    { "exp_status",         !string.IsNullOrEmpty(f.ExpStatus) },
                    { "card_product_name",  f.CardProductNames?.Any()    == true }
                };

                if (filterConflicts.TryGetValue(dim, out var hasConflict) && hasConflict)
                {
                    sql = $"""
                        SELECT
                            m.{dim}          AS payment_scheme,
                            SUM(total_cards) AS total_cards,
                            100.00           AS share_percent
                        FROM mv_card_portfolio m
                        {filter.WhereClause}
                        GROUP BY m.{dim}
                        ORDER BY total_cards DESC
                        """;
                }
                else
                {
                    sql = $"""
                        WITH filtered_data AS (
                            SELECT *
                            FROM mv_card_portfolio m
                            {filter.WhereClause}
                        ),
                        grouped_data AS (
                            SELECT
                                {dim}            AS payment_scheme,
                                SUM(total_cards) AS total_cards
                            FROM filtered_data
                            GROUP BY {dim}
                        )
                        SELECT
                            payment_scheme,
                            total_cards,
                            ROUND(
                                total_cards * 100.0 / SUM(total_cards) OVER (),
                                2
                            ) AS share_percent
                        FROM grouped_data
                        ORDER BY total_cards DESC
                        """;
                }
            }

            using var db = Connect();
            return await db.QueryAsync<TopSchemeCardDto>(sql, filter.Parameters);
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
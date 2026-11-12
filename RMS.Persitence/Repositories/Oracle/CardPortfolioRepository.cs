using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
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
            _connStr = config.GetConnectionString("OracleConnection")
                ?? config.GetConnectionString("PostgreSqlConnection")!;
        }

        private OracleConnection Connect() => new(_connStr);

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
                fb.AddRaw($"{alias}.contactless_status = :ContactlessStatus");
                fb.Parameters.Add("ContactlessStatus", f.ContactlessStatus);
            }

            if (!string.IsNullOrEmpty(f.ExpStatus))
            {
                fb.AddRaw($"{alias}.exp_status = :ExpStatus");
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
            var countSql = $"SELECT COUNT(*) FROM ({baseSql}) t_cnt";
            var dataSql = $"""
                {baseSql}
                ORDER BY {orderBy}
                OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
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

        private static FilterOptionsResponse? _cachedFilterOptions;
        private static DateTime _filterOptionsCacheExpiry = DateTime.MinValue;

        // ─────────────────────────────────────────────
        // FILTER OPTIONS
        // ─────────────────────────────────────────────
        public async Task<FilterOptionsResponse> GetFilterOptionsAsync()
        {
            if (_cachedFilterOptions is not null && DateTime.UtcNow < _filterOptionsCacheExpiry)
                return _cachedFilterOptions;

            async Task<List<string>> QueryDistinctAsync(string col)
            {
                using var db = Connect();
                return (await db.QueryAsync<string>(
                    $"SELECT DISTINCT {col} FROM mv_card_portfolio WHERE {col} IS NOT NULL ORDER BY {col}")).ToList();
            }

            async Task<(DateTime min_month, DateTime max_month)> QueryMinMaxAsync()
            {
                using var db = Connect();
                return await db.QuerySingleAsync<(DateTime min_month, DateTime max_month)>(
                    "SELECT MIN(report_month) AS min_month, MAX(report_month) AS max_month FROM mv_card_portfolio");
            }

            var minMaxTask       = QueryMinMaxAsync();
            var banksTask        = QueryDistinctAsync("bank_name");
            var regionsTask      = QueryDistinctAsync("region_name");
            var schemesTask      = QueryDistinctAsync("payment_scheme");
            var productTypesTask = QueryDistinctAsync("product_type");
            var contactlessTask  = QueryDistinctAsync("contactless_status");
            var expStatusesTask  = QueryDistinctAsync("exp_status");
            var status3DsTask    = QueryDistinctAsync("status_3d");
            var currenciesTask   = QueryDistinctAsync("base_currency_name");
            var cardProductsTask = QueryDistinctAsync("card_product_name");

            await Task.WhenAll(
                minMaxTask, banksTask, regionsTask, schemesTask, productTypesTask,
                contactlessTask, expStatusesTask, status3DsTask, currenciesTask, cardProductsTask);

            var minMax = await minMaxTask;

            var response = new FilterOptionsResponse
            {
                Banks               = await banksTask,
                Regions             = await regionsTask,
                PaymentSchemes      = await schemesTask,
                ProductTypes        = await productTypesTask,
                ContactlessStatuses = await contactlessTask,
                ExpStatuses         = await expStatusesTask,
                Status3Ds           = await status3DsTask,
                Currencies          = await currenciesTask,
                CardProducts        = await cardProductsTask,
                MinMonth            = minMax.min_month,
                MaxMonth            = minMax.max_month
            };

            _cachedFilterOptions = response;
            _filterOptionsCacheExpiry = DateTime.UtcNow.AddMinutes(30);
            return response;
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
            var rows = (await db.QueryAsync<CrossTableDbItem>(sql, filter.Parameters)).ToList();

            var colHeaders = rows
                .Select(x => x.Product_Type ?? "")
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var tableRows = rows
                .GroupBy(x => x.Label ?? "")
                .Select(g => new CrossTableRow
                {
                    Label = g.Key,
                    Total = g.Sum(x => x.Cur_Cnt),
                    Columns = g.Select(x => new CrossTableCell
                    {
                        ProductType = x.Product_Type ?? "",
                        Count = x.Cur_Cnt,
                        MomPct = x.Mom_Pct,
                        MomUp = x.Mom_Pct >= 0
                    }).ToList()
                }).ToList();

            return new CrossTableResponse { ColumnHeaders = colHeaders, Rows = tableRows };
        }

        private record CrossTableDbItem
        {
            public string? Label { get; set; }
            public string? Product_Type { get; set; }
            public long Cur_Cnt { get; set; }
            public long Prev_Cnt { get; set; }
            public double Mom_Pct { get; set; }
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
            var rows = await db.QueryAsync<PayChartDbItem>(sql, filter.Parameters);

            return new PayChartResponse
            {
                Items = rows.Select(x => new PayChartItem
                {
                    Label = x.Label ?? "",
                    Count = x.Cnt,
                    Percent = x.Pct
                }).ToList()
            };
        }

        private record PayChartDbItem
        {
            public string? Label { get; set; }
            public long Cnt { get; set; }
            public double Pct { get; set; }
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
                ? "TRUNC(report_month, 'YYYY')"
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
            var rows = await db.QueryAsync<TrendDbItem>(sql, filter.Parameters);

            var series = rows
                .GroupBy(x => x.Series_Label ?? "")
                .Select(g => new TrendSeries
                {
                    Label = g.Key,
                    Points = g.Select(x => new TrendPoint
                    {
                        Period = x.Period ?? DateTime.MinValue,
                        Count = x.Cnt ?? 0L
                    }).ToList()
                }).ToList();

            return new TrendChartResponse { Series = series };
        }

        private record TrendDbItem
        {
            public string? Series_Label { get; set; }
            public DateTime? Period { get; set; }
            public long? Cnt { get; set; }
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
                    CASE WHEN SUM(total_cards) >= COALESCE(SUM(prev_total_cards), 0) THEN 1 ELSE 0 END AS mom_is_up
                FROM mv_card_portfolio m
                {filter.WhereClause}
                GROUP BY {xDim}, {yDim}
                ORDER BY {xDim}, cnt DESC
                """;

            using var db = Connect();
            var rows = (await db.QueryAsync<XyChartDbItem>(sql, filter.Parameters)).ToList();

            return new XyChartResponse
            {
                XLabels = rows.Select(x => x.X_Label ?? "").Distinct().OrderBy(x => x).ToList(),
                YLabels = rows.Select(x => x.Y_Label ?? "").Distinct().OrderBy(x => x).ToList(),
                Cells = rows.Select(x => new XyCell
                {
                    XLabel = x.X_Label ?? "",
                    YLabel = x.Y_Label ?? "",
                    Count = x.Cnt,
                    SharePct = x.Share_Pct,
                    MomPct = x.Mom_Pct,
                    MomIsUp = x.Mom_Is_Up == 1
                }).ToList()
            };
        }

        private record XyChartDbItem
        {
            public string? X_Label { get; set; }
            public string? Y_Label { get; set; }
            public long Cnt { get; set; }
            public double Share_Pct { get; set; }
            public double Mom_Pct { get; set; }
            public int Mom_Is_Up { get; set; }
        }
    }
}
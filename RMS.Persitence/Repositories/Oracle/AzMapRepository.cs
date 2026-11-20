using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Persitence.Repositories.Oracle
{
    public class AzMapRepository : OracleRepositoryBase, IAzMapRepository
    {
        private static List<string>? _cachedCities;
        private static DateTime _citiesExpiry = DateTime.MinValue;

        private static List<string>? _cachedRegions;
        private static DateTime _regionsExpiry = DateTime.MinValue;

        private static List<string>? _cachedBanks;
        private static DateTime _banksExpiry = DateTime.MinValue;

        private static readonly ConcurrentDictionary<string, (List<string> Values, DateTime Expiry)> _columnCache = new();
        private static readonly HashSet<string> AllowedFilterColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "ACQUIRING_DEVICE_TYPE",
            "TRANS_GROUP",
            "OPERATION_TYPE",
            "TOKEN_STATUS",
            "MCC",
            "IS_ACQUIRING_CATEGORY",
            "TARGET_BANK_NAME",
            "SOURCE_BANK_NAME",
            "TARGET_CITY",
            "PAYMENT_SYSTEM",
        };

        public AzMapRepository(IConfiguration configuration) : base(configuration) { }

        private FilterBuilder BuildFilter(AzMapTransaction f)
        {
            var canonExpr = NormalizeCitySql("COALESCE(m.SOURCE_CITY_CLEAN, m.SOURCE_CITY)");
            var fb = new FilterBuilder()
                .AddString("m.BANK_NAME            = :BankName", "BankName", f.BankName)
                .AddString($"{canonExpr}           = :SourceCity", "SourceCity", f.SourceCity)
                .AddString($"{canonExpr}           = :SourceCityClean", "SourceCityClean", f.SourceCityClean)
                .AddString("m.SOURCE_CITY_CATEGORY = :CityCategory", "CityCategory", f.SourceCityCategory)
                .AddString("m.ACQUIRING_DEVICE_TYPE = :AcquiringDeviceType", "AcquiringDeviceType", f.AcquiringDeviceType)
                .AddString("m.TRANS_GROUP           = :TransGroup", "TransGroup", f.TransGroup)
                .AddString("m.OPERATION_TYPE        = :OperationType", "OperationType", f.OperationType)
                .AddString("m.TOKEN_STATUS          = :TokenStatus", "TokenStatus", f.TokenStatus)
                .AddString("TO_CHAR(m.MCC)        = :Mcc", "Mcc", f.Mcc)
                .AddString("m.IS_ACQUIRING_CATEGORY = :IsAcquiringCategory", "IsAcquiringCategory", f.IsAcquiringCategory)
                .AddString("m.TARGET_BANK_NAME      = :TargetBankName", "TargetBankName", f.TargetBankName)
                .AddString("m.SOURCE_BANK_NAME      = :SourceBankName", "SourceBankName", f.SourceBankName)
                .AddString($"{NormalizeCitySql("m.TARGET_CITY")} = :TargetCity", "TargetCity", f.TargetCity)
                .AddString("m.PAYMENT_SYSTEM        = :PaymentSystem", "PaymentSystem", f.PaymentSystem)
                .AddMonth("m.REPORT_MONTH", "Month",
                    f.ReportMonth == default ? null : f.ReportMonth);

            if (f.StartDate is not null)
                fb.Add("m.REPORT_MONTH >= TRUNC(:StartDate, 'MM')", "StartDate",
                    new DateTime(f.StartDate.Value.Year, f.StartDate.Value.Month, 1));

            if (f.EndDate is not null)
                fb.Add("m.REPORT_MONTH <= TRUNC(:EndDate, 'MM')", "EndDate",
                    new DateTime(f.EndDate.Value.Year, f.EndDate.Value.Month, 1));

            return fb;
        }

        private static string NormalizeCitySql(string expr)
        {
            var u = $"TRANSLATE(REPLACE(UPPER(TRIM({expr})), CHR(775), ''), 'ƏĞİÖŞÜÇəğıöşüç', 'EGIOSUCEGIOSUC')";
            return $"""
                CASE
                    WHEN {u} IS NULL OR TRIM({u}) = '' THEN 'DIGER'
                    -- Naxcivan rayonlari (NAXCIVAN-dan evvel yoxlanilir)
                    WHEN {u} LIKE '%ORDUBAD%' THEN 'ORDUBAD'
                    WHEN {u} LIKE '%CULFA%' OR {u} LIKE '%JULFA%' THEN 'CULFA'
                    WHEN {u} LIKE '%SERUR%' OR {u} LIKE '%SHARUR%' THEN 'SERUR'
                    WHEN {u} LIKE '%SAHBUZ%' OR {u} LIKE '%SHAHBUZ%' THEN 'SAHBUZ'
                    WHEN {u} LIKE '%BABEK%' THEN 'BABEK'
                    WHEN {u} LIKE '%KENGERLI%' OR {u} LIKE '%KANGARLI%' THEN 'KENGERLI'
                    WHEN {u} LIKE '%SEDEREK%' OR {u} LIKE '%SADARAK%' THEN 'SEDEREK'
                    WHEN {u} LIKE '%NAXCIVAN%' OR {u} LIKE '%NAKHCHIVAN%' OR {u} LIKE '%NAKHICHEVAN%' OR {u} LIKE '%NAXCHIVAN%' THEN 'NAXCIVAN'
                    -- Qubadli Quba-dan evvel yoxlanilir
                    WHEN {u} LIKE '%QUBADLI%' OR {u} LIKE '%GUBADLY%' THEN 'QUBADLI'
                    WHEN {u} LIKE '%QUBA%' OR {u} LIKE '%GUBA%' THEN 'QUBA'
                    WHEN {u} LIKE '%QUSAR%' OR {u} LIKE '%GUSAR%' THEN 'QUSAR'
                    WHEN {u} LIKE '%XACMAZ%' OR {u} LIKE '%KHACHMAZ%' OR {u} LIKE '%XACHMAZ%' OR {u} LIKE '%XUDAT%' OR {u} LIKE '%NABRAN%' THEN 'XACMAZ'
                    WHEN {u} LIKE '%SABRAN%' OR {u} LIKE '%SHABRAN%' OR {u} LIKE '%DEVECI%' OR {u} LIKE '%DEVECHI%' THEN 'SABRAN'
                    WHEN {u} LIKE '%SIYEZEN%' OR {u} LIKE '%SIYAZAN%' THEN 'SIYEZEN'
                    WHEN {u} LIKE '%XIZI%' OR {u} LIKE '%KHIZI%' THEN 'XIZI'
                    -- Sumqayit & Abseron
                    WHEN {u} LIKE '%SUMQAYIT%' OR {u} LIKE '%SUMGAYIT%' OR {u} LIKE '%SUMGAIT%' OR {u} LIKE '%SUMQAYT%' OR {u} LIKE '%CORAT%' OR REGEXP_LIKE({u}, '(^|[^A-Z0-9])AZ\s*50[0-9][0-9]([^0-9]|$)') THEN 'SUMQAYIT'
                    WHEN {u} LIKE '%ABSERON%' OR {u} LIKE '%ABSHERON%' OR {u} LIKE '%XIRDALAN%' OR {u} LIKE '%KHIRDALAN%' OR {u} LIKE '%MASAZIR%' OR {u} LIKE '%HOKMELI%' OR {u} LIKE '%QOBU%' THEN 'ABSERON'
                    -- Gence & Qerb bolgesi (GENCE NIZAMI-den evvel yoxlanilir)
                    WHEN {u} LIKE '%GENCE%' OR {u} LIKE '%GANJA%' OR {u} LIKE '%GANCA%' OR {u} LIKE '%KEPEZ%' OR {u} LIKE '%KAPAZ%' OR REGEXP_LIKE({u}, '(^|[^A-Z0-9])AZ\s*20[0-9][0-9]([^0-9]|$)') THEN 'GENCE'
                    WHEN {u} LIKE '%NAFTALAN%' THEN 'NAFTALAN'
                    WHEN {u} LIKE '%GORANBOY%' THEN 'GORANBOY'
                    WHEN {u} LIKE '%GOYGOL%' OR {u} LIKE '%GOYGEL%' OR {u} LIKE '%XANLAR%' OR {u} LIKE '%KHANLAR%' THEN 'GOYGOL'
                    WHEN {u} LIKE '%SAMUX%' OR {u} LIKE '%SAMUKH%' THEN 'SAMUX'
                    WHEN {u} LIKE '%DASKESEN%' OR {u} LIKE '%DASHKESEN%' OR {u} LIKE '%DASHKASAN%' THEN 'DASKESEN'
                    WHEN {u} LIKE '%GEDEBEY%' OR {u} LIKE '%GADABAY%' OR {u} LIKE '%GADABEY%' THEN 'GEDEBEY'
                    WHEN {u} LIKE '%SEMKIR%' OR {u} LIKE '%SHAMKIR%' OR {u} LIKE '%SAMKIR%' THEN 'SEMKIR'
                    WHEN {u} LIKE '%TOVUZ%' THEN 'TOVUZ'
                    WHEN {u} LIKE '%AGSTAFA%' OR {u} LIKE '%AGHSTAFA%' OR {u} LIKE '%ASTAFA%' THEN 'AGSTAFA'
                    WHEN {u} LIKE '%QAZAX%' OR {u} LIKE '%GAZAKH%' OR {u} LIKE '%KAZAKH%' THEN 'QAZAX'
                    -- Baki & Baki rayonlari / qesebeleri
                    WHEN {u} LIKE '%BAKI%' OR {u} LIKE '%BAKU%'
                      OR {u} LIKE '%BINAQADI%' OR {u} LIKE '%BINAGADI%'
                      OR {u} LIKE '%YASAMAL%' OR {u} LIKE '%XETAI%' OR {u} LIKE '%KHATAI%' OR {u} LIKE '%XATAI%'
                      OR {u} LIKE '%SABUNCU%' OR {u} LIKE '%SABUNCHU%'
                      OR {u} LIKE '%SURAXANI%' OR {u} LIKE '%SURAKHANI%'
                      OR {u} LIKE '%NERIMANOV%' OR {u} LIKE '%NARIMANOV%'
                      OR {u} LIKE '%NESIMI%' OR {u} LIKE '%NASIMI%'
                      OR {u} LIKE '%NIZAMI%'
                      OR {u} LIKE '%SEBAIL%' OR {u} LIKE '%SABAIL%'
                      OR {u} LIKE '%QARADAG%' OR {u} LIKE '%GARADAG%'
                      OR {u} LIKE '%XEZER%' OR {u} LIKE '%KHAZAR%' OR {u} LIKE '%XAZAR%'
                      OR {u} LIKE '%PIRALLAHI%'
                      OR {u} LIKE '%BAKIXANOV%' OR {u} LIKE '%EHMEDLI%' OR {u} LIKE '%AHMADLI%' OR {u} LIKE '%AHMEDLI%'
                      OR {u} LIKE '%GUNESLI%' OR {u} LIKE '%GUNASHLI%' OR {u} LIKE '%HOVSAN%'
                      OR {u} LIKE '%LOKBATAN%' OR {u} LIKE '%MERDEKAN%' OR {u} LIKE '%MARDAKAN%'
                      OR {u} LIKE '%BUZOVNA%' OR {u} LIKE '%BILGEH%' OR {u} LIKE '%NARDARAN%'
                      OR {u} LIKE '%MASTAGA%' OR {u} LIKE '%MASHTAGA%' OR {u} LIKE '%ZABRAT%'
                      OR {u} LIKE '%RAMANA%' OR {u} LIKE '%BADAMDAR%' OR {u} LIKE '%BAYIL%'
                      OR {u} LIKE '%KESLE%' OR {u} LIKE '%KESHLA%' OR {u} LIKE '%BILECERI%' OR {u} LIKE '%BILAJARI%'
                      OR {u} LIKE '%QARACUXUR%' OR {u} LIKE '%EMIRCAN%' OR {u} LIKE '%BULBULE%'
                      OR {u} LIKE '%SULUTEPE%' OR {u} LIKE '%MEHEMMEDI%' OR {u} LIKE '%MEHDIABAD%'
                      OR {u} LIKE '%TURKAN%' OR {u} LIKE '%ZIRE%' OR {u} LIKE '%SANGACAL%'
                      OR REGEXP_LIKE({u}, '(^|[^A-Z0-9])AZ\s*1[01][0-9][0-9]([^0-9]|$)') THEN 'BAKI'
                    -- Simal-Qerb
                    WHEN {u} LIKE '%SEKI%' OR {u} LIKE '%SHAKI%' OR {u} LIKE '%SAKI%' OR {u} LIKE '%SHEKI%' OR {u} LIKE '%NUXA%' OR {u} LIKE '%GOYNUK%' THEN 'SEKI'
                    WHEN {u} LIKE '%QAX%' OR {u} LIKE '%GAKH%' OR {u} LIKE '%QAKH%' THEN 'QAX'
                    WHEN {u} LIKE '%ZAQATALA%' OR {u} LIKE '%ZAGATALA%' THEN 'ZAQATALA'
                    WHEN {u} LIKE '%BALAKEN%' OR {u} LIKE '%BALAKAN%' THEN 'BALAKEN'
                    WHEN {u} LIKE '%QEBELE%' OR {u} LIKE '%GABALA%' OR {u} LIKE '%QABALA%' OR {u} LIKE '%QUTQASEN%' THEN 'QEBELE'
                    WHEN {u} LIKE '%OGUZ%' OR {u} LIKE '%OGHUZ%' OR {u} LIKE '%VARTASEN%' THEN 'OGUZ'
                    -- Dagliq Sirvan
                    WHEN {u} LIKE '%ISMAYILLI%' OR {u} LIKE '%ISMAYILI%' OR {u} LIKE '%ISMAILI%' THEN 'ISMAYILLI'
                    WHEN {u} LIKE '%SAMAXI%' OR {u} LIKE '%SHAMAKHI%' OR {u} LIKE '%SHAMAKHA%' OR {u} LIKE '%SAMAKHI%' OR {u} LIKE '%GOYLER%' THEN 'SAMAXI'
                    WHEN {u} LIKE '%QOBUSTAN%' OR {u} LIKE '%GOBUSTAN%' OR {u} LIKE '%MEREZE%' THEN 'QOBUSTAN'
                    WHEN {u} LIKE '%AGSU%' OR {u} LIKE '%AGHSU%' THEN 'AGSU'
                    -- Merkezi Aran & Mil-Mugan & Sirvan
                    WHEN {u} LIKE '%MINGECEVIR%' OR {u} LIKE '%MINGACHEVIR%' OR {u} LIKE '%MINGACEVIR%' OR {u} LIKE '%MINGECHEVIR%' THEN 'MINGECEVIR'
                    WHEN {u} LIKE '%YEVLAX%' OR {u} LIKE '%YEVLAKH%' THEN 'YEVLAX'
                    WHEN {u} LIKE '%GOYCAY%' OR {u} LIKE '%GOYCHAY%' THEN 'GOYCAY'
                    WHEN {u} LIKE '%UCAR%' OR {u} LIKE '%UJAR%' THEN 'UCAR'
                    WHEN {u} LIKE '%KURDEMIR%' OR {u} LIKE '%KYURDAMIR%' OR {u} LIKE '%KURDAMIR%' THEN 'KURDEMIR'
                    WHEN {u} LIKE '%ZERDAB%' OR {u} LIKE '%ZARDAB%' THEN 'ZERDAB'
                    WHEN {u} LIKE '%AGDAS%' OR {u} LIKE '%AGHDASH%' OR {u} LIKE '%AGDASH%' THEN 'AGDAS'
                    WHEN {u} LIKE '%BERDE%' OR {u} LIKE '%BARDA%' THEN 'BERDE'
                    WHEN {u} LIKE '%TERTER%' OR {u} LIKE '%TARTAR%' OR {u} LIKE '%MIRBESIR%' THEN 'TERTER'
                    WHEN {u} LIKE '%AGDAM%' OR {u} LIKE '%AGHDAM%' OR {u} LIKE '%QUZANLI%' THEN 'AGDAM'
                    WHEN {u} LIKE '%AGCABEDI%' OR {u} LIKE '%AGJABADI%' OR {u} LIKE '%AGCABADI%' OR {u} LIKE '%AGHCABEDI%' THEN 'AGCABEDI'
                    WHEN {u} LIKE '%BEYLEQAN%' OR {u} LIKE '%BEYLAGAN%' OR {u} LIKE '%JDANOV%' THEN 'BEYLEQAN'
                    WHEN {u} LIKE '%IMISLI%' OR {u} LIKE '%IMISHLI%' THEN 'IMISLI'
                    WHEN {u} LIKE '%SAATLI%' THEN 'SAATLI'
                    WHEN {u} LIKE '%SABIRABAD%' THEN 'SABIRABAD'
                    WHEN {u} LIKE '%HACIQABUL%' OR {u} LIKE '%HAJIGABUL%' OR {u} LIKE '%QAZI-MEMMED%' OR {u} LIKE '%QAZIMEMMED%' THEN 'HACIQABUL'
                    WHEN {u} LIKE '%SIRVAN%' OR {u} LIKE '%SHIRVAN%' OR {u} LIKE '%ELI-BAYRAMLI%' OR {u} LIKE '%ELIBAYRAMLI%' OR {u} LIKE '%ALI-BAYRAMLI%' OR {u} LIKE '%ALIBAYRAMLI%' THEN 'SIRVAN'
                    WHEN {u} LIKE '%SALYAN%' THEN 'SALYAN'
                    WHEN {u} LIKE '%NEFTCALA%' OR {u} LIKE '%NEFTCHALA%' THEN 'NEFTCALA'
                    WHEN {u} LIKE '%BILESUVAR%' OR {u} LIKE '%BILASUVAR%' THEN 'BILESUVAR'
                    -- Cenub bolgesi
                    WHEN {u} LIKE '%CELILABAD%' OR {u} LIKE '%JALILABAD%' OR {u} LIKE '%ASTRAXAN-BAZAR%' THEN 'CELILABAD'
                    WHEN {u} LIKE '%MASALLI%' THEN 'MASALLI'
                    WHEN {u} LIKE '%YARDIMLI%' THEN 'YARDIMLI'
                    WHEN {u} LIKE '%LERIK%' THEN 'LERIK'
                    WHEN {u} LIKE '%LENKERAN%' OR {u} LIKE '%LANKARAN%' OR {u} LIKE '%LENKORAN%' OR {u} LIKE '%PORT-ILIC%' OR {u} LIKE '%LIMAN%' THEN 'LENKERAN'
                    WHEN {u} LIKE '%ASTARA%' THEN 'ASTARA'
                    -- Qarabag & Serqi Zengezur
                    WHEN {u} LIKE '%SUSA%' OR {u} LIKE '%SHUSHA%' THEN 'SUSA'
                    WHEN {u} LIKE '%XANKENDI%' OR {u} LIKE '%KHANKENDI%' THEN 'XANKENDI'
                    WHEN {u} LIKE '%XOCALI%' OR {u} LIKE '%KHOJALY%' THEN 'XOCALI'
                    WHEN {u} LIKE '%XOCAVEND%' OR {u} LIKE '%XOCAVAND%' OR {u} LIKE '%KHOJAVEND%' THEN 'XOCAVEND'
                    WHEN {u} LIKE '%KELBECER%' OR {u} LIKE '%KALBAJAR%' THEN 'KELBECER'
                    WHEN {u} LIKE '%LACIN%' OR {u} LIKE '%LACHIN%' THEN 'LACIN'
                    WHEN {u} LIKE '%ZENGILAN%' OR {u} LIKE '%ZANGILAN%' THEN 'ZENGILAN'
                    WHEN {u} LIKE '%CEBRAYIL%' OR {u} LIKE '%JABRAYIL%' THEN 'CEBRAYIL'
                    WHEN {u} LIKE '%FUZULI%' OR {u} LIKE '%FIZULI%' OR {u} LIKE '%FUZUL %' OR {u} LIKE '%HORADIZ%' THEN 'FUZULI'
                    ELSE 'DIGER'
                END
                """;
        }

        private static string NormCitiesCte()
        {
            var normalizedCity = NormalizeCitySql("city_name");
            return $"""
                norm_cities AS (
                    SELECT
                        {normalizedCity} AS norm_name,
                        MAX(latitude)    AS latitude,
                        MAX(longitude)   AS longitude
                    FROM cities
                    GROUP BY {normalizedCity}
                )
                """;
        }

        public async Task<PagedResult<AzMapTransaction>> FilterAsync(
            AzMapTransaction f,
            PageRequest? pageReq = null,
            CancellationToken ct = default)
        {
            var filter = BuildFilter(f);
            var cityKeyFromMv = NormalizeCitySql("pm.SOURCE_CITY_CLEAN");
            var innerPagingSql = pageReq is not null
                ? "ORDER BY m.REPORT_MONTH DESC, m.TOTAL_AMOUNT DESC\n                    OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY"
                : string.Empty;

            var sql = $"""
                WITH paged_m AS (
                    SELECT
                        m.REPORT_MONTH,
                        m.BANK_NAME,
                        m.SOURCE_CITY,
                        m.SOURCE_CITY_CLEAN,
                        m.SOURCE_CITY_CATEGORY,
                        m.TOTAL_AMOUNT,
                        m.TOTAL_COUNT
                    FROM pg_mv_req4_az_map m
                    {filter.WhereClause}
                    {innerPagingSql}
                ),
                {NormCitiesCte()}
                SELECT
                    pm.REPORT_MONTH         AS ReportMonth,
                    pm.BANK_NAME            AS BankName,
                    pm.SOURCE_CITY          AS SourceCity,
                    pm.SOURCE_CITY_CLEAN    AS SourceCityClean,
                    pm.SOURCE_CITY_CATEGORY AS SourceCityCategory,
                    pm.TOTAL_AMOUNT         AS TotalAmount,
                    pm.TOTAL_COUNT          AS TotalCount,
                    c.latitude              AS Latitude,
                    c.longitude             AS Longitude
                FROM paged_m pm
                LEFT JOIN norm_cities c
                       ON pm.SOURCE_CITY_CLEAN IS NOT NULL
                      AND c.norm_name = {cityKeyFromMv}
                ORDER BY pm.REPORT_MONTH DESC, pm.TOTAL_AMOUNT DESC
                """;

            using var conn = CreateConnection();

            if (pageReq is null)
            {
                var canonCitySql = NormalizeCitySql("COALESCE(m.SOURCE_CITY_CLEAN, m.SOURCE_CITY)");
                var aggSql = $"""
                    WITH base_m AS (
                        SELECT
                            m.REPORT_MONTH,
                            m.BANK_NAME,
                            {canonCitySql}         AS CANON_CITY,
                            m.SOURCE_CITY_CATEGORY,
                            m.TOTAL_AMOUNT,
                            m.TOTAL_COUNT
                        FROM pg_mv_req4_az_map m
                        {filter.WhereClause}
                    ),
                    agg_m AS (
                        SELECT
                            MAX(REPORT_MONTH)      AS REPORT_MONTH,
                            BANK_NAME,
                            CANON_CITY             AS SOURCE_CITY,
                            CANON_CITY             AS SOURCE_CITY_CLEAN,
                            SOURCE_CITY_CATEGORY,
                            SUM(TOTAL_AMOUNT)      AS TOTAL_AMOUNT,
                            SUM(TOTAL_COUNT)       AS TOTAL_COUNT
                        FROM base_m
                        GROUP BY
                            BANK_NAME,
                            CANON_CITY,
                            SOURCE_CITY_CATEGORY
                    ),
                    {NormCitiesCte()}
                    SELECT
                        am.REPORT_MONTH         AS ReportMonth,
                        am.BANK_NAME            AS BankName,
                        am.SOURCE_CITY          AS SourceCity,
                        am.SOURCE_CITY_CLEAN    AS SourceCityClean,
                        am.SOURCE_CITY_CATEGORY AS SourceCityCategory,
                        am.TOTAL_AMOUNT         AS TotalAmount,
                        am.TOTAL_COUNT          AS TotalCount,
                        c.latitude              AS Latitude,
                        c.longitude             AS Longitude
                    FROM agg_m am
                    LEFT JOIN norm_cities c
                           ON am.SOURCE_CITY_CLEAN IS NOT NULL
                          AND c.norm_name = am.SOURCE_CITY_CLEAN
                    ORDER BY am.TOTAL_AMOUNT DESC
                    """;

                var allItems = (await conn.QueryAsync<AzMapTransaction>(
                    new CommandDefinition(aggSql, filter.Parameters, cancellationToken: ct))).ToList();

                return new PagedResult<AzMapTransaction>
                {
                    Items = allItems,
                    TotalCount = allItems.Count,
                    Page = 1,
                    PageSize = allItems.Count > 0 ? allItems.Count : 1
                };
            }

            var countSql = $"SELECT COUNT(*) FROM pg_mv_req4_az_map m {filter.WhereClause}";

            var dynParam = new DynamicParameters(filter.Parameters);
            dynParam.Add("Offset", (pageReq.Page - 1) * pageReq.PageSize);
            dynParam.Add("PageSize", pageReq.PageSize);

            var totalCount = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, filter.Parameters, cancellationToken: ct));

            var items = await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, dynParam, cancellationToken: ct));

            return new PagedResult<AzMapTransaction>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pageReq.Page,
                PageSize = pageReq.PageSize
            };
        }

        public async Task<IEnumerable<AzMapTransaction>> GetHeatmapDataAsync(
            string? bankName,
            string? sourceCity,
            DateTime? month,
            CancellationToken ct = default)
        {
            var canonCitySql = NormalizeCitySql("COALESCE(m.SOURCE_CITY_CLEAN, m.SOURCE_CITY)");
            var filter = new FilterBuilder()
                .AddString("m.BANK_NAME   = :BankName", "BankName", bankName)
                .AddString($"{canonCitySql} = :SourceCity", "SourceCity", sourceCity)
                .AddMonth("m.REPORT_MONTH", "Month", month);

            // Əvvəlcə MV daxilində şəhər üzrə qruplaşdırılır (~70 sətir), yalnız ondan sonra cities ilə JOIN edilir
            var sql = $"""
                WITH base_m AS (
                    SELECT
                        {canonCitySql} AS CANON_CITY,
                        m.TOTAL_AMOUNT,
                        m.TOTAL_COUNT
                    FROM pg_mv_req4_az_map m
                    {filter.WhereClause}
                ),
                city_agg AS (
                    SELECT
                        CANON_CITY        AS SOURCE_CITY_CLEAN,
                        SUM(TOTAL_AMOUNT) AS TOTAL_AMOUNT,
                        SUM(TOTAL_COUNT)  AS TOTAL_COUNT
                    FROM base_m
                    GROUP BY CANON_CITY
                ),
                {NormCitiesCte()}
                SELECT
                    ca.SOURCE_CITY_CLEAN AS SourceCityClean,
                    ca.TOTAL_AMOUNT      AS TotalAmount,
                    ca.TOTAL_COUNT       AS TotalCount,
                    c.latitude           AS Latitude,
                    c.longitude          AS Longitude
                FROM city_agg ca
                LEFT JOIN norm_cities c
                       ON ca.SOURCE_CITY_CLEAN IS NOT NULL
                      AND c.norm_name = ca.SOURCE_CITY_CLEAN
                WHERE c.latitude IS NOT NULL AND c.longitude IS NOT NULL
                """;

            using var conn = CreateConnection();
            return await conn.QueryAsync<AzMapTransaction>(
                new CommandDefinition(sql, filter.Parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<string>> GetDistinctCitiesAsync(
            CancellationToken ct = default)
        {
            if (_cachedCities is not null && DateTime.UtcNow < _citiesExpiry)
                return _cachedCities;

            var canonCitySql = NormalizeCitySql("RAW_CITY");
            var sql = $"""
                SELECT DISTINCT CITY_NAME FROM (
                    SELECT {canonCitySql} AS CITY_NAME
                    FROM (
                        SELECT DISTINCT COALESCE(SOURCE_CITY_CLEAN, SOURCE_CITY) AS RAW_CITY
                        FROM pg_mv_req4_az_map
                        WHERE COALESCE(SOURCE_CITY_CLEAN, SOURCE_CITY) IS NOT NULL
                    )
                )
                WHERE CITY_NAME != 'DIGER'
                ORDER BY CITY_NAME
                """;
            using var conn = CreateConnection();
            var list = (await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct))).ToList();

            _cachedCities = list;
            _citiesExpiry = DateTime.UtcNow.AddMinutes(30);
            return list;
        }

        public Task<IEnumerable<string>> GetDistinctRegionsAsync(
            CancellationToken ct = default)
            => GetDistinctCitiesAsync(ct);

        public async Task<IEnumerable<string>> GetDistinctBanksAsync(
            CancellationToken ct = default)
        {
            if (_cachedBanks is not null && DateTime.UtcNow < _banksExpiry)
                return _cachedBanks;

            const string sql = """
                SELECT DISTINCT BANK_NAME
                FROM pg_mv_req4_az_map
                WHERE BANK_NAME IS NOT NULL
                ORDER BY BANK_NAME
                """;
            using var conn = CreateConnection();
            var list = (await conn.QueryAsync<string>(
                new CommandDefinition(sql, cancellationToken: ct))).ToList();

            _cachedBanks = list;
            _banksExpiry = DateTime.UtcNow.AddMinutes(30);
            return list;
        }

        public async Task<IEnumerable<string>> GetDistinctColumnValuesAsync(
            string columnName,
            bool normalizeCity = false,
            CancellationToken ct = default)
        {
            if (!AllowedFilterColumns.Contains(columnName))
                return Array.Empty<string>();

            var cacheKey = $"{columnName}:{normalizeCity}";
            if (_columnCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.Expiry)
                return cached.Values;

            string sql;
            if (normalizeCity)
            {
                var canonSql = NormalizeCitySql("RAW_VAL");
                sql = $"""
                    SELECT DISTINCT VAL FROM (
                        SELECT {canonSql} AS VAL
                        FROM (
                            SELECT DISTINCT {columnName} AS RAW_VAL
                            FROM pg_mv_req4_az_map
                            WHERE {columnName} IS NOT NULL
                        )
                    )
                    WHERE VAL != 'DIGER'
                    ORDER BY VAL
                    """;
            }
            else
            {
                sql = $"""
                    SELECT DISTINCT TO_CHAR({columnName}) AS VAL
                    FROM pg_mv_req4_az_map
                    WHERE {columnName} IS NOT NULL
                    ORDER BY VAL
                    """;
            }

            try
            {
                using var conn = CreateConnection();
                var list = (await conn.QueryAsync<string>(
                    new CommandDefinition(sql, cancellationToken: ct)))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                _columnCache[cacheKey] = (list, DateTime.UtcNow.AddMinutes(30));
                return list;
            }
            catch (OracleException ex) when (ex.Number == 904)
            {
                return Array.Empty<string>();
            }
        }
    }
}
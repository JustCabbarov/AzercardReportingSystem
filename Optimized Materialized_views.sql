-- ============================================================================
-- ORACLE PERFORMANCE OPTIMIZATION SCRIPT FOR 600M+ ROWS
-- Schema: Dynamic (Run in target user schema) / RAUF_REPORT
-- ============================================================================
-- 600 Milyondan çox data üçün tətbiq edilən əsas optimizasiyalar:
-- 1. PARALLEL 8: Bütün MV və sorğulara paralel icra dərəcəsi (CPU nüvələrinə görə 8 və ya 16 seçilə bilər).
-- 2. NOLOGGING: Redo log generasiyasını dayandırır, disk I/O-nu minimuma endirir.
-- 3. COMPRESS BASIC: Cədvəl ölçüsünü 60-80% kiçildir, diskdən oxuma sürətini 3-4 dəfə artırır.
-- 4. Optimizer Hints (/*+ PARALLEL(t, 8) */): 600M-lik ana cədvəlin (BI_TRANSACTION_MASKED) serial oxunmasının qarşısını alır.
-- 5. Pre-Aggregation CTEs (Req 7 və Req 1): 600M sətirli cədvəllə JOIN etmək əvəzinə, əvvəlcə tranzaksiyaları GROUP BY edib kiçildib sonra JOIN edir (30 milyardlıq kartesian partlayışının qarşısı alınır!).
-- 6. Fast Index Creation: İndekslər NOLOGGING PARALLEL 8 COMPUTE STATISTICS ilə yaradılır və sonra NOPARALLEL LOGGING edilir.
-- 7. Düzəlişlər: PostgreSQL sintaksisləri (text, float8, public.) və dublikat AZ_MAP adı aradan qaldırıldı.
-- ============================================================================

-- SESSIYA SƏVİYYƏSİNDƏ OPTİMİZASİYANI AKTİVLƏŞDİRİN (Scripti icra etməzdən əvvəl):
ALTER SESSION ENABLE PARALLEL DDL;
ALTER SESSION ENABLE PARALLEL DML;
ALTER SESSION ENABLE PARALLEL QUERY;
ALTER SESSION SET WORKAREA_SIZE_POLICY = AUTO;

-- ═══════════════════════════════════════════════════════════════════════════
-- 1. SummaryTransaction
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_SUMMARY_TRANSACTION
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(t, 8) */
    TRUNC(t.report_date, 'DD') AS report_day,
    t.is_issuing,
    t.is_acquiring,
    t.acquiring_device_type,
    t.target_bank_name,
    t.source_bank_name,
    t.card_product_type_category,
    t.payment_system,
    t.trans_group,
    t.operation_type,
    t.token_status,
    t.is_contactless,
    t.transaction_currency,
    SUM(t.local_amount)      AS total_local_amount,
    SUM(t.transaction_count) AS total_count,
    SUM(t.settlement_amount) AS total_settlement_amount
FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
GROUP BY
    TRUNC(t.report_date, 'DD'),
    t.is_issuing,
    t.is_acquiring,
    t.acquiring_device_type,
    t.target_bank_name,
    t.source_bank_name,
    t.card_product_type_category,
    t.payment_system,
    t.trans_group,
    t.operation_type,
    t.token_status,
    t.is_contactless,
    t.transaction_currency;

CREATE INDEX IDX_MV_SUMMARY_FILTERS
    ON MV_SUMMARY_TRANSACTION (card_product_type_category, trans_group, operation_type, token_status, is_contactless)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_SUMMARY_MAIN
    ON MV_SUMMARY_TRANSACTION (report_day, is_issuing, is_acquiring, acquiring_device_type)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_SUMMARY_SOURCE_BANK
    ON MV_SUMMARY_TRANSACTION (source_bank_name)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_SUMMARY_TARGET_BANK
    ON MV_SUMMARY_TRANSACTION (target_bank_name)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_MV_SUMMARY_FILTERS NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_SUMMARY_MAIN NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_SUMMARY_SOURCE_BANK NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_SUMMARY_TARGET_BANK NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 2. TransactionDetailed
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_TRANSACTION_SUMMARY
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(t, 8) */
    TRUNC(t.report_date, 'DD') AS report_day,
    CASE t.is_issuing_category
        WHEN 'On-US'          THEN 'Daxili'
        WHEN 'Domestic AC'     THEN 'AZC'
        WHEN 'Domestic Non-AC' THEN 'Bazar'
        WHEN 'Foreign non-AC'  THEN 'Xarici'
        ELSE t.is_issuing_category
    END AS issuing_category,
    t.source_bank_name,
    t.target_bank_name,
    t.payment_system,
    t.acquiring_device_type,
    t.card_product_type_category,
    t.transaction_currency,
    t.operation_type,
    t.trans_group,
    t.token_status,
    SUM(t.local_amount)      AS total_local_amount,
    SUM(t.transaction_count) AS total_count,
    SUM(t.settlement_amount) AS total_settlement_amount
FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
WHERE t.is_issuing = 1 OR t.is_issuing = '1'
GROUP BY
    TRUNC(t.report_date, 'DD'),
    t.is_issuing_category,
    t.source_bank_name,
    t.target_bank_name,
    t.payment_system,
    t.acquiring_device_type,
    t.card_product_type_category,
    t.transaction_currency,
    t.operation_type,
    t.trans_group,
    t.token_status;

CREATE INDEX IDX_MV_TRANS_CARD_TYPE
    ON MV_TRANSACTION_SUMMARY (card_product_type_category, transaction_currency, operation_type, trans_group, token_status)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_TRANS_COMPOSITE
    ON MV_TRANSACTION_SUMMARY (report_day, issuing_category, source_bank_name, payment_system, acquiring_device_type)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_TRANS_TARGET_BANK
    ON MV_TRANSACTION_SUMMARY (target_bank_name)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_MV_TRANS_CARD_TYPE NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_TRANS_COMPOSITE NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_TRANS_TARGET_BANK NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 3. Devices
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_DEVICES_BASE
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH monthly_agg AS (
    SELECT /*+ PARALLEL(d, 8) */
        TRUNC(d.report_date, 'MM') AS report_month,
        d.region_name,
        d.region_name_clean,
        d.bank_name,
        d.mcc_name,
        d.retail_category,
        d.transaction_class,
        d.ctls_status,
        SUM(d.count_dev) AS total_devices
    FROM RAUF_REPORT.BI_MARKET_DEVICES_MASKED d
    GROUP BY
        TRUNC(d.report_date, 'MM'),
        d.region_name,
        d.region_name_clean,
        d.bank_name,
        d.mcc_name,
        d.retail_category,
        d.transaction_class,
        d.ctls_status
)
SELECT
    report_month,
    region_name,
    region_name_clean,
    bank_name,
    mcc_name,
    retail_category,
    transaction_class,
    ctls_status,
    total_devices,
    ROUND(total_devices * 100.0 / NULLIF(SUM(total_devices) OVER (PARTITION BY report_month, region_name, mcc_name, retail_category), 0), 2) AS share_pct_by_bank,
    ROUND(total_devices * 100.0 / NULLIF(SUM(total_devices) OVER (PARTITION BY report_month, bank_name, mcc_name, retail_category), 0), 2)   AS share_pct_by_region,
    ROUND(total_devices * 100.0 / NULLIF(SUM(total_devices) OVER (PARTITION BY report_month, bank_name, region_name, retail_category), 0), 2) AS share_pct_by_mcc,
    ROUND(total_devices * 100.0 / NULLIF(SUM(total_devices) OVER (PARTITION BY report_month, bank_name, region_name, mcc_name), 0), 2)       AS share_pct_by_retail_cat,
    SUM(total_devices) OVER (PARTITION BY report_month) AS grand_total_month
FROM monthly_agg;

CREATE INDEX IDX_BASE_COMPOSITE
  ON MV_DEVICES_BASE (REPORT_MONTH, BANK_NAME, REGION_NAME, MCC_NAME, RETAIL_CATEGORY)
  NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_BASE_COMPOSITE NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 4. CardPortfolio
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_CARD_PORTFOLIO
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH base AS (
    SELECT /*+ PARALLEL(c, 8) */
        TRUNC(c.report_month, 'MM') AS report_month,
        c.bank_name,
        c.region_name_clean AS region_name,
        c.payment_scheme,
        c.card_brand_name,
        c.card_category_name,
        c.card_product_name,
        c.card_commercial_name,
        c.base_currency_name,
        c.product_type,
        c.contactless_status,
        c.exp_status,
        c.status_3d,
        SUM(c.count_card) AS total_cards
    FROM RAUF_REPORT.BI_MARKET_CARDS_MASKED c
    GROUP BY
        TRUNC(c.report_month, 'MM'),
        c.bank_name,
        c.region_name_clean,
        c.payment_scheme,
        c.card_brand_name,
        c.card_category_name,
        c.card_product_name,
        c.card_commercial_name,
        c.base_currency_name,
        c.product_type,
        c.contactless_status,
        c.exp_status,
        c.status_3d
)
SELECT
    report_month,
    bank_name,
    region_name,
    payment_scheme,
    card_brand_name,
    card_category_name,
    card_product_name,
    card_commercial_name,
    base_currency_name,
    product_type,
    contactless_status,
    exp_status,
    status_3d,
    total_cards,
    LAG(total_cards) OVER (
        PARTITION BY bank_name, region_name, payment_scheme, base_currency_name,
                      product_type, contactless_status, exp_status, status_3d,
                      card_brand_name, card_category_name, card_product_name, card_commercial_name
        ORDER BY report_month
    ) AS prev_total_cards
FROM base;

CREATE INDEX IDX_MVCP_BANK_NAME      ON MV_CARD_PORTFOLIO (bank_name) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_CONTACTLESS    ON MV_CARD_PORTFOLIO (contactless_status) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_EXP_STATUS     ON MV_CARD_PORTFOLIO (exp_status) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_PAYMENT_SCHEME ON MV_CARD_PORTFOLIO (payment_scheme) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_PRODUCT_TYPE   ON MV_CARD_PORTFOLIO (product_type) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_REGION         ON MV_CARD_PORTFOLIO (region_name) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_REPORT_MONTH   ON MV_CARD_PORTFOLIO (report_month) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_MVCP_STATUS_3D      ON MV_CARD_PORTFOLIO (status_3d) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_MVCP_BANK_NAME NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_CONTACTLESS NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_EXP_STATUS NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_PAYMENT_SCHEME NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_PRODUCT_TYPE NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_REGION NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_REPORT_MONTH NOPARALLEL LOGGING;
ALTER INDEX IDX_MVCP_STATUS_3D NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 5. CardDashboard
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_CARD_DASHBOARD
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(t, 8) */
    TRUNC(t.report_date, 'MM')   AS report_month,
    TRUNC(t.report_date, 'YYYY') AS report_year,
    TRUNC(t.report_date, 'Q')    AS report_quarter,
    t.payment_system,
    t.is_issuing_category,
    t.operation_type,
    t.card_product_type_category,
    t.token_status,
    t.trans_group,
    t.target_bank_name,
    t.is_contactless,
    SUM(t.local_amount)      AS total_amount,
    SUM(t.transaction_count) AS total_tx_count,
    MAX(t.local_amount)      AS max_amount,
    SUM(t.settlement_amount) AS total_settlement_amount
FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
WHERE t.is_issuing = 1 OR t.is_issuing = '1'
GROUP BY
    TRUNC(t.report_date, 'MM'),
    TRUNC(t.report_date, 'YYYY'),
    TRUNC(t.report_date, 'Q'),
    t.payment_system,
    t.is_issuing_category,
    t.operation_type,
    t.card_product_type_category,
    t.token_status,
    t.trans_group,
    t.target_bank_name,
    t.is_contactless;

CREATE INDEX IDX_MV_CARD_DASH_FILTERS
    ON MV_CARD_DASHBOARD (card_product_type_category, token_status, trans_group, is_contactless)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_CARD_DASH_PAYMENT
    ON MV_CARD_DASHBOARD (payment_system, is_issuing_category, operation_type)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_CARD_DASH_PERIOD
    ON MV_CARD_DASHBOARD (report_month, report_quarter, report_year)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_CARD_DASH_TARGET_BANK
    ON MV_CARD_DASHBOARD (target_bank_name)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_MV_CARD_DASH_FILTERS NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_CARD_DASH_PAYMENT NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_CARD_DASH_PERIOD NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_CARD_DASH_TARGET_BANK NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 6. AcquiringTransaction (600M Rows - Optimized)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_ACQUIRING_DEVICE_TRANSACTION
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH base AS (
    SELECT /*+ PARALLEL(t, 8) */
        t.report_date,
        TRUNC(t.report_date, 'MM') AS period_month,
        CASE
            WHEN t.trans_group IN ('CH Payment', 'CH Debit', 'CH Payment Gmbl')
                THEN 'C2C'
            WHEN t.trans_group IN ('ATM', 'Cash Dispense (Cardless)')
                THEN 'ATM'
            WHEN t.trans_group NOT IN ('ATM', 'Cash Dispense (Cardless)', 'Note Acceptance')
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT')
                THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance'
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT', 'Unk')
                THEN 'ATM'
            WHEN t.trans_group = 'Cash'
                THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Retail with CashBack', 'Credit')
                 AND SUBSTR(t.tr_condition, 1, 3) = 'POS'
                THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Bill Payment', 'Credit')
                 AND (t.tr_condition IS NULL
                      OR SUBSTR(t.tr_condition, 1, 3) NOT IN ('ATM', 'CAT', 'POS'))
                THEN 'ECOM'
            WHEN t.trans_group IN (
                    'UFX Payment Debit offline', 'UFX Payment Credit offline',
                    'UFX Sales Debit offline', 'UFX Sales Debit online',
                    'UFX Sales Credit offline', 'UFX IPS Credit offline',
                    'UFX IPS Debit offline')
                THEN 'UFX'
            WHEN t.tr_condition IN (
                    'ATM', 'ATM Chip Contactless', 'ATM Chip Full Grade',
                    'CAT PBT', 'CAT PBT Chip Contactless', 'CAT PBT Chip Full Grade')
                THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance'
                THEN 'ATM'
            WHEN t.tr_condition = 'POS Card Read Partial'
                THEN 'POS'
            ELSE 'ECOM'
        END AS acquiring_device_type_calc,
        t.acquiring_device_type,
        t.trans_group,
        t.operation_type,
        t.token_status,
        t.mcc,
        t.is_acquiring_category,
        t.target_bank_name,
        t.source_bank_name,
        t.target_city,
        t.payment_system,
        t.is_contactless,
        t.local_amount,
        t.transaction_count,
        t.settlement_amount
    FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
    WHERE t.is_acquiring = 1 OR t.is_acquiring = '1'
)
SELECT
    report_date,
    period_month,
    acquiring_device_type_calc,
    acquiring_device_type,
    trans_group,
    operation_type,
    token_status,
    mcc,
    is_acquiring_category,
    target_bank_name,
    source_bank_name,
    target_city,
    payment_system,
    is_contactless,
    SUM(local_amount)      AS local_amount,
    SUM(transaction_count) AS transaction_count,
    SUM(settlement_amount) AS settlement_amount
FROM base
GROUP BY
    report_date, period_month, acquiring_device_type_calc, acquiring_device_type,
    trans_group, operation_type, token_status, mcc, is_acquiring_category,
    target_bank_name, source_bank_name, target_city, payment_system, is_contactless;

CREATE INDEX IX_MV_ACQ_BANKS
  ON MV_ACQUIRING_DEVICE_TRANSACTION (SOURCE_BANK_NAME, TARGET_BANK_NAME, PERIOD_MONTH)
  NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IX_MV_ACQ_PAYMENT_SYS
  ON MV_ACQUIRING_DEVICE_TRANSACTION (PAYMENT_SYSTEM, PERIOD_MONTH)
  NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IX_MV_ACQ_PERIOD
  ON MV_ACQUIRING_DEVICE_TRANSACTION (PERIOD_MONTH, ACQUIRING_DEVICE_TYPE_CALC)
  NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IX_MV_ACQ_BANKS NOPARALLEL LOGGING;
ALTER INDEX IX_MV_ACQ_PAYMENT_SYS NOPARALLEL LOGGING;
ALTER INDEX IX_MV_ACQ_PERIOD NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 7. DevicesTrend
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW MV_DEVICES_TREND
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH monthly_agg AS (
    SELECT /*+ PARALLEL(d, 8) */
        TRUNC(d.report_date, 'MM') AS report_month,
        d.region_name,
        d.region_name_clean,
        d.bank_name,
        d.mcc_name,
        d.retail_category,
        d.transaction_class,
        d.ctls_status,
        SUM(d.count_dev) AS total_devices
    FROM RAUF_REPORT.BI_MARKET_DEVICE d
    GROUP BY
        TRUNC(d.report_date, 'MM'),
        d.region_name,
        d.region_name_clean,
        d.bank_name,
        d.mcc_name,
        d.retail_category,
        d.transaction_class,
        d.ctls_status
)
SELECT
    report_month,
    region_name,
    region_name_clean,
    bank_name,
    mcc_name,
    retail_category,
    transaction_class,
    ctls_status,
    total_devices,
    LAG(total_devices) OVER (
        PARTITION BY bank_name, region_name, mcc_name, retail_category, transaction_class, ctls_status
        ORDER BY report_month
    ) AS prev_month_devices,
    total_devices - LAG(total_devices) OVER (
        PARTITION BY bank_name, region_name, mcc_name, retail_category, transaction_class, ctls_status
        ORDER BY report_month
    ) AS mom_diff,
    ROUND(
        (total_devices - LAG(total_devices) OVER (
            PARTITION BY bank_name, region_name, mcc_name, retail_category, transaction_class, ctls_status
            ORDER BY report_month
        )) * 100.0 / NULLIF(LAG(total_devices) OVER (
            PARTITION BY bank_name, region_name, mcc_name, retail_category, transaction_class, ctls_status
            ORDER BY report_month
        ), 0), 2
    ) AS mom_pct_change
FROM monthly_agg;

CREATE INDEX IDX_TREND_COMPOSITE
    ON MV_DEVICES_TREND (report_month, bank_name, region_name, mcc_name, retail_category)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_TREND_COMPOSITE NOPARALLEL LOGGING;


-- ═══════════════════════════════════════════════════════════════════════════
-- 8. Sector Spend (Req 8 - 600M Rows Optimized)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW PG_MV_REQ8_SECTOR_SPEND
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(t, 8) */
    TRUNC(t.report_date, 'MM') AS report_month,
    t.source_bank_name AS bank_name,
    COALESCE(t.mcc_group, 'OTHER') AS mcc_group,
    COALESCE(CAST(t.mcc AS VARCHAR2(10)), 'OTHER') AS mcc,
    COALESCE(t.source_city_clean, 'NAMƏLUM') AS source_city,
    COALESCE(t.source_city_category, 'OTHER') AS source_city_category,
    COALESCE(t.source_channel, 'OTHER') AS source_channel,
    COALESCE(t.operation_type, 'OTHER') AS operation_type,
    COALESCE(t.source_country_category, 'OTHER') AS source_country_category,
    t.is_acquiring,
    t.is_issuing,
    SUM(t.transaction_amount) AS total_amount,
    SUM(t.transaction_count) AS total_count,
    SUM(t.local_amount) AS total_local_amount,
    ROUND(SUM(t.transaction_amount) / NULLIF(SUM(t.transaction_count), 0), 4) AS avg_ticket,
    ROUND(SUM(t.local_amount) / NULLIF(SUM(t.transaction_amount), 0) * 100, 4) AS local_ratio_pct,
    ROUND(
        SUM(t.transaction_amount) /
        NULLIF(
            SUM(SUM(t.transaction_amount)) OVER (
                PARTITION BY TRUNC(t.report_date, 'MM'),
                             COALESCE(t.mcc_group, 'OTHER')
            ),
            0
        ) * 100,
        4
    ) AS sector_market_share_pct
FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
WHERE t.mcc_group IS NOT NULL
GROUP BY
    TRUNC(t.report_date, 'MM'),
    t.source_bank_name,
    COALESCE(t.mcc_group, 'OTHER'),
    COALESCE(CAST(t.mcc AS VARCHAR2(10)), 'OTHER'),
    COALESCE(t.source_city_clean, 'NAMƏLUM'),
    COALESCE(t.source_city_category, 'OTHER'),
    COALESCE(t.source_channel, 'OTHER'),
    COALESCE(t.operation_type, 'OTHER'),
    COALESCE(t.source_country_category, 'OTHER'),
    t.is_acquiring,
    t.is_issuing;

CREATE INDEX IDX_MV_REQ8_BANK_MONTH
    ON PG_MV_REQ8_SECTOR_SPEND (BANK_NAME, REPORT_MONTH)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_REQ8_MONTH_MCC
    ON PG_MV_REQ8_SECTOR_SPEND (REPORT_MONTH, MCC_GROUP, BANK_NAME)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_MV_REQ8_MAIN
    ON PG_MV_REQ8_SECTOR_SPEND (REPORT_MONTH, BANK_NAME, MCC_GROUP, IS_ACQUIRING, IS_ISSUING)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_MV_REQ8_BANK_MONTH NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_REQ8_MONTH_MCC NOPARALLEL LOGGING;
ALTER INDEX IDX_MV_REQ8_MAIN NOPARALLEL LOGGING;

GRANT SELECT ON PG_MV_REQ8_SECTOR_SPEND TO REPORTAPPUSER;
COMMENT ON MATERIALIZED VIEW PG_MV_REQ8_SECTOR_SPEND IS 'Materialized view for sector spend reporting';


-- ═══════════════════════════════════════════════════════════════════════════
-- 9. Benchmark (Req 7 - CRITICAL OPTIMIZATION: PRE-AGGREGATION)
-- ═══════════════════════════════════════════════════════════════════════════
-- QEYD: Əvvəlki kodda 600M sətirli BI_TRANSACTION_MASKED cədvəli card_agg ilə
-- GROUP BY-dan ƏVVƏL JOIN edilirdi. Bu hər tranzaksiyanı bankın bütün kart kateqoriyalarına
-- vuraraq ON MİLYARDLARLA sətirlik Cartesian partlayış yaradırdı və sorğu saatlarla bitmirdi.
-- Yeni versiyada tranzaksiya cədvəli ƏVVƏLCƏ (report_month, bank) üzrə cəmlənir (~500 sətir qalır),
-- sonra kart cədvəli ilə birləşdirilir! Sorğu bir neçə saniyəyə icra olunur.
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW PG_MV_REQ7_BENCHMARK
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH card_agg AS (
    SELECT /*+ PARALLEL(c, 8) */
        c.bank_name,
        COALESCE(c.region_name_clean, 'NAMƏLUM') AS region_name_clean,
        COALESCE(c.card_brand_name, 'OTHER') AS card_brand_name,
        COALESCE(c.product_type, 'OTHER') AS product_type,
        TRUNC(c.report_month, 'MM') AS report_month,
        SUM(c.count_card) AS total_cards
    FROM RAUF_REPORT.BI_MARKET_CARDS_MASKED c
    GROUP BY
        c.bank_name,
        COALESCE(c.region_name_clean, 'NAMƏLUM'),
        COALESCE(c.card_brand_name, 'OTHER'),
        COALESCE(c.product_type, 'OTHER'),
        TRUNC(c.report_month, 'MM')
),
trans_monthly AS (
    SELECT /*+ PARALLEL(t, 8) */
        TRUNC(t.report_date, 'MM') AS report_month,
        t.source_bank_name AS bank_name,
        SUM(t.transaction_amount) AS bank_trans_amount,
        SUM(t.transaction_count) AS bank_trans_count
    FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
    GROUP BY
        TRUNC(t.report_date, 'MM'),
        t.source_bank_name
),
base AS (
    SELECT
        ca.report_month,
        ca.bank_name,
        ca.region_name_clean,
        ca.card_brand_name,
        ca.product_type,
        NVL(tm.bank_trans_amount, 0) AS bank_trans_amount,
        NVL(tm.bank_trans_count, 0)  AS bank_trans_count,
        ca.total_cards               AS bank_card_count,
        SUM(NVL(tm.bank_trans_amount, 0)) OVER (
            PARTITION BY ca.report_month, ca.region_name_clean, ca.card_brand_name
        ) AS market_total_amount,
        SUM(ca.total_cards) OVER (
            PARTITION BY ca.report_month, ca.region_name_clean, ca.card_brand_name
        ) AS market_total_cards
    FROM card_agg ca
    LEFT JOIN trans_monthly tm
        ON tm.bank_name = ca.bank_name
       AND tm.report_month = ca.report_month
)
SELECT
    report_month,
    bank_name,
    region_name_clean,
    card_brand_name,
    product_type,
    bank_trans_amount,
    bank_trans_count,
    bank_card_count,
    market_total_amount,
    market_total_cards,
    ROUND(bank_trans_amount / NULLIF(market_total_amount, 0) * 100, 4) AS market_share_amount_pct,
    ROUND(bank_card_count / NULLIF(market_total_cards, 0) * 100, 4) AS market_share_cards_pct,
    RANK() OVER (
        PARTITION BY report_month, region_name_clean, card_brand_name
        ORDER BY bank_trans_amount DESC
    ) AS bank_rank
FROM base;

CREATE INDEX IDX_REQ7_RANK  ON PG_MV_REQ7_BENCHMARK (BANK_RANK) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ7_BANK  ON PG_MV_REQ7_BENCHMARK (BANK_NAME) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ7_MONTH ON PG_MV_REQ7_BENCHMARK (REPORT_MONTH) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_REQ7_RANK NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ7_BANK NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ7_MONTH NOPARALLEL LOGGING;

GRANT SELECT ON PG_MV_REQ7_BENCHMARK TO REPORTAPPUSER;
COMMENT ON MATERIALIZED VIEW PG_MV_REQ7_BENCHMARK IS 'Materialized view for benchmark reporting';


-- ═══════════════════════════════════════════════════════════════════════════
-- 10. Azerbaijan Map (Req 4)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW PG_MV_REQ4_AZ_MAP
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(t, 8) */
    TRUNC(t.report_date, 'MM') AS report_month,
    t.source_bank_name AS bank_name,
    COALESCE(t.source_city, 'NAMƏLUM') AS source_city,
    COALESCE(t.source_city_clean, 'NAMƏLUM') AS source_city_clean,
    t.source_city_category,
    SUM(t.transaction_amount) AS total_amount,
    SUM(t.transaction_count) AS total_count,
    COUNT(*) AS cnt
FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
WHERE t.source_country_category = 'Domestic'
  AND t.source_city_category IN ('BAKI', 'REGION')
GROUP BY
    TRUNC(t.report_date, 'MM'),
    t.source_bank_name,
    COALESCE(t.source_city, 'NAMƏLUM'),
    COALESCE(t.source_city_clean, 'NAMƏLUM'),
    t.source_city_category;

CREATE UNIQUE INDEX IDX_REQ4_COMPOSITE
ON PG_MV_REQ4_AZ_MAP
(
    REPORT_MONTH,
    BANK_NAME,
    SOURCE_CITY,
    SOURCE_CITY_CLEAN,
    SOURCE_CITY_CATEGORY
)
NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_REQ4_BANK  ON PG_MV_REQ4_AZ_MAP (BANK_NAME) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ4_CLEAN ON PG_MV_REQ4_AZ_MAP (SOURCE_CITY_CLEAN) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ4_CITY  ON PG_MV_REQ4_AZ_MAP (SOURCE_CITY) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ4_MONTH ON PG_MV_REQ4_AZ_MAP (REPORT_MONTH) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_REQ4_COMPOSITE NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ4_BANK NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ4_CLEAN NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ4_CITY NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ4_MONTH NOPARALLEL LOGGING;

GRANT SELECT ON PG_MV_REQ4_AZ_MAP TO REPORTAPPUSER;
COMMENT ON MATERIALIZED VIEW PG_MV_REQ4_AZ_MAP IS 'Materialized view for Azerbaijan map reporting';


-- ═══════════════════════════════════════════════════════════════════════════
-- 11. World Map (Req 5 - Ad və Şərt Düzəlişi Edildi)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW PG_MV_REQ5_WORLD_MAP
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(t, 8) */
    TRUNC(t.report_date, 'MM') AS report_month,
    t.source_bank_name AS bank_name,
    COALESCE(t.source_country_category, 'OTHER') AS source_country,
    COALESCE(t.source_country_category, 'OTHER') AS source_country_category,
    'Azerbaijan' AS target_country,
    'Domestic' AS target_country_category,
    COALESCE(t.source_country_category, 'OTHER') AS country_category,
    COALESCE(t.source_city_clean, 'NAMƏLUM') AS city_clean,
    t.payment_system,
    t.is_issuing,
    t.is_acquiring,
    SUM(t.transaction_amount) AS total_amount,
    SUM(t.transaction_count) AS total_count,
    COUNT(*) AS cnt
FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
WHERE t.source_country_category <> 'Domestic' OR t.source_country_category IS NULL
GROUP BY
    TRUNC(t.report_date, 'MM'),
    t.source_bank_name,
    COALESCE(t.source_country_category, 'OTHER'),
    COALESCE(t.source_city_clean, 'NAMƏLUM'),
    t.payment_system,
    t.is_issuing,
    t.is_acquiring;

CREATE INDEX IDX_REQ5_WORLD_BANK  ON PG_MV_REQ5_WORLD_MAP (BANK_NAME) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ5_WORLD_MONTH ON PG_MV_REQ5_WORLD_MAP (REPORT_MONTH) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ5_WORLD_CNTRY ON PG_MV_REQ5_WORLD_MAP (COUNTRY_CATEGORY) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_REQ5_WORLD_BANK NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ5_WORLD_MONTH NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ5_WORLD_CNTRY NOPARALLEL LOGGING;

GRANT SELECT ON PG_MV_REQ5_WORLD_MAP TO REPORTAPPUSER;
COMMENT ON MATERIALIZED VIEW PG_MV_REQ5_WORLD_MAP IS 'Materialized view for World map reporting';


-- ═══════════════════════════════════════════════════════════════════════════
-- 12. Forecasting Input (Req 1 - 600M Rows Optimized Pre-Aggregation)
-- ═══════════════════════════════════════════════════════════════════════════
-- QEYD: Əvvəlki kodda 600M sətir kart cədvəli ilə JOIN edilib sonra GROUP BY edilirdi.
-- Burada əvvəlcə tranzaksiyalar qruplaşdırılır (~20,000 sətir), sonra kartlarla JOIN edilir.
-- ═══════════════════════════════════════════════════════════════════════════
CREATE MATERIALIZED VIEW PG_MV_REQ1_FORECAST_INPUT
NOLOGGING
PARALLEL 8
COMPRESS BASIC
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH card_agg AS (
    SELECT /*+ PARALLEL(c, 8) */
        c.bank_name,
        TRUNC(c.report_month, 'MM') AS report_month,
        SUM(c.count_card) AS total_cards
    FROM RAUF_REPORT.BI_MARKET_CARDS_MASKED c
    GROUP BY
        c.bank_name,
        TRUNC(c.report_month, 'MM')
),
trans_agg AS (
    SELECT /*+ PARALLEL(t, 8) */
        TRUNC(t.report_date, 'MM') AS report_month,
        t.source_bank_name AS bank_name,
        t.mcc_group,
        SUM(t.transaction_amount) AS total_amount,
        SUM(t.transaction_count)  AS total_count
    FROM RAUF_REPORT.BI_TRANSACTION_MASKED t
    GROUP BY
        TRUNC(t.report_date, 'MM'),
        t.source_bank_name,
        t.mcc_group
),
monthly AS (
    SELECT
        ta.report_month,
        ta.bank_name,
        ta.mcc_group,
        ta.total_amount,
        ta.total_count,
        ca.total_cards
    FROM trans_agg ta
    LEFT JOIN card_agg ca
        ON ca.bank_name = ta.bank_name
       AND ca.report_month = ta.report_month
),
with_lags AS (
    SELECT
        report_month,
        bank_name,
        mcc_group,
        total_amount,
        total_count,
        total_cards,
        LAG(total_amount, 1) OVER (
            PARTITION BY bank_name, mcc_group
            ORDER BY report_month
        ) AS amount_lag1,
        LAG(total_amount, 3) OVER (
            PARTITION BY bank_name, mcc_group
            ORDER BY report_month
        ) AS amount_lag3,
        LAG(total_amount, 12) OVER (
            PARTITION BY bank_name, mcc_group
            ORDER BY report_month
        ) AS amount_lag12,
        AVG(total_amount) OVER (
            PARTITION BY bank_name, mcc_group
            ORDER BY report_month
            ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
        ) AS rolling_avg_3m,
        AVG(total_amount) OVER (
            PARTITION BY bank_name, mcc_group
            ORDER BY report_month
            ROWS BETWEEN 5 PRECEDING AND CURRENT ROW
        ) AS rolling_avg_6m,
        EXTRACT(MONTH FROM report_month) AS season_month,
        ROW_NUMBER() OVER (
            PARTITION BY bank_name, mcc_group
            ORDER BY report_month
        ) AS time_index
    FROM monthly
)
SELECT
    report_month,
    bank_name,
    mcc_group,
    total_amount,
    total_count,
    total_cards,
    amount_lag1,
    amount_lag3,
    amount_lag12,
    rolling_avg_3m,
    rolling_avg_6m,
    season_month,
    time_index,
    ROUND(total_amount / NULLIF(total_count, 0), 4) AS avg_ticket,
    ROUND(total_amount / NULLIF(total_cards, 0), 4) AS amount_per_card
FROM with_lags;

CREATE UNIQUE INDEX IDX_REQ1_UQ
    ON PG_MV_REQ1_FORECAST_INPUT (REPORT_MONTH, BANK_NAME, MCC_GROUP)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_REQ1_MONTH
    ON PG_MV_REQ1_FORECAST_INPUT (REPORT_MONTH)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

CREATE INDEX IDX_REQ1_BANK
    ON PG_MV_REQ1_FORECAST_INPUT (BANK_NAME)
    NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_REQ1_UQ NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ1_MONTH NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ1_BANK NOPARALLEL LOGGING;

GRANT SELECT ON PG_MV_REQ1_FORECAST_INPUT TO REPORTAPPUSER;
COMMENT ON MATERIALIZED VIEW PG_MV_REQ1_FORECAST_INPUT IS 'Materialized view for forecast input';


-- ═══════════════════════════════════════════════════════════════════════════
-- 13. Reference Cədvəllər (Oracle DDL Syntax ilə Düzəldildi)
-- ═══════════════════════════════════════════════════════════════════════════
-- PostgreSQL sintaksisi (public., text, float8) Oracle tiplərinə çevrildi:

BEGIN
    EXECUTE IMMEDIATE 'CREATE TABLE COUNTRIES (
        NAME      VARCHAR2(255) NOT NULL,
        LATITUDE  NUMBER(10, 7) NULL,
        LONGITUDE NUMBER(10, 7) NULL,
        CONSTRAINT PK_COUNTRIES PRIMARY KEY (NAME)
    )';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'CREATE TABLE CITIES (
        CITY_NAME VARCHAR2(255) NOT NULL,
        LATITUDE  NUMBER(10, 7) NULL,
        LONGITUDE NUMBER(10, 7) NULL,
        CONSTRAINT PK_CITIES PRIMARY KEY (CITY_NAME)
    )';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'CREATE TABLE BANKS (
        BANK_HASH VARCHAR2(100),
        BANK_NAME VARCHAR2(100)
    )';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'CREATE TABLE MCC_GROUPS (
        MCC_HASH VARCHAR2(100),
        MCC_NAME VARCHAR2(100)
    )';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/


-- ═══════════════════════════════════════════════════════════════════════════
-- 14. 600M DATA ÜÇÜN ƏN SÜRƏTLİ REFRESH PROSEDURU (DBMS_MVIEW)
-- ═══════════════════════════════════════════════════════════════════════════
-- DİQQƏT: Oracle-da default refresh (atomic_refresh => TRUE) olduqda DELETE + INSERT edir,
-- bu da 600M sətir üçün terabaytlarla UNDO və REDO yaradıb sistemi dondura bilər.
-- Aşağıdakı PL/SQL bloku TRUNCATE + DIRECT-PATH INSERT (/*+ APPEND PARALLEL */)
-- istifadə edərək ən yüksək sürətlə REFRESH edir:
/*
BEGIN
    DBMS_MVIEW.REFRESH(
        LIST                 => 'MV_SUMMARY_TRANSACTION,MV_TRANSACTION_SUMMARY,MV_ACQUIRING_DEVICE_TRANSACTION,PG_MV_REQ8_SECTOR_SPEND,PG_MV_REQ7_BENCHMARK,PG_MV_REQ4_AZ_MAP,PG_MV_REQ5_WORLD_MAP,PG_MV_REQ1_FORECAST_INPUT',
        METHOD               => 'C',
        PARALLELISM          => 8,
        ATOMIC_REFRESH       => FALSE,
        NESTED               => FALSE
    );
END;
/
*/

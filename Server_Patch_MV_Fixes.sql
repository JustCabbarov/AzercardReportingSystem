-- ============================================================================
-- AZERCARD REPORTING SYSTEM - PRODUCTION MATERIALIZED VIEW FIX VƏ PATCH SCRIPT
-- Hədəf mühit: Oracle Database (600M+ sətir / BI_TRANSACTION_MASKED)
--
-- Mənbə Cədvəllər:
--   1. BI_TRANSACTION_MASKED
--   2. BI_MARKET_CARDS_MASKED
--   3. BI_MARKET_DEVICES_MASKED
--
-- Əhatə olunan Materialized View-lar (12 ədəd):
--   1.  MV_SUMMARY_TRANSACTION          (Summary Transaction / Prototip Summary)
--   2.  MV_TRANSACTION_SUMMARY          (Transaction Detailed / Prototip Card Summary)
--   3.  MV_ACQUIRING_DEVICE_TRANSACTION (Acquiring Device Transaction)
--   4.  MV_CARD_DASHBOARD               (Card Dashboard)
--   5.  MV_CARD_PORTFOLIO               (Card Portfolio Dashboard)
--   6.  MV_DEVICES_BASE                 (Devices Portfolio - Base)
--   7.  MV_DEVICES_TREND                (Devices Portfolio - Trend)
--   8.  PG_MV_REQ4_AZ_MAP               (Azərbaycan Xəritəsi)
--   9.  PG_MV_REQ5_WORLD_MAP            (Dünya Xəritəsi)
--   10. PG_MV_REQ8_SECTOR_SPEND         (Sektorlar üzrə Xərcləmə)
--   11. PG_MV_REQ7_BENCHMARK            (Bazar Müqayisəsi / Benchmark)
--   12. PG_MV_REQ1_FORECAST_INPUT       (Proqnoz Tətbiqi / Forecast)
--
-- ============================================================================
-- BU SKRİPTDƏ HƏLL EDİLƏN ƏSAS HESABLAMA VƏ SERVER PROBLEMLƏRİ:
--   1. MÖVCUD MV-LƏRİN TƏHLÜKƏSİZ YENİLƏNMƏSİ VƏ İCAZƏLƏRİN (GRANT) QORUNMASI:
--      - Serverdə əvvəlcədən yaradılmış MV, VIEW və ya eyni adlı cədvəllər
--        xəta vermədən (ORA-12003, ORA-00942, ORA-00955) təmizlənir və yenidən qurulur.
--      - Bölmə 0-da mövcud MV-lər üzərindəki bütün GRANT-lar avtomatik yadda
--        saxlanılır və Bölmə 13-də avtomatik bərpa edilir (həmçinin REPORTAPPUSER üçün).
--   2. CİHAZ NÖVÜ (ACQUIRING_DEVICE_TYPE) HESABLAMASI DÜZƏLDİLDİ:
--      - MV_SUMMARY_TRANSACTION və MV_TRANSACTION_SUMMARY daxilində xam bazadakı
--        'E-COMMERCE' dəyəri COALESCE səbəbilə Slide 4 CASE düsturunu bloklayırdı
--        (nəticədə ECOM=0, C2C=0, UFX=0 çıxırdı). Artıq birbaşa PPTX Slide 4
--        qaydası ilə ('POS', 'ATM', 'ECOM', 'C2C', 'UFX') hesablanır.
--   3. MV_TRANSACTION_SUMMARY - ISSUING_CATEGORY DÜZƏLDİLDİ:
--      - PPTX tələbinə uyğun 4 kateqoriya bərpa olundu:
--        'On-US' -> 'Daxili', 'Domestic AC' -> 'AZC',
--        'Domestic Non-AC' -> 'Bazar', 'International'/'Foreign' -> 'Xarici'.
--   4. PG_MV_REQ4_AZ_MAP (AZƏRBAYCAN XƏRİTƏSİ) DÜZƏLDİLDİ:
--      - PPTX tələbinə uyğun YALNIZ IS_ACQUIRING = '1' və Azərbaycan daxili
--        əməliyyatlar götürülür, TARGET_CITY üzrə ('Baku'/'Baki' -> 'Baki',
--        'Ganja'/'Ganca' -> 'Ganca' və s.) normallaşdırılır və Repository ilə
--        tam uyğunluq üçün SOURCE_CITY / SOURCE_CITY_CLEAN kimi aliaslanır.
--   5. PG_MV_REQ5_WORLD_MAP (DÜNYA XƏRİTƏSİ) DÜZƏLDİLDİ:
--      - PPTX tələbinə uyğun YALNIZ IS_ISSUING = '1' süzgəci və LOCAL_AMOUNT
--        cəmi tətbiq edildi.
--   6. MV_CARD_PORTFOLIO VƏ MV_DEVICES_BASE DÜZƏLDİLDİ:
--      - MV_CARD_PORTFOLIO-ya çatışmayan CARD_COMMERCIAL_NAME sütunu əlavə edildi
--        və REGION_NAME normallaşdırıldı.
--      - MV_DEVICES_BASE-ə həm faiz payları (SHARE_PCT_BY_*), həm də MoM artım
--        sütunları daxil edildi; hər iki MV-də ən son ETL_DATE deduplikasiyası saxlanıldı.
--   7. 600M+ SƏTİR PERFORMANS OPTİMİZASİYASI:
--      - PG_MV_REQ7_BENCHMARK və PG_MV_REQ1_FORECAST_INPUT 600M-lıq cədvəli təkrar
--        skan etmir, birbaşa PG_MV_REQ8_SECTOR_SPEND üzərindən saniyələr içində qurulur.
--      - Statistikalar DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV','CURRENT_SCHEMA'), ...)
--        ilə toplanır ki, həm schema sahibi, həm də DBA tərəfindən problemsiz işləsin.
-- ============================================================================

-- SQL*Plus / SQLcl mühitində '&' simvolunun dəyişən kimi soruşulmaması üçün:
SET DEFINE OFF;


-- ============================================================================
-- 0. MÖVCUD İCAZƏLƏRİN (GRANT) AVTOMATİK YADDA SAXLANILMASI
-- MV-lər DROP edildikdə üzərlərindəki GRANT-lar silindiyi üçün əvvəlcə
-- müvəqqəti cədvəldə saxlayırıq və skriptin sonunda avtomatik bərpa edirik.
-- ============================================================================
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE mv_patch_grants_bkp PURGE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

CREATE TABLE mv_patch_grants_bkp AS
SELECT grantee, table_name, privilege, grantable
FROM all_tab_privs
WHERE table_schema = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
  AND table_name IN (
      'MV_SUMMARY_TRANSACTION', 'MV_TRANSACTION_SUMMARY', 'MV_ACQUIRING_DEVICE_TRANSACTION',
      'MV_CARD_DASHBOARD', 'MV_CARD_PORTFOLIO', 'MV_DEVICES_BASE', 'MV_DEVICES_TREND',
      'PG_MV_REQ4_AZ_MAP', 'PG_MV_REQ5_WORLD_MAP', 'PG_MV_REQ8_SECTOR_SPEND',
      'PG_MV_REQ7_BENCHMARK', 'PG_MV_REQ1_FORECAST_INPUT'
  );


-- ============================================================================
-- 1. MV_SUMMARY_TRANSACTION (Slide 1 - Summary Transaction və Prototip Summary)
-- Fix:
--   - acquiring_device_type birbaşa PPTX Slide 4 CASE qaydası ilə hesablanır
--     ('POS', 'ATM', 'ECOM', 'C2C', 'UFX') ki, 'E-COMMERCE' ucbatından ECOM/C2C/UFX itməsin
--   - is_issuing, is_acquiring, is_contactless '0'/'1' normallaşdırılır
--   - report_day TRUNC(t.report_date, 'DD') saxlanır
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_summary_transaction'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_summary_transaction'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_summary_transaction CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW mv_summary_transaction
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT
    s.report_day,
    s.is_issuing,
    s.is_acquiring,
    s.acquiring_device_type,
    s.target_bank_name,
    s.source_bank_name,
    s.card_product_type_category,
    s.payment_system,
    s.trans_group,
    s.operation_type,
    s.token_status,
    s.is_contactless,
    s.transaction_currency,
    SUM(s.local_amount)      AS total_local_amount,
    SUM(s.transaction_count) AS total_count,
    SUM(s.settlement_amount) AS total_settlement_amount
FROM (
    SELECT /*+ PARALLEL(8) */
        TRUNC(t.report_date, 'DD')              AS report_day,
        NVL(TRIM(TO_CHAR(t.is_issuing)), '0')   AS is_issuing,
        NVL(TRIM(TO_CHAR(t.is_acquiring)), '0') AS is_acquiring,
        CASE
            WHEN t.trans_group IN ('CH Payment', 'CH Debit', 'CH Payment Gmbl') THEN 'C2C'
            WHEN t.trans_group IN ('ATM', 'Cash Dispense (Cardless)') THEN 'ATM'
            WHEN t.trans_group NOT IN ('ATM', 'Cash Dispense (Cardless)', 'Note Acceptance')
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT') THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance'
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT', 'Unk') THEN 'ATM'
            WHEN t.trans_group = 'Cash' THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Retail with CashBack', 'Credit')
                 AND SUBSTR(t.tr_condition, 1, 3) = 'POS' THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Bill Payment', 'Credit')
                 AND (t.tr_condition IS NULL OR SUBSTR(t.tr_condition, 1, 3) NOT IN ('ATM', 'CAT', 'POS')) THEN 'ECOM'
            WHEN t.trans_group IN (
                'UFX Payment Debit offline', 'UFX Payment Credit offline',
                'UFX Sales Debit offline', 'UFX Sales Debit online',
                'UFX Sales Credit offline', 'UFX IPS Credit offline', 'UFX IPS Debit offline'
            ) THEN 'UFX'
            WHEN t.tr_condition IN (
                'ATM', 'ATM Chip Contactless', 'ATM Chip Full Grade',
                'CAT PBT', 'CAT PBT Chip Contactless', 'CAT PBT Chip Full Grade'
            ) THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance' THEN 'ATM'
            WHEN t.tr_condition = 'POS Card Read Partial' THEN 'POS'
            WHEN UPPER(TRIM(t.acquiring_device_type)) IN ('ATM', 'POS', 'C2C', 'UFX') THEN UPPER(TRIM(t.acquiring_device_type))
            ELSE 'ECOM'
        END                                     AS acquiring_device_type,
        t.target_bank_name,
        t.source_bank_name,
        t.card_product_type_category,
        t.payment_system,
        t.trans_group,
        t.operation_type,
        t.token_status,
        CASE
            WHEN LOWER(TRIM(TO_CHAR(t.is_contactless))) IN ('1', 'true', 'yes', 'y', 'contactless') THEN '1'
            ELSE '0'
        END                                     AS is_contactless,
        t.transaction_currency,
        NVL(t.local_amount, 0)                  AS local_amount,
        NVL(t.transaction_count, 0)             AS transaction_count,
        NVL(t.settlement_amount, 0)             AS settlement_amount
    FROM bi_transaction_masked t
) s
GROUP BY
    s.report_day,
    s.is_issuing,
    s.is_acquiring,
    s.acquiring_device_type,
    s.target_bank_name,
    s.source_bank_name,
    s.card_product_type_category,
    s.payment_system,
    s.trans_group,
    s.operation_type,
    s.token_status,
    s.is_contactless,
    s.transaction_currency;

CREATE INDEX idx_mv_sum_tr_day     ON mv_summary_transaction (report_day) NOLOGGING;
CREATE INDEX idx_mv_sum_tr_iss_acq ON mv_summary_transaction (is_issuing, is_acquiring, report_day) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_SUMMARY_TRANSACTION', cascade => TRUE); END;
/


-- ============================================================================
-- 2. MV_TRANSACTION_SUMMARY (Slide 2 - Card Transaction Detailed)
-- Fix:
--   - issuing_category PPTX-dəki 4 kateqoriyaya uyğunlaşdırılır:
--     'On-US' -> 'Daxili', 'Domestic AC' -> 'AZC', 'Domestic Non-AC' -> 'Bazar',
--     'International' / 'Foreign' -> 'Xarici'
--   - acquiring_device_type birbaşa Slide 4 CASE qaydası ilə hesablanır
--   - CARD_ACCOUNT_CURRENCY prioritet götürülür (null olarsa transaction_currency)
--   - TRIM(TO_CHAR(t.is_issuing)) = '1'
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_transaction_summary'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_transaction_summary'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_transaction_summary CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW mv_transaction_summary
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT
    s.report_day,
    s.issuing_category,
    s.source_bank_name,
    s.target_bank_name,
    s.payment_system,
    s.acquiring_device_type,
    s.card_product_type_category,
    s.transaction_currency,
    s.operation_type,
    s.trans_group,
    s.token_status,
    SUM(s.local_amount)      AS total_local_amount,
    SUM(s.transaction_count) AS total_count,
    SUM(s.settlement_amount) AS total_settlement_amount
FROM (
    SELECT /*+ PARALLEL(8) */
        TRUNC(t.report_date, 'DD') AS report_day,
        CASE
            WHEN UPPER(TRIM(t.is_issuing_category)) IN ('ON-US', 'ON US', 'ONUS', 'DAXILI', 'DAXİLİ') THEN 'Daxili'
            WHEN UPPER(TRIM(t.is_issuing_category)) IN ('DOMESTIC AC', 'AZC') THEN 'AZC'
            WHEN UPPER(TRIM(t.is_issuing_category)) IN ('DOMESTIC NON-AC', 'DOMESTIC NON AC', 'BAZAR') THEN 'Bazar'
            WHEN UPPER(TRIM(t.is_issuing_category)) = 'DOMESTIC' THEN 'AZC'
            WHEN UPPER(TRIM(t.is_issuing_category)) IN ('INTERNATIONAL', 'FOREIGN', 'FOREIGN NON-AC', 'FOREIGN AC', 'XARICI', 'XARİCİ') THEN 'Xarici'
            ELSE NVL(TRIM(t.is_issuing_category), 'Digər')
        END                        AS issuing_category,
        t.source_bank_name,
        t.target_bank_name,
        t.payment_system,
        CASE
            WHEN t.trans_group IN ('CH Payment', 'CH Debit', 'CH Payment Gmbl') THEN 'C2C'
            WHEN t.trans_group IN ('ATM', 'Cash Dispense (Cardless)') THEN 'ATM'
            WHEN t.trans_group NOT IN ('ATM', 'Cash Dispense (Cardless)', 'Note Acceptance')
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT') THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance'
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT', 'Unk') THEN 'ATM'
            WHEN t.trans_group = 'Cash' THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Retail with CashBack', 'Credit')
                 AND SUBSTR(t.tr_condition, 1, 3) = 'POS' THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Bill Payment', 'Credit')
                 AND (t.tr_condition IS NULL OR SUBSTR(t.tr_condition, 1, 3) NOT IN ('ATM', 'CAT', 'POS')) THEN 'ECOM'
            WHEN t.trans_group IN (
                'UFX Payment Debit offline', 'UFX Payment Credit offline',
                'UFX Sales Debit offline', 'UFX Sales Debit online',
                'UFX Sales Credit offline', 'UFX IPS Credit offline', 'UFX IPS Debit offline'
            ) THEN 'UFX'
            WHEN t.tr_condition IN (
                'ATM', 'ATM Chip Contactless', 'ATM Chip Full Grade',
                'CAT PBT', 'CAT PBT Chip Contactless', 'CAT PBT Chip Full Grade'
            ) THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance' THEN 'ATM'
            WHEN t.tr_condition = 'POS Card Read Partial' THEN 'POS'
            WHEN UPPER(TRIM(t.acquiring_device_type)) IN ('ATM', 'POS', 'C2C', 'UFX') THEN UPPER(TRIM(t.acquiring_device_type))
            ELSE 'ECOM'
        END                        AS acquiring_device_type,
        t.card_product_type_category,
        COALESCE(NULLIF(TRIM(t.card_account_currency), ''), t.transaction_currency) AS transaction_currency,
        t.operation_type,
        t.trans_group,
        t.token_status,
        NVL(t.local_amount, 0)      AS local_amount,
        NVL(t.transaction_count, 0) AS transaction_count,
        NVL(t.settlement_amount, 0) AS settlement_amount
    FROM bi_transaction_masked t
    WHERE TRIM(TO_CHAR(t.is_issuing)) = '1'
) s
GROUP BY
    s.report_day,
    s.issuing_category,
    s.source_bank_name,
    s.target_bank_name,
    s.payment_system,
    s.acquiring_device_type,
    s.card_product_type_category,
    s.transaction_currency,
    s.operation_type,
    s.trans_group,
    s.token_status;

CREATE INDEX idx_mv_tx_sum_day      ON mv_transaction_summary (report_day) NOLOGGING;
CREATE INDEX idx_mv_tx_sum_src_bank ON mv_transaction_summary (source_bank_name, report_day) NOLOGGING;
CREATE INDEX idx_mv_tx_sum_cat      ON mv_transaction_summary (issuing_category, report_day) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_TRANSACTION_SUMMARY', cascade => TRUE); END;
/


-- ============================================================================
-- 3. MV_ACQUIRING_DEVICE_TRANSACTION (Slide 4 - Acquiring Device Transaction)
-- Fix:
--   - report_date TRUNC(t.report_date, 'DD') ilə saxlanır
--   - TRIM(TO_CHAR(t.is_acquiring)) = '1'
--   - target_city üzrə ingilis/azərbaycan dublikat adları ('Baku'/'Baki' -> 'Baki')
--     vahid standarta gətirilir
--   - is_contactless '1'/'0' normallaşdırılır
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_acquiring_device_transaction'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_acquiring_device_transaction'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_acquiring_device_transaction CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW mv_acquiring_device_transaction
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT
    s.report_date,
    s.period_month,
    s.acquiring_device_type_calc,
    s.acquiring_device_type,
    s.trans_group,
    s.operation_type,
    s.token_status,
    s.mcc,
    s.is_acquiring_category,
    s.target_bank_name,
    s.source_bank_name,
    s.target_city,
    s.payment_system,
    s.is_contactless,
    SUM(s.local_amount)      AS local_amount,
    SUM(s.transaction_count) AS transaction_count,
    SUM(s.settlement_amount) AS settlement_amount
FROM (
    SELECT /*+ PARALLEL(8) */
        TRUNC(t.report_date, 'DD') AS report_date,
        TRUNC(t.report_date, 'MM') AS period_month,
        CASE
            WHEN t.trans_group IN ('CH Payment', 'CH Debit', 'CH Payment Gmbl') THEN 'C2C'
            WHEN t.trans_group IN ('ATM', 'Cash Dispense (Cardless)') THEN 'ATM'
            WHEN t.trans_group NOT IN ('ATM', 'Cash Dispense (Cardless)', 'Note Acceptance')
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT') THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance'
                 AND SUBSTR(t.tr_condition, 1, 3) IN ('ATM', 'CAT', 'Unk') THEN 'ATM'
            WHEN t.trans_group = 'Cash' THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Retail with CashBack', 'Credit')
                 AND SUBSTR(t.tr_condition, 1, 3) = 'POS' THEN 'POS'
            WHEN t.trans_group IN ('Retail', 'Unique', 'Bill Payment', 'Credit')
                 AND (t.tr_condition IS NULL OR SUBSTR(t.tr_condition, 1, 3) NOT IN ('ATM', 'CAT', 'POS')) THEN 'ECOM'
            WHEN t.trans_group IN (
                'UFX Payment Debit offline', 'UFX Payment Credit offline',
                'UFX Sales Debit offline', 'UFX Sales Debit online',
                'UFX Sales Credit offline', 'UFX IPS Credit offline', 'UFX IPS Debit offline'
            ) THEN 'UFX'
            WHEN t.tr_condition IN (
                'ATM', 'ATM Chip Contactless', 'ATM Chip Full Grade',
                'CAT PBT', 'CAT PBT Chip Contactless', 'CAT PBT Chip Full Grade'
            ) THEN 'ATM'
            WHEN t.trans_group = 'Note Acceptance' THEN 'ATM'
            WHEN t.tr_condition = 'POS Card Read Partial' THEN 'POS'
            WHEN UPPER(TRIM(t.acquiring_device_type)) IN ('ATM', 'POS', 'C2C', 'UFX') THEN UPPER(TRIM(t.acquiring_device_type))
            ELSE 'ECOM'
        END                        AS acquiring_device_type_calc,
        t.acquiring_device_type,
        t.trans_group,
        t.operation_type,
        t.token_status,
        t.mcc,
        t.is_acquiring_category,
        t.target_bank_name,
        t.source_bank_name,
        CASE
            WHEN UPPER(TRIM(t.target_city)) IN ('BAKU', 'BAKI', 'BAKİ') THEN 'Baki'
            WHEN UPPER(TRIM(t.target_city)) IN ('GANJA', 'GANCA', 'GƏNCƏ') THEN 'Ganca'
            WHEN UPPER(TRIM(t.target_city)) IN ('SUMGAYIT', 'SUMQAYIT') THEN 'Sumqayit'
            WHEN UPPER(TRIM(t.target_city)) IN ('MINGACHEVIR', 'MINGACEVIR', 'MİNGƏÇEVİR') THEN 'Mingacevir'
            WHEN UPPER(TRIM(t.target_city)) IN ('SHIRVAN', 'SIRVAN', 'ŞİRVAN') THEN 'Sirvan'
            WHEN UPPER(TRIM(t.target_city)) IN ('NAKHCHIVAN', 'NAXCIVAN', 'NAXÇIVAN') THEN 'Naxcivan'
            WHEN UPPER(TRIM(t.target_city)) IN ('SHAKI', 'SAKI', 'ŞƏKİ') THEN 'Saki'
            WHEN UPPER(TRIM(t.target_city)) IN ('SHUSHA', 'SUSA', 'ŞUŞA') THEN 'Susa'
            WHEN UPPER(TRIM(t.target_city)) IN ('LANKARAN', 'LƏNKƏRAN') THEN 'Lankaran'
            WHEN UPPER(TRIM(t.target_city)) IN ('YEVLAKH', 'YEVLAX') THEN 'Yevlakh'
            ELSE TRIM(t.target_city)
        END                        AS target_city,
        t.payment_system,
        CASE
            WHEN LOWER(TRIM(TO_CHAR(t.is_contactless))) IN ('1', 'true', 'yes', 'y', 'contactless') THEN '1'
            ELSE '0'
        END                        AS is_contactless,
        NVL(t.local_amount, 0)      AS local_amount,
        NVL(t.transaction_count, 0) AS transaction_count,
        NVL(t.settlement_amount, 0) AS settlement_amount
    FROM bi_transaction_masked t
    WHERE TRIM(TO_CHAR(t.is_acquiring)) = '1'
) s
GROUP BY
    s.report_date,
    s.period_month,
    s.acquiring_device_type_calc,
    s.acquiring_device_type,
    s.trans_group,
    s.operation_type,
    s.token_status,
    s.mcc,
    s.is_acquiring_category,
    s.target_bank_name,
    s.source_bank_name,
    s.target_city,
    s.payment_system,
    s.is_contactless;

CREATE INDEX idx_mv_acq_dev_date ON mv_acquiring_device_transaction (report_date) NOLOGGING;
CREATE INDEX idx_mv_acq_dev_calc ON mv_acquiring_device_transaction (acquiring_device_type_calc, report_date) NOLOGGING;
CREATE INDEX idx_mv_acq_dev_bank ON mv_acquiring_device_transaction (target_bank_name, report_date) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_ACQUIRING_DEVICE_TRANSACTION', cascade => TRUE); END;
/


-- ============================================================================
-- 4. MV_CARD_DASHBOARD (Slide 3 - Card Dashboard)
-- Fix:
--   - TRIM(TO_CHAR(t.is_issuing)) = '1'
--   - is_contactless '1'/'0' normallaşdırılır
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_card_dashboard'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_card_dashboard'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_card_dashboard CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW mv_card_dashboard
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(8) */
    TRUNC(t.report_date, 'MM')                                           AS report_month,
    TRUNC(t.report_date, 'YYYY')                                         AS report_year,
    TRUNC(t.report_date, 'Q')                                            AS report_quarter,
    t.payment_system,
    t.is_issuing_category,
    t.operation_type,
    t.card_product_type_category,
    t.token_status,
    t.trans_group,
    t.target_bank_name,
    CASE
        WHEN LOWER(TRIM(TO_CHAR(t.is_contactless))) IN ('1', 'true', 'yes', 'y', 'contactless') THEN '1'
        ELSE '0'
    END                                                                  AS is_contactless,
    SUM(NVL(t.local_amount, 0))                                          AS total_amount,
    SUM(NVL(t.transaction_count, 0))                                     AS total_tx_count,
    MAX(NVL(t.local_amount, 0))                                          AS max_amount,
    SUM(NVL(t.settlement_amount, 0))                                     AS total_settlement_amount
FROM bi_transaction_masked t
WHERE TRIM(TO_CHAR(t.is_issuing)) = '1'
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
    CASE
        WHEN LOWER(TRIM(TO_CHAR(t.is_contactless))) IN ('1', 'true', 'yes', 'y', 'contactless') THEN '1'
        ELSE '0'
    END;

CREATE INDEX idx_mv_card_dash_month ON mv_card_dashboard (report_month) NOLOGGING;
CREATE INDEX idx_mv_card_dash_bank  ON mv_card_dashboard (target_bank_name, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_CARD_DASHBOARD', cascade => TRUE); END;
/


-- ============================================================================
-- 5. MV_CARD_PORTFOLIO (PPTX - Card Portfolio Dashboard)
-- Source: BI_MARKET_CARDS_MASKED
-- Fix:
--   - Eyni (report_month, bank_name) üçün yalnız ən son ETL_DATE snapshot-ı
--   - CardPortfolioRepository.AllowedDimensions-da tələb olunan
--     card_commercial_name sütunu daxil edilir
--   - region_name_clean və region_name normallaşdırılır
--   - prev_total_cards, status_3d, base_currency_name, mom_growth_pct, yoy_growth_pct
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_card_portfolio'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_card_portfolio'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_card_portfolio CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW mv_card_portfolio
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH latest_cards AS (
    SELECT c.*,
           DENSE_RANK() OVER (
               PARTITION BY TRUNC(c.report_month, 'MM'), c.bank_name
               ORDER BY c.etl_date DESC NULLS LAST
           ) AS etl_rnk
    FROM bi_market_cards_masked c
),
base_agg AS (
    SELECT
        TRUNC(report_month, 'MM')                                   AS report_month,
        bank_name,
        bank_branch_name,
        COALESCE(NULLIF(TRIM(region_name_clean), ''), region_name)  AS region_name,
        COALESCE(NULLIF(TRIM(region_name_clean), ''), region_name)  AS region_name_clean,
        payment_scheme,
        card_brand_name,
        card_category_name,
        product_type,
        card_product_name,
        card_commercial_name,
        exp_status,
        contactless_status,
        status_3d,
        base_currency_name,
        SUM(NVL(count_card, 0))                                     AS total_cards
    FROM latest_cards
    WHERE etl_rnk = 1
    GROUP BY
        TRUNC(report_month, 'MM'),
        bank_name,
        bank_branch_name,
        COALESCE(NULLIF(TRIM(region_name_clean), ''), region_name),
        payment_scheme,
        card_brand_name,
        card_category_name,
        product_type,
        card_product_name,
        card_commercial_name,
        exp_status,
        contactless_status,
        status_3d,
        base_currency_name
)
SELECT
    b.report_month,
    b.bank_name,
    b.bank_branch_name,
    b.region_name,
    b.region_name_clean,
    b.payment_scheme,
    b.card_brand_name,
    b.card_category_name,
    b.product_type,
    b.card_product_name,
    b.card_commercial_name,
    b.exp_status,
    b.contactless_status,
    b.status_3d,
    b.base_currency_name,
    b.total_cards,
    LAG(b.total_cards, 1) OVER (
        PARTITION BY b.bank_name, b.bank_branch_name, b.region_name_clean,
                     b.payment_scheme, b.card_brand_name, b.card_category_name,
                     b.product_type, b.card_product_name, b.card_commercial_name,
                     b.exp_status, b.contactless_status, b.status_3d, b.base_currency_name
        ORDER BY b.report_month
    ) AS prev_total_cards,
    ROUND(
        (b.total_cards - LAG(b.total_cards, 1) OVER (
            PARTITION BY b.bank_name, b.bank_branch_name, b.region_name_clean,
                         b.payment_scheme, b.card_brand_name, b.card_category_name,
                         b.product_type, b.card_product_name, b.card_commercial_name,
                         b.exp_status, b.contactless_status, b.status_3d, b.base_currency_name
            ORDER BY b.report_month
        )) * 100.0
        / NULLIF(LAG(b.total_cards, 1) OVER (
            PARTITION BY b.bank_name, b.bank_branch_name, b.region_name_clean,
                         b.payment_scheme, b.card_brand_name, b.card_category_name,
                         b.product_type, b.card_product_name, b.card_commercial_name,
                         b.exp_status, b.contactless_status, b.status_3d, b.base_currency_name
            ORDER BY b.report_month
        ), 0),
    2) AS mom_growth_pct,
    ROUND(
        (b.total_cards - LAG(b.total_cards, 12) OVER (
            PARTITION BY b.bank_name, b.bank_branch_name, b.region_name_clean,
                         b.payment_scheme, b.card_brand_name, b.card_category_name,
                         b.product_type, b.card_product_name, b.card_commercial_name,
                         b.exp_status, b.contactless_status, b.status_3d, b.base_currency_name
            ORDER BY b.report_month
        )) * 100.0
        / NULLIF(LAG(b.total_cards, 12) OVER (
            PARTITION BY b.bank_name, b.bank_branch_name, b.region_name_clean,
                         b.payment_scheme, b.card_brand_name, b.card_category_name,
                         b.product_type, b.card_product_name, b.card_commercial_name,
                         b.exp_status, b.contactless_status, b.status_3d, b.base_currency_name
            ORDER BY b.report_month
        ), 0),
    2) AS yoy_growth_pct
FROM base_agg b;

CREATE INDEX idx_mv_card_port_month ON mv_card_portfolio (report_month) NOLOGGING;
CREATE INDEX idx_mv_card_port_bank  ON mv_card_portfolio (bank_name, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_CARD_PORTFOLIO', cascade => TRUE); END;
/


-- ============================================================================
-- 6. MV_DEVICES_BASE və MV_DEVICES_TREND (PPTX - Devices Portfolio Dashboard)
-- Source: BI_MARKET_DEVICES_MASKED
-- Fix:
--   - Eyni (TRUNC(report_date, 'MM'), bank_name) üzrə ən son (report_date, etl_date)
--     snapshot-ı götürülür (inventar count_dev cəmlənib şişməsin!)
--   - Həm share_pct_by_* sütunları, həm də prev_month_devices / mom_diff / mom_pct_change
--     daxil edilir
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_devices_trend'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_devices_trend'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_devices_trend CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW mv_devices_base'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW mv_devices_base'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE mv_devices_base CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW mv_devices_base
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH latest_devices AS (
    SELECT d.*,
           DENSE_RANK() OVER (
               PARTITION BY TRUNC(d.report_date, 'MM'), d.bank_name
               ORDER BY TRUNC(d.report_date, 'DD') DESC, d.etl_date DESC NULLS LAST
           ) AS snap_rnk
    FROM bi_market_devices_masked d
),
monthly_agg AS (
    SELECT
        TRUNC(report_date, 'MM')                                   AS report_month,
        bank_name,
        COALESCE(NULLIF(TRIM(region_name_clean), ''), region_name) AS region_name,
        COALESCE(NULLIF(TRIM(region_name_clean), ''), region_name) AS region_name_clean,
        mcc_name,
        retail_category,
        transaction_class,
        ctls_status,
        SUM(NVL(count_dev, 0))                                     AS total_devices
    FROM latest_devices
    WHERE snap_rnk = 1
    GROUP BY
        TRUNC(report_date, 'MM'),
        bank_name,
        COALESCE(NULLIF(TRIM(region_name_clean), ''), region_name),
        mcc_name,
        retail_category,
        transaction_class,
        ctls_status
)
SELECT
    m.report_month,
    m.bank_name,
    m.region_name,
    m.region_name_clean,
    m.mcc_name,
    m.retail_category,
    m.transaction_class,
    m.ctls_status,
    m.total_devices,
    SUM(m.total_devices) OVER (PARTITION BY m.report_month) AS grand_total_month,
    ROUND(
        SUM(m.total_devices) OVER (PARTITION BY m.report_month, m.bank_name) * 100.0
        / NULLIF(SUM(m.total_devices) OVER (PARTITION BY m.report_month), 0),
    2) AS share_pct_by_bank,
    ROUND(
        SUM(m.total_devices) OVER (PARTITION BY m.report_month, m.region_name_clean) * 100.0
        / NULLIF(SUM(m.total_devices) OVER (PARTITION BY m.report_month), 0),
    2) AS share_pct_by_region,
    ROUND(
        SUM(m.total_devices) OVER (PARTITION BY m.report_month, m.mcc_name) * 100.0
        / NULLIF(SUM(m.total_devices) OVER (PARTITION BY m.report_month), 0),
    2) AS share_pct_by_mcc,
    ROUND(
        SUM(m.total_devices) OVER (PARTITION BY m.report_month, m.retail_category) * 100.0
        / NULLIF(SUM(m.total_devices) OVER (PARTITION BY m.report_month), 0),
    2) AS share_pct_by_retail_cat,
    NVL(LAG(m.total_devices, 1) OVER (
        PARTITION BY m.bank_name, m.region_name_clean,
                     m.mcc_name, m.retail_category, m.transaction_class, m.ctls_status
        ORDER BY m.report_month
    ), 0) AS prev_month_devices,
    m.total_devices - NVL(LAG(m.total_devices, 1) OVER (
        PARTITION BY m.bank_name, m.region_name_clean,
                     m.mcc_name, m.retail_category, m.transaction_class, m.ctls_status
        ORDER BY m.report_month
    ), 0) AS mom_diff,
    ROUND(
        (m.total_devices - LAG(m.total_devices, 1) OVER (
            PARTITION BY m.bank_name, m.region_name_clean,
                         m.mcc_name, m.retail_category, m.transaction_class, m.ctls_status
            ORDER BY m.report_month
        )) * 100.0
        / NULLIF(LAG(m.total_devices, 1) OVER (
            PARTITION BY m.bank_name, m.region_name_clean,
                         m.mcc_name, m.retail_category, m.transaction_class, m.ctls_status
            ORDER BY m.report_month
        ), 0),
    2) AS mom_pct_change
FROM monthly_agg m;

CREATE INDEX idx_mv_dev_base_month ON mv_devices_base (report_month) NOLOGGING;
CREATE INDEX idx_mv_dev_base_bank  ON mv_devices_base (bank_name, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_DEVICES_BASE', cascade => TRUE); END;
/

CREATE MATERIALIZED VIEW mv_devices_trend
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT
    report_month,
    bank_name,
    region_name,
    region_name_clean,
    mcc_name,
    retail_category,
    transaction_class,
    ctls_status,
    total_devices,
    prev_month_devices,
    mom_diff,
    mom_pct_change
FROM mv_devices_base;

CREATE INDEX idx_mv_dev_trend_month ON mv_devices_trend (report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'MV_DEVICES_TREND', cascade => TRUE); END;
/


-- ============================================================================
-- 7. PG_MV_REQ4_AZ_MAP (PPTX - Azərbaycan Xəritəsi)
-- Biznes Qaydası (Azərbaycan_xəritəsi.pptx):
--   - Yalnız ekvayrinq əməliyyatları: TRIM(TO_CHAR(t.is_acquiring)) = '1'
--   - Azərbaycan daxili əməliyyatlar: target_country AZ / NULL
--   - Şəhər bölgüsü TARGET_CITY əsasında aparılır ('Baku'/'Baki' -> 'Baki' və s.)
--     və AzMapRepository ilə tam uyğunluq üçün SOURCE_CITY / SOURCE_CITY_CLEAN
--     sütun adları ilə çıxarılır
--   - Məbləğ: SUM(NVL(t.local_amount, 0))
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req4_az_map'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW pg_mv_req4_az_map'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE pg_mv_req4_az_map CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req4_az_map
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT
    s.report_month,
    s.bank_name,
    s.city_norm AS source_city,
    s.city_norm AS source_city_clean,
    CASE
        WHEN UPPER(s.city_norm) = 'BAKI' THEN 'BAKI'
        ELSE 'REGION'
    END         AS source_city_category,
    SUM(s.local_amount)      AS total_amount,
    SUM(s.transaction_count) AS total_count,
    COUNT(*)                 AS cnt
FROM (
    SELECT /*+ PARALLEL(8) */
        TRUNC(t.report_date, 'MM')                                         AS report_month,
        COALESCE(NULLIF(TRIM(t.target_bank_name), ''), t.source_bank_name) AS bank_name,
        CASE
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('BAKU', 'BAKI', 'BAKİ') THEN 'Baki'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('GANJA', 'GANCA', 'GƏNCƏ') THEN 'Ganca'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('SUMGAYIT', 'SUMQAYIT') THEN 'Sumqayit'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('MINGACHEVIR', 'MINGACEVIR', 'MİNGƏÇEVİR') THEN 'Mingacevir'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('SHIRVAN', 'SIRVAN', 'ŞİRVAN') THEN 'Sirvan'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('NAKHCHIVAN', 'NAXCIVAN', 'NAXÇIVAN') THEN 'Naxcivan'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('SHAKI', 'SAKI', 'ŞƏKİ') THEN 'Saki'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('SHUSHA', 'SUSA', 'ŞUŞA') THEN 'Susa'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('LANKARAN', 'LƏNKƏRAN') THEN 'Lankaran'
            WHEN UPPER(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city))) IN ('YEVLAKH', 'YEVLAX') THEN 'Yevlakh'
            ELSE NVL(TRIM(COALESCE(NULLIF(TRIM(t.target_city), ''), NULLIF(TRIM(t.source_city_clean), ''), t.source_city)), 'NAMƏLUM')
        END                                                                AS city_norm,
        NVL(t.local_amount, 0)                                             AS local_amount,
        NVL(t.transaction_count, 0)                                        AS transaction_count
    FROM bi_transaction_masked t
    WHERE TRIM(TO_CHAR(t.is_acquiring)) = '1'
      AND (t.target_country IS NULL
           OR UPPER(TRIM(t.target_country)) IN ('AZ', 'AZE', '031', '31', 'AZERBAIJAN', 'AZƏRBAYCAN')
           OR UPPER(TRIM(t.target_country_category)) IN ('DOMESTIC', 'LOCAL', 'AZERBAIJAN'))
) s
WHERE UPPER(s.city_norm) NOT IN ('OTHER', 'ISTANBUL', 'MOSCOW', 'LONDON')
GROUP BY
    s.report_month,
    s.bank_name,
    s.city_norm;

CREATE INDEX idx_mv_req4_month ON pg_mv_req4_az_map (report_month) NOLOGGING;
CREATE INDEX idx_mv_req4_city  ON pg_mv_req4_az_map (source_city_clean, report_month) NOLOGGING;
CREATE INDEX idx_mv_req4_bank  ON pg_mv_req4_az_map (bank_name, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'PG_MV_REQ4_AZ_MAP', cascade => TRUE); END;
/


-- ============================================================================
-- 8. PG_MV_REQ5_WORLD_MAP (PPTX - Dünya Xəritəsi)
-- Biznes Qaydası (Dünya_xəritəsi.pptx):
--   - Yalnız emissiya əməliyyatları: TRIM(TO_CHAR(t.is_issuing)) = '1'
--   - Məbləğ: SUM(NVL(t.local_amount, 0))
--   - Əsl source_country və target_country saxlanır
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req5_world_map'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW pg_mv_req5_world_map'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE pg_mv_req5_world_map CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req5_world_map
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(8) */
    TRUNC(t.report_date, 'MM')                                                                AS report_month,
    t.source_bank_name                                                                        AS bank_name,
    COALESCE(NULLIF(TRIM(t.source_country), ''), 'OTHER')                                     AS source_country,
    COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER')                            AS source_country_category,
    COALESCE(NULLIF(TRIM(t.target_country), ''), 'Azerbaijan')                                AS target_country,
    COALESCE(NULLIF(TRIM(t.target_country_category), ''), 'OTHER')                            AS target_country_category,
    COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER')                            AS country_category,
    COALESCE(NULLIF(TRIM(t.source_city_clean), ''), NULLIF(TRIM(t.source_city), ''), 'OTHER') AS city_clean,
    t.payment_system,
    NVL(TRIM(TO_CHAR(t.is_issuing)), '0')                                                     AS is_issuing,
    NVL(TRIM(TO_CHAR(t.is_acquiring)), '0')                                                   AS is_acquiring,
    SUM(NVL(t.local_amount, 0))                                                               AS total_amount,
    SUM(NVL(t.transaction_count, 0))                                                          AS total_count,
    COUNT(*)                                                                                  AS cnt
FROM bi_transaction_masked t
WHERE TRIM(TO_CHAR(t.is_issuing)) = '1'
GROUP BY
    TRUNC(t.report_date, 'MM'),
    t.source_bank_name,
    COALESCE(NULLIF(TRIM(t.source_country), ''), 'OTHER'),
    COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER'),
    COALESCE(NULLIF(TRIM(t.target_country), ''), 'Azerbaijan'),
    COALESCE(NULLIF(TRIM(t.target_country_category), ''), 'OTHER'),
    COALESCE(NULLIF(TRIM(t.source_city_clean), ''), NULLIF(TRIM(t.source_city), ''), 'OTHER'),
    t.payment_system,
    NVL(TRIM(TO_CHAR(t.is_issuing)), '0'),
    NVL(TRIM(TO_CHAR(t.is_acquiring)), '0');

CREATE INDEX idx_mv_req5_month   ON pg_mv_req5_world_map (report_month) NOLOGGING;
CREATE INDEX idx_mv_req5_country ON pg_mv_req5_world_map (source_country, target_country) NOLOGGING;
CREATE INDEX idx_mv_req5_bank    ON pg_mv_req5_world_map (bank_name, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'PG_MV_REQ5_WORLD_MAP', cascade => TRUE); END;
/


-- ============================================================================
-- 9. PG_MV_REQ8_SECTOR_SPEND (Sektorlar üzrə Xərcləmə)
-- QEYD: PG_MV_REQ7_BENCHMARK və PG_MV_REQ1_FORECAST_INPUT aşağıda məhz bu
--       MV-dən oxuyur ki, 600M+ sətir təkrar skan edilməsin!
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req8_sector_spend'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW pg_mv_req8_sector_spend'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE pg_mv_req8_sector_spend CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req8_sector_spend
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH sector_agg AS (
    SELECT /*+ PARALLEL(8) */
        TRUNC(t.report_date, 'MM')                    AS report_month,
        t.source_bank_name                            AS bank_name,
        NVL(TRIM(t.mcc_group), 'OTHER')               AS mcc_group,
        NVL(TRIM(TO_CHAR(t.mcc)), '0000')             AS mcc,
        NVL(TRIM(t.source_city), 'NAMƏLUM')           AS source_city,
        NVL(TRIM(t.source_city_category), 'OTHER')    AS source_city_category,
        NVL(TRIM(t.source_channel), 'OTHER')          AS source_channel,
        t.operation_type,
        NVL(TRIM(t.source_country_category), 'OTHER') AS source_country_category,
        NVL(TRIM(TO_CHAR(t.is_acquiring)), '0')       AS is_acquiring,
        NVL(TRIM(TO_CHAR(t.is_issuing)), '0')         AS is_issuing,
        SUM(NVL(t.local_amount, 0))                   AS total_amount,
        SUM(NVL(t.transaction_count, 0))              AS total_count,
        SUM(CASE WHEN UPPER(TRIM(t.source_country_category)) = 'DOMESTIC'
                 THEN NVL(t.local_amount, 0) ELSE 0 END) AS total_local_amount
    FROM bi_transaction_masked t
    GROUP BY
        TRUNC(t.report_date, 'MM'),
        t.source_bank_name,
        NVL(TRIM(t.mcc_group), 'OTHER'),
        NVL(TRIM(TO_CHAR(t.mcc)), '0000'),
        NVL(TRIM(t.source_city), 'NAMƏLUM'),
        NVL(TRIM(t.source_city_category), 'OTHER'),
        NVL(TRIM(t.source_channel), 'OTHER'),
        t.operation_type,
        NVL(TRIM(t.source_country_category), 'OTHER'),
        NVL(TRIM(TO_CHAR(t.is_acquiring)), '0'),
        NVL(TRIM(TO_CHAR(t.is_issuing)), '0')
)
SELECT
    s.report_month,
    s.bank_name,
    s.mcc_group,
    s.mcc,
    s.source_city,
    s.source_city_category,
    s.source_channel,
    s.operation_type,
    s.source_country_category,
    s.is_acquiring,
    s.is_issuing,
    s.total_amount,
    s.total_count,
    s.total_local_amount,
    ROUND(s.total_amount / NULLIF(s.total_count, 0), 2) AS avg_ticket,
    ROUND(s.total_local_amount * 100.0 / NULLIF(s.total_amount, 0), 2) AS local_ratio_pct,
    ROUND(s.total_amount * 100.0 / NULLIF(SUM(s.total_amount) OVER (PARTITION BY s.report_month), 0), 4) AS sector_market_share_pct
FROM sector_agg s;

CREATE INDEX idx_mv_req8_month    ON pg_mv_req8_sector_spend (report_month) NOLOGGING;
CREATE INDEX idx_mv_req8_mcc      ON pg_mv_req8_sector_spend (mcc_group, report_month) NOLOGGING;
CREATE INDEX idx_mv_req8_bank_mcc ON pg_mv_req8_sector_spend (bank_name, mcc_group, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'PG_MV_REQ8_SECTOR_SPEND', cascade => TRUE); END;
/


-- ============================================================================
-- 10. PG_MV_REQ7_BENCHMARK (Bazar Müqayisəsi / Market Benchmark)
-- Optimizasiya (600M+):
--   - trans_monthly 600M-lıq BI_TRANSACTION_MASKED əvəzinə birbaşa hazır
--     PG_MV_REQ8_SECTOR_SPEND MV-dən oxuyur (~1 saniyə!)
--   - Kart sayında latest ETL_DATE deduplikasiyası tətbiq olunur
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req7_benchmark'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW pg_mv_req7_benchmark'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE pg_mv_req7_benchmark CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req7_benchmark
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH trans_monthly AS (
    SELECT
        s.report_month,
        s.bank_name,
        SUM(s.total_amount) AS bank_trans_amount,
        SUM(s.total_count)  AS bank_trans_count
    FROM pg_mv_req8_sector_spend s
    WHERE s.bank_name IS NOT NULL
    GROUP BY s.report_month, s.bank_name
),
market_trans_monthly AS (
    SELECT
        report_month,
        SUM(bank_trans_amount) AS market_total_amount
    FROM trans_monthly
    GROUP BY report_month
),
latest_cards AS (
    SELECT c.*,
           DENSE_RANK() OVER (
               PARTITION BY TRUNC(c.report_month, 'MM'), c.bank_name
               ORDER BY c.etl_date DESC NULLS LAST
           ) AS etl_rnk
    FROM bi_market_cards_masked c
),
cards_monthly AS (
    SELECT
        TRUNC(c.report_month, 'MM')                                                    AS report_month,
        c.bank_name,
        NVL(COALESCE(NULLIF(TRIM(c.region_name_clean), ''), TRIM(c.region_name)), 'OTHER') AS region_name_clean,
        NVL(TRIM(c.card_brand_name), 'OTHER')                                          AS card_brand_name,
        NVL(TRIM(c.product_type), 'OTHER')                                             AS product_type,
        SUM(NVL(c.count_card, 0))                                                      AS bank_card_count
    FROM latest_cards c
    WHERE c.etl_rnk = 1
      AND c.bank_name IS NOT NULL
    GROUP BY
        TRUNC(c.report_month, 'MM'),
        c.bank_name,
        NVL(COALESCE(NULLIF(TRIM(c.region_name_clean), ''), TRIM(c.region_name)), 'OTHER'),
        NVL(TRIM(c.card_brand_name), 'OTHER'),
        NVL(TRIM(c.product_type), 'OTHER')
),
joined_data AS (
    SELECT
        COALESCE(tm.report_month, cm.report_month) AS report_month,
        COALESCE(tm.bank_name, cm.bank_name)       AS bank_name,
        NVL(cm.region_name_clean, 'OTHER')         AS region_name_clean,
        NVL(cm.card_brand_name, 'OTHER')           AS card_brand_name,
        NVL(cm.product_type, 'OTHER')              AS product_type,
        NVL(tm.bank_trans_amount, 0)               AS bank_trans_amount,
        NVL(tm.bank_trans_count, 0)                AS bank_trans_count,
        NVL(cm.bank_card_count, 0)                 AS bank_card_count
    FROM trans_monthly tm
    FULL OUTER JOIN cards_monthly cm
        ON tm.report_month = cm.report_month
       AND tm.bank_name    = cm.bank_name
)
SELECT
    j.report_month,
    j.bank_name,
    j.region_name_clean,
    j.card_brand_name,
    j.product_type,
    j.bank_trans_amount,
    j.bank_trans_count,
    j.bank_card_count,
    NVL(mt.market_total_amount, 0) AS market_total_amount,
    SUM(j.bank_card_count) OVER (
        PARTITION BY j.report_month, j.region_name_clean, j.card_brand_name
    ) AS market_total_cards,
    ROUND(j.bank_trans_amount * 100.0 / NULLIF(mt.market_total_amount, 0), 4) AS market_share_amount_pct,
    ROUND(j.bank_card_count * 100.0 / NULLIF(
        SUM(j.bank_card_count) OVER (PARTITION BY j.report_month, j.region_name_clean, j.card_brand_name), 0
    ), 4) AS market_share_cards_pct,
    DENSE_RANK() OVER (
        PARTITION BY j.report_month
        ORDER BY j.bank_trans_amount DESC NULLS LAST
    ) AS bank_rank
FROM joined_data j
LEFT JOIN market_trans_monthly mt
    ON mt.report_month = j.report_month;

CREATE INDEX idx_mv_req7_month ON pg_mv_req7_benchmark (report_month) NOLOGGING;
CREATE INDEX idx_mv_req7_bank  ON pg_mv_req7_benchmark (bank_name, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'PG_MV_REQ7_BENCHMARK', cascade => TRUE); END;
/


-- ============================================================================
-- 11. PG_MV_REQ1_FORECAST_INPUT (PPTX - Proqnoz Tətbiqi / Forecasting)
-- Optimizasiya (600M+):
--   - trans_agg 600M-lıq BI_TRANSACTION_MASKED əvəzinə birbaşa hazır
--     PG_MV_REQ8_SECTOR_SPEND MV-dən oxuyur (~1 saniyə!)
--   - BI_MARKET_CARDS_MASKED üzrə latest ETL_DATE deduplikasiyası
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req1_forecast_input'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP VIEW pg_mv_req1_forecast_input'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE pg_mv_req1_forecast_input CASCADE CONSTRAINTS PURGE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req1_forecast_input
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
WITH trans_agg AS (
    SELECT
        s.report_month,
        s.bank_name,
        s.mcc_group,
        SUM(s.total_amount) AS total_amount,
        SUM(s.total_count)  AS total_count
    FROM pg_mv_req8_sector_spend s
    WHERE s.bank_name IS NOT NULL
    GROUP BY
        s.report_month,
        s.bank_name,
        s.mcc_group
),
latest_cards AS (
    SELECT c.*,
           DENSE_RANK() OVER (
               PARTITION BY TRUNC(c.report_month, 'MM'), c.bank_name
               ORDER BY c.etl_date DESC NULLS LAST
           ) AS etl_rnk
    FROM bi_market_cards_masked c
),
card_agg AS (
    SELECT
        TRUNC(report_month, 'MM') AS report_month,
        bank_name,
        SUM(NVL(count_card, 0))   AS total_cards
    FROM latest_cards
    WHERE etl_rnk = 1
    GROUP BY TRUNC(report_month, 'MM'), bank_name
),
base AS (
    SELECT
        t.report_month,
        t.bank_name,
        t.mcc_group,
        t.total_amount,
        t.total_count,
        NVL(c.total_cards, 0) AS total_cards
    FROM trans_agg t
    LEFT JOIN card_agg c
        ON c.report_month = t.report_month
       AND c.bank_name    = t.bank_name
)
SELECT
    b.report_month,
    b.bank_name,
    b.mcc_group,
    b.total_amount,
    b.total_count,
    b.total_cards,
    LAG(b.total_amount, 1)  OVER (PARTITION BY b.bank_name, b.mcc_group ORDER BY b.report_month) AS amount_lag1,
    LAG(b.total_amount, 3)  OVER (PARTITION BY b.bank_name, b.mcc_group ORDER BY b.report_month) AS amount_lag3,
    LAG(b.total_amount, 12) OVER (PARTITION BY b.bank_name, b.mcc_group ORDER BY b.report_month) AS amount_lag12,
    ROUND(AVG(b.total_amount) OVER (
        PARTITION BY b.bank_name, b.mcc_group ORDER BY b.report_month ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
    ), 2) AS rolling_avg_3m,
    ROUND(AVG(b.total_amount) OVER (
        PARTITION BY b.bank_name, b.mcc_group ORDER BY b.report_month ROWS BETWEEN 5 PRECEDING AND CURRENT ROW
    ), 2) AS rolling_avg_6m,
    EXTRACT(MONTH FROM b.report_month) AS season_month,
    ROW_NUMBER() OVER (PARTITION BY b.bank_name, b.mcc_group ORDER BY b.report_month) AS time_index,
    ROUND(b.total_amount / NULLIF(b.total_count, 0), 2) AS avg_ticket,
    ROUND(b.total_amount / NULLIF(b.total_cards, 0), 2) AS amount_per_card
FROM base b;

CREATE INDEX idx_mv_req1_month    ON pg_mv_req1_forecast_input (report_month) NOLOGGING;
CREATE INDEX idx_mv_req1_bank_mcc ON pg_mv_req1_forecast_input (bank_name, mcc_group, report_month) NOLOGGING;
BEGIN DBMS_STATS.GATHER_TABLE_STATS(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'), 'PG_MV_REQ1_FORECAST_INPUT', cascade => TRUE); END;
/


-- ============================================================================
-- 12. GÜNLÜK / GECƏ ETL REFRESH PROSEDURU (Yalnız Job üçün - Deployda işlətməyin!)
-- QEYD: Yuxarıda BUILD IMMEDIATE artıq MV-ləri 1 dəfə doldurub.
--       Gecə ETL Job-unda asılılıq sırası VACİBDİR:
--         1) MV_DEVICES_BASE         -> MV_DEVICES_TREND
--         2) PG_MV_REQ8_SECTOR_SPEND -> PG_MV_REQ7_BENCHMARK və PG_MV_REQ1_FORECAST_INPUT
-- ============================================================================
/*
BEGIN
    -- Əvvəl müstəqil və baza MV-lər
    DBMS_MVIEW.REFRESH('MV_SUMMARY_TRANSACTION',          'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('MV_TRANSACTION_SUMMARY',          'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('MV_ACQUIRING_DEVICE_TRANSACTION', 'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('MV_CARD_DASHBOARD',               'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('MV_CARD_PORTFOLIO',               'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('MV_DEVICES_BASE',                 'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('PG_MV_REQ4_AZ_MAP',               'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('PG_MV_REQ5_WORLD_MAP',            'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('PG_MV_REQ8_SECTOR_SPEND',         'C', atomic_refresh => FALSE);

    -- Sonra baza MV-lərdən oxuyan törəmə MV-lər (saniyələr ərzində bitir)
    DBMS_MVIEW.REFRESH('MV_DEVICES_TREND',                'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('PG_MV_REQ7_BENCHMARK',            'C', atomic_refresh => FALSE);
    DBMS_MVIEW.REFRESH('PG_MV_REQ1_FORECAST_INPUT',       'C', atomic_refresh => FALSE);
END;
/
*/


-- ============================================================================
-- 13. İCAZƏLƏRİN (GRANT) AVTOMATİK BƏRPASI VƏ YOXLANMASI
-- ============================================================================
DECLARE
    v_sql VARCHAR2(1000);
BEGIN
    -- 13.1. Bölmə 0-da yadda saxlanılmış bütün mövcud GRANT-ları bərpa et
    FOR r IN (SELECT grantee, table_name, privilege, grantable FROM mv_patch_grants_bkp) LOOP
        BEGIN
            v_sql := 'GRANT ' || r.privilege || ' ON ' || r.table_name || ' TO "' || r.grantee || '"'
                     || CASE WHEN r.grantable = 'YES' THEN ' WITH GRANT OPTION' ELSE '' END;
            EXECUTE IMMEDIATE v_sql;
        EXCEPTION
            WHEN OTHERS THEN NULL;
        END;
    END LOOP;

    -- 13.2. REPORTAPPUSER mövcuddursa, bütün 12 MV üzrə SELECT icazəsi ver
    FOR t IN (
        SELECT column_value AS mv_name
        FROM TABLE(sys.odcivarchar2list(
            'MV_SUMMARY_TRANSACTION', 'MV_TRANSACTION_SUMMARY', 'MV_ACQUIRING_DEVICE_TRANSACTION',
            'MV_CARD_DASHBOARD', 'MV_CARD_PORTFOLIO', 'MV_DEVICES_BASE', 'MV_DEVICES_TREND',
            'PG_MV_REQ4_AZ_MAP', 'PG_MV_REQ5_WORLD_MAP', 'PG_MV_REQ8_SECTOR_SPEND',
            'PG_MV_REQ7_BENCHMARK', 'PG_MV_REQ1_FORECAST_INPUT'
        ))
    ) LOOP
        BEGIN
            EXECUTE IMMEDIATE 'GRANT SELECT ON ' || t.mv_name || ' TO REPORTAPPUSER';
        EXCEPTION
            WHEN OTHERS THEN NULL; -- REPORTAPPUSER yoxdursa və ya özüdürsə xəta vermə
        END;
    END LOOP;

    -- 13.3. Müvəqqəti backup cədvəlini təmizlə
    BEGIN
        EXECUTE IMMEDIATE 'DROP TABLE mv_patch_grants_bkp PURGE';
    EXCEPTION
        WHEN OTHERS THEN NULL;
    END;
END;
/

-- 14. POST-DEPLOY YOXLAMA SORĞUSU
SELECT
    mview_name,
    compile_state,
    staleness,
    TO_CHAR(last_refresh_date, 'YYYY-MM-DD HH24:MI:SS') AS refreshed_at
FROM all_mviews
WHERE owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
  AND mview_name IN (
      'MV_SUMMARY_TRANSACTION', 'MV_TRANSACTION_SUMMARY', 'MV_ACQUIRING_DEVICE_TRANSACTION',
      'MV_CARD_DASHBOARD', 'MV_CARD_PORTFOLIO', 'MV_DEVICES_BASE', 'MV_DEVICES_TREND',
      'PG_MV_REQ4_AZ_MAP', 'PG_MV_REQ5_WORLD_MAP', 'PG_MV_REQ8_SECTOR_SPEND',
      'PG_MV_REQ7_BENCHMARK', 'PG_MV_REQ1_FORECAST_INPUT'
  )
ORDER BY mview_name;

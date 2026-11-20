-- ============================================================================
-- PG_MV_REQ4_AZ_MAP və PG_MV_REQ5_WORLD_MAP Materialized View Yeniləmə Skripti
-- Bütün tələb olunan yeni filter sütunları + indekslər daxildir.
-- ============================================================================

-- ============================================================================
-- 1. AZƏRBAYCAN XƏRİTƏSİ: PG_MV_REQ4_AZ_MAP
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req4_az_map'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req4_az_map
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
USING NO INDEX
REFRESH COMPLETE ON DEMAND
AS
SELECT
    s.report_month,
    CAST(SUBSTR(s.bank_name, 1, 200) AS VARCHAR2(200 CHAR))             AS bank_name,
    CAST(SUBSTR(s.city_norm, 1, 150) AS VARCHAR2(150 CHAR))             AS source_city,
    CAST(SUBSTR(s.city_norm, 1, 150) AS VARCHAR2(150 CHAR))             AS source_city_clean,
    CASE
        WHEN UPPER(s.city_norm) = 'BAKI' THEN 'BAKI'
        ELSE 'REGION'
    END                                                                 AS source_city_category,
    CAST(SUBSTR(s.acquiring_device_type, 1, 100) AS VARCHAR2(100 CHAR)) AS acquiring_device_type,
    CAST(SUBSTR(s.trans_group, 1, 100) AS VARCHAR2(100 CHAR))           AS trans_group,
    CAST(SUBSTR(s.operation_type, 1, 100) AS VARCHAR2(100 CHAR))        AS operation_type,
    CAST(SUBSTR(s.token_status, 1, 100) AS VARCHAR2(100 CHAR))          AS token_status,
    CAST(SUBSTR(TO_CHAR(s.mcc), 1, 50) AS VARCHAR2(50 CHAR))            AS mcc,
    CAST(SUBSTR(s.is_acquiring_category, 1, 100) AS VARCHAR2(100 CHAR)) AS is_acquiring_category,
    CAST(SUBSTR(s.target_bank_name, 1, 200) AS VARCHAR2(200 CHAR))      AS target_bank_name,
    CAST(SUBSTR(s.source_bank_name, 1, 200) AS VARCHAR2(200 CHAR))      AS source_bank_name,
    CAST(SUBSTR(s.city_norm, 1, 150) AS VARCHAR2(150 CHAR))             AS target_city,
    CAST(SUBSTR(s.payment_system, 1, 100) AS VARCHAR2(100 CHAR))        AS payment_system,
    SUM(s.local_amount)                                                 AS total_amount,
    SUM(s.transaction_count)                                            AS total_count,
    COUNT(*)                                                            AS cnt
FROM (
    SELECT /*+ PARALLEL(8) */
        TRUNC(t.report_date, 'MM') AS report_month,
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
        END                        AS city_norm,
        t.acquiring_device_type,
        t.trans_group,
        t.operation_type,
        t.token_status,
        t.mcc,
        t.is_acquiring_category,
        t.target_bank_name,
        t.source_bank_name,
        t.payment_system,
        NVL(t.local_amount, 0)     AS local_amount,
        NVL(t.transaction_count, 0) AS transaction_count
    FROM bi_transaction_masked t
    WHERE TRIM(TO_CHAR(t.is_acquiring)) = '1'
      AND (t.target_country IS NULL
           OR UPPER(TRIM(t.target_country)) IN ('AZ', 'AZE', '031', '31', 'AZERBAIJAN', 'AZƏRBAYCAN')
           OR UPPER(TRIM(t.target_country_category)) IN ('DOMESTIC', 'LOCAL', 'AZERBAIJAN'))
) s
WHERE UPPER(s.city_norm) NOT IN ('OTHER', 'ISTANBUL', 'MOSCOW', 'LONDON')
GROUP BY
    s.report_month,
    CAST(SUBSTR(s.bank_name, 1, 200) AS VARCHAR2(200 CHAR)),
    CAST(SUBSTR(s.city_norm, 1, 150) AS VARCHAR2(150 CHAR)),
    CASE WHEN UPPER(s.city_norm) = 'BAKI' THEN 'BAKI' ELSE 'REGION' END,
    CAST(SUBSTR(s.acquiring_device_type, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(s.trans_group, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(s.operation_type, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(s.token_status, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(TO_CHAR(s.mcc), 1, 50) AS VARCHAR2(50 CHAR)),
    CAST(SUBSTR(s.is_acquiring_category, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(s.target_bank_name, 1, 200) AS VARCHAR2(200 CHAR)),
    CAST(SUBSTR(s.source_bank_name, 1, 200) AS VARCHAR2(200 CHAR)),
    CAST(SUBSTR(s.payment_system, 1, 100) AS VARCHAR2(100 CHAR));

CREATE INDEX idx_mv_azmap_month       ON pg_mv_req4_az_map (REPORT_MONTH) NOLOGGING;
CREATE INDEX idx_mv_azmap_src_bank    ON pg_mv_req4_az_map (SOURCE_BANK_NAME) NOLOGGING;
CREATE INDEX idx_mv_azmap_tgt_bank    ON pg_mv_req4_az_map (TARGET_BANK_NAME) NOLOGGING;
CREATE INDEX idx_mv_azmap_pay_sys     ON pg_mv_req4_az_map (PAYMENT_SYSTEM) NOLOGGING;


-- ============================================================================
-- 2. DÜNYA XƏRİTƏSİ: PG_MV_REQ5_WORLD_MAP
-- ============================================================================
BEGIN EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW pg_mv_req5_world_map'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -12003 THEN RAISE; END IF; END;
/

CREATE MATERIALIZED VIEW pg_mv_req5_world_map
PCTFREE 0 NOLOGGING
BUILD IMMEDIATE
USING NO INDEX
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(8) */
    TRUNC(t.report_date, 'MM')                                                                        AS report_month,
    CAST(SUBSTR(t.source_bank_name, 1, 200) AS VARCHAR2(200 CHAR))                                    AS bank_name,
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_country), ''), 'OTHER'), 1, 150) AS VARCHAR2(150 CHAR)) AS source_country,
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER'), 1, 100) AS VARCHAR2(100 CHAR)) AS source_country_category,
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.target_country), ''), 'Azerbaijan'), 1, 150) AS VARCHAR2(150 CHAR)) AS target_country,
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.target_country_category), ''), 'OTHER'), 1, 100) AS VARCHAR2(100 CHAR)) AS target_country_category,
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER'), 1, 100) AS VARCHAR2(100 CHAR)) AS country_category,
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_city_clean), ''), NULLIF(TRIM(t.source_city), ''), 'OTHER'), 1, 150) AS VARCHAR2(150 CHAR)) AS city_clean,
    CAST(SUBSTR(t.payment_system, 1, 100) AS VARCHAR2(100 CHAR))                                      AS payment_system,
    NVL(TRIM(TO_CHAR(t.is_issuing)), '0')                                                             AS is_issuing,
    NVL(TRIM(TO_CHAR(t.is_acquiring)), '0')                                                           AS is_acquiring,
    CAST(SUBSTR(t.is_issuing_category, 1, 100) AS VARCHAR2(100 CHAR))                                 AS is_issuing_category,
    CAST(SUBSTR(t.card_product_type_category, 1, 100) AS VARCHAR2(100 CHAR))                          AS card_product_type_category,
    CAST(SUBSTR(t.card_account_currency, 1, 50) AS VARCHAR2(50 CHAR))                                 AS card_account_currency,
    CAST(SUBSTR(t.source_bank_name, 1, 200) AS VARCHAR2(200 CHAR))                                    AS source_bank_name,
    CAST(SUBSTR(t.acquiring_device_type, 1, 100) AS VARCHAR2(100 CHAR))                               AS acquiring_device_type,
    CAST(SUBSTR(t.trans_group, 1, 100) AS VARCHAR2(100 CHAR))                                         AS trans_group,
    CAST(SUBSTR(t.operation_type, 1, 100) AS VARCHAR2(100 CHAR))                                      AS operation_type,
    CAST(SUBSTR(t.token_status, 1, 100) AS VARCHAR2(100 CHAR))                                        AS token_status,
    SUM(NVL(t.local_amount, 0))                                                                       AS total_amount,
    SUM(NVL(t.transaction_count, 0))                                                                  AS total_count,
    COUNT(*)                                                                                          AS cnt
FROM bi_transaction_masked t
WHERE TRIM(TO_CHAR(t.is_issuing)) = '1'
GROUP BY
    TRUNC(t.report_date, 'MM'),
    CAST(SUBSTR(t.source_bank_name, 1, 200) AS VARCHAR2(200 CHAR)),
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_country), ''), 'OTHER'), 1, 150) AS VARCHAR2(150 CHAR)),
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER'), 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.target_country), ''), 'Azerbaijan'), 1, 150) AS VARCHAR2(150 CHAR)),
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.target_country_category), ''), 'OTHER'), 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(COALESCE(NULLIF(TRIM(t.source_city_clean), ''), NULLIF(TRIM(t.source_city), ''), 'OTHER'), 1, 150) AS VARCHAR2(150 CHAR)),
    CAST(SUBSTR(t.payment_system, 1, 100) AS VARCHAR2(100 CHAR)),
    NVL(TRIM(TO_CHAR(t.is_issuing)), '0'),
    NVL(TRIM(TO_CHAR(t.is_acquiring)), '0'),
    CAST(SUBSTR(t.is_issuing_category, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(t.card_product_type_category, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(t.card_account_currency, 1, 50) AS VARCHAR2(50 CHAR)),
    CAST(SUBSTR(t.acquiring_device_type, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(t.trans_group, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(t.operation_type, 1, 100) AS VARCHAR2(100 CHAR)),
    CAST(SUBSTR(t.token_status, 1, 100) AS VARCHAR2(100 CHAR));

CREATE INDEX idx_mv_worldmap_month       ON pg_mv_req5_world_map (REPORT_MONTH) NOLOGGING;
CREATE INDEX idx_mv_worldmap_src_country ON pg_mv_req5_world_map (SOURCE_COUNTRY) NOLOGGING;
CREATE INDEX idx_mv_worldmap_tgt_country ON pg_mv_req5_world_map (TARGET_COUNTRY) NOLOGGING;
CREATE INDEX idx_mv_worldmap_pay_sys     ON pg_mv_req5_world_map (PAYMENT_SYSTEM) NOLOGGING;
CREATE INDEX idx_mv_worldmap_src_bank    ON pg_mv_req5_world_map (SOURCE_BANK_NAME) NOLOGGING;

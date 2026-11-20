-- ============================================================================
-- PG_MV_REQ4_AZ_MAP və PG_MV_REQ5_WORLD_MAP Materialized View Yeniləmə Skripti
-- Bütün tələb olunan yeni filter sütunları + indekslər daxildir.
-- ============================================================================

-- ============================================================================
-- 1. AZƏRBAYCAN XƏRİTƏSİ: PG_MV_REQ4_AZ_MAP
-- ============================================================================
DROP MATERIALIZED VIEW pg_mv_req4_az_map;

CREATE MATERIALIZED VIEW pg_mv_req4_az_map
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT
    s.report_month,
    s.bank_name,
    s.city_norm              AS source_city,
    s.city_norm              AS source_city_clean,
    CASE
        WHEN UPPER(s.city_norm) = 'BAKI' THEN 'BAKI'
        ELSE 'REGION'
    END                      AS source_city_category,
    s.acquiring_device_type,
    s.trans_group,
    s.operation_type,
    s.token_status,
    s.mcc,
    s.is_acquiring_category,
    s.target_bank_name,
    s.source_bank_name,
    s.city_norm              AS target_city,
    s.payment_system,
    SUM(s.local_amount)      AS total_amount,
    SUM(s.transaction_count) AS total_count,
    COUNT(*)                 AS cnt
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
    s.bank_name,
    s.city_norm,
    s.acquiring_device_type,
    s.trans_group,
    s.operation_type,
    s.token_status,
    s.mcc,
    s.is_acquiring_category,
    s.target_bank_name,
    s.source_bank_name,
    s.payment_system;

CREATE INDEX idx_mv_azmap_month       ON pg_mv_req4_az_map (REPORT_MONTH);
CREATE INDEX idx_mv_azmap_src_bank    ON pg_mv_req4_az_map (SOURCE_BANK_NAME);
CREATE INDEX idx_mv_azmap_tgt_bank    ON pg_mv_req4_az_map (TARGET_BANK_NAME);
CREATE INDEX idx_mv_azmap_pay_sys     ON pg_mv_req4_az_map (PAYMENT_SYSTEM);


-- ============================================================================
-- 2. DÜNYA XƏRİTƏSİ: PG_MV_REQ5_WORLD_MAP
-- ============================================================================
DROP MATERIALIZED VIEW pg_mv_req5_world_map;

CREATE MATERIALIZED VIEW pg_mv_req5_world_map
BUILD IMMEDIATE
REFRESH COMPLETE ON DEMAND
AS
SELECT /*+ PARALLEL(8) */
    TRUNC(t.report_date, 'MM')                                               AS report_month,
    t.source_bank_name                                                       AS bank_name,
    COALESCE(NULLIF(TRIM(t.source_country), ''), 'OTHER')                    AS source_country,
    COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER')           AS source_country_category,
    COALESCE(NULLIF(TRIM(t.target_country), ''), 'Azerbaijan')               AS target_country,
    COALESCE(NULLIF(TRIM(t.target_country_category), ''), 'OTHER')           AS target_country_category,
    COALESCE(NULLIF(TRIM(t.source_country_category), ''), 'OTHER')           AS country_category,
    COALESCE(NULLIF(TRIM(t.source_city_clean), ''), NULLIF(TRIM(t.source_city), ''), 'OTHER') AS city_clean,
    t.payment_system,
    NVL(TRIM(TO_CHAR(t.is_issuing)), '0')                                    AS is_issuing,
    NVL(TRIM(TO_CHAR(t.is_acquiring)), '0')                                  AS is_acquiring,
    t.is_issuing_category,
    t.card_product_type_category,
    t.card_account_currency,
    t.source_bank_name,
    t.acquiring_device_type,
    t.trans_group,
    t.operation_type,
    t.token_status,
    SUM(NVL(t.local_amount, 0))                                              AS total_amount,
    SUM(NVL(t.transaction_count, 0))                                         AS total_count,
    COUNT(*)                                                                 AS cnt
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
    NVL(TRIM(TO_CHAR(t.is_acquiring)), '0'),
    t.is_issuing_category,
    t.card_product_type_category,
    t.card_account_currency,
    t.acquiring_device_type,
    t.trans_group,
    t.operation_type,
    t.token_status;

CREATE INDEX idx_mv_worldmap_month       ON pg_mv_req5_world_map (REPORT_MONTH);
CREATE INDEX idx_mv_worldmap_src_country ON pg_mv_req5_world_map (SOURCE_COUNTRY);
CREATE INDEX idx_mv_worldmap_tgt_country ON pg_mv_req5_world_map (TARGET_COUNTRY);
CREATE INDEX idx_mv_worldmap_pay_sys     ON pg_mv_req5_world_map (PAYMENT_SYSTEM);
CREATE INDEX idx_mv_worldmap_src_bank    ON pg_mv_req5_world_map (SOURCE_BANK_NAME);

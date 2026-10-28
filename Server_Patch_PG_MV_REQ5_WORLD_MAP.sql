-- ============================================================================
-- AZERİCARD REPORTING SYSTEM - SERVER PATCH SCRIPT
-- Təyinat: Serverdə köhnə MV skripti icra olunubsa, yalnız dəyişən hissələri
--          (əsasən PG_MV_REQ5_WORLD_MAP) sürətli tətbiq etmək üçün.
-- İcra ediləcək istifadəçi / sxem: AZERCARD (və ya MV-lərin yerləşdiyi sxem)
-- ============================================================================

ALTER SESSION ENABLE PARALLEL DDL;
ALTER SESSION ENABLE PARALLEL DML;
ALTER SESSION ENABLE PARALLEL QUERY;

-- ═══════════════════════════════════════════════════════════════════════════
-- 1. PG_MV_REQ5_WORLD_MAP YENİLƏNMƏSİ
-- Səbəb: Backend tətbiqi (WorldMapRepository) tərəfindən tələb olunan
--        source_country, target_country, payment_system, is_issuing, is_acquiring
--        sütunları əlavə edilir.
-- ═══════════════════════════════════════════════════════════════════════════

-- Əgər mövcuddursa, köhnə MV silinir
BEGIN
    EXECUTE IMMEDIATE 'DROP MATERIALIZED VIEW PG_MV_REQ5_WORLD_MAP';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

-- Yeni sütunlarla yenidən yaradılır (Paralel 8 ilə sürətli icra olunur)
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

-- İndekslərin yaradılması
CREATE INDEX IDX_REQ5_WORLD_BANK  ON PG_MV_REQ5_WORLD_MAP (BANK_NAME) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ5_WORLD_MONTH ON PG_MV_REQ5_WORLD_MAP (REPORT_MONTH) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;
CREATE INDEX IDX_REQ5_WORLD_CNTRY ON PG_MV_REQ5_WORLD_MAP (COUNTRY_CATEGORY) NOLOGGING PARALLEL 8 COMPUTE STATISTICS;

ALTER INDEX IDX_REQ5_WORLD_BANK NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ5_WORLD_MONTH NOPARALLEL LOGGING;
ALTER INDEX IDX_REQ5_WORLD_CNTRY NOPARALLEL LOGGING;

BEGIN
    EXECUTE IMMEDIATE 'GRANT SELECT ON PG_MV_REQ5_WORLD_MAP TO REPORTAPPUSER';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/

COMMENT ON MATERIALIZED VIEW PG_MV_REQ5_WORLD_MAP IS 'Materialized view for World map reporting';


-- ═══════════════════════════════════════════════════════════════════════════
-- 2. PG_MV_REQ4_AZ_MAP ÜÇÜN İNDEKS DÜZƏLİŞİ (Əgər köhnə skriptdə xəta veribsə)
-- ═══════════════════════════════════════════════════════════════════════════
BEGIN
    EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX IDX_REQ4_COMPOSITE ON PG_MV_REQ4_AZ_MAP (
        REPORT_MONTH, BANK_NAME, SOURCE_CITY, SOURCE_CITY_CLEAN, SOURCE_CITY_CATEGORY
    ) NOLOGGING PARALLEL 8 COMPUTE STATISTICS';
    EXECUTE IMMEDIATE 'ALTER INDEX IDX_REQ4_COMPOSITE NOPARALLEL LOGGING';
EXCEPTION
    WHEN OTHERS THEN NULL;
END;
/


-- ═══════════════════════════════════════════════════════════════════════════
-- 3. ARAYIŞ VƏ KÖMƏKÇİ CƏDVƏLLƏRİN YOXLAMASI
-- (Əgər serverdə artıq mövcuddursa toxunulmur, yoxdursa yaradılır)
-- ═══════════════════════════════════════════════════════════════════════════

-- COUNTRIES (Dünya xəritəsi koordinatları üçün)
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

-- CITIES (Azərbaycan xəritəsi koordinatları üçün)
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

-- BANKS (Bank Hash / Name map üçün)
BEGIN
    EXECUTE IMMEDIATE 'CREATE TABLE BANKS (
        BANK_HASH VARCHAR2(100),
        BANK_NAME VARCHAR2(100)
    )';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- MCC_GROUPS (MCC Hash / Name map üçün)
BEGIN
    EXECUTE IMMEDIATE 'CREATE TABLE MCC_GROUPS (
        MCC_HASH VARCHAR2(100),
        MCC_NAME VARCHAR2(100)
    )';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

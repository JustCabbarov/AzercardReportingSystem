# AzeriCard Reporting System — Server və DBA Quraşdırma Təlimatı

Bu sənəd **AzeriCard Reporting System** layihəsinin server mühitində (Oracle DB, PostgreSQL və .NET 8 Web API) yerləşdirilməsi (deployment), konfiqurasiyası və yenilənməsi üçün addım-addım texniki təlimatdır.

---

## 1. Oracle Verilənlər Bazası Təlimatı

Serverdə artıq 600M+ sətirlik baza üzərində Materialized View-lar yaradılıbsa, **bütün skripti təkrar işə salmaq qətiyyən lazım deyil!** Digər 11 MV toxunulmaz qalır. Yalnız dəyişən hissələri patch etmək kifayətdir.

### Variant A (Tövsiyə Olunan — Sürətli Patch, 1-2 dəqiqə)
Əgər serverdə MV-lər artıq mövcuddursa, sadəcə masaüstündəki **`Server_Patch_PG_MV_REQ5_WORLD_MAP.sql`** skriptini `AZERCARD` (və ya MV-lərin olduğu istifadəçi) sessiyasında icra edin.

**Bu patch nə edir?**
1. **`PG_MV_REQ5_WORLD_MAP` yenilənir:** Köhnə MV-də çatışmayan `source_country`, `target_country`, `source_country_category`, `target_country_category`, `payment_system`, `is_issuing`, `is_acquiring` sütunları əlavə edilir (backend tətbiqinin düzgün işləməsi üçün bu sütunlar mütləqdir).
2. **`PG_MV_REQ4_AZ_MAP` üçün indeks adı:** Köhnə skriptdə `I_SNAP$_...` adı sistem adı ilə toqquşurdu, `IDX_REQ4_COMPOSITE` adı ilə unikal indeks yaradılır.
3. **Arayış cədvəlləri (`COUNTRIES`, `CITIES`, `BANKS`, `MCC_GROUPS`):** Əgər bazada yoxdursa yaradılır, varsa toxunulmur.

### Variant B (Sıfırdan Quraşdırma)
Əgər yeni bir serverdə və ya yeni sxemdə sıfırdan qurulursa, masaüstündəki tam **`Optimized_Materialized_views.sql`** faylı icra olunmalıdır.

### 4 Vacib Arayış Cədvəlinin Məlumatları
- **`COUNTRIES`:** `NAME (VARCHAR2)`, `LATITUDE (NUMBER)`, `LONGITUDE (NUMBER)` (Dünya xəritəsi heatmap-i üçün).
- **`CITIES`:** `CITY_NAME (VARCHAR2)`, `LATITUDE (NUMBER)`, `LONGITUDE (NUMBER)` (Azərbaycan xəritəsi heatmap-i üçün).
- **`BANKS`:** `BANK_HASH (VARCHAR2)`, `BANK_NAME (VARCHAR2)` (Filtr və hesabatlarda bank adlarının düzgün çıxması üçün).
- **`MCC_GROUPS`:** `MCC_HASH (VARCHAR2)`, `MCC_NAME (VARCHAR2)` (Sektor və MCC qrupları üçün).

---

## 2. Backend (.NET 8 Web API) Konfiqurasiyası və Deploy

Sistem iki fərqli verilənlər bazasına qoşulur:
1. **PostgreSQL:** Sistem istifadəçiləri, rollar, departamentlər, refresh tokenlər və audit loqları.
2. **Oracle:** 600M+ sətirlik analitika, tranzaksiyalar və Materialized View-lar.

### Addım 1: `appsettings.Production.json` (və ya `appsettings.json`) Tənzimlənməsi
Server mühitinə uyğun connection stringləri daxil edin:
```json
{
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=POSTGRES_HOST;Port=5432;Database=AzercardSystemDB;Username=postgres;Password=YOUR_POSTGRES_PASSWORD;",
    "OracleConnection": "Data Source=ORACLE_HOST:1521/ORACLE_SERVICE_NAME;User Id=AZERCARD;Password=YOUR_ORACLE_PASSWORD;"
  },
  "JwtSettings": {
    "SecretKey": "SUPER_SECRET_KEY_MIN_32_CHARACTERS_LONG!",
    "Issuer": "RMS.API",
    "Audience": "RMS.Client",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Addım 2: PostgreSQL Miqrasiyası (EF Core)
Sistem bazasının ilkin cədvəllərinin (Identity, User, Role və s.) yaranması üçün:
```bash
dotnet ef database update --project RMS.Persitence --startup-project RMS.Presentation
```

### Addım 3: Layihənin Build və Publish Edilməsi
```bash
# Release rejimində publish qovluğu yaratmaq:
dotnet publish RMS.Presentation/RMS.Presentation.csproj -c Release -o ./publish

# İcra etmək:
cd publish
dotnet RMS.Presentation.dll
```
*(Və ya IIS / Systemd Service / Docker konteyneri olaraq işə salına bilər)*

### Addım 4: Docker ilə Deploy (Alternativ)
Layihə daxilində hazır `Dockerfile` və `docker-compose.yml` mövcuddur:
```bash
docker compose up -d --build
```

---

## 3. Sistem Yoxlama və Test Addımları

Deploy bitdikdən sonra sistemin canlı işlədiyini təsdiqləmək üçün aşağıdakı test sorğularını yoxlaya bilərsiniz:

1. **Swagger Sənədləşməsi:**
   `http://SERVER_IP:PORT/swagger/index.html`
2. **Kart Dashboard KPI və Trendlər (Oracle MV_CARD_DASHBOARD):**
   `GET /api/carddashboard/dashboard` (HTTP 200)
3. **Kart Portfeli və Bazar Payı (Oracle MV_CARD_PORTFOLIO):**
   `GET /api/cards/top-cards?dimension=bank_name` (HTTP 200)
4. **Azərbaycan Xəritəsi Heatmap (Oracle PG_MV_REQ4_AZ_MAP):**
   `GET /api/azmap/heatmap` (HTTP 200)
5. **Dünya Xəritəsi Tranzaksiyaları (Oracle PG_MV_REQ5_WORLD_MAP):**
   `GET /api/worldmap/filter?Page=1&PageSize=10` (HTTP 200)
6. **Maşın Öyrənməsi / ML Proqnozlaşdırma (Oracle PG_MV_REQ1_FORECAST_INPUT):**
   `GET /api/forecasting?horizon=6` (HTTP 200)
7. **Sektor Xərcləmələri:**
   `GET /api/sectorspend/filter?page=1&pageSize=10` (HTTP 200)

---

## 4. Faylların Xülasəsi
Masaüstündə server komandasına göndərilmək üçün aşağıdakı fayllar hazırlanmışdır:
- **`AzercardReportingSystem_Latest.zip`** — .NET 8 tətbiqinin ən son, təmizlənmiş mənbə kodu.
- **`Server_Patch_PG_MV_REQ5_WORLD_MAP.sql`** — Serverdəki köhnə MV-lərin üzərinə sürətli tətbiq ediləcək patch skripti.
- **`Optimized_Materialized_views.sql`** — Bütün 12 MV və indekslərin tam sıfırdan qurulma skripti.
- **`SERVER_DEPLOYMENT_README.md`** — Bu sənəd (Server və DBA təlimatı).

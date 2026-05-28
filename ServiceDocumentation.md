# AzeriCard Reporting System — Servis və Metod Sənədləşməsi

Bu sənəd **RMS.Application** layındakı bütün servisləri, onların metodlarını, hər metodun nə etdiyini, hansı parametrləri aldığını və nə qaytardığını ətraflı izah edir.

---

## MÜNDƏRİCAT

1. [System Servisləri](#1-system-servisləri)
   - [AuthService](#11-authservice)
   - [TokenHandler](#12-tokenhandler)
   - [GenericService\<TVM, TEntity\>](#13-genericservicetvm-tentity)
   - [PasswordResetService](#14-passwordresetservice)
   - [EmailSender](#15-emailsender)

2. [Oracle Servisləri](#2-oracle-servisləri)
   - [AlertService](#21-alertservice)
   - [AzMapService](#22-azmapservice)
   - [CardActivityService](#23-cardactivityservice)
   - [ForecastService](#24-forecastservice)
   - [MarketBenchmarkService](#25-marketbenchmarkservice)
   - [NewCardService](#26-newcardservice)
   - [SectorSpendService](#27-sectorspendservice)
   - [WorldMapService](#28-worldmapservice)

---

## 1. System Servisləri

Bu servislər **PostgreSQL** verilənlər bazası üzərindən istifadəçiləri, rolları, tokenləri və şifrə bərpasını idarə edir.

---

### 1.1 `AuthService`

**Məqsədi:** Sistemdəki giriş/çıxış, qeydiyyat, rol idarəetməsi kimi bütün autentifikasiya əməliyyatlarını yerinə yetirir.

**Fayl:** `RMS.Application/Services/System/AuthService.cs`

---

#### `LoginAsync(LoginDTO loginDTO)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | E-poçt və şifrə ilə giriş edir, uğurlu giriş zamanı `AccessToken` + `RefreshToken` cütlüyü yaradır |
| **Input** | `LoginDTO` → `Email` (string), `Password` (string) |
| **Return** | `Task<LoginResponseDTO>` → `AccessToken`, `RefreshToken`, `ExpireAt` (token bitmə tarixi) |
| **Səhv halı** | Email mövcud deyilsə və ya şifrə yanlışdırsa → `UnauthorizedAccessException` atır |

**Addım-addım iş prinsipi:**
1. `Email`-ə görə istifadəçi axtarılır
2. `SignInManager` ilə şifrənin düzgünlüyü yoxlanılır
3. `TokenHandler` vasitəsilə `AccessToken` + `RefreshToken` yaradılır
4. `RefreshToken` bazaya yazılır, müddət konfiqurasiyadan oxunur
5. `LoginResponseDTO` qaytarılır

---

#### `RegisterAsync(RegisterDTO registerDTO)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Sistemə yeni istifadəçi qeyd edir, `AppUser` + `Employee` yaradır, standart `"User"` rolu verir |
| **Input** | `RegisterDTO` → `Email`, `Password`, `FirstName`, `LastName`, `Phone`, `DepartmentId`, `PositionId` |
| **Return** | `Task` (void — heç bir dəyər qaytarmır) |
| **Səhv halı** | `UserManager.CreateAsync` uğursuz olarsa → bütün xəta mesajları birləşərək `Exception` atılır |

**Addım-addım iş prinsipi:**
1. `AppUser` obyekti yaradılır və bazaya əlavə edilir
2. `Employee` profili yaradılır, `AppUserId` ilə `AppUser`-ə bağlanır
3. İstifadəçiyə avtomatik olaraq `"User"` rolu verilir

---

#### `LogoutAsync(string refreshToken)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | İstifadəçini sistemdən çıxarır, `RefreshToken`-ni ləğv edir |
| **Input** | `refreshToken` — string (bazadakı aktiv token) |
| **Return** | `Task` (void) |

---

#### `AssignRoleAsync(Guid userId, string roleName)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Mövcud istifadəçiyə yeni rol əlavə edir |
| **Input** | `userId` — Guid (istifadəçi ID), `roleName` — string (məs: `"Admin"`, `"Manager"`) |
| **Return** | `Task` (void) |

---

#### `CreateRoleAsync(CreateRoleDTO appRole)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Sistemdə yeni bir rol yaradır |
| **Input** | `CreateRoleDTO` → `Name` (string), `Description` (string) |
| **Return** | `Task<IdentityResult>` → yaratma əməliyyatının nəticəsi (uğurlu/uğursuz + xəta mesajları) |

---

#### `GetUserByRoleIdAsync(Guid roleId)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən rol ID-sinə aid bütün istifadəçiləri qaytarır |
| **Input** | `roleId` — Guid |
| **Return** | `Task<IList<AppUser>>` — həmin roldakı istifadəçilərin siyahısı |

---

#### `RemoveRoleAsync(Guid userId, string roleName)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | İstifadəçidən müəyyən rolü çıxarır |
| **Input** | `userId` — Guid, `roleName` — string |
| **Return** | `Task<IdentityResult>` — silmə əməliyyatının nəticəsi |

---

### 1.2 `TokenHandler`

**Məqsədi:** JWT `AccessToken` yaratmaq, imzalamaq, doğrulamaq; `RefreshToken` yaratmaq, ləğv etmək və yeniləmək.

**Fayl:** `RMS.Application/Services/System/TokenHandler.cs`

---

#### `CreateAccessTokenAsync(AppUser user)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | İstifadəçinin rolları daxil olmaqla bütün claim-lərini ehtiva edən imzalanmış JWT token yaradır |
| **Input** | `AppUser` — Identity istifadəçi obyekti |
| **Return** | `Task<string>` — JWT formatında access token string-i |

**Token içindəki claim-lər:**
- `NameIdentifier` — `user.Id`
- `Email` — `user.Email`
- `Jti` — unikal UUID
- `Role` — istifadəçinin bütün rolları (bir neçə ola bilər)

---

#### `CreateRefreshTokenAsync(AppUser user)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Kriptoqrafik təsadüfi 64-baytlıq `RefreshToken` yaradır |
| **Input** | `AppUser` — (yalnız imza üçün qəbul edilir, daxilən istifadə olunmur) |
| **Return** | `Task<string>` — Base64 formatında refresh token |

---

#### `RefreshTokenAsync(string refreshToken)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Köhnə `RefreshToken`-i yoxlayır, ləğv edir, yeni token cütlüyü yaradıb qaytarır |
| **Input** | `refreshToken` — string (cari aktiv token) |
| **Return** | `Task<LoginResponseDTO>` → yeni `AccessToken`, `RefreshToken`, `ExpireAt` |
| **Səhv halı** | Token tapılmasa, ləğv edilmişsə və ya müddəti bitmişsə → `UnauthorizedAccessException` |

---

#### `RevokeTokenAsync(string refreshToken)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Verilmiş `RefreshToken`-i bazada ləğv edilmiş kimi işarələyir (`IsRevoked = true`) |
| **Input** | `refreshToken` — string |
| **Return** | `Task` (void) |

---

#### `ValidateTokenAsync(string token)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | JWT `AccessToken`-in imzasını, issuer, audience və ömrünü yoxlayır |
| **Input** | `token` — JWT string |
| **Return** | `Task<bool>` → `true` (etibarlıdır); etibarsızsa exception atır |

---

### 1.3 `GenericService<TVM, TEntity>`

**Məqsədi:** Standart CRUD əməliyyatlarını (Create, Read, Update, Delete) generik (ümumi-tip) şəkildə tətbiq edir. Bir sinifin bütün entity-lər üçün işləməsini təmin edən ümumi xidmət sinifidir. `TVM` → ViewModel/DTO tipi; `TEntity` → Domain entity tipidir (`BaseEntity`-dən törəməli).

**Fayl:** `RMS.Application/Services/System/GenericService.cs`

---

#### `AddAsync(TVM entity)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Yeni entity bazaya əlavə edir |
| **Input** | `TVM` — ViewModel/DTO obyekti (null ola bilməz) |
| **Return** | `Task<TVM>` — bazaya yazılan entity-nin DTO forması |
| **Səhv halı** | `null` gələrsə → `NotNullExceptions` atılır |

---

#### `DeleteAsync(Guid id)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Verilmiş ID-yə görə entity-ni bazadan silir |
| **Input** | `id` — Guid |
| **Return** | `Task<bool>` → `true` (uğurlu silmə) |
| **Səhv halı** | Entity tapılmasa → `NotNullExceptions` atılır |

---

#### `GetAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün entity-ləri DTO siyahısı kimi qaytarır |
| **Input** | `include` — optional lambda (əlaqəli cədvəlləri daxil etmək üçün, məs: `.Include(x => x.Department)`) |
| **Return** | `Task<IEnumerable<TVM>>` — bütün qeydlər |

---

#### `GetByIdAsync(Guid id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Verilmiş ID-yə görə tək entity tapır |
| **Input** | `id` — Guid; `include` — optional lambda |
| **Return** | `Task<TVM>` — tapılan entity-nin DTO forması |
| **Səhv halı** | Tapılmasa → `NotNullExceptions` |

---

#### `UpdateAsync(TVM entity)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Mövcud entity-ni yeni dəyərlərlə güncəlləyir |
| **Input** | `TVM` — yeni məlumatları ehtiva edən DTO |
| **Return** | `Task<TVM>` — güncəlləndikdən sonra DTO forması |
| **Səhv halı** | `null` gələrsə → `NotNullExceptions` |

---

### 1.4 `PasswordResetService`

**Məqsədi:** E-poçt vasitəsilə göndərilən 6 rəqəmli OTP (One-Time Password) kodu ilə istifadəçinin şifrəsini bərpa edir.

**Fayl:** `RMS.Application/Services/System/PasswordResetService.cs`

---

#### `SendOtpAsync(OTPrequest request)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | İstifadəçinin e-poçtuna OTP göndərir, bərpa kodunu bazaya yazır |
| **Input** | `OTPrequest` → `Email` (string) |
| **Return** | `Task` (void) |
| **Səhv halı** | İstifadəçi tapılmasa → `InvalidOperationException("User tapılmadı")` |

**Addım-addım iş prinsipi:**
1. Email-lə istifadəçi axtarılır
2. `100000–999999` aralığında 6 rəqəmli OTP yaradılır
3. Kod bazaya yazılır, müddəti `UTC+5 dəqiqə` kimi qeyd edilir
4. `EmailSender` vasitəsilə e-poçt göndərilir

---

#### `ResetPasswordAsync(string email, string otp, string newPassword)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | OTP kodu yoxlayır, düzgündürsə şifrəni yeniləyir |
| **Input** | `email` — string, `otp` — string (6 rəqəmli), `newPassword` — string |
| **Return** | `Task<IdentityResult>` — şifrə dəyişikliyi nəticəsi |
| **Səhv halı** | İstifadəçi yoxdursa → `InvalidOperationException`; OTP yanlış/müddəti bitmişsə → `"Kod yanlışdır və ya müddəti bitib"` |

---

### 1.5 `EmailSender`

**Məqsədi:** Gmail SMTP protokolu üzərindən OTP şifrə bərpa e-poçtu göndərir.

**Fayl:** `RMS.Application/Services/System/EmailSender.cs`

---

#### `SendOtpEmailAsync(string toEmail, string otp)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Hədəf e-poçta OTP kodu ilə formatlanmış HTML e-poçt göndərir |
| **Input** | `toEmail` — alıcı e-poçt ünvanı (string); `otp` — 6 rəqəmli OTP kodu (string) |
| **Return** | `Task` (void) |
| **SMTP Konfiqurasiya** | `smtp.gmail.com : 587`, SSL aktiv, `giftcardmessenger@gmail.com` hesabı |

**E-poçt formatı:** HTML şablonu, OTP kodu böyük `<h2>` elementi ilə göstərilir, kodun 5 dəqiqə etibarlı olduğu qeyd edilir.

---

## 2. Oracle Servisləri

Bu servislər **Oracle Data Warehouse** bazasından hesabat məlumatlarını oxuyur. Əsasən **Read-Only** əməliyyatlar həyata keçirirlər.

---

### 2.1 `AlertService`

**Məqsədi:** Bank tranzaksiyalarında anomaliya siqnallarını (kəskin artım — *Spike*, kəskin azalma — *Drop*) idarə edir. `AlertSignal` entity-ləri üzərindən işləyir.

**Fayl:** `RMS.Application/Services/Oracle/AlertService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün alert siqnallarını qaytarır |
| **Input** | `ct` — isteğe bağlı iptal tokeni |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən banka aid bütün siqnalları qaytarır |
| **Input** | `bankName` — string (məs: `"Kapital Bank"`), `ct` |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

#### `GetActiveAlertsAsync(string? bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Aktiv (hələ həll edilməmiş) siqnalları qaytarır. `bankName` ötürülsə, yalnız o banka aid olanlar gəlir |
| **Input** | `bankName` — nullable string, `ct` |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

#### `GetByMonthAsync(DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən aya aid siqnalları qaytarır |
| **Input** | `month` — DateTime (yalnız il+ay əhəmiyyətlidir), `ct` |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

#### `GetBySeverityAsync(string severity, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Ciddilik dərəcəsinə görə siqnalları filtrləyir |
| **Input** | `severity` — string (məs: `"High"`, `"Medium"`, `"Low"`), `ct` |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

#### `GetSpikesAsync(string? bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | `SignalType == "Spike"` olan — yəni əvvəlki aya görə kəskin artım qeydə alınan siqnalları qaytarır |
| **Input** | `bankName` — nullable string, `ct` |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

#### `GetDropsAsync(string? bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | `SignalType == "Drop"` olan — yəni əvvəlki aya görə kəskin azalma olan siqnalları qaytarır |
| **Input** | `bankName` — nullable string, `ct` |
| **Return** | `Task<IEnumerable<AlertSignal>>` |

---

### 2.2 `AzMapService`

**Məqsədi:** Azərbaycan xəritəsi üzrə regional tranzaksiya məlumatlarını idarə edir. Regionlar, şəhərlər, cihaz tipləri üzrə hesabat yaradır.

**Fayl:** `RMS.Application/Services/Oracle/AzMapService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün Azərbaycan xəritəsi tranzaksiyalarını qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<AzMapTransaction>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən banka aid tranzaksiyaları qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<AzMapTransaction>>` |

---

#### `GetByMonthAsync(DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən aya aid tranzaksiyaları qaytarır |
| **Input** | `month` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<AzMapTransaction>>` |

---

#### `GetByRegionAsync(string regionNameClean, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Azərbaycanın müəyyən rayonuna/regionuna aid tranzaksiyaları qaytarır |
| **Input** | `regionNameClean` — string (məs: `"Baku"`, `"Ganja"`), `ct` |
| **Return** | `Task<IEnumerable<AzMapTransaction>>` |

---

#### `GetByDeviceTypeAsync(string deviceType, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Cihaz tipinə görə (POS, ATM, mPOS) tranzaksiyaları filtrləyir |
| **Input** | `deviceType` — string, `ct` |
| **Return** | `Task<IEnumerable<AzMapTransaction>>` |

---

#### `GetByCityAsync(string cityClean, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən şəhərə aid tranzaksiyaları qaytarır |
| **Input** | `cityClean` — string (məs: `"Baku"`, `"Sumqayit"`), `ct` |
| **Return** | `Task<IEnumerable<AzMapTransaction>>` |

---

#### `GetAmountByRegionAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Azərbaycan xəritəsi üçün hər regionun həmin aydakı **ümumi tranzaksiya məbləğini** hesablayır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<Dictionary<string, decimal>>` → `{ "Baku": 150000.50, "Ganja": 45000.00, ... }` |

---

#### `GetDeviceCountByTypeAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Terminal tipi üzrə cihaz sayını hesablayır — POS, ATM, mPOS siyahısı |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<Dictionary<string, long>>` → `{ "POS": 1250, "ATM": 320, "mPOS": 88, ... }` |

---

### 2.3 `CardActivityService`

**Məqsədi:** Kart aktivliyi məlumatlarını idarə edir. Contactless istifadə faizi, aktivlik seqmenti paylanması kimi hesabatlar yaradır.

**Fayl:** `RMS.Application/Services/Oracle/CardActivityService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün kart aktivlik qeydlərini qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<CardActivity>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün aktivlik məlumatlarını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<CardActivity>>` |

---

#### `GetByMonthAsync(DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən aya aid aktivlik məlumatlarını qaytarır |
| **Input** | `month` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<CardActivity>>` |

---

#### `GetByProductTypeAsync(string productType, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Kart məhsul tipinə görə (VISA Classic, MasterCard Gold və s.) filtrləyir |
| **Input** | `productType` — string, `ct` |
| **Return** | `Task<IEnumerable<CardActivity>>` |

---

#### `GetByActivitySegmentAsync(string bankName, string segment, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bank + aktivlik seqmenti üzrə (aktiv/passiv/yeni) qeydləri qaytarır |
| **Input** | `bankName` — string, `segment` — string (məs: `"Active"`, `"Dormant"`), `ct` |
| **Return** | `Task<IEnumerable<CardActivity>>` |

---

#### `GetAvgContactlessRateAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bank + ay üzrə **ortalama contactless ödəniş faizini** hesablayır (ağırlıqlı ortalama) |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<decimal>` → faiz (məs: `34.75` = 34.75%) |
| **Formula** | `(Contactless tranzaksiya sayı ÷ Ümumi tranzaksiya sayı) × 100` |

---

#### `GetSegmentDistributionAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Hər aktivlik seqmentinin **ümumi kartlara nisbətini faiz kimi** hesablayır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<Dictionary<string, decimal>>` → `{ "Active": 68.5, "Dormant": 21.3, "New": 10.2 }` |

---

### 2.4 `ForecastService`

**Məqsədi:** ML.NET SSA (Singular Spectrum Analysis) modeli üçün vaxt seriyası məlumatlarını hazırlayır. Statistik proqnozlaşdırma tahminlər üçün data pipeline-ni idarə edir.

**Fayl:** `RMS.Application/Services/Oracle/ForecastService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün forecast input məlumatlarını qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<ForecastInput>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün forecast məlumatlarını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<ForecastInput>>` |

---

#### `GetTimeSeriesAsync(string bankName, string mccGroup, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | ML.NET SSA modeli üçün bank + MCC qrupu üzrə **TimeIndex-ə görə sıralanmış fasiləsiz vaxt seriyası** qaytarır |
| **Input** | `bankName` — string, `mccGroup` — string (məs: `"Supermarket"`, `"Fuel"`), `ct` |
| **Return** | `Task<IEnumerable<ForecastInput>>` — xronoloji ardıcıllıqla sıralanmış |

---

#### `GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən tarix aralığındakı məlumatları qaytarır |
| **Input** | `from` — DateTime (başlanğıc), `to` — DateTime (son), `ct` |
| **Return** | `Task<IEnumerable<ForecastInput>>` |
| **Səhv halı** | `from > to` olарса → `ArgumentException` atılır |

---

#### `GetMccGroupsAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın məlumat bazasında mövcud olan bütün **unikal MCC qruplarının** siyahısını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<string>>` → `["Fuel", "Grocery", "Restaurant", ...]` (əlifba sırası ilə) |

---

### 2.5 `MarketBenchmarkService`

**Məqsədi:** Bankın bazar payını, kart payını, rank sıralamasını hesablayır. Rəqabət müqayisəsi (benchmark) hesabatları üçün istifadə olunur.

**Fayl:** `RMS.Application/Services/Oracle/MarketBenchmarkService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün bazar benchmark məlumatlarını qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<MarketBenchmark>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün benchmark tarixçəsini qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<MarketBenchmark>>` |

---

#### `GetByMonthAsync(DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən aya aid benchmark məlumatlarını qaytarır |
| **Input** | `month` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<MarketBenchmark>>` |

---

#### `GetRankedAsync(DateTime month, string? regionNameClean, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Ay + region üzrə bankları tranzaksiya həcminə görə sıralanmış şəkildə qaytarır |
| **Input** | `month` — DateTime, `regionNameClean` — nullable string (region görə filtr), `ct` |
| **Return** | `Task<IEnumerable<MarketBenchmark>>` — sıralanmış siyahı |

---

#### `GetMarketShareAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın müəyyən baydakı **bazar payını faiz kimi** hesablayır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<decimal>` → faiz (məs: `12.45` = 12.45%) |
| **Formula** | `(Bankın tranzaksiya məbləği ÷ Bazarın ümumi məbləği) × 100` |

---

#### `GetCardShareAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın **kart sayına görə** bazar payını faiz kimi hesablayır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<decimal>` → faiz (məs: `8.20` = 8.20%) |
| **Formula** | `(Bankın kart sayı ÷ Bazarın ümumi kart sayı) × 100` |

---

#### `GetBankRankAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın həmin aydakı sırasını qaytarır (`1` = ən böyük bank) |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<int>` → rank nömrəsi (tapılmasa `-1`) |

---

### 2.6 `NewCardService`

**Məqsədi:** Yeni kart aktivasiyalarını idarə edir. Aktivasyon faizi, seqment paylanması, ilk istifadəyə qədər keçən müddət kimi KPI-ları hesablayır.

**Fayl:** `RMS.Application/Services/Oracle/NewCardService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün yeni kart aktivasiya məlumatlarını qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<NewCardActivation>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün kart aktivasiya məlumatlarını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<NewCardActivation>>` |

---

#### `GetByFirstMonthAsync(DateTime firstMonth, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən ayda ilk dəfə aktivləşdirilmiş kartları qaytarır |
| **Input** | `firstMonth` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<NewCardActivation>>` |

---

#### `GetBySegmentAsync(string bankName, string segment, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bank + aktivasion seqmentinə görə filtrləyir |
| **Input** | `bankName` — string, `segment` — string (məs: `"Premium"`, `"Standard"`), `ct` |
| **Return** | `Task<IEnumerable<NewCardActivation>>` |

---

#### `GetInactiveCardsAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Aktivasiya edilməmiş (hələ istifadəyə başlanmamış) kartları qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<NewCardActivation>>` |

---

#### `GetAvgActivationRateAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün kartlar üzrə **ortalama 3 aylıq aktivasyon faizini** hesablayır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<decimal>` → faiz (məs: `72.50` = 72.50%) |
| **Mənbə** | `Avg3MActiveRate` entity field-i üzərindən ortalama götürülür |

---

#### `GetSegmentDistributionAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Hər aktivasiya seqmentinin ümumi kart sayına nisbətini faiz kimi hesablayır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<Dictionary<string, decimal>>` → `{ "Premium": 15.3, "Standard": 72.1, "Budget": 12.6 }` |

---

#### `GetAvgMonthsToFirstUseAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Kart alındıqdan ilk tranzaksiyaya qədər keçən **ortalama ay sayını** hesablayır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<double>` → ortalama ay sayı (məs: `2.35` ay); aktivasiya edilməmişlər nəzərə alınmır |

---

### 2.7 `SectorSpendService`

**Məqsədi:** MCC (Merchant Category Code) qrupları üzrə xərc məlumatlarını idarə edir. Onlayn/oflayn kanal paylanması, top xərclər kimi hesabatlar yaradır.

**Fayl:** `RMS.Application/Services/Oracle/SectorSpendService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün sektor xərc məlumatlarını qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<SectorSpend>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün sektor xərc tarixçəsini qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<SectorSpend>>` |

---

#### `GetByBankAndMonthAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bank + ay üzrə sektor xərclərini qaytarır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<SectorSpend>>` |

---

#### `GetByMccGroupAsync(string mccGroup, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən MCC qrupuna (məs: `"Grocery"`, `"Fuel"`) aid bütün qeydləri qaytarır |
| **Input** | `mccGroup` — string, `ct` |
| **Return** | `Task<IEnumerable<SectorSpend>>` |

---

#### `GetWithShareOfWalletAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın həmin ay üçün `ShareOfWallet` məlumatlarını ehtiva edən sektor xərc qeydlərini qaytarır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<SectorSpend>>` |

---

#### `GetTopMccGroupsAsync(string bankName, DateTime month, int topN, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bank + ay üzrə **ən çox xərc edilən Top N MCC qrupunu** qaytarır |
| **Input** | `bankName` — string, `month` — DateTime, `topN` — int (default: `10`), `ct` |
| **Return** | `Task<IEnumerable<SectorSpend>>` — `TotalAmount`-a görə azalan sıra ilə |

---

#### `GetChannelDistributionAsync(string bankName, DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Onlayn vs Oflayn (`SOURCE_CHANNEL` sütunu) kanal paylanmasını faiz kimi hesablayır |
| **Input** | `bankName` — string, `month` — DateTime, `ct` |
| **Return** | `Task<Dictionary<string, decimal>>` → `{ "Online": 42.3, "POS": 57.7 }` |
| **Formula** | `(Kanalın ümumi məbləği ÷ Ümumi xərc) × 100` |

---

### 2.8 `WorldMapService`

**Məqsədi:** Dünya xəritəsi üzrə beynəlxalq kart tranzaksiyalarını idarə edir. Issuing (verilmiş kart) vs Acquiring (qəbul edilən kart) əməliyyatlarını ayırd edir. Heat map vizualizasiyası üçün ölkə-məbləğ cütlüyü yaradır.

**Fayl:** `RMS.Application/Services/Oracle/WorldMapService.cs`

---

#### `GetAllAsync(CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bütün dünya xəritəsi tranzaksiyalarını qaytarır |
| **Input** | `ct` |
| **Return** | `Task<IEnumerable<WorldMapTransaction>>` |

---

#### `GetByBankAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın bütün beynəlxalq tranzaksiyalarını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<WorldMapTransaction>>` |

---

#### `GetByMonthAsync(DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Müəyyən aya aid beynəlxalq tranzaksiyaları qaytarır |
| **Input** | `month` — DateTime, `ct` |
| **Return** | `Task<IEnumerable<WorldMapTransaction>>` |

---

#### `GetBySourceCountryAsync(string country, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Mənbə ölkəsinə görə tranzaksiyaları filtrləyir |
| **Input** | `country` — string (məs: `"Turkey"`, `"Russia"`), `ct` |
| **Return** | `Task<IEnumerable<WorldMapTransaction>>` |

---

#### `GetIssuingAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın **issuing** (xaricdə AzeriCard kartı ilə edilən) tranzaksiyalarını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<WorldMapTransaction>>` |

---

#### `GetAcquiringAsync(string bankName, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Bankın **acquiring** (xarici kartlarla Azərbaycanda edilən) tranzaksiyalarını qaytarır |
| **Input** | `bankName` — string, `ct` |
| **Return** | `Task<IEnumerable<WorldMapTransaction>>` |

---

#### `GetAmountByCountryAsync(DateTime month, CancellationToken ct)`

| Xüsusiyyət | Dəyər |
|---|---|
| **Məqsədi** | Dünya xəritəsi heat map üçün hər ölkənin həmin aydakı **ümumi tranzaksiya məbləğini** hesablayır |
| **Input** | `month` — DateTime, `ct` |
| **Return** | `Task<Dictionary<string, decimal>>` → `{ "Turkey": 85000.00, "Russia": 42000.50, ... }` |
| **Qeyd** | `SOURCE_COUNTRY` boş olan qeydlər nəzərə alınmır |

---

## Servis Xülasəsi Cədvəli

| Servis | Qrup | Method Sayı | Əsas Məqsəd |
|---|---|---|---|
| `AuthService` | System | 6 | Giriş, çıxış, qeydiyyat, rol idarəetməsi |
| `TokenHandler` | System | 5 | JWT yaratmaq, yoxlamaq, yeniləmək |
| `GenericService<TVM,TEntity>` | System | 5 | Universal CRUD əməliyyatları |
| `PasswordResetService` | System | 2 | OTP ilə şifrə bərpası |
| `EmailSender` | System | 1 | E-poçt göndərmə (SMTP) |
| `AlertService` | Oracle | 6 | Spike/Drop anomaliya siqnalları |
| `AzMapService` | Oracle | 8 | Azərbaycan regional xəritə məlumatları |
| `CardActivityService` | Oracle | 7 | Kart aktivlik seqmentləri, contactless faiz |
| `ForecastService` | Oracle | 5 | ML vaxt seriyası məlumatları |
| `MarketBenchmarkService` | Oracle | 7 | Bazar payı, kart payı, bank rankı |
| `NewCardService` | Oracle | 8 | Yeni kart aktivasiyaları, KPI-lar |
| `SectorSpendService` | Oracle | 7 | MCC sektor xərcləri, kanal paylanması |
| `WorldMapService` | Oracle | 8 | Beynəlxalq tranzaksiyalar, heat map |

---

*Bu sənəd `RMS.Application` layının tam servis və metod sənədləşməsidir. Son yenilənmə: 2026-04-14*

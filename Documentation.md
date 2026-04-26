# AzeriCard Reporting System - Texniki Sənədləşmə

Bu sənəd **AzeriCard Reporting System** .NET əsaslı proqram təminatı layihəsinin arxitekturasını, qovluq strukturunu və əsas iş proseslərini detayı ilə izah edir.

## 1. Ümumi Sistem Arxitekturası (Overall System Architecture)
Sistem müasir **Clean Architecture** (Təmiz Arxitektura) konsepsiyasına əsaslanaraq qurulmuşdur. ASP.NET Core (.NET 8) üzərində işləyən bu arxitektura asılılıqların (dependencies) mərkəzə — yəni `Domain` layına — doğru istiqamətlənməsini təmin edir. Bu struktur modullar arasında yüksək izolasyon yaradır və sistemi asanlıqla test edilə bilən (testable), saxlanıla bilən (maintainable) və yeni texnologiyalara uyğunlaşan (scalable) hala gətirir.

Asılılıq (Dependency) zənciri aşağıdakı kimidir:
`Presentation -> Application -> Domain <- Persistence`
Burada `Contract` layı isə Application və Presentation arasında vasitəçi rolunu oynayır.

## 2. Qovluq və Fayl Strukturu (Folder and File Structure)
Layihə fiziki olaraq 5 əsas qovluğa (laylara) bölünmüşdür:

- **RMS.Domain/ (Nüvə Məntiqi)**
  - `Entities/`: Məlumat bazasına uyğun C# obyektləri (modellər). İkiyə ayrılır: sistemin daxili strukturu cədvəlləri (`System Models`) və hesabat data cədvəlləri (`Oracle`).
  - `Repositories/`: Verilənlər bazasına çıxış üçün nəzərdə tutulan asılılıqsız interfeyslər (məsələn, `IAzMapRepository`, `IGenericRepository`).
  
- **RMS.Application/ (Biznes Məntiqi və İstifadələr)**
  - `Services/`: Bütün biznes prosedurları və məntiqi yoxlamalar (`AuthService`, `CardActivityService` və s.).
  - `BackgroundServices/`: Arxa planda işləyən tapşırıqlar qovluğu (məsələn, `ModelRetrainBackgroundService`).
  - `MLForcasting/`: Verilənlər əsasında Maşın Öyrənməsi/Süni İntellekt (ML) ilə qabaqcadan proqnozlaşdırma əməliyyatları.
  - `Profiles/`: AutoMapper profilləri.

- **RMS.Contract/ (Məlumat Ötürmə Strukturu)**
  - `DTOs/` (Data Transfer Objects): İstifadeci ilə layihə arasında informasiya daşıyan model obyektləri (məsələn, `LoginDTO`, `RegisterDTO`). Həm Oracle, həm də System tiplərinə ayrılır.

- **RMS.Persitence/ (İnfrastruktur və Data)** *(Qeyd: qovluq adı "Persistence" deyil, belə yazılıb)*
  - `Data/`: Entity Framework Core işlədən `AppDbContext`.
  - `Migrations/`: PostgreSQL məlumat bazasının dəyişiklik kodları.
  - `Repositories/`: `Domain` qatındaki interfeyslərin həqiqi işini görən kod blokları. Həm EF Core üzərindən sistem işləri, həm də `Oracle.ManagedDataAccess.Client` istifadə edərək birbaşa Oracle verilənlər bazasına qoşulan kodlar buradadır.

- **RMS.Presentation/ (API Giriş Qapısı)**
  - `Controllers/`: HTTP API nöqtələri. Routing, POST/GET idarəçiliyi (`AuthController`, `SectorSpendController` və s.).
  - `ExceptionHandler/`: Qlobal error (səhv) oxuma Middleware-ləri (NotFound, Unauthorized və s.).
  - `Program.cs`: Layihənin başlanğıc nöqtəsi. Dependency Injection (ASILIQ), Swagger qeydiyyatı, Serilog və JWT təyinatları burada qurulur.

## 3. Əsas Komponentlər və Onların Məsuliyyətləri (Key Components and Responsibilities)

- **Controllers (API Endpoints):** Kənardan gələn HTTP müraciətlərini qəbul etmək, ilkin validasiya aparmaq və müraciəti uyğun "Service" qatına yönləndirmək məsuliyyətini daşıyır.
- **Application Services (Servislər - Məsələn, `ForecastService`):** Kontrollerdən çağırılan və layihənin bütün işləmə şərtlərinin (if-else biznes məntiqinin) cəmləndiyi moduldur. DTO obyektlərini götürür, işləyir, hesabat yaradır və qaytarır.
- **Background Workers:** Şəbəkədəki tranzaksiya tarixi çoxaldıqca ML forecasting (təxmin yürütmə) funksiyasının düzgün çalışması üçün öyrənmə (retraining) modelini arxa planda async olaraq işə salır (`ModelRetrainBackgroundService`).
- **Repositories (EF Core və Oracle):** Application xidmətlərinin SQL query və ya məlumat bazası logikası ilə maraqlanmamasına, təmiz saxlanmasına xidmət edir. Onlar Data Access işini görürlər (CRUD daxil).
- **Global Error Handlers:** Gözlənilməyən səhvləri "Try-Catch"-ə ehtiyac olmadan layihə mərkəzində tutur və istifadəçiyə anlaşılır bir Response (Exception) qaytarır.

## 4. Sistem Hissələrinin Qarşılıqlı Əlaqəsi (System Interactions)
Komponentlərin bir-biri ilə qarşılıqlı əlaqəsi ardıcıl formada cərəyan edir:
1. Müştəri tətbiqi (məsələn: React.js, Angular və s.) RESTful protokolu vasitəsilə `Presentation` layer-inə HTTP POST/GET sorğusu göndərir.
2. `Presentation` layı JWT Authentication (token yoxlaması) həyata keçirir.
3. Kontroller DI (Dependency Injection) xüsusiyyəti ilə qeydiyyata alınmış System və ya Oracle servisini (`RMS.Application`) çağırır və `Contract` layındakı DTO obyekti funksiyaya arqument kimi ötürülür.
4. Çapari Xidmət (Service), iş məntiqini tətbiq edir. Daha sonra Entity şəklində məlumatlar oxumaq və ya yazmaq üçün `Domain` qatında təyin olunmuş `IRepository` interfeysindən istifadə edir.
5. Inversion of Control prisipinə görə, interfeysin əsl tətbiqi (implementation) asılılıq inyeksiyası ilə `Persistence` layından çağırılır və məlumatlar yoxlanıb DB-dən/DB-yə oxunur/yazılır. 
6. Qayıdan Data "Entity" tipli olduğuna görə, AutoMapper onu "Response DTO"-suna çevirir və geriyə (Kontroller vasitəsilə frontend-ə) tam təhlükəsiz şəkildə qaytarır.

## 5. Məlumat Axını (Data Flow)
Məlumatın bazaya gedib-gəlməsi aşağıdakı axın (pipeline) çərçivəsində mövcuddur:

**Sorğu Oxuma Axını:**  
`Client Request` -> `Controller` -> `Application Service` -> `IRepository` -> `Persistence Repository (EF Core or OracleClient)` -> `Databases`
*Nəticənin Qayıtması:*
`Databases` -> `Entity Response` -> `Service (AutoMapper ilə DTO-ya keçid)` -> `Controller` -> `HTTP 200 OK (JSON)`

Sistem tərkibində Data **2 fərqli məlumat bazasına** ayrılır:
- **PostgreSQL (Sistem DB):** İstifadeci rolları, Refresh Token-lər, Departament/Vəzifə quruluşu kimi internal əməliyyat məlumatları üçün.
- **Oracle (Data Warehouse DB):** "Kart Aktivliyi", "Sektor Xərcləri" və "Dunya/Azərbaycan Xəritəsi üzrə əməliyyatlar" kimi böyük həcmli tranzaksional data üçün. (Adətən sadəcə Oxunma/Hesablama - Read Only fəaliyyət üçün istifadə olunur).

## 6. Əsas Biznes Məntiqinin İzahı (Main Business Logic Formulation)

Sistem əsasən bir neçə sütun ətrafında biznes planını reallaşdırır:

**A. Analitika və Hesabatlılıq (Reporting):** 
Bu, AzərKard şirkətinin əməliyyatlarının hesabat formasına salınmasıdır. İxtisaslaşmış tiplərə (MarketBenchmark, CardActivity, AzMapTransaction) bölünür. `Application/Services/Oracle` şəxsində müxtəlif departamentlərin bank hesabları, sektorlar üzrə edilən xərcləmələr yığılır və dashboard (hesabat) kimi təqdim olunur.

**B. Gələcək Proqnozlaşdırma (ML Forecasting):** 
`ForecastService`-i tətbiq olunmuş maşın öyrənməsi/Data Science modelindən istifadə edərək gələcək maliyyə trendlərini (xərclər və kart fəaliyyətlərinin böyüməsi/azalması qrafiklərini) proqnozlaşdırır. Avtomatlaşdırılmış *BackgroundService* tranzaksiya datasını daim izləyir və modeli avtomatik yeniləyir.

**C. Autentifikasiya və Səlahiyyət (Auth & Authorization):**
Məlumat bazasında məlumatların həssaslığı nəzərə alınaraq güclü Identifikasiya mexanizmi tətbiq olunub. JWT ilə qorunan API, eyni zamanda OTP (Bir dəfəlik parol - One-time password) vasitəsilə parol bərpasını dəstəkləyir (`PasswordResetService`), istifadəçinin sistemə girmə izni və Refresh token logikası idarə olunur.

**D. Bildiriş Sistemi və Konfiqurasiyalar (Threshold & Setup):**
Daxildə Departament, Vəzifələr tənzimlənir, dinamik SQL sorğularına (`SQLQuery` obyekti) dəstək verilir və müəyyən edilmiş limitləri (Thresholds) keçdikcə fərqli istifadəçilərə bildiriş (Notification) və `EmailSender` ilə e-poçt xəbərdarlıqları göndərərək mühitə aid limitəsaslı məlumatlandırma sistemini realizə edib.

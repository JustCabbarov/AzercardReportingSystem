# Azercard Reporting Management System (RMS) - API

Azercard Reporting Management System (RMS) — bank və kart əməliyyatları, acquiring cihazları, market bençmarkları, coğrafi xəritə statistikaları və proqnozlaşdırma (ML/Forecasting) modullarını təmin edən yüksək performanslı **.NET 8 Web API** layihəsidir.

---

## 🏗 Memarlıq və Texnologiyalar

- **Framework**: .NET 8 (C#)
- **Verilənlər Bazaları**:
  - **Oracle Database**: Əməliyyat hesabatları, kart portfeli, cihazlar (POS/ATM), tranzaksiyalar və statistik analitika üçün (yüksək performans üçün **Dapper** və **Oracle.ManagedDataAccess.Core** istifadə olunur).
  - **PostgreSQL**: İstifadəçilər, rollar, autentifikasiya (Identity), audit jurnalları və xəbərdarlıqlar (Alerts) üçün (**Entity Framework Core** istifadə olunur).
- **Təhlükəsizlik**: JWT Bearer Token (ASP.NET Core Identity ilə).
- **Loglama**: Serilog (Həm konsol, həm də gündəlik fırlanan `Logs/log-*.txt` faylları).
- **Konteynerləşdirmə**: Docker və Docker Compose.
- **Sənədləşmə**: Swagger UI (OpenAPI).

---

## 🗄 Verilənlər Bazası Bağlantıları (Connection Strings)

Layihə iki verilənlər bazasından istifadə edir. Bütün konfiqurasiyalar `RMS.Presentation/appsettings.json` və ya mühit dəyişənləri (Environment Variables) vasitəsilə tənzimlənir.

### 1. Oracle Connection String Qaydaları və Formatları

Oracle bağlantısı `OracleConnection` açarı altında təyin olunur. Oracle Managed Data Access həm sadə **EZConnect**, həm də detallı **TNS** formatlarını dəstəkləyir.

#### A. EZConnect Formatı (Tövsiyə olunan / Ən sadə format)
Server IP, port və Service Name (məsələn: Pluggable Database `XEPDB1`, `ORCLPDB` və ya `ORCL`):
```text
Data Source=192.168.1.50:1521/XEPDB1;User Id=AZERCARD;Password=GucluParol123!;
```

#### B. Pooling və Timeout Parametrləri ilə (Production üçün Tövsiyə olunan)
Yüksək yüklü sistemlərdə bağlantı hovuzunu (Connection Pooling) aktivləşdirmək məsləhətdir:
```text
Data Source=192.168.1.50:1521/XEPDB1;User Id=AZERCARD;Password=GucluParol123!;Pooling=true;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;
```

#### C. TNS / SID Formatı (Əgər Service Name əvəzinə SID istifadə olunursa)
```text
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.50)(PORT=1521))(CONNECT_DATA=(SID=ORCL)));User Id=AZERCARD;Password=GucluParol123!;
```

#### D. TNS Service Name ilə Tam Təsvir Formatı
```text
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.50)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCLPDB)));User Id=AZERCARD;Password=GucluParol123!;Pooling=true;
```

> **Qeyd (Oracle İstifadəçi Hüquqları):**  
> `User Id`-də qeyd olunan istifadəçinin sistemdə istifadə olunan hesabat cədvəllərinə və görünüşlərinə (Views: `V_ACQUIRING_SUMMARY`, `V_CARD_PORTFOLIO`, `V_DEVICES`, `V_AZ_MAP`, `V_WORLD_MAP`, `V_SECTOR_SPEND`, və s.) `SELECT` hüququ olmalıdır.

---

### 2. PostgreSQL Connection String

PostgreSQL bağlantısı istifadəçi və sistem məlumatları üçün `PostgreSqlConnection` açarı altında yazılır:

```text
Host=192.168.1.60;Port=5432;Database=AzercardReportingDb;Username=postgres;Password=YourPostgresPassword;
```

---

## ⚙ Konfiqurasiya Faylı: `appsettings.json`

Production mühitinə uyğun tam `appsettings.json` nümunəsi:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=postgres-server;Port=5432;Database=AzercardReportingDb;Username=postgres;Password=YourPostgresPassword;",
    "OracleConnection": "Data Source=oracle-server:1521/XEPDB1;User Id=AZERCARD;Password=YourOraclePassword;Pooling=true;Min Pool Size=5;Max Pool Size=100;"
  },
  "ModelStore": {
    "BasePath": "models"
  },
  "ForecastTraining": {
    "RunAtHour": 2,
    "RunAtMinute": 30
  },
  "JwtSettings": {
    "SecretKey": "BU-MINIMUM-32-SIMVOL-UZUNLUGUNDA-GIZLI-ACARDIR!",
    "Issuer": "RMS.API",
    "Audience": "RMS.Client",
    "AccessTokenExpirationMinutes": 1200,
    "RefreshTokenExpirationDays": 7
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Npgsql": "Error",
        "Microsoft.EntityFrameworkCore": "Error"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "restrictedToMinimumLevel": "Information",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}"
        }
      }
    ],
    "Enrich": [ "FromLogContext" ],
    "Properties": {
      "Application": "AzercardRMS"
    }
  }
}
```

---

## 🚀 Deployment (Quraşdırma) Üsulları

Layihəni istənilən mühitdə deploy etmək mümkündür.

### Metod 1: Docker və Docker Compose ilə Deploy (Tövsiyə olunan)

Bu metod ən asan və etibarlı üsuldur. Serverdə yalnız **Docker** və **Docker Compose** quraşdırılmış olması kifayətdir.

#### 1. Layihə qovluğuna daxil olun:
```bash
cd AzercardReportingSystem
```

#### 2. `docker-compose.yml` faylında connection string-ləri təyin edin:
Faylı redaktə edərək mühit dəyişənlərini (`environment`) daxil edin:

```yaml
services:
  rms-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: rms-api
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__OracleConnection=Data Source=192.168.1.50:1521/XEPDB1;User Id=AZERCARD;Password=YourOraclePassword;Pooling=true;
      - ConnectionStrings__PostgreSqlConnection=Host=192.168.1.60;Port=5432;Database=AzercardReportingDb;Username=postgres;Password=YourPostgresPassword;
      - JwtSettings__SecretKey=BU-MINIMUM-32-SIMVOL-UZUNLUGUNDA-GIZLI-ACARDIR!
    ports:
      - "8080:8080"
    volumes:
      - ./Logs:/app/Logs
      - ./models:/app/models
```

#### 3. Konteyneri qurun və işə salın:
```bash
docker compose up -d --build
```

#### 4. Konteynerin statusunu və logları izləyin:
```bash
docker ps
docker logs -f rms-api
```

---

### Metod 2: Linux Serverdə (Ubuntu / Debian / RHEL) Systemd ilə Deploy

#### 1. Lazımi tələblər:
Serverdə `.NET 8 Runtime` və ya `ASP.NET Core Runtime 8.0` quraşdırılmalıdır:
```bash
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0
```

#### 2. Layihəni dərc edin (Publish):
Lokal kompüterdə və ya CI/CD boru xəttində Release rejimində publish edin:
```bash
dotnet publish RMS.Presentation/RMS.Presentation.csproj -c Release -o ./publish
```
Yaranan `./publish` qovluğundakı faylları serverə köçürün (məsələn: `/var/www/azercard-rms`).

#### 3. Serverdə qovluq hüquqlarını tənzimləyin:
```bash
sudo chown -R www-data:www-data /var/www/azercard-rms
sudo chmod -R 755 /var/www/azercard-rms
```

#### 4. Systemd Service faylı yaradın:
`/etc/systemd/system/azercard-rms.service` faylı yaradın:
```ini
[Unit]
Description=Azercard Reporting Management System API
After=network.target

[Service]
WorkingDirectory=/var/www/azercard-rms
ExecStart=/usr/bin/dotnet /var/www/azercard-rms/RMS.Presentation.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=azercard-rms
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

#### 5. Servisi aktivləşdirin və başladın:
```bash
sudo systemctl daemon-reload
sudo systemctl enable azercard-rms.service
sudo systemctl start azercard-rms.service
sudo systemctl status azercard-rms.service
```

#### 6. Nginx Reverse Proxy (İstəyə görə):
`/etc/nginx/sites-available/azercard-rms`:
```nginx
server {
    listen 80;
    server_name rms-api.yourcompany.az;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

---

### Metod 3: Windows Server (IIS) üzərində Deploy

1. Serverdə **ASP.NET Core 8.0 Hosting Bundle** quraşdırın.
2. IIS Manager-də yeni **Website** yaradın.
3. **Application Pool** parametrlərində `.NET CLR Version`-u **"No Managed Code"** seçin.
4. `dotnet publish` nəticəsində yaranan faylları sayt qovluğuna yerləşdirin.
5. Saytın `appsettings.json` faylında Oracle və PostgreSQL connection string-lərini təyin edin.
6. IIS saytı başladın.

---

## 🗃 PostgreSQL İlkin Miqrasiya (Database Migration)

İlk dəfə təmiz PostgreSQL bazasına qoşulduqda cədvəllərin yaranması üçün miqrasiya əmrini icra edin:

```bash
dotnet ef database update --project RMS.Persitence --startup-project RMS.Presentation
```

---

## 🔍 Yoxlanış və Swagger API

Deploy edildikdən sonra API-nin işləkliyini yoxlamaq üçün brauzerdə aşağıdakı ünvana daxil olun:

```text
http://<SERVER_IP>:8080/swagger
```

- **Swagger UI** vasitəsilə bütün endpoint-lər (Acquiring, Devices, Card Portfolio, Sector Spend, Market Benchmark, World Map, Az Map, Auth) görünəcəkdir.
- Autentifikasiya üçün `/api/Auth/login` endpoint-indən Bearer token əldə edib Swagger-də **Authorize** düyməsi ilə daxil edə bilərsiniz.

---

## 🛠 Xətaların Həlli (Troubleshooting)

1. **`Oracle.ManagedDataAccess.Client.OracleException: ORA-12170: TNS:Connect timeout occurred`**
   - Səbəb: Server Oracle bazasının IP və portuna (1521) şəbəkə səviyyəsində çata bilmir.
   - Həll: Serverdən `telnet <oracle_ip> 1521` və ya `Test-NetConnection -ComputerName <oracle_ip> -Port 1521` ilə portun açıq olmasını yoxlayın. Firewall və ya təhlükəsizlik qruplarını (Security Groups) yoxlayın.

2. **`ORA-12514: TNS:listener does not currently know of service requested in connect descriptor`**
   - Səbəb: Connection String-də göstərilən Service Name (məsələn: `XEPDB1`) Oracle Listener tərəfindən tanınmır.
   - Həll: Oracle serverdə `lsnrctl status` əmri ilə dəqiq servis adını öyrənin və `Data Source=host:port/DƏQİQ_SERVİS_ADI` olaraq daxil edin.

3. **Log fayllarını harada tapmaq olar?**
   - Bütün sistem logları layihənin kök qovluğundakı `Logs/log-YYYYMMDD.txt` fayllarına yazılır.
   - Docker istifadə edildikdə: `docker logs -f rms-api` əmri ilə canlı logları izləmək mümkündür.

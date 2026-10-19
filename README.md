# Verilənlər Bazası Qoşulma Təlimatı

Layihənin işləməsi üçün **PostgreSQL** və **Oracle** bazalarının connection string-ləri qeyd olunmalıdır.

Bu ayarlar **`RMS.Presentation/appsettings.json`** faylında yazılır:

```json
{
  "ConnectionStrings": {
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YourPassword;",
    "OracleConnection": "Data Source=localhost:1521/XEPDB1;User Id=AZERCARD;Password=YourPassword;"
  }
}
```

---

## 1. Oracle Bağlantısı (`OracleConnection`)

Layihə Oracle bazasından hesabat məlumatlarını oxumaq üçün istifadə edir.

### Formatlar:

- **Sadə format (Service Name / PDB ilə - Ən çox istifadə olunan):**
  ```text
  Data Source=SERVER_IP:1521/SERVICE_NAME;User Id=ISTIFADECI_ADI;Password=PAROL;
  ```
  *Nümunə:*
  ```text
  Data Source=192.168.1.100:1521/XEPDB1;User Id=AZERCARD;Password=MySecretPass123;
  ```

- **SID ilə (Əgər Service Name əvəzinə SID istifadə olunursa):**
  ```text
  Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=SERVER_IP)(PORT=1521))(CONNECT_DATA=(SID=ORCL)));User Id=ISTIFADECI_ADI;Password=PAROL;
  ```

- **Connection Pool ilə (Tövsiyə olunur):**
  ```text
  Data Source=SERVER_IP:1521/SERVICE_NAME;User Id=ISTIFADECI_ADI;Password=PAROL;Pooling=true;Min Pool Size=5;Max Pool Size=100;
  ```

> **Qeyd:** Göstərilən Oracle istifadəçisinin hesabat cədvəllərinə və view-lərinə (`V_ACQUIRING_SUMMARY`, `V_CARD_PORTFOLIO`, `V_DEVICES`, `V_AZ_MAP`, `V_WORLD_MAP` və s.) `SELECT` icazəsi olmalıdır.

---

## 2. PostgreSQL Bağlantısı (`PostgreSqlConnection`)

İstifadəçilər, rollar və login məlumatları üçün istifadə olunur:

```text
Host=SERVER_IP;Port=5432;Database=BAZA_ADI;Username=ISTIFADECI;Password=PAROL;
```
*Nümunə:*
```text
Host=localhost;Port=5432;Database=AzercardReportingDb;Username=postgres;Password=12345;
```

---

## 3. Docker ilə işə salarkən (`docker-compose.yml`)

Əgər Docker istifadə olunursa, connection string-ləri birbaşa `docker-compose.yml` faylında `environment` bölməsində yaza bilərsiniz:

```yaml
services:
  rms-api:
    environment:
      - ConnectionStrings__OracleConnection=Data Source=192.168.1.100:1521/XEPDB1;User Id=AZERCARD;Password=MySecretPass123;
      - ConnectionStrings__PostgreSqlConnection=Host=192.168.1.100;Port=5432;Database=AzercardReportingDb;Username=postgres;Password=12345;
```

İşə salmaq üçün:
```bash
docker compose up -d --build
```

---

## 4. Birbaşa (.NET CLI ilə) işə salmaq

1. `RMS.Presentation/appsettings.json` faylında yuxarıdakı string-ləri yazın.
2. Əmri icra edin:
   ```bash
   dotnet run --project RMS.Presentation
   ```
3. API və Swagger ünvanı: `http://localhost:8080/swagger`

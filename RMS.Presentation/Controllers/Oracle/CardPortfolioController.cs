using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;

namespace RMS.API.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardPortfolioController : ControllerBase
    {
        private readonly ICardPortfolioService _service;

        public CardPortfolioController(ICardPortfolioService service)
        {
            _service = service;
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
            => Ok(await _service.GetFilterOptionsAsync());


        [HttpGet("top-schemes")]
        public async Task<IActionResult> GetTopSchemes([FromQuery] CardPortfolioFilter filter)
            => Ok(await _service.GetTopCardsAsync(filter));

        [HttpGet("cross-table")]
        public async Task<IActionResult> GetCrossTable([FromQuery] CrossTableRequest request)
            => Ok(await _service.GetCrossTableAsync(request));

        [HttpGet("pay-chart")]
        public async Task<IActionResult> GetPayChart([FromQuery] PayChartRequest request)
            => Ok(await _service.GetPayChartAsync(request));

        [HttpGet("trend")]
        public async Task<IActionResult> GetTrend([FromQuery] TrendChartRequest request)
            => Ok(await _service.GetTrendAsync(request));

        [HttpGet("xy-chart")]
        public async Task<IActionResult> GetXyChart([FromQuery] XyChartRequest request)
            => Ok(await _service.GetXyChartAsync(request));
    }
}

//=== ENDPOINT → DASHBOARD HİSSƏSİ ===

//────────────────────────────────────────────────────────
//1.GET api / cards / filter - options
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Aşağıdakı filterlər + Sol parametr sütunu dropdown dəyərləri
//Əhatə etdiyi UI elementləri:
//  - Aşağıdakı filter dropdown-ları:
//      Status_3D, CONTACTLESS_STATUS, Bank_Name, Exp_status, Product_type
//  - Sol sütundakı parametr seçimləri üçün mövcud dəyərlər:
//      Bank_Name, Region_Name, Payment_scheme, BASE_CURRENCY_NAME,
//      PRODUCT_TYPE, CARD_PRODUCT_NAME, Status_3D, Contactless_status
//  - Tarix aralığı üçün min/max ay
//Nə qaytarır: Bütün dropdown-ların doldurulması üçün lazım olan unikal dəyərlər siyahısı

//────────────────────────────────────────────────────────
//2. GET api/cards/latest-month
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Səhifə ilk açıldığında default ay seçimi
//Əhatə etdiyi UI elementləri:
//  - Dashboard ilk yükləndikdə hansı ayın göstəriləcəyini müəyyən edir
//  - Bütün digər endpointlər bu ay ilə çağırılır by default
//Nə qaytarır: Ən son mövcud report_month dəyəri (DateTime)

//────────────────────────────────────────────────────────
//3. GET api/cards/top-schemes
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Yuxarı hissədəki rəngli kartlar (VISA, MC, AMEX, JCB)
//Əhatə etdiyi UI elementləri:
//  - Hər kart üzərindəki böyük rəqəm (TotalCards)
//  - Sol ox işarəsi → keçən ay ilə müqayisə (MomChange + MomIsUp)
//  - Sağ ox işarəsi → ümumi içindəki payı (SharePercent + ShareIsUp)
//  - Kartın altındakı breakdown: Əmək/Sosial/Kredit/Digər sayları
//  - Yuxarı sol küncdəki "Cəm" rəqəmi (GrandTotal)
//  - Sol parametr sütunundan seçim edildikdə bu endpoint yenidən çağırılır
//    (məs. Bank_Name seçildikdə → hər bankın VISA/MC/... üzrə sayları)
//Nə qaytarır: GrandTotal + hər payment scheme üçün say, pay%, MoM%,
//             Əmək/Sosial/Kredit/Digər breakdown

//────────────────────────────────────────────────────────
//4. GET api/cards/cross-table
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Ortadakı əsas cədvəl (sol hissə)
//Əhatə etdiyi UI elementləri:
//  - Sətirlərdə: seçilmiş parametr dəyərləri
//    (məs. rowDimension=bank_name → ABB, Rabitə, Respublika...)
//  - Sütunlarda: Əmək | Sosial | Kredit | Taksit | ... (product_type dəyərləri)
//  - Hər xanada: say + MoM ox işarəsi (artdı/azaldı)
//  - Sütunların sağa scroll edilməsi bu endpointin qaytardığı sütunlar üzrədir
//  - Sətirlərin aşağı scroll edilməsi bu endpointin qaytardığı sətirlər üzrədir
//Nə qaytarır: ColumnHeaders (sütun adları) + Rows (hər sətir üçün label,
//             total, və hər product_type üzrə say + MoM%)

//────────────────────────────────────────────────────────
//5. GET api/cards/pay-chart
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Sağ panel → "Pay" toggle seçildikdə görünən donut chart
//Əhatə etdiyi UI elementləri:
//  - Sağdakı dairəvi (donut) qrafik — 100% yazılmış
//  - Hər dilimin faizi və sayı
//  - "Pay" düyməsinə basıldıqda aktivləşir
//  - Yuxarıda hər hansı kartı seçdikdə (məs. VISA) ona əsasən dəyişir
//Nə qaytarır: Hər dimension dəyəri üçün say + faiz (məs. VISA 60%, MC 38%...)

//────────────────────────────────────────────────────────
//6. GET api/cards/trend
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Sağ panel → "Trend" toggle seçildikdə görünən qrafik
//Əhatə etdiyi UI elementləri:
//  - "Trend" düyməsinə basıldıqda donut-un yerini alan zaman seriyası qrafiki
//  - By default: seçilmiş parametrin ümumi artma/azalma tendensiyası
//  - Yuxarıda VISA kartına klik edildikdə → yalnız VISA-nın trend-i göstərilir
//  - "ay" / "il" toggle-u (granularity=month|year)
//  - Sağda olan Trend məlumatlar yuxarıda olan click zamanı dəyişir
//Nə qaytarır: Hər series üçün (label + period + count) zaman nöqtələri

//────────────────────────────────────────────────────────
//7. GET api/cards/xy-chart
//────────────────────────────────────────────────────────
//Dashboard hissəsi: Sol üstdəki X/Y oxları olan analiz sahəsi
//Əhatə etdiyi UI elementləri:
//  - Qrafikin üstündəki iki dropdown: X seçimi + Y seçimi
//  - X-də Bank_name, Y-də Base_Currency_name seçildikdə →
//    hər bank üzrə currency bölgüsü cədvəldə əks olunur
//  - Hər xana üçün say + həmin X dəyəri içindəki faiz payı (SharePct)
//Nə qaytarır: XLabels + YLabels + Cells (x_label, y_label, count, share_pct);
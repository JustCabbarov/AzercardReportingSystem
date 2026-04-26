using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class AzMapController : ControllerBase
    {
        private readonly IAzMapService _service;

        public AzMapController(IAzMapService service)
        {
            _service = service;
        }

        /// <summary>
        /// Bütün Azərbaycan xəritəsi tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AzMapTransaction>>> GetAll(
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə Azərbaycan xəritəsi tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<IEnumerable<AzMapTransaction>>> GetByBank(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aya görə Azərbaycan xəritəsi tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet("month")]
        public async Task<ActionResult<IEnumerable<AzMapTransaction>>> GetByMonth(
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMonthAsync(month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Region adına görə tranzaksiyaları qaytarır.
        /// </summary>
        [HttpGet("region/{regionNameClean}")]
        public async Task<ActionResult<IEnumerable<AzMapTransaction>>> GetByRegion(
            string regionNameClean, CancellationToken ct = default)
        {
            var result = await _service.GetByRegionAsync(regionNameClean, ct);
            return Ok(result);
        }

        /// <summary>
        /// Cihaz tipinə görə (POS, ATM, mPOS və s.) tranzaksiyaları qaytarır.
        /// </summary>
        [HttpGet("device/{deviceType}")]
        public async Task<ActionResult<IEnumerable<AzMapTransaction>>> GetByDeviceType(
            string deviceType, CancellationToken ct = default)
        {
            var result = await _service.GetByDeviceTypeAsync(deviceType, ct);
            return Ok(result);
        }

        /// <summary>
        /// Şəhərə görə tranzaksiyaları qaytarır.
        /// </summary>
        [HttpGet("city/{cityClean}")]
        public async Task<ActionResult<IEnumerable<AzMapTransaction>>> GetByCity(
            string cityClean, CancellationToken ct = default)
        {
            var result = await _service.GetByCityAsync(cityClean, ct);
            return Ok(result);
        }

        /// <summary>
        /// Azərbaycan xəritəsi üçün — hər regionun ümumi tranzaksiya məbləği.
        /// </summary>
        [HttpGet("bank/{bankName}/amount-by-region")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetAmountByRegion(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetAmountByRegionAsync(bankName, month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Terminal tipi üzrə cihaz sayı — POS, ATM, mPOS və s.
        /// </summary>
        [HttpGet("bank/{bankName}/device-count")]
        public async Task<ActionResult<Dictionary<string, long>>> GetDeviceCountByType(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetDeviceCountByTypeAsync(bankName, month, ct);
            return Ok(result);
        }
    }
}

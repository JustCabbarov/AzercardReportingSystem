using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorldMapController : ControllerBase
    {
        private readonly IWorldMapService _service;

        public WorldMapController(IWorldMapService service)
        {
            _service = service;
        }

        /// <summary>
        /// Bütün dünya xəritəsi tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorldMapTransaction>>> GetAll(
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə dünya xəritəsi tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<IEnumerable<WorldMapTransaction>>> GetByBank(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aya görə dünya xəritəsi tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet("month")]
        public async Task<ActionResult<IEnumerable<WorldMapTransaction>>> GetByMonth(
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMonthAsync(month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Mənbə ölkəyə görə tranzaksiyaları qaytarır.
        /// </summary>
        [HttpGet("country/{country}")]
        public async Task<ActionResult<IEnumerable<WorldMapTransaction>>> GetBySourceCountry(
            string country, CancellationToken ct = default)
        {
            var result = await _service.GetBySourceCountryAsync(country, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bankın issuing tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/issuing")]
        public async Task<ActionResult<IEnumerable<WorldMapTransaction>>> GetIssuing(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetIssuingAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bankın acquiring tranzaksiyalarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/acquiring")]
        public async Task<ActionResult<IEnumerable<WorldMapTransaction>>> GetAcquiring(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetAcquiringAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Heat map üçün — hər ölkənin ümumi tranzaksiya məbləği.
        /// </summary>
        [HttpGet("heatmap")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetAmountByCountry(
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetAmountByCountryAsync(month, ct);
            return Ok(result);
        }
    }
}

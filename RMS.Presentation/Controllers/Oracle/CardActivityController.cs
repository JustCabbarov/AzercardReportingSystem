using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardActivityController : ControllerBase
    {
        private readonly ICardActivityService _service;

        public CardActivityController(ICardActivityService service)
        {
            _service = service;
        }

        /// <summary>
        /// Bütün kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CardActivity>>> GetAll(
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<IEnumerable<CardActivity>>> GetByBank(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aya görə kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("month")]
        public async Task<ActionResult<IEnumerable<CardActivity>>> GetByMonth(
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMonthAsync(month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Məhsul tipinə görə kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("product/{productType}")]
        public async Task<ActionResult<IEnumerable<CardActivity>>> GetByProductType(
            string productType, CancellationToken ct = default)
        {
            var result = await _service.GetByProductTypeAsync(productType, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank + aktivlik seqmentinə görə kart məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/segment/{segment}")]
        public async Task<ActionResult<IEnumerable<CardActivity>>> GetByActivitySegment(
            string bankName, string segment, CancellationToken ct = default)
        {
            var result = await _service.GetByActivitySegmentAsync(bankName, segment, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank + ay üzrə ortalama contactless istifadə faizini qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/avg-contactless-rate")]
        public async Task<ActionResult<decimal>> GetAvgContactlessRate(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetAvgContactlessRateAsync(bankName, month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Seqment payı — hər seqmentin ümumi kartlara nisbəti (%).
        /// </summary>
        [HttpGet("bank/{bankName}/segment-distribution")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetSegmentDistribution(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetSegmentDistributionAsync(bankName, month, ct);
            return Ok(result);
        }
    }
}

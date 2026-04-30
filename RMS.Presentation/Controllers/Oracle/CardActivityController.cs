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
        /// Bank adına görə kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<PagedResult<CardActivity>>> GetByBank(
            string bankName,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aya görə kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("month")]
        public async Task<ActionResult<PagedResult<CardActivity>>> GetByMonth(
            [FromQuery] DateTime month,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMonthAsync(month, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Məhsul tipinə görə kart aktivlik məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("product/{productType}")]
        public async Task<ActionResult<PagedResult<CardActivity>>> GetByProductType(
            string productType,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetByProductTypeAsync(productType, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank + aktivlik seqmentinə görə kart məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/segment/{segment}")]
        public async Task<ActionResult<PagedResult<CardActivity>>> GetByActivitySegment(
            string bankName,
            string segment,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetByActivitySegmentAsync(bankName, segment, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Universal filter.
        /// </summary>
        [HttpGet("filter")]
        public async Task<ActionResult<PagedResult<CardActivity>>> Filter(
            [FromQuery] CardActivity f,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.FilterAsync(f, pageReq, ct);
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
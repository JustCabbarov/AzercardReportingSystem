using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewCardController : ControllerBase
    {
        private readonly INewCardService _service;

        public NewCardController(INewCardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Bütün yeni kart aktivasiya məlumatlarını qaytarır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NewCardActivation>>> GetAll(
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə yeni kart aktivasiya məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<IEnumerable<NewCardActivation>>> GetByBank(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// İlk aya görə yeni kart aktivasiya məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("first-month")]
        public async Task<ActionResult<IEnumerable<NewCardActivation>>> GetByFirstMonth(
            [FromQuery] DateTime firstMonth,
            CancellationToken ct = default)
        {
            var result = await _service.GetByFirstMonthAsync(firstMonth, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank + seqmentə görə yeni kart aktivasiya məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/segment/{segment}")]
        public async Task<ActionResult<IEnumerable<NewCardActivation>>> GetBySegment(
            string bankName, string segment, CancellationToken ct = default)
        {
            var result = await _service.GetBySegmentAsync(bankName, segment, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bankın inaktiv kartlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/inactive")]
        public async Task<ActionResult<IEnumerable<NewCardActivation>>> GetInactiveCards(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetInactiveCardsAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank üzrə ortalama 3 aylıq aktivasyon faizini qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/avg-activation-rate")]
        public async Task<ActionResult<decimal>> GetAvgActivationRate(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetAvgActivationRateAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Seqment paylanmasını (%) qaytarır — hər seqmentin kart sayına görə faizi.
        /// </summary>
        [HttpGet("bank/{bankName}/segment-distribution")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetSegmentDistribution(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetSegmentDistributionAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// İlk istifadəyə qədər ortalama ay sayını qaytarır.
        /// Yalnız aktivləşmiş kartlar nəzərə alınır.
        /// </summary>
        [HttpGet("bank/{bankName}/avg-months-to-first-use")]
        public async Task<ActionResult<double>> GetAvgMonthsToFirstUse(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetAvgMonthsToFirstUseAsync(bankName, ct);
            return Ok(result);
        }
    }
}

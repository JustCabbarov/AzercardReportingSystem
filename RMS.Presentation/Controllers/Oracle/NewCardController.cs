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

        private static readonly HashSet<string> ValidSegments =
            ["EarlyActive", "DelayedActive", "SlowActive", "Inactive"];

        public NewCardController(INewCardService service)
        {
            _service = service;
        }

        private static PageRequest ToPageRequest(int page, int pageSize) =>
            new() { Page = Math.Max(1, page), PageSize = Math.Clamp(pageSize, 1, 100) };

        // GET api/newcard?page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<PagedResult<NewCardActivation>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ToPageRequest(page, pageSize), ct);
            return Ok(result);
        }

        // GET api/newcard/bank/ABB
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<PagedResult<NewCardActivation>>> GetByBank(
            string bankName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return BadRequest("bankName boş ola bilməz.");

            var result = await _service.GetByBankAsync(bankName, ToPageRequest(page, pageSize), ct);
            return Ok(result);
        }

        // GET api/newcard/first-month?firstMonth=2024-01-01
        [HttpGet("first-month")]
        public async Task<ActionResult<PagedResult<NewCardActivation>>> GetByFirstMonth(
            [FromQuery] DateTime firstMonth,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (firstMonth == default)
                return BadRequest("firstMonth düzgün deyil.");

            var result = await _service.GetByFirstMonthAsync(firstMonth, ToPageRequest(page, pageSize), ct);
            return Ok(result);
        }

        // GET api/newcard/bank/ABB/segment/EarlyActive
        [HttpGet("bank/{bankName}/segment/{segment}")]
        public async Task<ActionResult<PagedResult<NewCardActivation>>> GetBySegment(
            string bankName,
            string segment,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return BadRequest("bankName boş ola bilməz.");

            if (!ValidSegments.Contains(segment))
                return BadRequest($"Yanlış segment. Mümkün dəyərlər: {string.Join(", ", ValidSegments)}");

            var result = await _service.GetBySegmentAsync(bankName, segment, ToPageRequest(page, pageSize), ct);
            return Ok(result);
        }

        // GET api/newcard/bank/ABB/inactive
        [HttpGet("bank/{bankName}/inactive")]
        public async Task<ActionResult<PagedResult<NewCardActivation>>> GetInactiveCards(
            string bankName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return BadRequest("bankName boş ola bilməz.");

            var result = await _service.GetInactiveCardsAsync(bankName, ToPageRequest(page, pageSize), ct);
            return Ok(result);
        }

        // GET api/newcard/filter?bankName=ABB&productType=DEBIT&page=1
        [HttpGet("filter")]
        public async Task<ActionResult<PagedResult<NewCardActivation>>> Filter(
            [FromQuery] string? bankName,
            [FromQuery] string? cardProductName,
            [FromQuery] string? cardBrandName,
            [FromQuery] string? productType,
            [FromQuery] string? regionNameClean,
            [FromQuery] DateTime? firstMonth,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var f = new NewCardActivation
            {
                BankName = bankName ?? "",
                CardProductName = cardProductName ?? "",
                CardBrandName = cardBrandName,
                ProductType = productType,
                RegionNameClean = regionNameClean,
                FirstMonth = firstMonth ?? default
            };

            var result = await _service.FilterAsync(f, ToPageRequest(page, pageSize), ct);
            return Ok(result);
        }

        // GET api/newcard/bank/ABB/avg-activation-rate
        [HttpGet("bank/{bankName}/avg-activation-rate")]
        public async Task<ActionResult<decimal>> GetAvgActivationRate(
            string bankName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return BadRequest("bankName boş ola bilməz.");

            var result = await _service.GetAvgActivationRateAsync(bankName, ct);
            return Ok(result);
        }

        // GET api/newcard/bank/ABB/segment-distribution
        [HttpGet("bank/{bankName}/segment-distribution")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetSegmentDistribution(
            string bankName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return BadRequest("bankName boş ola bilməz.");

            var result = await _service.GetSegmentDistributionAsync(bankName, ct);
            return Ok(result);
        }
    }
}
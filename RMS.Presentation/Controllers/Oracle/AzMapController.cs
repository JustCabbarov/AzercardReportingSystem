using Microsoft.AspNetCore.Mvc;
using RMS.Contract.DTOs;
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

        [HttpGet("filter")]
        public async Task<ActionResult<PagedResult<AzMapTransaction>>> Filter(
            [FromQuery] AzMapFilterRequest f,
            CancellationToken ct = default)
        {
            var pageReq = new PageRequest { Page = f.Page, PageSize = f.PageSize };
            var result = await _service.FilterAsync(f, pageReq, ct);
            return Ok(result);
        }

        [HttpGet("heatmap")]
        public async Task<ActionResult<IEnumerable<CityAmountDto>>> GetHeatmap(  
            [FromQuery] string? bankName,
            [FromQuery] string? sourceCity,
            [FromQuery] DateTime? month,
            CancellationToken ct = default)
        {
            var result = await _service.GetHeatmapDataAsync(bankName, sourceCity, month, ct);
            return Ok(result);
        }

        [HttpGet("cities")]
        public async Task<ActionResult<IEnumerable<string>>> GetDistinctCities(
            CancellationToken ct = default)
        {
            var result = await _service.GetDistinctCitiesAsync(ct);
            return Ok(result);
        }

        [HttpGet("regions")]
        public async Task<ActionResult<IEnumerable<string>>> GetDistinctRegions(
            CancellationToken ct = default)
        {
            var result = await _service.GetDistinctRegionsAsync(ct);
            return Ok(result);
        }

        [HttpGet("banks")]
        public async Task<ActionResult<IEnumerable<string>>> GetDistinctBanks(
            CancellationToken ct = default)
        {
            var result = await _service.GetDistinctBanksAsync(ct);
            return Ok(result);
        }
    }
}
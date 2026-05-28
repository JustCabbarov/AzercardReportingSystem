using Microsoft.AspNetCore.Mvc;
using RMS.Contract.DTOs;
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
        /// Bütün fieldlər üzrə filter və səhifələmə ilə tranzaksiyaları qaytarır.
        /// </summary>
        [HttpGet("filter")]
        public async Task<ActionResult<PagedResult<WorldMapTransaction>>> Filter(
            [FromQuery] WorldMapTransaction f,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.FilterAsync(f, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Heat map üçün — hər ölkənin ümumi tranzaksiya məbləği və koordinatları.
        /// </summary>
        [HttpGet("heatmap")]
        public async Task<ActionResult<IEnumerable<CountryAmountDto>>> GetAmountByCountry(
            [FromQuery] string? bankName,
            [FromQuery] DateTime? month,
            CancellationToken ct = default)
        {
            var result = await _service.GetAmountByCountryAsync(bankName, month, ct);
            return Ok(result);
        }
    }
}
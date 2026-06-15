using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.CardTransaction;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardDashboardController : ControllerBase
    {
        private readonly ICardDashboardService _svc;

        public CardDashboardController(ICardDashboardService svc)
            => _svc = svc;

        // GET /api/carddashboard/dashboard
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(DashboardResponse), 200)]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(
            [FromQuery] DashboardFilterRequest filter)
            => Ok(await _svc.GetFullDashboardAsync(filter));

        // GET /api/carddashboard/filters
        [HttpGet("filters")]
        [ProducesResponseType(typeof(FilterLookupResponse), 200)]
        public async Task<ActionResult<FilterLookupResponse>> GetFilters()
            => Ok(await _svc.GetFilterLookupsAsync());
    }
}

using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [ApiController]
    [Route("api/devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IDevicesService _service;

        public DevicesController(IDevicesService service)
        {
            _service = service;
        }

        // GET api/devices/filters
        [HttpGet("filters")]
        public async Task<IActionResult> GetAllFilters()
        {
            var result = new
            {
                BankNames = (await _service.GetFiltersAsync("bank_name")).Select(x => x.DimValue),
                RegionNames = (await _service.GetFiltersAsync("region_name")).Select(x => x.DimValue),
                MccNames = (await _service.GetFiltersAsync("mcc_name")).Select(x => x.DimValue),
                RetailCategories = (await _service.GetFiltersAsync("retail_category")).Select(x => x.DimValue)
            };
            return Ok(result);
        }

        // GET api/devices/summary?reportMonth=2024-03-01&bankName=ABB
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] DateTime reportMonth,
            [FromQuery] string? bankName,
            [FromQuery] string? regionName,
            [FromQuery] string? mccName,
            [FromQuery] string? retailCategory)
        {
            var result = await _service.GetSummaryAsync(
                reportMonth, bankName, regionName, mccName, retailCategory);
            return Ok(result);
        }

        // GET api/devices/share?reportMonth=2024-03-01
        [HttpGet("share")]
        public async Task<IActionResult> GetShare(
            [FromQuery] DateTime reportMonth,
            [FromQuery] string? bankName,
            [FromQuery] string? regionName,
            [FromQuery] string? mccName,
            [FromQuery] string? retailCategory)
        {
            var result = await _service.GetShareAsync(
                reportMonth, bankName, regionName, mccName, retailCategory);
            return Ok(result);
        }

        // GET api/devices/mom-comparison?reportMonth=2024-03-01
        [HttpGet("mom-comparison")]
        public async Task<IActionResult> GetMomComparison(
            [FromQuery] DateTime reportMonth,
            [FromQuery] string? bankName,
            [FromQuery] string? regionName,
            [FromQuery] string? mccName,
            [FromQuery] string? retailCategory)
        {
            var result = await _service.GetMomComparisonAsync(
                reportMonth, bankName, regionName, mccName, retailCategory);
            return Ok(result);
        }

        // GET api/devices/trend?dateFrom=2024-01-01&dateTo=2024-06-01
        [HttpGet("trend")]
        public async Task<IActionResult> GetTrend(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] string? bankName,
            [FromQuery] string? regionName,
            [FromQuery] string? mccName,
            [FromQuery] string? retailCategory)
        {
            var result = await _service.GetTrendAsync(
                dateFrom, dateTo, bankName, regionName, mccName, retailCategory);
            return Ok(result);
        }

        // GET api/devices/xy-analysis?reportMonth=2024-03-01&xDimension=bank_name&yDimension=region_name
        [HttpGet("xy-analysis")]
        public async Task<IActionResult> GetXyAnalysis(
            [FromQuery] DateTime reportMonth,
            [FromQuery] string xDimension,
            [FromQuery] string yDimension,
            [FromQuery] string? bankName,
            [FromQuery] string? regionName,
            [FromQuery] string? mccName,
            [FromQuery] string? retailCategory)
        {
            var result = await _service.GetXyAnalysisAsync(
                reportMonth, xDimension, yDimension,
                bankName, regionName, mccName, retailCategory);
            return Ok(result);
        }

        [HttpGet("latest-month")]
        public async Task<IActionResult> GetLatestMonth()
        {
            var result = await _service.GetLatestReportMonthAsync();
            return Ok(new { reportMonth = result });
        }

        [HttpGet("total")]
        public async Task<IActionResult> GetTotal(
            [FromQuery] DateTime reportMonth,
            [FromQuery] string? bankName,
            [FromQuery] string? regionName,
            [FromQuery] string? mccName,
            [FromQuery] string? retailCategory)
        {
            var result = await _service.GetTotalDevicesAsync(reportMonth, bankName, regionName, mccName, retailCategory);
            return Ok(new { reportMonth, total = result });
        }
    }
}

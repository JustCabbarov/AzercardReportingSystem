using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.DeviceModel;

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

        // GET api/devices/summary?dateFrom=2003-01-01&dateTo=2004-01-01&bankNames=ABC&bankNames=XYZ
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] List<string>? bankNames,
            [FromQuery] List<string>? regionNames,
            [FromQuery] List<string>? mccNames,
            [FromQuery] List<string>? retailCategories)
        {
            var result = await _service.GetSummaryAsync(
                dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);
            return Ok(result);
        }

        // GET api/devices/share?dateFrom=2003-01-01&dateTo=2004-01-01&dimension=bank_name&dimensionValues=ABB&dimensionValues=Kapital
        [HttpGet("share")]
        public async Task<IActionResult> GetShare(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] string? dimension,
            [FromQuery] List<string>? dimensionValues)
        {
            var result = await _service.GetShareAsync(
                dateFrom, dateTo, dimension, dimensionValues);
            return Ok(result);
        }

        // GET api/devices/mom-comparison?dateFrom=2003-01-01&dateTo=2004-01-01&bankNames=ABC&bankNames=XYZ
        [HttpGet("mom-comparison")]
        public async Task<IActionResult> GetMomComparison(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] List<string>? bankNames,
            [FromQuery] List<string>? regionNames,
            [FromQuery] List<string>? mccNames,
            [FromQuery] List<string>? retailCategories)
        {
            var result = await _service.GetMomComparisonAsync(
                dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);
            return Ok(result);
        }

        // GET api/devices/trend?dateFrom=2003-01-01&dateTo=2004-01-01&dimension=bank_name&dimensionValues=ABB&dimensionValues=Kapital
        [HttpGet("trend")]
        public async Task<IActionResult> GetTrend(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] string? dimension,
            [FromQuery] List<string>? dimensionValues)
        {
            var result = await _service.GetTrendAsync(new DeviceTrendRequest
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Dimension = dimension,
                DimensionValues = dimensionValues
            });
            return Ok(result);
        }

        // GET api/devices/xy-analysis?dateFrom=2003-01-01&dateTo=2004-01-01&xDimension=bank_name&yDimension=region_name
        [HttpGet("xy-analysis")]
        public async Task<IActionResult> GetXyAnalysis(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] string xDimension,
            [FromQuery] string yDimension,
            [FromQuery] List<string>? bankNames,
            [FromQuery] List<string>? regionNames,
            [FromQuery] List<string>? mccNames,
            [FromQuery] List<string>? retailCategories)
        {
            var result = await _service.GetXyAnalysisAsync(
                dateFrom, dateTo, xDimension, yDimension,
                bankNames, regionNames, mccNames, retailCategories);
            return Ok(result);
        }

        // GET api/devices/latest-month
        [HttpGet("latest-month")]
        public async Task<IActionResult> GetLatestMonth()
        {
            var result = await _service.GetLatestReportMonthAsync();
            return Ok(new { reportMonth = result });
        }

        // GET api/devices/total?dateFrom=2003-01-01&dateTo=2004-01-01&bankNames=ABC&bankNames=XYZ
        [HttpGet("total")]
        public async Task<IActionResult> GetTotal(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] List<string>? bankNames,
            [FromQuery] List<string>? regionNames,
            [FromQuery] List<string>? mccNames,
            [FromQuery] List<string>? retailCategories)
        {
            var result = await _service.GetTotalDevicesAsync(
                dateFrom, dateTo, bankNames, regionNames, mccNames, retailCategories);
            return Ok(result);
        }
    }
}
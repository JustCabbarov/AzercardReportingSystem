using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.DTOs.Position;
using RMS.Contract.Services.System;
using RMS.Domain.Entities;

namespace RMS.Presentation.Controllers.System
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionController : ControllerBase
    {
        private readonly IGenericService<PositionDTO, Position> _departmentService;

        public PositionController(IGenericService<PositionDTO, Position> departmentService)
        {
            _departmentService = departmentService;
        }


        [HttpPost]
        public async Task<IActionResult> AddPosition(PositionDTO positionDto)
        {
            var result = await _departmentService.AddAsync(positionDto);
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePosition(Guid id)
        {
            var result = await _departmentService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPositionById(Guid id)
        {
            var result = await _departmentService.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPositions()
        {
            var result = await _departmentService.GetAllAsync();
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePosition(PositionDTO positionDto)
        {
            var result = await _departmentService.UpdateAsync(positionDto);
            if (result == null)
                return NotFound();
            return Ok(result);

        }
    }
}

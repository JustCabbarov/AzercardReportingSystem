using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RMS.Contract.DTOs;
using RMS.Contract.Services;
using RMS.Contract.Services.System;
using RMS.Domain.Entities;

namespace RMS.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IGenericService<DepartmentDTO, Department> _departmentService;

        public DepartmentController(IGenericService<DepartmentDTO, Department> departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(DepartmentDTO departmentDto)
        {
            var result = await _departmentService.AddAsync(departmentDto);
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var result = await _departmentService.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentById(Guid id)
        {
            var result = await _departmentService.GetByIdAsync(id,
                q => q.Include(d => d.Employees));
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var result = await _departmentService.GetAllAsync(
                q => q.Include(d => d.Employees));
            return Ok(result);
        }
        [HttpPut("Update")]
        public async Task<IActionResult> UpdateDepartment(DepartmentDTO departmentDto)
        {
            var result = await _departmentService.UpdateAsync(departmentDto);
            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}

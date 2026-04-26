
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Application.Exceptions;
using RMS.Contract.DTOs;
using RMS.Contract.Services.System;
using RMS.Domain.Entities;

namespace RMS.Presentation.Controllers.System
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenHandler _tokenHandler;
        private readonly IPasswordResetService _passwordResetService;

        public AuthController(
            IAuthService authService,
            ITokenHandler tokenHandler,
            IPasswordResetService passwordResetService)
        {
            _authService = authService;
            _tokenHandler = tokenHandler;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            await _authService.RegisterAsync(registerDTO);
            return StatusCode(201, new { message = "Qeydiyyat uğurla tamamlandı" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var response = await _authService.LoginAsync(loginDTO);
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDTO request)
        {
            var response = await _tokenHandler.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDTO request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [HttpPost("assign-role")]
        
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDTO request)
        {
            await _authService.AssignRoleAsync(request.UserId, request.RoleName);
            return Ok(new { message = "Rol uğurla təyin edildi" });
        }

        [HttpPost("create-role")]
        
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDTO request)
        {
            var result = await _authService.CreateRoleAsync(request);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return StatusCode(201, new { message = "Rol uğurla yaradıldı" });
        }

        [HttpDelete("remove-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleDTO request)
        {
            var result = await _authService.RemoveRoleAsync(request.UserId, request.RoleName);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        [HttpGet("users/{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoleUsers(Guid roleId)
        {
            var users = await _authService.GetUserByRoleIdAsync(roleId);
            return Ok(users);
        }
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDTO passwordResetDTO)
        {
            var result = await _passwordResetService.ResetPasswordAsync(passwordResetDTO.Email, passwordResetDTO.Token, passwordResetDTO.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new { Message = "Password reset successfully" });
            }
            else
            {
                throw new UnauthorizedException("Password reset UnSuccessfuly");
            }

        }
        [Authorize]
        [HttpPost("SendResetOtp")]

        public async Task<IActionResult> SendResetOtp([FromBody] OTPrequest request)
        {
            await _passwordResetService.SendOtpAsync(request);
            return Ok(new { Message = "Password reset OTP sent successfully" });
        }
    }
}
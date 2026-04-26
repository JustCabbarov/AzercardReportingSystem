using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RMS.Contract.DTOs;
using RMS.Contract.Services.System;
using RMS.Domain.Entities;
using RMS.Domain.Repositories.System;

namespace RMS.Application.Services.System
{

    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenHandler _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            SignInManager<AppUser> signInManager,
            ITokenHandler tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            IGenericRepository<Employee> employeeRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            _logger.LogInformation("Login cəhdi: {Email}", loginDTO.Email);

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user is null)
            {
                _logger.LogWarning("İstifadəçi tapılmadı: {Email}", loginDTO.Email);
                throw new UnauthorizedAccessException("Email və ya şifrə yanlışdır.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDTO.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Şifrə yanlışdır: {Email}", loginDTO.Email);
                throw new UnauthorizedAccessException("Email və ya şifrə yanlışdır.");
            }

            var accessToken = await _tokenService.CreateAccessTokenAsync(user);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(user);

            
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    Convert.ToDouble(_configuration["JwtSettings:RefreshTokenExpirationDays"])),
                IsRevoked = false
            });

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpireAt = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:AccessTokenExpirationMinutes"]))
            };
        }

        public async Task RegisterAsync(RegisterDTO registerDTO)
        {
            _logger.LogInformation("Qeydiyyat cəhdi: {Email}", registerDTO.Email);

            // 1. User-i əvvəl yaradın
            var user = new AppUser
            {
                Email = registerDTO.Email,
                UserName = registerDTO.FirstName + registerDTO.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!createResult.Succeeded)
                throw new Exception(string.Join(", ", createResult.Errors.Select(e => e.Description)));

           
            var employee = new Employee
            {
                FirstName = registerDTO.FirstName,
                LastName = registerDTO.LastName,
               
                Phone = registerDTO.Phone,
                DepartmentId = registerDTO.DepartmentId,
                PositionId = registerDTO.PositionId,
                IsActive = true,
                AppUserId = user.Id   
            };

            await _employeeRepository.AddAsync(employee);

           
            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("Qeydiyyat uğurlu: {Email}, UserId: {UserId}", registerDTO.Email, user.Id);
        }
        public async Task LogoutAsync(string refreshToken)
        {
            _logger.LogInformation("Logout cəhdi");

            await _tokenService.RevokeTokenAsync(refreshToken);
            await _signInManager.SignOutAsync();

            _logger.LogInformation("Logout uğurlu");
        }

        public async Task AssignRoleAsync(Guid userId, string roleName)
        {
            _logger.LogInformation("Rol təyin edilir: UserId: {UserId}, Rol: {Role}", userId, roleName);

            var user = await _userManager.FindByIdAsync(userId.ToString());

            await _userManager.AddToRoleAsync(user!, roleName);

            _logger.LogInformation("Rol uğurla təyin edildi: UserId: {UserId}, Rol: {Role}", userId, roleName);
        }

        public async Task<IdentityResult> CreateRoleAsync(CreateRoleDTO appRole)
        {
            _logger.LogInformation("Rol yaradılır: {Role}", appRole.Name);

            var result = await _roleManager.CreateAsync(new AppRole
            {
                Name = appRole.Name,
                Description = appRole.Description,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Rol uğurla yaradıldı: {Role}", appRole.Name);

            return result;
        }

        public async Task<IList<AppUser>> GetUserByRoleIdAsync(Guid roleId)
        {
            _logger.LogInformation("Rolün istifadəçiləri alınır: RoleId: {RoleId}", roleId);
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            var users = await _userManager.GetUsersInRoleAsync(role.Name);
            _logger.LogInformation("Rolün istifadəçiləri uğurla alındı: RoleId: {RoleId}, UserCount: {UserCount}", roleId, users.Count);
            return users;
        }

        public async Task<IdentityResult> RemoveRoleAsync(Guid userId, string roleName)
        {
            _logger.LogInformation("Rol silinir: UserId: {UserId}, Rol: {Role}", userId, roleName);

            var user = await _userManager.FindByIdAsync(userId.ToString());

            var result = await _userManager.RemoveFromRoleAsync(user!, roleName);

            _logger.LogInformation("Rol uğurla silindi: UserId: {UserId}, Rol: {Role}", userId, roleName);

            return result;
        }

      
    }
}
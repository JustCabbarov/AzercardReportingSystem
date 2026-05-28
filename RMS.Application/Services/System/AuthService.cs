using AutoMapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RMS.Application.Exceptions;
using RMS.Contract.DTOs;
using RMS.Contract.Services;
using RMS.Contract.Services.System;
using RMS.Domain.Entities;
using RMS.Domain.Repositories;
using RMS.Domain.Repositories.System;
using RMS.Persitence.Repositories.System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RMS.Application.Services
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
        private readonly IMapper _mapper;
        private readonly IUnityOfWork _unityOfwork;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            SignInManager<AppUser> signInManager,
            ITokenHandler tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            IGenericRepository<Employee> employeeRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger, IUnityOfWork unityOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
            _logger = logger;
            _unityOfwork= unityOfWork;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            _logger.LogInformation("Login cəhdi: {Email}", loginDTO.Email);

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user is null)
            {
                _logger.LogWarning("İstifadəçi tapılmadı: {Email}", loginDTO.Email);
                throw new InvalidCredentialsException("Email və ya şifrə yanlışdır.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDTO.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Şifrə yanlışdır: {Email}", loginDTO.Email);
                throw new InvalidCredentialsException("Email və ya şifrə yanlışdır.");
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
                UserName = registerDTO.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!createResult.Succeeded)
                throw new InvalidCredentialsException(string.Join(", ", createResult.Errors.Select(e => e.Description)));


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

        public async Task<UserDTO> GetUserByJWTToken(string jwtToken)
        {
            _logger.LogInformation("GetUserByJWTToken başladı. Token: {Token}", jwtToken[..20]);

            if (jwtToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                jwtToken = jwtToken.Substring(7);

            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(jwtToken))
                throw new UnauthorizedAccessException("Token oxunmadı");

            var jwtSecurityToken = tokenHandler.ReadJwtToken(jwtToken);
            _logger.LogInformation("Token oxundu. Claims: {Claims}",
                string.Join(", ", jwtSecurityToken.Claims.Select(c => $"{c.Type}={c.Value}")));

            var userId = jwtSecurityToken.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier
                                  || c.Type == "nameid"
                                  || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            _logger.LogInformation("UserId: {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Token-də user tapılmadı");

            var user = await _userManager.FindByIdAsync(userId);
            _logger.LogInformation("User: {Email}", user?.Email);

            if (user is null)
                throw new KeyNotFoundException("İstifadəçi tapılmadı");

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("Roles: {Roles}", string.Join(", ", roles));



            var userDTO = new UserDTO
            {
                Id = user.Id,
                
                Email = user.Email ?? string.Empty,
                RoleName = roles?.ToList() ?? new List<string>()
            };


            _logger.LogInformation("GetUserByJWTToken uğurla tamamlandı");

            return userDTO;
        }



        public async Task<PaginatedResult<UserDTO>> GetAllUser(UserFilterDTO filter)
        {
            var query = _userManager.Users
                .Include(u => u.Employee)
                .AsQueryable();

            if (filter.UserId.HasValue)
                query = query.Where(u => u.Id == filter.UserId.Value);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(u =>
                    u.Email.ToLower().Contains(filter.Search.ToLower()) ||
                    (u.Employee != null && u.Employee.FirstName.ToLower().Contains(filter.Search.ToLower())) ||
                    (u.Employee != null && u.Employee.LastName.ToLower().Contains(filter.Search.ToLower())));

            if (!string.IsNullOrEmpty(filter.Role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(filter.Role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            if (filter.DepartmentId.HasValue)
                query = query.Where(u => u.Employee != null &&
                                         u.Employee.DepartmentId == filter.DepartmentId);

            if (filter.PositionId.HasValue)
                query = query.Where(u => u.Employee != null &&
                                         u.Employee.PositionId == filter.PositionId);

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var userDtos = new List<UserDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.Employee?.FirstName ?? string.Empty,
                    LastName = user.Employee?.LastName ?? string.Empty,
                    PhoneNumber=user.Employee?.Phone ?? string.Empty,
                    RoleName = roles.ToList()
                });
            }

            return new PaginatedResult<UserDTO>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<List<CreateRoleDTO>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles
                .Select(r => new CreateRoleDTO
                {
                    Name = r.Name,
                    Description = r.Description
                })
                .ToListAsync();

            return roles;
        }
        public async Task<UserDTO> EditUserAsync(Guid userId, EditUserDTO editUserDTO)
        {
            _logger.LogInformation("İstifadəçi yenilənir: UserId: {UserId}", userId);

            var user = await _userManager.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                throw new KeyNotFoundException("İstifadəçi tapılmadı");

            // Email yalnız göndərilibsə yenilə
            if (!string.IsNullOrEmpty(editUserDTO.Email) && editUserDTO.Email != user.Email)
            {
                user.Email = editUserDTO.Email;
                user.UserName = editUserDTO.Email;
                var emailResult = await _userManager.UpdateAsync(user);
                if (!emailResult.Succeeded)
                    throw new InvalidCredentialsException(string.Join(", ", emailResult.Errors.Select(e => e.Description)));
            }

            if (user.Employee is not null)
            {
                user.Employee.FirstName = !string.IsNullOrEmpty(editUserDTO.FirstName)
                    ? editUserDTO.FirstName
                    : user.Employee.FirstName;

                user.Employee.LastName = !string.IsNullOrEmpty(editUserDTO.LastName)
                    ? editUserDTO.LastName
                    : user.Employee.LastName;

                user.Employee.Phone = !string.IsNullOrEmpty(editUserDTO.Phone)
                    ? editUserDTO.Phone
                    : user.Employee.Phone;

                user.Employee.DepartmentId = editUserDTO.DepartmentId.HasValue
                    ? editUserDTO.DepartmentId.Value
                    : user.Employee.DepartmentId;

                user.Employee.PositionId = editUserDTO.PositionId.HasValue
                    ? editUserDTO.PositionId.Value
                    : user.Employee.PositionId;

                await _employeeRepository.UpdateAsync(user.Employee);
                await _unityOfwork.SaveChangesAsync();
            }

            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("İstifadəçi uğurla yeniləndi: UserId: {UserId}", userId);

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.Employee?.FirstName ?? string.Empty,
                LastName = user.Employee?.LastName ?? string.Empty,
                RoleName = roles.ToList()
            };


        }
        public async Task DeleteUserAsync(Guid userId)
        {
            _logger.LogInformation("İstifadəçi silinir: UserId: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
                throw new KeyNotFoundException("İstifadəçi tapılmadı");

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
                throw new InvalidCredentialsException(string.Join(", ", result.Errors.Select(e => e.Description)));

            _logger.LogInformation("İstifadəçi uğurla silindi: UserId: {UserId}", userId);
        }
    }


    
}
using Microsoft.AspNetCore.Identity;
using RMS.Contract.DTOs;
using RMS.Domain.Entities;

namespace RMS.Contract.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
        Task RegisterAsync(RegisterDTO registerDTO);
        Task LogoutAsync(string refreshToken);
        Task AssignRoleAsync(Guid userId, string roleName);
        Task<IdentityResult> CreateRoleAsync(CreateRoleDTO role);
        Task<IList<AppUser>> GetUserByRoleIdAsync(Guid roleId);
        Task<IdentityResult> RemoveRoleAsync(Guid userId, string roleName);
        Task<UserDTO> GetUserByJWTToken(string jwtToken);
        Task<PaginatedResult<UserDTO>> GetAllUser(UserFilterDTO filter);
        Task<List<CreateRoleDTO>> GetAllRolesAsync();
        Task<UserDTO> EditUserAsync(Guid userId, EditUserDTO editUserDTO);
        Task DeleteUserAsync(Guid userId);

    }
}
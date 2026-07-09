using AlumniManagementApi.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetUsersAsync();
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserRoleAsync(string email, UpdateRoleRequest request, Guid? performingUserId, string? ipAddress = null);
        Task<bool> DeleteUserAsync(string email, Guid? performingUserId, string? ipAddress = null);
    }
}

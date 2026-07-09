using AlumniManagementApi.DTOs;

namespace AlumniManagementApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress = null);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
    }
}

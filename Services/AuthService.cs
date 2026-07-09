using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using AlumniManagementApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlumniManagementApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IAuditService _auditService;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings, IAuditService auditService)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _auditService = auditService;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress = null)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            // Verify Role exists (Student role is 3)
            var role = await _context.Roles.FindAsync(3);
            if (role == null)
            {
                throw new InvalidOperationException("Default role 'Student' not found in database.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            _context.Users.Add(user);

            // Create initial Alumni Profile for the new user to store their Name and Email
            var profile = new AlumniProfile
            {
                UserId = user.Id,
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = string.Empty,
                degree = string.Empty,
                currentCompany = string.Empty,
                currentRole = string.Empty,
                location = string.Empty,
                LinkedinURL = string.Empty
            };
            _context.AlumniProfiles.Add(profile);

            await _context.SaveChangesAsync();

            // Log the mutating register action
            await _auditService.LogAsync("User.Register", "User", user.Id.ToString(), user.Id, ipAddress, $"User registered with email: {user.Email}");

            var token = GenerateJwtToken(user, role.RoleName);

            return new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = role.RoleName
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return null;
            }

            bool verificationResult = false;
            try
            {
                verificationResult = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Fallback for legacy or plain-text passwords seeded in the development database
                verificationResult = (request.Password == user.PasswordHash);
            }

            if (!verificationResult)
            {
                return null;
            }

            var token = GenerateJwtToken(user, user.Role.RoleName);

            return new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.RoleName
            };
        }

        private string GenerateJwtToken(User user, string roleName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}

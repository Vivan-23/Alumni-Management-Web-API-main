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
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return null;
            }

            // Verify Role exists (Student role is 3)
            var role = await _context.Roles.FindAsync(3);
            if (role == null)
            {
                return null;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

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

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
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

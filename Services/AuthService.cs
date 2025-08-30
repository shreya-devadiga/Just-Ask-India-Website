using JustAskIndia.Data;
using JustAskIndia.DTOs;
using JustAskIndia.Models;
using JustAskIndia.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JustAskIndia.Services
{
    public class AuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly CacheService _cache;
        //private readonly EmailSerivice emailSerivice

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<AppRole> roleManager,
            AppDbContext context,
            CacheService cache
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _cache = cache;
        }

        public async Task<AppUser?> GetCurrentAppUser(string? username, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(username)) return null;
            var appUser = (await _cache.GetAppUsersToFindByIdAsync(ct))
                .Values.FirstOrDefault(e => e.UserName == username);
            return appUser;
        }

        public async Task<(bool, string)> RegisterAsync(RegisterDTO model)
        {
            var userExists = await _userManager.FindByNameAsync(model.UserName);
            if (userExists != null) return (false, $"User '{model.UserName}' already exists");

            var userPhone = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (userPhone != null) return (false, $"Phone number '{model.PhoneNumber}' already exists");

            var user = new AppUser
            {
                UserName = model.UserName,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return (false, "Registration failed");

            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new AppRole(model.Role));

            await _userManager.AddToRoleAsync(user, model.Role);
            _cache.ResetCachedUsersAndRoles();

            return (true, "Registration successful");
        }

        public async Task<(bool success, string message, string? token)> LoginAsync(LoginDTO dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null) return (false, "Invalid credentials", null);

            var result = await _signInManager.PasswordSignInAsync(user, dto.Password, false, false);
            if (!result.Succeeded) return (false, "Invalid credentials", null);

            return (true, "Login successful", null);
        }

        public async Task<(bool, string)> ResetPassword(AppUserResetPassword dto)
        {
            var user = await _context.Users.FindAsync(dto.Id);
            if (user == null) return (false, "Invalid user");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.Password);

            return result.Succeeded ? (true, "Password reset") : (false, "Reset failed");
        }

        public async Task<(bool, string)> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return (false, "User not found");

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _cache.ResetCachedUsersAndRoles();
                return (true, "User deleted");
            }
            return (false, "Delete failed");
        }

        public async Task<(bool, string, List<AppUserDTO>)> GetUsersAsync(CancellationToken ct)
        {
            var cachedUsers = await _cache.GetAppUsersWithRolesAsync(ct);
            var users = cachedUsers.Select(c => new AppUserDTO(
                c.Id,
                c.AppUser?.FullName ?? "",
                c.AppUser?.FirstName ?? "",
                c.AppUser?.LastName ?? "",
                c.AppUser?.Email ?? "",
                c.AppUser?.PhoneNumber ?? "",
                c.AppUser?.UserName ?? "",
                c.AppUser?.LastLogin,
                c.Role)).ToList();

            return (true, "Success", users);
        }

        public async Task<(bool, string, AppUserDTO?)> GetUserInfoAsync(string username)
        {
            var user = (await _cache.GetAppUsersToFindByIdAsync())
                .Values.FirstOrDefault(e => e.UserName == username);

            if (user == null) return (false, "User not found", null);

            return (true, "Success", new AppUserDTO(
                user.Id, user.FullName, user.FirstName, user.LastName,
                user.Email, user.PhoneNumber, user.UserName, user.LastLogin, ""));
        }

        public async Task<(bool, string, string)> RefreshLoginAsync(string? userName)
        {
            if (string.IsNullOrEmpty(userName)) return (false, "Invalid username", "");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null) return (false, "User not found", "");

            user.LastLogin = DateTime.UtcNow;
            _context.AppUserLogins.Add(new UserLogin { AppUserId = user.Id, Login = user.LastLogin });
            await _context.SaveChangesAsync();
            _cache.ResetCachedUsersAndRoles();

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.GivenName, user.FullName),
                new(ClaimTypes.DateOfBirth, user.LastLogin?.ToString("o") ?? "")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = GenerateToken(claims);
            return (true, "Refreshed", token);
        }

        public async Task<(bool, string)> UpdateAsync(AppUserUpdateDTO dto)
        {
            var user = await _context.Users.FindAsync(dto.Id);
            if (user == null) return (false, "User not found");

            if (user.UserName != "admin" && user.UserName != dto.UserName)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                    return (false, "Username already taken");
                user.UserName = dto.UserName;
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            await _context.SaveChangesAsync();
            _cache.ResetCachedUsersAndRoles();

            return (true, "Updated");
        }

        public async Task<(bool, string)> UpdateRoleAsync(AppUserUpdateRole dto)
        {
            var user = await _context.Users.FindAsync(dto.Id);
            if (user == null) return (false, "User not found");

            await _userManager.RemoveFromRoleAsync(user, dto.OldRole);

            if (!await _roleManager.RoleExistsAsync(dto.NewRole))
                await _roleManager.CreateAsync(new AppRole(dto.NewRole));

            await _userManager.AddToRoleAsync(user, dto.NewRole);
            _cache.ResetCachedUsersAndRoles();
            return (true, "Role updated");
        }

        public async Task<(bool, string)> CreateRoleAsync(AppRoleCreate dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role)) return (false, "Invalid role");

            if (await _roleManager.RoleExistsAsync(dto.Role)) return (false, "Role exists");

            await _roleManager.CreateAsync(new AppRole(dto.Role));
            _cache.ResetCachedUsersAndRoles();
            return (true, "Role created");
        }

        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-super-secret-key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "JustAskIndia",
                audience: "JustAskIndiaAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ValueTask<Dictionary<int, AppUser>> GetAppUsersAsync(CancellationToken ct = default)
            => _cache.GetAppUsersToFindByIdAsync(ct);

        public Task<List<CachedUserDTO>> GetCachedUsers(CancellationToken ct)
            => _cache.GetAppUsersWithRolesAsync(ct);

        public async Task<List<string>> GetUsersEmailsAsync(string userName)
        {
            var users = (await _cache.GetAppUsersToFindByIdAsync()).Values
                .Where(u => u.UserName != userName && u.Id != 1)
                .Select(u => u.Email ?? "").ToList();

            return users;
        }
    }
}

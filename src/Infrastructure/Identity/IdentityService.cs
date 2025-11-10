using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Models;
using LotusDharma.Domain.Entities;
using LotusDharma.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LotusDharma.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthorizationService _authorizationService;

    public IdentityService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _authorizationService = authorizationService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        if (!int.TryParse(userId, out int id))
            return null;

        var user = await _context.Users.FindAsync(id);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        // Kiểm tra user đã tồn tại chưa
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == userName || u.Email == userName);

        if (existingUser != null)
        {
            return (Result.Failure(new[] { "User already exists." }), string.Empty);
        }

        var user = new User
        {
            UserName = userName,
            Email = userName,
            PasswordHash = _passwordHasher.HashPassword(password),
            EmailConfirmed = false,
            LockoutEnabled = true,
            Created = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(default);

        return (Result.Success(), user.Id.ToString());
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        if (!int.TryParse(userId, out int id))
            return false;

        var userRole = await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == id && ur.Role.Name == role);

        return userRole != null;
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        if (!int.TryParse(userId, out int id))
            return false;

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        // Tạo claims principal từ user
        var claims = await GetUserClaimsAsync(user);
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Custom");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        if (!int.TryParse(userId, out int id))
            return Result.Failure(new[] { "Invalid user ID." });

        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return Result.Success();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(default);

        return Result.Success();
    }

    public async Task<LoginResult?> LoginAsync(string email, string password)
    {
        // Tìm user theo email
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return null;

        // Kiểm tra password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, password))
            return null;

        // Lấy roles
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        // Lưu token vào database
        var userToken = new UserToken
        {
            UserId = user.Id,
            Token = token,
            RefreshToken = refreshToken,
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(24),
            RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.UserTokens.Add(userToken);
        await _context.SaveChangesAsync(default);

        return new LoginResult
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = userToken.TokenExpiry,
            UserId = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            Roles = roles
        };
    }

    private async Task<List<System.Security.Claims.Claim>> GetUserClaimsAsync(User user)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.UserName),
            new(System.Security.Claims.ClaimTypes.Email, user.Email),
        };

        // Lấy roles của user
        var roles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        foreach (var role in roles)
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        }

        return claims;
    }
}

using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Models;
using LotusDharma.Domain.Entities;
using LotusDharma.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthorizationService authorizationService,
        ILogger<IdentityService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _authorizationService = authorizationService;
        _logger = logger;
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
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return null;

        if (IsLockedOut(user))
        {
            _logger.LogWarning("Login attempt for locked-out account: {Email}", email);
            return null;
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, password))
        {
            await RecordFailedLoginAsync(user);
            return null;
        }

        await ResetAccessFailedCountAsync(user);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var userToken = new UserToken
        {
            UserId = user.Id,
            Token = token,
            RefreshToken = refreshToken,
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.UserTokens.Add(userToken);

        user.LastLoginAt = DateTimeOffset.UtcNow;

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

    private static bool IsLockedOut(User user)
    {
        return user.LockoutEnabled
               && user.LockoutEnd.HasValue
               && user.LockoutEnd > DateTimeOffset.UtcNow;
    }

    private async Task RecordFailedLoginAsync(User user)
    {
        user.AccessFailedCount++;

        if (user.LockoutEnabled && user.AccessFailedCount >= MaxFailedAttempts)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
            _logger.LogWarning("Account locked out due to {Count} failed attempts: UserId={UserId}",
                user.AccessFailedCount, user.Id);
        }

        await _context.SaveChangesAsync(default);
    }

    private async Task ResetAccessFailedCountAsync(User user)
    {
        if (user.AccessFailedCount > 0)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync(default);
        }
    }

    private async Task<List<System.Security.Claims.Claim>> GetUserClaimsAsync(User user)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.UserName),
            new(System.Security.Claims.ClaimTypes.Email, user.Email),
        };

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

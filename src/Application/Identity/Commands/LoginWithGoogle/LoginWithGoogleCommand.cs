using LotusDharma.Application.Identity.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Models;

namespace LotusDharma.Application.Identity.Commands.LoginWithGoogle;

public record LoginWithGoogleCommand : IRequest<AuthResponseDto?>
{
    public string IdToken { get; init; } = string.Empty;
}

public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, AuthResponseDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginWithGoogleCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuthService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto?> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var info = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);
        if (info == null || string.IsNullOrWhiteSpace(info.Email))
            return null;

        // Try find by provider+providerId first
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTokens)
            .FirstOrDefaultAsync(u => u.Provider == "Google" && u.ProviderId == info.Sub, cancellationToken);

        // Fallback to email
        if (user == null)
        {
            user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserTokens)
                .FirstOrDefaultAsync(u => u.Email == info.Email, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;

        if (user == null)
        {
            // Create new user (OAuth users have no password)
            user = new LotusDharma.Domain.Entities.User
            {
                UserName = info.Email!,
                Email = info.Email!,
                PasswordHash = string.Empty,
                EmailConfirmed = true,
                Provider = "Google",
                ProviderId = info.Sub,
                PictureUrl = info.Picture,
                Created = now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Ensure default role "User" exists
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
            if (role == null)
            {
                role = new LotusDharma.Domain.Entities.Role { Name = "User", Description = "Default application user" };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var userRole = new LotusDharma.Domain.Entities.UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync(cancellationToken);

            // Reload user with roles
            user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == user.Id, cancellationToken);
        }
        else
        {
            // Update provider fields if missing
            var updated = false;
            if (string.IsNullOrWhiteSpace(user.Provider))
            {
                user.Provider = "Google";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(user.ProviderId) && !string.IsNullOrWhiteSpace(info.Sub))
            {
                user.ProviderId = info.Sub;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(user.PictureUrl) && !string.IsNullOrWhiteSpace(info.Picture))
            {
                user.PictureUrl = info.Picture;
                updated = true;
            }
            if (updated)
            {
                user.LastModified = now;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Update last login
        user.LastLoginAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        // Collect roles
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Generate tokens
        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var userToken = new LotusDharma.Domain.Entities.UserToken
        {
            UserId = user.Id,
            Token = token,
            RefreshToken = refreshToken,
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(24),
            RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.UserTokens.Add(userToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            Token: token,
            RefreshToken: refreshToken,
            Expiration: userToken.TokenExpiry,
            UserId: user.Id,
            Email: user.Email,
            UserName: user.UserName,
            Roles: roles
        );
    }
}



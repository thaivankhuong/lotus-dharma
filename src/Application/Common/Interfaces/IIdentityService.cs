using LotusDharma.Application.Common.Models;

namespace LotusDharma.Application.Common.Interfaces;

public record LoginResult
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset Expiration { get; init; }
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    Task<LoginResult?> LoginAsync(string email, string password);
}

namespace LotusDharma.Domain.Entities;

/// <summary>
/// Custom User entity thay thế IdentityUser
/// </summary>
public class User : BaseAuditableEntity
{
    public string UserName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    
    public bool EmailConfirmed { get; set; }
    
    public bool PhoneNumberConfirmed { get; set; }
    
    public bool TwoFactorEnabled { get; set; }
    
    public DateTimeOffset? LockoutEnd { get; set; }
    
    public bool LockoutEnabled { get; set; }
    
    public int AccessFailedCount { get; set; }
    
    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    
    public ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
}


namespace LotusDharma.Domain.Entities;

/// <summary>
/// Lưu trữ JWT tokens và refresh tokens
/// </summary>
public class UserToken : BaseAuditableEntity
{
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public string Token { get; set; } = string.Empty;
    
    public string? RefreshToken { get; set; }
    
    public DateTimeOffset TokenExpiry { get; set; }
    
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    
    public bool IsRevoked { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
}


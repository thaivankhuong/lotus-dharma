namespace LotusDharma.Domain.Entities;

/// <summary>
/// Many-to-many relationship giữa User và Role
/// </summary>
public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public int RoleId { get; set; }
    
    public Role Role { get; set; } = null!;
}


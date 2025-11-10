namespace LotusDharma.Domain.Entities;

/// <summary>
/// Custom Role entity thay thế IdentityRole
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}


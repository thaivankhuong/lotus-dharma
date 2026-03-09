namespace LotusDharma.Domain.Entities;

/// <summary>
/// Many-to-many relationship between Role and Permission
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Foreign key to Role
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Foreign key to Permission
    /// </summary>
    public int PermissionId { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
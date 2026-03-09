namespace LotusDharma.Domain.Entities;

/// <summary>
/// User-specific permission assignments (overrides role permissions)
/// </summary>
public class UserPermission : BaseEntity
{
    /// <summary>
    /// Foreign key to User
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Foreign key to Permission
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// Whether this permission is granted (true) or denied (false) for this user
    /// Used to override role-based permissions
    /// </summary>
    public bool IsGranted { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
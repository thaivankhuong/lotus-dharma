namespace LotusDharma.Domain.Entities;

/// <summary>
/// Permission entity for permission-based authorization system
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// Unique permission name in Module.Action format (e.g., "Users.View", "Products.Create")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Permission module (e.g., "Users", "Products", "Orders")
    /// </summary>
    public string Module { get; private set; } = string.Empty;

    /// <summary>
    /// Permission action (e.g., "View", "Create", "Update", "Delete")
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of what this permission allows
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this permission is active and can be assigned
    /// </summary>
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Permission() { }

    /// <summary>
    /// Creates a new permission with validation
    /// </summary>
    /// <param name="module">Permission module (e.g., "Users")</param>
    /// <param name="action">Permission action (e.g., "View")</param>
    /// <param name="description">Optional description</param>
    /// <exception cref="ArgumentException">Thrown when module or action is invalid</exception>
    public Permission(string module, string action, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module cannot be null or empty", nameof(module));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        Module = module.Trim();
        Action = action.Trim();
        Name = $"{Module}.{Action}";
        Description = description?.Trim();

        ValidatePermissionName();
    }

    /// <summary>
    /// Updates the permission description
    /// </summary>
    /// <param name="description">New description</param>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }

    /// <summary>
    /// Activates the permission
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the permission
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Validates that the permission name follows Module.Action format
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when permission name format is invalid</exception>
    private void ValidatePermissionName()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Permission name cannot be null or empty");

        if (!Name.Contains('.'))
            throw new InvalidOperationException("Permission name must be in Module.Action format");

        var parts = Name.Split('.');
        if (parts.Length != 2)
            throw new InvalidOperationException("Permission name must have exactly one dot separator");

        var modulePart = parts[0].Trim();
        var actionPart = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(modulePart))
            throw new InvalidOperationException("Module part cannot be empty");

        if (string.IsNullOrWhiteSpace(actionPart))
            throw new InvalidOperationException("Action part cannot be empty");

        // Ensure module and action start with uppercase letters (PascalCase convention)
        if (!char.IsUpper(modulePart[0]))
            throw new InvalidOperationException("Module must start with uppercase letter");

        if (!char.IsUpper(actionPart[0]))
            throw new InvalidOperationException("Action must start with uppercase letter");

        // Ensure no spaces or special characters except underscore
        if (modulePart.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            throw new InvalidOperationException("Module can only contain letters, digits, and underscores");

        if (actionPart.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            throw new InvalidOperationException("Action can only contain letters, digits, and underscores");
    }
}
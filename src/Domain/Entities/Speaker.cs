namespace LotusDharma.Domain.Entities;

public class Speaker : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Slug { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DharmaTalk> DharmaTalks { get; set; } = new List<DharmaTalk>();
}

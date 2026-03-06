namespace LotusDharma.Domain.Entities;

public class DharmaTalkSeries : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Slug { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DharmaTalk> DharmaTalks { get; set; } = new List<DharmaTalk>();
}

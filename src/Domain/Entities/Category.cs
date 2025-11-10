namespace LotusDharma.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? CreatedIdUser { get; set; }
    
    public string? UpdatedIdUser { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property: 1 Category có nhiều Products
    public ICollection<Product> Products { get; private set; } = new List<Product>();
}


namespace LotusDharma.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public decimal Price { get; set; }
    
    public int Stock { get; set; }
    
    // Foreign Key
    public int CategoryId { get; set; }
    
    public string? CreatedIdUser { get; set; }
    
    public string? UpdatedIdUser { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property: Product thuộc về 1 Category
    public Category Category { get; set; } = null!;
}

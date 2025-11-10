using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Products.Queries.GetProductsWithCategory;

public class ProductWithCategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    
    // Category information
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? CategoryDescription { get; init; }
    
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastModified { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Product, ProductWithCategoryDto>()
                .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name))
                .ForMember(d => d.CategoryDescription, opt => opt.MapFrom(s => s.Category.Description));
        }
    }
}


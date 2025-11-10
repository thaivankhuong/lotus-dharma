using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Products.Queries.GetProducts;

public class ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int CategoryId { get; init; }
    public string? CreatedIdUser { get; init; }
    public string? UpdatedIdUser { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastModified { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Product, ProductDto>();
        }
    }
}

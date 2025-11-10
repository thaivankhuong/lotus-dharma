using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Categories.Queries.GetCategories;

public class CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CreatedIdUser { get; init; }
    public string? UpdatedIdUser { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastModified { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Category, CategoryDto>();
        }
    }
}


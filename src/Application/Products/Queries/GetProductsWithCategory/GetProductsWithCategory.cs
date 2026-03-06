using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Security;

namespace LotusDharma.Application.Products.Queries.GetProductsWithCategory;

[Authorize]
public record GetProductsWithCategoryQuery : IRequest<List<ProductWithCategoryDto>>
{
    public int? CategoryId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetProductsWithCategoryQueryHandler : IRequestHandler<GetProductsWithCategoryQuery, List<ProductWithCategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsWithCategoryQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<ProductWithCategoryDto>> Handle(GetProductsWithCategoryQuery request, CancellationToken cancellationToken)
    {
        // ✨ OPTION 1: Use Navigation Property + Select (RECOMMENDED - Best SQL)
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        // Filter by CategoryId if provided
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductWithCategoryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,  // ← Use Navigation Property
                CategoryDescription = p.Category.Description,
                Created = p.Created,
                LastModified = p.LastModified
            })
            .ToListAsync(cancellationToken);
        
        // SQL Generated:
        // SELECT p."Id", p."Name", p."Description", p."Price", p."Stock", 
        //        p."IsActive", p."CategoryId", c."Name", c."Description",
        //        p."Created", p."LastModified"
        // FROM "Products" p
        // INNER JOIN "Categories" c ON p."CategoryId" = c."Id"
        // WHERE p."IsActive" = TRUE
        // ORDER BY p."Name"
        // OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
    }
}


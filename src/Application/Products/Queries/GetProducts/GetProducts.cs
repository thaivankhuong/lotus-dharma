using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Security;

namespace LotusDharma.Application.Products.Queries.GetProducts;

[Authorize]
public record GetProductsQuery : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public GetProductsQueryHandler(
        IApplicationDbContext context, 
        IMapper mapper,
        ICacheService cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Cache products for 1 hour
        return await _cache.GetOrCreateAsync(
            CacheKeys.AllProducts,
            async () => await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken),
            TimeSpan.FromHours(1),
            cancellationToken
        );
    }
}

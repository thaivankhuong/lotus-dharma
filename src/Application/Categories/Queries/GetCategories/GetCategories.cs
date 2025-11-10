using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Security;

namespace LotusDharma.Application.Categories.Queries.GetCategories;

[Authorize]
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public GetCategoriesQueryHandler(
        IApplicationDbContext context, 
        IMapper mapper,
        ICacheService cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        // Categories rarely change, cache for 12 hours
        return await _cache.GetOrCreateAsync(
            CacheKeys.AllCategories,
            async () => await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken),
            TimeSpan.FromHours(12),
            cancellationToken
        );
    }
}


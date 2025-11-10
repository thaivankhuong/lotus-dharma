using LotusDharma.Application.Categories.Queries.GetCategories;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Security;

namespace LotusDharma.Application.Categories.Queries.GetCategoryById;

[Authorize]
public record GetCategoryByIdQuery(int Id) : IRequest<CategoryDto>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoryByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return entity;
    }
}


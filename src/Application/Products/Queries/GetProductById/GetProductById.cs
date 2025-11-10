using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Security;
using LotusDharma.Application.Products.Queries.GetProducts;

namespace LotusDharma.Application.Products.Queries.GetProductById;

[Authorize]
public record GetProductByIdQuery(int Id) : IRequest<ProductDto>;
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return entity;
    }
}

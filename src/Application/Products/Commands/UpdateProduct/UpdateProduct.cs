using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;

namespace LotusDharma.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand : IRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int CategoryId { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICacheService _cache;

    public UpdateProductCommandHandler(IApplicationDbContext context, IUser user, ICacheService cache)
    {
        _context = context;
        _user = user;
        _cache = cache;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products
            .FindAsync(new object[] { request.Id }, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Price = request.Price;
        entity.Stock = request.Stock;
        entity.CategoryId = request.CategoryId;
        entity.UpdatedIdUser = _user.Id;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.AllProducts, cancellationToken);
    }
}

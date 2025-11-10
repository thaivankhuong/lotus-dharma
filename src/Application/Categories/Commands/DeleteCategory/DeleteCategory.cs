using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Events;

namespace LotusDharma.Application.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(int Id) : IRequest;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public DeleteCategoryCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .FindAsync(new object[] { request.Id }, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.Categories.Remove(entity);

        entity.AddDomainEvent(new CategoryDeletedEvent(entity));

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate all category-related caches
        await _cache.RemoveAsync(CacheKeys.CategoryById(request.Id), cancellationToken);
        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.AllCategories, cancellationToken);
        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.ProductsByCategory, cancellationToken);
    }
}


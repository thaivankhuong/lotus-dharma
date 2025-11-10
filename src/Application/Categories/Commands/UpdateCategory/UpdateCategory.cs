using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;

namespace LotusDharma.Application.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand : IRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    // UpdatedIdUser sẽ tự động lấy từ JWT claims (không cần truyền từ client)
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    private readonly IUser _user;

    public UpdateCategoryCommandHandler(IApplicationDbContext context, ICacheService cache, IUser user)
    {
        _context = context;
        _cache = cache;
        _user = user;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .FindAsync(new object[] { request.Id }, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UpdatedIdUser = _user.Id;  // Tự động lấy UserId từ JWT claims
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate category caches
        await _cache.RemoveAsync(CacheKeys.CategoryById(request.Id), cancellationToken);
        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.AllCategories, cancellationToken);
        
        // Also invalidate related product caches
        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.ProductsByCategory, cancellationToken);
    }
}


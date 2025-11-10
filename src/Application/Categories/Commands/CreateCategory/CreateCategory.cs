using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Entities;
using LotusDharma.Domain.Events;

namespace LotusDharma.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    // CreatedIdUser sẽ tự động lấy từ JWT claims (không cần truyền từ client)
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    private readonly IUser _user;

    public CreateCategoryCommandHandler(IApplicationDbContext context, ICacheService cache, IUser user)
    {
        _context = context;
        _cache = cache;
        _user = user;
    }

    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Category
        {
            Name = request.Name,
            Description = request.Description,
            CreatedIdUser = _user.Id,  // Tự động lấy UserId từ JWT claims
            IsActive = true
        };

        entity.AddDomainEvent(new CategoryCreatedEvent(entity));

        _context.Categories.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate category caches
        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.AllCategories, cancellationToken);

        return entity.Id;
    }
}


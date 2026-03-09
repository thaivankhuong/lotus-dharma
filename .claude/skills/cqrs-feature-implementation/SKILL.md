# CQRS Feature Implementation Skill

## Purpose
Help Claude Code implement new CQRS features correctly in the LotusDharma .NET backend project, ensuring proper separation of Command/Query/Handler patterns and maintaining existing architectural patterns.

## When to Use This Skill
Use when:
- Creating new Commands or Queries
- Implementing business logic features
- Adding new API endpoints
- Modifying existing CQRS operations
- Following up on CQRS pattern implementation

## CQRS Implementation Workflow

### 1. Analyze Feature Requirements
- Determine if feature is Command (write) or Query (read)
- Identify required inputs and outputs
- Check for existing similar patterns in codebase
- Consider caching, validation, and authorization needs

### 2. Command Implementation Pattern
```csharp
// 1. Create folder: Application/{Feature}/Commands/{CommandName}/

// 2. Create {CommandName}.cs with Command record and Handler
public record CreateEntityCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class CreateEntityCommandHandler : IRequestHandler<CreateEntityCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    private readonly IUser _user;

    public CreateEntityCommandHandler(IApplicationDbContext context, ICacheService cache, IUser user)
    {
        _context = context;
        _cache = cache;
        _user = user;
    }

    public async Task<int> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
        var entity = new Entity
        {
            Name = request.Name,
            Description = request.Description,
            CreatedIdUser = _user.Id,
            IsActive = true
        };

        // Add domain events if needed
        entity.AddDomainEvent(new EntityCreatedEvent(entity));

        _context.Entities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate caches
        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.AllEntities, cancellationToken);

        return entity.Id;
    }
}

// 3. Create {CommandName}Validator.cs
public class CreateEntityCommandValidator : AbstractValidator<CreateEntityCommand>
{
    public CreateEntityCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
```

### 3. Query Implementation Pattern
```csharp
// 1. Create folder: Application/{Feature}/Queries/{QueryName}/

// 2. Create {QueryName}.cs with Query record and Handler
public record GetEntitiesQuery : IRequest<List<EntityDto>>
{
    public bool IncludeInactive { get; init; } = false;
}

public class GetEntitiesQueryHandler : IRequestHandler<GetEntitiesQuery, List<EntityDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetEntitiesQueryHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<EntityDto>> Handle(GetEntitiesQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Entities.All;

        var entities = await _cache.GetOrSetAsync(
            cacheKey,
            async () => await _context.Entities
                .Where(e => e.IsActive || request.IncludeInactive)
                .Select(e => new EntityDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description
                })
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken);

        return entities;
    }
}

// 3. Create DTO if needed
public class EntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

### 4. Endpoint Implementation
```csharp
// In Web/Endpoints/{Feature}.cs
app.MapPost("/{feature}/create",
    async (CreateEntityCommand command, ISender sender) =>
        await sender.Send(command))
    .WithName("CreateEntity")
    .WithTags("{Feature}")
    .RequireAuthorization(); // or specific permissions

app.MapGet("/{feature}",
    async (bool? includeInactive, ISender sender) =>
        await sender.Send(new GetEntitiesQuery { IncludeInactive = includeInactive ?? false }))
    .WithName("GetEntities")
    .WithTags("{Feature}")
    .RequireAuthorization();
```

## Naming Conventions
- **Commands**: `{Action}{Entity}Command` (CreateCategory, UpdateProduct, DeleteUser)
- **Queries**: `{Entity}Query` or `Get{Entity}Query` (GetCategories, GetProductsWithCategory)
- **Validators**: `{CommandName}Validator`
- **DTOs**: `{Entity}Dto` or `{Entity}{Suffix}Dto`
- **Handlers**: `{CommandName}Handler` (in same file as command)

## Common Patterns to Follow

### Entity Creation
- Always set `CreatedIdUser = _user.Id` from JWT claims
- Set `IsActive = true` by default
- Add domain events for side effects
- Invalidate related caches

### Entity Updates
- Use domain methods when available
- Update audit fields if entity supports it
- Invalidate caches after changes

### Entity Deletion (Soft Delete)
- Set `IsActive = false` instead of hard delete
- Consider cascade effects on related entities
- Invalidate caches

### Caching Strategy
- Cache read operations with appropriate keys
- Invalidate by pattern after writes
- Use `CacheKeys` class for consistency

### Validation Rules
- Required fields: `.NotEmpty()`
- String lengths: `.MaximumLength(n)`
- Email: `.EmailAddress()`
- Custom business rules in validator

## Safety Checks

### Before Implementation
- [ ] Feature type identified (Command vs Query)
- [ ] Similar patterns exist in codebase
- [ ] Required dependencies available
- [ ] Domain entities exist or need creation

### During Implementation
- [ ] Handler contains business logic, not controller
- [ ] Proper dependency injection
- [ ] Validation added for all inputs
- [ ] Cache invalidation after writes
- [ ] Domain events added when needed

### After Implementation
- [ ] Code compiles without errors
- [ ] Naming conventions followed
- [ ] English-only comments
- [ ] Layer boundaries respected
- [ ] Tests can be added (handler is testable)

## Error Prevention

### Common Mistakes to Avoid
1. **Business logic in controllers** - Move to handlers
2. **Direct DbContext usage** - Use IApplicationDbContext
3. **Missing validation** - Add validators for all inputs
4. **Forgotten cache invalidation** - Always invalidate after writes
5. **Mixed concerns** - Keep commands/queries separate
6. **Wrong dependencies** - Respect layer boundaries

### Best Practices
- Follow existing patterns exactly
- Use existing interfaces and services
- Add domain events for cross-cutting concerns
- Keep handlers focused on single responsibility
- Use meaningful variable names
- Add XML documentation comments

This skill ensures Claude Code maintains the CQRS architecture integrity while implementing new features efficiently and consistently.
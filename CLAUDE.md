# CLAUDE.md - Claude Code Configuration for LotusDharma

## Project Overview

LotusDharma is a .NET 9 backend API using **Clean Architecture** pattern (based on Jason Taylor's template). The solution follows SOLID principles with CQRS + MediatR, FluentValidation, and English-only code convention.

### Tech Stack
- **Framework**: .NET 9
- **Architecture**: Clean Architecture (Jason Taylor template)
- **Pattern**: CQRS with MediatR
- **Validation**: FluentValidation
- **Database**: Entity Framework Core with PostgreSQL/SQLite
- **API**: Minimal API with NSwag/Swagger
- **Authentication**: JWT
- **Hosting**: .NET Aspire

## Solution Structure

```
LotusDharma.sln
├── src/
│   ├── Domain/           # Enterprise business rules (entities, value objects, events)
│   ├── Application/      # Application business rules (CQRS commands/queries, behaviors)
│   ├── Infrastructure/   # External concerns (EF Core, services, migrations)
│   ├── Web/              # API endpoints, controllers, middleware
│   └── AppHost/          # .NET Aspire orchestration
```

### Layer Dependencies
```
Web → Application → Domain
       ↑
Infrastructure (implements Application interfaces)
```

## CQRS Architecture Rules

### Command vs Query Separation
- **Commands**: Write operations that modify state
- **Queries**: Read operations that return data
- **Controllers**: Call MediatR only, no business logic
- **Handlers**: Contain all business logic

### Command Structure
```csharp
public record CreateCategoryCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
{
    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
    }
}
```

### Query Structure
```csharp
public record GetCategoriesQuery : IRequest<List<CategoryDto>>
{
    public bool IncludeInactive { get; init; } = false;
}

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        // Read logic here
    }
}
```

### Folder Organization
```
Application/
├── {Feature}/
│   ├── Commands/
│   │   └── {CommandName}/
│   │       ├── {CommandName}.cs           # Command record + Handler class
│   │       └── {CommandName}Validator.cs  # FluentValidation rules
│   └── Queries/
│       └── {QueryName}/
│           ├── {QueryName}.cs             # Query record + Handler class
│           ├── {QueryName}Validator.cs    # Optional validation
│           └── {Dto}.cs                   # Response DTOs
```

## Handler Patterns

### Dependency Injection
- All handlers use constructor injection
- Common dependencies: `IApplicationDbContext`, `ICacheService`, `IUser`
- Never inject concrete classes, always interfaces

### Validation
- Every command/query with input needs a validator
- Validators inherit from `AbstractValidator<T>`
- Validation happens in MediatR pipeline via `ValidationBehaviour`

### Caching
- Use `ICacheService` for caching operations
- Invalidate caches after write operations: `await _cache.RemoveByPatternAsync(pattern, cancellationToken)`
- Cache keys defined in `CacheKeys` class

### Domain Events
- Entities can raise domain events: `entity.AddDomainEvent(new CategoryCreatedEvent(entity))`
- Handled by event handlers in `Application/{Feature}/EventHandlers/`

## Folder/Module Responsibilities

### Domain Layer (`src/Domain/`)
- **Entities**: Business objects with identity (`BaseEntity`, `BaseAuditableEntity`)
- **ValueObjects**: Immutable objects without identity
- **Enums**: Domain enumerations
- **Events**: Domain events
- **Common**: Base classes and domain services

### Application Layer (`src/Application/`)
- **Commands/Queries**: CQRS operations
- **Behaviors**: MediatR pipeline (Validation, Logging, Authorization, Performance)
- **Common/Interfaces**: Abstractions (`IApplicationDbContext`, `ICacheService`, `IUser`)
- **Mappings**: AutoMapper profiles
- **DTOs**: Data transfer objects

### Infrastructure Layer (`src/Infrastructure/`)
- **Data**: EF Core context, configurations, migrations
- **Services**: External service implementations
- **Caching**: Redis/memory cache implementation
- **Identity**: Authentication/authorization services

### Web Layer (`src/Web/`)
- **Endpoints**: Minimal API route definitions
- **Middleware**: Exception handling, authentication
- **Configuration**: DI container setup, service registration

## Dependency Rules

### Strict Layer Boundaries
1. **Domain** → No dependencies on other layers
2. **Application** → Depends only on Domain
3. **Infrastructure** → Implements Application interfaces
4. **Web** → Depends on Application and Infrastructure

### CQRS Boundaries
- Controllers cannot contain business logic
- Handlers cannot call other handlers directly
- Queries cannot modify data
- Commands cannot return complex data structures

## Important Safety Constraints

### Never Break These Rules
1. **Don't bypass MediatR** - All business operations go through Commands/Queries
2. **Don't reference Infrastructure from Domain** - Keep domain pure
3. **Don't put business logic in Web layer** - Only API concerns
4. **Don't forget cache invalidation** - Always invalidate after writes
5. **Don't skip validation** - Every input needs validation
6. **Don't use DbContext directly in Web** - Always through Application layer
7. **Don't mix command/query logic** - Keep separation strict

### Database Access
- Only Infrastructure uses `DbContext` directly
- Application uses `IApplicationDbContext` interface
- Web layer never accesses database directly

## Coding Conventions

### Naming (PascalCase)
| Type | Convention | Example |
|------|------------|---------|
| Classes, Methods, Properties | PascalCase | `CreateCategoryCommand` |
| Interfaces | IPascalCase | `IApplicationDbContext` |
| Private fields | _camelCase | `_context` |
| Parameters, Local variables | camelCase | `cancellationToken` |
| Constants | PascalCase | `MaxRetries` |

### File Organization
- One class per file
- File name matches class name
- Use file-scoped namespaces: `namespace X;`
- Commands/Queries: Handler in same file as request

### C# Style
- Use `record` for DTOs, Commands, Queries
- Expression-bodied members when appropriate
- Pattern matching over type checks
- Always use braces for control statements
- 4 spaces indentation, LF line endings
- English-only comments and documentation

## How Claude Code Should Process Requests

### Feature Implementation Workflow
1. **Analyze Request**: Understand feature requirements and CQRS implications
2. **Create Command/Query**: Use proper naming (`{Action}{Entity}Command`/`{Entity}Query`)
3. **Implement Handler**: Put all business logic in handler, follow existing patterns
4. **Add Validation**: Create validator for input validation
5. **Update Endpoints**: Add Minimal API endpoint in Web layer
6. **Handle Caching**: Invalidate caches after writes, use cache for reads
7. **Domain Events**: Add events for cross-cutting concerns

### Code Review Checklist
- [ ] CQRS separation maintained (commands write, queries read)
- [ ] Business logic in handlers, not controllers
- [ ] Proper dependency injection
- [ ] Validation present for all inputs
- [ ] Cache invalidation after writes
- [ ] Domain events for side effects
- [ ] English-only comments
- [ ] Naming conventions followed
- [ ] Layer boundaries respected

### Common Patterns to Follow
- **Entity Creation**: Use domain events, set audit fields from `IUser`
- **Caching**: Remove by pattern after writes, cache DTOs for reads
- **Error Handling**: Let global exception middleware handle errors
- **Authorization**: Use `AuthorizeAttribute` with permission requirements
- **Validation**: FluentValidation in pipeline, not in handlers

### When to Ask for Clarification
- Unclear CQRS implications (read vs write operation)
- Missing domain knowledge
- Conflicting requirements with existing patterns
- Architectural decisions needed

Remember: **Preserve existing patterns**, **maintain CQRS separation**, **respect layer boundaries**, and **follow SOLID principles**.
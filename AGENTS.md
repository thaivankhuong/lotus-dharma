# AGENTS.md - AI Agent Guidelines for LotusDharma

This document provides guidelines for AI agents working with the LotusDharma codebase.

## Project Overview

LotusDharma is a .NET 9 backend API using **Clean Architecture** pattern (based on Jason Taylor's template). The solution follows SOLID principles with English-only code convention.

## Solution Structure

```
LotusDharma.sln
├── src/
│   ├── Domain/           # Enterprise business rules
│   ├── Application/      # Application business rules (CQRS)
│   ├── Infrastructure/   # External concerns (DB, services)
│   ├── Web/              # API endpoints, controllers
│   └── AppHost/          # .NET Aspire host
```

### Layer Dependencies

```
Web → Application → Domain
       ↑
Infrastructure (implements Application interfaces)
```

## Architecture Patterns

### 1. CQRS with MediatR

Commands and Queries are separated using MediatR:

```
Application/
├── {Feature}/
│   ├── Commands/
│   │   └── {CommandName}/
│   │       ├── {CommandName}.cs           # Command + Handler
│   │       └── {CommandName}Validator.cs  # FluentValidation
│   └── Queries/
│       └── {QueryName}/
│           ├── {QueryName}.cs             # Query + Handler
│           ├── {QueryName}Validator.cs    # FluentValidation (optional)
│           └── {Dto}.cs                   # Response DTOs
```

**Command Example:**
```csharp
public record CreateCategoryCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
{
    private readonly IApplicationDbContext _context;
    
    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### 2. Domain Layer

- **Entities**: Business objects with identity (`src/Domain/Entities/`)
- **ValueObjects**: Immutable objects without identity (`src/Domain/ValueObjects/`)
- **Enums**: Domain enumerations (`src/Domain/Enums/`)
- **Events**: Domain events (`src/Domain/Events/`)
- **Common**: Base classes like `BaseEntity`, `BaseAuditableEntity`

### 3. Application Layer

- **Behaviours**: MediatR pipeline behaviors (Validation, Logging, Performance, Authorization)
- **Common/Interfaces**: Abstractions for infrastructure (`IApplicationDbContext`, `ICacheService`, etc.)
- **Mappings**: AutoMapper profiles
- **DTOs**: Data transfer objects for responses

### 4. Infrastructure Layer

- Entity Framework Core with PostgreSQL/SQLite support
- Implements all `Application/Common/Interfaces`
- Contains migrations and data configurations

### 5. Web Layer

- Minimal API endpoints (`src/Web/Endpoints/`)
- OpenAPI/Swagger via NSwag
- JWT Authentication
- Global exception handling

## Coding Conventions

### Naming

| Type | Convention | Example |
|------|------------|---------|
| Classes, Methods, Properties | PascalCase | `CreateCategoryCommand` |
| Interfaces | IPascalCase | `IApplicationDbContext` |
| Private fields | _camelCase | `_context` |
| Private static fields | s_camelCase | `s_instance` |
| Parameters, Local variables | camelCase | `cancellationToken` |
| Constants | PascalCase | `MaxRetries` |

### File Organization

- One class per file (generally)
- File name matches class name
- Use file-scoped namespaces (`namespace X;`)
- Commands/Queries: Handler in same file as request

### C# Style

- Use `record` for DTOs, Commands, Queries
- Use expression-bodied members when appropriate
- Prefer pattern matching over type checks
- Always use braces for control statements
- 4 spaces indentation, LF line endings

## Creating New Features

### Adding a New Command

1. Create folder: `Application/{Feature}/Commands/{CommandName}/`
2. Create `{CommandName}.cs` with Command record and Handler class
3. Create `{CommandName}Validator.cs` with FluentValidation rules
4. Add endpoint in `Web/Endpoints/{Feature}.cs`

### Adding a New Query

1. Create folder: `Application/{Feature}/Queries/{QueryName}/`
2. Create `{QueryName}.cs` with Query record and Handler class
3. Create DTOs for response if needed
4. Create Validator if query has parameters
5. Add endpoint in `Web/Endpoints/{Feature}.cs`

### Adding a New Entity

1. Create entity in `Domain/Entities/`
2. Inherit from `BaseEntity` or `BaseAuditableEntity`
3. Add `DbSet<T>` to `IApplicationDbContext`
4. Create EF configuration in `Infrastructure/Data/Configurations/`
5. Generate migration: `dotnet ef migrations add {Name}`

## Important Interfaces

```csharp
// Database context
IApplicationDbContext

// Current user info from JWT
IUser

// Caching
ICacheService

// JWT token generation
IJwtTokenGenerator

// Identity operations
IIdentityService
```

## Build & Run

```bash
# Restore and build
dotnet build

# Run with Aspire (recommended)
cd src/AppHost && dotnet run

# Run Web only
cd src/Web && dotnet run

# Run EF migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

## Testing Considerations

- Use MediatR for all business operations
- Inject interfaces, not implementations
- Domain entities should be self-validating
- Commands modify state, Queries only read

## Common Pitfalls to Avoid

1. **Don't bypass MediatR** - All business operations go through Commands/Queries
2. **Don't reference Infrastructure from Domain** - Keep domain pure
3. **Don't put business logic in Web layer** - Only API concerns
4. **Don't forget cache invalidation** - Use `ICacheService.RemoveByPatternAsync`
5. **Don't skip validation** - Every Command/Query with input needs a Validator

## Global Usings

The project uses implicit usings. Check `GlobalUsings.cs` in each project for commonly imported namespaces.

```csharp
// Application/GlobalUsings.cs includes:
global using MediatR;
global using FluentValidation;
global using AutoMapper;
global using Microsoft.EntityFrameworkCore;
```

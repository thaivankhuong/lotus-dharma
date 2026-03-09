# CQRS Boundary Safety Skill

## Purpose
Help Claude Code avoid breaking CQRS architecture boundaries and Clean Architecture principles in the LotusDharma .NET backend project. This skill enforces strict separation between layers and prevents common architectural violations.

## When to Use This Skill
Use when:
- Reviewing code changes for architectural violations
- Before implementing new features
- When considering code modifications
- During code reviews or refactoring
- Any time layer boundaries might be crossed

## Critical Safety Rules

### NEVER Allow These Violations

#### 1. Business Logic in Controllers/Web Layer
❌ **WRONG - Business logic in controller:**
```csharp
app.MapPost("/categories", async (CreateCategoryRequest request, IApplicationDbContext context) => {
    // Business logic here - VIOLATION!
    var category = new Category { Name = request.Name };
    context.Categories.Add(category);
    await context.SaveChangesAsync();
    return category.Id;
});
```

✅ **CORRECT - Controller calls MediatR:**
```csharp
app.MapPost("/categories", async (CreateCategoryCommand command, ISender sender) =>
    await sender.Send(command));
```

#### 2. Direct DbContext Usage in Application/Web Layers
❌ **WRONG - Application layer using DbContext directly:**
```csharp
// In Application layer
public class SomeHandler : IRequestHandler<SomeCommand, int>
{
    private readonly ApplicationDbContext _context; // VIOLATION!

    public SomeHandler(ApplicationDbContext context) // VIOLATION!
    {
        _context = context;
    }
}
```

✅ **CORRECT - Use interface abstraction:**
```csharp
public class SomeHandler : IRequestHandler<SomeCommand, int>
{
    private readonly IApplicationDbContext _context; // CORRECT

    public SomeHandler(IApplicationDbContext context) // CORRECT
    {
        _context = context;
    }
}
```

#### 3. Domain Layer Depending on Application/Infrastructure
❌ **WRONG - Domain referencing Application:**
```csharp
// In Domain/Entity.cs
using LotusDharma.Application.Interfaces; // VIOLATION!
```

✅ **CORRECT - Domain is independent:**
```csharp
// Domain has no external dependencies
namespace LotusDharma.Domain.Entities;
```

#### 4. Queries Modifying Data
❌ **WRONG - Query with side effects:**
```csharp
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // VIOLATION - Modifying data in a query!
        await _context.SaveChangesAsync(); // NEVER in queries
        return users;
    }
}
```

✅ **CORRECT - Queries are read-only:**
```csharp
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // Only read operations
        return await _context.Users.Select(u => new UserDto { ... }).ToListAsync();
    }
}
```

#### 5. Commands Returning Complex Data
❌ **WRONG - Command returning detailed data:**
```csharp
public record CreateUserCommand : IRequest<UserDetailsDto> // VIOLATION!
{
    public string Email { get; init; } = string.Empty;
}
```

✅ **CORRECT - Commands return simple results:**
```csharp
public record CreateUserCommand : IRequest<int> // CORRECT - just ID
{
    public string Email { get; init; } = string.Empty;
}
```

#### 6. Bypassing MediatR Pipeline
❌ **WRONG - Direct handler instantiation:**
```csharp
public class SomeController
{
    private readonly CreateUserCommandHandler _handler; // VIOLATION!

    public SomeController(CreateUserCommandHandler handler) // VIOLATION!
    {
        _handler = handler;
    }

    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        return await _handler.Handle(new CreateUserCommand { ... }, default); // VIOLATION!
    }
}
```

✅ **CORRECT - Use ISender/MediatR:**
```csharp
public class SomeController
{
    private readonly ISender _sender; // CORRECT

    public SomeController(ISender sender) // CORRECT
    {
        _sender = sender;
    }

    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var result = await _sender.Send(new CreateUserCommand { ... }); // CORRECT
        return Ok(result);
    }
}
```

#### 7. Missing Pipeline Behaviors
❌ **WRONG - Bypassing validation:**
```csharp
// No validator for command with input - VIOLATION!
public record CreateUserCommand : IRequest<int>
{
    public string Email { get; init; } = string.Empty;
}
```

✅ **CORRECT - Always validate inputs:**
```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().EmailAddress();
    }
}
```

#### 8. Mixed Command/Query Logic
❌ **WRONG - Command doing reads for business logic:**
```csharp
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, int>
{
    public async Task<int> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // VIOLATION - Command doing read operations for business logic
        var existingUser = await _context.Users.FindAsync(request.Id);

        if (existingUser.Email != request.Email)
        {
            // Business logic mixed with data access
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists) throw new ValidationException("Email already exists");
        }
    }
}
```

✅ **CORRECT - Commands focus on writes:**
```csharp
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, int>
{
    public async Task<int> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(request.Id)
            ?? throw new NotFoundException(nameof(User), request.Id);

        user.UpdateEmail(request.Email); // Domain method handles business rules
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPatternAsync(CacheKeys.Patterns.User(request.Id), cancellationToken);

        return user.Id;
    }
}
```

## Architecture Boundary Checks

### Layer Dependency Validation
- **Domain** ← Application (Application can depend on Domain)
- **Domain** ← Infrastructure (Infrastructure can depend on Domain)
- **Domain** ← Web (Web can depend on Domain)
- **Application** ← Infrastructure (Infrastructure implements Application interfaces)
- **Application** ← Web (Web uses Application)
- **Infrastructure** ← Web (Web configures Infrastructure)

### ❌ NEVER ALLOW:
- Domain → Application (Domain cannot depend on Application)
- Domain → Infrastructure (Domain cannot depend on Infrastructure)
- Domain → Web (Domain cannot depend on Web)
- Application → Infrastructure (Application cannot depend on Infrastructure implementations)

### CQRS Flow Validation
1. **Client** → **Controller** (Web layer)
2. **Controller** → **MediatR** (ISender.Send())
3. **MediatR** → **Pipeline Behaviors** (Validation, Authorization, Logging)
4. **Pipeline** → **Handler** (Application layer)
5. **Handler** → **Domain** (Business logic)
6. **Handler** → **Infrastructure** (Data access via interfaces)
7. **Infrastructure** → **External Systems** (Database, Cache, Services)

## Safety Inspection Checklist

### Before Code Changes
- [ ] Confirm layer boundaries will not be violated
- [ ] Verify CQRS separation (command vs query)
- [ ] Check for business logic placement
- [ ] Ensure proper dependency injection
- [ ] Validate interface usage over concrete classes

### During Implementation
- [ ] Controllers only call MediatR
- [ ] Handlers contain all business logic
- [ ] Domain entities have no external dependencies
- [ ] Infrastructure implements interfaces only
- [ ] No direct DbContext usage outside Infrastructure

### Code Review Questions
- Does this change cross layer boundaries?
- Is business logic properly contained in handlers?
- Are commands/queries properly separated?
- Is MediatR pipeline being used correctly?
- Are domain events being used for side effects?
- Is caching being handled appropriately?

## Emergency Stop Conditions

### IMMEDIATELY STOP if you see:
1. Business logic in controllers
2. Direct DbContext usage in Application/Web layers
3. Domain layer importing Application/Infrastructure namespaces
4. Queries modifying data
5. Commands returning complex objects
6. Handlers being called directly (not through MediatR)
7. Missing validation for user inputs
8. Mixed command/query responsibilities

### When Uncertain
- Ask for clarification about architectural implications
- Reference existing patterns in the codebase
- Check CLAUDE.md for specific guidance
- Look at similar existing implementations

## Prevention Strategies

### Code Structure Enforcement
- Always use interfaces over concrete classes
- Keep handlers focused on single responsibilities
- Use domain events for cross-cutting concerns
- Maintain strict folder organization

### Dependency Injection Rules
- All services must use DI
- Controllers get ISender, not handlers
- Application gets interfaces, not implementations
- Infrastructure registers concrete implementations

### Naming Convention Enforcement
- Commands end with "Command"
- Queries end with "Query"
- Handlers end with "Handler"
- Validators end with "Validator"

This skill ensures the CQRS architecture remains pure and maintainable by preventing common architectural violations that could compromise the system's integrity.
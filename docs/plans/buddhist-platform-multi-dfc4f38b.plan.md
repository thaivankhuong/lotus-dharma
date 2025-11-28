<!-- dfc4f38b-ca12-4bcc-9d33-39351458d0a5 2cdd2f97-2cee-4f5b-beb1-86ec6c5870aa -->
# Plan: Lotus Multi-Database Clean Architecture

## Overview

Xây dựng Lotus Platform với Clean Architecture pattern, tích hợp 3 loại database (SQL Server, PostgreSQL, MongoDB) với sample entities (User, Product, Category) làm foundation cho Buddhist Learning Platform.

## Phase 1: Project Initialization

### 1.1 Create Base Project

```bash
# Tạo project từ Clean Architecture template
dotnet new ca-sln -cf None -o MultiDbCleanArchitectureTemplate --database sqlserver

cd MultiDbCleanArchitectureTemplate
```

### 1.2 Rename to Generic Names

Đổi namespaces từ `CleanArchitecture` sang `MultiDbTemplate`:

- Update all .csproj files: `<RootNamespace>MultiDbTemplate.*</RootNamespace>`
- Update namespaces trong tất cả .cs files
- Update using statements

### 1.3 Clean Up Template Code

Remove template-specific code:

- Delete `Domain/Entities/TodoList.cs`, `TodoItem.cs`
- Delete `Domain/Events/TodoItem*.cs`
- Delete `Domain/Enums/PriorityLevel.cs`
- Delete `Domain/ValueObjects/Colour.cs`
- Delete `Application/TodoLists/`, `TodoItems/`, `WeatherForecasts/`
- Delete `Web/Endpoints/TodoLists.cs`, `TodoItems.cs`, `WeatherForecasts.cs`

## Phase 2: Multi-Database Infrastructure Setup

### 2.1 Install Required Packages

```bash
cd src/Infrastructure

# PostgreSQL support
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# MongoDB support
dotnet add package MongoDB.Driver
dotnet add package MongoDB.Bson
```

### 2.2 Create Database Context Strategy

**Infrastructure/Data/Identity/IdentityDbContext.cs** (SQL Server)

```csharp
namespace MultiDbTemplate.Infrastructure.Data.Identity;

public class IdentityDbContext : DbContext, IIdentityDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) 
        : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("identity");
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t.Namespace?.Contains("Identity.Configurations") ?? false);
        base.OnModelCreating(builder);
    }
}
```

**Infrastructure/Data/Catalog/CatalogDbContext.cs** (PostgreSQL)

```csharp
namespace MultiDbTemplate.Infrastructure.Data.Catalog;

public class CatalogDbContext : DbContext, ICatalogDbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
        : base(options) { }
    
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("catalog");
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t.Namespace?.Contains("Catalog.Configurations") ?? false);
        base.OnModelCreating(builder);
    }
}
```

**Infrastructure/Data/Documents/MongoDbContext.cs** (MongoDB)

```csharp
namespace MultiDbTemplate.Infrastructure.Data.Documents;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    
    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDb");
        var mongoUrl = new MongoUrl(connectionString);
        var client = new MongoClient(mongoUrl);
        _database = client.GetDatabase(mongoUrl.DatabaseName);
    }
    
    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
    
    // Sample collections
    public IMongoCollection<AuditLog> AuditLogs => GetCollection<AuditLog>("audit_logs");
    public IMongoCollection<ActivityLog> ActivityLogs => GetCollection<ActivityLog>("activity_logs");
}
```

### 2.3 Update DependencyInjection.cs

**Infrastructure/DependencyInjection.cs**

```csharp
public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
{
    // SQL Server - Identity & Authentication
    var identityConnection = builder.Configuration.GetConnectionString("IdentityDb");
    Guard.Against.Null(identityConnection, message: "Connection string 'IdentityDb' not found.");
    
    builder.Services.AddDbContext<IdentityDbContext>((sp, options) =>
    {
        options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        options.UseSqlServer(identityConnection, 
            b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName));
    });
    
    builder.Services.AddScoped<IIdentityDbContext>(provider => 
        provider.GetRequiredService<IdentityDbContext>());
    
    // PostgreSQL - Catalog & Products
    var catalogConnection = builder.Configuration.GetConnectionString("CatalogDb");
    Guard.Against.Null(catalogConnection, message: "Connection string 'CatalogDb' not found.");
    
    builder.Services.AddDbContext<CatalogDbContext>((sp, options) =>
    {
        options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        options.UseNpgsql(catalogConnection,
            b => b.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName));
    });
    
    builder.Services.AddScoped<ICatalogDbContext>(provider => 
        provider.GetRequiredService<CatalogDbContext>());
    
    // MongoDB - Logs & Documents
    builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
    
    // Database Initializers
    builder.Services.AddScoped<IdentityDbContextInitialiser>();
    builder.Services.AddScoped<CatalogDbContextInitialiser>();
    
    // Existing services...
    builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
    builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
    
    // Identity (keep existing code)
    builder.Services
        .AddDefaultIdentity<ApplicationUser>()
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<IdentityDbContext>();
    
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddTransient<IIdentityService, IdentityService>();
}
```

### 2.4 Update Configuration Files

**Web/appsettings.json**

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Server=(localdb)\\mssqllocaldb;Database=MultiDbTemplate_Identity;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    "CatalogDb": "Host=localhost;Port=5432;Database=MultiDbTemplate_Catalog;Username=postgres;Password=postgres;Include Error Detail=true",
    "MongoDb": "mongodb://localhost:27017/MultiDbTemplate_Documents"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Web/appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Server=(localdb)\\mssqllocaldb;Database=MultiDbTemplate_Identity_Dev;Trusted_Connection=true;MultipleActiveResultSets=true",
    "CatalogDb": "Host=localhost;Port=5432;Database=MultiDbTemplate_Catalog_Dev;Username=postgres;Password=postgres",
    "MongoDb": "mongodb://localhost:27017/MultiDbTemplate_Documents_Dev"
  }
}
```

## Phase 3: Domain Layer - Sample Entities

### 3.1 Identity Domain (SQL Server)

**Domain/Entities/Identity/User.cs**

```csharp
namespace MultiDbTemplate.Domain.Entities.Identity;

public class User : BaseAuditableEntity
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    // Navigation properties
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
}
```

**Domain/Entities/Identity/Role.cs**

```csharp
public class Role : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
}
```

**Domain/Entities/Identity/UserRole.cs**

```csharp
public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    
    public DateTime AssignedAt { get; set; }
}
```

**Domain/Enums/UserStatus.cs**

```csharp
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Deleted = 4
}
```

**Domain/Events/Identity/UserRegisteredEvent.cs**

```csharp
public class UserRegisteredEvent : BaseEvent
{
    public UserRegisteredEvent(User user)
    {
        User = user;
    }
    
    public User User { get; }
}
```

### 3.2 Catalog Domain (PostgreSQL)

**Domain/Entities/Catalog/Product.cs**

```csharp
namespace MultiDbTemplate.Domain.Entities.Catalog;

public class Product : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    // Business logic
    public bool IsInStock => StockQuantity > 0;
    public bool IsAvailable => Status == ProductStatus.Published && IsInStock;
}
```

**Domain/Entities/Catalog/Category.cs**

```csharp
public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public int? ParentCategoryId { get; set; }
    
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();
}
```

**Domain/Enums/ProductStatus.cs**

```csharp
public enum ProductStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
```

**Domain/Events/Catalog/ProductCreatedEvent.cs**

```csharp
public class ProductCreatedEvent : BaseEvent
{
    public ProductCreatedEvent(Product product)
    {
        Product = product;
    }
    
    public Product Product { get; }
}
```

### 3.3 Document Domain (MongoDB)

**Domain/Entities/Documents/AuditLog.cs**

```csharp
namespace MultiDbTemplate.Domain.Entities.Documents;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!; // Create, Update, Delete
    public string? UserId { get; set; }
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Domain/Entities/Documents/ActivityLog.cs**

```csharp
public class ActivityLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
}
```

## Phase 4: Application Layer - Interfaces & Use Cases

### 4.1 Define Database Interfaces

**Application/Common/Interfaces/IIdentityDbContext.cs**

```csharp
public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**Application/Common/Interfaces/ICatalogDbContext.cs**

```csharp
public interface ICatalogDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**Application/Common/Interfaces/IMongoDbContext.cs**

```csharp
public interface IMongoDbContext
{
    IMongoCollection<T> GetCollection<T>(string name);
    IMongoCollection<AuditLog> AuditLogs { get; }
    IMongoCollection<ActivityLog> ActivityLogs { get; }
}
```

### 4.2 User Module (Identity DB)

**Application/Users/Commands/CreateUser/CreateUser.cs**

```csharp
public record CreateUserCommand : IRequest<int>
{
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IIdentityDbContext _context;
    
    public CreateUserCommandValidator(IIdentityDbContext context)
    {
        _context = context;
        
        RuleFor(v => v.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9_]+$")
            .MustAsync(BeUniqueUsername).WithMessage("Username already exists");
        
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255)
            .MustAsync(BeUniqueEmail).WithMessage("Email already exists");
    }
    
    private async Task<bool> BeUniqueUsername(string username, CancellationToken ct)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username, ct);
    }
    
    private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email, ct);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IIdentityDbContext _context;
    
    public CreateUserCommandHandler(IIdentityDbContext context)
    {
        _context = context;
    }
    
    public async Task<int> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Status = UserStatus.Active
        };
        
        user.AddDomainEvent(new UserRegisteredEvent(user));
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        
        return user.Id;
    }
}
```

**Application/Users/Queries/GetUsers/GetUsers.cs**

```csharp
public record GetUsersQuery : IRequest<PaginatedList<UserDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public UserStatus? Status { get; init; }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly IMapper _mapper;
    
    public GetUsersQueryHandler(IIdentityDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = _context.Users.AsNoTracking();
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(u => 
                u.Username.Contains(request.SearchTerm) ||
                u.Email.Contains(request.SearchTerm) ||
                (u.FirstName != null && u.FirstName.Contains(request.SearchTerm)) ||
                (u.LastName != null && u.LastName.Contains(request.SearchTerm)));
        }
        
        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }
        
        return await query
            .OrderByDescending(u => u.Created)
            .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}

public class UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FullName { get; init; }
    public UserStatus Status { get; init; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<User, UserDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FullName));
        }
    }
}
```

### 4.3 Product Module (Catalog DB)

**Application/Products/Commands/CreateProduct/CreateProduct.cs**

```csharp
public record CreateProductCommand : IRequest<int>
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Sku { get; init; } = null!;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public int CategoryId { get; init; }
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly ICatalogDbContext _context;
    
    public CreateProductCommandValidator(ICatalogDbContext context)
    {
        _context = context;
        
        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(200);
        
        RuleFor(v => v.Sku)
            .NotEmpty()
            .MaximumLength(50)
            .MustAsync(BeUniqueSku).WithMessage("SKU already exists");
        
        RuleFor(v => v.Price)
            .GreaterThanOrEqualTo(0);
        
        RuleFor(v => v.StockQuantity)
            .GreaterThanOrEqualTo(0);
        
        RuleFor(v => v.CategoryId)
            .MustAsync(CategoryExists).WithMessage("Category not found");
    }
    
    private async Task<bool> BeUniqueSku(string sku, CancellationToken ct)
    {
        return !await _context.Products.AnyAsync(p => p.Sku == sku, ct);
    }
    
    private async Task<bool> CategoryExists(int categoryId, CancellationToken ct)
    {
        return await _context.Categories.AnyAsync(c => c.Id == categoryId, ct);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly ICatalogDbContext _context;
    
    public CreateProductCommandHandler(ICatalogDbContext context)
    {
        _context = context;
    }
    
    public async Task<int> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            Status = ProductStatus.Draft
        };
        
        product.AddDomainEvent(new ProductCreatedEvent(product));
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);
        
        return product.Id;
    }
}
```

**Application/Products/Queries/GetProducts/GetProducts.cs**

```csharp
public record GetProductsQuery : IRequest<PaginatedList<ProductDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public int? CategoryId { get; init; }
    public ProductStatus? Status { get; init; }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>
{
    private readonly ICatalogDbContext _context;
    private readonly IMapper _mapper;
    
    public GetProductsQueryHandler(ICatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<PaginatedList<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsNoTracking();
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(p => 
                p.Name.Contains(request.SearchTerm) ||
                p.Sku.Contains(request.SearchTerm));
        }
        
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }
        
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }
        
        return await query
            .OrderBy(p => p.Name)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}

public class ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string Sku { get; init; } = null!;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public ProductStatus Status { get; init; }
    public string CategoryName { get; init; } = null!;
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name));
        }
    }
}
```

### 4.4 Activity Log Service (MongoDB)

**Application/Common/Interfaces/IActivityLogService.cs**

```csharp
public interface IActivityLogService
{
    Task LogActivityAsync(string userId, string activityType, string description, 
        Dictionary<string, string>? metadata = null, CancellationToken ct = default);
    
    Task<List<ActivityLog>> GetUserActivitiesAsync(string userId, int limit = 100, 
        CancellationToken ct = default);
}
```

**Infrastructure/Services/ActivityLogService.cs**

```csharp
public class ActivityLogService : IActivityLogService
{
    private readonly IMongoDbContext _mongoContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ActivityLogService(IMongoDbContext mongoContext, IHttpContextAccessor httpContextAccessor)
    {
        _mongoContext = mongoContext;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task LogActivityAsync(string userId, string activityType, string description,
        Dictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        var log = new ActivityLog
        {
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
        };
        
        await _mongoContext.ActivityLogs.InsertOneAsync(log, cancellationToken: ct);
    }
    
    public async Task<List<ActivityLog>> GetUserActivitiesAsync(string userId, int limit = 100, 
        CancellationToken ct = default)
    {
        var filter = Builders<ActivityLog>.Filter.Eq(a => a.UserId, userId);
        var sort = Builders<ActivityLog>.Sort.Descending(a => a.Timestamp);
        
        return await _mongoContext.ActivityLogs
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync(ct);
    }
}
```

## Phase 5: Infrastructure - Entity Configurations & Initializers

### 5.1 Identity Configurations

**Infrastructure/Data/Identity/Configurations/UserConfiguration.cs**

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "identity");
        
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.FirstName)
            .HasMaxLength(100);
        
        builder.Property(u => u.LastName)
            .HasMaxLength(100);
        
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        
        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
```

### 5.2 Catalog Configurations

**Infrastructure/Data/Catalog/Configurations/ProductConfiguration.cs**

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "catalog");
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
        
        builder.HasIndex(p => p.Sku).IsUnique();
        
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
        
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Infrastructure/Data/Catalog/Configurations/CategoryConfiguration.cs**

```csharp
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "catalog");
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(c => c.Slug).IsUnique();
        
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 5.3 Database Initializers

**Infrastructure/Data/Identity/IdentityDbContextInitialiser.cs**

```csharp
public class IdentityDbContextInitialiser
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<IdentityDbContextInitialiser> _logger;
    
    public IdentityDbContextInitialiser(
        IdentityDbContext context,
        ILogger<IdentityDbContextInitialiser> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising Identity database");
            throw;
        }
    }
    
    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding Identity database");
            throw;
        }
    }
    
    private async Task TrySeedAsync()
    {
        // Seed Roles
        if (!await _context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new Role { Name = "Administrator", Description = "Full system access" },
                new Role { Name = "Manager", Description = "Manage users and products" },
                new Role { Name = "User", Description = "Standard user access" }
            };
            
            _context.Roles.AddRange(roles);
            await _context.SaveChangesAsync();
        }
        
        // Seed Users
        if (!await _context.Users.AnyAsync())
        {
            var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Administrator");
            var userRole = await _context.Roles.FirstAsync(r => r.Name == "User");
            
            var users = new[]
            {
                new User
                {
                    Username = "admin",
                    Email = "admin@multidbtemplate.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    Status = UserStatus.Active,
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow }
                    }
                },
                new User
                {
                    Username = "demo_user",
                    Email = "demo@example.com",
                    FirstName = "Demo",
                    LastName = "User",
                    Status = UserStatus.Active,
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { RoleId = userRole.Id, AssignedAt = DateTime.UtcNow }
                    }
                }
            };
            
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }
    }
}
```

**Infrastructure/Data/Catalog/CatalogDbContextInitialiser.cs**

```csharp
public class CatalogDbContextInitialiser
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<CatalogDbContextInitialiser> _logger;
    
    public CatalogDbContextInitialiser(
        CatalogDbContext context,
        ILogger<CatalogDbContextInitialiser> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising Catalog database");
            throw;
        }
    }
    
    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding Catalog database");
            throw;
        }
    }
    
    private async Task TrySeedAsync()
    {
        // Seed Categories
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Electronics", Slug = "electronics", Description = "Electronic devices and accessories" },
                new Category { Name = "Books", Slug = "books", Description = "Physical and digital books" },
                new Category { Name = "Clothing", Slug = "clothing", Description = "Fashion and apparel" }
            };
            
            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }
        
        // Seed Products
        if (!await _context.Products.AnyAsync())
        {
            var electronics = await _context.Categories.FirstAsync(c => c.Slug == "electronics");
            var books = await _context.Categories.FirstAsync(c => c.Slug == "books");
            
            var products = new[]
            {
                new Product
                {
                    Name = "Laptop Pro 15",
                    Description = "High-performance laptop",
                    Sku = "ELEC-001",
                    Price = 1299.99m,
                    StockQuantity = 50,
                    CategoryId = electronics.Id,
                    Status = ProductStatus.Published
                },
                new Product
                {
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse",
                    Sku = "ELEC-002",
                    Price = 29.99m,
                    StockQuantity = 200,
                    CategoryId = electronics.Id,
                    Status = ProductStatus.Published
                },
                new Product
                {
                    Name = "Clean Architecture Book",
                    Description = "Software architecture guide",
                    Sku = "BOOK-001",
                    Price = 39.99m,
                    StockQuantity = 100,
                    CategoryId = books.Id,
                    Status = ProductStatus.Published
                }
            };
            
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
        }
    }
}
```

## Phase 6: Web Layer - API Endpoints

### 6.1 User Endpoints

**Web/Endpoints/Users.cs**

```csharp
public class Users : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .Produces<int>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        
        group.MapGet(GetUsers)
            .WithName("GetUsers")
            .WithSummary("Get paginated list of users");
        
        group.MapGet(GetUserById, "{id}")
            .WithName("GetUserById");
    }
    
    public async Task<Created<int>> CreateUser(ISender sender, CreateUserCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/users/{id}", id);
    }
    
    public async Task<Ok<PaginatedList<UserDto>>> GetUsers(
        ISender sender,
        [AsParameters] GetUsersQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }
    
    public async Task<Results<Ok<UserDto>, NotFound>> GetUserById(
        ISender sender,
        int id)
    {
        var query = new GetUserByIdQuery { Id = id };
        var user = await sender.Send(query);
        return user != null ? TypedResults.Ok(user) : TypedResults.NotFound();
    }
}
```

### 6.2 Product Endpoints

**Web/Endpoints/Products.cs**

```csharp
public class Products : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new product");
        
        group.MapGet(GetProducts)
            .WithName("GetProducts")
            .WithSummary("Get paginated list of products");
        
        group.MapGet(GetProductById, "{id}")
            .WithName("GetProductById");
    }
    
    public async Task<Created<int>> CreateProduct(ISender sender, CreateProductCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/products/{id}", id);
    }
    
    public async Task<Ok<PaginatedList<ProductDto>>> GetProducts(
        ISender sender,
        [AsParameters] GetProductsQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }
    
    public async Task<Results<Ok<ProductDto>, NotFound>> GetProductById(
        ISender sender,
        int id)
    {
        var query = new GetProductByIdQuery { Id = id };
        var product = await sender.Send(query);
        return product != null ? TypedResults.Ok(product) : TypedResults.NotFound();
    }
}
```

### 6.3 Category Endpoints

**Web/Endpoints/Categories.cs**

```csharp
public class Categories : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreateCategory);
        group.MapGet(GetCategories);
        group.MapGet(GetCategoryById, "{id}");
    }
    
    public async Task<Created<int>> CreateCategory(ISender sender, CreateCategoryCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/categories/{id}", id);
    }
    
    public async Task<Ok<List<CategoryDto>>> GetCategories(ISender sender)
    {
        var query = new GetCategoriesQuery();
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }
    
    public async Task<Results<Ok<CategoryDto>, NotFound>> GetCategoryById(
        ISender sender, int id)
    {
        var query = new GetCategoryByIdQuery { Id = id };
        var category = await sender.Send(query);
        return category != null ? TypedResults.Ok(category) : TypedResults.NotFound();
    }
}
```

### 6.4 Update Program.cs

**Web/Program.cs**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Initialize databases in development
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabasesAsync();
}
else
{
    app.UseHsts();
}

app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.UseExceptionHandler(options => { });

app.MapEndpoints();

app.Run();
```

**Web/Infrastructure/WebApplicationExtensions.cs**

```csharp
public static class WebApplicationExtensions
{
    public static async Task InitialiseDatabasesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        // Initialize Identity DB (SQL Server)
        var identityInitialiser = scope.ServiceProvider
            .GetRequiredService<IdentityDbContextInitialiser>();
        await identityInitialiser.InitialiseAsync();
        await identityInitialiser.SeedAsync();
        
        // Initialize Catalog DB (PostgreSQL)
        var catalogInitialiser = scope.ServiceProvider
            .GetRequiredService<CatalogDbContextInitialiser>();
        await catalogInitialiser.InitialiseAsync();
        await catalogInitialiser.SeedAsync();
        
        // MongoDB doesn't need explicit initialization
        // Collections are created automatically on first insert
    }
}
```

## Phase 7: Database Migrations

### 7.1 Create Identity Migration (SQL Server)

```bash
cd src/Infrastructure

dotnet ef migrations add InitialIdentityCreate \
  --context IdentityDbContext \
  --output-dir Data/Identity/Migrations \
  --project Infrastructure.csproj \
  --startup-project ../Web/Web.csproj

dotnet ef database update --context IdentityDbContext --startup-project ../Web/Web.csproj
```

### 7.2 Create Catalog Migration (PostgreSQL)

```bash
dotnet ef migrations add InitialCatalogCreate \
  --context CatalogDbContext \
  --output-dir Data/Catalog/Migrations \
  --project Infrastructure.csproj \
  --startup-project ../Web/Web.csproj

dotnet ef database update --context CatalogDbContext --startup-project ../Web/Web.csproj
```

## Phase 8: Documentation

### 8.1 Create README.md

**README.md**

```markdown
# Multi-Database Clean Architecture Template

Generic boilerplate template implementing Clean Architecture with multi-database support.

## Architecture Overview

This template demonstrates Clean Architecture principles with three separate databases:

- **SQL Server** - Identity & Authentication (Users, Roles)
- **PostgreSQL** - Catalog Management (Products, Categories)
- **MongoDB** - Document Storage (Logs, Activity tracking)

## Tech Stack

- .NET 9.0
- Entity Framework Core 9
- MediatR (CQRS)
- FluentValidation
- AutoMapper
- MongoDB.Driver
- NSwag (OpenAPI)

## Project Structure

```

src/

├── Domain/              # Entities, Enums, Events, ValueObjects

├── Application/         # Use Cases (Commands/Queries), Interfaces

├── Infrastructure/      # Data Access, External Services

└── Web/                 # API Endpoints, Configuration

````

## Prerequisites

- .NET 9 SDK
- SQL Server or LocalDB
- PostgreSQL 14+
- MongoDB 6+
- Visual Studio 2022 or VS Code

## Getting Started

### 1. Clone the repository

### 2. Update connection strings

Edit `src/Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Server=(localdb)\\mssqllocaldb;Database=YourProject_Identity;...",
    "CatalogDb": "Host=localhost;Database=YourProject_Catalog;Username=postgres;Password=your_password",
    "MongoDb": "mongodb://localhost:27017/YourProject_Documents"
  }
}
````

### 3. Apply migrations

```bash
cd src/Infrastructure

# Identity DB (SQL Server)
dotnet ef database update --context IdentityDbContext --startup-project ../Web

# Catalog DB (PostgreSQL)
dotnet ef database update --context CatalogDbContext --startup-project ../Web
```

### 4. Run the application

```bash
cd src/Web
dotnet run
```

Access Swagger UI: https://localhost:5001/api

## Sample Entities

### Users (SQL Server - IdentityDb)

- User management with roles
- Optimized for transactional operations
- ACID compliance for authentication

### Products & Categories (PostgreSQL - CatalogDb)

- Product catalog with categories
- Better performance for complex queries
- JSON support for flexible data

### Activity Logs (MongoDB - MongoDb)

- User activity tracking
- Flexible schema for varied log types
- High-write performance

## Database Selection Guide

| Use Case | Database | Reason |

|----------|----------|--------|

| User Auth | SQL Server | ACID, Transactions, Security |

| Product Catalog | PostgreSQL | Advanced queries, JSON, Performance |

| Logs/Analytics | MongoDB | Flexible schema, High writes |

## API Endpoints

### Users

- `POST /api/users` - Create user
- `GET /api/users` - Get users (paginated, filterable)
- `GET /api/users/{id}` - Get user by ID

### Products

- `POST /api/products` - Create product
- `GET /api/products` - Get products (paginated, filterable)
- `GET /api/products/{id}` - Get product by ID

### Categories

- `POST /api/categories` - Create category
- `GET /api/categories` - Get all categories
- `GET /api/categories/{id}` - Get category by ID

## Key Features

- Clean Architecture (4 layers with clear dependencies)
- CQRS with MediatR
- Multi-database support
- Automatic validation (FluentValidation)
- Domain Events
- Repository Pattern
- Specification Pattern
- Pipeline Behaviors (Logging, Performance, Authorization)
- Swagger/OpenAPI documentation
- Health checks
- Audit trails

## Customization for New Projects

1. Rename namespaces from `MultiDbTemplate` to your project name
2. Update connection strings
3. Modify/add entities based on your domain
4. Create new use cases using the existing patterns
5. Update seed data in initializers

## Testing

Run tests:

```bash
dotnet test
```

## Deployment

This template is ready for Azure deployment using Azure Developer CLI (azd).

## License

MIT

````

### 8.2 Create Database Selection Guide

**docs/DATABASE_SELECTION_GUIDE.md**
```markdown
# Database Selection Guide

## When to Use SQL Server (IdentityDbContext)

### Use For:
- User authentication and authorization
- Transactional data requiring ACID compliance
- Data with strict relationships
- Financial transactions
- Audit trails requiring immutability

### Advantages:
- Strong ACID compliance
- Excellent for complex relationships
- Native Windows integration
- Great tooling and support
- Mature replication and backup

### Disadvantages:
- Licensing costs (non-Express versions)
- Windows-centric
- Less flexible schema changes

## When to Use PostgreSQL (CatalogDbContext)

### Use For:
- Product catalogs
- Content management
- Search-heavy applications
- Data requiring JSON columns
- Complex analytical queries

### Advantages:
- Open source and free
- Superior full-text search
- Native JSON support
- Advanced indexing
- Better performance for reads
- Cross-platform

### Disadvantages:
- Less tooling than SQL Server
- Smaller community in .NET ecosystem

## When to Use MongoDB (MongoDbContext)

### Use For:
- Activity/audit logs
- Real-time analytics
- Flexible schemas
- High-volume writes
- Document-oriented data
- Prototyping with changing requirements

### Advantages:
- Schema flexibility
- Horizontal scalability
- High write performance
- Native JSON/BSON
- Good for unstructured data

### Disadvantages:
- No transactions across documents (pre-4.0)
- More complex queries
- Eventual consistency challenges
- Larger storage footprint

## Decision Matrix

| Criteria | SQL Server | PostgreSQL | MongoDB |
|----------|------------|------------|---------|
| ACID Transactions | ★★★★★ | ★★★★★ | ★★★☆☆ |
| Schema Flexibility | ★★☆☆☆ | ★★★☆☆ | ★★★★★ |
| Write Performance | ★★★☆☆ | ★★★★☆ | ★★★★★ |
| Complex Queries | ★★★★★ | ★★★★★ | ★★★☆☆ |
| Full-Text Search | ★★★☆☆ | ★★★★★ | ★★★★☆ |
| Horizontal Scaling | ★★☆☆☆ | ★★★☆☆ | ★★★★★ |
| Windows Integration | ★★★★★ | ★★★☆☆ | ★★★☆☆ |
| Cost | ★★☆☆☆ | ★★★★★ | ★★★★★ |
````

### 8.3 Create Architecture Decision Record

**docs/architecture/ADR-001-Multi-Database-Strategy.md**

```markdown
# ADR 001: Multi-Database Strategy

## Status
Accepted

## Context
Modern applications often have diverse data storage requirements. Different types of data have different characteristics in terms of consistency, scalability, query patterns, and structure.

## Decision
We implement a multi-database architecture using:

1. **SQL Server** for transactional, relational data (Users, Authentication)
2. **PostgreSQL** for catalog/content data (Products, Categories)
3. **MongoDB** for flexible document storage (Logs, Analytics)

Each database has its own:
- DbContext/Repository
- Connection string
- Migration strategy
- Bounded context

## Consequences

### Positive
- Optimal database choice for each use case
- Independent scaling of different data types
- Technology diversity for learning
- Better performance for specific workloads

### Negative
- Increased complexity in operations
- Multiple database maintenance
- Potential data consistency challenges
- Higher infrastructure costs
- Steeper learning curve

### Mitigation
- Clear bounded contexts prevent confusion
- Comprehensive documentation
- Automated deployment scripts
- Eventual consistency patterns where needed
```

## Phase 9: Testing & Validation

### 9.1 Test SQL Server (Identity DB)

```bash
# Access SQL Server
# Check Users table exists
# Verify seed data (admin, demo_user)
```

### 9.2 Test PostgreSQL (Catalog DB)

```bash
# Access PostgreSQL
psql -U postgres -d MultiDbTemplate_Catalog_Dev
\dt catalog.*
SELECT * FROM catalog."Products";
```

### 9.3 Test MongoDB

```bash
# Access MongoDB
mongosh
use MultiDbTemplate_Documents_Dev
db.activity_logs.find()
```

### 9.4 Test API Endpoints

```bash
# Run application
cd src/Web
dotnet run

# Test with curl or Postman
curl -X GET https://localhost:5001/api/users
curl -X GET https://localhost:5001/api/products
curl -X GET https://localhost:5001/api/categories
```

## Phase 10: Create Reusable Package (Optional)

### 10.1 Create Template Package Script

**scripts/create-template-package.ps1**

```powershell
# PowerShell script to create new project from template

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "."
)

Write-Host "Creating project: $ProjectName" -ForegroundColor Green

# Clone template
$templatePath = "path/to/MultiDbCleanArchitectureTemplate"
$targetPath = Join-Path $OutputPath $ProjectName

Copy-Item -Path $templatePath -Destination $targetPath -Recurse -Exclude @("bin", "obj", ".vs", ".git")

# Rename namespaces
$files = Get-ChildItem -Path $targetPath -Include *.cs,*.csproj,*.json -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace "MultiDbTemplate", $ProjectName
    Set-Content $file.FullName $content
}

Write-Host "Project created successfully at: $targetPath" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update connection strings in appsettings.json"
Write-Host "2. Run: dotnet ef database update --context IdentityDbContext"
Write-Host "3. Run: dotnet ef database update --context CatalogDbContext"
Write-Host "4. Run: dotnet run"
```

## Summary

### What You'll Have

1. **Complete Clean Architecture Template**

   - 4 layers with clear separation
   - Multi-database support out of the box
   - Sample entities demonstrating patterns

2. **Sample Features**

   - User management (SQL Server)
   - Product catalog (PostgreSQL)
   - Activity logging (MongoDB)

3. **Production-Ready Patterns**

   - CQRS with MediatR
   - Validation pipeline
   - Domain events
   - Repository pattern
   - Audit trails

4. **Comprehensive Documentation**

   - README with setup instructions
   - Database selection guide
   - Architecture decision records
   - API documentation (Swagger)

### How to Use for New Projects

1. Clone this template
2. Run rename script (or manual find/replace)
3. Update connection strings
4. Modify entities for your domain
5. Create new use cases following existing patterns
6. Deploy

### Learning Outcomes

- Understand Clean Architecture principles
- Learn multi-database strategies
- Master CQRS pattern
- Practice Domain-Driven Design
- Experience with EF Core migrations
- Work with MongoDB in .NET

### To-dos

- [ ] Initialize project from template and clean up template-specific code
- [ ] Setup multi-database infrastructure (SQL Server, PostgreSQL, MongoDB contexts)
- [ ] Create sample domain entities (User, Product, Category) with events and enums
- [ ] Implement use cases (Commands/Queries) with validators for all entities
- [ ] Implement DbContexts, configurations, and initializers for all databases
- [ ] Create API endpoints for Users, Products, and Categories
- [ ] Create and apply migrations, test all endpoints and databases
- [ ] Create comprehensive documentation (README, guides, ADRs)
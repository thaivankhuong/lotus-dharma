# MÔ TẢ CHI TIẾT QUY TRÌNH CHẠY - CLEAN ARCHITECTURE PROJECT

## 📋 MỤC LỤC

1. [Phase 1: Application Startup](#phase-1-application-startup)
2. [Phase 2: Service Registration](#phase-2-service-registration)
3. [Phase 3: Middleware Pipeline Configuration](#phase-3-middleware-pipeline-configuration)
4. [Phase 4: Database Initialization](#phase-4-database-initialization)
5. [Phase 5: Request Processing Flow](#phase-5-request-processing-flow)
6. [Phase 6: MediatR Pipeline](#phase-6-mediatr-pipeline)
7. [Sơ Đồ Tổng Quan](#sơ-đồ-tổng-quan)

---

## PHASE 1: APPLICATION STARTUP

### **File: `src/Web/Program.cs`**

```csharp
var builder = WebApplication.CreateBuilder(args);
```

### **Các Bước:**

#### 1.1 **WebApplicationBuilder Được Tạo**
- Đọc configuration từ:
  - `appsettings.json`
  - `appsettings.Development.json` (môi trường Development)
  - Environment variables
  - Command-line arguments
  - User secrets (nếu có)

#### 1.2 **Logging Configuration**
- Configure logging providers (Console, Debug, EventSource)
- Log level được set theo `appsettings.json`:
  ```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
  ```

---

## PHASE 2: SERVICE REGISTRATION (Dependency Injection)

### **Thứ Tự Đăng Ký Services:**

#### 2.1 **AddKeyVaultIfConfigured()** (Optional)
```csharp
builder.AddKeyVaultIfConfigured();
```
- Kiểm tra có `AZURE_KEY_VAULT_ENDPOINT` không
- Nếu có → Kết nối Azure Key Vault để lấy secrets
- Dùng `DefaultAzureCredential` để authenticate

#### 2.2 **AddApplicationServices()** - Application Layer
**File: `src/Application/DependencyInjection.cs`**

```csharp
builder.AddApplicationServices();
```

**Đăng ký:**
1. ✅ **AutoMapper** - Object mapping
   ```csharp
   builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
   ```

2. ✅ **FluentValidation** - Validators cho Commands/Queries
   ```csharp
   builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
   ```

3. ✅ **MediatR** - CQRS pattern với Pipeline Behaviours
   ```csharp
   builder.Services.AddMediatR(cfg => {
       cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
       cfg.AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>));
       cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
       cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
       cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
       cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
   });
   ```

#### 2.3 **AddInfrastructureServices()** - Infrastructure Layer
**File: `src/Infrastructure/DependencyInjection.cs`**

```csharp
builder.AddInfrastructureServices();
```

**Đăng ký:**
1. ✅ **EF Core Interceptors**
   ```csharp
   builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
   builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
   ```

2. ✅ **ApplicationDbContext** (EF Core)
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>((sp, options) => {
       options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
       options.UseSqlServer(connectionString);
   });
   ```

3. ✅ **IApplicationDbContext Interface**
   ```csharp
   builder.Services.AddScoped<IApplicationDbContext>(
       provider => provider.GetRequiredService<ApplicationDbContext>()
   );
   ```

4. ✅ **Database Initializer**
   ```csharp
   builder.Services.AddScoped<ApplicationDbContextInitialiser>();
   ```

5. ✅ **ASP.NET Core Identity**
   ```csharp
   builder.Services
       .AddDefaultIdentity<ApplicationUser>()
       .AddRoles<IdentityRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>();
   ```

6. ✅ **Time Provider & Identity Service**
   ```csharp
   builder.Services.AddSingleton(TimeProvider.System);
   builder.Services.AddTransient<IIdentityService, IdentityService>();
   ```

7. ✅ **Authorization Policies**
   ```csharp
   builder.Services.AddAuthorization(options =>
       options.AddPolicy(Policies.CanPurge, 
           policy => policy.RequireRole(Roles.Administrator))
   );
   ```

#### 2.4 **AddWebServices()** - Web Layer
**File: `src/Web/DependencyInjection.cs`**

```csharp
builder.AddWebServices();
```

**Đăng ký:**
1. ✅ **Database Developer Page Exception Filter**
   ```csharp
   builder.Services.AddDatabaseDeveloperPageExceptionFilter();
   ```

2. ✅ **Current User Service**
   ```csharp
   builder.Services.AddScoped<IUser, CurrentUser>();
   ```

3. ✅ **HttpContextAccessor**
   ```csharp
   builder.Services.AddHttpContextAccessor();
   ```

4. ✅ **Health Checks**
   ```csharp
   builder.Services.AddHealthChecks()
       .AddDbContextCheck<ApplicationDbContext>();
   ```

5. ✅ **Custom Exception Handler**
   ```csharp
   builder.Services.AddExceptionHandler<CustomExceptionHandler>();
   ```

6. ✅ **API Behavior Configuration**
   ```csharp
   builder.Services.Configure<ApiBehaviorOptions>(options =>
       options.SuppressModelStateInvalidFilter = true
   );
   ```

7. ✅ **OpenAPI/Swagger (NSwag)**
   ```csharp
   builder.Services.AddOpenApiDocument((configure, sp) => {
       configure.Title = "CleanArchitecture API";
   });
   ```

---

## PHASE 3: MIDDLEWARE PIPELINE CONFIGURATION

### **File: `src/Web/Program.cs`**

```csharp
var app = builder.Build();
```

### **Thứ Tự Middleware (Rất Quan Trọng!):**

```
┌─────────────────────────────────────────────────────────┐
│  Request từ Client                                      │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 1. HSTS (HTTPS Strict Transport Security)              │
│    - Chỉ chạy trong Production                          │
│    - Force client dùng HTTPS                            │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 2. Health Checks Middleware                             │
│    app.UseHealthChecks("/health")                       │
│    - Kiểm tra health của app & database                │
│    - Endpoint: GET /health                              │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 3. HTTPS Redirection                                    │
│    app.UseHttpsRedirection()                            │
│    - HTTP → HTTPS redirect                              │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 4. Static Files Middleware                              │
│    app.UseStaticFiles()                                 │
│    - Serve files từ wwwroot/                            │
│    - CSS, JS, images, etc.                              │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 5. Swagger UI Middleware                                │
│    app.UseSwaggerUi()                                   │
│    - Swagger UI: /api                                   │
│    - OpenAPI spec: /api/specification.json              │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 6. Exception Handler Middleware                         │
│    app.UseExceptionHandler()                            │
│    - CustomExceptionHandler xử lý exceptions            │
│    - Convert exceptions → HTTP responses                │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 7. Routing                                              │
│    - Match request đến endpoint                         │
│    - app.MapEndpoints() - Custom minimal APIs           │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 8. Authentication & Authorization                       │
│    - RequireAuthorization() trên từng endpoint          │
│    - Kiểm tra user identity & roles                     │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│ 9. Endpoint Execution                                   │
│    - Chạy handler của endpoint                          │
│    - Gọi MediatR để xử lý Command/Query                 │
└─────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│  Response trả về Client                                 │
└─────────────────────────────────────────────────────────┘
```

### **Chi Tiết Code:**

```csharp
var app = builder.Build();

// 🔧 Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Development only: Initialize & seed database
    await app.InitialiseDatabaseAsync();
}
else
{
    // Production only: HSTS
    app.UseHsts();
}

// Middleware Pipeline (thứ tự quan trọng!)
app.UseHealthChecks("/health");        // 1. Health checks
app.UseHttpsRedirection();             // 2. HTTPS redirect
app.UseStaticFiles();                  // 3. Static files

// 4. Swagger UI
app.UseSwaggerUi(settings => {
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

// 5. Exception handling
app.UseExceptionHandler(options => { });

// 6. Endpoint mapping
app.MapEndpoints();                    // Custom minimal API endpoints

// 7. Start listening
app.Run();
```

---

## PHASE 4: DATABASE INITIALIZATION (Development Only)

### **File: `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs`**

```csharp
await app.InitialiseDatabaseAsync();
```

### **Các Bước:**

#### 4.1 **InitialiseAsync()**
```csharp
await _context.Database.EnsureDeletedAsync();   // Xóa DB cũ
await _context.Database.EnsureCreatedAsync();   // Tạo DB mới từ model
```

⚠️ **Lưu ý:** Chỉ dùng trong Development! Production dùng Migrations.

#### 4.2 **SeedAsync()**
```csharp
// 1. Tạo roles
var administratorRole = new IdentityRole(Roles.Administrator);
await _roleManager.CreateAsync(administratorRole);

// 2. Tạo default admin user
var administrator = new ApplicationUser { 
    UserName = "administrator@localhost", 
    Email = "administrator@localhost" 
};
await _userManager.CreateAsync(administrator, "Administrator1!");
await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });

// 3. Seed sample data
if (!_context.TodoLists.Any())
{
    _context.TodoLists.Add(new TodoList
    {
        Title = "Todo List",
        Items = {
            new TodoItem { Title = "Make a todo list 📃" },
            new TodoItem { Title = "Check off the first item ✅" },
            // ...
        }
    });
    await _context.SaveChangesAsync();
}
```

---

## PHASE 5: REQUEST PROCESSING FLOW

### **Ví Dụ: Request để lấy danh sách Todo Items**

```
GET /api/todoitems?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

### **Luồng Xử Lý:**

```
┌──────────────────────────────────────────────────────────────┐
│ 1. Client gửi HTTP Request                                   │
│    GET /api/todoitems?pageNumber=1&pageSize=10               │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ 2. Request đi qua Middleware Pipeline                        │
│    ├─ UseHttpsRedirection()                                  │
│    ├─ UseStaticFiles()                                       │
│    └─ UseExceptionHandler()                                  │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ 3. Routing Match Endpoint                                    │
│    Pattern: GET /api/todoitems                               │
│    Handler: TodoItems.GetTodoItemsWithPagination()           │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ 4. Authorization Check                                       │
│    .RequireAuthorization() → Kiểm tra JWT token              │
│    ├─ Valid? → Continue                                      │
│    └─ Invalid? → 401 Unauthorized                            │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ 5. Model Binding                                             │
│    Query parameters → GetTodoItemsWithPaginationQuery        │
│    {                                                         │
│        PageNumber = 1,                                       │
│        PageSize = 10                                         │
│    }                                                         │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ 6. Endpoint Handler Execution                                │
│    File: src/Web/Endpoints/TodoItems.cs                      │
└──────────────────────────────────────────────────────────────┘
```

### **Endpoint Handler Code:**

```csharp
// File: src/Web/Endpoints/TodoItems.cs
public async Task<Ok<PaginatedList<TodoItemBriefDto>>> 
    GetTodoItemsWithPagination(
        ISender sender, 
        [AsParameters] GetTodoItemsWithPaginationQuery query)
{
    // Gửi query đến MediatR
    var result = await sender.Send(query);
    
    return TypedResults.Ok(result);
}
```

---

## PHASE 6: MEDIATR PIPELINE

### **Khi `sender.Send(query)` được gọi:**

```
┌──────────────────────────────────────────────────────────────┐
│ Query/Command được gửi vào MediatR Pipeline                  │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ BEHAVIOUR 1: LoggingBehaviour                                │
│ ├─ Log request name: GetTodoItemsWithPaginationQuery         │
│ ├─ Log user info (userId, userName)                          │
│ └─ Log request data                                          │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ BEHAVIOUR 2: UnhandledExceptionBehaviour                     │
│ ├─ Try-catch wrapper                                         │
│ └─ Log any unhandled exceptions                              │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ BEHAVIOUR 3: AuthorizationBehaviour                          │
│ ├─ Kiểm tra [Authorize] attributes                           │
│ ├─ Kiểm tra roles nếu có                                     │
│ └─ Throw UnauthorizedAccessException nếu không đủ quyền      │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ BEHAVIOUR 4: ValidationBehaviour                             │
│ ├─ Tìm validators cho Query/Command                          │
│ ├─ Chạy FluentValidation validators                          │
│ └─ Throw ValidationException nếu có lỗi                      │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ BEHAVIOUR 5: PerformanceBehaviour                            │
│ ├─ Start timer                                               │
│ ├─ Execute handler                                           │
│ ├─ Stop timer                                                │
│ └─ Log warning nếu > 500ms                                   │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ HANDLER EXECUTION                                            │
│ GetTodoItemsWithPaginationQueryHandler                       │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ Database Query (EF Core)                                     │
│ ├─ AuditableEntityInterceptor (auto audit fields)           │
│ ├─ DispatchDomainEventsInterceptor (domain events)          │
│ └─ Execute SQL query                                         │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ AutoMapper Projection                                        │
│ Entity → DTO (TodoItemBriefDto)                              │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ Return Result                                                │
│ PaginatedList<TodoItemBriefDto>                              │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ HTTP Response                                                │
│ Status: 200 OK                                               │
│ Content-Type: application/json                               │
│ Body: { items: [...], pageNumber: 1, totalPages: 5 }        │
└──────────────────────────────────────────────────────────────┘
```

### **Handler Code:**

```csharp
// File: src/Application/TodoItems/Queries/GetTodoItemsWithPagination.cs
public class GetTodoItemsWithPaginationQueryHandler 
    : IRequestHandler<GetTodoItemsWithPaginationQuery, PaginatedList<TodoItemBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public async Task<PaginatedList<TodoItemBriefDto>> Handle(
        GetTodoItemsWithPaginationQuery request, 
        CancellationToken cancellationToken)
    {
        return await _context.TodoItems
            .Where(x => x.ListId == request.ListId)
            .OrderBy(x => x.Title)
            .ProjectTo<TodoItemBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
```

---

## EXCEPTION HANDLING FLOW

### **File: `src/Web/Infrastructure/CustomExceptionHandler.cs`**

```
┌──────────────────────────────────────────────────────────────┐
│ Exception xảy ra trong pipeline                              │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ UseExceptionHandler() Middleware catches exception           │
└──────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│ CustomExceptionHandler.TryHandleAsync()                      │
│ ├─ Check exception type                                      │
│ └─ Route đến handler tương ứng                               │
└──────────────────────────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
┌────────────────┐ ┌─────────────┐ ┌─────────────────┐
│ ValidationExc  │ │ NotFoundExc │ │ UnauthorizedExc │
│   → 400        │ │   → 404     │ │   → 401         │
└────────────────┘ └─────────────┘ └─────────────────┘
```

### **Exception Handling Code:**

```csharp
public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler()
    {
        _exceptionHandlers = new()
        {
            { typeof(ValidationException), HandleValidationException },
            { typeof(NotFoundException), HandleNotFoundException },
            { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
            { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
        };
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true; // Exception đã xử lý
        }

        return false; // Không xử lý, chuyển cho handler tiếp theo
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        
        await httpContext.Response.WriteAsJsonAsync(
            new ValidationProblemDetails(exception.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            });
    }
    
    // ... các handler khác
}
```

---

## SƠ ĐỒ TỔNG QUAN: COMPLETE REQUEST FLOW

```
┌─────────────────────────────────────────────────────────────────┐
│                    CLIENT (Browser/Postman)                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP Request
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      KESTREL WEB SERVER                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    MIDDLEWARE PIPELINE                           │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. HTTPS Redirection                                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 2. Static Files                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 3. Exception Handler (CustomExceptionHandler)            │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 4. Routing                                               │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 5. Authentication & Authorization                        │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   WEB LAYER (Endpoints)                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Minimal API Endpoint Handler                             │   │
│  │ - Model binding                                          │   │
│  │ - Parameter validation                                   │   │
│  │ - Call MediatR: await sender.Send(command/query)        │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   MEDIATR PIPELINE                               │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 1. LoggingBehaviour - Log request                       │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 2. UnhandledExceptionBehaviour - Catch exceptions       │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 3. AuthorizationBehaviour - Check permissions           │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 4. ValidationBehaviour - FluentValidation               │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 5. PerformanceBehaviour - Monitor slow queries          │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│            APPLICATION LAYER (Command/Query Handler)             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Handler Logic:                                           │   │
│  │ - Access IApplicationDbContext                          │   │
│  │ - Business logic                                        │   │
│  │ - Return result/DTO                                     │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ ApplicationDbContext (EF Core)                           │   │
│  │ ┌────────────────────────────────────────────────────┐   │   │
│  │ │ Interceptors:                                      │   │   │
│  │ │ - AuditableEntityInterceptor                       │   │   │
│  │ │ - DispatchDomainEventsInterceptor                  │   │   │
│  │ └────────────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   DATABASE (SQL Server)                          │
│  - TodoLists table                                               │
│  - TodoItems table                                               │
│  - AspNetUsers table                                             │
│  - AspNetRoles table                                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Results
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   HTTP RESPONSE                                  │
│  Status: 200 OK                                                  │
│  Content-Type: application/json                                  │
│  Body: { ... }                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## TÓM TẮT: CÁC BƯỚC QUAN TRỌNG

### **🚀 Startup Phase (Chỉ chạy 1 lần khi app khởi động)**
1. Load configuration
2. Register services (DI Container)
3. Build application
4. Configure middleware pipeline
5. Initialize database (Development only)
6. Start listening for requests

### **🔄 Request Processing Phase (Mỗi request)**
1. Request đến → Middleware pipeline
2. Routing match endpoint
3. Authorization check
4. Model binding
5. Execute endpoint handler
6. MediatR pipeline (behaviours)
7. Command/Query handler execution
8. Database access (với interceptors)
9. Return response

### **⚙️ MediatR Pipeline Behaviours (Thứ tự thực thi)**
1. **LoggingBehaviour** → Log request
2. **UnhandledExceptionBehaviour** → Catch exceptions
3. **AuthorizationBehaviour** → Check [Authorize]
4. **ValidationBehaviour** → FluentValidation
5. **PerformanceBehaviour** → Monitor performance
6. **Handler** → Business logic

---

## 📚 FILES QUAN TRỌNG ĐỂ HIỂU FLOW

| File | Mục Đích |
|------|----------|
| `src/Web/Program.cs` | Entry point, middleware configuration |
| `src/Web/DependencyInjection.cs` | Web services registration |
| `src/Application/DependencyInjection.cs` | Application services + MediatR |
| `src/Infrastructure/DependencyInjection.cs` | Infrastructure services + EF Core |
| `src/Web/Infrastructure/CustomExceptionHandler.cs` | Global exception handling |
| `src/Web/Endpoints/TodoItems.cs` | Endpoint definition |
| `src/Application/Common/Behaviours/` | MediatR pipeline behaviours |
| `src/Infrastructure/Data/Interceptors/` | EF Core interceptors |

---

## 🎯 KEY TAKEAWAYS

1. **Middleware order matters!** Thứ tự middleware rất quan trọng
2. **MediatR is the heart** của CQRS pattern - mọi business logic đi qua MediatR
3. **Separation of Concerns** - Mỗi layer có trách nhiệm riêng biệt
4. **Pipeline Behaviours** giúp implement cross-cutting concerns (logging, validation, etc.)
5. **Dependency Injection** everywhere - Không hardcode dependencies
6. **Exception handling** được centralized trong CustomExceptionHandler

---

**🎉 Bây giờ bạn đã hiểu toàn bộ flow từ khi app start đến khi xử lý request!**


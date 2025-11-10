# 🔐 Hướng dẫn tự động lấy UserId từ JWT Claims

## 📋 Tổng quan

Project đã được cấu hình để **tự động lấy UserId** từ JWT token khi user đã login, không cần truyền `CreatedIdUser` hoặc `UpdatedIdUser` từ client nữa.

## ✅ Đã implement cho

- ✅ **Product**: `CreatedIdUser`, `UpdatedIdUser`
- ✅ **Category**: `CreatedIdUser`, `UpdatedIdUser`

## 🔧 Cách hoạt động

### 1. IUser Service

```csharp
// src/Application/Common/Interfaces/IUser.cs
public interface IUser
{
    string? Id { get; }           // UserId từ JWT claims
    List<string>? Roles { get; }  // Roles từ JWT claims
}
```

### 2. Implementation

```csharp
// src/Web/Services/CurrentUser.cs
public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string? Id => _httpContextAccessor.HttpContext?.User
        ?.FindFirstValue(ClaimTypes.NameIdentifier);
        
    public List<string>? Roles => _httpContextAccessor.HttpContext?.User
        ?.FindAll(ClaimTypes.Role)
        .Select(x => x.Value)
        .ToList();
}
```

### 3. Sử dụng trong Command Handlers

#### CreateProductCommand

```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;  // ← Inject IUser

    public CreateProductCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            CreatedIdUser = _user.Id,  // ← Tự động lấy từ JWT
            IsActive = true
        };

        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
```

#### UpdateProductCommand

```csharp
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;  // ← Inject IUser

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products.FindAsync(request.Id);
        
        Guard.Against.NotFound(request.Id, entity);

        entity.Name = request.Name;
        entity.Price = request.Price;
        entity.UpdatedIdUser = _user.Id;  // ← Tự động lấy từ JWT

        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

## 📝 API Request Examples

### TRƯỚC (Phải truyền UserId thủ công - KHÔNG AN TOÀN)

```json
POST /api/products
Headers:
  Authorization: Bearer eyJhbGc...

Body:
{
  "name": "iPhone 15",
  "price": 999,
  "categoryId": 1,
  "createdIdUser": "1"  ← ❌ Client có thể fake UserId!
}
```

### SAU (Tự động lấy từ JWT - AN TOÀN)

```json
POST /api/products
Headers:
  Authorization: Bearer eyJhbGc...

Body:
{
  "name": "iPhone 15",
  "price": 999,
  "categoryId": 1
  // ✅ Không cần createdIdUser, tự động lấy từ JWT token
}
```

## 🧪 Testing Flow

### 1. Register User

```bash
POST /api/users/register
{
  "email": "test@example.com",
  "password": "Test123!@#",
  "confirmPassword": "Test123!@#"
}

Response: 200 OK
{
  "message": "Đăng ký thành công!"
}
```

### 2. Login

```bash
POST /api/users/login
{
  "email": "test@example.com",
  "password": "Test123!@#"
}

Response: 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": 1,  ← UserId này sẽ được lưu vào CreatedIdUser
  "email": "test@example.com",
  "userName": "test@example.com"
}
```

### 3. Create Product (với JWT token)

```bash
POST /api/products
Headers:
  Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
  
Body:
{
  "name": "iPhone 15 Pro",
  "description": "Latest iPhone",
  "price": 999.99,
  "stock": 100,
  "categoryId": 1
}

Response: 201 Created
{
  "id": 1
}
```

### 4. Check Database

```sql
SELECT 
    "Id", 
    "Name", 
    "CreatedIdUser", 
    "Created"
FROM "Products"
WHERE "Id" = 1;

-- Result:
-- Id | Name           | CreatedIdUser | Created
-- 1  | iPhone 15 Pro  | 1            | 2025-11-07 10:30:00
--                       ↑ Tự động lấy từ JWT!
```

### 5. Update Product

```bash
PUT /api/products/1
Headers:
  Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
  
Body:
{
  "id": 1,
  "name": "iPhone 15 Pro Max",
  "price": 1099.99,
  "stock": 80,
  "categoryId": 1,
  "isActive": true
}

Response: 204 No Content
```

### 6. Check Database Again

```sql
SELECT 
    "Id", 
    "Name", 
    "CreatedIdUser",
    "UpdatedIdUser",
    "LastModified"
FROM "Products"
WHERE "Id" = 1;

-- Result:
-- Id | Name              | CreatedIdUser | UpdatedIdUser | LastModified
-- 1  | iPhone 15 Pro Max | 1            | 1             | 2025-11-07 11:00:00
--                                         ↑ Tự động update!
```

## 🔍 JWT Token Claims

Khi user login, JWT token chứa các claims:

```json
{
  "nameid": "1",                    ← ClaimTypes.NameIdentifier (UserId)
  "unique_name": "test@example.com", ← ClaimTypes.Name
  "email": "test@example.com",       ← ClaimTypes.Email
  "role": ["User", "Admin"],         ← ClaimTypes.Role
  "nbf": 1699360200,
  "exp": 1699446600,
  "iat": 1699360200
}
```

`CurrentUser` service extract `nameid` claim để lấy UserId.

## 🚀 Migration Database

Sau khi update code, cần tạo migration cho field mới:

```bash
# Tạo migration
cd src/Infrastructure
dotnet ef migrations add AddUpdatedIdUserToProduct -s ../Web

# Apply migration
dotnet ef database update -s ../Web
```

**Migration sẽ tạo:**
- `UpdatedIdUser` column trong `Products` table
- Nullable string field

## 🎯 Benefits

### Security
- ✅ Client không thể fake UserId
- ✅ UserId luôn khớp với user đang login
- ✅ Audit trail chính xác

### Code Quality
- ✅ Clean code: Không có UserId trong request body
- ✅ Separation of concerns
- ✅ Single source of truth (JWT token)

### Developer Experience
- ✅ Không cần remember truyền UserId
- ✅ Frontend code đơn giản hơn
- ✅ Ít bug hơn

## 📖 Pattern cho Entity mới

Nếu bạn tạo entity mới cần track UserId:

### 1. Thêm field vào Entity

```csharp
// Domain/Entities/YourEntity.cs
public class YourEntity : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    
    public string? CreatedIdUser { get; set; }
    public string? UpdatedIdUser { get; set; }
}
```

### 2. Inject IUser vào Command Handler

```csharp
// Application/YourEntity/Commands/CreateYourEntity.cs
public class CreateYourEntityCommandHandler : IRequestHandler<CreateYourEntityCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;  // ← Inject

    public CreateYourEntityCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateYourEntityCommand request, CancellationToken cancellationToken)
    {
        var entity = new YourEntity
        {
            Name = request.Name,
            CreatedIdUser = _user.Id  // ← Set automatically
        };

        _context.YourEntities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
```

### 3. Không cần field trong Command

```csharp
public record CreateYourEntityCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    // KHÔNG CẦN: public string? CreatedIdUser { get; init; }
}
```

## 🔒 Null Check (Optional)

Nếu muốn bắt buộc user phải login:

```csharp
public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
{
    // Check user có login không
    if (string.IsNullOrEmpty(_user.Id))
    {
        throw new UnauthorizedAccessException("User must be logged in");
    }
    
    var entity = new Product
    {
        CreatedIdUser = _user.Id,
        // ...
    };
    
    // ...
}
```

**Lưu ý:** Authorization behavior đã check ở pipeline level, thường không cần check thủ công.

## 📚 Related Files

- **Interface**: `src/Application/Common/Interfaces/IUser.cs`
- **Implementation**: `src/Web/Services/CurrentUser.cs`
- **Registration**: `src/Web/DependencyInjection.cs` (line 17)
- **Product Entity**: `src/Domain/Entities/Product.cs`
- **Category Entity**: `src/Domain/Entities/Category.cs`

## ✅ Summary

| Action | Before | After |
|--------|--------|-------|
| **Create Product** | Phải truyền `createdIdUser` | Tự động từ JWT |
| **Update Product** | Phải truyền `updatedIdUser` | Tự động từ JWT |
| **Create Category** | Phải truyền `createdIdUser` | Tự động từ JWT |
| **Update Category** | Phải truyền `updatedIdUser` | Tự động từ JWT |
| **Security** | Client có thể fake | 100% secure |
| **Code** | Nhiều boilerplate | Clean & simple |

---

**Status:** ✅ Implemented and ready to use!


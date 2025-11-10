# Hướng dẫn Custom Identity System với JWT Authentication

## Tổng quan

Dự án này đã được cập nhật để **không sử dụng ASP.NET Core Identity mặc định** nữa. Thay vào đó, chúng ta sử dụng **Custom Identity entities** và **JWT Bearer Token authentication** để dễ dàng học tập và tìm hiểu sâu về cách hoạt động của hệ thống authentication.

## Cấu trúc Database (PostgreSQL)

### Các bảng được tạo:

#### 1. **Users** - Bảng người dùng
```sql
- Id: int (Primary Key)
- UserName: string(256) - Unique
- Email: string(256) - Unique  
- PasswordHash: string - Mật khẩu đã hash
- PhoneNumber: string(50)
- EmailConfirmed: bool
- PhoneNumberConfirmed: bool
- TwoFactorEnabled: bool
- LockoutEnd: DateTimeOffset?
- LockoutEnabled: bool
- AccessFailedCount: int
- Created: DateTimeOffset
- CreatedBy: string
- LastModified: DateTimeOffset?
- LastModifiedBy: string?
```

#### 2. **Roles** - Bảng vai trò
```sql
- Id: int (Primary Key)
- Name: string(256) - Unique
- Description: string(500)
```

#### 3. **UserRoles** - Bảng many-to-many giữa Users và Roles
```sql
- Id: int (Primary Key)
- UserId: int (Foreign Key → Users)
- RoleId: int (Foreign Key → Roles)
- Unique Index: (UserId, RoleId)
```

#### 4. **UserTokens** - Bảng lưu trữ JWT tokens
```sql
- Id: int (Primary Key)
- UserId: int (Foreign Key → Users)
- Token: string - JWT access token
- RefreshToken: string(500) - Refresh token
- TokenExpiry: DateTimeOffset
- RefreshTokenExpiry: DateTimeOffset?
- IsRevoked: bool
- IpAddress: string(50)
- UserAgent: string(500)
- Created: DateTimeOffset
```

## Cấu hình Database

### PostgreSQL Connection String

File: `appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "CleanArchitectureDb": "Host=localhost;Port=5432;Database=CleanArchitectureDb_Dev;Username=postgres;Password=postgres;"
  }
}
```

**Lưu ý**: Thay đổi `Username` và `Password` phù hợp với PostgreSQL của bạn.

### JWT Settings

```json
{
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForJWTTokenGeneration!",
    "Issuer": "CleanArchitecture",
    "Audience": "CleanArchitecture",
    "ExpiryHours": "24"
  }
}
```

## User mặc định

Khi chạy ứng dụng lần đầu, hệ thống sẽ tự động tạo:

- **Email**: `administrator@localhost`
- **Password**: `Administrator1!`
- **Role**: `Administrator`

## Sử dụng API Authentication

### 1. Đăng ký tài khoản mới

**Endpoint**: `POST /api/identity/register`

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response** (Success):
```json
{
  "message": "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ."
}
```

**Yêu cầu Password**:
- Tối thiểu 6 ký tự
- Có ít nhất 1 chữ HOA
- Có ít nhất 1 chữ thường
- Có ít nhất 1 chữ số
- Có ít nhất 1 ký tự đặc biệt (!?*.@#$%^&+=)

### 2. Đăng nhập

**Endpoint**: `POST /api/identity/login`

**Request Body**:
```json
{
  "email": "administrator@localhost",
  "password": "Administrator1!"
}
```

**Response** (Success):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "xxxxxxxxxxxxxxxxxxx",
  "expiration": "2024-10-23T10:00:00Z",
  "userId": 1,
  "email": "administrator@localhost",
  "userName": "administrator@localhost",
  "roles": ["Administrator"]
}
```

### 3. Sử dụng Token trong Swagger

1. Đăng nhập và copy `token` từ response
2. Click nút **Authorize** 🔓 ở góc trên bên phải Swagger UI
3. Trong popup, chỉ cần paste token vào (Swagger tự động thêm "Bearer " prefix)
4. Click **Authorize**
5. Giờ bạn có thể gọi các API có `[Authorize]` attribute

### 4. Sử dụng Token trong HTTP Client

Thêm header vào request:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Ví dụ với curl**:
```bash
curl -X GET "https://localhost:7000/api/todoitems" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

## Cấu trúc Code

### Domain Layer
- `Domain/Entities/User.cs` - Entity người dùng
- `Domain/Entities/Role.cs` - Entity vai trò  
- `Domain/Entities/UserRole.cs` - Entity many-to-many
- `Domain/Entities/UserToken.cs` - Entity lưu token

### Infrastructure Layer
- `Infrastructure/Data/Configurations/` - EF Core configurations cho các entity
- `Infrastructure/Identity/JwtTokenGenerator.cs` - Generate JWT tokens
- `Infrastructure/Identity/PasswordHasher.cs` - Hash và verify passwords (PBKDF2)
- `Infrastructure/Identity/IdentityService.cs` - Service quản lý user

### Application Layer
- `Application/Identity/Commands/RegisterUser/` - Command đăng ký user
- `Application/Identity/Commands/LoginUser/` - Command đăng nhập

### Web Layer
- `Web/Endpoints/Identity.cs` - API endpoints cho authentication

## Cách hoạt động của JWT Authentication

### 1. Password Hashing (PBKDF2)
```
User nhập password → PBKDF2 + Random Salt → Password Hash lưu vào DB
```

**Verify**:
```
User đăng nhập → Extract Salt từ Hash → Hash password nhập vào với Salt → So sánh
```

### 2. JWT Token Generation

**Claims trong token**:
- `sub` (Subject): User ID
- `email`: Email của user
- `unique_name`: Username
- `jti` (JWT ID): Unique ID của token
- `role`: Roles của user (có thể nhiều roles)

**Token Structure**:
```
Header.Payload.Signature
```

- **Header**: Algorithm (HS256) và Type (JWT)
- **Payload**: Claims (user info, roles, expiry)
- **Signature**: HMAC SHA-256 với Secret Key

### 3. Authentication Flow

```
1. User đăng nhập → Email + Password
2. Server verify password
3. Server generate JWT token + Refresh token
4. Server lưu tokens vào UserTokens table
5. Return tokens cho client
6. Client lưu token (localStorage/cookie)
7. Mỗi request, client gửi: Authorization: Bearer {token}
8. Server validate token và xác thực user
```

## Authorization với Roles

### Gán Role cho User

Thêm record vào bảng `UserRoles`:
```csharp
var userRole = new UserRole
{
    UserId = userId,
    RoleId = roleId
};
_context.UserRoles.Add(userRole);
await _context.SaveChangesAsync();
```

### Sử dụng trong Code

**Check role trong endpoint**:
```csharp
[Authorize(Roles = "Administrator")]
public async Task<IResult> AdminOnlyEndpoint()
{
    // Chỉ Administrator mới access được
}
```

**Check policy**:
```csharp
[Authorize(Policy = Policies.CanPurge)]
public async Task<IResult> PurgeData()
{
    // Chỉ user có policy CanPurge mới access được
}
```

## Debugging và Testing

### View JWT Token Claims

Sử dụng [jwt.io](https://jwt.io) để decode và xem claims trong token.

### Test với Swagger

1. Mở Swagger UI: `https://localhost:{port}/api`
2. Test endpoint `/api/identity/register` để tạo user
3. Test endpoint `/api/identity/login` để lấy token
4. Click **Authorize** và paste token
5. Test các endpoint có authentication

### Xem Database

Kết nối PostgreSQL và xem các bảng:
```sql
SELECT * FROM "Users";
SELECT * FROM "Roles";
SELECT * FROM "UserRoles";
SELECT * FROM "UserTokens";
```

## Bảo mật

### Password Hashing
- Sử dụng **PBKDF2** với SHA-256
- Salt ngẫu nhiên 128-bit cho mỗi password
- 10,000 iterations
- Output hash 256-bit

### JWT Secret Key
**QUAN TRỌNG**: Trong production, đừng hardcode secret key trong appsettings.json!

Sử dụng:
- Azure Key Vault
- Environment Variables
- User Secrets (Development)

### Token Expiry
- Access Token: 24 giờ (có thể thay đổi)
- Refresh Token: 7 ngày
- Tokens được lưu trong DB để có thể revoke khi cần

### HTTPS
Luôn sử dụng HTTPS trong production để bảo vệ token khi truyền tải.

## Mở rộng

### Thêm Refresh Token Endpoint
```csharp
public record RefreshTokenCommand : IRequest<LoginResponse?>
{
    public string RefreshToken { get; init; }
}
```

### Thêm Logout (Revoke Token)
```csharp
var token = await _context.UserTokens
    .FirstOrDefaultAsync(t => t.Token == currentToken);
    
if (token != null)
{
    token.IsRevoked = true;
    await _context.SaveChangesAsync();
}
```

### Two-Factor Authentication
Sử dụng field `TwoFactorEnabled` trong User entity và implement TOTP.

## Tài liệu tham khảo

- [JWT.io - JWT Introduction](https://jwt.io/introduction)
- [PBKDF2 - Wikipedia](https://en.wikipedia.org/wiki/PBKDF2)
- [OWASP Password Storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [Microsoft JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)


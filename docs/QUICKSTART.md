# 🚀 Quick Start Guide

## Tổng quan thay đổi

Dự án này đã được cập nhật với:
- ✅ **PostgreSQL** thay vì SQL Server
- ✅ **Custom Identity System** thay vì ASP.NET Core Identity
- ✅ **JWT Bearer Authentication** với token rõ ràng
- ✅ **Swagger UI với nút Authorize** 🔓

## Bắt đầu nhanh (5 phút)

### 1. Cài đặt PostgreSQL

**Windows**: Download từ https://www.postgresql.org/download/

**Linux**:
```bash
sudo apt install postgresql
```

**macOS**:
```bash
brew install postgresql@15
brew services start postgresql@15
```

### 2. Cập nhật Connection String

Mở `src/Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "CleanArchitectureDb": "Host=localhost;Port=5432;Database=CleanArchitectureDb_Dev;Username=postgres;Password=YOUR_PASSWORD;"
  }
}
```

Thay `YOUR_PASSWORD` bằng password PostgreSQL của bạn.

### 3. Chạy ứng dụng

```bash
cd src/Web
dotnet run
```

### 4. Mở Swagger

```
https://localhost:5001/api
hoặc
https://localhost:7000/api
```

### 5. Test Authentication

#### Đăng nhập với user mặc định

**Endpoint**: `POST /api/identity/login`

```json
{
  "email": "administrator@localhost",
  "password": "Administrator1!"
}
```

#### Copy token từ response

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  ...
}
```

#### Click nút **Authorize** 🔓 trong Swagger

1. Click nút **Authorize** ở góc trên bên phải
2. Paste token vào ô (chỉ cần paste token, không cần gõ "Bearer")
3. Click **Authorize**
4. Click **Close**

#### Test endpoint có authentication

Thử gọi `GET /api/todoitems` - sẽ thành công với status 200!

## Cấu trúc Database

Các bảng được tạo tự động:

### Identity Tables
- **Users** - Người dùng (thay vì AspNetUsers)
- **Roles** - Vai trò (thay vì AspNetRoles)
- **UserRoles** - Liên kết User-Role
- **UserTokens** - Lưu JWT tokens

### Domain Tables
- **TodoLists** - Danh sách todo
- **TodoItems** - Các item todo

## API Endpoints mới

### 🔐 Authentication

#### Đăng ký
```
POST /api/identity/register
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Yêu cầu password**:
- Tối thiểu 6 ký tự
- Có chữ HOA, chữ thường, số, ký tự đặc biệt

#### Đăng nhập
```
POST /api/identity/login
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response**:
```json
{
  "token": "eyJhbGci...",
  "refreshToken": "xxx...",
  "expiration": "2024-10-23T10:00:00Z",
  "userId": 1,
  "email": "user@example.com",
  "userName": "user@example.com",
  "roles": ["Administrator"]
}
```

## Kiểm tra Database

### Sử dụng psql
```bash
psql -U postgres -d CleanArchitectureDb_Dev

# Xem tất cả users
SELECT * FROM "Users";

# Xem tất cả roles
SELECT * FROM "Roles";

# Xem user-role mapping
SELECT u."Email", r."Name" 
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON r."Id" = ur."RoleId";

# Xem tokens
SELECT "UserId", "TokenExpiry", "IsRevoked" FROM "UserTokens";
```

### Sử dụng pgAdmin
1. Connect tới localhost
2. Expand "CleanArchitectureDb_Dev"
3. Expand "Schemas" → "public" → "Tables"
4. Right-click bảng → "View/Edit Data" → "All Rows"

## Tài liệu chi tiết

- 📖 [Custom Identity Guide](docs/CUSTOM_IDENTITY_GUIDE.md) - Giải thích chi tiết về hệ thống authentication
- 📖 [PostgreSQL Setup](docs/POSTGRESQL_SETUP_GUIDE.md) - Hướng dẫn setup PostgreSQL chi tiết
- 📖 [Bearer Token Guide](docs/BEARER_TOKEN_GUIDE.md) - Hướng dẫn về JWT tokens (nếu có)

## Học và Tìm hiểu

### Xem cách Password được hash
```csharp
// File: Infrastructure/Identity/PasswordHasher.cs
public string HashPassword(string password)
{
    // PBKDF2 với SHA-256
    // Salt ngẫu nhiên 128-bit
    // 10,000 iterations
    ...
}
```

### Xem cách JWT Token được tạo
```csharp
// File: Infrastructure/Identity/JwtTokenGenerator.cs
public string GenerateToken(User user, IEnumerable<string> roles)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        ...
    };
    // Token valid 24 hours
    ...
}
```

### Xem cách Authentication hoạt động
```csharp
// File: Application/Identity/Commands/LoginUser/LoginUserCommand.cs
// 1. Tìm user theo email
// 2. Verify password với PasswordHasher
// 3. Lấy roles của user
// 4. Generate JWT token
// 5. Lưu token vào database
// 6. Return token cho client
```

## Troubleshooting

### ❌ Không thấy nút Authorize trong Swagger
- Kiểm tra file `Web/DependencyInjection.cs`
- Đảm bảo có code:
  ```csharp
  configure.AddSecurity("Bearer", ...)
  configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
  ```

### ❌ 401 Unauthorized khi gọi API
- Đảm bảo đã click nút **Authorize** và paste token
- Kiểm tra token chưa hết hạn (24 giờ)
- Kiểm tra format: chỉ paste token, không cần "Bearer " prefix trong Swagger

### ❌ Connection failed to PostgreSQL
- Kiểm tra PostgreSQL service đang chạy
- Kiểm tra username/password trong connection string
- Thử test connection bằng pgAdmin hoặc psql

### ❌ Build errors
```bash
# Clean và rebuild
dotnet clean
dotnet build
```

## Tips & Tricks

### Decode JWT Token
Copy token và paste vào https://jwt.io để xem claims bên trong.

### Test API với curl
```bash
# Login
TOKEN=$(curl -X POST "https://localhost:5001/api/identity/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"administrator@localhost","password":"Administrator1!"}' \
  | jq -r '.token')

# Call API với token
curl -X GET "https://localhost:5001/api/todoitems" \
  -H "Authorization: Bearer $TOKEN"
```

### Xem SQL queries trong console
Connection string đã được cấu hình để log tất cả SQL commands vào console. Xem window terminal khi chạy API!

## Câu hỏi thường gặp

**Q: Tại sao không dùng ASP.NET Core Identity?**
A: Để dễ học và hiểu rõ cách authentication hoạt động. Custom implementation giúp bạn thấy rõ từng bước: hash password, generate token, verify credentials.

**Q: JWT token có an toàn không?**
A: Có, nếu:
- Sử dụng HTTPS trong production
- Secret key đủ mạnh (32+ characters)
- Token có thời gian hết hạn hợp lý
- Lưu token an toàn trên client

**Q: Refresh token dùng để làm gì?**
A: Khi access token hết hạn, dùng refresh token để lấy access token mới mà không cần đăng nhập lại.

**Q: Làm sao để revoke (thu hồi) token?**
A: Set `IsRevoked = true` trong bảng `UserTokens`. Cần implement middleware để check trước khi validate token.

## Tiếp theo

Sau khi đã chạy được project:

1. ✅ Đọc [CUSTOM_IDENTITY_GUIDE.md](docs/CUSTOM_IDENTITY_GUIDE.md) để hiểu sâu về authentication
2. ✅ Thử tạo thêm roles và users mới
3. ✅ Implement refresh token endpoint
4. ✅ Implement logout (revoke token)
5. ✅ Thử thêm custom claims vào JWT token
6. ✅ Implement Two-Factor Authentication (2FA)

Happy coding! 🎉


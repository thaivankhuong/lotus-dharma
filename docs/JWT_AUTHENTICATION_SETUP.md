# JWT Authentication Setup - Hướng dẫn Chi Tiết

## 📋 Tình Trạng Hiện Tại

✅ **Đã hoàn toàn cấu hình:**
- JWT Bearer token authentication sử dụng HS256
- tất cả các API endpoints (TodoItems, TodoLists, WeatherForecasts) đều bắt buộc token
- Swagger UI đã hiển thị nút **Authorize** 🔓 để nhập token
- Identity endpoints (Register/Login) cho phép anonymous

---

## 🚀 Cách Sử Dụng

### Bước 1: Đăng Ký Tài Khoản (Register)

Truy cập **POST /api/identity/register** trong Swagger UI:

```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response (Nếu thành công):**
```json
{
  "message": "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ."
}
```

---

### Bước 2: Đăng Nhập (Login) để Lấy Token

Truy cập **POST /api/identity/login** trong Swagger UI:

```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response (Nếu thành công):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "xxxxxxx...",
  "expiration": "2024-10-24T10:00:00Z"
}
```

**⚠️ Quan trọng:** Sao chép giá trị của `token`

---

### Bước 3: Thêm Token vào Swagger UI

1. Nhấp vào nút **Authorize** 🔓 ở góc trên bên phải của Swagger UI
2. Dán token vào ô text trong format:
   ```
   Bearer {your_access_token}
   ```
   **Ví dụ:**
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
3. Nhấp **Authorize** để xác thực
4. Nhấp **Close** để đóng dialog

---

### Bước 4: Gọi API Có Authentication

Giờ bạn có thể gọi các endpoint cần authentication:
- ✅ **GET /api/todolists** - Lấy danh sách
- ✅ **POST /api/todolists** - Tạo mới
- ✅ **PUT /api/todolists/{id}** - Cập nhật
- ✅ **DELETE /api/todolists/{id}** - Xóa
- ✅ **GET /api/todoitems** - Lấy danh sách item
- ✅ **GET /api/weatherforecasts** - Lấy dự báo thời tiết

---

## 🔧 Cấu Hình Chi Tiết

### JWT Settings (`appsettings.json`)
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

### Authentication Configuration (`src/Infrastructure/DependencyInjection.cs`)
- **Scheme:** JWT Bearer (HS256)
- **Issuer:** CleanArchitecture
- **Audience:** CleanArchitecture
- **Expiry:** 24 giờ
- **Validation:** Đầy đủ (signature, issuer, audience, lifetime)

---

## 📚 Cấu Trúc Authorization

### 1. Endpoints Yêu Cầu Authorization

```csharp
[Authorize]  // ← Chỉ định yêu cầu authentication
public class TodoItems : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTodoItems).RequireAuthorization();
        // ... các endpoints khác
    }
}
```

### 2. Endpoints Cho Phép Anonymous

```csharp
public class Identity : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register").AllowAnonymous();  // ← Public
        groupBuilder.MapPost(Login, "login").AllowAnonymous();         // ← Public
    }
}
```

---

## 🔐 MediatR Pipeline Security Flow

Khi một request được gửi, nó sẽ đi qua các middleware:

1. **Authentication Middleware** → Validate JWT token
2. **Authorization Middleware** → Kiểm tra [Authorize] attribute
3. **MediatR Pipeline:**
   - LoggingBehaviour → Log request
   - UnhandledExceptionBehaviour → Catch exceptions
   - **AuthorizationBehaviour** → Kiểm tra [Authorize] trên Command/Query
   - ValidationBehaviour → Validate data
   - PerformanceBehaviour → Log slow requests

---

## 🛠️ Test với cURL

```bash
# 1. Đăng ký
curl -X POST "https://localhost:5001/api/identity/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# 2. Đăng nhập
curl -X POST "https://localhost:5001/api/identity/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# 3. Gọi API với token
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
curl -X GET "https://localhost:5001/api/todolists" \
  -H "Authorization: Bearer $TOKEN"
```

---

## ⚠️ Lỗi Thường Gặp

### 401 Unauthorized
**Nguyên nhân:** Token không được gửi hoặc không hợp lệ

**Giải pháp:**
- Kiểm tra token đã hết hạn chưa (mặc định 24 giờ)
- Kiểm tra format: `Bearer {token}` (có khoảng trắng)
- Đăng nhập lại để lấy token mới

### 403 Forbidden
**Nguyên nhân:** User không có quyền cần thiết

**Giải pháp:**
- Kiểm tra quyền/roles của user
- Nếu endpoint yêu cầu role Admin, đảm bảo user có role đó

---

## 🔄 Refresh Token (Tùy Chọn)

Hiện tại chỉ có login endpoint. Để thêm refresh token flow:

```csharp
// POST /api/identity/refresh
public record RefreshTokenCommand(string Token, string RefreshToken) : IRequest<LoginResult>;
```

---

## 📖 Tài Liệu Liên Quan

- [BEARER_TOKEN_GUIDE.md](./BEARER_TOKEN_GUIDE.md)
- [IDENTITY_ENDPOINTS_GUIDE.md](./IDENTITY_ENDPOINTS_GUIDE.md)
- [CUSTOM_IDENTITY_GUIDE.md](./CUSTOM_IDENTITY_GUIDE.md)

---

**Cập nhật:** Tháng 10, 2024


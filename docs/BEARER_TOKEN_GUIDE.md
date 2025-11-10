# HƯỚNG DẪN SỬ DỤNG BEARER TOKEN AUTHENTICATION

## ✅ ĐÃ ENABLE

Bearer Token authentication đã được kích hoạt trong project này!

---

## 🔐 CÁC API ENDPOINT MỚI

Sau khi enable Bearer Token, bạn có các endpoints sau:

### **1. Register (Đăng ký user mới)**
```http
POST /api/users/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "YourPassword123!"
}
```

**Response 200 OK:**
```json
{
  // Empty response if successful
}
```

---

### **2. Login (Đăng nhập)**
```http
POST /api/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "YourPassword123!"
}
```

**Response 200 OK:**
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "CfDJ8..."
}
```

---

### **3. Refresh Token**
```http
POST /api/users/refresh
Content-Type: application/json

{
  "refreshToken": "CfDJ8..."
}
```

---

### **4. Manage Info (Lấy thông tin user)**
```http
GET /api/users/manage/info
Authorization: Bearer {accessToken}
```

---

## 🚀 CÁCH SỬ DỤNG TRONG SWAGGER

### **Bước 1: Chạy ứng dụng**
```bash
cd src/Web
dotnet run
```

### **Bước 2: Mở Swagger**
```
http://localhost:5000/api
```

### **Bước 3: Register user mới**

1. Tìm endpoint **POST /api/users/register** (trong section "Users")
2. Click "Try it out"
3. Nhập:
   ```json
   {
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
4. Click "Execute"
5. Kiểm tra Response: 200 OK

### **Bước 4: Login để lấy token**

1. Tìm endpoint **POST /api/users/login** (trong section "Users")
2. Click "Try it out"
3. Nhập email/password vừa đăng ký
4. Click "Execute"
5. **Copy** `accessToken` từ response

### **Bước 5: Authorize trong Swagger**

1. Click nút **"Authorize"** (hoặc icon ổ khóa 🔒) ở góc phải trên
2. Trong popup, nhập:
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
   ⚠️ **CHÚ Ý**: Phải có chữ "Bearer " (có dấu cách) trước token!
   
3. Click "Authorize"
4. Click "Close"

### **Bước 6: Test API có Authorization**

1. Tìm endpoint **GET /api/todoitems** (có icon khóa 🔒)
2. Click "Try it out"
3. Click "Execute"
4. **Thành công!** Bạn sẽ thấy data, không còn lỗi 401

---

## 🐛 DEBUG AUTHORIZATION FLOW

### **Đặt Breakpoint để hiểu flow:**

#### **1. Ở Endpoint (Web Layer)**
**File: `src/Web/Endpoints/TodoItems.cs`, line 24**
```csharp
public async Task<Ok<PaginatedList<TodoItemBriefDto>>> GetTodoItemsWithPagination(...)
{
    var result = await sender.Send(query);  // ← Đặt breakpoint
    return TypedResults.Ok(result);
}
```

#### **2. Ở AuthorizationBehaviour (Application Layer)**
**File: `src/Application/Common/Behaviours/AuthorizationBehaviour.cs`, line 29**
```csharp
// Must be authenticated user
if (_user.Id == null)  // ← Đặt breakpoint
{
    throw new UnauthorizedAccessException();
}
```

#### **3. Run Debug (F5) và test:**

1. Gọi API **KHÔNG CÓ token** → Breakpoint ở line 29, `_user.Id` = null → Exception
2. Gọi API **CÓ token** → Breakpoint ở line 29, `_user.Id` = "user-id-string" → Passed!

---

## 📊 AUTHORIZATION FLOW CHI TIẾT

```
┌──────────────────────────────────────────────────────────┐
│ 1. Client gửi request                                     │
│    Authorization: Bearer eyJhbGc...                       │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│ 2. ASP.NET Core Authentication Middleware                │
│    - Parse Bearer token từ header                        │
│    - Validate token (signature, expiry)                  │
│    - Tạo ClaimsPrincipal với user info                   │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│ 3. Endpoint với .RequireAuthorization()                  │
│    - Check user có authenticated không?                  │
│    - Nếu không → 401 Unauthorized (DỪNG)                 │
│    - Nếu có → Tiếp tục                                   │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│ 4. MediatR Pipeline                                       │
│    LoggingBehaviour → UnhandledExceptionBehaviour        │
│    → AuthorizationBehaviour ← BREAKPOINT Ở ĐÂY!         │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│ 5. AuthorizationBehaviour                                 │
│    - Check [Authorize] attributes trên Query/Command     │
│    - Check roles nếu có                                  │
│    - Check policies nếu có                               │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│ 6. Handler Execution                                      │
│    - Business logic                                      │
│    - Database query                                      │
└──────────────────────────────────────────────────────────┘
```

---

## 🔍 KIỂM TRA USER INFO TRONG CODE

### **Trong Handler, có thể access user info:**

```csharp
public class SomeCommandHandler : IRequestHandler<SomeCommand, int>
{
    private readonly IUser _user;  // ← Inject IUser

    public SomeCommandHandler(IUser user)
    {
        _user = user;
    }

    public async Task<int> Handle(SomeCommand request, CancellationToken ct)
    {
        // Lấy user ID
        var userId = _user.Id;  // ← User ID của người đang login
        
        // Lấy user email
        var email = _user.Email;
        
        // Check roles
        var isAdmin = _user.Roles?.Contains("Administrator") ?? false;
        
        // Business logic...
    }
}
```

---

## 🎯 TEST AUTHORIZATION SCENARIOS

### **Scenario 1: API Yêu Cầu Login**

**Endpoint:**
```csharp
groupBuilder.MapGet(GetTodoItems).RequireAuthorization();
```

**Test:**
1. ❌ Gọi KHÔNG CÓ token → 401 Unauthorized
2. ✅ Gọi CÓ token → 200 OK

---

### **Scenario 2: Query Có [Authorize] Attribute**

**Query:**
```csharp
[Authorize]
public record GetSomethingQuery : IRequest<Something>;
```

**Flow:**
1. Request đến Endpoint (qua .RequireAuthorization())
2. MediatR Pipeline chạy AuthorizationBehaviour
3. Check `[Authorize]` attribute
4. Validate user authenticated

**Breakpoint:** `AuthorizationBehaviour.cs` line 29

---

### **Scenario 3: Role-Based Authorization**

**Query:**
```csharp
[Authorize(Roles = "Administrator")]
public record PurgeTodoListsCommand : IRequest;
```

**Flow:**
1. Check user authenticated
2. Check user có role "Administrator" không?
3. Nếu không → 403 Forbidden

**Breakpoint:** `AuthorizationBehaviour.cs` line 45

---

## 🔑 ADMIN USER CÓ SẴN

Database seed sẵn admin user:

- **Email:** `administrator@localhost`
- **Password:** `Administrator1!`
- **Role:** Administrator

**Để test với admin:**
1. Login với credentials trên
2. Lấy token
3. Test API yêu cầu Administrator role (như PurgeTodoLists)

---

## 💡 TIPS

### **1. Token Expiry**
- Token hết hạn sau 3600 seconds (1 hour)
- Dùng `/refresh` endpoint để lấy token mới

### **2. Không cần gõ "Bearer " trong code**
- Swagger UI tự động thêm
- Nhưng nếu test bằng Postman/curl, phải thêm thủ công

### **3. Debug Tips**
```csharp
// Trong AuthorizationBehaviour, line 29
if (_user.Id == null)  // ← Đặt breakpoint ở đây
{
    // Watch _user để xem user info
    // _user.Id = null → Chưa login
    // _user.Id = "xxx" → Đã login
}
```

### **4. Common Errors**

| Lỗi | Nguyên nhân | Giải pháp |
|-----|-------------|-----------|
| 401 Unauthorized | Không có token hoặc token invalid | Login lại, lấy token mới |
| 403 Forbidden | Không đủ quyền (role) | Check user role |
| Token expired | Token hết hạn | Dùng /refresh để lấy token mới |

---

## 📚 TÀI LIỆU THAM KHẢO

- **ASP.NET Core Identity API:** https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization
- **Bearer Token:** https://jwt.io/
- **Authorization Best Practices:** https://learn.microsoft.com/en-us/aspnet/core/security/authorization/

---

## 🎉 TÓM TẮT

✅ **Đã có:**
- POST /register - Đăng ký user
- POST /login - Lấy bearer token
- Authorization button trong Swagger
- Debug flow với breakpoint

✅ **Cách test:**
1. Register user
2. Login → Copy token
3. Authorize trong Swagger (nút 🔒)
4. Test API có authorization
5. Debug với breakpoint ở AuthorizationBehaviour

✅ **Hiểu flow:**
- Request → Authentication Middleware → Endpoint Authorization → MediatR Pipeline → AuthorizationBehaviour → Handler

---

**Giờ bạn có thể test và hiểu toàn bộ authorization flow!** 🚀


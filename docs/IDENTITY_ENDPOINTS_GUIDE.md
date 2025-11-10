# HƯỚNG DẪN SỬ DỤNG IDENTITY ENDPOINTS

## ⚠️ LƯU Ý QUAN TRỌNG

Identity endpoints **KHÔNG HIỂN THỊ TRONG SWAGGER UI** nhưng **HOẠT ĐỘNG BÌNHạ**!

**Nguyên nhân:** `MapIdentityApi<>()` không được NSwag document generator nhận diện.

**Giải pháp:** Test bằng Postman, curl, hoặc Browser Developer Tools.

---

## 📝 HƯỚNG DẪN CÓ SẴN TRONG SWAGGER

Khi bạn mở `http://localhost:5000/api`, ngay đầu trang sẽ có hướng dẫn:

- ✅ POST `/api/users/register` - Register user
- ✅ POST `/api/users/login` - Login, lấy token
- ✅ POST `/api/users/refresh` - Refresh token
- ✅ GET `/api/users/manage/info` - User info (cần auth)

---

## 🚀 TEST BẰNG POSTMAN (DỄ NHẤT)

### **Bước 1: Register User**

```
POST http://localhost:5000/api/users/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}
```

**Response:** 200 OK

---

### **Bước 2: Login**

```
POST http://localhost:5000/api/users/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}
```

**Response:**
```json
{
  "tokenType": "Bearer",
  "accessToken": "CfDJ8OEM7qN...",
  "expiresIn": 3600,
  "refreshToken": "..."
}
```

**📝 Copy `accessToken`!**

---

### **Bước 3: Authorize trong Swagger**

1. Mở Swagger UI: `http://localhost:5000/api`
2. Click nút **"Authorize"** 🔒 (góc phải trên)
3. Nhập: `Bearer {token_vừa_copy}`
4. Click "Authorize"
5. Click "Close"

---

### **Bước 4: Test API có Authorization**

Bây giờ gọi bất kỳ API nào có icon khóa 🔒:

```
GET /api/todoitems?listId=1
```

**Thành công!** ✅ Trả về data, không còn 401.

---

## 💻 TEST BẰNG POWERSHELL

### **1. Register:**
```powershell
$registerBody = @{
    email = "test@example.com"
    password = "Test123!"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/users/register" `
  -Method Post `
  -Body $registerBody `
  -ContentType "application/json"
```

### **2. Login & Lưu Token:**
```powershell
$loginBody = @{
    email = "test@example.com"
    password = "Test123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/users/login" `
  -Method Post `
  -Body $loginBody `
  -ContentType "application/json"

$token = $response.accessToken
Write-Host "Token: $token"
```

### **3. Test Protected API:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/todoitems?listId=1" `
  -Method Get `
  -Headers @{Authorization="Bearer $token"}
```

---

## 🌐 TEST BẰNG BROWSER CONSOLE (F12)

### **1. Register:**
```javascript
fetch('http://localhost:5000/api/users/register', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({
    email: 'test@example.com',
    password: 'Test123!'
  })
}).then(r => console.log('Status:', r.status));
```

### **2. Login & Save Token:**
```javascript
fetch('http://localhost:5000/api/users/login', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({
    email: 'test@example.com',
    password: 'Test123!'
  })
})
.then(r => r.json())
.then(data => {
  window.token = data.accessToken;
  console.log('Token saved!', data.accessToken);
});
```

### **3. Test API với Token:**
```javascript
fetch('http://localhost:5000/api/todoitems?listId=1', {
  headers: {
    'Authorization': `Bearer ${window.token}`
  }
})
.then(r => r.json())
.then(data => console.log('Data:', data));
```

---

## 🐛 DEBUG AUTHORIZATION FLOW

### **Bước 1: Đặt Breakpoint**

**File:** `src/Application/Common/Behaviours/AuthorizationBehaviour.cs`

**Line 29:**
```csharp
if (_user.Id == null)  // ← Đặt breakpoint ở đây
{
    throw new UnauthorizedAccessException();
}
```

### **Bước 2: Run Debug (F5)**

### **Bước 3: Test với Postman**

1. **Gọi API KHÔNG CÓ token:**
   - `GET /api/todoitems?listId=1`
   - Breakpoint hit
   - `_user.Id` = `null`
   - Exception → 401

2. **Gọi API CÓ token:**
   - Add header: `Authorization: Bearer {token}`
   - Breakpoint hit
   - `_user.Id` = `"user-id-string"` ✅
   - Passed!

---

## 📊 TẤT CẢ IDENTITY ENDPOINTS

`MapIdentityApi<>()` tự động tạo các endpoints sau:

### **Authentication:**
- `POST /api/users/register` - Đăng ký user mới
- `POST /api/users/login` - Login, lấy access token
- `POST /api/users/refresh` - Refresh token
- `POST /api/users/confirmEmail` - Confirm email
- `POST /api/users/resendConfirmationEmail` - Resend confirmation
- `POST /api/users/forgotPassword` - Forgot password
- `POST /api/users/resetPassword` - Reset password

### **Account Management:**
- `GET /api/users/manage/info` - Lấy user info (cần auth)
- `POST /api/users/manage/info` - Update user info (cần auth)
- `POST /api/users/manage/2fa` - Enable 2FA (cần auth)

Tất cả đều hoạt động, chỉ không hiển thị trong Swagger!

---

## ✅ USER CÓ SẴN

Database seed sẵn admin user:

- **Email:** `administrator@localhost`
- **Password:** `Administrator1!`
- **Role:** Administrator

**Test ngay:**
```json
POST /api/users/login
{
  "email": "administrator@localhost",
  "password": "Administrator1!"
}
```

---

## 🎯 WORKFLOW HOÀN CHỈNH

1. **Register user** (POST /api/users/register)
2. **Login** (POST /api/users/login) → Lấy token
3. **Authorize trong Swagger** (nút 🔒) → Paste token
4. **Test protected APIs** → Thành công!
5. **Debug** → Đặt breakpoint ở AuthorizationBehaviour
6. **Hiểu flow** → Token → Authentication → Authorization → Handler

---

## 💡 TIPS

### **1. Token Expiry**
Token hết hạn sau 3600 giây (1 giờ). Dùng `/refresh` để lấy token mới.

### **2. Copy Token đúng cách**
Phải có "Bearer " (có dấu cách) trước token:
```
Bearer CfDJ8OEM7qN...
```

### **3. Test nhiều scenarios**
- ✅ No token → 401
- ✅ Invalid token → 401
- ✅ Valid token → 200 OK
- ✅ Expired token → 401

### **4. Xem user info trong code**
```csharp
public class MyHandler : IRequestHandler<MyCommand, int>
{
    private readonly IUser _user;

    public async Task<int> Handle(...)
    {
        var userId = _user.Id;  // User ID
        var email = _user.Email;  // Email
        var roles = _user.Roles;  // Roles
    }
}
```

---

## ❓ FAQ

### **Q: Tại sao không thấy trong Swagger?**
**A:** NSwag không document `MapIdentityApi<>()` tự động. Nhưng endpoints vẫn hoạt động 100%.

### **Q: Có cách nào hiển thị trong Swagger không?**
**A:** Có, nhưng phức tạp:
1. Tạo custom endpoints thủ công
2. Gọi Identity services bên trong
3. Hoặc dùng Swashbuckle thay vì NSwag

Nhưng không cần thiết - test bằng Postman đơn giản hơn!

### **Q: Token có an toàn không?**
**A:** Có! `MapIdentityApi<>()` dùng ASP.NET Core Identity Bearer token, đã được Microsoft implement an toàn.

### **Q: Làm sao biết token còn hạn?**
**A:** Check `expiresIn` field trong login response (giây).

---

## 🎉 TÓM TẮT

✅ **Identity endpoints hoạt động hoàn hảo**
✅ **Chỉ không hiển thị trong Swagger UI**
✅ **Test bằng Postman/curl/Browser Console**
✅ **Token authentication hoạt động đúng**
✅ **Debug flow với breakpoint**
✅ **Hiểu Authorization Behaviour**

---

**Bây giờ bạn có thể test authorization đầy đủ!** 🚀



# Hướng dẫn Setup PostgreSQL cho dự án

## Yêu cầu

- PostgreSQL 12+ đã được cài đặt
- pgAdmin hoặc PostgreSQL command line tools

## Bước 1: Cài đặt PostgreSQL

### Windows
1. Download PostgreSQL từ: https://www.postgresql.org/download/windows/
2. Chạy installer và làm theo hướng dẫn
3. Ghi nhớ password cho user `postgres`

### Linux (Ubuntu/Debian)
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
```

### macOS
```bash
brew install postgresql@15
brew services start postgresql@15
```

## Bước 2: Tạo Database

### Cách 1: Sử dụng pgAdmin
1. Mở pgAdmin
2. Connect tới PostgreSQL server
3. Right-click "Databases" → "Create" → "Database"
4. Nhập tên: `CleanArchitectureDb_Dev`
5. Click Save

### Cách 2: Sử dụng Command Line (psql)
```bash
# Đăng nhập vào PostgreSQL
psql -U postgres

# Tạo database
CREATE DATABASE "CleanArchitectureDb_Dev";

# Thoát
\q
```

## Bước 3: Cập nhật Connection String

Mở file `src/Web/appsettings.Development.json` và cập nhật:

```json
{
  "ConnectionStrings": {
    "CleanArchitectureDb": "Host=localhost;Port=5432;Database=CleanArchitectureDb_Dev;Username=postgres;Password=YOUR_PASSWORD_HERE;"
  }
}
```

Thay `YOUR_PASSWORD_HERE` bằng password PostgreSQL của bạn.

## Bước 4: Chạy ứng dụng

```bash
cd src/Web
dotnet run
```

Ứng dụng sẽ tự động:
1. Xóa database cũ (nếu có)
2. Tạo database mới
3. Tạo tất cả các bảng
4. Seed dữ liệu mẫu

## Bước 5: Kiểm tra Database

### Sử dụng pgAdmin
1. Refresh "CleanArchitectureDb_Dev" database
2. Expand "Schemas" → "public" → "Tables"
3. Bạn sẽ thấy các bảng:
   - Users
   - Roles
   - UserRoles
   - UserTokens
   - TodoLists
   - TodoItems

### Sử dụng psql
```bash
psql -U postgres -d CleanArchitectureDb_Dev

# Liệt kê tất cả tables
\dt

# Xem dữ liệu users
SELECT * FROM "Users";

# Xem dữ liệu roles
SELECT * FROM "Roles";

# Thoát
\q
```

## Bước 6: Test Authentication

### Mở Swagger
```
https://localhost:{port}/api
```

### Test Login với user mặc định
**Endpoint**: `POST /api/identity/login`

**Request**:
```json
{
  "email": "administrator@localhost",
  "password": "Administrator1!"
}
```

**Expected Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiration": "2024-10-23T...",
  "userId": 1,
  "email": "administrator@localhost",
  "userName": "administrator@localhost",
  "roles": ["Administrator"]
}
```

## Troubleshooting

### Lỗi: "Connection refused"
- Kiểm tra PostgreSQL service đang chạy:
  ```bash
  # Windows
  services.msc → Tìm "postgresql"
  
  # Linux
  sudo systemctl status postgresql
  
  # macOS
  brew services list
  ```

### Lỗi: "password authentication failed"
- Kiểm tra lại username và password trong connection string
- Default username thường là `postgres`

### Lỗi: "database does not exist"
- Tạo database theo hướng dẫn Bước 2

### Lỗi: "role 'postgres' does not exist"
- Tạo user postgres:
  ```sql
  CREATE USER postgres WITH PASSWORD 'postgres';
  ALTER USER postgres WITH SUPERUSER;
  ```

## Production Notes

### Không nên dùng EnsureCreated trong Production

File `ApplicationDbContextInitialiser.cs` hiện tại:
```csharp
await _context.Database.EnsureDeletedAsync(); // ⚠️ XÓA DB!
await _context.Database.EnsureCreatedAsync();
```

**Trong Production**, sử dụng EF Core Migrations:

```bash
# Tạo migration
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Web

# Apply migration
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

### Connection String Security

**Không hardcode password trong appsettings.json cho Production!**

Sử dụng:
- Azure Key Vault
- Environment Variables
- User Secrets (Development only)

**Ví dụ với Environment Variable**:
```bash
# Linux/macOS
export ConnectionStrings__CleanArchitectureDb="Host=...;Password=xxx"

# Windows PowerShell
$env:ConnectionStrings__CleanArchitectureDb="Host=...;Password=xxx"
```

## Tools hữu ích

### pgAdmin 4
- GUI tool mạnh mẽ cho PostgreSQL
- Download: https://www.pgadmin.org/download/

### DBeaver
- Universal database tool
- Download: https://dbeaver.io/download/

### Azure Data Studio
- Cross-platform database tool
- Download: https://docs.microsoft.com/en-us/sql/azure-data-studio/download

## Tham khảo thêm

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql - .NET PostgreSQL Provider](https://www.npgsql.org/doc/)
- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/)


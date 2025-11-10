# 📖 VÍ DỤ QUERY JOIN PRODUCT-CATEGORY

## 🎯 Cách 1: Dùng Navigation Property + Select (KHUYÊN DÙNG)

```csharp
// File: GetProductsWithCategory.cs đã có sẵn
var products = await _context.Products
    .AsNoTracking()
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .Take(100)
    .Select(p => new ProductWithCategoryDto
    {
        Id = p.Id,
        Name = p.Name,
        CategoryId = p.CategoryId,
        CategoryName = p.Category.Name,  // ← Dùng Navigation Property
        CategoryDescription = p.Category.Description
    })
    .ToListAsync();

// SQL Generated (TỐT):
// SELECT p."Id", p."Name", p."CategoryId", c."Name", c."Description"
// FROM "Products" p
// INNER JOIN "Categories" c ON p."CategoryId" = c."Id"
// WHERE p."IsActive" = TRUE
// ORDER BY p."Name"
// LIMIT 100
```

## 🎯 Cách 2: Manual Join (Không có Navigation Property)

```csharp
var products = await _context.Products
    .AsNoTracking()
    .Where(p => p.IsActive)
    .Join(_context.Categories,
        p => p.CategoryId,
        c => c.Id,
        (p, c) => new ProductWithCategoryDto
        {
            Id = p.Id,
            Name = p.Name,
            CategoryId = p.CategoryId,
            CategoryName = c.Name
        })
    .OrderBy(p => p.Name)
    .Take(100)
    .ToListAsync();

// SQL Generated (TỐT):
// SELECT p."Id", p."Name", p."CategoryId", c."Name"
// FROM "Products" p
// INNER JOIN "Categories" c ON p."CategoryId" = c."Id"
// WHERE p."IsActive" = TRUE
// ORDER BY p."Name"
// LIMIT 100
```

## 🎯 Cách 3: Include (OK nhưng dư thừa data)

```csharp
var products = await _context.Products
    .AsNoTracking()
    .Include(p => p.Category)  // ← Include toàn bộ Category
    .Where(p => p.IsActive)
    .Take(100)
    .ToListAsync();
    
// Sau đó map thủ công
var dtos = products.Select(p => new 
{
    p.Id,
    p.Name,
    CategoryName = p.Category.Name
}).ToList();

// SQL Generated (DƯ THỪA):
// SELECT p.*, c.*  -- ← Lấy TẤT CẢ columns
// FROM "Products" p
// LEFT JOIN "Categories" c ON p."CategoryId" = c."Id"
// WHERE p."IsActive" = TRUE
// LIMIT 100
```

## 🎯 Cách 4: GroupJoin (cho aggregation)

```csharp
var report = await _context.Categories
    .AsNoTracking()
    .GroupJoin(_context.Products,
        c => c.Id,
        p => p.CategoryId,
        (c, products) => new
        {
            CategoryName = c.Name,
            ProductCount = products.Count(),
            TotalValue = products.Sum(p => p.Price * p.Stock)
        })
    .ToListAsync();

// SQL Generated:
// SELECT c."Name", COUNT(p."Id"), SUM(p."Price" * p."Stock")
// FROM "Categories" c
// LEFT JOIN "Products" p ON c."Id" = p."CategoryId"
// GROUP BY c."Name"
```

## 🎯 Cách 5: WHERE trên joined table

```csharp
var products = await _context.Products
    .AsNoTracking()
    .Where(p => p.IsActive && p.Category.IsActive)  // ← Filter cả 2 tables
    .Select(p => new ProductWithCategoryDto
    {
        Id = p.Id,
        Name = p.Name,
        CategoryName = p.Category.Name
    })
    .Take(100)
    .ToListAsync();

// SQL Generated:
// SELECT p."Id", p."Name", c."Name"
// FROM "Products" p
// INNER JOIN "Categories" c ON p."CategoryId" = c."Id"
// WHERE p."IsActive" = TRUE AND c."IsActive" = TRUE
// LIMIT 100
```

## 📊 SO SÁNH PERFORMANCE

| Cách | SQL Quality | Memory | Khuyên dùng |
|------|------------|--------|-------------|
| Navigation + Select | ⭐⭐⭐⭐⭐ | Low | ✅ TỐT NHẤT |
| Manual Join | ⭐⭐⭐⭐⭐ | Low | ✅ TỐT |
| Include | ⭐⭐⭐ | Medium | ⚠️ OK |
| GroupJoin | ⭐⭐⭐⭐⭐ | Low | ✅ Cho aggregation |

## 🚀 API ENDPOINTS ĐÃ TẠO

### Category CRUD
- `GET /api/categories` - Lấy tất cả categories
- `GET /api/categories/{id}` - Lấy category theo ID
- `POST /api/categories` - Tạo category mới
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Xóa category

### Product with Category
- `GET /api/products/with-category` - Lấy products với category info
- Query parameters:
  - `categoryId` (optional): Filter theo category
  - `pageNumber` (default: 1): Số trang
  - `pageSize` (default: 20): Số items/trang

## 📝 USAGE EXAMPLES

### 1. Tạo Category
```json
POST /api/categories
{
  "name": "Electronics",
  "description": "Electronic devices",
  "createdIdUser": "user-123"
}

Response: 1 (Category ID)
```

### 2. Tạo Product với CategoryId
```json
POST /api/products
{
  "name": "iPhone 15 Pro",
  "description": "Latest iPhone",
  "price": 999.99,
  "stock": 50,
  "categoryId": 1,
  "createdIdUser": "user-123"
}

Response: 1 (Product ID)
```

### 3. Get Products với Category info
```http
GET /api/products/with-category?categoryId=1&pageNumber=1&pageSize=20

Response:
[
  {
    "id": 1,
    "name": "iPhone 15 Pro",
    "description": "Latest iPhone",
    "price": 999.99,
    "stock": 50,
    "categoryId": 1,
    "categoryName": "Electronics",
    "categoryDescription": "Electronic devices",
    "isActive": true,
    "created": "2024-11-06T10:00:00Z",
    "lastModified": "2024-11-06T10:00:00Z"
  }
]
```

## ⚡ PERFORMANCE TIPS

1. **Luôn dùng AsNoTracking() cho read-only queries**
```csharp
.AsNoTracking()  // ← Giảm 50% memory
```

2. **Pagination bắt buộc với data lớn**
```csharp
.Skip((pageNumber - 1) * pageSize)
.Take(pageSize)
```

3. **Select chỉ cần thiết, không Include**
```csharp
.Select(p => new { p.Id, p.Name, CategoryName = p.Category.Name })
// Thay vì
.Include(p => p.Category)
```

4. **Filter trước, Join sau**
```csharp
.Where(p => p.IsActive)  // Filter trước
.Select(p => new { ..., CategoryName = p.Category.Name })  // Join sau
```

## 🗄️ DATABASE SCHEMA

```sql
-- Categories table
CREATE TABLE "Categories" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "CreatedIdUser" VARCHAR(450),
    "UpdatedIdUser" VARCHAR(450),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Created" TIMESTAMP NOT NULL,
    "CreatedBy" VARCHAR(450),
    "LastModified" TIMESTAMP NOT NULL,
    "LastModifiedBy" VARCHAR(450)
);

-- Products table with FK
CREATE TABLE "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(1000),
    "Price" DECIMAL(18,2) NOT NULL,
    "Stock" INT NOT NULL,
    "CategoryId" INT NOT NULL,
    "CreatedIdUser" VARCHAR(450),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Created" TIMESTAMP NOT NULL,
    "CreatedBy" VARCHAR(450),
    "LastModified" TIMESTAMP NOT NULL,
    "LastModifiedBy" VARCHAR(450),
    
    CONSTRAINT "FK_Products_Categories" 
        FOREIGN KEY ("CategoryId") 
        REFERENCES "Categories"("Id") 
        ON DELETE RESTRICT
);

-- Index for performance
CREATE INDEX "IX_Products_CategoryId" ON "Products"("CategoryId");
CREATE INDEX "IX_Products_IsActive_CategoryId" ON "Products"("IsActive", "CategoryId");
```


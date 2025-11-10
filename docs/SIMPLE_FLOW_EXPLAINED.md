# HIỂU CLEAN ARCHITECTURE BẰNG VÍ DỤ ĐơN GIẢN

## 🎯 Mục Tiêu: Hiểu Request Flow qua 1 Ví Dụ Cụ Thể

Chúng ta sẽ trace request: **GET /api/todoitems?pageNumber=1&pageSize=10**

---

## 📦 4 LAYERS - HIỂU ĐƠN GIẢN

### Tưởng tượng bạn đang xây một nhà:

```
┌────────────────────────────────────────────────────────┐
│  4. WEB (Cửa ra vào)                                    │
│     - Người dùng gõ cửa ở đây                          │
│     - API Endpoints                                    │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│  3. APPLICATION (Công nhân điều hành)                  │
│     - Nhận yêu cầu và xử lý                           │
│     - Commands & Queries (CQRS)                       │
│     - Validation, Authorization                        │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│  2. INFRASTRUCTURE (Kho chứa đồ)                       │
│     - Lưu trữ và lấy data từ database                 │
│     - Entity Framework Core                            │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│  1. DOMAIN (Quy tắc của nhà)                           │
│     - TodoList, TodoItem entities                      │
│     - Business rules                                   │
└────────────────────────────────────────────────────────┘
```

---

## 🚀 VÍ DỤ CỤ THỂ: GET Todo Items

### **Bước 0: Bạn gửi request trong Swagger**

```http
GET http://localhost:5000/api/todoitems?pageNumber=1&pageSize=10
```

---

### **Bước 1: WEB LAYER - Cửa vào**

**File: `src/Web/Endpoints/TodoItems.cs`**

```csharp
public class TodoItems : EndpointGroupBase
{
    public async Task<Ok<PaginatedList<TodoItemBriefDto>>> 
        GetTodoItemsWithPagination(
            ISender sender,                              // ← MediatR
            [AsParameters] GetTodoItemsWithPaginationQuery query)  // ← Query object
    {
        // Chỉ đơn giản: Nhận request và gửi cho MediatR xử lý
        var result = await sender.Send(query);
        
        return TypedResults.Ok(result);
    }
}
```

**Giải thích đơn giản:**
- Endpoint này giống như "lễ tân khách sạn"
- Nhận request từ bạn
- Không xử lý gì cả, chỉ **chuyển** cho người khác (MediatR)
- Nhận kết quả và trả về

**Tại sao làm vậy?**
- Web layer KHÔNG NÊN chứa business logic
- Giữ nó đơn giản, dễ test

---

### **Bước 2: MEDIATR - Bưu điện trung tâm**

**MediatR nhận query và tìm Handler phù hợp**

```
Query: GetTodoItemsWithPaginationQuery
  ↓
MediatR Pipeline (chạy qua các Behaviours):
  1. ✓ Logging - Ghi log request
  2. ✓ Validation - Kiểm tra pageNumber, pageSize có hợp lệ không
  3. ✓ Authorization - Kiểm tra user có quyền không
  ↓
Tìm Handler: GetTodoItemsWithPaginationQueryHandler
```

**Tại sao dùng MediatR?**
- Tách biệt giữa "nơi nhận request" và "nơi xử lý"
- Tự động chạy validation, logging, authorization
- Code sạch hơn, dễ test hơn

---

### **Bước 3: APPLICATION LAYER - Nơi xử lý logic**

**File: `src/Application/TodoItems/Queries/GetTodoItemsWithPagination/GetTodoItemsWithPagination.cs`**

```csharp
// Query Object (giống như đơn đặt hàng)
public record GetTodoItemsWithPaginationQuery : IRequest<PaginatedList<TodoItemBriefDto>>
{
    public int ListId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

// Handler (người xử lý đơn hàng)
public class GetTodoItemsWithPaginationQueryHandler 
    : IRequestHandler<GetTodoItemsWithPaginationQuery, PaginatedList<TodoItemBriefDto>>
{
    private readonly IApplicationDbContext _context;  // ← Database
    private readonly IMapper _mapper;                  // ← AutoMapper

    public async Task<PaginatedList<TodoItemBriefDto>> Handle(
        GetTodoItemsWithPaginationQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Lấy data từ database
        // 2. Convert Entity → DTO
        // 3. Phân trang
        return await _context.TodoItems
            .Where(x => x.ListId == request.ListId)
            .OrderBy(x => x.Title)
            .ProjectTo<TodoItemBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
```

**Giải thích đơn giản:**

1. **Query Object** (`GetTodoItemsWithPaginationQuery`):
   - Giống như "đơn đặt hàng"
   - Chứa thông tin: ListId gì? Trang thứ mấy? Bao nhiêu items?

2. **Handler** (`GetTodoItemsWithPaginationQueryHandler`):
   - Giống như "nhân viên xử lý đơn hàng"
   - Nhận đơn → Lấy data từ kho (database) → Trả kết quả

**Tại sao tách ra như vậy?**
- **Query** = What (Cái gì)
- **Handler** = How (Làm thế nào)
- Dễ test, dễ maintain

---

### **Bước 4: INFRASTRUCTURE LAYER - Lấy data từ database**

**File: `src/Infrastructure/Data/ApplicationDbContext.cs`**

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();  // ← Đây!
}
```

**Khi Handler gọi `_context.TodoItems`:**

```
1. EF Core tạo SQL query:
   SELECT * FROM TodoItems 
   WHERE ListId = @ListId 
   ORDER BY Title
   OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY

2. Chạy query trên database

3. Map data từ database → TodoItem entities

4. AutoMapper convert: TodoItem entity → TodoItemBriefDto
```

**Giải thích:**
- `ApplicationDbContext` = Cổng kết nối đến database
- `DbSet<TodoItem>` = Table "TodoItems" trong database
- EF Core tự động tạo SQL query, bạn không cần viết SQL

---

### **Bước 5: DOMAIN LAYER - Entity (Model)**

**File: `src/Domain/Entities/TodoItem.cs`**

```csharp
public class TodoItem : BaseAuditableEntity
{
    public int ListId { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime? Reminder { get; set; }
    public bool Done { get; set; }
    
    public TodoList List { get; set; } = null!;
}
```

**Giải thích:**
- `TodoItem` = 1 row trong database table
- Properties = columns
- `BaseAuditableEntity` = Tự động có Created, Modified dates

---

### **Bước 6: Trả về kết quả**

```
Handler trả về: PaginatedList<TodoItemBriefDto>
  ↓
MediatR nhận kết quả
  ↓
Endpoint nhận kết quả
  ↓
ASP.NET Core serialize JSON
  ↓
HTTP Response gửi cho browser
```

**Response JSON:**

```json
{
  "items": [
    {
      "id": 1,
      "listId": 1,
      "title": "Make a todo list 📃",
      "done": false,
      "priority": 1
    },
    {
      "id": 2,
      "listId": 1,
      "title": "Check off the first item ✅",
      "done": false,
      "priority": 0
    }
  ],
  "pageNumber": 1,
  "totalPages": 1,
  "totalCount": 4
}
```

---

## 🔄 SƠ ĐỒ TỔNG HỢP - TOÀN BỘ FLOW

```
┌─────────────────────────────────────────────────────────────┐
│ 1. BẠN (Client)                                              │
│    GET /api/todoitems?pageNumber=1&pageSize=10              │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. WEB LAYER (Endpoints/TodoItems.cs)                       │
│    → Nhận request                                           │
│    → Tạo GetTodoItemsWithPaginationQuery object             │
│    → Gửi cho MediatR: await sender.Send(query)              │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. MEDIATR PIPELINE                                          │
│    ├─ LoggingBehaviour: Log request                         │
│    ├─ ValidationBehaviour: Validate pageNumber, pageSize    │
│    ├─ AuthorizationBehaviour: Check user permissions        │
│    └─ Tìm Handler: GetTodoItemsWithPaginationQueryHandler   │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. APPLICATION LAYER (Handler)                               │
│    → Nhận query object                                      │
│    → Gọi _context.TodoItems (từ Infrastructure)             │
│    → Apply filters, sorting, pagination                     │
│    → ProjectTo<TodoItemBriefDto> (AutoMapper)               │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. INFRASTRUCTURE LAYER (EF Core)                            │
│    → ApplicationDbContext.TodoItems                         │
│    → Generate SQL query                                     │
│    → Execute query on database                              │
│    → Return TodoItem entities                               │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. DOMAIN LAYER (Entity)                                     │
│    → TodoItem entity (model)                                │
│    → Properties: Id, Title, Done, Priority, etc.            │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. TRẢ VỀ (Response)                                         │
│    Entity → DTO → JSON → HTTP Response → Browser            │
└─────────────────────────────────────────────────────────────┘
```

---

## 🤔 TẠI SAO LẠI PHỨC TẠP VẬY?

### **Câu hỏi:** Tại sao không làm đơn giản thế này?

```csharp
// Đơn giản nhưng SAI
public IActionResult GetTodoItems()
{
    var items = _context.TodoItems.ToList();  // ← Trực tiếp query database
    return Ok(items);                          // ← Trả về entity
}
```

### **Trả lời:**

#### ❌ **Cách đơn giản (như trên) có vấn đề:**

1. **Không có Validation** - Ai đó gửi pageSize = 10000000? → Crash!
2. **Không có Authorization** - User nào cũng xem được?
3. **Không có Logging** - Không biết ai gọi API khi nào
4. **Khó test** - Phải có database để test
5. **Tight coupling** - Web layer biết database → Khó thay đổi
6. **Trả entity** - Expose toàn bộ data, kể cả sensitive fields

#### ✅ **Clean Architecture giải quyết:**

1. **Validation tự động** - ValidationBehaviour check input
2. **Authorization tự động** - AuthorizationBehaviour check quyền
3. **Logging tự động** - LoggingBehaviour ghi log
4. **Dễ test** - Mock IApplicationDbContext, không cần database
5. **Loose coupling** - Web chỉ biết MediatR, không biết database
6. **Trả DTO** - Chỉ trả data cần thiết, security tốt hơn

---

## 📚 CÁC THUẬT NGỮ QUAN TRỌNG

| Thuật ngữ | Giải thích đơn giản | Ví dụ |
|-----------|---------------------|-------|
| **Command** | Yêu cầu **THAY ĐỔI** data | CreateTodoItem, UpdateTodoItem |
| **Query** | Yêu cầu **ĐỌC** data | GetTodoItems |
| **Handler** | Người xử lý Command/Query | GetTodoItemsQueryHandler |
| **MediatR** | "Bưu điện" chuyển request đến Handler | sender.Send(query) |
| **Entity** | Model trong database | TodoItem, TodoList |
| **DTO** | Data Transfer Object - Dữ liệu trả về API | TodoItemBriefDto |
| **DbContext** | Kết nối đến database | ApplicationDbContext |
| **Behaviour** | Xử lý tự động trước/sau Handler | Validation, Logging, Authorization |

---

## 🎯 LỘ TRÌNH HỌC

### **Bước 1: Hiểu Flow (Bạn đang ở đây!)**
- ✅ Đã hiểu flow của 1 request GET
- ✅ Biết 4 layers làm gì

### **Bước 2: Thực hành với POST (Create)**

Hãy thử API:
```
POST /api/todoitems
{
  "listId": 1,
  "title": "New task",
  "priority": 1
}
```

Flow tương tự nhưng dùng **Command** thay vì Query:

```
Endpoint 
  → MediatR 
  → CreateTodoItemCommandHandler 
  → Tạo entity 
  → Save vào database
```

### **Bước 3: Trace trong Code**

Mở các files theo thứ tự:

1. `src/Web/Endpoints/TodoItems.cs` - Xem endpoint
2. `src/Application/TodoItems/Queries/GetTodoItemsWithPagination/GetTodoItemsWithPagination.cs` - Xem Handler
3. `src/Domain/Entities/TodoItem.cs` - Xem Entity

### **Bước 4: Tạo Feature Mới**

Thử tạo một feature mới (ví dụ: Products):
1. Tạo Entity trong Domain
2. Tạo Commands/Queries trong Application
3. Tạo Endpoint trong Web

---

## 💡 TIPS ĐỂ KHÔNG BỊ RỐI

### **1. Nhớ công thức:**

```
Request → Web (Endpoint) → MediatR → Application (Handler) → Infrastructure (Database) → Domain (Entity)
```

### **2. Mỗi layer có 1 nhiệm vụ duy nhất:**

- **Web**: Nhận/trả HTTP request
- **Application**: Business logic, validation
- **Infrastructure**: Database, external services
- **Domain**: Entities, business rules

### **3. Không cố hiểu hết một lúc:**

Chỉ cần hiểu:
1. ✅ Request đến Endpoint
2. ✅ Endpoint gọi MediatR
3. ✅ Handler xử lý và trả kết quả

Chi tiết còn lại học dần!

### **4. Debug để hiểu:**

Trong Visual Studio:
1. Đặt breakpoint ở Endpoint
2. F5 debug
3. Step through (F10) từng bước
4. Xem data thay đổi thế nào

---

## 🚀 THỰC HÀNH NGAY

### **Bài tập 1: Trace GET request**

1. Mở Swagger: `http://localhost:5000/api`
2. Gọi `GET /api/todoitems`
3. Mở code và đọc theo flow:
   - `src/Web/Endpoints/TodoItems.cs` → Line 22
   - `src/Application/TodoItems/Queries/...` 
   - `src/Domain/Entities/TodoItem.cs`

### **Bài tập 2: Trace POST request**

1. Gọi `POST /api/todoitems` trong Swagger
2. Trace flow tương tự
3. So sánh khác gì với GET

### **Bài tập 3: Debug**

1. Đặt breakpoint ở `GetTodoItemsWithPagination` method
2. F5 trong Visual Studio
3. Gọi API từ Swagger
4. Step through code (F10)
5. Quan sát biến thay đổi

---

## 📖 KẾT LUẬN

Clean Architecture nhìn phức tạp nhưng:

✅ **Lợi ích lớn:**
- Code sạch, dễ maintain
- Dễ test
- Dễ mở rộng
- Tách biệt concerns

✅ **Pattern cốt lõi:**
```
Endpoint → MediatR → Handler → Database → Entity
```

✅ **Chỉ cần nhớ:**
- **Web** = Cửa vào/ra
- **Application** = Xử lý logic
- **Infrastructure** = Database
- **Domain** = Models

---

**🎉 Bây giờ bạn đã hiểu Clean Architecture qua 1 ví dụ cụ thể!**

Có câu hỏi gì cứ hỏi, tôi sẽ giải thích thêm! 😊


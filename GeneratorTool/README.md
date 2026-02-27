# GeneratorTool - Clean Architecture Scaffold Generator

Công cụ tự động generate scaffold theo kiến trúc Clean Architecture cho dự án Lotus Dharma.

## Cách sử dụng

```bash
# Build project
dotnet build

# Generate scaffold cho entity Patient
dotnet run --project src/GeneratorTool -- generate --table Patient
```

## Cấu trúc files được tạo

Với entity `Patient`, tool sẽ tạo:

### Domain Layer
- `src/Domain/Entities/Patient.cs` - Entity kế thừa BaseAuditableEntity
- `src/Domain/Events/PatientCreatedEvent.cs` - Domain event

### Application Layer (CQRS)
- `src/Application/Patients/Commands/CreatePatient/CreatePatient.cs` - Command + Handler
- `src/Application/Patients/Commands/CreatePatient/CreatePatientCommandValidator.cs` - Validator
- `src/Application/Patients/Commands/UpdatePatient/UpdatePatient.cs` - Command + Handler
- `src/Application/Patients/Commands/UpdatePatient/UpdatePatientCommandValidator.cs` - Validator
- `src/Application/Patients/Commands/DeletePatient/DeletePatient.cs` - Command + Handler
- `src/Application/Patients/Queries/GetPatients/GetPatients.cs` - Query + Handler
- `src/Application/Patients/Queries/GetPatients/PatientDto.cs` - DTO với AutoMapper mapping
- `src/Application/Patients/Queries/GetPatientById/GetPatientById.cs` - Query + Handler

### Infrastructure Layer
- `src/Infrastructure/Data/Configurations/PatientConfiguration.cs` - EF Core configuration

### Web Layer
- `src/Web/Endpoints/Patients.cs` - REST endpoints kế thừa EndpointGroupBase

### Cache Keys (updated)
- `src/Application/Common/Caching/CacheKeys.cs` - Thêm Patient cache keys

## Features

### ✅ Audit tự động
- Commands tự động lấy `UserId` từ JWT claims (IUser)
- Không cần truyền `CreatedIdUser/UpdatedIdUser` từ client

### ✅ CQRS Pattern
- MediatR Commands/Queries tách biệt
- FluentValidation cho tất cả commands

### ✅ Caching
- Queries sử dụng `ICacheService.GetOrCreateAsync`
- Commands invalidate cache
- Centralized cache keys

### ✅ Clean Architecture
- Domain thuần, không dependency
- Application chỉ chứa logic business
- Infrastructure implement persistence
- Web chỉ endpoints mỏng

### ✅ Safe Generation
- Skip files đã tồn tại
- Không overwrite code hiện có

## Next Steps sau khi generate

1. **Register DbSet trong ApplicationDbContext**:
   ```csharp
   public DbSet<Patient> Patients { get; set; }
   ```

2. **Add Configuration vào ApplicationDbContext**:
   ```csharp
   builder.ApplyConfiguration(new PatientConfiguration());
   ```

3. **Register Endpoints trong Program.cs**:
   ```csharp
   app.MapGroup("api/patients").MapPatients();
   ```

4. **Tạo Migration**:
   ```bash
   dotnet ef migrations add AddPatient
   dotnet ef database update
   ```

## Naming Conventions

- **Entity**: PascalCase (Patient, Category)
- **Plural**: Sử dụng Humanizer (Patient → Patients, Category → Categories)
- **Namespaces**: `LotusDharma.{Layer}.{EntityPlural}.{Type}.{Action}{Entity}`
- **Cache Keys**: `{entityLower}s:{action/id}:{params}`

## Dependencies

- System.CommandLine: Command line parsing
- Humanizer.Core: Pluralization logic
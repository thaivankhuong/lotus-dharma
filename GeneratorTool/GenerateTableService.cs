namespace GeneratorTool;

using Humanizer;

public sealed class GenerateTableService
{
    private readonly string _repoRoot;

    public GenerateTableService()
    {
        _repoRoot = ResolveRepoRoot();
    }

    public void Generate(string? entityNameOverride = null)
    {
        // Read model metadata
        var modelMeta = ModelIntrospector.ReadSingleModelMeta(_repoRoot);
        var entityName = entityNameOverride ?? modelMeta.Name;
        var fields = modelMeta.Fields;

        var plural = Pluralize(entityName);

        Console.WriteLine($"🔧 Generating MVP scaffold for entity: {entityName}");
        Console.WriteLine($"📁 Plural form: {plural}");
        Console.WriteLine($"📍 Repo root: {_repoRoot}");
        Console.WriteLine($"📋 Model fields: {string.Join(", ", fields.Select(f => $"{f.Name} ({f.TypeName})"))}");

        // Generate Domain layer
        GenerateDomainEntity(entityName, fields);
        GenerateDomainEvents(entityName);

        // Generate Application layer
        GenerateCreateCommand(entityName, plural, fields);
        GenerateCreateValidator(entityName, plural, fields);
        GenerateUpdateCommand(entityName, plural, fields);
        GenerateUpdateValidator(entityName, plural, fields);
        GenerateDeleteCommand(entityName, plural);
        GenerateGetAllQuery(entityName, plural, fields);
        GenerateDto(entityName, plural, fields);

        // Generate Infrastructure layer
        GenerateConfiguration(entityName, fields);

        // Generate Web layer
        GenerateEndpoints(entityName, plural);

        // Auto-add DbSet properties
        AddDbSetToApplicationDbContext(entityName, plural);
        AddDbSetToIApplicationDbContext(entityName, plural);

        Console.WriteLine($"✅ Successfully generated MVP scaffold for {entityName}");
        Console.WriteLine();
        Console.WriteLine("📋 Remaining TODO checklist for developer:");
        Console.WriteLine($"1. Add cache keys to CacheKeys.cs: All{plural}, {entityName}ById, All{plural} pattern");
        Console.WriteLine($"2. Register endpoints: app.MapGroup(\"api/{plural.ToLower()}\").Map{plural}();");
        Console.WriteLine($"3. Run: dotnet ef migrations add Add{entityName}");
        Console.WriteLine($"4. Run: dotnet ef database update");
        Console.WriteLine();
        Console.WriteLine($"Note: DbSet properties have been automatically added to ApplicationDbContext and IApplicationDbContext");
    }

    private string ResolveRepoRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var baseDir = AppContext.BaseDirectory;

        Console.WriteLine($"🔍 Looking for repo root from: {currentDir}");

        // Try from current directory first, then base directory
        var searchDirs = new[] { currentDir, baseDir };

        foreach (var startDir in searchDirs)
        {
            var dir = new DirectoryInfo(startDir);

            // Walk up the directory tree
            while (dir != null)
            {
                // Check if this directory contains both a .sln file and a src folder
                var slnFiles = dir.GetFiles("*.sln");
                var srcFolder = dir.GetDirectories("src");

                if (slnFiles.Length > 0 && srcFolder.Length > 0)
                {
                    Console.WriteLine($"✅ Found repo root: {dir.FullName}");
                    return dir.FullName;
                }

                dir = dir.Parent;
            }
        }

        throw new InvalidOperationException(
            $"Could not find repository root. Searched from:\n" +
            $"- CurrentDirectory: {currentDir}\n" +
            $"- BaseDirectory: {baseDir}\n" +
            $"Expected to find a directory containing both a *.sln file and a 'src' folder.");
    }

    private string Pluralize(string entityName)
    {
        return entityName.Pluralize();
    }

    private enum AccessorKind { Set, Init }

    private string RenderProperty(FieldMeta field, AccessorKind accessorKind, bool includeInitializerForStrings = true)
    {
        var accessor = accessorKind == AccessorKind.Set ? "set" : "init";

        // Add initializer for non-nullable strings if requested
        if (includeInitializerForStrings && field.TypeName == "string" && !field.IsNullable)
        {
            return $"    public {field.TypeName} {field.Name} {{ get; {accessor}; }} = string.Empty;";
        }

        // No initializer needed
        return $"    public {field.TypeName} {field.Name} {{ get; {accessor}; }}";
    }

    private static readonly string[] SystemFields = ["Id", "Created", "LastModified", "CreatedIdUser", "UpdatedIdUser"];

    private List<FieldMeta> GetCommandFields(List<FieldMeta> fields)
    {
        return fields.Where(f => !SystemFields.Contains(f.Name)).ToList();
    }

    private void WriteFileIfNotExists(string path, string content)
    {
        if (File.Exists(path))
        {
            Console.WriteLine($"⚠️  Skipping {Path.GetRelativePath(_repoRoot, path)} - file already exists");
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content);
        Console.WriteLine($"✅ Created {Path.GetRelativePath(_repoRoot, path)}");
    }

    // Domain Layer
    private void GenerateDomainEntity(string entityName, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Domain", "Entities", $"{entityName}.cs");

        var properties = new List<string>();
        foreach (var field in fields)
        {
            properties.Add(RenderProperty(field, AccessorKind.Set, includeInitializerForStrings: true));
        }

        var propertiesText = string.Join("\n", properties);

        var content = $$"""
namespace LotusDharma.Domain.Entities;

public class {{entityName}} : BaseAuditableEntity
{
{{propertiesText}}
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateDomainEvents(string entityName)
    {
        var createdPath = Path.Combine(_repoRoot, "src", "Domain", "Events", $"{entityName}CreatedEvent.cs");
        var createdContent = $$"""
namespace LotusDharma.Domain.Events;

public class {{entityName}}CreatedEvent : BaseEvent
{
    public {{entityName}}CreatedEvent({{entityName}} {{entityName.ToLower()}})
    {
        {{entityName}} = {{entityName.ToLower()}};
    }

    public {{entityName}} {{entityName}} { get; }
}
""";
        WriteFileIfNotExists(createdPath, createdContent);

        var deletedPath = Path.Combine(_repoRoot, "src", "Domain", "Events", $"{entityName}DeletedEvent.cs");
        var deletedContent = $$"""
namespace LotusDharma.Domain.Events;

public class {{entityName}}DeletedEvent : BaseEvent
{
    public {{entityName}}DeletedEvent({{entityName}} {{entityName.ToLower()}})
    {
        {{entityName}} = {{entityName.ToLower()}};
    }

    public {{entityName}} {{entityName}} { get; }
}
""";
        WriteFileIfNotExists(deletedPath, deletedContent);
    }

    // Application Layer - Commands
    private void GenerateCreateCommand(string entityName, string plural, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Commands", $"Create{entityName}", $"Create{entityName}.cs");

        var commandFields = GetCommandFields(fields);

        var commandProperties = new List<string>();
        foreach (var field in commandFields)
        {
            commandProperties.Add(RenderProperty(field, AccessorKind.Init, includeInitializerForStrings: true));
        }

        var entityAssignments = new List<string>();
        foreach (var field in commandFields)
        {
            entityAssignments.Add($"            {field.Name} = request.{field.Name},");
        }

        // Handle CreatedIdUser if present in model
        var hasCreatedIdUser = fields.Any(f => f.Name == "CreatedIdUser");
        if (hasCreatedIdUser)
        {
            entityAssignments.Add("            CreatedIdUser = _user.Id,");
        }

        // Handle IsActive if present in model
        var hasIsActive = fields.Any(f => f.Name == "IsActive");
        if (hasIsActive)
        {
            entityAssignments.Add("            IsActive = true");
        }
        else
        {
            // Remove trailing comma from last assignment
            if (entityAssignments.Count > 0)
            {
                entityAssignments[^1] = entityAssignments[^1].TrimEnd(',');
            }
        }

        var commandPropertiesText = string.Join("\n", commandProperties);
        var entityAssignmentsText = string.Join("\n", entityAssignments);

        var content = $$"""
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Entities;
using LotusDharma.Domain.Events;

namespace LotusDharma.Application.{{plural}}.Commands.Create{{entityName}};

public record Create{{entityName}}Command : IRequest<int>
{
{{commandPropertiesText}}
}

public class Create{{entityName}}CommandHandler : IRequestHandler<Create{{entityName}}Command, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public Create{{entityName}}CommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(Create{{entityName}}Command request, CancellationToken cancellationToken)
    {
        var entity = new {{entityName}}
        {
{{entityAssignmentsText}}
        };

        entity.AddDomainEvent(new {{entityName}}CreatedEvent(entity));
        _context.{{plural}}.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateCreateValidator(string entityName, string plural, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Commands", $"Create{entityName}", $"Create{entityName}CommandValidator.cs");

        var commandFields = GetCommandFields(fields);

        var validationRules = new List<string>();
        foreach (var field in commandFields)
        {
            if (field.TypeName == "string")
            {
                if (!field.IsNullable)
                {
                    validationRules.Add($$"""
        RuleFor(v => v.{{field.Name}})
            .NotEmpty().WithMessage("{{entityName}} {{field.Name.ToLower()}} is required.")
            .MaximumLength(255).WithMessage("{{entityName}} {{field.Name.ToLower()}} must not exceed 255 characters.");
""");
                }
                else
                {
                    validationRules.Add($$"""
        RuleFor(v => v.{{field.Name}})
            .MaximumLength(255).WithMessage("{{entityName}} {{field.Name.ToLower()}} must not exceed 255 characters.");
""");
                }
            }
        }

        var validationRulesText = string.Join("\n", validationRules);

        var content = $$"""
namespace LotusDharma.Application.{{plural}}.Commands.Create{{entityName}};

public class Create{{entityName}}CommandValidator : AbstractValidator<Create{{entityName}}Command>
{
    public Create{{entityName}}CommandValidator()
    {
{{validationRulesText}}
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateUpdateCommand(string entityName, string plural, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Commands", $"Update{entityName}", $"Update{entityName}.cs");

        var commandFields = GetCommandFields(fields);

        var commandProperties = new List<string>();
        commandProperties.Add("    public int Id { get; init; }");
        foreach (var field in commandFields)
        {
            commandProperties.Add(RenderProperty(field, AccessorKind.Init, includeInitializerForStrings: true));
        }

        var entityAssignments = new List<string>();
        foreach (var field in commandFields)
        {
            entityAssignments.Add($"        entity.{field.Name} = request.{field.Name};");
        }

        // Handle UpdatedIdUser if present in model
        var hasUpdatedIdUser = fields.Any(f => f.Name == "UpdatedIdUser");
        if (hasUpdatedIdUser)
        {
            entityAssignments.Add("        entity.UpdatedIdUser = _user.Id;");
        }

        var commandPropertiesText = string.Join("\n", commandProperties);
        var entityAssignmentsText = string.Join("\n", entityAssignments);

        var content = $$"""
using LotusDharma.Application.Common.Interfaces;

namespace LotusDharma.Application.{{plural}}.Commands.Update{{entityName}};

public record Update{{entityName}}Command : IRequest
{
{{commandPropertiesText}}
}

public class Update{{entityName}}CommandHandler : IRequestHandler<Update{{entityName}}Command>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public Update{{entityName}}CommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(Update{{entityName}}Command request, CancellationToken cancellationToken)
    {
        var entity = await _context.{{plural}}.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

{{entityAssignmentsText}}

        await _context.SaveChangesAsync(cancellationToken);
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateUpdateValidator(string entityName, string plural, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Commands", $"Update{entityName}", $"Update{entityName}CommandValidator.cs");

        var commandFields = GetCommandFields(fields);

        var validationRules = new List<string>();
        validationRules.Add("        RuleFor(v => v.Id).GreaterThan(0);");

        foreach (var field in commandFields)
        {
            if (field.TypeName == "string")
            {
                if (!field.IsNullable)
                {
                    validationRules.Add($$"""
        RuleFor(v => v.{{field.Name}})
            .NotEmpty().WithMessage("{{entityName}} {{field.Name.ToLower()}} is required.")
            .MaximumLength(255);
""");
                }
                else
                {
                    validationRules.Add($$"""
        RuleFor(v => v.{{field.Name}})
            .MaximumLength(255);
""");
                }
            }
        }

        var validationRulesText = string.Join("\n", validationRules);

        var content = $$"""
namespace LotusDharma.Application.{{plural}}.Commands.Update{{entityName}};

public class Update{{entityName}}CommandValidator : AbstractValidator<Update{{entityName}}Command>
{
    public Update{{entityName}}CommandValidator()
    {
{{validationRulesText}}
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateDeleteCommand(string entityName, string plural)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Commands", $"Delete{entityName}", $"Delete{entityName}.cs");
        var content = $$"""
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Events;

namespace LotusDharma.Application.{{plural}}.Commands.Delete{{entityName}};

public record Delete{{entityName}}Command(int Id) : IRequest;

public class Delete{{entityName}}CommandHandler : IRequestHandler<Delete{{entityName}}Command>
{
    private readonly IApplicationDbContext _context;

    public Delete{{entityName}}CommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(Delete{{entityName}}Command request, CancellationToken cancellationToken)
    {
        var entity = await _context.{{plural}}.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.{{plural}}.Remove(entity);
        entity.AddDomainEvent(new {{entityName}}DeletedEvent(entity));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateGetAllQuery(string entityName, string plural, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Queries", $"Get{plural}", $"Get{plural}.cs");

        var pluralLower = plural.ToLower();

        var content = $$"""
using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Security;

namespace LotusDharma.Application.{{plural}}.Queries.Get{{plural}};

[Authorize]
public record Get{{plural}}Query : IRequest<List<{{entityName}}Dto>>;

public class Get{{plural}}QueryHandler : IRequestHandler<Get{{plural}}Query, List<{{entityName}}Dto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public Get{{plural}}QueryHandler(IApplicationDbContext context, IMapper mapper, ICacheService cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<List<{{entityName}}Dto>> Handle(Get{{plural}}Query request, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            $"{{pluralLower}}:all",
            async () => await _context.{{plural}}
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .ProjectTo<{{entityName}}Dto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken),
            TimeSpan.FromHours(12),
            cancellationToken);
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void GenerateDto(string entityName, string plural, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", plural, "Queries", $"Get{plural}", $"{entityName}Dto.cs");

        var dtoProperties = new List<string>();

        // Always include Id and audit fields
        dtoProperties.Add("    public int Id { get; init; }");
        dtoProperties.Add("    public string? CreatedIdUser { get; init; }");
        dtoProperties.Add("    public string? UpdatedIdUser { get; init; }");
        dtoProperties.Add("    public DateTimeOffset Created { get; init; }");
        dtoProperties.Add("    public DateTimeOffset LastModified { get; init; }");

        // Include all model fields (including system fields like IsActive for consistency)
        foreach (var field in fields)
        {
            dtoProperties.Add(RenderProperty(field, AccessorKind.Init, includeInitializerForStrings: true));
        }

        var dtoPropertiesText = string.Join("\n", dtoProperties);

        var content = $$"""
using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.{{plural}}.Queries.Get{{plural}};

public class {{entityName}}Dto
{
{{dtoPropertiesText}}

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<{{entityName}}, {{entityName}}Dto>();
        }
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    // Infrastructure Layer
    private void GenerateConfiguration(string entityName, List<FieldMeta> fields)
    {
        var path = Path.Combine(_repoRoot, "src", "Infrastructure", "Data", "Configurations", $"{entityName}Configuration.cs");

        var configurations = new List<string>();

        foreach (var field in fields)
        {
            if (field.TypeName == "string")
            {
                var maxLength = field.Name.Contains("IdUser") ? 450 : 255;
                var config = $"        builder.Property(c => c.{field.Name})\n            .HasMaxLength({maxLength})";

                if (!field.IsNullable)
                {
                    config += "\n            .IsRequired();";
                }
                else
                {
                    config += ";";
                }

                configurations.Add(config);
            }
            else if (field.TypeName == "bool" && field.Name == "IsActive")
            {
                configurations.Add($$"""
        builder.Property(c => c.{{field.Name}})
            .IsRequired()
            .HasDefaultValue(true);
""");
            }
        }

        var configurationsText = string.Join("\n", configurations);

        var content = $$"""
using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class {{entityName}}Configuration : IEntityTypeConfiguration<{{entityName}}>
{
    public void Configure(EntityTypeBuilder<{{entityName}}> builder)
    {
{{configurationsText}}
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    // Web Layer
    private void GenerateEndpoints(string entityName, string plural)
    {
        var path = Path.Combine(_repoRoot, "src", "Web", "Endpoints", $"{plural}.cs");
        var pluralLower = plural.ToLower();
        var content = $$"""
using LotusDharma.Application.{{plural}}.Commands.Create{{entityName}};
using LotusDharma.Application.{{plural}}.Commands.Delete{{entityName}};
using LotusDharma.Application.{{plural}}.Commands.Update{{entityName}};
using LotusDharma.Application.{{plural}}.Queries.Get{{plural}};
using LotusDharma.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LotusDharma.Web.Endpoints;

[Authorize]
public class {{plural}} : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(Get{{plural}}).RequireAuthorization();
        groupBuilder.MapPost(Create{{entityName}}).RequireAuthorization();
        groupBuilder.MapPut(Update{{entityName}}, "{id}").RequireAuthorization();
        groupBuilder.MapDelete(Delete{{entityName}}, "{id}").RequireAuthorization();
    }

    public async Task<Ok<List<{{entityName}}Dto>>> Get{{plural}}(ISender sender)
    {
        var result = await sender.Send(new Get{{plural}}Query());
        return TypedResults.Ok(result);
    }

    public async Task<Created<int>> Create{{entityName}}(ISender sender, Create{{entityName}}Command command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof({{plural}})}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> Update{{entityName}}(ISender sender, int id, Update{{entityName}}Command command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> Delete{{entityName}}(ISender sender, int id)
    {
        await sender.Send(new Delete{{entityName}}Command(id));
        return TypedResults.NoContent();
    }
}
""";
        WriteFileIfNotExists(path, content);
    }

    private void AddDbSetToApplicationDbContext(string entityName, string plural)
    {
        var path = Path.Combine(_repoRoot, "src", "Infrastructure", "Data", "ApplicationDbContext.cs");
        var dbSetLine = $"    public DbSet<{entityName}> {plural} => Set<{entityName}>();";

        InsertBeforeMarkerIfMissing(path, dbSetLine, "    // Custom Identity entities");
    }

    private void AddDbSetToIApplicationDbContext(string entityName, string plural)
    {
        var path = Path.Combine(_repoRoot, "src", "Application", "Common", "Interfaces", "IApplicationDbContext.cs");
        var dbSetLine = $"    DbSet<{entityName}> {plural} {{ get; }}";

        InsertBeforeMarkerIfMissing(path, dbSetLine, "    // Identity");
    }

    private void InsertBeforeMarkerIfMissing(string filePath, string lineToInsert, string markerComment)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"⚠️  File not found: {Path.GetRelativePath(_repoRoot, filePath)}");
            return;
        }

        var content = File.ReadAllText(filePath);

        // Check if the line already exists
        if (content.Contains(lineToInsert.Trim()))
        {
            Console.WriteLine($"⚠️  DbSet already exists in {Path.GetRelativePath(_repoRoot, filePath)}");
            return;
        }

        var markerIndex = content.IndexOf(markerComment);
        if (markerIndex == -1)
        {
            Console.WriteLine($"⚠️  Marker comment not found in {Path.GetRelativePath(_repoRoot, filePath)}");
            return;
        }

        // Insert the line before the marker
        var newContent = content.Insert(markerIndex, lineToInsert + "\n");
        File.WriteAllText(filePath, newContent);
        Console.WriteLine($"✅ Added DbSet to {Path.GetRelativePath(_repoRoot, filePath)}");
    }
}

namespace GeneratorTool;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed record ModelMeta(string Name, List<FieldMeta> Fields);

internal sealed record FieldMeta(string Name, string TypeName, bool IsNullable);

internal static class ModelIntrospector
{
    private static readonly string[] ExcludedFileNames = ["GlobalUsings.cs"];
    private static readonly string[] ExcludedDirectories = ["bin", "obj"];

    public static ModelMeta ReadSingleModelMeta(string repoRoot)
    {
        var modelFolder = FindModelFolder(repoRoot);
        var csFiles = FindCsFiles(modelFolder);

        if (csFiles.Count == 0)
        {
            throw new InvalidOperationException(
                $"No C# model files found in {modelFolder}. " +
                "Please create a single public class in the ModelGennerator folder.");
        }

        if (csFiles.Count > 1)
        {
            var fileNames = string.Join(", ", csFiles.Select(Path.GetFileName));
            throw new InvalidOperationException(
                $"Multiple C# model files found in {modelFolder}: {fileNames}. " +
                "Please ensure there is exactly one model class file.");
        }

        var modelFile = csFiles[0];
        return ParseModelFile(modelFile);
    }

    private static string FindModelFolder(string repoRoot)
    {
        // Primary location: ModelGennerator (note spelling)
        var primaryPath = Path.Combine(repoRoot, "ModelGennerator");
        if (Directory.Exists(primaryPath))
        {
            return primaryPath;
        }

        // Fallback location: src/ModelGennerator
        var fallbackPath = Path.Combine(repoRoot, "src", "ModelGennerator");
        if (Directory.Exists(fallbackPath))
        {
            return fallbackPath;
        }

        // Additional fallback: GeneratorTool/ModelGennerator (when running from within GeneratorTool)
        var generatorToolPath = Path.Combine(repoRoot, "GeneratorTool", "ModelGennerator");
        if (Directory.Exists(generatorToolPath))
        {
            return generatorToolPath;
        }

        throw new DirectoryNotFoundException(
            $"ModelGennerator folder not found. Searched in: {primaryPath}, {fallbackPath}, {generatorToolPath}");
    }

    private static List<string> FindCsFiles(string modelFolder)
    {
        return Directory.GetFiles(modelFolder, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                var fileName = Path.GetFileName(file);
                var relativePath = Path.GetRelativePath(modelFolder, file);

                // Exclude specific file names
                if (ExcludedFileNames.Contains(fileName))
                {
                    return false;
                }

                // Exclude files in excluded directories
                if (ExcludedDirectories.Any(dir => relativePath.StartsWith(dir + Path.DirectorySeparatorChar) ||
                                                   relativePath.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar)))
                {
                    return false;
                }

                // Exclude temporary files
                if (fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            })
            .ToList();
    }

    private static ModelMeta ParseModelFile(string filePath)
    {
        var code = File.ReadAllText(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

        if (classDeclaration == null)
        {
            throw new InvalidOperationException(
                $"No public class found in {Path.GetFileName(filePath)}. " +
                "Please ensure there is at least one public class in the model file.");
        }

        var className = classDeclaration.Identifier.Text;
        var fields = ExtractFields(classDeclaration);

        return new ModelMeta(className, fields);
    }

    private static List<FieldMeta> ExtractFields(ClassDeclarationSyntax classDeclaration)
    {
        var fields = new List<FieldMeta>();

        // Find all property declarations
        var properties = classDeclaration.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.AccessorList != null &&
                       p.AccessorList.Accessors.Any(a => a.Keyword.IsKind(SyntaxKind.GetKeyword) ||
                                                        a.Keyword.IsKind(SyntaxKind.InitKeyword)));

        foreach (var property in properties)
        {
            // Skip static properties
            if (property.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                continue;
            }

            // Skip non-public properties
            if (!property.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                continue;
            }

            var fieldName = property.Identifier.Text;
            var typeName = property.Type.ToString();
            var isNullable = IsNullableType(property.Type);

            fields.Add(new FieldMeta(fieldName, typeName, isNullable));
        }

        return fields.OrderBy(f => f.Name).ToList(); // Deterministic ordering
    }

    private static bool IsNullableType(TypeSyntax typeSyntax)
    {
        // Check for nullable reference types (Type?)
        if (typeSyntax is NullableTypeSyntax)
        {
            return true;
        }

        // Check for Nullable<T>
        if (typeSyntax is GenericNameSyntax genericType &&
            genericType.Identifier.Text == "Nullable")
        {
            return true;
        }

        // For now, we'll be conservative and only mark explicitly nullable types as nullable
        // In the future, we could add nullable reference type analysis
        return false;
    }
}
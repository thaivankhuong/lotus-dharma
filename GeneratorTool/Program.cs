using System.CommandLine;
using GeneratorTool;

var rootCommand = new RootCommand("Clean Architecture Scaffold Generator for Lotus Dharma");

var generateCommand = new Command("generate", "Generate MVP scaffold for an entity (like Category module)");
var tableOption = new Option<string>(
    name: "--table",
    description: "The entity name to generate scaffold for (e.g., Patient, Category). If not provided, will be inferred from ModelGennerator folder.");

generateCommand.AddOption(tableOption);
generateCommand.SetHandler((tableName) =>
{
    GenerateScaffold(tableName);
}, tableOption);

rootCommand.AddCommand(generateCommand);

// If no arguments provided (F5 debug), default to "generate" command
if (args == null || args.Length == 0)
{
    args = new[] { "generate" };
}

return rootCommand.Invoke(args);

static void GenerateScaffold(string? entityName)
{
    try
    {
        var generator = new GenerateTableService();
        generator.Generate(entityName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
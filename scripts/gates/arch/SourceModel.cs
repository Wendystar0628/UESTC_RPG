using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

// Source is required: compiled IL inlines TestTolerance and loses the distinction this gate guards.
internal sealed record SourceFile(string Path, SyntaxTree Tree, SemanticModel Model)
{
    public CompilationUnitSyntax Root => (CompilationUnitSyntax)Tree.GetRoot();
    public bool IsTest => Path.Contains("/tests/", StringComparison.Ordinal) || Path.StartsWith("scripts/gates/arch/", StringComparison.Ordinal);
    public string At(SyntaxNode node, string message) => $"{Path}:{Tree.GetLineSpan(node.Span).StartLinePosition.Line + 1}: {message}";
}

internal static class SourceModel
{
    private static readonly Lazy<SourceFile[]> Files = new(Load);
    public static IEnumerable<SourceFile> Production => Files.Value.Where(file => !file.IsTest);
    public static IEnumerable<SourceFile> Tests => Files.Value.Where(file => file.IsTest);

    private static SourceFile[] Load()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? ArchitectureTests.FindRepository();
        var paths = ProjectItems().SelectMany(project => project.Sources).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => (Full: path, Relative: System.IO.Path.GetRelativePath(root, path).Replace('\\', '/')))
            .Where(item => !item.Relative.Split('/').Any(part => part.StartsWith('.') || part is "obj" or "bin" or "addons" ||
                part.StartsWith("gdunit4_testadapter", StringComparison.Ordinal)))
            .OrderBy(item => item.Relative, StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) throw new InvalidOperationException("G4 source model has no project sources.");
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)
            .WithPreprocessorSymbols("DEBUG", "TRACE", "GODOT", "GODOT4", "TOOLS");
        var trees = paths.Select(item => CSharpSyntaxTree.ParseText(File.ReadAllText(item.Full), options, item.Relative)).ToArray();
        var syntaxErrors = trees.SelectMany(tree => tree.GetDiagnostics()).Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        if (syntaxErrors.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, syntaxErrors.Select(item => item.ToString())));
        // Match the SDK implicit usings and the shared test projects' Campus.Tests import.
        var usings = CSharpSyntaxTree.ParseText("global using System; global using System.IO; global using System.Linq; " +
            "global using System.Collections.Generic; global using System.Threading; global using System.Threading.Tasks; " +
            "global using System.Net.Http; global using Campus.Tests;", options);
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(System.IO.Path.PathSeparator)
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .Append(typeof(Godot.Node).Assembly.Location).Append(typeof(GdUnit4.TestCaseAttribute).Assembly.Location)
            .Where(path => path.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Where(IsManaged)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("G4.Source", trees.Append(usings), references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        return trees.Select((tree, index) => new SourceFile(paths[index].Relative, tree, compilation.GetSemanticModel(tree))).ToArray();
    }

    internal sealed record ProjectSource(string Path, string[] Sources, string[] References);
    private static readonly Lazy<ProjectSource[]> Projects = new(EvaluateProjects);
    internal static ProjectSource[] ProjectItems() => Projects.Value;

    private static ProjectSource[] EvaluateProjects()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? ArchitectureTests.FindRepository();
        var pending = new Queue<string>(Directory.EnumerateFiles(Path.Combine(root, "game"), "*.csproj")
            .Where(path => Path.GetFileName(path) == "Campus.csproj" || path.EndsWith(".Tests.csproj", StringComparison.Ordinal))
            .Append(Path.Combine(root, "scripts", "gates", "arch", "Campus.Arch.Tests.csproj")));
        var projects = new Dictionary<string, ProjectSource>(StringComparer.OrdinalIgnoreCase);
        while (pending.TryDequeue(out var path))
        {
            path = Path.GetFullPath(path);
            if (projects.ContainsKey(path)) continue;
            var project = Evaluate(path);
            projects.Add(path, project);
            foreach (var reference in project.References) pending.Enqueue(reference);
        }
        return projects.Values.OrderBy(project => project.Path, StringComparer.Ordinal).ToArray();
    }

    private static ProjectSource Evaluate(string project)
    {
        var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "msbuild", project, "-nologo", "-getItem:Compile,ProjectReference" }) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not evaluate project sources: " + project);
        var errors = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var error = errors.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new InvalidOperationException($"MSBuild source evaluation failed for {project}: {error}\n{output}");
        using var document = JsonDocument.Parse(output);
        var items = document.RootElement.GetProperty("Items");
        var sources = items.GetProperty("Compile").EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString()!).Where(File.Exists).ToArray();
        var references = items.GetProperty("ProjectReference").EnumerateArray()
            .Where(item => !item.TryGetProperty("ReferenceOutputAssembly", out var outputAssembly) ||
                !string.Equals(outputAssembly.GetString(), "false", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.GetProperty("FullPath").GetString()!).ToArray();
        return new(project, sources, references);
    }

    private static bool IsManaged(string path)
    {
        try { _ = AssemblyName.GetAssemblyName(path); return true; }
        catch (BadImageFormatException) { return false; }
    }

    public static string Name(ITypeSymbol? type) => type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
    public static string Owner(SemanticModel model, SyntaxNode node) => Name(model.GetEnclosingSymbol(node.SpanStart)?.ContainingType);
    public static IMethodSymbol? Method(SourceFile file, InvocationExpressionSyntax call) =>
        file.Model.GetSymbolInfo(call).Symbol as IMethodSymbol;
    public static object? Constant(SemanticModel model, ExpressionSyntax expression)
    {
        var value = model.GetConstantValue(expression);
        return value.HasValue ? value.Value : null;
    }
    public static string CallName(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax member => member.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => ""
    };
    public static bool InNamespace(string name, string space) => name == space || name.StartsWith(space + ".", StringComparison.Ordinal);
    public static bool Inherits(ITypeSymbol? type, string name)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
            if (Name(current) == name) return true;
        return false;
    }

    public static void RequireNoViolations(IEnumerable<string> violations)
    {
        var items = violations.Distinct(StringComparer.Ordinal).ToArray();
        if (items.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, items));
    }
}

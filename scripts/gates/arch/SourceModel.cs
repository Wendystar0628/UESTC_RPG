// Supplies project-owned syntax and semantics to the G4 source rules.
// 用法：由依赖方向和方法规模检查读取，真实项目加载交给 SourceWorkspace。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

internal sealed record SourceFile(string Path, SyntaxTree Tree, SemanticModel Model)
{
    public CompilationUnitSyntax Root => (CompilationUnitSyntax)Tree.GetRoot();
    public bool IsTest => SourceModel.IsTestPath(Path);
    public string At(SyntaxNode node, string message) => $"{Path}:{Tree.GetLineSpan(node.Span).StartLinePosition.Line + 1}: {message}";
}

internal static class SourceModel
{
    private static readonly Lazy<SourceWorkspace> Model = new(() => SourceWorkspace.Load(RepositoryRoot()));
    public static IEnumerable<Project> Projects => Model.Value.Solution.Projects;
    public static IEnumerable<SourceFile> Production => Model.Value.Files.Where(file => !file.IsTest);
    internal static string RepositoryRoot() => Path.GetFullPath(
        Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? ArchitectureTests.FindRepository());
    internal static string RelativePath(string path) => Path.IsPathFullyQualified(path)
        ? Path.GetRelativePath(RepositoryRoot(), path).Replace('\\', '/') : path.Replace('\\', '/');
    internal static bool IsTestPath(string path)
    {
        path = RelativePath(path);
        return path.StartsWith("game/testing/", StringComparison.Ordinal) || path.Contains("/tests/", StringComparison.Ordinal) ||
            path.StartsWith("scripts/gates/arch/", StringComparison.Ordinal);
    }

    public static string Name(ITypeSymbol? type) => type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
    public static void RequireNoViolations(IEnumerable<string> violations)
    {
        var items = violations.Distinct(StringComparer.Ordinal).ToArray();
        if (items.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, items));
    }
}

// Supplies project-owned syntax and semantics to the G4 source rules.
// 用法：由架构与语义规则读取，真实项目加载交给 SourceWorkspace。
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

// Source is required: compiled IL inlines TestTolerance and loses the distinction this gate guards.
internal sealed record SourceFile(string Path, SyntaxTree Tree, SemanticModel Model)
{
    public CompilationUnitSyntax Root => (CompilationUnitSyntax)Tree.GetRoot();
    public bool IsTest => SourceModel.IsTestPath(Path);
    public string At(SyntaxNode node, string message) => $"{Path}:{Tree.GetLineSpan(node.Span).StartLinePosition.Line + 1}: {message}";
}

internal static class SourceModel
{
    private static readonly ConditionalWeakTable<Compilation, ConditionalWeakTable<SyntaxTree, SemanticModel>> DeclarationModels = new();
    private static SemanticModel CachedModel(Compilation compilation, SyntaxTree tree) =>
        DeclarationModels.GetValue(compilation, _ => new()).GetValue(tree, value => compilation.GetSemanticModel(value));

    private static readonly Lazy<SourceWorkspace> Model = new(() => SourceWorkspace.Load(RepositoryRoot()));
    public static IEnumerable<Project> Projects => Model.Value.Solution.Projects;
    public static IEnumerable<SourceFile> Production => Model.Value.Files.Where(file => !file.IsTest);
    public static IEnumerable<SourceFile> Tests => Model.Value.Files.Where(file => file.IsTest);
    public static Compilation ForProject(Project project) => Model.Value.Compilations[project.Id];
    public static IEnumerable<SourceFile> FilesFor(Project project) => Model.Value.Files.Where(file =>
        ReferenceEquals(file.Model.Compilation, ForProject(project)));
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

    internal static SemanticModel DeclarationModel(SemanticModel context, ISymbol symbol, SyntaxNode declaration)
    {
        var tree = declaration.SyntaxTree;
        // Tree identity keeps the current project, macro variant, or regression fixture's semantics.
        if (context.Compilation.ContainsSyntaxTree(tree)) return ReferenceEquals(context.SyntaxTree, tree) ? context : CachedModel(context.Compilation, tree);
        // A source declaration's tree ownership can differ from the symbol's containing assembly.
        var owners = Model.Value.Compilations.Values.Where(compilation => compilation.ContainsSyntaxTree(tree)).ToArray();
        if (owners.Length == 1) return CachedModel(owners[0], tree);
        if (owners.Length == 0) throw new InvalidOperationException(
            $"G4 could not locate the project declaration for {symbol.ToDisplayString()} in {tree.FilePath}: " +
            $"the exact syntax tree belongs to neither context {context.Compilation.Assembly.Identity} nor any cached project compilation.");
        var assembly = symbol.ContainingAssembly?.Identity;
        var matches = owners.Where(compilation => assembly is not null && compilation.Assembly.Identity.Equals(assembly)).ToArray();
        if (matches.Length == 1) return CachedModel(matches[0], tree);
        throw new InvalidOperationException(
            $"G4 could not disambiguate the project declaration for {symbol.ToDisplayString()} in {tree.FilePath}: " +
            $"symbol assembly {assembly}; tree owners {string.Join("; ", owners.Select(compilation => compilation.Assembly.Identity))}.");
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

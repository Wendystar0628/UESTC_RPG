using System.Text.Json;
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class SourceSizeGateTests
{
    private const int MethodLineLimit = 80;

    [TestCase]
    [TestCategory("Contract")]
    public void HandwrittenMethodsAndConstructorsStayWithinLineLimit()
    {
        var errors = new List<string>();
        foreach (var file in HandwrittenSources(SourceModel.Production))
        foreach (var node in file.Root.DescendantNodes().Where(node => node is BaseMethodDeclarationSyntax or
            LocalFunctionStatementSyntax or AccessorDeclarationSyntax or
            PropertyDeclarationSyntax { ExpressionBody: not null } or IndexerDeclarationSyntax { ExpressionBody: not null }))
        {
            if (IsGenerated(file, node)) continue;
            var lines = PhysicalLines(file.Tree, node) - StringContentLineBreaks(file.Tree, node);
            if (lines <= MethodLineLimit) continue;
            var symbol = file.Model.GetDeclaredSymbol(node);
            errors.Add(file.At(node,
                $"{symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? node.Kind().ToString()} has {lines} method lines; limit {MethodLineLimit}. Split into responsibility phases."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void HandwrittenPartialTypesRequireOneGeneratorOwnedDeclaration()
    {
        var errors = new List<string>();
        foreach (var project in SourceModel.Projects)
        {
            var files = HandwrittenSources(SourceModel.FilesFor(project)).ToDictionary(file => file.Tree);
            foreach (var file in files.Values)
            foreach (var node in file.Root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (!node.Modifiers.Any(SyntaxKind.PartialKeyword) || IsGenerated(file, node)) continue;
                var symbol = file.Model.GetDeclaredSymbol(node)
                    ?? throw new InvalidOperationException(file.At(node, "G4 could not resolve the partial type identity."));
                var declarations = symbol.DeclaringSyntaxReferences.Where(reference => files.ContainsKey(reference.SyntaxTree))
                    .Select(reference => (File: files[reference.SyntaxTree], Node: reference.GetSyntax()))
                    .Where(part => part.Node is TypeDeclarationSyntax && !IsGenerated(part.File, part.Node)).ToArray();
                if (declarations.Length == 1 && (SourceModel.Inherits(symbol, "Godot.GodotObject") ||
                    RequiresCompilerPartial(file, node, symbol))) continue;
                errors.Add(file.At(node,
                    $"{SourceModel.Name(symbol)} uses partial for handwritten responsibilities; split them into independent types. " +
                    "Only one Godot host or compiler-generator input declaration may remain partial."));
            }
        }
        SourceModel.RequireNoViolations(errors);
    }

    private static bool RequiresCompilerPartial(SourceFile file, TypeDeclarationSyntax node, INamedTypeSymbol symbol)
    {
        if (SourceModel.Inherits(symbol, "System.Text.Json.Serialization.JsonSerializerContext") &&
            symbol.GetAttributes().Any(attribute => SourceModel.Name(attribute.AttributeClass) ==
                "System.Text.Json.Serialization.JsonSerializableAttribute")) return true;
        // A generated regex also requires every enclosing type to be partial.
        return node.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(method =>
            method.Body is null && method.ExpressionBody is null &&
            file.Model.GetDeclaredSymbol(method) is IMethodSymbol { IsPartialDefinition: true } generated &&
            SourceModel.Name(generated.ReturnType) == "System.Text.RegularExpressions.Regex" &&
            generated.GetAttributes().Any(attribute => SourceModel.Name(attribute.AttributeClass) ==
                "System.Text.RegularExpressions.GeneratedRegexAttribute"));
    }

    private static IEnumerable<SourceFile> HandwrittenSources(IEnumerable<SourceFile> sources)
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? ArchitectureTests.FindRepository();
        using var thirdParty = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "third_party.json")));
        // Use the existing provenance registry, rather than maintaining a size-gate exception list.
        var externalPaths = thirdParty.RootElement.GetProperty("entries").EnumerateArray()
            .Where(entry => entry.TryGetProperty("path", out _))
            .Select(entry => entry.GetProperty("path").GetString()!.Replace('\\', '/').TrimEnd('/')).ToArray();
        foreach (var file in sources)
        {
            if (externalPaths.Any(path => file.Path.Equals(path, StringComparison.OrdinalIgnoreCase) ||
                file.Path.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase))) continue;
            if (file.Path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                file.Path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
                file.Path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
                file.Path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
                file.Root.GetLeadingTrivia().ToFullString().Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)) continue;
            yield return file;
        }
    }

    private static bool IsGenerated(SourceFile file, SyntaxNode node)
    {
        foreach (var declaration in node.AncestorsAndSelf().Where(item => item is MemberDeclarationSyntax or
            LocalFunctionStatementSyntax or AccessorDeclarationSyntax))
        {
            var symbol = file.Model.GetDeclaredSymbol(declaration);
            if (symbol?.GetAttributes().Any(attribute => SourceModel.Name(attribute.AttributeClass) is
                "System.CodeDom.Compiler.GeneratedCodeAttribute" or
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute") == true) return true;
        }
        return false;
    }

    private static int PhysicalLines(SyntaxTree tree, SyntaxNode node)
    {
        var text = tree.GetText();
        return text.Lines.GetLineFromPosition(node.Span.End - 1).LineNumber -
            text.Lines.GetLineFromPosition(node.Span.Start).LineNumber + 1;
    }

    private static int StringContentLineBreaks(SyntaxTree tree, SyntaxNode node)
    {
        var breaks = 0;
        foreach (var token in node.DescendantTokens())
        {
            if (token.Parent is LiteralExpressionSyntax literal && literal.Kind() is
                    SyntaxKind.StringLiteralExpression or SyntaxKind.Utf8StringLiteralExpression ||
                token.Parent is InterpolatedStringTextSyntax or InterpolatedStringExpressionSyntax)
            {
                var span = tree.GetLineSpan(token.Span);
                breaks += span.EndLinePosition.Line - span.StartLinePosition.Line;
            }
        }
        return breaks;
    }
}

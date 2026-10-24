// Guards behavior-based fixtures and unused controls on product-contract test doubles.
// 用法：由 TestPolicyGateTests 经同一 G4 入口运行，引用查询使用 Roslyn 自带 SymbolFinder。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Campus.Arch.Tests;

internal static class TestBehaviorPolicy
{
    public static IEnumerable<string> ReflectionAndAssertionViolations(SourceFile file)
    {
        foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var method = SourceModel.Method(file, call);
            if (method is not null && SourceModel.Name(method.ContainingType) == "System.Type" &&
                method.Name.StartsWith("Get", StringComparison.Ordinal) &&
                call.ArgumentList.Arguments.Any(argument => NonPublic(file.Model, argument.Expression, new(SymbolEqualityComparer.Default))))
            {
                var target = call.Expression is MemberAccessExpressionSyntax member ? ReflectedType(file.Model, member.Expression) : null;
                if (target is null || !TestOwned(target))
                    yield return file.At(call, "Use public or existing internal behavior; nonpublic reflection is only allowed on a known test-owned type.");
            }
            if (method is not null && method.Name == "AssertThat" &&
                SourceModel.InNamespace(SourceModel.Name(method.ContainingType), "GdUnit4") &&
                call.ArgumentList.Arguments.Count == 1 && call.Ancestors().OfType<InvocationExpressionSyntax>()
                    .Any(outer => SourceModel.CallName(outer) is "IsTrue" or "IsFalse" or "IsEqual" or "IsNotEqual"))
            {
                var value = call.ArgumentList.Arguments[0].Expression;
                if (file.Model.GetConstantValue(value).HasValue && !AuthorityConstant(file.Model, value, new(SymbolEqualityComparer.Default)))
                    yield return file.At(call, "Assert an observed behavior, not a literal-only constant expression.");
            }
        }
    }

    // This is deliberately limited to additional controls on a test double implementing a production contract.
    // Contract implementations, overrides, static factories and framework-attributed methods have other callers.
    internal static IEnumerable<(SourceFile File, MethodDeclarationSyntax Syntax, IMethodSymbol Method)> ExtraDoubleMethods(IEnumerable<SourceFile> files)
    {
        foreach (var file in files.Where(file => file.Path.StartsWith("game/", StringComparison.Ordinal)))
        foreach (var syntax in file.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (file.Model.GetDeclaredSymbol(syntax) is not IMethodSymbol method || method.IsStatic || method.IsOverride ||
                method.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal) || method.GetAttributes().Any(attribute =>
                    SourceModel.InNamespace(SourceModel.Name(attribute.AttributeClass), "GdUnit4"))) continue;
            var type = method.ContainingType;
            if (!type.AllInterfaces.Any(contract => SourceModel.InNamespace(SourceModel.Name(contract), "Campus.Contracts"))) continue;
            if (type.AllInterfaces.SelectMany(contract => contract.GetMembers()).Any(member =>
                SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member), method))) continue;
            yield return (file, syntax, method);
        }
    }

    public static async Task<IReadOnlyList<string>> UnusedDoubleMethods(IEnumerable<SourceFile> files, Solution solution)
    {
        var errors = new List<string>();
        foreach (var (file, syntax, method) in ExtraDoubleMethods(files))
        {
            var references = await SymbolFinder.FindReferencesAsync(method, solution).ConfigureAwait(false);
            if (!references.SelectMany(reference => reference.Locations).Any(reference => reference.Location.IsInSource &&
                !(reference.Location.SourceTree == syntax.SyntaxTree && syntax.Span.Contains(reference.Location.SourceSpan))))
                errors.Add(file.At(syntax, $"Test-double control {method.Name} has no consumer; remove it."));
        }
        return errors;
    }

    private static bool TestOwned(ITypeSymbol type) => type.DeclaringSyntaxReferences.Length != 0 &&
        type.DeclaringSyntaxReferences.All(reference => SourceModel.IsTestPath(reference.SyntaxTree.FilePath));

    private static ITypeSymbol? ReflectedType(SemanticModel model, ExpressionSyntax expression)
    {
        if (expression is TypeOfExpressionSyntax type) return model.GetTypeInfo(type.Type).Type;
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } call &&
            model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "GetType" } method && method.ContainingType.SpecialType == SpecialType.System_Object)
            return model.GetTypeInfo(member.Expression).Type;
        if (model.GetSymbolInfo(expression).Symbol is ILocalSymbol local && Initializer(local) is { } value)
            return ReflectedType(SourceModel.DeclarationModel(model, local, value), value);
        return null;
    }

    private static bool NonPublic(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        if (SourceModel.Name(model.GetTypeInfo(expression).Type) == "System.Reflection.BindingFlags" &&
            model.GetConstantValue(expression) is { HasValue: true, Value: int flags }) return (flags & 32) != 0;
        if (model.GetSymbolInfo(expression).Symbol is ILocalSymbol local && seen.Add(local) && Initializer(local) is { } value)
            return NonPublic(SourceModel.DeclarationModel(model, local, value), value, seen);
        return expression.DescendantNodes().OfType<ExpressionSyntax>().Any(node =>
            model.GetSymbolInfo(node).Symbol is IFieldSymbol { Name: "NonPublic" } field &&
            SourceModel.Name(field.ContainingType) == "System.Reflection.BindingFlags");
    }

    private static bool AuthorityConstant(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        foreach (var node in expression.DescendantNodesAndSelf().OfType<ExpressionSyntax>())
        {
            var symbol = model.GetSymbolInfo(node).Symbol;
            if (symbol is IFieldSymbol { IsConst: true } field &&
                (field.ContainingType.TypeKind == TypeKind.Enum || field.DeclaringSyntaxReferences.Length == 0 ||
                 field.DeclaringSyntaxReferences.All(reference => !SourceModel.IsTestPath(reference.SyntaxTree.FilePath)))) return true;
            if (symbol is ILocalSymbol { IsConst: true } local && seen.Add(local) && Initializer(local) is { } value &&
                AuthorityConstant(SourceModel.DeclarationModel(model, local, value), value, seen)) return true;
        }
        return false;
    }

    private static ExpressionSyntax? Initializer(ILocalSymbol symbol) => symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
        .OfType<VariableDeclaratorSyntax>().Select(variable => variable.Initializer?.Value).FirstOrDefault();
}

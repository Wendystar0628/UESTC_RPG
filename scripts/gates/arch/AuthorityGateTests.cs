using System.Text.RegularExpressions;
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class AuthorityGateTests
{
    [TestCase]
    [TestCategory("Contract")]
    public void PublicFunctionFamiliesOnlyHaveTheirAuthoritativeDefinitions()
    {
        var errors = new List<string>();
        foreach (var file in SourceModel.Production)
        foreach (var syntax in file.Root.DescendantNodes().Where(node => node is MethodDeclarationSyntax or LocalFunctionStatementSyntax))
        {
            if (file.Model.GetDeclaredSymbol(syntax) is not IMethodSymbol method) continue;
            foreach (var family in PublicFunctionIndex.All.Where(f => f.Names.Contains(method.Name, StringComparer.Ordinal) && f.Shape(method)))
                if (!family.Owners.Contains(SourceModel.Name(method.ContainingType), StringComparer.Ordinal))
                    errors.Add(file.At(syntax, $"{method.Name} belongs to {string.Join(" / ", family.Owners)} for this parameter family."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void NodesAreNotLookedUpByNamesOrPaths()
    {
        var errors = new List<string>();
        string[] forbidden = ["GetNode", "GetNodeOrNull", "GetNodeAndResource", "HasNode", "HasNodeAndResource", "FindChild", "FindParent"];
        foreach (var file in SourceModel.Production)
        {
            foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var method = SourceModel.Method(file, call);
                if (method is null || SourceModel.Name(method.ContainingType) != "Godot.Node") continue;
                if (forbidden.Contains(method.Name, StringComparer.Ordinal))
                    errors.Add(file.At(call, "Pass a node reference; node-name/path lookup is forbidden."));
                if (method.Name == "FindChildren" && (call.ArgumentList.Arguments.Count < 2 ||
                    SourceModel.Constant(file.Model, call.ArgumentList.Arguments[0].Expression) is not "*" ||
                    SourceModel.Constant(file.Model, call.ArgumentList.Arguments[1].Expression) is not string { Length: > 0 }))
                    errors.Add(file.At(call, "FindChildren is only a wildcard traversal filtered by engine type."));
            }
            foreach (var name in file.Root.DescendantNodes().OfType<ExpressionSyntax>().Where(node =>
                node is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Name" } or IdentifierNameSyntax { Identifier.ValueText: "Name" }))
            {
                if (file.Model.GetSymbolInfo(name).Symbol is not IPropertySymbol { Name: "Name" } property ||
                    SourceModel.Name(property.ContainingType) != "Godot.Node") continue;
                if (name.Ancestors().Any(node => node is BinaryExpressionSyntax comparison &&
                    comparison.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression) ||
                    name.Ancestors().OfType<InvocationExpressionSyntax>().Any(call => SourceModel.CallName(call) is
                        "Equals" or "StartsWith" or "EndsWith" or "Contains" or "IndexOf" or "CompareTo"))
                    errors.Add(file.At(name, "Do not identify a node by its Name; retain its reference or contract."));
            }
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void ShaderGlobalWritesUseTheRegisteredSoleWriter()
    {
        var registry = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var file in SourceModel.Production)
        foreach (var field in file.Root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (field.Parent is not TypeDeclarationSyntax type ||
                SourceModel.Name(file.Model.GetDeclaredSymbol(type)) != "Campus.Contracts.ShaderGlobals") continue;
            var comment = field.GetLeadingTrivia().ToFullString();
            var writer = Regex.Match(comment, @"Sole writer:\s*([A-Za-z_][A-Za-z0-9_.]*)");
            if (!writer.Success && !comment.Contains("Reserved:", StringComparison.Ordinal))
                throw new InvalidOperationException(file.At(field, "Shader global has no sole-writer or Reserved registration."));
            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Initializer is null || SourceModel.Constant(file.Model, variable.Initializer.Value) is not string key)
                    throw new InvalidOperationException(file.At(variable, "ShaderGlobals entries must be named string constants."));
                registry.Add(key, writer.Success ? writer.Groups[1].Value.TrimEnd('.') : null);
            }
        }
        if (registry.Count == 0) throw new InvalidOperationException("ShaderGlobals registration is missing.");
        var errors = new List<string>();
        foreach (var file in SourceModel.Production)
        foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var method = SourceModel.Method(file, call);
            if (method is null || SourceModel.Name(method.ContainingType) is not ("Godot.RenderingServer" or "Godot.RenderingServerInstance")) continue;
            if (method.Name == "GlobalShaderParameterAdd")
                errors.Add(file.At(call, "Runtime shader-global registration is forbidden."));
            if (method.Name != "GlobalShaderParameterSet") continue;
            var keys = ResolveKeys(file.Model, call.ArgumentList.Arguments[0].Expression, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
            if (keys is null || keys.Length == 0 || keys.Any(key => !registry.TryGetValue(key, out var writer) || writer is null || writer != SourceModel.Owner(file.Model, call)))
                errors.Add(file.At(call, $"Global {(keys is null ? "<unresolved>" : string.Join(" / ", keys))} may only be written by its ShaderGlobals sole writer."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    private static string[]? ResolveKeys(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        if (SourceModel.Constant(model, expression) is string value) return [value];
        if (expression is ParenthesizedExpressionSyntax parentheses) return ResolveKeys(model, parentheses.Expression, seen);
        if (expression is CastExpressionSyntax cast) return ResolveKeys(model, cast.Expression, seen);
        if (expression is BaseObjectCreationExpressionSyntax { ArgumentList: { Arguments.Count: 1 } arguments })
            return ResolveKeys(model, arguments.Arguments[0].Expression, seen);
        if (expression is ElementAccessExpressionSyntax element && ArrayEntries(model, element.Expression, seen) is { } array)
        {
            var index = element.ArgumentList.Arguments.Count == 1 ? SourceModel.Constant(model, element.ArgumentList.Arguments[0].Expression) : null;
            if (index is int at) return at >= 0 && at < array.Values.Length
                ? ResolveKeys(array.Model, array.Values[at], seen) : null;
            // A dynamic index can select any entry; every possible key must have the same approved writer.
            return ResolveEntries(array.Model, array.Values, seen);
        }
        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (symbol is null || !seen.Add(symbol)) return null;
        if (!StableAlias(model, symbol)) return null;
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is VariableDeclaratorSyntax { Initializer: { } initializer })
                return ResolveKeys(model.Compilation.GetSemanticModel(initializer.SyntaxTree), initializer.Value, seen);
            if (reference.GetSyntax() is ForEachStatementSyntax loop && ArrayEntries(model, loop.Expression, seen) is { } entries)
                return ResolveEntries(entries.Model, entries.Values, seen);
        }
        return null;
    }

    private static string[]? ResolveEntries(SemanticModel model, ExpressionSyntax[] values, HashSet<ISymbol> seen)
    {
        var keys = new List<string>();
        foreach (var value in values)
        {
            var resolved = ResolveKeys(model, value, new HashSet<ISymbol>(seen, SymbolEqualityComparer.Default));
            if (resolved is null) return null;
            keys.AddRange(resolved);
        }
        return keys.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static (SemanticModel Model, ExpressionSyntax[] Values)? ArrayEntries(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        if (expression is ParenthesizedExpressionSyntax parentheses) return ArrayEntries(model, parentheses.Expression, seen);
        var values = expression switch
        {
            ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions.ToArray(),
            ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions.ToArray(),
            CollectionExpressionSyntax collection when collection.Elements.All(element => element is ExpressionElementSyntax) =>
                collection.Elements.OfType<ExpressionElementSyntax>().Select(element => element.Expression).ToArray(),
            _ => null
        };
        if (values is not null) return (model, values);
        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (symbol is null || !seen.Add(symbol) || !StableAlias(model, symbol)) return null;
        foreach (var reference in symbol.DeclaringSyntaxReferences)
            if (reference.GetSyntax() is VariableDeclaratorSyntax { Initializer: { } initializer })
                return ArrayEntries(model.Compilation.GetSemanticModel(initializer.SyntaxTree), initializer.Value, seen);
        return null;
    }

    private static bool StableAlias(SemanticModel model, ISymbol symbol)
    {
        if (symbol is IFieldSymbol field) return field.IsConst || field.IsReadOnly;
        if (symbol is not ILocalSymbol) return false;
        var declaration = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (declaration is null) return false;
        var scope = declaration.Ancestors().FirstOrDefault(node => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax)
            ?? declaration.SyntaxTree.GetRoot();
        var source = model.Compilation.GetSemanticModel(scope.SyntaxTree);
        return !scope.DescendantNodes().Any(node => node switch
        {
            AssignmentExpressionSyntax assignment => SameTarget(assignment.Left),
            PrefixUnaryExpressionSyntax prefix when prefix.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression => SameTarget(prefix.Operand),
            PostfixUnaryExpressionSyntax postfix when postfix.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression => SameTarget(postfix.Operand),
            ArgumentSyntax argument when argument.RefKindKeyword.Kind() is SyntaxKind.RefKeyword or SyntaxKind.OutKeyword => SameTarget(argument.Expression),
            _ => false
        });
        bool SameTarget(ExpressionSyntax target) => SymbolEqualityComparer.Default.Equals(source.GetSymbolInfo(target is ElementAccessExpressionSyntax element ? element.Expression : target).Symbol, symbol);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void CameraPoseWritesStayWithinArrivalAndPresentationOwners()
    {
        var errors = new List<string>();
        string[] spaces = ["Campus.Flow", "Campus.Camera.Cinematic", "Campus.Kernel.Boot", "Campus.Kernel.Maps", "Campus.Kernel.Inspect", "Campus.Gates"];
        string[] types = ["Campus.Camera.ObserverCamera", "Campus.Camera.TransitDriver"];
        foreach (var file in SourceModel.Production)
        foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var method = SourceModel.Method(file, call);
            if (method?.Name != "SetPose" || !(SourceModel.Name(method.ContainingType) == "Campus.Contracts.ICameraRig" ||
                method.ContainingType.AllInterfaces.Any(item => SourceModel.Name(item) == "Campus.Contracts.ICameraRig"))) continue;
            var owner = SourceModel.Owner(file.Model, call);
            if (!types.Contains(owner, StringComparer.Ordinal) && !spaces.Any(space => SourceModel.InNamespace(owner, space)))
                errors.Add(file.At(call, "ICameraRig.SetPose is reserved for arrival, cinematic presentation, restore and inspection."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void ContractConsumersDoNotDowncastToConcreteImplementations()
    {
        var errors = new List<string>();
        foreach (var file in SourceModel.Production)
        {
            foreach (var cast in file.Root.DescendantNodes().OfType<CastExpressionSyntax>())
                Check(cast, file.Model.GetTypeInfo(cast.Expression).Type, file.Model.GetTypeInfo(cast.Type).Type);
            foreach (var cast in file.Root.DescendantNodes().OfType<BinaryExpressionSyntax>().Where(item => item.IsKind(SyntaxKind.AsExpression)))
                Check(cast, file.Model.GetTypeInfo(cast.Left).Type, file.Model.GetTypeInfo(cast.Right).Type);
            foreach (var test in file.Root.DescendantNodes().OfType<IsPatternExpressionSyntax>())
            foreach (var pattern in test.Pattern.DescendantNodesAndSelf())
            {
                var type = pattern switch
                {
                    DeclarationPatternSyntax declaration => declaration.Type,
                    TypePatternSyntax declaration => declaration.Type,
                    RecursivePatternSyntax declaration => declaration.Type,
                    _ => null
                };
                if (type is not null) Check(pattern, file.Model.GetTypeInfo(test.Expression).Type, file.Model.GetTypeInfo(type).Type);
            }
            foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var method = SourceModel.Method(file, call);
                if (method?.Name is not ("Cast" or "OfType") || method.TypeArguments.Length != 1 ||
                    SourceModel.Name(method.ContainingType) != "System.Linq.Enumerable") continue;
                var receiver = method.ReducedFrom is not null && call.Expression is MemberAccessExpressionSyntax member
                    ? member.Expression : call.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (receiver is null) continue;
                var source = file.Model.GetTypeInfo(receiver).Type as INamedTypeSymbol;
                if (source is null) continue;
                var enumerable = source.AllInterfaces.Prepend(source).FirstOrDefault(item => item.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
                if (enumerable is not null) Check(call, enumerable.TypeArguments[0], method.TypeArguments[0]);
            }
            void Check(SyntaxNode node, ITypeSymbol? from, ITypeSymbol? to)
            {
                if (from is { TypeKind: TypeKind.Interface } && SourceModel.InNamespace(from.ContainingNamespace.ToDisplayString(), "Campus.Contracts") &&
                    to is { TypeKind: TypeKind.Class or TypeKind.Struct } && SourceModel.InNamespace(to.ContainingNamespace.ToDisplayString(), "Campus") &&
                    !SourceModel.InNamespace(to.ContainingNamespace.ToDisplayString(), "Campus.Contracts"))
                    errors.Add(file.At(node, $"Publish the needed member on {SourceModel.Name(from)}; do not downcast to {SourceModel.Name(to)}."));
            }
        }
        SourceModel.RequireNoViolations(errors);
    }
}

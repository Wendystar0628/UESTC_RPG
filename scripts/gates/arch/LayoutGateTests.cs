using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class LayoutGateTests
{
    [TestCase]
    [TestCategory("Contract")]
    public void PagesUseContainersAndAnchorsInsteadOfPixelLayouts()
    {
        var errors = new List<string>();
        foreach (var file in SourceModel.Production)
        {
            foreach (var assignment in file.Root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (file.Model.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol property ||
                    !SourceModel.Inherits(property.ContainingType, "Godot.Control")) continue;
                if (!IsPageLayout(file, assignment)) continue;
                if (property.Name is "Position" or "GlobalPosition" or "OffsetLeft" or "OffsetTop" or "OffsetRight" or "OffsetBottom")
                    errors.Add(file.At(assignment, "A page is positioned by its parent container or anchors."));
                if (property.Name is "Size" or "CustomMinimumSize" or "CustomMaximumSize" && HasPixelLiteral(file.Model, assignment.Right))
                    errors.Add(file.At(assignment, "Page dimensions come from content or UI metrics, not pixel literals."));
            }
            foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var method = SourceModel.Method(file, call);
                if (method is null || !IsPageLayout(file, call) || !SourceModel.Inherits(method.ContainingType, "Godot.Control")) continue;
                if (method.Name is "SetPosition" or "SetGlobalPosition" or "SetOffset" or "SetBegin" or "SetEnd")
                    errors.Add(file.At(call, "Page positions come from parent containers or anchors."));
                if (method.Name == "SetSize" && call.ArgumentList.Arguments.Count > 0 && HasPixelLiteral(file.Model, call.ArgumentList.Arguments[0].Expression))
                    errors.Add(file.At(call, "Page dimensions come from content or UI metrics."));
            }
        }
        SourceModel.RequireNoViolations(errors);
    }

    private static bool IsPageLayout(SourceFile file, SyntaxNode node)
    {
        var owner = file.Model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
        var space = owner?.ContainingNamespace.ToDisplayString() ?? "";
        if (SourceModel.InNamespace(space, "Campus.Hud.Pages")) return true;
        // A map's self-drawn marker/player layer uses map-space transforms, not page layout.
        return SourceModel.InNamespace(space, "Campus.Hud.Map") &&
            owner?.GetMembers("_Draw").OfType<IMethodSymbol>().Any(method => method.IsOverride) != true;
    }

    private static bool HasPixelLiteral(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol>? seen = null)
    {
        // Only complete dimension values are checked. Metric/2 is a ratio, not a two-pixel dimension.
        if (expression is BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments })
            return arguments.Arguments.Any(argument => HasPixelLiteral(model, argument.Expression, seen));
        var value = model.GetConstantValue(expression);
        if (!value.HasValue)
        {
            var symbol = model.GetSymbolInfo(expression).Symbol;
            seen ??= new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            if (symbol is ILocalSymbol or IFieldSymbol && seen.Add(symbol) && symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is
                VariableDeclaratorSyntax { Initializer: { } initializer })
                return HasPixelLiteral(SourceModel.DeclarationModel(model, symbol, initializer), initializer.Value, seen);
            return false;
        }
        if (value.Value is not (int or long or float or double or decimal)) return false;
        var number = Convert.ToDouble(value.Value, System.Globalization.CultureInfo.InvariantCulture);
        // Zero means content size; -1 means unconstrained. They are layout sentinels.
        return number is not (0 or -1);
    }
}

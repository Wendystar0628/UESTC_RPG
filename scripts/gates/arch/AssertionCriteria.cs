using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

// Distinguish an approximation's error margin from a shape's extent or a required minimum separation.
internal static class AssertionCriteria
{
    public static bool IsComparison(BinaryExpressionSyntax node) => node.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression or
        SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression;

    public static ExpressionSyntax? UpperErrorBound(SourceFile file, BinaryExpressionSyntax comparison)
    {
        var left = IsError(file.Model, comparison.Left, NewSeen());
        var right = IsError(file.Model, comparison.Right, NewSeen());
        if (left == right) return null; // Relative optimality/error-to-error comparisons are not a numeric tolerance.
        var expected = ExpectedTruth(comparison);
        if (expected is null) return null;
        var less = comparison.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression;
        if ((left ? less : !less) != expected.Value) return null; // e > minimum asserted true is a lower bound.
        return left ? comparison.Right : comparison.Left;
    }

    private static bool? ExpectedTruth(SyntaxNode node)
    {
        var assertion = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault(call => SourceModel.CallName(call) == "AssertThat");
        SyntaxNode boundary;
        bool truth;
        if (assertion is not null)
        {
            var result = assertion.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault(call => SourceModel.CallName(call) is "IsTrue" or "IsFalse");
            if (result is null) return null;
            truth = SourceModel.CallName(result) == "IsTrue";
            boundary = assertion;
        }
        else
        {
            var guard = node.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault(item => item.Condition.Span.Contains(node.Span) &&
                item.Statement.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>().Any());
            if (guard is null) return null;
            truth = false; // A successful guard does not take its throw branch.
            boundary = guard;
        }
        foreach (var ancestor in node.Ancestors().TakeWhile(item => item != boundary))
            if (ancestor.IsKind(SyntaxKind.LogicalNotExpression)) truth = !truth;
        return truth;
    }

    public static bool IsAssertionOrGuard(SyntaxNode node) => node.AncestorsAndSelf().Any(ancestor =>
        ancestor is InvocationExpressionSyntax call && SourceModel.CallName(call) is "AssertThat" or "AssertThrown" or "Assert" or "IsEqualApprox" ||
        ancestor is IfStatementSyntax guard && guard.Condition.Span.Contains(node.Span) && guard.Statement.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>().Any());

    private static bool IsError(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        expression = Unwrap(expression);
        if (expression is InvocationExpressionSyntax call)
        {
            if (SourceModel.CallName(call) == "Abs" && call.ArgumentList.Arguments.Any(argument => IsDifference(model, argument.Expression, NewSeen()))) return true;
            if (SourceModel.CallName(call) is "Length" or "LengthSquared" && call.Expression is MemberAccessExpressionSyntax member &&
                IsDifference(model, member.Expression, NewSeen())) return true;
            // Bare Distance(a,b) is also used for extents, selecting nearby colliders and minimum clearance.
            // An explicit vector difference (or an approximation assertion) expresses numerical error unambiguously.
            if (SourceModel.CallName(call) is "Distance" or "DistanceSquared" &&
                call.ArgumentList.Arguments.Any(argument => IsDifference(model, argument.Expression, NewSeen()))) return true;
        }
        var symbol = model.GetSymbolInfo(expression).Symbol;
        return symbol is ILocalSymbol && seen.Add(symbol) && Initializer(symbol) is { } value &&
            IsError(SourceModel.DeclarationModel(model, symbol, value), value, seen);
    }

    private static bool IsDifference(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        expression = Unwrap(expression);
        if (expression.IsKind(SyntaxKind.SubtractExpression)) return true;
        if (expression is InvocationExpressionSyntax call && model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "SignedDelta" } method &&
            SourceModel.Name(method.ContainingType) == "Campus.Contracts.Angles") return true;
        var symbol = model.GetSymbolInfo(expression).Symbol;
        return symbol is ILocalSymbol && seen.Add(symbol) && Initializer(symbol) is { } value &&
            IsDifference(SourceModel.DeclarationModel(model, symbol, value), value, seen);
    }

    public static bool HasLiteralCriterion(SourceFile file, ExpressionSyntax expression) => HasLiteral(file.Model, expression, NewSeen());

    private static bool HasLiteral(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        expression = Unwrap(expression);
        if (expression is PrefixUnaryExpressionSyntax unary) return HasLiteral(model, unary.Operand, seen);
        if (IsRuleField(model, expression)) return false;
        if (expression is LiteralExpressionSyntax literal) return literal.IsKind(SyntaxKind.NumericLiteralExpression);
        if (expression is BinaryExpressionSyntax encoding && encoding.IsKind(SyntaxKind.DivideExpression) &&
            encoding.Left is LiteralExpressionSyntax coefficient && coefficient.IsKind(SyntaxKind.NumericLiteralExpression) &&
            model.GetSymbolInfo(encoding.Right).Symbol is IFieldSymbol { IsConst: true, Name: "SeasonQuantizationMax" } limit &&
            SourceModel.Name(limit.ContainingType) == "Campus.Gen.Diorama.DioramaVertex") return false;
        if (expression is BinaryExpressionSyntax binary)
        {
            // Relative normalization such as sharedTolerance * Max(1,Abs(expected)) reuses one criterion.
            if (binary.Kind() is SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression && HasRule(model, binary.Left, NewSeen())) return false;
            if (binary.IsKind(SyntaxKind.MultiplyExpression) && HasRule(model, binary.Right, NewSeen())) return false;
            return HasLiteral(model, binary.Left, seen) || HasLiteral(model, binary.Right, seen);
        }
        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (symbol is ILocalSymbol or IFieldSymbol or IParameterSymbol && seen.Add(symbol) && Initializer(symbol) is { } value)
        {
            var origin = SourceModel.DeclarationModel(model, symbol, value);
            // Do not chase a fixture's computed size/distance back into its input coordinates.
            if (SourceModel.Constant(origin, value) is null && !HasRule(origin, value, NewSeen()) && !IsCriterionCalculation(value)) return false;
            return HasLiteral(origin, value, seen);
        }
        if (expression is InvocationExpressionSyntax call && SourceModel.CallName(call) is "Max" or "Min" or "Clamp")
            return call.ArgumentList.Arguments.Any(argument => HasLiteral(model, argument.Expression, seen));
        return false;
    }

    private static bool HasRule(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> seen)
    {
        expression = Unwrap(expression);
        if (IsRuleField(model, expression)) return true;
        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (symbol is ILocalSymbol or IFieldSymbol or IParameterSymbol && seen.Add(symbol) && Initializer(symbol) is { } value)
            return HasRule(SourceModel.DeclarationModel(model, symbol, value), value, seen);
        return expression.DescendantNodes().OfType<ExpressionSyntax>().Any(child => IsRuleField(model, child));
    }

    private static bool IsCriterionCalculation(ExpressionSyntax expression) => expression is BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or
        ParenthesizedExpressionSyntax or CastExpressionSyntax || expression is InvocationExpressionSyntax call && SourceModel.CallName(call) is "Min" or "Max" or "Clamp";
    private static bool IsRuleField(SemanticModel model, ExpressionSyntax expression) => model.GetSymbolInfo(expression).Symbol is IFieldSymbol field &&
        (SourceModel.Name(field.ContainingType) == "Campus.Tests.TestTolerance" ||
            field.IsConst && !field.DeclaringSyntaxReferences.Any(reference => SourceModel.IsTestPath(reference.SyntaxTree.FilePath)));
    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parentheses => Unwrap(parentheses.Expression),
        CastExpressionSyntax cast => Unwrap(cast.Expression),
        _ => expression
    };
    private static ExpressionSyntax? Initializer(ISymbol symbol) => symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
        .Select(node => node switch { VariableDeclaratorSyntax variable => variable.Initializer?.Value, ParameterSyntax parameter => parameter.Default?.Value, _ => null }).FirstOrDefault();
    private static HashSet<ISymbol> NewSeen() => new(SymbolEqualityComparer.Default);
}

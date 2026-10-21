// Recognizes identity and format branches from declaration attributes and their data flow.
// 用法：本批由 SemanticPolicyRegressionTests 调用；全仓入口待 TOOL-227 接入。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Campus.Arch.Tests;

internal sealed class DataBranchPolicy(SourceFile file)
{
    [Flags]
    private enum Fact { None = 0, Id = 1, Version = 2 }

    public IEnumerable<string> Violations()
    {
        foreach (var comparison in file.Root.DescendantNodes().OfType<BinaryExpressionSyntax>().Where(node =>
            node.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression or SyntaxKind.LessThanExpression or
                SyntaxKind.LessThanOrEqualExpression or SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression))
        {
            var facts = Facts(comparison.Left) | Facts(comparison.Right);
            if (facts.HasFlag(Fact.Id) && (StringLiteral(comparison.Left) || StringLiteral(comparison.Right)))
                yield return file.At(comparison, "Identity comparisons read the data/catalog declaration; no string-literal special cases (STD-CODE).");
            if (facts.HasFlag(Fact.Version) && !RejectsOnly(comparison))
                yield return file.At(comparison, "Data versions may reject unsupported input; they must not select multiple readers (STD-CODE).");
        }
        foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (file.Model.GetOperation(call) is not IInvocationOperation operation || operation.TargetMethod.Name is not
                ("Equals" or "Compare" or "CompareTo" or "StartsWith" or "EndsWith" or "Contains" or "IndexOf" or "LastIndexOf")) continue;
            var operands = call.ArgumentList.Arguments.Select(argument => argument.Expression)
                .Concat(call.Expression is MemberAccessExpressionSyntax member ? [member.Expression] : []).ToArray();
            if (operands.Any(operand => Facts(operand).HasFlag(Fact.Id)) && operands.Any(StringLiteral))
                yield return file.At(call, "Identity equality/order checks read the catalog declaration; no string literals (STD-CODE).");
            if (operands.Any(operand => Facts(operand).HasFlag(Fact.Version)) && !RejectsOnly(call))
                yield return file.At(call, "Version comparison must be a rejecting guard, not a reader selector (STD-CODE).");
        }
        // Named constants can parse as binary IsExpression syntax, then bind as an is-pattern operation.
        foreach (var match in file.Root.DescendantNodes().OfType<ExpressionSyntax>().Where(node =>
            node is IsPatternExpressionSyntax || node.IsKind(SyntaxKind.IsExpression)))
        {
            if (file.Model.GetOperation(match) is not IIsPatternOperation operation) continue;
            var facts = Facts((ExpressionSyntax)operation.Value.Syntax);
            if (facts.HasFlag(Fact.Id) && match.DescendantNodes().OfType<ConstantPatternSyntax>().Any(pattern => StringLiteral(pattern.Expression)))
                yield return file.At(match, "Identity patterns reference catalog declarations instead of string literals (STD-CODE).");
            if (facts.HasFlag(Fact.Version) && !RejectsOnly(match))
                yield return file.At(match, "Version patterns may reject unsupported input, not select readers (STD-CODE).");
        }
        foreach (var selection in file.Root.DescendantNodes().OfType<SwitchStatementSyntax>())
        {
            var facts = Facts(selection.Expression);
            if (facts.HasFlag(Fact.Id) && selection.Sections.SelectMany(section => section.Labels).Any(label =>
                label.DescendantNodes().OfType<LiteralExpressionSyntax>().Any(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))))
                yield return file.At(selection, "Identity switches reference catalog declarations instead of string literals (STD-CODE).");
            if (facts.HasFlag(Fact.Version) && selection.Sections.Count(section => !Rejecting(section.Statements.LastOrDefault())) > 1)
                yield return file.At(selection, "A version switch has multiple accepted paths; retain the current reader and reject others (STD-CODE).");
        }
        foreach (var selection in file.Root.DescendantNodes().OfType<SwitchExpressionSyntax>())
        {
            var facts = Facts(selection.GoverningExpression);
            if (facts.HasFlag(Fact.Id) && selection.Arms.Any(arm => arm.Pattern.DescendantNodesAndSelf().OfType<ConstantPatternSyntax>().Any(pattern => StringLiteral(pattern.Expression))))
                yield return file.At(selection, "Identity switch arms reference catalog declarations instead of string literals (STD-CODE).");
            if (facts.HasFlag(Fact.Version) && selection.Arms.Count(arm => arm.Expression is not ThrowExpressionSyntax) > 1)
                yield return file.At(selection, "A version expression has multiple accepted paths (STD-CODE).");
        }
    }

    private Fact Facts(ExpressionSyntax expression) => Facts(file.Model, expression, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

    private Fact Facts(SemanticModel model, ExpressionSyntax expression, HashSet<ISymbol> visiting)
    {
        var symbol = model.GetSymbolInfo(expression).Symbol;
        var type = model.GetTypeInfo(expression).Type;
        var result = Marked(symbol) | Marked(type);
        if (symbol is not null && visiting.Add(symbol))
        {
            foreach (var reference in symbol.DeclaringSyntaxReferences)
            {
                var syntax = reference.GetSyntax();
                var sourceModel = model.Compilation.GetSemanticModel(syntax.SyntaxTree);
                foreach (var value in DefinitionValues(syntax)) result |= Facts(sourceModel, value, visiting);
            }
            // A local can receive a fact after declaration. This is provenance, not a runtime value analysis.
            if (symbol is ILocalSymbol)
            {
                var owner = expression.Ancestors().FirstOrDefault(node => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);
                if (owner is not null)
                    foreach (var assignment in owner.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                        if (SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, symbol))
                            result |= Facts(model, assignment.Right, visiting);
            }
            visiting.Remove(symbol);
        }
        if (expression is MemberAccessExpressionSyntax member) result |= Facts(model, member.Expression, visiting);
        if (expression is ParenthesizedExpressionSyntax group) result |= Facts(model, group.Expression, visiting);
        if (expression is CastExpressionSyntax cast) result |= Facts(model, cast.Expression, visiting);
        if (expression is PostfixUnaryExpressionSyntax suppressed && suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            result |= Facts(model, suppressed.Operand, visiting);
        if (expression is ConditionalExpressionSyntax choice) result |= Facts(model, choice.WhenTrue, visiting) | Facts(model, choice.WhenFalse, visiting);
        if (expression is BinaryExpressionSyntax binary && binary.Kind() is not (SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression or
            SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression or SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression))
            result |= Facts(model, binary.Left, visiting) | Facts(model, binary.Right, visiting);
        if (expression is ElementAccessExpressionSyntax element) result |= Facts(model, element.Expression, visiting);
        if (expression is PrefixUnaryExpressionSyntax unary && !unary.IsKind(SyntaxKind.LogicalNotExpression)) result |= Facts(model, unary.Operand, visiting);
        // A declaration marked on the method's return is authoritative; argument names are irrelevant.
        if (expression is InvocationExpressionSyntax call && model.GetSymbolInfo(call).Symbol is IMethodSymbol method)
            result |= Marked(method) | Marked(method.ReturnType);
        return result;
    }

    private static Fact Marked(ISymbol? symbol) =>
        (SemanticPolicySources.Has(symbol, "DataId") ? Fact.Id : Fact.None) |
        (SemanticPolicySources.Has(symbol, "DataVersion") ? Fact.Version : Fact.None) |
        (symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? Marked(nullable.TypeArguments[0]) : Fact.None);

    private static IEnumerable<ExpressionSyntax> DefinitionValues(SyntaxNode syntax)
    {
        if (syntax is VariableDeclaratorSyntax { Initializer: { } initializer }) yield return initializer.Value;
        if (syntax is ForEachStatementSyntax iteration) yield return iteration.Expression;
        if (syntax is PropertyDeclarationSyntax property)
        {
            if (property.Initializer is { } value) yield return value.Value;
            if (property.ExpressionBody is { } body) yield return body.Expression;
            foreach (var returned in property.AccessorList?.DescendantNodes().OfType<ReturnStatementSyntax>() ?? [])
                if (returned.Expression is { } expression) yield return expression;
        }
        if (syntax is MethodDeclarationSyntax method)
        {
            if (method.ExpressionBody is { } body) yield return body.Expression;
            foreach (var returned in method.Body?.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax).OfType<ReturnStatementSyntax>() ?? [])
                if (returned.Expression is { } expression) yield return expression;
        }
    }

    private bool StringLiteral(ExpressionSyntax expression) => StringLiteral(expression, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

    private bool StringLiteral(ExpressionSyntax expression, HashSet<ISymbol> visiting)
    {
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)) return true;
        if (expression is ParenthesizedExpressionSyntax group) return StringLiteral(group.Expression, visiting);
        if (expression is CastExpressionSyntax cast) return StringLiteral(cast.Expression, visiting);
        if (expression is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax && Facts(expression).HasFlag(Fact.Id) &&
            file.Model.GetOperation(expression) is IObjectCreationOperation created)
            return created.Arguments.Any(argument => argument.Value.Syntax is ExpressionSyntax value && StringLiteral(value, visiting));
        // Catalog fields/properties remain legal; hiding a literal in a local does not change its meaning.
        if (file.Model.GetSymbolInfo(expression).Symbol is ILocalSymbol local && visiting.Add(local))
            return local.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is VariableDeclaratorSyntax { Initializer: { } initializer } &&
                StringLiteral(initializer.Value, visiting));
        return false;
    }

    private bool RejectsOnly(ExpressionSyntax expression)
    {
        var branch = expression.Ancestors().FirstOrDefault(node => node is IfStatementSyntax or ConditionalExpressionSyntax or CatchFilterClauseSyntax);
        if (branch is CatchFilterClauseSyntax { Parent: CatchClauseSyntax clause }) return Rejecting(clause.Block);
        if (branch is IfStatementSyntax conditional && conditional.Condition.Span.Contains(expression.Span))
            return Rejecting(conditional.Statement) || conditional.Else is not null && Rejecting(conditional.Else.Statement);
        return branch is ConditionalExpressionSyntax choice && choice.Condition.Span.Contains(expression.Span) &&
            (choice.WhenTrue is ThrowExpressionSyntax || choice.WhenFalse is ThrowExpressionSyntax);
    }

    private bool Rejecting(StatementSyntax? statement) => statement switch
    {
        ThrowStatementSyntax => true,
        BlockSyntax block => Rejecting(block.Statements.LastOrDefault()),
        IfStatementSyntax { Else: not null } branch => Rejecting(branch.Statement) && Rejecting(branch.Else.Statement),
        ReturnStatementSyntax { Expression: ThrowExpressionSyntax } => true,
        _ => false
    };
}

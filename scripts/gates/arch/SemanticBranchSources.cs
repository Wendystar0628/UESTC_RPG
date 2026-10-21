// Gives each conditional source branch its own Roslyn model without combining mutually exclusive declarations.
// 用法：由 SemanticPolicySources 调用。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

internal static class SemanticBranchSources
{
    private sealed record Frame(List<(ExpressionSyntax Expression, bool Value)> Parent, List<ExpressionSyntax> Seen);

    public static IEnumerable<SourceFile> Expand(SourceFile file)
    {
        yield return file;
        var options = (CSharpParseOptions)file.Tree.Options;
        var variants = new HashSet<string>(StringComparer.Ordinal) { string.Join(";", options.PreprocessorSymbolNames.Order(StringComparer.Ordinal)) };
        foreach (var branch in Constraints(file.Root))
        {
            var symbols = branch.SelectMany(item => item.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                .Select(name => name.Identifier.ValueText).Distinct(StringComparer.Ordinal).ToArray();
            var assignment = Solve(branch, symbols, 0, new Dictionary<string, bool>(StringComparer.Ordinal), options.PreprocessorSymbolNames.ToHashSet(StringComparer.Ordinal));
            if (assignment is null) continue; // Every assignment contradicts a condition; unknown expressions throw in Solve.
            var enabled = options.PreprocessorSymbolNames.Where(symbol => !assignment.ContainsKey(symbol))
                .Concat(assignment.Where(item => item.Value).Select(item => item.Key)).Order(StringComparer.Ordinal).ToArray();
            if (!variants.Add(string.Join(";", enabled))) continue;
            var tree = CSharpSyntaxTree.ParseText(file.Tree.GetText(), options.WithPreprocessorSymbols(enabled), file.Path);
            var errors = tree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
            var compilation = file.Model.Compilation.ReplaceSyntaxTree(file.Tree, tree);
            yield return new SourceFile(file.Path, tree, compilation.GetSemanticModel(tree));
        }
    }

    private static IEnumerable<List<(ExpressionSyntax Expression, bool Value)>> Constraints(CompilationUnitSyntax root)
    {
        var current = new List<(ExpressionSyntax Expression, bool Value)>();
        var stack = new Stack<Frame>();
        foreach (var directive in root.DescendantTrivia(descendIntoTrivia: true).Select(trivia => trivia.GetStructure()))
        {
            switch (directive)
            {
                case IfDirectiveTriviaSyntax first:
                    stack.Push(new(current.ToList(), [first.Condition]));
                    current = [.. current, (first.Condition, true)];
                    yield return current;
                    break;
                case ElifDirectiveTriviaSyntax next when stack.TryPeek(out var nextFrame):
                    current = [.. nextFrame.Parent, .. nextFrame.Seen.Select(expression => (expression, false)), (next.Condition, true)];
                    nextFrame.Seen.Add(next.Condition);
                    yield return current;
                    break;
                case ElseDirectiveTriviaSyntax when stack.TryPeek(out var elseFrame):
                    current = [.. elseFrame.Parent, .. elseFrame.Seen.Select(expression => (expression, false))];
                    yield return current;
                    break;
                case EndIfDirectiveTriviaSyntax when stack.TryPop(out var closedFrame): current = closedFrame.Parent; break;
            }
        }
    }

    private static Dictionary<string, bool>? Solve(List<(ExpressionSyntax Expression, bool Value)> conditions, string[] symbols,
        int index, Dictionary<string, bool> values, HashSet<string> defaults)
    {
        var evaluated = conditions.Select(item => (Actual: Evaluate(item.Expression, values), Expected: item.Value)).ToArray();
        if (evaluated.Any(item => item.Actual is { } actual && actual != item.Expected)) return null;
        if (evaluated.All(item => item.Actual is not null)) return new(values, StringComparer.Ordinal);
        if (index == symbols.Length)
        {
            var unresolved = conditions.First(item => Evaluate(item.Expression, values) is null).Expression;
            throw new InvalidOperationException($"{unresolved.GetLocation().GetLineSpan()}: Cannot evaluate conditional source branch: {unresolved}");
        }
        var symbol = symbols[index];
        foreach (var value in new[] { defaults.Contains(symbol), !defaults.Contains(symbol) })
        {
            values[symbol] = value;
            var answer = Solve(conditions, symbols, index + 1, values, defaults);
            if (answer is not null) return answer;
        }
        values.Remove(symbol);
        return null;
    }

    private static bool? Evaluate(ExpressionSyntax expression, Dictionary<string, bool> values)
    {
        if (expression is IdentifierNameSyntax name) return values.TryGetValue(name.Identifier.ValueText, out var value) ? value : null;
        if (expression.IsKind(SyntaxKind.TrueLiteralExpression)) return true;
        if (expression.IsKind(SyntaxKind.FalseLiteralExpression)) return false;
        if (expression is ParenthesizedExpressionSyntax group) return Evaluate(group.Expression, values);
        if (expression is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
            return Evaluate(unary.Operand, values) is { } operand ? !operand : null;
        if (expression is not BinaryExpressionSyntax binary) return null;
        var left = Evaluate(binary.Left, values);
        var right = Evaluate(binary.Right, values);
        return binary.Kind() switch
        {
            SyntaxKind.LogicalAndExpression => left == false || right == false ? false : left == true && right == true ? true : null,
            SyntaxKind.LogicalOrExpression => left == true || right == true ? true : left == false && right == false ? false : null,
            SyntaxKind.EqualsExpression when left is not null && right is not null => left == right,
            SyntaxKind.NotEqualsExpression when left is not null && right is not null => left != right,
            _ => null
        };
    }
}

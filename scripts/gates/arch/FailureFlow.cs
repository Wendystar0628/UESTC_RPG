// Follows caught exception objects through structured paths to throws or documented receivers.
// 用法：本批由 SemanticPolicyRegressionTests 调用；全仓入口待 TOOL-227 接入。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Campus.Arch.Tests;

internal sealed class FailureFlow(SourceFile file, CatchClauseSyntax? origin, HashSet<string> documents,
    HashSet<IMethodSymbol>? activeFactories = null)
{
    private readonly HashSet<IMethodSymbol> factories = activeFactories ?? new(SymbolEqualityComparer.Default);
    private enum End { Running, Return, Break, Continue, Throw }
    private sealed record ObjectReference(ISymbol Symbol, string Receiver);
    private sealed class ReferenceComparer : IEqualityComparer<ObjectReference>
    {
        public bool Equals(ObjectReference? left, ObjectReference? right) => left is not null && right is not null &&
            SymbolEqualityComparer.Default.Equals(left.Symbol, right.Symbol) && left.Receiver == right.Receiver;
        public int GetHashCode(ObjectReference value) => HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(value.Symbol), value.Receiver);
    }
    private sealed class PathState
    {
        public HashSet<ObjectReference> Objects { get; } = new(new ReferenceComparer());
        public bool Handled;
        public bool ReturnRetains;
        public End Exit;
        public PathState Copy()
        {
            var copy = new PathState { Handled = Handled, ReturnRetains = ReturnRetains, Exit = Exit };
            copy.Objects.UnionWith(Objects);
            return copy;
        }
    }

    public static bool IsHandler(IParameterSymbol parameter, HashSet<string> documents) =>
        IsException(parameter.Type) && parameter.GetAttributes().Any(attribute =>
            SemanticPolicySources.Is(attribute, "FailureHandler") && attribute.ConstructorArguments.Length == 1 &&
            attribute.ConstructorArguments[0].Value is string document && documents.Contains(document));

    public static bool Delivered(SourceFile file, CatchClauseSyntax clause, HashSet<string> documents)
    {
        // A cancellation token being set does not turn an arbitrary Exception into cancellation.
        if (clause.Declaration is { } declaration &&
            SourceModel.Inherits(file.Model.GetTypeInfo(declaration.Type).Type, "System.OperationCanceledException")) return true;
        if (clause.Filter is { } filter && PureCancellation(file, clause, filter.FilterExpression)) return true;
        var flow = new FailureFlow(file, clause, documents);
        var start = new PathState();
        if (clause.Declaration is { } caught && file.Model.GetDeclaredSymbol(caught) is { } symbol) start.Objects.Add(new(symbol, ""));
        var paths = flow.Statement(clause.Block, [start]);
        paths = flow.ContinueOutside(clause, paths);
        return paths.All(path => path.Handled);
    }

    private static bool PureCancellation(SourceFile file, CatchClauseSyntax clause, ExpressionSyntax condition)
    {
        if (condition is ParenthesizedExpressionSyntax group) return PureCancellation(file, clause, group.Expression);
        if (condition is BinaryExpressionSyntax binary)
            return binary.Kind() switch
            {
                SyntaxKind.LogicalAndExpression => PureCancellation(file, clause, binary.Left) || PureCancellation(file, clause, binary.Right),
                SyntaxKind.LogicalOrExpression => PureCancellation(file, clause, binary.Left) && PureCancellation(file, clause, binary.Right),
                SyntaxKind.IsExpression => clause.Declaration is not null &&
                    SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(binary.Left).Symbol, file.Model.GetDeclaredSymbol(clause.Declaration)) &&
                    SourceModel.Inherits(file.Model.GetTypeInfo(binary.Right).Type, "System.OperationCanceledException"),
                _ => false
            };
        if (condition is not IsPatternExpressionSyntax match || clause.Declaration is null ||
            !SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(match.Expression).Symbol, file.Model.GetDeclaredSymbol(clause.Declaration))) return false;
        var type = match.Pattern switch { TypePatternSyntax pattern => pattern.Type, DeclarationPatternSyntax pattern => pattern.Type, _ => null };
        return type is not null && SourceModel.Inherits(file.Model.GetTypeInfo(type).Type, "System.OperationCanceledException");
    }

    private List<PathState> ContinueOutside(SyntaxNode current, List<PathState> paths)
    {
        while (current.Parent is { } parent)
        {
            if (parent is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax or AccessorDeclarationSyntax)
                return paths;
            if (parent is BlockSyntax block && current is StatementSyntax statement)
                paths = Statements(block.Statements.Skip(block.Statements.IndexOf(statement) + 1), paths);
            if (parent is TryStatementSyntax attempt && (current is CatchClauseSyntax || current == attempt.Block) && attempt.Finally is { } cleanup)
                paths = Finally(cleanup.Block, paths);
            if (parent is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax or SwitchStatementSyntax)
                foreach (var path in paths.Where(path => path.Exit is End.Break or End.Continue)) path.Exit = End.Running;
            current = parent;
        }
        return paths;
    }

    private List<PathState> Statements(IEnumerable<StatementSyntax> statements, List<PathState> paths)
    {
        foreach (var statement in statements) paths = Statement(statement, paths);
        return paths;
    }

    private List<PathState> Statement(StatementSyntax statement, List<PathState> paths)
    {
        var stopped = paths.Where(path => path.Exit != End.Running || path.Handled).ToList();
        foreach (var path in paths.Where(path => path.Exit == End.Running && !path.Handled)) stopped.AddRange(Step(statement, path));
        return stopped;
    }

    private List<PathState> Step(StatementSyntax statement, PathState state)
    {
        switch (statement)
        {
            case BlockSyntax block: return Statements(block.Statements, [state]);
            case ExpressionStatementSyntax expression: return Expression(expression.Expression, state);
            case LocalDeclarationStatementSyntax declaration:
                var declared = new List<PathState> { state };
                foreach (var variable in declaration.Declaration.Variables)
                {
                    if (variable.Initializer is null) continue;
                    declared = declared.SelectMany(path => Expression(variable.Initializer.Value, path)).ToList();
                    foreach (var path in declared) Set(variable, Retains(variable.Initializer.Value, path), path);
                }
                return declared;
            case IfStatementSyntax branch:
                return Expression(branch.Condition, state).SelectMany(path => Branch(branch, path)).ToList();
            case ThrowStatementSyntax thrown:
                state.Handled = thrown.Expression is null ? origin is not null && thrown.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault() == origin : Retains(thrown.Expression, state);
                state.Exit = End.Throw;
                return [state];
            case ReturnStatementSyntax returned:
                var returns = returned.Expression is null ? new List<PathState> { state } : Expression(returned.Expression, state);
                foreach (var path in returns.Where(path => path.Exit == End.Running))
                {
                    path.ReturnRetains = returned.Expression is not null && Retains(returned.Expression, path);
                    path.Exit = End.Return;
                }
                return returns;
            case ContinueStatementSyntax: state.Exit = End.Continue; return [state];
            case BreakStatementSyntax: state.Exit = End.Break; return [state];
            case TryStatementSyntax attempt:
                var alternatives = Statement(attempt.Block, [state.Copy()]);
                foreach (var clause in attempt.Catches) alternatives.AddRange(Statement(clause.Block, [state.Copy()]));
                return attempt.Finally is null ? alternatives : Finally(attempt.Finally.Block, alternatives);
            case UsingStatementSyntax resource: return Statement(resource.Statement, [state]);
            case LockStatementSyntax locked: return Statement(locked.Statement, [state]);
            case CheckedStatementSyntax checkedBlock: return Statement(checkedBlock.Block, [state]);
            case SwitchStatementSyntax selection:
                var branches = selection.Sections.SelectMany(section => Statements(section.Statements, [state.Copy()])).ToList();
                if (!selection.Sections.Any(section => section.Labels.Any(label => label is DefaultSwitchLabelSyntax))) branches.Add(state.Copy());
                foreach (var path in branches.Where(path => path.Exit == End.Break)) path.Exit = End.Running;
                return branches;
            case ForEachStatementSyntax loop: return Loop(loop.Statement, state, true);
            case ForStatementSyntax loop: return Loop(loop.Statement, state, true);
            case WhileStatementSyntax loop: return Loop(loop.Statement, state, true);
            case DoStatementSyntax loop: return Loop(loop.Statement, state, false);
            default: return [state];
        }
    }

    private List<PathState> Branch(IfStatementSyntax branch, PathState state)
    {
        var known = KnownCondition(branch.Condition, state);
        if (known == true) return Statement(branch.Statement, [state]);
        if (known == false) return branch.Else is null ? [state] : Statement(branch.Else.Statement, [state]);
        return Statement(branch.Statement, [state.Copy()]).Concat(branch.Else is null ? new List<PathState> { state.Copy() } :
            Statement(branch.Else.Statement, [state.Copy()])).ToList();
    }

    private List<PathState> Finally(BlockSyntax cleanup, List<PathState> paths)
    {
        var result = new List<PathState>();
        foreach (var path in paths)
        {
            var previous = path.Exit;
            path.Exit = End.Running;
            foreach (var cleaned in Statement(cleanup, [path]))
            {
                if (cleaned.Exit == End.Running) cleaned.Exit = previous;
                result.Add(cleaned);
            }
        }
        return result;
    }

    private List<PathState> Loop(StatementSyntax body, PathState state, bool mayBeEmpty)
    {
        var paths = Statement(body, [state.Copy()]);
        foreach (var path in paths.Where(path => path.Exit is End.Break or End.Continue)) path.Exit = End.Running;
        if (mayBeEmpty) paths.Add(state);
        return paths;
    }

    private List<PathState> Expression(ExpressionSyntax expression, PathState state)
    {
        if (expression is ConditionalExpressionSyntax conditional)
            return Expression(conditional.WhenTrue, state.Copy()).Concat(Expression(conditional.WhenFalse, state.Copy())).ToList();
        if (expression is BinaryExpressionSyntax binary && binary.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression or SyntaxKind.CoalesceExpression)
            return Expression(binary.Left, state.Copy()).SelectMany(left => new[] { left.Copy() }.Concat(Expression(binary.Right, left.Copy()))).ToList();
        if (expression is ConditionalAccessExpressionSyntax access)
            return new[] { state.Copy() }.Concat(Expression(access.WhenNotNull, state.Copy())).ToList();
        if (expression is ThrowExpressionSyntax thrown)
        {
            state.Handled = Retains(thrown.Expression, state);
            state.Exit = End.Throw;
            return [state];
        }
        if (expression is AssignmentExpressionSyntax assignment)
        {
            var retains = Retains(assignment.Right, state);
            var paths = Expression(assignment.Right, state);
            foreach (var path in paths) Set(assignment.Left, retains, path);
            return paths;
        }
        if (expression is InvocationExpressionSyntax call && file.Model.GetOperation(call) is IInvocationOperation operation)
        {
            foreach (var argument in operation.Arguments)
                if (argument.Parameter is { } parameter && IsHandler(parameter, documents) &&
                    argument.Value.Syntax is ExpressionSyntax value && Retains(value, state)) state.Handled = true;
            if (operation.Instance?.Syntax is ExpressionSyntax receiver && operation.TargetMethod.Name is "Add" or "AddRange" or "Enqueue" or "Push" &&
                operation.TargetMethod.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal) &&
                operation.Arguments.Any(argument => argument.Value.Syntax is ExpressionSyntax value && Retains(value, state)))
                Set(receiver, true, state);
            if (operation.Instance?.Syntax is ExpressionSyntax cleared && operation.TargetMethod.Name == "Clear" &&
                operation.TargetMethod.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal))
                Set(cleared, false, state);
            foreach (var argument in operation.Arguments.Where(argument => argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out))
                if (argument.Value.Syntax is ExpressionSyntax changed) Set(changed, false, state);
        }
        // Descend only into expressions executed here; lambda/local-function bodies execute elsewhere.
        var current = new List<PathState> { state };
        foreach (var child in expression.DescendantNodes(descendIntoChildren: node =>
            node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax && (node == expression || node is not ExpressionSyntax))
            .OfType<ExpressionSyntax>().Where(node => node is not AnonymousFunctionExpressionSyntax))
            current = current.SelectMany(path => Expression(child, path)).ToList();
        return current;
    }

    private bool Retains(ExpressionSyntax expression, PathState state)
    {
        if (Reference(expression) is { } reference && state.Objects.Contains(reference)) return true;
        if (expression is MemberAccessExpressionSyntax property && ExceptionCarrier(file.Model.GetTypeInfo(expression).Type))
        {
            if (Retains(property.Expression, state)) return true;
            if (property.Name.Identifier.ValueText == "Exception" && origin?.Parent is TryStatementSyntax attempt &&
                attempt.Block.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                    .OfType<AwaitExpressionSyntax>().Any(awaited => SameAwaitedObject(awaited.Expression, property.Expression))) return true;
        }
        switch (expression)
        {
            case ParenthesizedExpressionSyntax group: return Retains(group.Expression, state);
            case CastExpressionSyntax cast: return Retains(cast.Expression, state);
            case PostfixUnaryExpressionSyntax suppressed when suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression): return Retains(suppressed.Operand, state);
            case AwaitExpressionSyntax awaited: return Retains(awaited.Expression, state);
            case ConditionalExpressionSyntax choice: return Retains(choice.WhenTrue, state) && Retains(choice.WhenFalse, state);
            case SwitchExpressionSyntax choice: return choice.Arms.Count != 0 && choice.Arms.All(arm => Retains(arm.Expression, state));
            case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression): return Retains(coalesce.Left, state) && Retains(coalesce.Right, state);
            case ArrayCreationExpressionSyntax array: return array.Initializer?.Expressions.Any(value => Retains(value, state)) == true;
            case ImplicitArrayCreationExpressionSyntax array: return array.Initializer.Expressions.Any(value => Retains(value, state));
            case CollectionExpressionSyntax collection: return collection.Elements.Any(item => item is ExpressionElementSyntax value && Retains(value.Expression, state) || item is SpreadElementSyntax spread && Retains(spread.Expression, state));
            case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax:
                return file.Model.GetOperation(expression) is IObjectCreationOperation created &&
                    IsException(created.Type) && created.Arguments.Any(argument =>
                        ExceptionCarrier(argument.Parameter?.Type) &&
                        argument.Value.Syntax is ExpressionSyntax value && Retains(value, state));
            case InvocationExpressionSyntax call when file.Model.GetOperation(call) is IInvocationOperation invocation:
                if (invocation.TargetMethod.ContainingNamespace.ToDisplayString() == "System.Linq" && ExceptionCarrier(invocation.Type))
                    return invocation.Arguments.Any(argument => argument.Value.Syntax is ExpressionSyntax value && Retains(value, state)) ||
                        invocation.Instance?.Syntax is ExpressionSyntax receiver && Retains(receiver, state);
                return FactoryRetains(invocation, state);
            default: return false;
        }
    }

    private bool FactoryRetains(IInvocationOperation invocation, PathState state)
    {
        var method = (invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod).OriginalDefinition;
        if (!IsException(method.ReturnType) || method.IsAbstract ||
            ((method.IsVirtual || method.IsOverride) && !method.IsSealed && !method.ContainingType.IsSealed)) return false;
        var retained = invocation.Arguments.Where(argument => argument.Parameter is { } parameter &&
            IsException(parameter.Type) && argument.Value.Syntax is ExpressionSyntax value && Retains(value, state))
            .Select(argument => argument.Parameter!.Ordinal + (invocation.TargetMethod.ReducedFrom is null ? 0 : 1)).ToHashSet();
        if (invocation.TargetMethod.ReducedFrom is not null && invocation.Instance?.Syntax is ExpressionSyntax receiver &&
            IsException(method.Parameters[0].Type) && Retains(receiver, state)) retained.Add(0);
        if (retained.Count == 0 || !factories.Add(method)) return false;
        try
        {
            var definitions = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).ToArray();
            if (definitions.Length != 1) return false;
            var definition = definitions[0];
            var body = definition switch { MethodDeclarationSyntax item => item.Body, LocalFunctionStatementSyntax item => item.Body, _ => null };
            var expression = definition switch { MethodDeclarationSyntax item => item.ExpressionBody?.Expression,
                LocalFunctionStatementSyntax item => item.ExpressionBody?.Expression, _ => null };
            var source = new SourceFile(definition.SyntaxTree.FilePath, definition.SyntaxTree,
                file.Model.Compilation.GetSemanticModel(definition.SyntaxTree));
            var flow = new FailureFlow(source, null, documents, factories);
            var start = new PathState();
            foreach (var parameter in method.Parameters.Where(parameter => retained.Contains(parameter.Ordinal))) start.Objects.Add(new(parameter, ""));
            if (expression is not null) return flow.Retains(expression, start);
            if (body is null) return false;
            var paths = flow.Statement(body, [start]);
            return paths.Count != 0 && paths.All(path => path.Exit == End.Return && path.ReturnRetains);
        }
        finally { factories.Remove(method); }
    }

    private void Set(SyntaxNode target, bool retains, PathState state)
    {
        if (Reference(target) is not { } reference) return;
        if (retains) state.Objects.Add(reference); else state.Objects.Remove(reference);
    }

    private ObjectReference? Reference(SyntaxNode target)
    {
        var symbol = target is VariableDeclaratorSyntax variable ? file.Model.GetDeclaredSymbol(variable) : file.Model.GetSymbolInfo(target).Symbol;
        if (symbol is null) return null;
        var receiver = "";
        if (!symbol.IsStatic && symbol is IFieldSymbol or IPropertySymbol)
            receiver = target is MemberAccessExpressionSyntax member ? member.Expression.WithoutTrivia().ToString() : "this";
        return new(symbol, receiver);
    }

    // Nullable annotations describe static nullability, not a different exception identity.
    private static bool IsException(ITypeSymbol? type) =>
        SourceModel.Inherits(type?.WithNullableAnnotation(NullableAnnotation.NotAnnotated), "System.Exception");

    private static bool ExceptionCarrier(ITypeSymbol? type) => IsException(type) ||
        type is IArrayTypeSymbol array && ExceptionCarrier(array.ElementType) ||
        type is INamedTypeSymbol named && named.AllInterfaces.Append(named).Any(item =>
            item.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
            item.TypeArguments.Any(IsException));

    private bool SameAwaitedObject(ExpressionSyntax awaited, ExpressionSyntax task)
    {
        while (awaited is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } call &&
            file.Model.GetSymbolInfo(call).Symbol is IMethodSymbol method &&
            SourceModel.InNamespace(method.ContainingNamespace.ToDisplayString(), "System.Threading.Tasks") &&
            method.Name is "ConfigureAwait" or "WaitAsync") awaited = member.Expression;
        var symbol = file.Model.GetSymbolInfo(awaited).Symbol;
        return symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, file.Model.GetSymbolInfo(task).Symbol);
    }

    private bool? KnownCondition(ExpressionSyntax condition, PathState state)
    {
        if (SourceModel.Constant(file.Model, condition) is bool constant) return constant;
        if (condition is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
            return KnownCondition(unary.Operand, state) is { } operand ? !operand : null;
        if (condition is BinaryExpressionSyntax binary && binary.Left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" } count &&
            Retains(count.Expression, state) && SourceModel.Constant(file.Model, binary.Right) is 0)
            return binary.Kind() switch { SyntaxKind.NotEqualsExpression or SyntaxKind.GreaterThanExpression => true, SyntaxKind.EqualsExpression or SyntaxKind.LessThanOrEqualExpression => false, _ => null };
        if (condition is BinaryExpressionSyntax nullCheck && SourceModel.Constant(file.Model, nullCheck.Right) is null &&
            nullCheck.Right.IsKind(SyntaxKind.NullLiteralExpression) && Retains(nullCheck.Left, state))
            return nullCheck.Kind() switch { SyntaxKind.NotEqualsExpression => true, SyntaxKind.EqualsExpression => false, _ => null };
        return null;
    }
}

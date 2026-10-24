// Follows caught exception objects through structured paths to throws or documented receivers.
// 用法：由 SemanticPolicyGateTests 经 scripts/gate.ps1 的 G4 入口调用。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Campus.Arch.Tests;

internal sealed class FailureFlow(SourceFile file, CatchClauseSyntax? origin, HashSet<string> documents,
    HashSet<IMethodSymbol>? activeFactories = null)
{
    private readonly HashSet<IMethodSymbol> factories = activeFactories ?? new(SymbolEqualityComparer.Default);
    private FailureTaskOrigin? taskOrigin;
    private FailureTaskOrigin Tasks => taskOrigin ??= new(file, origin, (left, right) =>
        Reference(left) is { } first && Reference(right) is { } second && new ReferenceComparer().Equals(first, second));
    private readonly Dictionary<(SemanticModel Model, SyntaxNode Definition, string Retained, bool Recursive), bool> factoryResults = new();
    private IMethodSymbol? currentFactory;
    private HashSet<int>? retainedParameters;
    private bool allowRecursiveFactory = true;
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
        public Dictionary<ObjectReference, bool> NullFacts { get; } = new(new ReferenceComparer());
        public Dictionary<ObjectReference, HashSet<int>> TupleSlots { get; } = new(new ReferenceComparer());
        public bool CallerKeeps;
        public bool ObservedTaskFailure;
        public bool Handled;
        public bool ReturnRetains;
        public bool ThrowRetains;
        public bool UsedRecursion;
        public End Exit;
        public PathState Copy()
        {
            var copy = new PathState { Handled = Handled, ReturnRetains = ReturnRetains, ThrowRetains = ThrowRetains,
                UsedRecursion = UsedRecursion, Exit = Exit, CallerKeeps = CallerKeeps, ObservedTaskFailure = ObservedTaskFailure };
            copy.Objects.UnionWith(Objects);
            foreach (var fact in NullFacts) copy.NullFacts.Add(fact.Key, fact.Value);
            foreach (var tuple in TupleSlots) copy.TupleSlots.Add(tuple.Key, [.. tuple.Value]);
            return copy;
        }
    }

    private sealed class PathStateComparer : IEqualityComparer<PathState>
    {
        private static readonly ReferenceComparer References = new();
        public bool Equals(PathState? left, PathState? right) => ReferenceEquals(left, right) ||
            left is not null && right is not null && left.CallerKeeps == right.CallerKeeps &&
            left.ObservedTaskFailure == right.ObservedTaskFailure && left.Handled == right.Handled &&
            left.ReturnRetains == right.ReturnRetains && left.ThrowRetains == right.ThrowRetains &&
            left.UsedRecursion == right.UsedRecursion && left.Exit == right.Exit &&
            left.Objects.SetEquals(right.Objects) && left.NullFacts.Count == right.NullFacts.Count &&
            left.NullFacts.All(fact => right.NullFacts.TryGetValue(fact.Key, out var value) && value == fact.Value) &&
            left.TupleSlots.Count == right.TupleSlots.Count && left.TupleSlots.All(tuple =>
                right.TupleSlots.TryGetValue(tuple.Key, out var slots) && tuple.Value.SetEquals(slots));

        public int GetHashCode(PathState state)
        {
            var objects = 0; var facts = 0; var tuples = 0;
            foreach (var item in state.Objects) objects ^= References.GetHashCode(item);
            foreach (var fact in state.NullFacts) facts ^= HashCode.Combine(References.GetHashCode(fact.Key), fact.Value);
            foreach (var tuple in state.TupleSlots)
            {
                var slots = 0;
                foreach (var slot in tuple.Value) slots ^= slot.GetHashCode();
                tuples ^= HashCode.Combine(References.GetHashCode(tuple.Key), tuple.Value.Count, slots);
            }
            var hash = new HashCode();
            hash.Add(HashCode.Combine(state.Objects.Count, objects));
            hash.Add(HashCode.Combine(state.NullFacts.Count, facts));
            hash.Add(HashCode.Combine(state.TupleSlots.Count, tuples));
            hash.Add(state.CallerKeeps); hash.Add(state.ObservedTaskFailure); hash.Add(state.Handled);
            hash.Add(state.ReturnRetains); hash.Add(state.ThrowRetains); hash.Add(state.UsedRecursion); hash.Add(state.Exit);
            return hash.ToHashCode();
        }
    }

    private static readonly PathStateComparer States = new();
    private static List<PathState> Converge(IEnumerable<PathState> paths)
    {
        // Finish transfers before hashing mutable states; no state is changed while Distinct holds it.
        var completed = paths.ToList();
        return completed.Distinct(States).ToList();
    }

    public static bool IsHandler(IParameterSymbol parameter, HashSet<string> documents) =>
        ExceptionCarrier(parameter.Type) && parameter.GetAttributes().Any(attribute =>
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
        if (clause.Filter is { } caughtFilter) flow.Assume(caughtFilter.FilterExpression, true, start);
        // A sole guarded try body reaches its catch only after entering that guard.
        if (clause.Parent is TryStatementSyntax { Block.Statements: [IfStatementSyntax { Else: null } guarded] } &&
            !guarded.Condition.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any() &&
            !guarded.Statement.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().Any())
            flow.Assume(guarded.Condition, true, start);
        var paths = flow.Statement(clause.Block, [start]);
        paths = flow.ContinueOutside(clause, paths);
        return paths.All(flow.DeliveredPath);
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
            if (paths.All(DeliveryComplete)) return paths;
            if (parent is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax or AccessorDeclarationSyntax)
                return paths;
            if (parent is BlockSyntax block && current is StatementSyntax statement)
                paths = Statements(block.Statements.Skip(block.Statements.IndexOf(statement) + 1), paths);
            if (parent is TryStatementSyntax attempt)
            {
                if (current == attempt.Block) paths = CatchThrows(attempt, paths);
                if ((current is CatchClauseSyntax || current == attempt.Block) && attempt.Finally is { } cleanup)
                    paths = Finally(cleanup.Block, paths);
            }
            if (parent is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax or SwitchStatementSyntax)
            {
                foreach (var path in paths.Where(path => path.Exit is End.Break or End.Continue)) path.Exit = End.Running;
                paths = Converge(paths);
            }
            current = parent;
        }
        return paths;
    }

    private List<PathState> Statements(IEnumerable<StatementSyntax> statements, List<PathState> paths)
    {
        foreach (var statement in statements)
        {
            if (paths.All(path => path.Exit != End.Running || DeliveryComplete(path))) break;
            paths = Statement(statement, paths);
        }
        return paths;
    }

    private List<PathState> Statement(StatementSyntax statement, List<PathState> paths)
    {
        var stopped = paths.Where(path => path.Exit != End.Running || DeliveryComplete(path)).ToList();
        foreach (var path in paths.Where(path => path.Exit == End.Running && !DeliveryComplete(path))) stopped.AddRange(Step(statement, path));
        return Converge(stopped);
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
                    declared = Converge(declared.SelectMany(path => Expression(variable.Initializer.Value, path)));
                    foreach (var path in declared.Where(path => !DeliveryComplete(path))) Assign(variable, variable.Initializer.Value, path);
                    declared = Converge(declared);
                }
                return declared;
            case IfStatementSyntax branch:
                return Converge(Expression(branch.Condition, state).SelectMany(path => Branch(branch, path)));
            case ThrowStatementSyntax thrown:
                var caught = thrown.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault();
                state.ThrowRetains = thrown.Expression is not null ? Retains(thrown.Expression, state) :
                    caught == origin || caught?.Declaration is { } caughtDeclaration &&
                    file.Model.GetDeclaredSymbol(caughtDeclaration) is { } caughtSymbol && Has(new(caughtSymbol, ""), state);
                state.Exit = End.Throw;
                return [state];
            case ReturnStatementSyntax returned:
                var returns = returned.Expression is null ? new List<PathState> { state } : Expression(returned.Expression, state);
                foreach (var path in returns.Where(path => path.Exit == End.Running && !DeliveryComplete(path)))
                {
                    path.ReturnRetains = returned.Expression is not null && Retains(returned.Expression, path);
                    path.Exit = End.Return;
                }
                return Converge(returns);
            case ContinueStatementSyntax: state.Exit = End.Continue; return [state];
            case BreakStatementSyntax: state.Exit = End.Break; return [state];
            case TryStatementSyntax attempt:
                var alternatives = Statement(attempt.Block, [state.Copy()]);
                var mayComplete = alternatives.Any(path => path.Exit == End.Running);
                alternatives = CatchThrows(attempt, alternatives);
                if (mayComplete)
                    foreach (var clause in attempt.Catches) alternatives.AddRange(Statement(clause.Block, [state.Copy()]));
                return attempt.Finally is null ? Converge(alternatives) : Finally(attempt.Finally.Block, Converge(alternatives));
            case UsingStatementSyntax resource: return Statement(resource.Statement, [state]);
            case LockStatementSyntax locked: return Statement(locked.Statement, [state]);
            case CheckedStatementSyntax checkedBlock: return Statement(checkedBlock.Block, [state]);
            case SwitchStatementSyntax selection:
                var branches = selection.Sections.SelectMany(section => Statements(section.Statements, [state.Copy()])).ToList();
                if (!selection.Sections.Any(section => section.Labels.Any(label => label is DefaultSwitchLabelSyntax))) branches.Add(state.Copy());
                foreach (var path in branches.Where(path => path.Exit == End.Break)) path.Exit = End.Running;
                return Converge(branches);
            case ForEachStatementSyntax loop: return Loop(loop.Statement, state, true);
            case ForEachVariableStatementSyntax loop:
                if (Reference(loop.Expression) is not { } collection || !state.TupleSlots.TryGetValue(collection, out var slots))
                    return Loop(loop.Statement, state, true);
                var variables = FailureTaskOrigin.Bindings(loop);
                for (var index = 0; index < variables.Length; index++)
                    if (variables[index] is SingleVariableDesignationSyntax variable) Set(variable, slots.Contains(index), state);
                return Loop(loop.Statement, state, false);
            case ForStatementSyntax loop: return Loop(loop.Statement, state, true);
            case WhileStatementSyntax loop: return Loop(loop.Statement, state, true);
            case DoStatementSyntax loop: return Loop(loop.Statement, state, false);
            default: return [state];
        }
    }

    private List<PathState> Branch(IfStatementSyntax branch, PathState state)
    {
        if (DeliveryComplete(state)) return [state];
        var known = KnownCondition(branch.Condition, state);
        var matched = state.Copy();
        var unmatched = state.Copy();
        Assume(branch.Condition, true, matched);
        Assume(branch.Condition, false, unmatched);
        if (known == false) return branch.Else is null ? [unmatched] : Statement(branch.Else.Statement, [unmatched]);
        if (known == true) return Statement(branch.Statement, [matched]);
        return Converge(Statement(branch.Statement, [matched]).Concat(branch.Else is null ? new List<PathState> { unmatched } :
            Statement(branch.Else.Statement, [unmatched])));
    }

    private void Assume(ExpressionSyntax condition, bool matched, PathState state)
    {
        if (Tasks.CallerKeeps(condition, matched, task => Retains(task, state)))
        { state.CallerKeeps = true; if (state.ObservedTaskFailure) state.Handled = true; }
        if (condition is ParenthesizedExpressionSyntax group) { Assume(group.Expression, matched, state); return; }
        if (condition is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
        { Assume(unary.Operand, !matched, state); return; }
        if (condition is BinaryExpressionSyntax binary && (matched && binary.IsKind(SyntaxKind.LogicalAndExpression) ||
            !matched && binary.IsKind(SyntaxKind.LogicalOrExpression)))
        { Assume(binary.Left, matched, state); Assume(binary.Right, matched, state); return; }
        if (NullTest(condition) is { } test && Reference(test.Target) is { } reference)
            state.NullFacts[reference] = matched == test.NonNull;
        if (!matched) return;
        if (file.Model.GetOperation(condition) is not IInvocationOperation operation ||
            operation.TargetMethod.Name != "ReferenceEquals" || !BclMethod(operation.TargetMethod, "System.Object") ||
            operation.Arguments.Length != 2) return;
        var left = operation.Arguments[0].Value;
        var right = operation.Arguments[1].Value;
        if (Retains(left, state)) Set(right.Syntax, true, state);
        if (Retains(right, state)) Set(left.Syntax, true, state);
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
        return Converge(result);
    }

    private List<PathState> Loop(StatementSyntax body, PathState state, bool mayBeEmpty)
    {
        var paths = Statement(body, [state.Copy()]);
        foreach (var path in paths.Where(path => path.Exit is End.Break or End.Continue)) path.Exit = End.Running;
        if (mayBeEmpty) paths.Add(state);
        return Converge(paths);
    }

    private List<PathState> Expression(ExpressionSyntax expression, PathState state)
    {
        if (DeliveryComplete(state)) return [state];
        if (Tasks.IsAggregate(expression))
        { state.ObservedTaskFailure = true; if (state.CallerKeeps) state.Handled = true; }
        if (DeliveryComplete(state)) return [state];
        if (expression is ConditionalExpressionSyntax conditional)
        {
            var known = KnownCondition(conditional.Condition, state);
            var yes = state.Copy(); var no = state.Copy();
            Assume(conditional.Condition, true, yes); Assume(conditional.Condition, false, no);
            if (known is { } selected) return Expression(selected ? conditional.WhenTrue : conditional.WhenFalse, selected ? yes : no);
            return Converge(Expression(conditional.WhenTrue, yes).Concat(Expression(conditional.WhenFalse, no)));
        }
        if (expression is BinaryExpressionSyntax binary && binary.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression or SyntaxKind.CoalesceExpression)
            return Converge(Expression(binary.Left, state.Copy()).SelectMany(left => new[] { left.Copy() }.Concat(Expression(binary.Right, left.Copy()))));
        if (expression is ConditionalAccessExpressionSyntax access)
        {
            var known = NonNull(access.Expression, state);
            var skipped = state.Copy(); var invoked = state.Copy();
            if (Reference(access.Expression) is { } receiver)
            { skipped.NullFacts[receiver] = false; invoked.NullFacts[receiver] = true; }
            if (known == false) return [skipped];
            var called = Expression(access.WhenNotNull, invoked);
            return known == true ? called : Converge(new[] { skipped }.Concat(called));
        }
        if (expression is ThrowExpressionSyntax thrown)
        {
            state.ThrowRetains = Retains(thrown.Expression, state);
            state.Exit = End.Throw;
            return [state];
        }
        if (expression is AssignmentExpressionSyntax assignment)
        {
            if (assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression) && NonNull(assignment.Left, state) == true) return [state];
            var paths = Expression(assignment.Right, state);
            foreach (var path in paths.Where(path => !DeliveryComplete(path)))
                if (assignment.Left is ElementAccessExpressionSyntax dataAccess && DataOwner(dataAccess.Expression) is { } owner)
                {
                    if (Retains(assignment.Right, path)) path.Objects.Add(owner);
                }
                else Assign(assignment.Left, assignment.Right, path);
            return Converge(paths);
        }
        if (expression is InvocationExpressionSyntax call && file.Model.GetOperation(call) is IInvocationOperation operation)
        {
            // Receiver/argument effects happen before delivery or collection mutation.
            List<PathState> paths = call.Expression is MemberAccessExpressionSyntax member ? Expression(member.Expression, state) : [state];
            foreach (var argument in call.ArgumentList.Arguments)
                paths = Converge(paths.SelectMany(path => Expression(argument.Expression, path)));
            foreach (var path in paths.Where(path => path.Exit == End.Running && !DeliveryComplete(path))) ApplyCall(operation, path);
            return Converge(paths);
        }
        // Descend only into expressions executed here; lambda/local-function bodies execute elsewhere.
        var current = new List<PathState> { state };
        foreach (var child in expression.DescendantNodes(descendIntoChildren: node =>
            node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax && (node == expression || node is not ExpressionSyntax))
            .OfType<ExpressionSyntax>().Where(node => node is not AnonymousFunctionExpressionSyntax))
            current = Converge(current.SelectMany(path => Expression(child, path)));
        return current;
    }

    private void ApplyCall(IInvocationOperation operation, PathState state)
    {
        var method = operation.TargetMethod;
        if (SymbolEqualityComparer.Default.Equals(currentFactory, (method.ReducedFrom ?? method).OriginalDefinition)) state.UsedRecursion = true;
        var retainsArgument = operation.Arguments.Any(argument => ExceptionCarrier(argument.Parameter?.Type) && Retains(argument.Value, state));
        var documented = operation.Arguments.Any(argument => argument.Parameter is { } parameter && IsHandler(parameter, documents) && Retains(argument.Value, state));
        var completion = method.Name is "SetException" or "TrySetException" &&
            (BclMethod(method, "System.Threading.Tasks.TaskCompletionSource") || BclMethod(method, "System.Threading.Tasks.TaskCompletionSource`1"));
        var dispatch = method.Name == "Throw" && BclMethod(method, "System.Runtime.ExceptionServices.ExceptionDispatchInfo");
        if ((documented || completion && retainsArgument || dispatch) && operation.Instance is { } instance and not IInstanceReferenceOperation &&
            instance.Syntax is ExpressionSyntax receiverSyntax && NonNull(receiverSyntax, state) != true)
        { state.Exit = End.Throw; state.ThrowRetains = false; return; }
        if (documented || completion && retainsArgument) state.Handled = true;
        if (dispatch)
        {
            state.ThrowRetains = method.IsStatic ? retainsArgument : operation.Instance is { } dispatched && Retains(dispatched, state);
            state.Exit = End.Throw;
        }
        if (operation.Instance?.Syntax is ExpressionSyntax receiver && method.Name is "Add" or "AddRange" or "Enqueue" or "Push" &&
            method.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal) &&
            operation.Arguments.Any(argument => Retains(argument.Value, state)))
        {
            Set(receiver, true, state);
            if (Reference(receiver) is { } collection && operation.Arguments is [{ Value.Syntax: TupleExpressionSyntax tuple }])
                state.TupleSlots[collection] = tuple.Arguments.Select((value, index) => (value.Expression, index))
                    .Where(value => Retains(value.Expression, state)).Select(value => value.index).ToHashSet();
        }
        if (operation.Instance?.Syntax is ExpressionSyntax cleared && method.Name == "Clear" &&
            method.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal)) Set(cleared, false, state);
        foreach (var argument in operation.Arguments.Where(argument => argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out))
            if (argument.Value.Syntax is ExpressionSyntax changed) Set(changed, false, state);
    }

    private bool BclMethod(IMethodSymbol method, string metadataName) =>
        Tasks.IsBclType(method.ContainingType, metadataName);

    private bool Retains(ExpressionSyntax expression, PathState state)
    {
        if (Reference(expression) is { } reference && Has(reference, state)) return true;
        if (Tasks.IsAwaited(expression)) return true;
        if (expression is MemberAccessExpressionSyntax property && ExceptionCarrier(file.Model.GetTypeInfo(expression).Type))
        {
            if (file.Model.GetSymbolInfo(property).Symbol is IPropertySymbol { Name: "SourceException" } dispatch &&
                Tasks.IsBclType(dispatch.ContainingType, "System.Runtime.ExceptionServices.ExceptionDispatchInfo") && Retains(property.Expression, state)) return true;
            if (file.Model.GetSymbolInfo(property).Symbol is IPropertySymbol { Name: "Exception" } taskFailure &&
                Tasks.IsBclType(taskFailure.ContainingType, "System.Threading.Tasks.Task") && Retains(property.Expression, state)) return true;
            if (file.Model.GetSymbolInfo(property).Symbol is IPropertySymbol { Name: "InnerExceptions" } aggregate &&
                Tasks.IsBclType(aggregate.ContainingType, "System.AggregateException") && Tasks.IsAggregate(property.Expression)) return true;
            if (file.Model.GetSymbolInfo(property).Symbol is IPropertySymbol { Name: "InnerException" } inner &&
                Tasks.IsBclType(inner.ContainingType, "System.Exception") &&
                file.Model.GetTypeInfo(property.Expression).Type is INamedTypeSymbol reflected &&
                Tasks.IsBclType(reflected, "System.Reflection.TargetInvocationException") && Retains(property.Expression, state)) return true;
        }
        switch (expression)
        {
            case ParenthesizedExpressionSyntax group: return Retains(group.Expression, state);
            case CastExpressionSyntax cast: return Retains(cast.Expression, state);
            case PostfixUnaryExpressionSyntax suppressed when suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression): return Retains(suppressed.Operand, state);
            case AwaitExpressionSyntax awaited: return Retains(awaited.Expression, state);
            case ConditionalExpressionSyntax choice:
                return KnownCondition(choice.Condition, state) is { } selected ? Retains(selected ? choice.WhenTrue : choice.WhenFalse, state) :
                    Retains(choice.WhenTrue, state) && Retains(choice.WhenFalse, state);
            case SwitchExpressionSyntax choice: return choice.Arms.Count != 0 && choice.Arms.All(arm => Retains(arm.Expression, state));
            case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression):
                return Tasks.IsAggregate(coalesce.Left, false) && Retains(coalesce.Right, state) || (NonNull(coalesce.Left, state) switch
                { true => Retains(coalesce.Left, state), false => Retains(coalesce.Right, state), _ => Retains(coalesce.Left, state) && Retains(coalesce.Right, state) });
            case ArrayCreationExpressionSyntax array: return array.Initializer?.Expressions.Any(value => Retains(value, state)) == true;
            case ImplicitArrayCreationExpressionSyntax array: return array.Initializer.Expressions.Any(value => Retains(value, state));
            case CollectionExpressionSyntax collection: return collection.Elements.Any(item => item is ExpressionElementSyntax value && Retains(value.Expression, state) || item is SpreadElementSyntax spread && Retains(spread.Expression, state));
            case TupleExpressionSyntax tuple: return tuple.Arguments.Any(value => Retains(value.Expression, state));
            case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax:
                return file.Model.GetOperation(expression) is IObjectCreationOperation created &&
                    IsException(created.Type) && created.Arguments.Any(argument =>
                        ExceptionCarrier(argument.Parameter?.Type) &&
                        Retains(argument.Value, state));
            case InvocationExpressionSyntax call when file.Model.GetOperation(call) is IInvocationOperation invocation:
                if (invocation.TargetMethod.Name == "FromException" && BclMethod(invocation.TargetMethod, "System.Threading.Tasks.Task"))
                    return invocation.Arguments.Any(argument => Retains(argument.Value, state));
                if (invocation.TargetMethod.Name == "Capture" && BclMethod(invocation.TargetMethod, "System.Runtime.ExceptionServices.ExceptionDispatchInfo"))
                    return invocation.Arguments.Any(argument => Retains(argument.Value, state));
                if (invocation.TargetMethod.ContainingNamespace.ToDisplayString() == "System.Linq" && ExceptionCarrier(invocation.Type))
                    return invocation.Arguments.Any(argument => Retains(argument.Value, state)) ||
                        invocation.Instance is { } receiver && Retains(receiver, state);
                return FactoryRetains(invocation, state);
            default: return false;
        }
    }

    private bool Retains(IOperation operation, PathState state) => operation switch
    {
        // Expanded params containers and omitted defaults may carry the entire call as Syntax.
        // Follow their semantic values before returning to source expressions.
        IConversionOperation conversion => Retains(conversion.Operand, state),
        IArrayCreationOperation array => array.Initializer is { } initializer && Retains(initializer, state),
        IArrayInitializerOperation initializer => initializer.ElementValues.Any(value => Retains(value, state)),
        ICollectionExpressionOperation collection => collection.Elements.Any(value => Retains(value, state)),
        ISpreadOperation spread => Retains(spread.Operand, state),
        IConditionalAccessInstanceOperation instance => instance.Syntax is ExpressionSyntax receiver && Retains(receiver, state),
        IDefaultValueOperation => false,
        _ => !operation.IsImplicit && operation.Syntax is ExpressionSyntax expression && Retains(expression, state)
    };

    private bool FactoryRetains(IInvocationOperation invocation, PathState state)
    {
        var method = (invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod).OriginalDefinition;
        if (!IsException(method.ReturnType) || method.IsAbstract ||
            ((method.IsVirtual || method.IsOverride) && !method.IsSealed && !method.ContainingType.IsSealed)) return false;
        var retained = invocation.Arguments.Where(argument => argument.Parameter is { } parameter &&
            IsException(parameter.Type) && Retains(argument.Value, state))
            .Select(argument => argument.Parameter!.Ordinal + (invocation.TargetMethod.ReducedFrom is null ? 0 : 1)).ToHashSet();
        if (invocation.TargetMethod.ReducedFrom is not null && invocation.Instance is { } receiver &&
            IsException(method.Parameters[0].Type) && Retains(receiver, state)) retained.Add(0);
        if (retained.Count == 0) return false;
        var definitions = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).ToArray();
        if (definitions.Length != 1) return false;
        var definition = definitions[0];
        var source = new SourceFile(SourceModel.RelativePath(definition.SyntaxTree.FilePath), definition.SyntaxTree,
            SourceModel.DeclarationModel(file.Model, method, definition));
        var declared = source.Model.GetDeclaredSymbol(definition) as IMethodSymbol
            ?? throw new InvalidOperationException(source.At(definition, "G4 could not resolve the factory declaration."));
        // Only independent calls are cached; an active recursion stack is part of nested call semantics.
        var cacheable = factories.Count == 0;
        var key = (source.Model, definition, string.Join(",", retained.Order()), allowRecursiveFactory);
        if (cacheable && factoryResults.TryGetValue(key, out var cached)) return cached;
        if (!factories.Add(declared))
        {
            state.UsedRecursion = true;
            return allowRecursiveFactory && SymbolEqualityComparer.Default.Equals(currentFactory, declared) &&
                retainedParameters?.SetEquals(retained) == true;
        }
        try
        {
            var body = definition switch { MethodDeclarationSyntax item => item.Body, LocalFunctionStatementSyntax item => item.Body, _ => null };
            var expression = definition switch { MethodDeclarationSyntax item => item.ExpressionBody?.Expression,
                LocalFunctionStatementSyntax item => item.ExpressionBody?.Expression, _ => null };
            List<PathState> Returns(bool recursive)
            {
                var flow = new FailureFlow(source, null, documents, factories)
                { currentFactory = declared, retainedParameters = retained, allowRecursiveFactory = recursive };
                var start = new PathState();
                foreach (var parameter in declared.Parameters.Where(parameter => retained.Contains(parameter.Ordinal))) start.Objects.Add(new(parameter, ""));
                if (expression is not null)
                {
                    start.ReturnRetains = flow.Retains(expression, start);
                    start.Exit = End.Return;
                    return [start];
                }
                return body is null ? [] : flow.Statement(body, [start]);
            }
            var paths = Returns(allowRecursiveFactory);
            var result = paths.Count != 0 && paths.All(path => path.Exit == End.Return && path.ReturnRetains) &&
                (!allowRecursiveFactory || !paths.Any(path => path.UsedRecursion) ||
                    Returns(false).Any(path => !path.UsedRecursion && path.Exit == End.Return && path.ReturnRetains));
            if (cacheable) factoryResults[key] = result;
            return result;
        }
        finally { factories.Remove(declared); }
    }

    private bool Has(ObjectReference reference, PathState state) => state.Objects.Contains(reference);

    private ObjectReference? DataOwner(SyntaxNode expression) => expression is MemberAccessExpressionSyntax data &&
        file.Model.GetSymbolInfo(data).Symbol is IPropertySymbol { Name: "Data" } property &&
        !property.Locations.Any(location => location.IsInSource) && property.ContainingType.ToDisplayString() == "System.Exception"
            ? Reference(data.Expression) : null;

    // Only an unfiltered catch-all is known to consume an explicit throw here.
    private List<PathState> CatchThrows(TryStatementSyntax attempt, List<PathState> paths)
    {
        var result = paths.Where(path => path.Exit != End.Throw).ToList();
        foreach (var path in paths.Where(path => path.Exit == End.Throw))
        {
            var consumed = false;
            foreach (var clause in attempt.Catches)
            {
                var caught = path.Copy();
                caught.Exit = End.Running;
                caught.ThrowRetains = false;
                if (clause.Declaration is { } declaration) Set(declaration, path.ThrowRetains, caught);
                result.AddRange(Statement(clause.Block, [caught]));
                if (clause.Filter is null && (clause.Declaration is null ||
                    file.Model.GetTypeInfo(clause.Declaration.Type).Type?.ToDisplayString() == "System.Exception"))
                { consumed = true; break; }
            }
            if (!consumed) result.Add(path);
        }
        return Converge(result);
    }

    private bool DeliveryComplete(PathState path) => origin is not null && path.Handled;

    private bool DeliveredPath(PathState path) => path.Handled || path.Exit == End.Throw && path.ThrowRetains ||
        path.Exit == End.Return && path.ReturnRetains ||
        path.Objects.Any(reference => reference.Symbol is IParameterSymbol parameter && IsHandler(parameter, documents));

    private void Set(SyntaxNode target, bool retains, PathState state)
    {
        if (Reference(target) is not { } reference) return;
        state.NullFacts.Remove(reference);
        state.TupleSlots.Remove(reference);
        if (retains) state.Objects.Add(reference); else state.Objects.Remove(reference);
    }

    private void Assign(SyntaxNode target, ExpressionSyntax value, PathState state)
    {
        var retains = Retains(value, state);
        var nonNull = NonNull(value, state);
        Set(target, retains, state);
        if (Reference(target) is { } reference && nonNull is { } fact) state.NullFacts[reference] = fact;
    }

    private bool? NonNull(ExpressionSyntax expression, PathState state)
    {
        if (expression is ParenthesizedExpressionSyntax group) return NonNull(group.Expression, state);
        if (expression is PostfixUnaryExpressionSyntax suppressed && suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            return NonNull(suppressed.Operand, state);
        if (Reference(expression) is { } reference && state.NullFacts.TryGetValue(reference, out var fact)) return fact;
        if (expression.IsKind(SyntaxKind.NullLiteralExpression)) return false;
        if (file.Model.GetTypeInfo(expression).Nullability.FlowState == NullableFlowState.NotNull)
            return expression.Parent is PostfixUnaryExpressionSyntax assertion && assertion.IsKind(SyntaxKind.SuppressNullableWarningExpression) &&
                KnownNullLocal(expression) ? false : true;
        return Retains(expression, state) ? true : null;
    }

    private bool KnownNullLocal(ExpressionSyntax expression)
    {
        if (file.Model.GetSymbolInfo(expression).Symbol is not ILocalSymbol local ||
            local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax declaration ||
            declaration.Ancestors().OfType<BlockSyntax>().FirstOrDefault() is not { } scope) return false;
        var writes = scope.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(assignment => assignment.SpanStart < expression.SpanStart &&
            SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(assignment.Left).Symbol, local)).OrderBy(assignment => assignment.SpanStart).ToArray();
        var last = writes.LastOrDefault();
        if (last is not null && (last.Parent?.Parent is not BlockSyntax block || !expression.Ancestors().Contains(block))) return false;
        var value = last?.Right ?? declaration.Initializer?.Value;
        if (value is null || scope.DescendantNodes().OfType<ArgumentSyntax>().Any(argument => argument.SpanStart > value.SpanStart &&
            argument.SpanStart < expression.SpanStart && !argument.RefKindKeyword.IsKind(SyntaxKind.None) &&
            SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(argument.Expression).Symbol, local))) return false;
        while (value is PostfixUnaryExpressionSyntax suppressed && suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression)) value = suppressed.Operand;
        var constant = file.Model.GetConstantValue(value);
        return constant.HasValue && constant.Value is null;
    }

    private static (ExpressionSyntax Target, bool NonNull)? NullTest(ExpressionSyntax condition)
    {
        if (condition is IsPatternExpressionSyntax match && NonNullPattern(match.Pattern) is { } nonNull) return (match.Expression, nonNull);
        if (condition is not BinaryExpressionSyntax binary || binary.Kind() is not (SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression)) return null;
        if (binary.Right.IsKind(SyntaxKind.NullLiteralExpression)) return (binary.Left, binary.IsKind(SyntaxKind.NotEqualsExpression));
        if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression)) return (binary.Right, binary.IsKind(SyntaxKind.NotEqualsExpression));
        return null;
    }

    private ObjectReference? Reference(SyntaxNode target)
    {
        if (target is ParenthesizedExpressionSyntax group) return Reference(group.Expression);
        if (target is PostfixUnaryExpressionSyntax suppression && suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression)) return Reference(suppression.Operand);
        if (target is AssignmentExpressionSyntax assignment && assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
            return file.Model.GetSymbolInfo(assignment.Left).Symbol is ILocalSymbol ? Reference(assignment.Left) : null;
        var symbol = target is VariableDeclaratorSyntax variable ? file.Model.GetDeclaredSymbol(variable) :
            target is SingleVariableDesignationSyntax designation ? file.Model.GetDeclaredSymbol(designation) :
            target is CatchDeclarationSyntax caught ? file.Model.GetDeclaredSymbol(caught) : file.Model.GetSymbolInfo(target).Symbol;
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

    private bool? KnownCondition(ExpressionSyntax condition, PathState state)
    {
        if (condition is ParenthesizedExpressionSyntax group) return KnownCondition(group.Expression, state);
        if (SourceModel.Constant(file.Model, condition) is bool constant) return constant;
        if (condition is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
            return KnownCondition(unary.Operand, state) is { } operand ? !operand : null;
        if (condition is BinaryExpressionSyntax logical && logical.IsKind(SyntaxKind.LogicalAndExpression))
            return KnownCondition(logical.Left, state) == false || KnownCondition(logical.Right, state) == false ? false :
                KnownCondition(logical.Left, state) == true && KnownCondition(logical.Right, state) == true ? true : null;
        if (condition is BinaryExpressionSyntax either && either.IsKind(SyntaxKind.LogicalOrExpression))
            return KnownCondition(either.Left, state) == true || KnownCondition(either.Right, state) == true ? true :
                KnownCondition(either.Left, state) == false && KnownCondition(either.Right, state) == false ? false : null;
        if (NullTest(condition) is { } test && NonNull(test.Target, state) is { } fact) return fact == test.NonNull;
        if (condition is BinaryExpressionSyntax binary && binary.Left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" } count &&
            Retains(count.Expression, state) && SourceModel.Constant(file.Model, binary.Right) is 0)
            return binary.Kind() switch { SyntaxKind.NotEqualsExpression or SyntaxKind.GreaterThanExpression => true, SyntaxKind.EqualsExpression or SyntaxKind.LessThanOrEqualExpression => false, _ => null };
        return null;
    }

    private static bool? NonNullPattern(PatternSyntax pattern) => pattern switch
    {
        ConstantPatternSyntax constant when constant.Expression.IsKind(SyntaxKind.NullLiteralExpression) => false,
        UnaryPatternSyntax negated when negated.IsKind(SyntaxKind.NotPattern) && NonNullPattern(negated.Pattern) is { } known => !known,
        ParenthesizedPatternSyntax group => NonNullPattern(group.Pattern),
        RecursivePatternSyntax { Type: null, PositionalPatternClause: null, PropertyPatternClause.Subpatterns.Count: 0 } => true,
        _ => null
    };
}

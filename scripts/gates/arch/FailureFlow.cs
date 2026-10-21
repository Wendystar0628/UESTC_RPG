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
    private readonly HashSet<IMethodSymbol> continuations = new(SymbolEqualityComparer.Default);
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
        public bool ThrowRetains;
        public HashSet<SyntaxNode> Values { get; } = [];
        public Dictionary<ObjectReference, HashSet<string>> Data { get; } = new(new ReferenceComparer());
        public End Exit;
        public PathState Copy()
        {
            var copy = new PathState { Handled = Handled, ReturnRetains = ReturnRetains, ThrowRetains = ThrowRetains, Exit = Exit };
            copy.Objects.UnionWith(Objects);
            copy.Values.UnionWith(Values);
            foreach (var pair in Data) copy.Data.Add(pair.Key, new(pair.Value, StringComparer.Ordinal));
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
        return flow.Complete(clause, paths);
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
            if (parent is TryStatementSyntax attempt)
            {
                if (current == attempt.Block) paths = CatchThrows(attempt, paths);
                if ((current is CatchClauseSyntax || current == attempt.Block) && attempt.Finally is { } cleanup)
                    paths = Finally(cleanup.Block, paths);
            }
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
                var caught = thrown.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault();
                state.ThrowRetains = thrown.Expression is not null ? Retains(thrown.Expression, state) :
                    caught == origin || caught?.Declaration is { } caughtDeclaration &&
                    file.Model.GetDeclaredSymbol(caughtDeclaration) is { } caughtSymbol && Has(new(caughtSymbol, ""), state);
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
                var mayComplete = alternatives.Any(path => path.Exit == End.Running);
                alternatives = CatchThrows(attempt, alternatives);
                if (mayComplete)
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
        {
            var known = KnownCondition(conditional.Condition, state);
            if (known is { } selected) return Expression(selected ? conditional.WhenTrue : conditional.WhenFalse, state);
            return Expression(conditional.WhenTrue, state.Copy()).Concat(Expression(conditional.WhenFalse, state.Copy())).ToList();
        }
        if (expression is BinaryExpressionSyntax binary && binary.Kind() is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression or SyntaxKind.CoalesceExpression)
            return Expression(binary.Left, state.Copy()).SelectMany(left => new[] { left.Copy() }.Concat(Expression(binary.Right, left.Copy()))).ToList();
        if (expression is ConditionalAccessExpressionSyntax access)
            return new[] { state.Copy() }.Concat(Expression(access.WhenNotNull, state.Copy())).ToList();
        if (expression is ThrowExpressionSyntax thrown)
        {
            state.ThrowRetains = Retains(thrown.Expression, state);
            state.Exit = End.Throw;
            return [state];
        }
        if (expression is AssignmentExpressionSyntax assignment)
        {
            var retains = Retains(assignment.Right, state) ||
                assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression) && Retains(assignment.Left, state);
            var paths = Expression(assignment.Right, state);
            foreach (var path in paths)
                if (DataSlot(assignment.Left) is { } slot)
                {
                    if (!path.Data.TryGetValue(slot.Owner, out var keys)) path.Data.Add(slot.Owner, keys = new(StringComparer.Ordinal));
                    if (retains) keys.Add(slot.Key); else keys.Remove(slot.Key);
                }
                else Set(assignment.Left, retains, path);
            return paths;
        }
        if (expression is InvocationExpressionSyntax call && file.Model.GetOperation(call) is IInvocationOperation operation)
        {
            // Receiver/argument effects happen before delivery or collection mutation.
            List<PathState> paths = call.Expression is MemberAccessExpressionSyntax member ? Expression(member.Expression, state) : [state];
            foreach (var argument in call.ArgumentList.Arguments)
                paths = paths.SelectMany(path => Expression(argument.Expression, path)).ToList();
            foreach (var path in paths.Where(path => path.Exit == End.Running && !path.Handled)) ApplyCall(operation, path);
            return paths;
        }
        // Descend only into expressions executed here; lambda/local-function bodies execute elsewhere.
        var current = new List<PathState> { state };
        foreach (var child in expression.DescendantNodes(descendIntoChildren: node =>
            node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax && (node == expression || node is not ExpressionSyntax))
            .OfType<ExpressionSyntax>().Where(node => node is not AnonymousFunctionExpressionSyntax))
            current = current.SelectMany(path => Expression(child, path)).ToList();
        return current;
    }

    private void ApplyCall(IInvocationOperation operation, PathState state)
    {
        var method = operation.TargetMethod;
        var retainsArgument = operation.Arguments.Any(argument => ExceptionCarrier(argument.Parameter?.Type) && Retains(argument.Value, state));
        foreach (var argument in operation.Arguments)
            if (argument.Parameter is { } parameter && IsHandler(parameter, documents) && Retains(argument.Value, state)) state.Handled = true;
        if (method.Name is "SetException" or "TrySetException" &&
            (BclMethod(method, "System.Threading.Tasks.TaskCompletionSource") || BclMethod(method, "System.Threading.Tasks.TaskCompletionSource`1")) &&
            retainsArgument) state.Handled = true;
        if (method.Name == "Throw" && BclMethod(method, "System.Runtime.ExceptionServices.ExceptionDispatchInfo"))
        {
            state.ThrowRetains = method.IsStatic ? retainsArgument : operation.Instance is { } dispatched && Retains(dispatched, state);
            state.Exit = End.Throw;
        }
        if (operation.Instance?.Syntax is ExpressionSyntax receiver && method.Name is "Add" or "AddRange" or "Enqueue" or "Push" &&
            method.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal) &&
            operation.Arguments.Any(argument => Retains(argument.Value, state))) Set(receiver, true, state);
        if (operation.Instance?.Syntax is ExpressionSyntax cleared && method.Name == "Clear" &&
            method.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal)) Set(cleared, false, state);
        if (operation.Instance?.Syntax is ExpressionSyntax data && DataOwner(data) is { } owner &&
            method.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal))
        {
            if (method.Name == "Clear") state.Data.Remove(owner);
            if (method.Name == "Remove" && operation.Arguments.Length == 1 && ConstantKey(operation.Arguments[0].Value.Syntax) is { } key &&
                state.Data.TryGetValue(owner, out var keys)) keys.Remove(key);
        }
        foreach (var argument in operation.Arguments.Where(argument => argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out))
            if (argument.Value.Syntax is ExpressionSyntax changed) Set(changed, false, state);
    }

    private bool BclMethod(IMethodSymbol method, string metadataName) =>
        !method.ContainingType.Locations.Any(location => location.IsInSource) &&
        SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, file.Model.Compilation.GetTypeByMetadataName(metadataName));

    private bool Retains(ExpressionSyntax expression, PathState state)
    {
        if (state.Values.Contains(expression) || Reference(expression) is { } reference && Has(reference, state)) return true;
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
            case ConditionalExpressionSyntax choice:
                return KnownCondition(choice.Condition, state) is { } selected ? Retains(selected ? choice.WhenTrue : choice.WhenFalse, state) :
                    Retains(choice.WhenTrue, state) && Retains(choice.WhenFalse, state);
            case SwitchExpressionSyntax choice: return choice.Arms.Count != 0 && choice.Arms.All(arm => Retains(arm.Expression, state));
            case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression): return Retains(coalesce.Left, state) && Retains(coalesce.Right, state);
            case ArrayCreationExpressionSyntax array: return array.Initializer?.Expressions.Any(value => Retains(value, state)) == true;
            case ImplicitArrayCreationExpressionSyntax array: return array.Initializer.Expressions.Any(value => Retains(value, state));
            case CollectionExpressionSyntax collection: return collection.Elements.Any(item => item is ExpressionElementSyntax value && Retains(value.Expression, state) || item is SpreadElementSyntax spread && Retains(spread.Expression, state));
            case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax:
                return file.Model.GetOperation(expression) is IObjectCreationOperation created &&
                    IsException(created.Type) && created.Arguments.Any(argument =>
                        ExceptionCarrier(argument.Parameter?.Type) &&
                        Retains(argument.Value, state));
            case InvocationExpressionSyntax call when file.Model.GetOperation(call) is IInvocationOperation invocation:
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

    private bool Has(ObjectReference reference, PathState state) => state.Objects.Contains(reference) ||
        state.Data.TryGetValue(reference, out var keys) && keys.Count != 0;

    private ObjectReference? DataOwner(SyntaxNode expression) => expression is MemberAccessExpressionSyntax data &&
        file.Model.GetSymbolInfo(data).Symbol is IPropertySymbol { Name: "Data" } property &&
        !property.Locations.Any(location => location.IsInSource) && property.ContainingType.ToDisplayString() == "System.Exception"
            ? Reference(data.Expression) : null;

    private (ObjectReference Owner, string Key)? DataSlot(SyntaxNode target) =>
        target is ElementAccessExpressionSyntax access && access.ArgumentList.Arguments.Count == 1 &&
        DataOwner(access.Expression) is { } owner && ConstantKey(access.ArgumentList.Arguments[0].Expression) is { } key ? (owner, key) : null;

    private string? ConstantKey(SyntaxNode node) => node is ExpressionSyntax expression &&
        file.Model.GetConstantValue(expression) is { HasValue: true, Value: { } value } ? value.GetType().FullName + ":" + value : null;

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
        return result;
    }

    // Bounded to direct calls of a private helper in this source file. Delegates,
    // virtual dispatch, external callers and recursive continuations stay unproved.
    private bool Complete(SyntaxNode current, List<PathState> paths)
    {
        if (paths.All(DeliveredPath)) return true;
        var declaration = current.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (declaration is null || file.Model.GetDeclaredSymbol(declaration) is not { DeclaredAccessibility: Accessibility.Private } method ||
            method.IsVirtual || method.IsOverride || !LocalType(method.ContainingType) || !continuations.Add(method)) return false;
        try
        {
            var names = file.Root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(name =>
                SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(name).Symbol, method)).ToArray();
            if (names.Length == 0) return false;
            foreach (var name in names)
            {
                var call = name.Parent is MemberAccessExpressionSyntax member ? member.Parent as InvocationExpressionSyntax : name.Parent as InvocationExpressionSyntax;
                if (call is null || call.Ancestors().Any(node => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax) ||
                    file.Model.GetOperation(call) is not IInvocationOperation invocation) return false;
                var statement = call.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
                if (statement is not ExpressionStatementSyntax and not LocalDeclarationStatementSyntax and not ThrowStatementSyntax and not ReturnStatementSyntax) return false;
                foreach (var path in paths.Where(path => !DeliveredPath(path)))
                {
                    var start = new PathState();
                    if (path.Exit == End.Return && path.ReturnRetains) start.Values.Add(call);
                    foreach (var argument in invocation.Arguments)
                        if (argument.Parameter is { } parameter && ExceptionCarrier(parameter.Type) && Has(new(parameter, ""), path) &&
                            argument.Value.Syntax is ExpressionSyntax value) Set(value, true, start);
                    if (start.Values.Count == 0 && start.Objects.Count == 0) return false;
                    var resumed = ContinueOutside(statement, Statement(statement, [start]));
                    if (!Complete(statement, resumed)) return false;
                }
            }
            return true;
        }
        finally { continuations.Remove(method); }
    }

    private bool LocalType(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.Length != 0 &&
        type.DeclaringSyntaxReferences.All(reference => reference.SyntaxTree == file.Tree) &&
        (type.ContainingType is null || LocalType(type.ContainingType));

    private static bool DeliveredPath(PathState path) => path.Handled || path.Exit == End.Throw && path.ThrowRetains;

    private void Set(SyntaxNode target, bool retains, PathState state)
    {
        if (Reference(target) is not { } reference) return;
        state.Data.Remove(reference);
        if (retains) state.Objects.Add(reference); else state.Objects.Remove(reference);
    }

    private ObjectReference? Reference(SyntaxNode target)
    {
        if (target is ParenthesizedExpressionSyntax group) return Reference(group.Expression);
        if (target is AssignmentExpressionSyntax assignment && assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
            return file.Model.GetSymbolInfo(assignment.Left).Symbol is ILocalSymbol ? Reference(assignment.Left) : null;
        var symbol = target is VariableDeclaratorSyntax variable ? file.Model.GetDeclaredSymbol(variable) :
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
        if (condition is ParenthesizedExpressionSyntax group) return KnownCondition(group.Expression, state);
        if (SourceModel.Constant(file.Model, condition) is bool constant) return constant;
        if (condition is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
            return KnownCondition(unary.Operand, state) is { } operand ? !operand : null;
        if (condition is BinaryExpressionSyntax binary && binary.Left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" } count &&
            Retains(count.Expression, state) && SourceModel.Constant(file.Model, binary.Right) is 0)
            return binary.Kind() switch { SyntaxKind.NotEqualsExpression or SyntaxKind.GreaterThanExpression => true, SyntaxKind.EqualsExpression or SyntaxKind.LessThanOrEqualExpression => false, _ => null };
        if (condition is BinaryExpressionSyntax nullCheck && SourceModel.Constant(file.Model, nullCheck.Right) is null &&
            nullCheck.Right.IsKind(SyntaxKind.NullLiteralExpression) && Retains(nullCheck.Left, state))
            return nullCheck.Kind() switch { SyntaxKind.NotEqualsExpression => true, SyntaxKind.EqualsExpression => false, _ => null };
        if (condition is IsPatternExpressionSyntax match && Retains(match.Expression, state)) return NonNullPattern(match.Pattern);
        return null;
    }

    private static bool? NonNullPattern(PatternSyntax pattern) => pattern switch
    {
        ConstantPatternSyntax constant when constant.Expression.IsKind(SyntaxKind.NullLiteralExpression) => false,
        UnaryPatternSyntax negated when negated.IsKind(SyntaxKind.NotPattern) && NonNullPattern(negated.Pattern) is { } known => !known,
        ParenthesizedPatternSyntax group => NonNullPattern(group.Pattern),
        _ => null
    };
}

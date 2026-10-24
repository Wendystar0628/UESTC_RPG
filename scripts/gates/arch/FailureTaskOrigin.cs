// Resolves the BCL task and aggregate that can carry a catch's original producer failure.
// 用法：由 FailureFlow 在同一 SourceFile 语义上下文中查询，不执行另一套失败流分析。
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

internal sealed class FailureTaskOrigin(SourceFile file, CatchClauseSyntax? origin,
    Func<ExpressionSyntax, ExpressionSyntax, bool> sameReference)
{
    public bool IsBclType(INamedTypeSymbol type, string name) => !type.Locations.Any(location => location.IsInSource) &&
        SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, file.Model.Compilation.GetTypeByMetadataName(name));

    private bool awaitedResolved;
    private ExpressionSyntax? awaitedTask;
    private readonly Dictionary<(ExpressionSyntax Task, bool Complete), bool> awaitedResults = new();
    private readonly List<(ExpressionSyntax Dictionary, bool Missing, bool Result)> registrations = [];

    public bool IsAwaited(ExpressionSyntax task, bool completeBlock = true)
    {
        var key = (task, completeBlock);
        if (awaitedResults.TryGetValue(key, out var result)) return result;
        result = origin?.Parent is TryStatementSyntax attempt && (!completeBlock || attempt.Block.Statements.Count == 1) &&
            AwaitedTask() is { } awaited && sameReference(awaited, task) &&
            SourceModel.Inherits(file.Model.GetTypeInfo(task).Type?.WithNullableAnnotation(NullableAnnotation.NotAnnotated), "System.Threading.Tasks.Task") &&
            !Executed(attempt.Block).OfType<AssignmentExpressionSyntax>().Any(assignment => sameReference(assignment.Left, task)) &&
            !Executed(origin.Block).OfType<AssignmentExpressionSyntax>().Any(assignment =>
                assignment.SpanStart < task.SpanStart && sameReference(assignment.Left, task));
        awaitedResults[key] = result;
        return result;
    }

    public bool IsAggregate(ExpressionSyntax expression, bool completeBlock = true)
    {
        if (expression is ParenthesizedExpressionSyntax group) return IsAggregate(group.Expression, completeBlock);
        if (expression is CastExpressionSyntax cast) return IsAggregate(cast.Expression, completeBlock);
        if (expression is PostfixUnaryExpressionSyntax suppressed && suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            return IsAggregate(suppressed.Operand, completeBlock);
        return expression is MemberAccessExpressionSyntax property &&
            file.Model.GetSymbolInfo(property).Symbol is IPropertySymbol { Name: "Exception" } symbol &&
            IsBclType(symbol.ContainingType, "System.Threading.Tasks.Task") && IsAwaited(property.Expression, completeBlock);
    }

    // A shutdown observer may leave a fault on the exact task already returned to its caller.
    // The boolean belongs to that task's dictionary entry, rather than to an arbitrary catch flag.
    public bool CallerKeeps(ExpressionSyntax condition, bool matched, Func<ExpressionSyntax, bool> carriesTask)
    {
        if (condition is ParenthesizedExpressionSyntax group) return CallerKeeps(group.Expression, matched, carriesTask);
        if (condition is PrefixUnaryExpressionSyntax negated && negated.IsKind(SyntaxKind.LogicalNotExpression))
            return CallerKeeps(negated.Operand, !matched, carriesTask);
        // Reject unrelated conditions before resolving snapshots or scanning the containing type.
        if (matched || condition is not (IdentifierNameSyntax or ElementAccessExpressionSyntax or BinaryExpressionSyntax) ||
            origin?.Ancestors().OfType<ForEachVariableStatementSyntax>().FirstOrDefault() is not { } loop) return false;
        var variables = Bindings(loop);
        if (variables.Length != 2 || variables[0] is not SingleVariableDesignationSyntax task || AwaitedTask() is not { } awaited ||
            !SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(awaited).Symbol, file.Model.GetDeclaredSymbol(task))) return false;
        ExpressionSyntax? source = null;
        var missing = false;
        if (condition is ElementAccessExpressionSyntax { ArgumentList.Arguments: [var key] } access && carriesTask(key.Expression))
            source = access.Expression;
        else if (condition is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.LogicalAndExpression) &&
            binary.Left is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax lookup,
                ArgumentList.Arguments: [var lookupKey, var output] } call &&
            file.Model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "TryGetValue" } && carriesTask(lookupKey.Expression) &&
            output.Expression is DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax lookupFlag } &&
            SymbolEqualityComparer.Default.Equals(file.Model.GetDeclaredSymbol(lookupFlag), file.Model.GetSymbolInfo(binary.Right).Symbol))
        { source = lookup.Expression; missing = true; }
        else if (variables[1] is SingleVariableDesignationSyntax flag && file.Model.GetDeclaredSymbol(flag) is { } ownership &&
            SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(condition).Symbol, ownership) &&
            !Executed(loop.Statement).OfType<AssignmentExpressionSyntax>().Any(write => write.SpanStart < condition.SpanStart &&
                SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(write.Left).Symbol, ownership))) source = loop.Expression;
        if (source is null || DictionarySource(loop.Expression, loop.SpanStart) is not { } dictionary ||
            DictionarySource(source, condition.SpanStart) is not { } selected || !sameReference(dictionary, selected)) return false;
        return ReturnsRegisteredTask(dictionary, missing);
    }

    public static VariableDesignationSyntax[] Bindings(ForEachVariableStatementSyntax loop) =>
        loop.Variable is DeclarationExpressionSyntax { Designation: ParenthesizedVariableDesignationSyntax tuple }
            ? tuple.Variables.ToArray() : [];

    private static IEnumerable<SyntaxNode> Executed(SyntaxNode scope) => scope.DescendantNodes(node =>
        node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax);

    private ExpressionSyntax? DictionarySource(ExpressionSyntax expression, int before)
    {
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } call &&
            file.Model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "ToArray", ContainingNamespace: { } space } &&
            space.ToDisplayString() == "System.Linq") expression = member.Expression;
        if (file.Model.GetSymbolInfo(expression).Symbol is ILocalSymbol local &&
            local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is VariableDeclaratorSyntax declaration)
        {
            var scope = declaration.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
            var value = scope is null ? null : Executed(scope).OfType<AssignmentExpressionSyntax>()
                .Where(write => write.SpanStart < before && SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(write.Left).Symbol, local))
                .OrderBy(write => write.SpanStart).LastOrDefault()?.Right ?? declaration.Initializer?.Value;
            return value is null || value.SpanStart >= expression.SpanStart ? null : DictionarySource(value, value.SpanStart);
        }
        return file.Model.GetTypeInfo(expression).Type is INamedTypeSymbol type &&
            IsBclType(type, "System.Collections.Generic.Dictionary`2") && type.TypeArguments.Length == 2 &&
            SourceModel.Inherits(type.TypeArguments[0], "System.Threading.Tasks.Task") &&
            type.TypeArguments[1].SpecialType == SpecialType.System_Boolean ? expression : null;
    }

    private bool ReturnsRegisteredTask(ExpressionSyntax dictionary, bool missing)
    {
        foreach (var cached in registrations)
            if (cached.Missing == missing && sameReference(cached.Dictionary, dictionary)) return cached.Result;
        var result = ProveRegistrations(dictionary, missing);
        registrations.Add((dictionary, missing, result));
        return result;
    }

    private bool ProveRegistrations(ExpressionSyntax dictionary, bool missing)
    {
        var type = dictionary.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (type is null || file.Model.GetSymbolInfo(dictionary).Symbol is not IFieldSymbol { IsReadOnly: true } field ||
            field.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax { Initializer.Value: { } initial } ||
            !(initial is CollectionExpressionSyntax { Elements.Count: 0 } ||
                file.Model.GetOperation(initial) is Microsoft.CodeAnalysis.Operations.IObjectCreationOperation { Arguments.Length: 0, Initializer: null })) return false;
        bool SameDictionary(ExpressionSyntax value) => DictionarySource(value, value.SpanStart) is { } selected && sameReference(selected, dictionary);
        foreach (var use in type.DescendantNodes().OfType<IdentifierNameSyntax>().Where(use => SameDictionary(use)))
            if (use.Parent is ArgumentSyntax or ReturnStatementSyntax ||
                use.Parent is AssignmentExpressionSyntax assignment && assignment.Left == use) return false;
        var caller = false;
        var allReturned = true;
        var guardedHandoff = false;
        foreach (var write in type.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (write.Left is not ElementAccessExpressionSyntax { ArgumentList.Arguments: [var key] } access || !SameDictionary(access.Expression)) continue;
            if (SourceModel.Constant(file.Model, write.Right) is not bool owned) return false;
            if (write.Ancestors().OfType<IfStatementSyntax>().Any(guard => guard.Statement.Span.Contains(write.Span) &&
                guard.Condition is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax contains,
                    ArgumentList.Arguments: [var checkedKey] } call && SameDictionary(contains.Expression) &&
                file.Model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "ContainsKey" } && sameReference(key.Expression, checkedKey.Expression)))
            { guardedHandoff |= !owned; continue; }
            var returned = RegistrationReturns(write, key.Expression);
            if (!owned && !returned) return false;
            caller |= !owned;
            allReturned &= returned;
        }
        foreach (var call in type.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax member || !SameDictionary(member.Expression) ||
                file.Model.GetSymbolInfo(call).Symbol is not IMethodSymbol method || method.Name is not ("Add" or "TryAdd")) continue;
            if (call.ArgumentList.Arguments is not [var key, var flag]) return false;
            var ownership = SourceModel.Constant(file.Model, flag.Expression);
            if (ownership is not bool && file.Model.GetSymbolInfo(flag.Expression).Symbol is not IParameterSymbol {
                Type.SpecialType: SpecialType.System_Boolean, HasExplicitDefaultValue: true, ExplicitDefaultValue: false }) return false;
            var barrier = NeverFaults(key.Expression);
            var returned = barrier || RegistrationReturns(call, key.Expression);
            if (ownership is not true && !returned) return false;
            caller |= ownership is not true && !barrier;
            allReturned &= returned;
        }
        // Missing entries or a later ownership handoff are safe only for tasks that already have a caller.
        return caller && (!(missing || guardedHandoff) || allReturned);
    }

    private bool RegistrationReturns(SyntaxNode registration, ExpressionSyntax task)
    {
        var method = registration.Ancestors().FirstOrDefault(node => node is AnonymousFunctionExpressionSyntax or
            LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax);
        if (method is null) return false;
        var scope = registration.Ancestors().TakeWhile(node => node != method).OfType<BlockSyntax>()
            .FirstOrDefault(block => block.Statements.LastOrDefault() is ReturnStatementSyntax { Expression: { } value } && sameReference(task, value));
        if (scope is null) return false;
        // Nested callback exits do not exit the registering method. Explicit throws must be caught before its return.
        return !Executed(scope).Any(node => node.SpanStart > registration.SpanStart && node switch
        {
            AssignmentExpressionSyntax write => sameReference(write.Left, task) ||
                task is MemberAccessExpressionSyntax member && sameReference(write.Left, member.Expression),
            ReturnStatementSyntax exit => exit.Expression is null || !sameReference(task, exit.Expression),
            ThrowStatementSyntax thrown => !ExclusiveGuards(registration, thrown) && !CaughtBeforeReturn(thrown, scope),
            ThrowExpressionSyntax => true,
            _ => false
        });
    }

    private bool ExclusiveGuards(SyntaxNode registration, SyntaxNode exit)
    {
        static (ExpressionSyntax Value, bool Positive) Test(ExpressionSyntax condition) => condition is PrefixUnaryExpressionSyntax negated &&
            negated.IsKind(SyntaxKind.LogicalNotExpression) ? (negated.Operand, false) : (condition, true);
        foreach (var first in registration.Ancestors().OfType<IfStatementSyntax>())
        foreach (var second in exit.Ancestors().OfType<IfStatementSyntax>())
        {
            var left = Test(first.Condition); var right = Test(second.Condition);
            var leftBranch = first.Statement.Span.Contains(registration.Span);
            var rightBranch = second.Statement.Span.Contains(exit.Span);
            if (sameReference(left.Value, right.Value) && (left.Positive == leftBranch) != (right.Positive == rightBranch)) return true;
        }
        return false;
    }

    private bool CaughtBeforeReturn(ThrowStatementSyntax thrown, BlockSyntax scope) => thrown.Ancestors().TakeWhile(node => node != scope)
        .OfType<TryStatementSyntax>().Any(attempt => attempt.Block.Span.Contains(thrown.Span) && attempt.Catches.Any(clause =>
            clause.Filter is null && (clause.Declaration is null || file.Model.GetTypeInfo(clause.Declaration.Type).Type?.ToDisplayString() == "System.Exception")));

    private bool NeverFaults(ExpressionSyntax task)
    {
        if (task is not MemberAccessExpressionSyntax member || file.Model.GetSymbolInfo(member).Symbol is not IPropertySymbol { Name: "Task" } property ||
            !(IsBclType(property.ContainingType, "System.Threading.Tasks.TaskCompletionSource") ||
                IsBclType(property.ContainingType, "System.Threading.Tasks.TaskCompletionSource`1")) ||
            file.Model.GetSymbolInfo(member.Expression).Symbol is not ILocalSymbol local ||
            local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax { Initializer.Value: { } created } declaration ||
            file.Model.GetOperation(created) is not Microsoft.CodeAnalysis.Operations.IObjectCreationOperation ||
            declaration.Ancestors().OfType<BlockSyntax>().FirstOrDefault() is not { } scope) return false;
        var completes = false;
        foreach (var use in scope.DescendantNodes().OfType<IdentifierNameSyntax>().Where(use =>
            SymbolEqualityComparer.Default.Equals(file.Model.GetSymbolInfo(use).Symbol, local)))
        {
            if (use.Parent is not MemberAccessExpressionSyntax access) return false;
            if (file.Model.GetSymbolInfo(access).Symbol is IPropertySymbol { Name: "Task" }) continue;
            if (access.Parent is not InvocationExpressionSyntax call || file.Model.GetSymbolInfo(call).Symbol is not IMethodSymbol setter ||
                setter.Name is not ("SetResult" or "TrySetResult" or "SetCanceled" or "TrySetCanceled")) return false;
            completes = true;
        }
        return completes;
    }

    private ExpressionSyntax? AwaitedTask()
    {
        if (awaitedResolved) return awaitedTask;
        awaitedResolved = true;
        if (origin?.Parent is not TryStatementSyntax attempt) return null;
        var awaited = attempt.Block.Statements.FirstOrDefault() switch
        {
            ExpressionStatementSyntax { Expression: AwaitExpressionSyntax expression } => expression.Expression,
            ReturnStatementSyntax { Expression: AwaitExpressionSyntax expression } => expression.Expression,
            _ => null
        };
        while (awaited is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } call &&
            file.Model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "ConfigureAwait" } method &&
            (IsBclType(method.ContainingType, "System.Threading.Tasks.Task") || IsBclType(method.ContainingType, "System.Threading.Tasks.Task`1")))
            awaited = member.Expression;
        return awaitedTask = awaited;
    }
}

// Keeps semantic gates sensitive to object flow and declaration meaning rather than spelling.
// 用法：经 scripts/gate.ps1 的 G4 入口运行。
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class SemanticPolicyRegressionTests
{
    [TestCase]
    [TestCategory("Regression")]
    public void UnreadableSemanticInputsFailTheGate()
    {
        // A file cannot serve as a repository directory; neither loader may turn this read failure into an empty input.
        var root = Path.Combine(ArchitectureTests.FindRepository(), "third_party.json");
        GdUnit4.Assertions.AssertThrown(() => { _ = SemanticPolicySources.LoadFiles(root); }).IsInstanceOf<IOException>();
        GdUnit4.Assertions.AssertThrown(() => { _ = SemanticPolicySources.LoadDocuments(root); }).IsInstanceOf<IOException>();
    }

    [TestCase]
    [TestCategory("Regression")]
    public void CatchPathsPreserveTheOriginalFailure()
    {
        var equivalentSuffix = string.Concat(Enumerable.Repeat("_ = flag && flag; _ = flag || flag; _ = marker ?? marker;", 12));
        var file = Fixture("""
            class Sample {
                static void Handle([FailureHandler("ENG-ARCH")] Exception error) { }
                static void Unknown([FailureHandler("DOES-NOT-EXIST")] Exception error) { }
                static void Log(string message) { }
                static void Run() { }
                System.Threading.Tasks.TaskCompletionSource? owned;
                sealed class Receiver { public void Keep([FailureHandler("ENG-ARCH")] Exception error) { } }
                class TaskCompletionSource {
                    public void SetException(Exception error) { }
                    public void SetException(string message) { }
                    public bool TrySetException(Exception error) => true;
                }
                class TaskCompletionSource<T> {
                    public void SetException(Exception error) { }
                    public bool TrySetException(Exception error) => true;
                }
                class ExceptionDispatchInfo {
                    public static ExceptionDispatchInfo Capture(Exception error) => new();
                    public void Throw() { }
                    public static void Throw(Exception error) { }
                }
                sealed class StartupFailure(string message, Exception cause) : Exception(message, cause) {
                    public static StartupFailure Stalled(string text, Exception renamed) => new(text, renamed);
                }
                static Exception ChooseFailure(string message, Exception renamed, bool flag) {
                    var alias = renamed;
                    if(flag) return StartupFailure.Stalled(renamed: alias, text: message);
                    return new StartupFailure(message, alias);
                }
                static Exception HandledFactory(Exception cause) { Handle(cause); return new Exception("x", cause); }
                static Exception HandledReplacement(Exception cause) { Handle(cause); return new Exception("x"); }
                static Exception MessageFailure(Exception cause) => new Exception(cause.Message);
                static Exception? NullableFailure(Exception? cause) => new InvalidOperationException("x", cause);
                static Exception OptionalFailure(Exception? cause = null) => new InvalidOperationException("x", cause);
                static Exception RecursiveFailure(Exception cause) => RecursiveFailure(cause);
                static Exception MutualFailure(Exception cause) => OtherFailure(cause);
                static Exception OtherFailure(Exception cause) => MutualFailure(cause);
                static Exception Accumulate(Exception? kept, Exception next) {
                    if(ReferenceEquals(kept, next)) return next;
                    if(next is AggregateException { InnerExceptions.Count: > 0 } many) {
                        foreach(var item in many.InnerExceptions) kept = Accumulate(kept, item);
                        return kept!;
                    }
                    if(kept is null) return next;
                    return new AggregateException(kept, next);
                }
                static Exception LosingAccumulator(Exception kept, Exception next, bool repeat) {
                    if(repeat) return LosingAccumulator(next, kept, false);
                    return kept;
                }
                sealed class ManyFailure(params Exception[] causes) : AggregateException(causes) { }
                sealed class CollectedFailure(params IEnumerable<Exception> causes) : AggregateException(causes) { }
                static Exception UnrelatedFailure(Exception cause) => new Exception("x", new Exception());
                static Exception LosingFailure(Exception cause, bool flag) {
                    if(flag) return new Exception("x", cause);
                    return new Exception("x");
                }
                void GoodThrow() { try { Run(); } catch { throw; } }
                void GoodWrap() { try { Run(); } catch(Exception e) { throw new InvalidOperationException("x", e); } }
                void GoodFactory(bool flag) { try { Run(); } catch(Exception e) { throw ChooseFailure(flag: flag, renamed: e, message: "x"); } }
                void GoodFactoryAfterHandler() { try { Run(); } catch(Exception e) { throw HandledFactory(e); } }
                void GoodNullableFactory() { try { Run(); } catch(Exception e) { throw NullableFailure(e)!; } }
                void GoodOptionalFactory() { try { Run(); } catch(Exception e) { throw OptionalFailure(e); } }
                void GoodRecursiveAggregate() { try { Run(); } catch(Exception e) { throw Accumulate(e, new AggregateException(new Exception())); } }
                void GoodAggregateParams() { try { Run(); } catch(Exception e) { throw new AggregateException("x", new Exception(), e); } }
                void GoodNestedAggregateParams() { try { Run(); } catch(Exception e) { throw new AggregateException(new AggregateException(e)); } }
                void GoodTargetTypedParams() { try { Run(); } catch(Exception e) { AggregateException failure = new("x", e, new Exception()); throw failure; } }
                void GoodExplicitArray() { try { Run(); } catch(Exception e) { throw new AggregateException(new Exception[] { new Exception(), e }); } }
                void GoodArrayParams() { try { Run(); } catch(Exception e) { throw new ManyFailure(e); } }
                void GoodCollectedParams() { try { Run(); } catch(Exception e) { throw new CollectedFailure(new Exception(), e); } }
                void GoodReceive() { try { Run(); } catch(Exception e) { var renamed = e; Handle(renamed); } }
                void GoodNamed() { try { Run(); } catch(Exception e) { Handle(error: e); } }
                void GoodNullableHandler(Receiver? receiver) { try { Run(); } catch(Exception e) { receiver?.Keep(e); if(receiver is null) throw; } }
                int GoodInput() { try { Run(); } catch(System.IO.InvalidDataException e) { Handle(e); return 0; } return 1; }
                void GoodCollect() { var errors = new List<Exception>(); try { Run(); } catch(Exception e) { errors.Add(e); } if(errors.Count != 0) throw new AggregateException(errors); }
                void GoodCompletionSet() { var done = new System.Threading.Tasks.TaskCompletionSource(); try { Run(); } catch(Exception e) { done.SetException(e); } }
                void GoodCompletionTrySet() { var done = new System.Threading.Tasks.TaskCompletionSource(); try { Run(); } catch(Exception e) { done.TrySetException(e); } }
                void GoodGenericCompletionSet() { var done = new System.Threading.Tasks.TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.SetException(e); } }
                void GoodGenericCompletionTrySet() { var done = new System.Threading.Tasks.TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.TrySetException(e); } }
                void GoodCompletionCollection() { var done = new System.Threading.Tasks.TaskCompletionSource(); try { Run(); } catch(Exception e) { done.SetException(new Exception[] { new Exception(), e }); } }
                void GoodGenericCompletionCollection() { var done = new System.Threading.Tasks.TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.TrySetException(new[] { e }); } }
                void GoodConditionalCompletion(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { done?.TrySetException(e); if(done is null) throw; } }
                void GoodAssertedOwnedCompletion() { try { Run(); } catch(Exception e) { owned!.TrySetException(e); } }
                void GoodAssertedCompletionContract(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { done!.SetException(e); } }
                void GoodConditionalGenericCompletion(System.Threading.Tasks.TaskCompletionSource<int>? done) { try { Run(); } catch(Exception e) { done?.SetException(e); if(null == done) throw; } }
                void GoodGuardedCompletion(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { if(done is not null) done?.TrySetException(e); else throw; } }
                void GoodConditionalDispatch() { try { Run(); } catch(Exception e) { var failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e); failure?.Throw(); } }
                Exception GoodReferenceFilter(Exception original) { try { Run(); } catch(Exception e) when(ReferenceEquals(e, original)) { return original; } return new Exception(); }
                Exception GoodReferenceNegated(Exception original) { try { Run(); } catch(Exception e) { if(!ReferenceEquals(original, e)) throw; return original; } return new Exception(); }
                Exception GoodReferenceElse(Exception original) { try { Run(); } catch(Exception e) { if(ReferenceEquals(original, e)) return original; else throw; } return new Exception(); }
                System.Threading.Tasks.Task GoodFailureTask() { try { Run(); } catch(Exception e) { return System.Threading.Tasks.Task.FromException(e); } return System.Threading.Tasks.Task.CompletedTask; }
                System.Threading.Tasks.Task<int> GoodGenericFailureTask() { try { Run(); } catch(Exception e) { return System.Threading.Tasks.Task.FromException<int>(e); } return System.Threading.Tasks.Task.FromResult(1); }
                async System.Threading.Tasks.Task<System.Threading.Tasks.Task> GoodHeldTask(System.Threading.Tasks.Task owned) { try { await owned.ConfigureAwait(false); } catch(Exception e) { return owned; } return owned; }
                async System.Threading.Tasks.Task<Exception?> GoodHeldTaskFailure(System.Threading.Tasks.Task owned) { try { await owned; } catch(Exception e) { return owned.Exception; } return null; }
                static Exception Append(Exception? retained, [FailureHandler("ENG-ARCH")] Exception error) => retained is null ? error : new AggregateException(retained, error);
                void GoodRetainedAppend() { Exception? failure = null; try { Run(); } catch(Exception e) { failure = Append(failure, e); } if(failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
                void GoodReflectionUnwrap() { try { Run(); } catch(System.Reflection.TargetInvocationException e) when(e.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; } }
                void GoodReflectionWrap() { try { Run(); } catch(System.Reflection.TargetInvocationException e) { throw new InvalidOperationException("x", e.InnerException ?? e); } }
                async System.Threading.Tasks.Task GoodTaskInnerFailures(System.Threading.Tasks.Task owned) { var failures = new List<Exception>(); try { await owned.ConfigureAwait(false); } catch { failures.AddRange(owned.Exception!.InnerExceptions); } if(failures.Count > 0) throw new AggregateException(failures); }
                void GoodGuardedCatchReceiver(Receiver? receiver) { try { if(receiver is not null) Run(); } catch(Exception e) { receiver!.Keep(e); } }
                void GoodDispatchCapture() { try { Run(); } catch(Exception e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw(); } }
                void GoodDispatchStatic() { try { Run(); } catch(Exception e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(e); } }
                void GoodDispatchOutside() { Exception? failure = null; try { Run(); } catch(Exception e) { failure = e; } if(failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
                void GoodCapturedDispatchOutside() { System.Runtime.ExceptionServices.ExceptionDispatchInfo? failure = null; try { Run(); } catch(Exception e) { failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e); } if(failure != null) failure.Throw(); }
                void GoodCoalesceCollect() { List<Exception>? errors = null; try { Run(); } catch(Exception e) { (errors ??= []).Add(e); } if(errors is not null) throw new AggregateException(errors); }
                void GoodCoalesceRetains() { List<Exception>? errors = null; try { Run(); } catch(Exception e) { (errors ??= []).Add(e); errors ??= []; } if(errors != null) throw new AggregateException(errors); }
                void GoodConditionalCollect() { Exception? failure = null; try { Run(); } catch(Exception e) { failure = e; } try { Run(); } catch(Exception e) { failure = failure is null ? e : new AggregateException(failure, e); } if(failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
                void GoodEquivalentLocalSuffix(bool flag, string? marker) { Exception? failure = null; try { Run(); } catch(Exception e) { failure = e; } /* equivalent suffix */ if(failure is not null) throw failure; }
                void BadEquivalentLocalSuffixCleared(bool flag, string? marker) { Exception? failure = null; try { Run(); } catch(Exception e) { failure = e; } /* equivalent suffix */ failure = null; if(failure is not null) throw failure; }
                void GoodEquivalentCollectionSuffix(bool flag, string? marker) { var failures = new List<Exception>(); try { Run(); } catch(Exception e) { failures.Add(e); } /* equivalent suffix */ throw new AggregateException(failures); }
                void BadEquivalentCollectionSuffixCleared(bool flag, string? marker) { var failures = new List<Exception>(); try { Run(); } catch(Exception e) { failures.Add(e); } /* equivalent suffix */ failures.Clear(); throw new AggregateException(failures); }
                void GoodEquivalentDispatchSuffix(bool flag, string? marker) { System.Runtime.ExceptionServices.ExceptionDispatchInfo? failure = null; try { Run(); } catch(Exception e) { failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e); } /* equivalent suffix */ if(failure is not null) failure.Throw(); }
                void BadEquivalentDispatchSuffixCleared(bool flag, string? marker) { System.Runtime.ExceptionServices.ExceptionDispatchInfo? failure = null; try { Run(); } catch(Exception e) { failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e); } /* equivalent suffix */ failure = null; if(failure is not null) failure.Throw(); }
                void GoodThrowThroughFinally() { try { try { Run(); } catch(Exception e) { throw e; } } finally { Run(); } }
                Exception GoodReturnThroughFinally() { try { try { Run(); } catch(Exception e) { return e; } return new Exception(); } finally { Run(); } }
                void BadThrowReplacedByFinally() { try { try { Run(); } catch(Exception e) { throw e; } } finally { throw new Exception(); } }
                Exception BadReturnReplacedByFinally() { try { try { Run(); } catch(Exception e) { return e; } return new Exception(); } finally { throw new Exception(); } }
                void BadOuterCatchSwallows() { try { try { Run(); } catch(Exception e) { throw e; } } catch(Exception swallowed) { Log(swallowed.Message); } }
                void GoodHandledBeforeFinally() { try { try { Run(); } catch(Exception e) { Handle(e); } } finally { throw new Exception(); } }
                void BadConditionalDrops() { Exception? failure = null; try { Run(); } catch(Exception e) { failure = e; } failure = null; if(failure is not null) throw failure; }
                void GoodNestedThrow() { try { Run(); } catch(Exception e) { try { throw e; } catch(Exception forwarded) { Handle(forwarded); } } }
                void BadNestedSwallows() { try { Run(); } catch(Exception e) { try { throw e; } catch(Exception forwarded) { Log(forwarded.Message); } } }
                void GoodNestedCleanup() { try { Run(); } catch(Exception failure) { try { throw new Exception(); } catch(Exception cleanup) { failure.Data["cleanup"] = cleanup; } throw; } }
                void GoodCancel() { try { Run(); } catch(OperationCanceledException) { } }
                void GoodFilter() { try { Run(); } catch(Exception e) when(e is OperationCanceledException) { } }
                void BadBranch(bool flag) { try { Run(); } catch(Exception e) { if(flag) Handle(e); } }
                void BadThrow() { try { Run(); } catch(Exception e) { throw new InvalidOperationException("x"); } }
                void BadInnerReplacesCaught() { try { Run(); } catch(Exception e) { throw e.InnerException!; } }
                void BadFactoryReturnAfterHandler() { try { Run(); } catch(Exception e) { throw HandledReplacement(e); } }
                void BadFactoryMessage() { try { Run(); } catch(Exception e) { throw MessageFailure(e); } }
                void BadOptionalFactory() { try { Run(); } catch(Exception e) { throw OptionalFailure(); } }
                void BadRecursiveFactory() { try { Run(); } catch(Exception e) { throw RecursiveFailure(e); } }
                void BadMutualFactory() { try { Run(); } catch(Exception e) { throw MutualFailure(e); } }
                void BadRecursiveParameterSwap(bool flag) { try { Run(); } catch(Exception e) { throw LosingAccumulator(e, new Exception(), flag); } }
                void BadAggregateParams() { try { Run(); } catch(Exception e) { throw new AggregateException("x", new Exception(), new Exception()); } }
                void BadNestedAggregateParams() { try { Run(); } catch(Exception e) { throw new AggregateException(new AggregateException(new Exception())); } }
                void BadTargetTypedParams() { try { Run(); } catch(Exception e) { AggregateException failure = new("x", new Exception(), new Exception()); throw failure; } }
                void BadExplicitArray() { try { Run(); } catch(Exception e) { throw new AggregateException(new Exception[] { new Exception() }); } }
                void BadEmptyParams() { try { Run(); } catch(Exception e) { throw new ManyFailure(); } }
                void BadCollectedParams() { try { Run(); } catch(Exception e) { throw new CollectedFailure(new Exception()); } }
                void BadEmptyCollectedParams() { try { Run(); } catch(Exception e) { throw new CollectedFailure(); } }
                void BadFactoryUnrelated() { try { Run(); } catch(Exception e) { throw UnrelatedFailure(e); } }
                void BadFactoryBranch(bool flag) { try { Run(); } catch(Exception e) { throw LosingFailure(e, flag); } }
                void BadText() { try { Run(); } catch(Exception e) { Log(e.Message); } }
                void BadLambda() { try { Run(); } catch(Exception e) { Action later = () => Handle(e); } }
                void BadCompletionSameName() { var done = new TaskCompletionSource(); try { Run(); } catch(Exception e) { done.SetException(e); } }
                void BadCompletionTrySameName() { var done = new TaskCompletionSource(); try { Run(); } catch(Exception e) { done.TrySetException(e); } }
                void BadGenericCompletionSameName() { var done = new TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.SetException(e); } }
                void BadGenericCompletionTrySameName() { var done = new TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.TrySetException(e); } }
                void BadCompletionMessageOnly() { var done = new TaskCompletionSource(); try { Run(); } catch(Exception e) { done.SetException(e.Message); } }
                void BadCompletionNewFailure() { var done = new System.Threading.Tasks.TaskCompletionSource(); try { Run(); } catch(Exception e) { done.TrySetException(new Exception(e.Message)); } }
                void BadGenericCompletionNewFailure() { var done = new System.Threading.Tasks.TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.SetException(new Exception()); } }
                void BadCompletionCollection() { var done = new System.Threading.Tasks.TaskCompletionSource(); try { Run(); } catch(Exception e) { done.SetException(new[] { new Exception() }); } }
                void BadGenericCompletionCollection() { var done = new System.Threading.Tasks.TaskCompletionSource<int>(); try { Run(); } catch(Exception e) { done.TrySetException(new[] { new Exception(e.Message) }); } }
                void BadNullCompletion() { System.Threading.Tasks.TaskCompletionSource? done = null; try { Run(); } catch(Exception e) { done!.SetException(e); } }
                void BadNullableCompletion(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { done?.TrySetException(e); } }
                void BadNullableCompletionMissingPath(System.Threading.Tasks.TaskCompletionSource? done, bool flag) { try { Run(); } catch(Exception e) { done?.TrySetException(e); if(done is null && flag) throw; } }
                void BadNullableCompletionReplacement(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { done?.SetException(e); if(done is null) throw new Exception(e.Message); } }
                void BadNullableDirectCompletion(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { done.TrySetException(e); } }
                void BadNullCompletionThenThrow() { try { Run(); } catch(Exception e) { owned = null; owned!.SetException(e); throw; } }
                void BadAssignedNullCompletion() { var done = new System.Threading.Tasks.TaskCompletionSource(); done = null; try { Run(); } catch(Exception e) { done!.SetException(e); } }
                void BadNullableHandler(Receiver? receiver) { try { Run(); } catch(Exception e) { receiver?.Keep(e); } }
                void BadGuardMutation(Receiver? receiver) { try { if(receiver is not null) { receiver = null; Run(); } } catch(Exception e) { receiver.Keep(e); } }
                IEnumerable<Exception> BadAggregateRootDrops() { try { Run(); } catch(AggregateException e) { return e.InnerExceptions; } return []; }
                void BadCompletionReceiverOverwrite(System.Threading.Tasks.TaskCompletionSource? done) { try { Run(); } catch(Exception e) { if(done is not null) { done = null; done?.TrySetException(e); } else throw; } }
                Exception BadReferenceReplacement(Exception original) { try { Run(); } catch(Exception e) { if(ReferenceEquals(original, e)) return new Exception(); throw; } return new Exception(); }
                System.Threading.Tasks.Task BadUnrelatedFailureTask() { try { Run(); } catch(Exception e) { return System.Threading.Tasks.Task.FromException(new Exception(e.Message)); } return System.Threading.Tasks.Task.CompletedTask; }
                void BadUnownedFailureTask() { try { Run(); } catch(Exception e) { _ = System.Threading.Tasks.Task.FromException(e); } }
                async System.Threading.Tasks.Task<System.Threading.Tasks.Task> BadUnrelatedTask(System.Threading.Tasks.Task owned, System.Threading.Tasks.Task other) { try { await owned; } catch(Exception e) { return other; } return owned; }
                async System.Threading.Tasks.Task<Exception?> BadUnrelatedTaskFailure(System.Threading.Tasks.Task owned, System.Threading.Tasks.Task other) { try { await owned; } catch(Exception e) { return other.Exception; } return null; }
                async System.Threading.Tasks.Task<System.Threading.Tasks.Task> BadTimedWaitTask(System.Threading.Tasks.Task owned) { try { await owned.WaitAsync(TimeSpan.Zero); } catch(Exception e) { return owned; } return owned; }
                async System.Threading.Tasks.Task<System.Threading.Tasks.Task> BadMultipleAwaitTask(System.Threading.Tasks.Task owned, System.Threading.Tasks.Task other) { try { await owned; await other; } catch(Exception e) { return owned; } return owned; }
                async System.Threading.Tasks.Task<System.Threading.Tasks.Task> BadReplacedTask(System.Threading.Tasks.Task owned) { try { await owned; owned = System.Threading.Tasks.Task.CompletedTask; } catch(Exception e) { return owned; } return owned; }
                async System.Threading.Tasks.Task<Exception?> BadPostAwaitFailure(System.Threading.Tasks.Task owned) { try { await owned; Run(); } catch(Exception e) { return owned.Exception; } return null; }
                async System.Threading.Tasks.Task<Exception> GoodPostAwaitFallback(System.Threading.Tasks.Task owned) { try { await owned; Run(); } catch(Exception e) { return (Exception?)owned.Exception ?? e; } return new Exception(); }
                void BadAppendReplacement() { try { Run(); } catch(Exception e) { _ = Append(null, new Exception(e.Message)); } }
                void BadCompletionLambda() { var done = new System.Threading.Tasks.TaskCompletionSource(); try { Run(); } catch(Exception e) { Action later = () => done.SetException(e); } }
                void BadDispatchCaptureOnly() { try { Run(); } catch(Exception e) { _ = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e); } }
                void BadDispatchNewFailure() { try { Run(); } catch(Exception e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(new Exception(e.Message)).Throw(); } }
                void BadDispatchStaticNewFailure() { try { Run(); } catch(Exception e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(new Exception()); } }
                void BadDispatchSameName() { try { Run(); } catch(Exception e) { ExceptionDispatchInfo.Capture(e).Throw(); } }
                void BadDispatchStaticSameName() { try { Run(); } catch(Exception e) { ExceptionDispatchInfo.Throw(e); } }
                void BadDispatchOverwrite() { try { Run(); } catch(Exception e) { var failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e); failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(new Exception()); failure.Throw(); } }
                void BadDispatchLambda() { try { Run(); } catch(Exception e) { Action later = () => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw(); } }
                void BadCoalesceClear() { List<Exception>? errors = null; try { Run(); } catch(Exception e) { (errors ??= []).Add(e); errors.Clear(); } if(errors is not null) throw new AggregateException(errors); }
                void BadCoalesceEmpty() { List<Exception>? errors = null; try { Run(); } catch(Exception e) { (errors ??= []).Add(e); errors = []; } if(errors is not null) throw new AggregateException(errors); }
                void BadCoalesceNull() { List<Exception>? errors = null; try { Run(); } catch(Exception e) { (errors ??= []).Add(e); errors = null; } if(errors is not null) throw new AggregateException(errors); }
                void BadCoalesceOtherCollection() { List<Exception>? errors = null; var other = new List<Exception>(); try { Run(); } catch(Exception e) { (errors ??= []).Add(e); } if(errors is not null) throw new AggregateException(other); }
                void BadDocument() { try { Run(); } catch(Exception e) { Unknown(e); } }
                void BadCancel(bool flag) { try { Run(); } catch(Exception) when(flag) { } }
                void BadOverwrite() { try { Run(); } catch(Exception e) { var renamed = e; renamed = null; throw renamed; } }
                class Carrier { public Exception Failure; }
                void BadReceiver() { var left = new Carrier(); var right = new Carrier(); try { Run(); } catch(Exception e) { left.Failure = e; throw right.Failure; } }
            }
            """.Replace("/* equivalent suffix */", equivalentSuffix, StringComparison.Ordinal));
        var mismatches = new List<string>();
        foreach (var clause in file.Root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            var method = clause.Ancestors().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText;
            var actual = FailureFlow.Delivered(file, clause, new HashSet<string>(StringComparer.Ordinal) { "ENG-ARCH" });
            if (actual != method.StartsWith("Good", StringComparison.Ordinal))
                mismatches.Add(method + " (actual: " + actual + ")");
        }
        if (mismatches.Count != 0)
            throw new InvalidOperationException("Exception provenance regression: " + string.Join(", ", mismatches));
    }

    [TestCase]
    [TestCategory("Regression")]
    public void ShutdownObserversKeepTheRegisteredCallerTask()
    {
        const string returned = "Task Register(Task task, bool own = false) { tasks.Add(task, own); return task; }";
        const string direct = "foreach(var (task, own) in pending) { try { await task; } catch(Exception) when(!own) { _ = task.Exception; } catch { throw; } }";
        const string tuple = "var errors = new List<(Task Operation, Exception Error)>(); foreach(var (task, _) in pending) { try { await task.ConfigureAwait(false); } catch(Exception error) { errors.Add((task, task.Exception ?? error)); } } foreach(var (operation, error) in errors) if(tasks[operation]) Handle(error);";
        const string lookup = "if(tasks.TryGetValue(operation, out var ownsFailure) && ownsFailure)";
        var cases = new (string Name, string Register, string Drain, bool Expected)[]
        {
            ("returned observer", returned, direct, true),
            ("guarded caller registration", returned.Replace("tasks.Add(task, own);", "if(!task.IsCompleted) tasks.Add(task, false);"), direct, true),
            ("returned tuple", returned, tuple, true),
            ("lookup after removed returned task", returned + " void Release(Task task) { tasks.Remove(task); }", tuple.Replace("if(tasks[operation])", lookup), true),
            ("guarded ownership handoff", returned + " void Handoff(Task task) { if(tasks.ContainsKey(task)) tasks[task] = false; }", tuple.Replace("if(tasks[operation])", lookup), true),
            ("nested callback return", returned.Replace("return task;", "Action queued = () => { if(flag) return; }; return task;"), direct, true),
            ("success-only barrier", returned + " void Barrier() { var stopped = new TaskCompletionSource(); tasks.Add(stopped.Task, false); try { RunBarrier(); } finally { stopped.TrySetResult(); tasks.Remove(stopped.Task); } } void RunBarrier() { }", tuple.Replace("if(tasks[operation])", lookup), true),
            ("exclusive registration and throw", returned.Replace("tasks.Add(task, own);", "if(!flag) tasks.Add(task, own); if(flag) throw new Exception();"), direct, true),
            ("index registration returns caller", returned.Replace("tasks.Add(task, own);", "tasks[task] = false;"), direct, true),
            ("index registration loses caller", "void Register(Task task) { tasks[task] = false; }", direct, false),
            ("alias index registration loses caller", "void Register(Task task) { var alias = tasks; alias[task] = false; }", direct, false),
            ("alias add registration loses caller", "void Register(Task task) { var alias = tasks; alias.Add(task, false); }", direct, false),
            ("explicit throw after registration", returned.Replace("return task;", "if(flag) throw new Exception(); return task;"), direct, false),
            ("faultable unreturned barrier", returned + " void Barrier() { var stopped = new TaskCompletionSource(); tasks.Add(stopped.Task, false); stopped.TrySetException(new Exception()); }", direct, false),
            ("escaped barrier", returned + " void Barrier(Action<TaskCompletionSource> leak) { var stopped = new TaskCompletionSource(); tasks.Add(stopped.Task, false); leak(stopped); stopped.TrySetResult(); }", direct, false),
            ("unknown missing owned registration", returned + " void RegisterOwned(Task task) { tasks.Add(task, true); } void Release(Task task) { tasks.Remove(task); }", tuple.Replace("if(tasks[operation])", lookup), false),
            ("wrong lookup key", returned, tuple.Replace("if(tasks[operation])", "if(tasks.TryGetValue(other, out var ownsFailure) && ownsFailure)"), false),
            ("wrong lookup owner", returned, tuple.Replace("if(tasks[operation])", "if(others.TryGetValue(operation, out var ownsFailure) && ownsFailure)"), false),
            ("wrong handoff key", returned + " void Handoff(Task task) { if(tasks.ContainsKey(other)) tasks[task] = false; }", tuple.Replace("if(tasks[operation])", lookup), false),
            ("locked registration and snapshot", returned.Replace("tasks.Add(task, own);", "lock(tasks) { if(!flag) tasks.Add(task, own); }"),
                "KeyValuePair<Task,bool>[] saved; lock(tasks) saved = tasks.ToArray(); " + tuple.Replace("pending", "saved"), true),
            ("replaced task completion owner", "Task Register() { var done = new TaskCompletionSource(); tasks.Add(done.Task, false); done = new TaskCompletionSource(); return done.Task; }", direct, false),
            ("dictionary escapes to unknown writer", returned + " void Escape(Action<Dictionary<Task,bool>> mutate) { mutate(tasks); }", tuple.Replace("if(tasks[operation])", lookup), false),
            ("different returned task", returned.Replace("return task;", "return Task.CompletedTask;"), direct, false),
            ("different observed task", returned, direct.Replace("_ = task.Exception", "_ = other.Exception"), false),
            ("unrelated flag", returned, direct.Replace("when(!own)", "when(!flag)"), false),
            ("missing owner guard", returned, direct.Replace(" when(!own)", ""), false),
            ("timed wait", returned, direct.Replace("await task;", "await task.WaitAsync(TimeSpan.Zero);"), false),
            ("different awaited task", returned, direct.Replace("await task;", "await other;").Replace("_ = task.Exception", "_ = other.Exception"), false),
            ("different tuple task", returned, tuple.Replace("errors.Add((task,", "errors.Add((other,"), false),
            ("replacement tuple failure", returned, tuple.Replace("task.Exception ?? error", "new Exception()"), false),
            ("missing owned delivery", returned, tuple.Replace("Handle(error);", "{ }"), false),
            ("replacement owned failure", returned, tuple.Replace("Handle(error);", "Handle(new Exception());"), false),
            ("uncovered owned branch", returned, tuple.Replace("if(tasks[operation])", "if(tasks[operation] && flag)"), false),
        };
        // Compile the independent ownership samples together; each still has its own registry and method symbols.
        var file = Fixture(string.Join(Environment.NewLine, cases.Select((item, index) => $$"""
            namespace Ownership{{index}} {
                using System.Linq;
                using System.Threading.Tasks;
                class Sample {
                    readonly Dictionary<Task, bool> tasks = [];
                    readonly Task other = Task.CompletedTask;
                    readonly Dictionary<Task, bool> others = [];
                    bool flag;
                    static void Handle([FailureHandler("ENG-ARCH")] Exception error) { }
                    {{item.Register}}
                    async Task Drain() { var pending = tasks.ToArray(); {{item.Drain}} }
                }
            }
            """)));
        var samples = file.Root.DescendantNodes().OfType<NamespaceDeclarationSyntax>()
            .Where(space => space.Name.ToString().StartsWith("Ownership", StringComparison.Ordinal)).ToArray();
        for (var index = 0; index < cases.Length; index++)
        {
            var clause = samples[index].DescendantNodes().OfType<CatchClauseSyntax>().First();
            if (FailureFlow.Delivered(file, clause, new HashSet<string>(StringComparer.Ordinal) { "ENG-ARCH" }) != cases[index].Expected)
                throw new InvalidOperationException("Task ownership regression: " + cases[index].Name);
        }
    }

    [TestCase]
    [TestCategory("Regression")]
    public void ConditionalBranchesKeepTheirDeclarationContexts()
    {
        var file = Fixture("""
            class Sample {
            #if GODOT4 && !TOOLS
                [Obsolete] void Replaced() { }
            #if TOOLS
                void Impossible() { }
            #endif
            #elif GODOT4
                void Editor() { }
            #else
                void Current() { }
            #endif
            }
            """);
        var names = SemanticBranchSources.Expand(file).SelectMany(source => source.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Select(method => method.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        if (!names.Contains("Replaced") || !names.Contains("Editor") || !names.Contains("Current") || names.Contains("Impossible"))
            throw new InvalidOperationException("Conditional source branch was hidden or a contradictory branch was included.");

        var directive = file.Root.DescendantTrivia(descendIntoTrivia: true).Select(trivia => trivia.GetStructure())
            .OfType<IfDirectiveTriviaSyntax>().First();
        var unsupportedTree = file.Tree.WithRootAndOptions(file.Root.ReplaceNode(directive,
            directive.WithCondition(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)))), file.Tree.Options);
        var unsupported = new SourceFile(file.Path, unsupportedTree,
            file.Model.Compilation.ReplaceSyntaxTree(file.Tree, unsupportedTree).GetSemanticModel(unsupportedTree));
        var rejected = false;
        try { SemanticBranchSources.Expand(unsupported).ToArray(); }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("An unknown conditional source expression was silently skipped.");
    }

    [TestCase]
    [TestCategory("Regression")]
    public void DataMarkersSurviveRenamingAndDoNotBanVersionRejection()
    {
        var file = Fixture("""
            [DataId] readonly record struct Key(string Value);
            class Sample {
                const string CatalogEntry = "tree";
                [DataId] sealed record MultiKey { public MultiKey(params string[] values) { } }
                [DataId] sealed record CollectedKey { public CollectedKey(params IEnumerable<string> values) { } }
                [DataId] sealed record OptionalKey(string Value = "tree");
                [DataId] sealed record CatalogKey(string Value = CatalogEntry);
                [DataVersion] const int Current = 2;
                bool GoodCatalog(Key renamed) => renamed.Value == CatalogEntry;
                bool GoodIdentityParams(MultiKey renamed) => renamed == new MultiKey(CatalogEntry);
                bool BadIdentityParams(MultiKey renamed) => renamed == new MultiKey("tree");
                bool GoodEmptyIdentityParams(MultiKey renamed) => renamed == new MultiKey();
                bool GoodIdentityCollectedParams(CollectedKey renamed) => renamed == new CollectedKey(CatalogEntry);
                bool BadIdentityCollectedParams(CollectedKey renamed) => renamed == new CollectedKey("tree");
                bool GoodEmptyIdentityCollectedParams(CollectedKey renamed) => renamed == new CollectedKey();
                bool GoodIdentityDefaultCatalog(CatalogKey renamed) => renamed == new CatalogKey();
                bool BadIdentityDefaultLiteral(OptionalKey renamed) => renamed == new OptionalKey();
                bool GoodUnrelated(string id) => id == "tree";
                void GoodReject([DataVersion] int renamed) { if(renamed != Current) throw new Exception(); }
                bool BadIdentity(Key renamed) { var copy = renamed.Value; return copy == "tree"; }
                bool GoodIdentityPattern(Key renamed) => renamed.Value is CatalogEntry;
                bool BadIdentityPattern(Key renamed) => renamed.Value is "tree";
                bool GoodIdentityPatternWithLeftPattern(Key renamed, string mode) => (mode is "preview" ? renamed.Value : renamed.Value) is CatalogEntry;
                bool GoodIdentityNestedPatternWithLeftPattern(Key renamed, string mode) => (mode is "preview" ? renamed.Value : renamed.Value) is (CatalogEntry or null);
                bool BadIdentityNestedPattern(Key renamed) => renamed.Value is (CatalogEntry or ("other" and not null));
                bool BadIdentityPropertyPattern(Key renamed) => renamed is { Value: "tree" };
                int GoodIdentitySwitch(Key renamed) { switch(renamed.Value) { case CatalogEntry: return 1; default: return 0; } }
                int BadIdentitySwitch(Key renamed) { switch(renamed.Value) { case "tree": return 1; default: return 0; } }
                int GoodIdentityExpression(Key renamed) => renamed.Value switch { CatalogEntry => 1, _ => 0 };
                int BadIdentityExpression(Key renamed) => renamed.Value switch { "tree" => 1, _ => 0 };
                bool GoodEquals(Key renamed) => renamed.Value.Equals(CatalogEntry);
                bool BadEquals(Key renamed) => renamed.Value.Equals("tree");
                int GoodCompare(Key renamed) => string.Compare(renamed.Value, CatalogEntry, StringComparison.Ordinal);
                int BadCompare(Key renamed) => string.Compare(renamed.Value, "tree", StringComparison.Ordinal);
                int GoodCompareTo(Key renamed) => renamed.Value.CompareTo(CatalogEntry);
                int BadCompareTo(Key renamed) => renamed.Value.CompareTo("tree");
                bool GoodStartsWith(Key renamed) => renamed.Value.StartsWith(CatalogEntry, StringComparison.Ordinal);
                bool BadStartsWith(Key renamed) => renamed.Value.StartsWith("tree", StringComparison.Ordinal);
                bool GoodEndsWith(Key renamed) => renamed.Value.EndsWith(CatalogEntry, StringComparison.Ordinal);
                bool BadEndsWith(Key renamed) => renamed.Value.EndsWith("tree", StringComparison.Ordinal);
                bool GoodContains(Key renamed) => renamed.Value.Contains(CatalogEntry, StringComparison.Ordinal);
                bool BadContains(Key renamed) => renamed.Value.Contains("tree", StringComparison.Ordinal);
                int GoodIndexOf(Key renamed) => renamed.Value.IndexOf(CatalogEntry, StringComparison.Ordinal);
                int BadIndexOf(Key renamed) => renamed.Value.IndexOf("tree", StringComparison.Ordinal);
                int GoodLastIndexOf(Key renamed) => renamed.Value.LastIndexOf(CatalogEntry, StringComparison.Ordinal);
                int BadLastIndexOf(Key renamed) => renamed.Value.LastIndexOf("tree", StringComparison.Ordinal);
                void GoodVersionEqual([DataVersion] int renamed) { if(renamed == Current) { } else throw new Exception(); }
                bool BadVersionEqual([DataVersion] int renamed) => renamed == Current;
                bool BadVersionNotEqual([DataVersion] int renamed) => renamed != Current;
                void GoodVersionLess([DataVersion] int renamed) { if(renamed < Current) throw new Exception(); }
                bool BadVersionLess([DataVersion] int renamed) => renamed < Current;
                void GoodVersionLessOrEqual([DataVersion] int renamed) { if(renamed <= Current) throw new Exception(); }
                bool BadVersionLessOrEqual([DataVersion] int renamed) => renamed <= Current;
                void GoodVersionGreater([DataVersion] int renamed) { if(renamed > Current) throw new Exception(); }
                bool BadVersionGreater([DataVersion] int renamed) => renamed > Current;
                void GoodVersionGreaterOrEqual([DataVersion] int renamed) { if(renamed >= Current) throw new Exception(); }
                bool BadVersionGreaterOrEqual([DataVersion] int renamed) => renamed >= Current;
                void GoodVersionPattern([DataVersion] int renamed) { if(renamed is not Current) throw new Exception(); }
                void GoodVersionNamedPattern([DataVersion] int renamed) { if(renamed is Current) { } else throw new Exception(); }
                int BadVersionPattern([DataVersion] int renamed) => renamed is Current ? 10 : 20;
                int BadVersionNotPattern([DataVersion] int renamed) => renamed is not Current ? 10 : 20;
                int GoodVersionSwitch([DataVersion] int renamed) { switch(renamed) { case Current: return 20; default: throw new Exception(); } }
                int BadVersionSwitch([DataVersion] int renamed) { switch(renamed) { case 1: return 10; case Current: return 20; default: throw new Exception(); } }
                int GoodVersionExpression([DataVersion] int renamed) => renamed switch { Current => 20, _ => throw new Exception() };
                int BadVersionExpression([DataVersion] int renamed) => renamed switch { 1 => 10, Current => 20, _ => throw new Exception() };
                void GoodVersionCall([DataVersion] int renamed) { if(renamed.CompareTo(Current) != 0) throw new Exception(); }
                int BadVersionCall([DataVersion] int renamed) => renamed.CompareTo(Current);
                int GoodConditional([DataVersion] int renamed) => renamed == Current ? 20 : throw new Exception();
                int BadConditional([DataVersion] int renamed) => renamed == 1 ? 10 : 20;
                void GoodVersionFilter([DataVersion] int renamed) { try { throw new Exception(); } catch(Exception) when(renamed != Current) { throw; } }
                int BadVersionFilter([DataVersion] int renamed) { try { throw new Exception(); } catch(Exception) when(renamed == 1) { return 10; } catch(Exception) { return 20; } }
            }
            """);
        var errors = new DataBranchPolicy(file).Violations().ToArray();
        var mismatches = new List<string>();
        // Each case occupies one line so diagnostics can be matched to the case that owns the syntax.
        foreach (var method in file.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var actual = errors.Any(error => error.StartsWith(file.At(method, ""), StringComparison.Ordinal));
            if (actual != method.Identifier.ValueText.StartsWith("Bad", StringComparison.Ordinal))
                mismatches.Add(method.Identifier.ValueText + " (violation: " + actual + ")");
        }
        if (mismatches.Count != 0) throw new InvalidOperationException("Data declaration regression: " + string.Join(", ", mismatches));
    }

    internal static SourceFile Fixture(string body, params MetadataReference[] additionalReferences)
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using Campus.Contracts;
            namespace Campus.Contracts {
                [AttributeUsage(AttributeTargets.All)] sealed class DataIdAttribute : Attribute { }
                [AttributeUsage(AttributeTargets.All)] sealed class DataVersionAttribute : Attribute { }
                [AttributeUsage(AttributeTargets.Parameter)] sealed class FailureHandlerAttribute(string document) : Attribute { }
            }
            """ + body;
        var tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "game/semantic-case.cs");
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator)
            .Where(path => path.Length != 0).Select(path => MetadataReference.CreateFromFile(path)).Concat(additionalReferences);
        var compilation = CSharpCompilation.Create("G4.SemanticCase", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        return new SourceFile("game/semantic-case.cs", tree, compilation.GetSemanticModel(tree));
    }
}

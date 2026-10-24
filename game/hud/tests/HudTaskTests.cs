using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Hud;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class HudTaskTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task HudTaskReportsOriginalFailureAndEndsQuietlyOnScopeCancel()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var failure = new RecordingFailure();
        var operation = new HudTask(failure);
        var pending = new TaskCompletionSource();
        AssertThat(operation.Start(token => pending.Task.WaitAsync(token))).IsTrue();
        AssertThat(operation.Start(_ => Task.CompletedTask)).OverrideFailureMessage("A running UI operation accepted a second start.").IsFalse();
        // 作用域取消只结束界面一侧的等待，不算失败。
        operation.Cancel();
        await FrameAwaiter.ProcessAsync(root);
        await FrameAwaiter.ProcessAsync(root);
        AssertThat(!operation.Running && failure.Errors.Count == 0).OverrideFailureMessage("Scope cancellation was reported as a failure or kept the operation running.").IsTrue();
        // 以前被丢弃的故障任务吞掉失败：现在原异常交给失败接收方。
        var error = new InvalidOperationException();
        AssertThat(operation.Start(_ => Task.FromException(error))).IsTrue();
        await FrameAwaiter.ProcessAsync(root);
        AssertThat(failure.Errors.Count == 1 && ReferenceEquals(failure.Errors[0], error) && !operation.Running)
            .OverrideFailureMessage("A failed UI operation did not hand its original exception to the failure receiver.").IsTrue();
        // 服务按异步续体结束任务：操作体已结束、持有者的等待还没回来的那一刻再开，以前被当成正在跑拒掉，后交的意图无人等。
        var serviced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AssertThat(operation.Start(_ => serviced.Task)).IsTrue();
        var first = new InvalidOperationException();
        serviced.SetException(first);
        AssertThat(failure.Errors.Count == 1).OverrideFailureMessage("The fixture did not leave the first owner's wait pending.").IsTrue();
        AssertThat(operation.Start(_ => Task.CompletedTask)).OverrideFailureMessage("A start right after the previous body ended was refused.").IsTrue();
        // GAME-234：以前只记最后一份等待，离树取消看到这次已完就返回，前一次的失败落到界面释放之后；取消返回时就要交付完。
        operation.Cancel();
        AssertThat(failure.Errors.Count == 2 && ReferenceEquals(failure.Errors[1], first)).OverrideFailureMessage("Tree-exit cancellation returned before an earlier wait delivered its failure.").IsTrue();
        // 应用退出：停接后不再开；体不认令牌、取消后才失败的等待由排空等到它交付完（前一次若重复交付，计数也对不上）。
        var late = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AssertThat(operation.Start(_ => late.Task)).IsTrue();
        operation.Stop();
        var drained = operation.DrainAsync();
        AssertThat(drained.IsCompleted).OverrideFailureMessage("Drain completed before the accepted wait was delivered.").IsFalse();
        var lateError = new InvalidOperationException();
        late.SetException(lateError);
        await drained;
        AssertThat(failure.Errors.Count == 3 && ReferenceEquals(failure.Errors[2], lateError) && !operation.Start(_ => Task.CompletedTask))
            .OverrideFailureMessage("Drain lost the original failure, or a stopped UI operation accepted a new start.").IsTrue();
    }

    // GAME-234：记录开关的请求队列以前随设置卡离树取消等待，正在等的请求被丢下，晚到的失败没人交付；现在归 HUD，取消不丢请求，退出排空逐个等完。
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task HudRequestsKeepWaitingAfterCancelAndDrainEveryRequestOnExit()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var failure = new RecordingFailure();
        var requests = new HudRequests(failure);
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        requests.Submit(() => first.Task);
        requests.Submit(() => second.Task);
        AssertThat(requests.Waiting.Running).OverrideFailureMessage("The fixture did not leave the submitted requests pending.").IsTrue();
        // 设置卡离树时以前在这里取消等待；现在只有 HUD 离树会取消，也不提前结束等待。
        requests.Waiting.Cancel();
        var firstError = new InvalidOperationException();
        first.SetException(firstError);
        await FrameAwaiter.ProcessAsync(root);
        await FrameAwaiter.ProcessAsync(root);
        AssertThat(failure.Errors.Count == 1 && ReferenceEquals(failure.Errors[0], firstError))
            .OverrideFailureMessage("A request already handed to the service lost its failure after the wait was cancelled.").IsTrue();
        requests.Waiting.Stop();
        var drained = requests.Waiting.DrainAsync();
        AssertThat(drained.IsCompleted).OverrideFailureMessage("Drain completed before the remaining request settled.").IsFalse();
        var secondError = new InvalidOperationException();
        second.SetException(secondError);
        await drained;
        var sent = false;
        requests.Submit(() => { sent = true; return Task.CompletedTask; });
        AssertThat(failure.Errors.Count == 2 && ReferenceEquals(failure.Errors[1], secondError) && !sent)
            .OverrideFailureMessage("Drain lost a later request's failure, or a stopped holder sent a new request.").IsTrue();
    }

    internal sealed class RecordingFailure : IAppFailure
    {
        public List<Exception> Errors { get; } = [];
        public void Report(Exception error) => Errors.Add(error);
    }
}

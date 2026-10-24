using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Tests.MapServiceTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class MapPrepareCommandTests
{
    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public async Task JoinedPreparationClosesOnceAndMountedReuseHasItsOwnTerminalOperation()
    {
        var f = new Fixture(); f.Runtime.DelayMount();
        var events = new List<MapPrepareEvent>();
        IMapTransition? transition = null;
        var completedAfterCleanup = false;
        f.Service.PrepareCommand += value =>
        {
            events.Add(value);
            if (value.Phase == MapPreparePhase.Completed)
                completedAfterCleanup = transition!.Done.IsCompletedSuccessfully &&
                    transition.Done.Result == value.Result && !f.Camera.InputLocked && f.Service.Transition is null;
        };
        var preparing = f.Service.Prepare(f.A);
        transition = f.Service.Transition!;
        var joined = f.Service.Prepare(f.A);
        var rejectedPending = f.Service.Prepare(f.B);
        AssertThat(rejectedPending.IsFaulted && rejectedPending.Exception!.InnerException is InvalidOperationException).IsTrue();
        f.Until(() => f.Runtime.MountPending);
        AssertThat(!preparing.IsCompleted && !joined.IsCompleted && f.Camera.InputLocked &&
            events.All(e => e.Phase != MapPreparePhase.Completed)).IsTrue();
        f.Runtime.ReleaseMount();
        f.Until(() => transition.Done.IsCompleted);
        // Multiple waiters may resume through the engine context; yield after the owner finishes.
        await Task.WhenAll(preparing, joined).WaitAsync(TimeSpan.FromSeconds(5));
        var context = f.Service.Context;
        f.Service.Prepare(f.A).GetAwaiter().GetResult();
        var rejectedMounted = f.Service.Prepare(f.B);
        AssertThat(rejectedMounted.IsFaulted && rejectedMounted.Exception!.InnerException is InvalidOperationException).IsTrue();
        AssertThat(events.Select(e => e.Phase).SequenceEqual(new[]
        {
            MapPreparePhase.Started, MapPreparePhase.Joined, MapPreparePhase.Rejected,
            MapPreparePhase.Completed, MapPreparePhase.Reused, MapPreparePhase.Rejected
        })).IsTrue();
        var started = events[0]; var completed = events[3]; var reused = events[4];
        AssertThat(started.Operation > 0 && started.From == default && started.To == f.A && started.Result is null &&
            events[1] == (started with { Phase = MapPreparePhase.Joined }) &&
            completed == (started with { Phase = MapPreparePhase.Completed, Result = MapTravelResult.Arrived })).IsTrue();
        AssertThat(events[2].Operation > started.Operation && reused.Operation > events[2].Operation &&
            events[5].Operation > reused.Operation && events[2].To == f.B && events[2].Result is null &&
            events[5].From == f.A && events[5].To == f.B && events[5].Result is null &&
            reused.From == f.A && reused.To == f.A && reused.Result == MapTravelResult.Arrived).IsTrue();
        AssertThat(completedAfterCleanup && ReferenceEquals(context, f.Service.Context) &&
            f.Service.Rev == 1 && f.Loader.Loads == 1 && f.Runtime.Arrivals == 0).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public void AdmissionRejectionNeverStartsPreparationAndKeepsOriginalExceptionTypes()
    {
        var f = new Fixture(initialize: false);
        var events = new List<MapPrepareEvent>();
        f.Service.PrepareCommand += events.Add;
        var uninitialized = f.Service.Prepare(f.A);
        AssertThat(uninitialized.IsFaulted && uninitialized.Exception!.InnerException is InvalidOperationException).IsTrue();
        var initialized = f.Service.Initialize(); f.Until(() => initialized.IsCompleted);
        initialized.GetAwaiter().GetResult();
        var unknown = f.Service.Prepare(new("missing-map"));
        AssertThat(unknown.IsFaulted && unknown.Exception!.InnerException is InvalidOperationException).IsTrue();
        f.Service.StopNewWork();
        var stopped = f.Service.Prepare(f.A);
        AssertThat(stopped.IsFaulted && stopped.Exception!.InnerException is ObjectDisposedException).IsTrue();
        AssertThat(events.Count == 3 && events.All(e => e.Phase == MapPreparePhase.Rejected && e.Result is null) &&
            events.Select(e => e.Operation).Distinct().Count() == events.Count &&
            events.Zip(events.Skip(1)).All(pair => pair.First.Operation < pair.Second.Operation) &&
            f.Service.Transition is null && f.Loader.Loads == 0 && !f.Camera.InputLocked).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task DrainPublishesFailureOnlyAfterPendingReadAndCleanupHaveFinished()
    {
        var f = new Fixture(); f.Data.DelayRead();
        var events = new List<MapPrepareEvent>();
        IMapTransition? transition = null;
        var completedAfterCleanup = false;
        f.Service.PrepareCommand += value =>
        {
            events.Add(value);
            if (value.Phase == MapPreparePhase.Completed)
                completedAfterCleanup = transition!.Done.IsCompletedSuccessfully &&
                    transition.Done.Result == value.Result && !f.Camera.InputLocked && f.Service.Transition is null;
        };
        var preparing = f.Service.Prepare(f.A); transition = f.Service.Transition!;
        f.Service.Step(); f.Service.StopNewWork(); f.Runtime.StopNewWork();
        var drain = f.Service.DrainAsync();
        AssertThat(!drain.IsCompleted && !preparing.IsCompleted && f.Camera.InputLocked &&
            events.Count == 1 && events[0].Phase == MapPreparePhase.Started).IsTrue();
        var failure = new IOException("Input read failed during exit.");
        f.Data.ReleaseRead(failure);
        try { await drain; throw new InvalidOperationException("Drain hid the original input error."); }
        catch (IOException error) when (ReferenceEquals(error, failure)) { }
        try { await preparing; throw new InvalidOperationException("Preparation hid the original input error."); }
        catch (IOException error) when (ReferenceEquals(error, failure)) { }
        AssertThat(events.Count == 2 && events[1] == (events[0] with
            { Phase = MapPreparePhase.Completed, Result = MapTravelResult.Failed })).IsTrue();
        AssertThat(completedAfterCleanup && ReferenceEquals(transition.Failure, failure) &&
            drain.IsFaulted && preparing.IsFaulted && f.Runtime.Mounted.Count == 0 && f.Service.Rev == 0).IsTrue();
        AssertThat(ReferenceEquals(f.Service.DrainAsync(), drain) && events.Count == 2).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task CanceledMountCompletesWithFailedAfterDrainWithoutInventingAnArrival()
    {
        var f = new Fixture(); f.Runtime.DelayMount();
        var events = new List<MapPrepareEvent>();
        f.Service.PrepareCommand += events.Add;
        var preparing = f.Service.Prepare(f.A);
        var transition = f.Service.Transition!;
        f.Until(() => f.Runtime.MountPending);
        AssertThat(!preparing.IsCompleted && f.Camera.InputLocked && events.Count == 1).IsTrue();
        f.Service.StopNewWork(); f.Runtime.StopNewWork();
        await f.Service.DrainAsync();
        try { await preparing; throw new InvalidOperationException("Stopped mount completed preparation."); }
        catch (OperationCanceledException) when (preparing.IsCanceled) { }
        AssertThat(events.Count == 2 && events[1] == (events[0] with
            { Phase = MapPreparePhase.Completed, Result = MapTravelResult.Failed })).IsTrue();
        AssertThat(transition.Done.Result == MapTravelResult.Failed && transition.Failure is null &&
            !f.Camera.InputLocked && f.Service.Current == default && f.Service.Rev == 0 && f.Runtime.Arrivals == 0).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void InputReleaseFailureIsRetainedBeforeCompletedIsPublished()
    {
        var failure = new IOException("Fixture input release failure.");
        var camera = new InputReleaseFailingCamera(failure);
        var f = new Fixture(camera: camera);
        var events = new List<MapPrepareEvent>();
        IMapTransition? transition = null;
        var retainedBeforePublication = false;
        f.Service.PrepareCommand += value =>
        {
            events.Add(value);
            if (value.Phase == MapPreparePhase.Completed)
                retainedBeforePublication = transition!.Done.IsCompletedSuccessfully &&
                    transition.Done.Result == MapTravelResult.Failed && ReferenceEquals(transition.Failure, failure) &&
                    !camera.InputLocked && f.Service.Transition is null;
        };
        var preparing = f.Service.Prepare(f.A); transition = f.Service.Transition!;
        f.Until(() => preparing.IsCompleted);
        AssertThat(preparing.IsFaulted && ReferenceEquals(preparing.Exception!.InnerException, failure)).IsTrue();
        AssertThat(events.Count == 2 && events[1] == (events[0] with
            { Phase = MapPreparePhase.Completed, Result = MapTravelResult.Failed }) && retainedBeforePublication).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TestMapServicePublishesOnlyExplicitCommandValues()
    {
        var service = new TestMapService();
        var events = new List<MapPrepareEvent>();
        service.PrepareCommand += events.Add;
        service.Prepare(default).GetAwaiter().GetResult();
        AssertThat(events.Count == 0).IsTrue();
        var value = new MapPrepareEvent(1, MapPreparePhase.Reused, default, default, MapTravelResult.Arrived);
        service.PublishPrepareCommand(value);
        AssertThat(events.SequenceEqual(new[] { value })).IsTrue();
        service.PrepareCommand -= events.Add;
        service.PublishPrepareCommand(value);
        AssertThat(events.Count == 1).IsTrue();
    }

    private sealed class InputReleaseFailingCamera(Exception failure) : TestCameraRig, ICameraRig
    {
        IDisposable ICameraRig.LockInput(InputLockReason reason) => new FailingLease(base.LockInput(reason), failure);
        private sealed class FailingLease(IDisposable input, Exception failure) : IDisposable
        {
            public void Dispose() { input.Dispose(); throw failure; }
        }
    }
}

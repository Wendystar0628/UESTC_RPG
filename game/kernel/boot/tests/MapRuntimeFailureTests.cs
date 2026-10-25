using Campus.Kernel.Diagnostics;
using Campus.Kernel.Post;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;
using N = System.Numerics;
using static GdUnit4.Assertions;
using static Campus.Tests.MapRuntimeTestSupport;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class MapRuntimeFailureTests
{
    [TestCase(false), TestCase(true), TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ArrivalObserversCannotChangePreparationAndReachMapShutdown(bool preparationFails)
    {
        var points = new TaskCompletionSource();
        using var fixture = new MapRuntimeFixture(terrain: new ArrivalPreparation(points.Task));
        var runtime = fixture.Runtime;
        fixture.Stream.ResidentReady = fixture.Stream.DestinationReady = fixture.Producer.ContentReady = true;
        runtime._Process(0);
        var observed = new IOException("arrival observation failed");
        var progress = new InvalidOperationException("progress observation failed");
        var original = new InvalidDataException("terrain preparation failed");
        var arrivals = 0; var changes = 0;
        runtime.ArrivalChanged += _ => throw observed;
        runtime.ArrivalChanged += current =>
        {
            arrivals++;
            if (current is null) return;
            current.ProgressChanged += () => throw progress;
            current.ProgressChanged += () => changes++;
        };
        var arrival = runtime.Arrive(fixture.Camera.Pose, ViewMode.Fly);
        AssertThat(arrivals == 1 && !points.Task.IsCompleted && !arrival.Done.IsCompleted && runtime.Failure is null).IsTrue();
        if (preparationFails) points.SetException(original); else points.SetResult();
        await FrameAwaiter.ProcessAsync(runtime);
        runtime._Process(0);
        if (preparationFails)
        {
            try { await arrival.Done; throw new InvalidOperationException("Preparation failure became success."); }
            catch (AppStartupException error) { AssertThat(ReferenceEquals(error.InnerException, original)).IsTrue(); }
        }
        else AssertThat(arrival.Done.IsCompletedSuccessfully && arrival.Completed == arrival.Total && changes > 1 && runtime.Failure is null).IsTrue();
        Exception? shutdown = null;
        try { await runtime.ShutdownAsync(); } catch (Exception error) { shutdown = error; }
        var aggregate = shutdown as AggregateException ?? throw new InvalidOperationException("Shutdown lost observation failures.");
        var causes = aggregate.Flatten().InnerExceptions;
        AssertThat(arrivals == 2 && causes.Contains(observed) &&
            (preparationFails ? runtime.Failure is AppStartupException failure && ReferenceEquals(failure.InnerException, original)
                : causes.Contains(progress) && runtime.Failure is null)).IsTrue();
    }

    // 本组建的着色器与材质要等包装终结才归还原生引用；整组跑完在这里收齐，单跑本组时才不漏到引擎退出（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task SynchronousMountInputFailureReturnsTheOwnedTaskAndSurvivesShutdown()
    {
        var descriptor = TestMapContext.BackdropDescriptor();
        var context = new TestMapContext(descriptor,
            Inputs: TestMapContext.BackdropSnapshot(descriptor.Env.Backdrop, "{"));
        using var fixture = new MapRuntimeFixture(context: context);
        AssertThat(fixture.Mount.IsFaulted).IsTrue();
        AppStartupException? published = null;
        try { await fixture.Mount; throw new InvalidOperationException("Mount lost its input failure."); }
        catch (AppStartupException error)
        {
            AssertThat(ReferenceEquals(error, fixture.Runtime.Failure) && error.InnerException is InvalidDataException).IsTrue();
            published = error;
        }
        try { await fixture.Runtime.ShutdownAsync(); throw new InvalidOperationException("Shutdown lost the mount failure."); }
        catch (AppStartupException error)
        {
            AssertThat(ReferenceEquals(error, published) && ReferenceEquals(error.InnerException, published!.InnerException)).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void SharedRetentionPreservesNestedAndEmptyCausesAndConcurrentEffectFailures()
    {
        var original = new InvalidDataException("producer failed");
        var empty = new AggregateException();
        AssertThat(ReferenceEquals(RetainedFailure.Append(null, original), original) &&
            ReferenceEquals(RetainedFailure.Append(null, empty), empty)).IsTrue();
        var nested = new AggregateException(original, new AggregateException(empty, original));
        var retained = RetainedFailure.Append(null, nested);
        var causes = retained as AggregateException ?? throw new InvalidOperationException("Producer causes were not retained.");
        AssertThat(causes.InnerExceptions.Count == 2 && ReferenceEquals(causes.InnerExceptions[0], original) &&
            ReferenceEquals(causes.InnerExceptions[1], empty) &&
            ReferenceEquals(RetainedFailure.Append(retained, nested), retained) &&
            ReferenceEquals(RetainedFailure.Append(retained, original), retained)).IsTrue();
        Exception? effects = null;
        var errors = Enumerable.Range(0, 16).Select(index => new InvalidOperationException($"effect {index}")).ToArray();
        Parallel.For(0, errors.Length * 2, index => EffectFailure.Record(ref effects, errors[index % errors.Length]));
        var effectCauses = (effects as AggregateException ?? throw new InvalidOperationException("Concurrent effect failures were lost."))
            .Flatten().InnerExceptions;
        AssertThat(effectCauses.Count == errors.Length && errors.All(error => effectCauses.Any(cause => ReferenceEquals(cause, error)))).IsTrue();
        var observed = effects;
        foreach (var error in errors) EffectFailure.Record(ref effects, error);
        AssertThat(ReferenceEquals(effects, observed)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void VisibleProducerFailureReachesTheGlobalProjectionWithoutAPendingArrival()
    {
        foreach (var completeArrival in new[] { false, true })
        {
            using var fixture = new MapRuntimeFixture();
            fixture.Stream.ResidentReady = fixture.Stream.DestinationReady = true;
            fixture.Producer.ContentReady = true;
            fixture.Renderer.Pending = true;
            fixture.Runtime._Process(0);
            AssertThat(!fixture.Mount.IsCompleted && fixture.Runtime.Failure is null)
                .OverrideFailureMessage("Pending render uploads did not hold the otherwise ready map mount.").IsTrue();
            fixture.Renderer.Pending = false;
            fixture.Runtime._Process(0);
            AssertThat(fixture.Mount.IsCompletedSuccessfully).IsTrue();
            IMapArrival? arrival = null;
            if (completeArrival)
            {
                fixture.Renderer.Pending = true;
                arrival = fixture.Runtime.Arrive(fixture.Camera.Pose, ViewMode.Fly);
                fixture.Runtime._Process(0);
                AssertThat(!arrival.Done.IsCompleted && fixture.Runtime.Failure is null)
                    .OverrideFailureMessage("Pending render uploads did not hold the otherwise ready arrival.").IsTrue();
                fixture.Renderer.Pending = false;
                fixture.Runtime._Process(0);
                AssertThat(arrival.Done.IsCompletedSuccessfully).IsTrue();
            }
            var original = new InvalidDataException("visible producer failed");
            fixture.Producer.Failure = original;
            IMapRuntime globalSource = fixture.Runtime;
            AssertThat(globalSource.Failure is null && fixture.Renderer.Failure is null).IsTrue();
            fixture.Runtime._Process(0);
            var published = globalSource.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure is not terminal.");
            AssertThat(ReferenceEquals(published.InnerException, original)).IsTrue();
            AssertThat(arrival is null || arrival.Done.IsCompletedSuccessfully).IsTrue();
            // The same cause reaching another route still occupies one slot in the global projection.
            fixture.Renderer.Failure = original;
            fixture.Runtime._Process(0);
            AssertThat(ReferenceEquals(globalSource.Failure, published)).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void ReportedWorldFailureUsesTheExistingMapLifetimeProjection()
    {
        using var fixture = new MapRuntimeFixture();
        IMapRuntime source = fixture.Runtime;
        var original = new InvalidOperationException("world crossfade failed");
        var published = source.ReportFailure(original);
        AssertThat(ReferenceEquals(source.Failure, published) && ReferenceEquals(published.InnerException, original) &&
            source.Arrival is null).IsTrue();
        AssertThat(ReferenceEquals(source.ReportFailure(original), published)).IsTrue();
        var later = new AppStartupException("map resource is missing");
        var collected = source.ReportFailure(later);
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Reported map failures were not aggregated.");
        AssertThat(ReferenceEquals(source.Failure, collected) && causes.InnerExceptions.Count == 2 &&
            causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(later)).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Mount.Exception!.InnerException, collected)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void TerminalStartupFailureRetainsItsIdentityAcrossProducerAggregation()
    {
        using var fixture = new MapRuntimeFixture();
        var original = new AppStartupException("map resource is missing");
        fixture.Stream.Failure = original;
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Runtime.Failure, original) &&
            ReferenceEquals(fixture.Mount.Exception!.InnerException, original)).IsTrue();
        var later = new InvalidDataException("visible producer cleanup failed");
        fixture.Producer.Failure = later;
        fixture.Runtime._Process(0);
        var published = fixture.Runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure is not terminal.");
        var causes = published.InnerException as AggregateException ?? throw new InvalidOperationException("Map failure lost its diagnostic causes.");
        AssertThat(causes.InnerExceptions.Count == 2 &&
            causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(later)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void EmptyAggregateRemainsAPublishedFailureAcrossRepeatedPollingAndAggregation()
    {
        using var fixture = new MapRuntimeFixture();
        var original = new AggregateException();
        fixture.Stream.Failure = original;
        fixture.Runtime._Process(0);
        var published = fixture.Runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Empty aggregate was not published.");
        AssertThat(ReferenceEquals(published.InnerException, original) &&
            ReferenceEquals(fixture.Mount.Exception!.InnerException, published)).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Runtime.Failure, published)).IsTrue();
        var later = new AppStartupException("map resource is missing");
        var nested = new AggregateException();
        fixture.Producer.Failure = new AggregateException(original, later, nested);
        fixture.Runtime._Process(0);
        var collected = fixture.Runtime.Failure as AppStartupException ?? throw new InvalidOperationException("Aggregate failure was not published.");
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Aggregate failure lost its diagnostic causes.");
        AssertThat(causes.InnerExceptions.Count == 3 &&
            causes.InnerExceptions.Contains(original) && causes.InnerExceptions.Contains(later) && causes.InnerExceptions.Contains(nested)).IsTrue();
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(fixture.Runtime.Failure, collected)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task RetainedParticipantFailuresReachTheSameGlobalProjectionAndKeepTheirIdentity()
    {
        using var stream = new StreamFixture();
        var participant = stream.CollisionParticipant;
        participant.Need = StreamTileNeed.Required;
        stream.Director.Prefetch(stream.Camera.Pose, ViewMode.Fly);
        await stream.Until(() => participant.Mounted.Count > 0);
        participant.Need = StreamTileNeed.Retain;
        await stream.Until(() => participant.Retained);
        using var fixture = new MapRuntimeFixture(stream.Director, stream.Context);
        fixture.Runtime._Process(0);
        AssertThat(fixture.Mount.IsCompletedSuccessfully && participant.Mounted.Count > 0).IsTrue();
        var original = new InvalidDataException("retained participant failed");
        participant.Failure = original;
        IMapRuntime globalSource = fixture.Runtime;
        AssertThat(stream.Director.Failure is null && globalSource.Failure is null).IsTrue();
        stream.Director._Process(0);
        AssertThat(stream.Director.Failure is InvalidDataException && ReferenceEquals(stream.Director.Failure, original) && globalSource.Failure is null).IsTrue();
        fixture.Runtime._Process(0);
        var published = globalSource.Failure as AppStartupException ?? throw new InvalidOperationException("Map failure is not terminal.");
        AssertThat(ReferenceEquals(published.InnerException, original)).IsTrue();
        AssertThat(!stream.Director.ResidentReady && !stream.Director.DestinationReady && !stream.Director.ViewReady).IsTrue();
        var later = new InvalidOperationException("another participant failure");
        participant.Failure = new AggregateException(original, later);
        stream.Director._Process(0);
        fixture.Runtime._Process(0);
        var collected = globalSource.Failure as AppStartupException ?? throw new InvalidOperationException("Participant faults are not terminal.");
        var causes = collected.InnerException as AggregateException ?? throw new InvalidOperationException("Participant faults were not projected.");
        var failures = causes.Flatten().InnerExceptions;
        AssertThat(failures.Count == 2 && failures.Contains(original) && failures.Contains(later)).IsTrue();
        stream.Director._Process(0);
        fixture.Runtime._Process(0);
        AssertThat(ReferenceEquals(globalSource.Failure, collected)).IsTrue();
        try { await stream.Unload(); throw new InvalidOperationException("Drain lost retained participant failures."); }
        catch (AggregateException error)
        {
            AssertThat(error.Flatten().InnerExceptions.Contains(original) && error.Flatten().InnerExceptions.Contains(later)).IsTrue();
        }
        AssertThat(stream.Director.IsUnloaded && participant.Mounted.Count == 0).IsTrue();
    }
}

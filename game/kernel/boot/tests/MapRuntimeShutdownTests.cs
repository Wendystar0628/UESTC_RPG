using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using Campus.Kernel.Registry;
using static Campus.Tests.MapRuntimeTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class MapRuntimeShutdownTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task PreparationFailureStillStopsAllProducersDrainsStreamAndAwaitsEveryDataOwner()
    {
        var runtime = new MapRuntime();
        var stage = new Node();
        var viewCamera = new Camera3D();
        var order = new List<string>();
        var preparation = new TaskCompletionSource();
        var stream = new DrainStream(order);
        var original = new InvalidDataException("preparation failure");
        var stopping = new InvalidOperationException("producer stop failure");
        var draining = new InvalidOperationException("stream cleanup failure");
        var releasing = new InvalidOperationException("data cleanup failure");
        var first = new FirstLifetimeParticipant(order, preparation.Task, stopping);
        var second = new SecondLifetimeParticipant(order);
        var context = new TestMapContext(BackdropInputsTests.Descriptor() with { Id = new("lifetime-fixture") });
        var registry = new ServiceRegistry();
        registry.Discover([typeof(FirstLifetimeParticipant), typeof(SecondLifetimeParticipant)], _ => "fixture");
        registry.Add(first); registry.Add(second);
        var renderer = new RuntimeRenderer();
        runtime.Bind(new(registry, stream, new RuntimeBake(), new ArrivalCollision(), renderer,
            new TestCameraRig(), new FlowTerrainPreparation(), viewCamera));
        stage.AddChild(runtime);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(stage);
        var mounting = runtime.Mount(context);
        try
        {
            var shutdown = runtime.ShutdownAsync();
            AssertThat(order.Count == 0 && !shutdown.IsCompleted).IsTrue();
            preparation.SetException(original);
            await stream.Entered.Task;
            AssertThat(order.SequenceEqual(["stop-first", "stop-second", "stream"]) && !shutdown.IsCompleted).IsTrue();
            stream.Completion.SetException(draining);
            await first.Entered.Task;
            AssertThat(order.Last() == "data-first" && !order.Contains("data-second") && !shutdown.IsCompleted).IsTrue();
            first.Completion.SetException(releasing);
            await second.Entered.Task;
            AssertThat(!shutdown.IsCompleted).IsTrue();
            second.Completion.SetResult();
            try { await shutdown; throw new InvalidOperationException("Shutdown lost its preparation and cleanup failures."); }
            catch (AppStartupException error)
            {
                var causes = error.InnerException as AggregateException ?? throw new InvalidOperationException("Shutdown lost its diagnostic causes.");
                var failures = causes.Flatten().InnerExceptions;
                AssertThat(failures.Count == 4 && failures.Contains(original) &&
                    failures.Contains(stopping) && failures.Contains(draining) && failures.Contains(releasing)).IsTrue();
                AssertThat(ReferenceEquals(runtime.Failure, error) && original.StackTrace is not null).IsTrue();
            }
            AssertThat(order.SequenceEqual(["stop-first", "stop-second", "stream", "data-first", "data-second"])).IsTrue();
            AssertThat(stream.IsUnloaded && !mounting.IsCompletedSuccessfully && ReferenceEquals(runtime.ShutdownAsync(), shutdown)).IsTrue();
            try { await mounting; } catch (OperationCanceledException) { }
        }
        finally { stage.Free(); viewCamera.Free(); }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task ShutdownBeforeMountDrainsTheStreamWithoutReleasingAnyMapScope()
    {
        var runtime = new MapRuntime(); var stage = new Node(); var camera = new Camera3D();
        var registry = new ServiceRegistry(); var order = new List<string>(); var stream = new DrainStream(order);
        // A disposed registry rejects ReleaseMapAsync for every ID, including default(MapId).
        await registry.ShutdownAsync();
        runtime.Bind(new(registry, stream, new RuntimeBake(), new ArrivalCollision(), new RuntimeRenderer(),
            new TestCameraRig(), new FlowTerrainPreparation(), camera));
        stage.AddChild(runtime);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(stage);
        try
        {
            var shutdown = runtime.ShutdownAsync();
            AssertThat(order.SequenceEqual(["stream"]) && !shutdown.IsCompleted).IsTrue();
            stream.Completion.SetResult(); await shutdown;
            AssertThat(runtime.Failure is null && stream.IsUnloaded && ReferenceEquals(runtime.ShutdownAsync(), shutdown)).IsTrue();
        }
        finally { stage.Free(); camera.Free(); }
    }

    private class LifetimeParticipant(string name, List<string> order, Task preparation, Exception? stopping = null) : IMapAware, IMapDataParticipant
    {
        public TaskCompletionSource Entered { get; } = new();
        public TaskCompletionSource Completion { get; } = new();
        public Task Prepare(IMapContext context) => preparation;
        public void OnMapLoaded(IMapContext context) { }
        public void OnMapUnloading(MapId from)
        {
            order.Add("stop-" + name);
            if (stopping is not null) throw stopping;
        }
        public Task UnloadAsync(MapId from)
        {
            order.Add("data-" + name); Entered.SetResult(); return Completion.Task;
        }
    }

    [KernelService(typeof(FirstLifetimeParticipant), Map = "lifetime-fixture")]
    private sealed class FirstLifetimeParticipant(List<string> order, Task preparation, Exception stopping)
        : LifetimeParticipant("first", order, preparation, stopping) { }
    [KernelService(typeof(SecondLifetimeParticipant), Map = "lifetime-fixture")]
    private sealed class SecondLifetimeParticipant(List<string> order)
        : LifetimeParticipant("second", order, Task.CompletedTask) { }

    private sealed class DrainStream(List<string> order) : IMapStreamState
    {
        public TaskCompletionSource Entered { get; } = new();
        public TaskCompletionSource Completion { get; } = new();
        public Exception? Failure => null;
        public bool IsUnloaded => Completion.Task.IsCompleted;
        public bool ResidentReady => false;
        public bool DestinationReady => false;
        public MapStreamDestination? Destination => null;
        public bool HasPendingDestination => false;
        public IReadOnlyList<MapStreamDestination> PinnedDestinations => [];
        public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) { }
        public bool PinnedDestinationReady(MapStreamDestination destination) => false;
        public void ClearDestination() { }
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null) => throw new NotSupportedException();
        public Task DrainAsync() { order.Add("stream"); Entered.SetResult(); return Completion.Task; }
    }

}

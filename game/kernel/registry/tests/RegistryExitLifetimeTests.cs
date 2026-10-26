using Campus.Contracts;
using Campus.Kernel.Registry;
using GdUnit4;
using Godot;
using Campus.Contracts.Maps;

namespace Campus.Tests;

[TestSuite]
public sealed class RegistryExitLifetimeTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ExitStopsEveryOwnerAndDrainsAllWorkBeforeReleasingEvenWhenOneFails()
    {
        var registry = new ServiceRegistry();
        registry.Discover([typeof(FirstOwner), typeof(SecondOwner)], _ => "fixture");
        var first = registry.Get<FirstOwner>();
        var second = registry.Get<SecondOwner>();
        var original = new InvalidDataException("owned exit work failed");
        try
        {
            var shutdown = registry.ShutdownAsync();
            await Task.WhenAny(first.Draining.Task, second.Draining.Task, shutdown);
            if (!first.Stopped || !second.Stopped || first.Disposed || second.Disposed || shutdown.IsCompleted)
                throw new InvalidOperationException("Registry did not stop all owners before waiting for their retained work.");
            var active = first.Draining.Task.IsCompleted ? (ExitOwner)first : second;
            var other = ReferenceEquals(active, first) ? (ExitOwner)second : first;
            active.Failure = original;
            active.Release.TrySetResult();
            await Task.WhenAny(other.Draining.Task, shutdown);
            if (!other.Draining.Task.IsCompleted || first.Disposed || second.Disposed || shutdown.IsCompleted)
                throw new InvalidOperationException("One failed drain prevented another owner's work from draining before release.");
            other.Release.TrySetResult();
            try
            {
                await shutdown;
                throw new InvalidOperationException("Registry lost its owner's exit failure.");
            }
            catch (AggregateException error) when (error.Flatten().InnerExceptions.Any(cause => ReferenceEquals(cause, original))) { }
            if (!first.Disposed || !second.Disposed)
                throw new InvalidOperationException("Registry did not release its owners after every drain reached its terminal state.");
        }
        finally
        {
            first.Release.TrySetResult();
            second.Release.TrySetResult();
            try { await registry.ShutdownAsync(); }
            catch (AggregateException error) when (error.Flatten().InnerExceptions.Any(cause => ReferenceEquals(cause, original))) { }
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task FailedMapDrainKeepsNodesAndBindingsUntilAnExplicitShutdownResumes()
    {
        var original = new IOException("native retirement failed");
        var probe = new ShutdownProbe(); probe.RuntimeDrain.SetException(original);
        var registry = new ServiceRegistry(); registry.Add(probe);
        registry.Discover([typeof(ShutdownMapLifetime), typeof(MapLifetimeService), typeof(MapRuntimeTestSupport.ArrivalStream)], _ => "fixture");
        var lifetime = registry.Get<IMapLifetime>(); var map = new MapId("fixture-map");
        var stream = (MapRuntimeTestSupport.ArrivalStream)registry.Create(typeof(MapRuntimeTestSupport.ArrivalStream), map);
        stream.OnMapLoaded(new TestMapContext(TestMapContext.BackdropDescriptor() with { Id = map }));
        var owner = (MapLifetimeService)registry.Create(typeof(MapLifetimeService), map);
        var scene = new Node(); var child = new Node(); scene.AddChild(child);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(scene);
        try
        {
            var first = registry.ShutdownAsync(scene);
            await RequireOriginal(first);
            if (owner.Disposed || child.GetParent() != scene || !ReferenceEquals(lifetime, registry.Get<IMapLifetime>()))
                throw new InvalidOperationException("A failed native drain detached nodes or removed live bindings.");
            stream.OnMapUnloading(map);
            var resumed = registry.ShutdownAsync(scene);
            if (ReferenceEquals(first, resumed)) throw new InvalidOperationException("An explicit retry reused the failed incomplete shutdown task.");
            await RequireOriginal(resumed);
            if (!owner.Disposed || GodotObject.IsInstanceValid(child) || !ReferenceEquals(resumed, registry.ShutdownAsync(scene)))
                throw new InvalidOperationException("A drained map did not finish registry teardown with its original failure retained.");
        }
        finally { scene.Free(); }

        async Task RequireOriginal(Task shutdown)
        {
            try { await shutdown; throw new InvalidOperationException("Shutdown swallowed its original native failure."); }
            catch (AggregateException error) when (error.Flatten().InnerExceptions.Contains(original)) { }
        }
    }

    public abstract class ExitOwner : IRegistryExitParticipant, IDisposable
    {
        public TaskCompletionSource Draining { get; } = new();
        public TaskCompletionSource Release { get; } = new();
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }
        public Exception? Failure { get; set; }

        public void PrepareForExit() => Stopped = true;
        public async Task DrainForExitAsync()
        {
            Draining.TrySetResult();
            await Release.Task;
            if (Failure is { } error) throw error;
        }
        public void Dispose() => Disposed = true;
    }

    [KernelService(typeof(FirstOwner))]
    public sealed class FirstOwner : ExitOwner { }

    [KernelService(typeof(SecondOwner))]
    public sealed class SecondOwner : ExitOwner { }
}

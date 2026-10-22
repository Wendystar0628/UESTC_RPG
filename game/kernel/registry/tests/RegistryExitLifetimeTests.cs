using Campus.Contracts;
using Campus.Kernel.Registry;
using GdUnit4;

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

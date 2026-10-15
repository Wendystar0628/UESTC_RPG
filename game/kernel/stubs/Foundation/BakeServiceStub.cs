using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Stubs.Foundation;

/// <summary>D0 uses live generation. Missing bake providers are an explicit failure, never an empty success.</summary>
[KernelService(typeof(IBakeService), Owner = "WP-00", IsStub = true)]
public sealed class BakeServiceStub : IBakeService
{
    private readonly Dictionary<BakeKey, IBakeHandle> _handles = [];
    private readonly object _sync = new();
    public BakeProgress Progress
    {
        get { lock (_sync) return new(_handles.Count, _handles.Count, "D0 live generation; baking unavailable"); }
    }
    public long CacheBytes => 0;
    public IBakeHandle Ensure(BakeKey key, BakePriority priority)
    {
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        lock (_sync)
        {
            if (_handles.TryGetValue(key, out var found)) return found;
            var failed = new FailedBakeHandle($"{key.Map.Value}/{key.Module}/{key.Detail}: D0 live generation; no bake provider registered.");
            _handles.Add(key, failed);
            return failed;
        }
    }
    // Clear is a cache operation; Failed stays terminal until the caller explicitly retries.
    public void Clear() { }
    public void ClearFailures() { lock (_sync) _handles.Clear(); }

    private sealed class FailedBakeHandle(string error) : IBakeHandle
    {
        public BakeState State => BakeState.Failed;
        public object? Product => null;
        public string? Error { get; } = error;
    }
}

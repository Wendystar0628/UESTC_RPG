using Campus.Contracts;

namespace Campus.Kernel.World;

public sealed partial class WorldStateService
{
    private readonly List<FlowLease> _flowLeases = [];
    private FlowLease? _activeFlow;

    public IFlowLease AcquireFlow(FlowPriority priority, int rate)
    {
        EnsureUsable();
        _ = priority switch
        {
            FlowPriority.Pause => 0,
            FlowPriority.AppFlow => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };
        ValidateRate(rate);
        var lease = new FlowLease(this, priority, rate);
        _flowLeases.Add(lease);
        SelectFlow();
        Rev++;
        return lease;
    }

    private void SelectFlow()
    {
        _activeFlow = null;
        foreach (var lease in _flowLeases)
            if (_activeFlow is null || (byte)lease.Priority >= (byte)_activeFlow.Priority) _activeFlow = lease;
    }

    private void SetLeaseRate(FlowLease lease, int rate)
    {
        EnsureUsable();
        ValidateRate(rate);
        lease.Rate = rate;
        Rev++;
    }

    private void ReleaseFlow(FlowLease lease)
    {
        if (!_flowLeases.Remove(lease)) return;
        SelectFlow();
        Rev++;
    }

    private sealed class FlowLease(WorldStateService world, FlowPriority priority, int rate) : IFlowLease
    {
        private WorldStateService? _world = world;
        public FlowPriority Priority { get; } = priority;
        public int Rate { get; set; } = rate;
        public void SetRate(int rate) => Owner.SetLeaseRate(this, rate);
        public void Dispose()
        {
            var owner = _world;
            _world = null;
            owner?.ReleaseFlow(this);
        }
        private WorldStateService Owner => _world ?? throw new ObjectDisposedException(nameof(IFlowLease));
    }
}

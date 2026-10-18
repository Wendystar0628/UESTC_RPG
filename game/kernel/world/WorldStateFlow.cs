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
            FlowPriority.Cinematic => 1,
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
        // Another owner may have advanced past this suspended destination. Never rewind.
        if (_activeFlow?.Stop is { } stop && stop <= WorldHours) StopFlow(_activeFlow!);
    }

    private void SetLeaseRate(FlowLease lease, int rate)
    {
        EnsureUsable();
        ValidateRate(rate);
        lease.Rate = rate;
        lease.Stop = null;
        lease.Clock = null;
        Rev++;
    }

    private void FlowLeaseTo(FlowLease lease, double clock, int rate)
    {
        EnsureUsable();
        ValidateClock(clock);
        ValidateRate(rate);
        lease.Rate = rate;
        lease.Clock = clock;
        lease.Stop = _targetHours + ForwardHours(clock, ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24));
        if (lease.Stop == WorldHours && _targetHours == WorldHours) StopFlow(lease);
        Rev++;
    }

    private void ReleaseFlow(FlowLease lease)
    {
        if (!_flowLeases.Remove(lease)) return;
        SelectFlow();
        Rev++;
    }

    private void ClearFlowTargets()
    {
        foreach (var lease in _flowLeases) { lease.Stop = null; lease.Clock = null; }
    }

    private static void StopFlow(FlowLease lease)
    {
        lease.Rate = 0;
        lease.Stop = null;
        lease.Clock = null;
    }

    private sealed class FlowLease(WorldStateService world, FlowPriority priority, int rate) : IFlowLease
    {
        private WorldStateService? _world = world;
        public FlowPriority Priority { get; } = priority;
        public int Rate { get; set; } = rate;
        public double? Stop { get; set; }
        public double? Clock { get; set; }
        public void SetRate(int rate) => Owner.SetLeaseRate(this, rate);
        public void FlowTo(double clock, int rate) => Owner.FlowLeaseTo(this, clock, rate);
        public void Dispose()
        {
            var owner = _world;
            _world = null;
            owner?.ReleaseFlow(this);
        }
        private WorldStateService Owner => _world ?? throw new ObjectDisposedException(nameof(IFlowLease));
    }
}

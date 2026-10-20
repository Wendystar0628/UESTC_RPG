using Campus.Contracts;
using WorldFlow = Campus.Contracts.Flow;

namespace Campus.Kernel.World;

/// <summary>Owns baseline flow and override leases. Latest acquisition wins within one priority.</summary>
internal sealed class WorldFlowArbiter(Action ensureUsable, Action changed)
{
    private readonly List<FlowLease> _leases = [];
    private FlowLease? _active;

    public WorldFlow Current => new(_active?.Rate ?? Base.Rate);
    public WorldFlow Base { get; private set; }
    public int LastRateBeforePause { get; private set; } = 1;

    public void SetBase(int rate)
    {
        if (rate > 0) LastRateBeforePause = rate;
        Base = new(rate);
    }

    public void RestoreBase(int rate, int beforePause)
    {
        Base = new(rate);
        LastRateBeforePause = beforePause;
    }

    public IFlowLease Acquire(FlowPriority priority, int rate)
    {
        _ = priority switch
        {
            FlowPriority.Pause => 0,
            FlowPriority.AppFlow => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };
        FlowRates.Validate(rate);
        var lease = new FlowLease(this, priority, rate);
        _leases.Add(lease);
        Select();
        changed();
        return lease;
    }

    private void Select()
    {
        _active = null;
        foreach (var lease in _leases)
            if (_active is null || (byte)lease.Priority >= (byte)_active.Priority) _active = lease;
    }

    private void SetLeaseRate(FlowLease lease, int rate)
    {
        ensureUsable();
        FlowRates.Validate(rate);
        lease.Rate = rate;
        changed();
    }

    private void Release(FlowLease lease)
    {
        if (!_leases.Remove(lease)) return;
        Select();
        changed();
    }

    private sealed class FlowLease(WorldFlowArbiter owner, FlowPriority priority, int rate) : IFlowLease
    {
        private WorldFlowArbiter? _owner = owner;
        public FlowPriority Priority { get; } = priority;
        public int Rate { get; set; } = rate;
        public void SetRate(int rate) => Owner.SetLeaseRate(this, rate);
        public void Dispose()
        {
            var owner = _owner;
            _owner = null;
            owner?.Release(this);
        }
        private WorldFlowArbiter Owner => _owner ?? throw new ObjectDisposedException(nameof(IFlowLease));
    }
}

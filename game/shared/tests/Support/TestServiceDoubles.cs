using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Tests;

/// <summary>Shared IRuleset fake. Defaults: Fly and Walk, no cinematic states.</summary>
internal sealed class TestRuleset(string policy = "all-unlocked", IReadOnlyList<ViewMode>? viewModes = null,
    IReadOnlyList<AppState>? cinematicStates = null, string id = "test") : IRuleset
{
    /// <summary>The ruleset the entry flow and cinematic tests run under: Fly only, Title/Entering/Leaving cinematics.</summary>
    public static TestRuleset ForFlow() => new("none", [ViewMode.Fly], [AppState.Title, AppState.Entering, AppState.Leaving]);
    public RulesetDescriptor Current { get; } = new(new(id), viewModes ?? [ViewMode.Fly, ViewMode.Walk],
        WorldControlMask.All, policy, [], cinematicStates ?? []);
    public long Rev => 1;
}

internal sealed class TestMotionSettings(bool reduceMotion = false) : IMotionSettings
{
    public bool ReduceMotion => reduceMotion;
    public event Action? Changed { add { } remove { } }
}

/// <summary>Veil that covers and reveals instantly and counts readiness observations. Timing belongs to the veil timeline tests.</summary>
internal sealed class ImmediateVeil : IVeilService
{
    private readonly List<Lease> _leases = [];
    public float Opacity { get; private set; }
    public VeilReason Reason { get; private set; }
    public VeilContent Content { get; private set; }
    public bool Waiting => false;
    public int Reveals { get; private set; }
    public int ActiveObservations;
    public int ObservationCount;
    public IVeilLease Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog)
    {
        var lease = new Lease(this, reason, content);
        _leases.Add(lease);
        Reason = reason; Content = content; Opacity = 1;
        return lease;
    }
    public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness)
    {
        Interlocked.Increment(ref ActiveObservations);
        Interlocked.Increment(ref ObservationCount);
        return new Observation(() => Interlocked.Decrement(ref ActiveObservations));
    }
    private sealed class Lease(ImmediateVeil owner, VeilReason reason, VeilContent content) : IVeilLease
    {
        private bool _active = true;
        public Task Covered => Task.CompletedTask;
        public bool IsCurrent => _active && owner._leases.Count > 0 && ReferenceEquals(owner._leases[^1], this);
        public Task Reveal(float seconds)
        {
            if (!_active) return Task.CompletedTask;
            _active = false;
            owner._leases.Remove(this);
            if (owner._leases.Count == 0) { owner.Opacity = 0; owner.Reveals++; }
            else
            {
                var current = owner._leases[^1];
                owner.Reason = current.Reason;
                owner.Content = current.Content;
            }
            return Task.CompletedTask;
        }
        private VeilReason Reason => reason;
        private VeilContent Content => content;
        public void Dispose() => _ = Reveal(0);
    }
    private sealed class Observation(Action release) : IDisposable
    {
        private int _disposed;
        public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) release(); }
    }
}

/// <summary>Shared IAppFlow fake. Starts in Playing; <see cref="SetState"/> raises Changed and <see cref="Observers"/> counts subscribers. Enter throws, the rest are no-ops.</summary>
internal sealed class TestAppFlow : IAppFlow
{
    public AppState State { get; private set; } = AppState.Playing;
    public bool IsPlaying => State == AppState.Playing;
    private AppActivity _activity;
    public Func<AppActivity>? ActivitySource { get; set; }
    public AppActivity Activity { get => ActivitySource?.Invoke() ?? _activity; set => _activity = value; }
    public long Rev { get; private set; }
    public EnterPath? Path => null;
    public bool Skippable => false;
    public event Action<AppState, AppState>? Changed;
    public int Observers => Changed?.GetInvocationList().Length ?? 0;
    public void SetState(AppState state)
    {
        var previous = State;
        State = state;
        Rev++;
        Changed?.Invoke(previous, state);
    }
    public Task<EnterResult> Enter(EnterRequest request) => throw new NotSupportedException();
    public Task ReturnToTitle() => Task.CompletedTask;
    public void Skip() { }
    public Task Quit() => Task.CompletedTask;
}

/// <summary>Shared IAppExit fake. With allowQuit false any quit is a test failure.</summary>
internal sealed class TestAppExit(bool allowQuit = true) : IAppExit
{
    public int Calls { get; private set; }
    public TaskCompletionSource? Gate { get; set; }
    public TaskCompletionSource Started { get; } = new();
    public Task FinishQuit()
    {
        if (!allowQuit) throw new InvalidOperationException("This regression must never quit.");
        Calls++; Started.TrySetResult();
        return Gate?.Task ?? Task.CompletedTask;
    }
}

/// <summary>Shared ISaveService fake. Every operation is unexpected unless the test overrides it.</summary>
internal class TestSaveService : ISaveService
{
    public virtual bool Enabled => true;
    public virtual IReadOnlyList<SlotInfo> Slots => [];
    public virtual SlotId? Continue => null;
    public virtual SlotId? Active => null;
    public virtual bool Busy => false;
    public virtual SaveState State => new(Active, null, null, null, ReadOnlyReason.None);
    public event Action? SlotsChanged { add { } remove { } }
    public virtual bool HasPendingPose => false;
    public virtual void Request(SaveReason reason) { }
    public virtual Task<SaveResult> SaveNow(SaveReason reason) => throw new InvalidOperationException("Unexpected SaveNow.");
    public virtual Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full) => throw new InvalidOperationException("Unexpected Load.");
    public virtual Task CompleteResume() => throw new InvalidOperationException("Unexpected CompleteResume.");
    public virtual Task<LoadResult> LoadFile(string path) => throw new InvalidOperationException("Unexpected LoadFile.");
    public virtual Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null) => throw new InvalidOperationException("Unexpected NewGame.");
    public virtual Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => throw new InvalidOperationException("Unexpected FlushForQuit.");
    public virtual byte[]? Thumbnail(SlotId slot) => null;
}

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
    public float Opacity { get; private set; }
    public VeilReason Reason { get; private set; }
    public int Reveals { get; private set; }
    public int ActiveObservations;
    public int ObservationCount;
    public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog) { Reason = reason; Opacity = 1; return Task.CompletedTask; }
    public Task Reveal(VeilReason reason, float seconds) { Reason = reason; Opacity = 0; Reveals++; return Task.CompletedTask; }
    public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness)
    {
        Interlocked.Increment(ref ActiveObservations);
        Interlocked.Increment(ref ObservationCount);
        return new Observation(() => Interlocked.Decrement(ref ActiveObservations));
    }
    public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds) { }
    private sealed class Observation(Action release) : IDisposable
    {
        private int _disposed;
        public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) release(); }
    }
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

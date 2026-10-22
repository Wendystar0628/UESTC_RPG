using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Tests;

/// <summary>Shared IRuleset fake. Defaults: Fly and Walk.</summary>
internal sealed class TestRuleset(string policy = "all-unlocked", IReadOnlyList<ViewMode>? viewModes = null,
    string id = "test", ViewMode defaultViewMode = ViewMode.Fly) : IRuleset
{
    /// <summary>The ruleset used by entry-flow tests.</summary>
    public static TestRuleset ForFlow() => new("none", [ViewMode.Fly, ViewMode.Walk], defaultViewMode: ViewMode.Walk);
    public RulesetDescriptor Current { get; } = new(new(id), viewModes ?? [ViewMode.Fly, ViewMode.Walk],
        defaultViewMode, WorldControlMask.All, policy, []);
    public long Rev => 1;
}

internal sealed class TestMotionSettings(bool reduceMotion = false) : IMotionSettings
{
    public bool ReduceMotion => reduceMotion;
    public event Action? Changed { add { } remove { } }
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
    public string? LastError => null;
    public event Action<AppState, AppState>? Changed;
    public event Action<string>? PhaseReached { add { } remove { } }
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
    public Task Quit() => Task.CompletedTask;
}

/// <summary>Shared IAppExit fake. With allowQuit false any quit is a test failure.</summary>
internal sealed class TestAppExit(bool allowQuit = true) : IAppExit
{
    public int Calls { get; private set; }
    public TaskCompletionSource? Gate { get; set; }
    public TaskCompletionSource Started { get; } = new();
    public Task FinishQuit(int exitCode = 0)
    {
        RecordQuit();
        return Gate?.Task ?? Task.CompletedTask;
    }
    public void RequestQuit(int exitCode = 0) => RecordQuit();
    private void RecordQuit()
    {
        if (!allowQuit) throw new InvalidOperationException("This regression must never quit.");
        Calls++; Started.TrySetResult();
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
    public virtual void Request(SaveReason reason) { }
    public virtual Task<SaveResult> SaveNow(SaveReason reason) => throw new InvalidOperationException("Unexpected SaveNow.");
    public virtual Task<LoadResult> Load(SlotId slot) => throw new InvalidOperationException("Unexpected Load.");
    public virtual Task<LoadResult> LoadFile(string path) => throw new InvalidOperationException("Unexpected LoadFile.");
    public virtual Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null) => throw new InvalidOperationException("Unexpected NewGame.");
    public virtual Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => throw new InvalidOperationException("Unexpected FlushForQuit.");
    public virtual byte[]? Thumbnail(SlotId slot) => null;
}

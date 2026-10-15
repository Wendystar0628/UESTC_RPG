using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Quality.Adaptive;
using Godot;

namespace Campus.Kernel.Quality.Settings;

// Local engine boundary for the WP-32 application state machine, not a shared package contract.
public interface IQualityRuntime
{
    Task<double> NextFrame();
    bool Transitioning { get; }
    bool Booting { get; }
    bool OwnsQualityVeil { get; }
    float RefreshRate { get; }
    Vector2I OutputSize { get; }
    Vector2I ScreenSize { get; }
    GraphicsDevice ReadDevice();
    Task<DeviceTier> Sample(GraphicsDevice device);
    Task Cover();
    Task Reveal();
    Task WaitForDestination();
    void Dispatch(QualityView view, in QualityChange change);
    void ReportError(string message);
    void ReportInfo(string message);
}

[KernelService(typeof(IQualityService), Owner = "WP-32")]
public sealed class QualityService : IQualityService, IBootParticipant
{
    private sealed record Request(QualityProfile Profile, ApplyOrigin Origin, TaskCompletionSource<ApplyResult>? Completion);
    private readonly QualityCatalog _catalog;
    private readonly QualityState _state;
    private readonly IGraphicsSettingsStore _store;
    private IQualityRuntime? _runtime;
    private GraphicsDevice _device = new("Unknown", "Unknown", "Unknown", 0, 0, null, false);
    private string _deviceKey = "";
    private QualityProfile _durableProfile = QualityCatalog.Profile(QualityPreset.Balanced);
    private DeviceTier _durableTier = DeviceTier.MinSpec;
    private string _durableDeviceKey = "";
    private readonly Dictionary<QualityKnob, float> _pendingDisplay = [];
    private bool _userSelected;
    private bool _initialized;
    private bool _initializing;
    private Request? _pending;
    private Task? _active;
    private double _debounce;
    private double _confirmSeconds;
    private QualityProfile? _displayBefore;
    private Task _lastSave = Task.CompletedTask;

    public QualityService(QualityCatalog? catalog = null, IQualityRuntime? runtime = null, IGraphicsSettingsStore? store = null)
    {
        _catalog = catalog ?? new(Godot.FileAccess.GetFileAsString("res://data/quality-presets.json"));
        _state = new(_catalog);
        _store = store ?? new GraphicsSettingsStore(OS.GetUserDataDir(), _catalog);
        _runtime = runtime;
    }

    public DeviceTier Tier => _state.Tier;
    public QualityPreset Recommended => DeviceProbe.Recommend(Tier);
    public QualityProfile Requested => _state.Requested;
    public QualityView Effective => _state.Effective;
    public IReadOnlyList<KnobSpec> Knobs => _catalog.Knobs;
    public bool CaptureMode => _state.CaptureMode;
    public event Action<QualityChange>? Changed;
    public string Id => "quality";
    public int Order => 100;
    public float PresetValue(QualityPreset preset, QualityKnob knob) => _catalog.PresetValue(preset, knob);
    public ApplyPlan Plan(QualityProfile next) => _state.Plan(next);

    internal void Attach(QualityRuntime runtime) => _runtime = runtime;

    public Task Initialize(BootPhase phase, BootContext context) => phase switch
    {
        BootPhase.BeforeMap => Task.CompletedTask,
        BootPhase.AfterMapWarm => InitializeAsync(context),
        BootPhase.BeforeReveal => _lastSave,
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };

    /// <summary>Boot calls this after map/camera and prewarm are ready, while its veil is opaque.</summary>
    private async Task InitializeAsync(BootContext context)
    {
        if (_initialized) return;
        if (_initializing) throw new InvalidOperationException("Quality initialization is already running.");
        var runtime = _runtime ?? throw new InvalidOperationException("Quality runtime must be mounted before boot detection.");
        _initializing = true;
        try
        {
            _state.CaptureMode = context.CaptureMode;
            _catalog.PreserveOutputSize = CaptureMode;
            var size = CaptureMode ? runtime.OutputSize : runtime.ScreenSize;
            _catalog.OutputWidth = Math.Max(1, size.X);
            _catalog.OutputHeight = Math.Max(1, size.Y);
            _state.RefreshRate = runtime.RefreshRate;
            _catalog.RefreshRate = _state.RefreshRate;
            var stored = CaptureMode ? null : _store.Read();
            _device = runtime.ReadDevice();
            var outputWidth = _catalog.OutputWidth;
            var outputHeight = _catalog.OutputHeight;
            if (stored is not null && !CaptureMode)
            {
                var savedValues = _catalog.Resolve(stored.Profile, stored.Profile.Preset);
                if (savedValues[(int)QualityKnob.DisplayMode] == 0)
                {
                    var savedSize = QualityCatalog.WindowSizes[(int)savedValues[(int)QualityKnob.WindowSize]];
                    outputWidth = savedSize.Width;
                    outputHeight = savedSize.Height;
                }
            }
            _deviceKey = _device.CacheKey(outputWidth, outputHeight);
            var forcedTier = context.ExplicitArguments.Contains("tier");
            var tier = forcedTier ? context.Arguments["tier"] : null;
            _state.Tier = forcedTier ? tier switch
            {
                "min-spec" => DeviceTier.MinSpec,
                "dgpu-mid" => DeviceTier.DgpuMid,
                "dgpu-high" => DeviceTier.DgpuHigh,
                _ => throw new ArgumentException($"Unknown device tier: {tier}"),
            } : stored is not null && stored.DeviceKey == _deviceKey ? stored.Tier : DeviceProbe.Classify(_device) ?? await runtime.Sample(_device);
            _userSelected = stored?.UserSelected == true;
            var profile = !forcedTier && stored is not null && (stored.UserSelected || stored.DeviceKey == _deviceKey)
                ? stored.Profile : QualityCatalog.Profile(Recommended, !CaptureMode);
            _durableProfile = QualityCatalog.Freeze(forcedTier
                ? stored?.Profile ?? QualityCatalog.Profile(QualityPreset.Balanced)
                : profile);
            _durableTier = forcedTier ? stored?.Tier ?? DeviceTier.MinSpec : Tier;
            // A forced tier with no genuine cache must be detected again on the next ordinary launch.
            _durableDeviceKey = forcedTier ? stored?.DeviceKey ?? "" : _deviceKey;
            await runtime.NextFrame();
            Publish(_state.Commit(profile, ApplyOrigin.Startup, true));
            _initialized = true;
            runtime.ReportInfo($"[WP-32] device={_device.Name}; tier={Tier}; recommended={Recommended}; UMA={_device.Uma}; budget={_device.BudgetBytes}");
            // A command-line override must never be cached as the hardware recommendation.
            if (!CaptureMode && !forcedTier && (stored is null || stored.DeviceKey != _deviceKey)) QueueSave();
        }
        finally { _initializing = false; }
    }

    public Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin)
    {
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        if (origin == ApplyOrigin.Adaptive) throw new ArgumentException("Adaptive is a change origin, not a request.", nameof(origin));
        var plan = Plan(next);
        if (plan.Rejected.Count != 0) return Task.FromResult(Result(ApplyStatus.Rejected, plan.Rejected));
        var runtime = _runtime ?? throw new InvalidOperationException("Quality runtime is not mounted.");
        // The boot caller already owns an opaque veil; no second veil or transition wait is needed.
        if (origin == ApplyOrigin.Startup || origin == ApplyOrigin.CommandLine && runtime.Booting)
            return ApplyDuringBoot(plan.Next, origin);
        var deferred = runtime.Transitioning || _active is not null;
        _pending?.Completion?.TrySetResult(Result(ApplyStatus.Deferred));
        var completion = deferred ? null : new TaskCompletionSource<ApplyResult>();
        _pending = new(plan.Next, origin, completion);
        _debounce = origin == ApplyOrigin.User && plan.Heaviest is ApplyClass.Immediate or ApplyClass.Amortized ? 0.2 : 0;
        return completion?.Task ?? Task.FromResult(Result(ApplyStatus.Deferred));
    }

    public void ConfirmDisplay()
    {
        if (_displayBefore is null) return;
        _displayBefore = null;
        _confirmSeconds = 0;
        if (_pendingDisplay.Count != 0)
        {
            var before = _durableProfile;
            _durableProfile = MergeDurable(_durableProfile, _pendingDisplay);
            _pendingDisplay.Clear();
            if (!SameProfile(before, _durableProfile))
            {
                _userSelected = true;
                QueueSave();
            }
        }
    }

    public void Tick(double delta)
    {
        if (_active is { IsCompleted: true })
        {
            if (_active.IsFaulted) _runtime!.ReportError($"[WP-32] Quality apply failed: {_active.Exception!.GetBaseException().Message}");
            _active = null;
        }
        if (_displayBefore is not null)
        {
            _confirmSeconds -= Math.Max(0, delta);
            if (_confirmSeconds <= 0)
            {
                var restore = _state.RestoreDisplay(_displayBefore);
                _displayBefore = null;
                _pendingDisplay.Clear();
                _pending?.Completion?.TrySetResult(Result(ApplyStatus.Deferred));
                _pending = new(restore, ApplyOrigin.Revert, null);
                _debounce = 0;
            }
        }
        if (_pending is null || _active is not null || _initializing || _runtime!.Transitioning) return;
        _debounce -= Math.Max(0, delta);
        if (_debounce > 0) return;
        var request = _pending;
        _pending = null;
        _active = Execute(request);
    }

    private async Task<ApplyResult> ApplyDuringBoot(QualityProfile profile, ApplyOrigin origin)
    {
        await _runtime!.NextFrame();
        Publish(_state.Commit(profile, origin, true));
        return Result(ApplyStatus.Applied);
    }

    private async Task Execute(Request request)
    {
        var runtime = _runtime!;
        var snapshot = _state.Capture();
        var previousDisplay = _displayBefore;
        var previousCountdown = _confirmSeconds;
        var previousPendingDisplay = _pendingDisplay.ToArray();
        var previousDurable = _durableProfile;
        var previousUserSelected = _userSelected;
        var applied = false;
        var coverRequested = false;
        var persistenceAttempted = false;
        try
        {
            var plan = Plan(request.Profile);
            var veiled = plan.Deltas.Any(delta => delta.Apply == ApplyClass.UnderVeil);
            if (veiled)
            {
                coverRequested = true;
                await runtime.Cover();
            }
            await runtime.NextFrame();
            if (veiled && !runtime.OwnsQualityVeil) throw new InvalidOperationException("Quality veil was taken over before application.");
            applied = true;
            var change = _state.Commit(request.Profile, request.Origin, veiled);
            if (change.Knobs != 0) runtime.Dispatch(Effective, change);
            if (veiled)
            {
                await runtime.WaitForDestination();
                if (!runtime.OwnsQualityVeil) throw new InvalidOperationException("Quality veil was taken over during application.");
                await runtime.Reveal();
            }
            if (plan.NeedsConfirm && request.Origin == ApplyOrigin.User)
            {
                _displayBefore ??= snapshot.Profile;
                _confirmSeconds = 15;
            }
            // No failed dispatch/readiness/reveal operation is ever written to either settings file.
            if (request.Origin == ApplyOrigin.User && MergeUserIntoDurable(snapshot.Profile, request.Profile))
            {
                persistenceAttempted = !CaptureMode;
                QueueSave();
                await _lastSave;
            }
            Notify(change);
            request.Completion?.TrySetResult(Result(ApplyStatus.Applied));
        }
        catch (Exception error)
        {
            var recoveryErrors = new List<Exception>();
            _displayBefore = previousDisplay;
            _confirmSeconds = previousCountdown;
            _pendingDisplay.Clear();
            foreach (var pair in previousPendingDisplay) _pendingDisplay.Add(pair.Key, pair.Value);
            _durableProfile = previousDurable;
            _userSelected = previousUserSelected;
            if (applied)
            {
                var rollback = _state.Restore(snapshot, runtime.OwnsQualityVeil);
                try { runtime.Dispatch(Effective, rollback); }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
                Notify(rollback);
            }
            // The reason is checked again at the engine boundary; never reveal another flow's cover.
            if (coverRequested && runtime.OwnsQualityVeil)
            {
                try { await runtime.Reveal(); }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
            }
            if (persistenceAttempted)
            {
                try { QueueSave(); await _lastSave; }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
            }
            if (recoveryErrors.Count != 0)
            {
                var failure = new AggregateException("Quality application and recovery failed.", new[] { error }.Concat(recoveryErrors));
                request.Completion?.TrySetException(failure);
                throw failure;
            }
            request.Completion?.TrySetException(error);
            throw;
        }
    }

    private void Publish(in QualityChange change)
    {
        if (change.Knobs == 0) return;
        _runtime!.Dispatch(Effective, change);
        Notify(change);
    }

    private void Notify(in QualityChange change)
    {
        if (change.Knobs == 0 || Changed is null) return;
        foreach (Action<QualityChange> observer in Changed.GetInvocationList())
        {
            try { observer(change); }
            catch (Exception error) { _runtime!.ReportError($"[WP-32] Quality observer failed: {error.Message}"); }
        }
    }

    private bool MergeUserIntoDurable(QualityProfile beforeLive, QualityProfile input)
    {
        var before = _catalog.Resolve(beforeLive, beforeLive.Preset);
        var after = _catalog.Resolve(Requested, Requested.Preset);
        var priorDurable = _durableProfile;
        var durableValues = _catalog.Resolve(priorDurable, priorDurable.Preset);
        var presetChosen = input.Preset != QualityPreset.Custom && (input.Preset != beforeLive.Preset || input.Overrides.Count == 0);
        if (presetChosen) _durableProfile = QualityCatalog.Profile(Requested.Preset, priorDurable.Adaptive);
        var deltas = new Dictionary<QualityKnob, float>();
        foreach (var knob in QualityCatalog.All)
        {
            var changed = before[(int)knob] != after[(int)knob];
            if (!changed && (!presetChosen || knob == QualityKnob.Adaptive)) continue;
            if (QualityState.IsDisplay(knob) && _displayBefore is not null)
            {
                _pendingDisplay[knob] = after[(int)knob];
                // A new preset must not silently persist its unconfirmed display defaults.
                if (presetChosen) deltas[knob] = durableValues[(int)knob];
            }
            else deltas[knob] = after[(int)knob];
        }
        _durableProfile = MergeDurable(_durableProfile, deltas);
        var dirty = !SameProfile(priorDurable, _durableProfile) || presetChosen && !_userSelected;
        if (dirty) _userSelected = true;
        return dirty;
    }

    private QualityProfile MergeDurable(QualityProfile baseline, IReadOnlyDictionary<QualityKnob, float> deltas)
    {
        var overrides = baseline.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        var adaptive = baseline.Adaptive;
        foreach (var pair in deltas.OrderBy(pair => pair.Key))
        {
            if (pair.Key == QualityKnob.Adaptive) adaptive = pair.Value != 0;
            else overrides[pair.Key] = pair.Value;
        }
        // Reuse the existing canonicalization (including output-dependent render scale) without touching live state.
        var canonical = new QualityState(_catalog);
        canonical.Commit(new(baseline.Preset, overrides, adaptive), ApplyOrigin.Startup, false);
        return canonical.Requested;
    }

    private static bool SameProfile(QualityProfile left, QualityProfile right) => left.Preset == right.Preset &&
        left.Adaptive == right.Adaptive && left.Overrides.Count == right.Overrides.Count &&
        left.Overrides.All(pair => right.Overrides.TryGetValue(pair.Key, out var value) && pair.Value == value);

    private ApplyResult Result(ApplyStatus status, IReadOnlyList<QualityKnob>? rejected = null) =>
        new(status, Effective, _displayBefore is not null, _state.RestartPending, rejected ?? []);

    private void QueueSave()
    {
        if (CaptureMode) return;
        _lastSave = _store.Write(_durableProfile, _device, _durableTier, _durableDeviceKey, _userSelected);
    }
}

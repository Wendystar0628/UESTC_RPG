using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Godot;

namespace Campus.Kernel.Quality.Settings;

// Local engine boundary for the WP-32 application state machine, not a shared package contract.
public interface IQualityRuntime
{
    Task<double> NextFrame(CancellationToken cancellationToken);
    bool Transitioning { get; }
    bool Booting { get; }
    float RefreshRate { get; }
    Vector2I OutputSize { get; }
    Vector2I ScreenSize { get; }
    IVeilLease Cover(CancellationToken cancellationToken);
    Task WaitForDestination(CancellationToken cancellationToken);
    void Dispatch(QualityView view, in QualityChange change);
    void ReportError(string message);
    void ReportInfo(string message);
}

public interface IQualityFrame : IQualityService
{
    bool CaptureMode { get; }
    float GrassRadius { get; }
    float FoliageTransitionSeconds { get; }
    CancellationToken Bind(IQualityRuntime runtime);
    void Unbind(IQualityRuntime runtime);
    void Tick(double delta);
}

[KernelService(typeof(IQualityFrame), Owner = "WP-32")]
[KernelService(typeof(IQualityService), Owner = "WP-32")]
public sealed class QualityService : IQualityFrame, IBootParticipant
{
    internal const float VeilSeconds = 0.3f;
    private sealed class Binding
    {
        public IQualityRuntime Runtime { get; }
        public CancellationTokenSource Lifetime { get; } = new();
        public CancellationToken Token { get; }

        public Binding(IQualityRuntime runtime) { Runtime = runtime; Token = Lifetime.Token; }
    }

    private sealed record Request(QualityProfile Profile, ApplyOrigin Origin, TaskCompletionSource<ApplyResult>? Completion);
    private readonly QualityCatalog _catalog;
    private readonly QualityState _state;
    private readonly IGraphicsSettingsStore _store;
    private Binding? _binding;
    private QualityProfile _durableProfile;
    private bool _initialized;
    private bool _initializing;
    private Request? _pending;
    private Task? _active;
    private Request? _activeRequest;
    private double _debounce;
    private Task _lastSave = Task.CompletedTask;

    public QualityService(ISettingsStore settings)
        : this(new QualityCatalog(Godot.FileAccess.GetFileAsString("res://data/quality-presets.json")), settings) { }

    private QualityService(QualityCatalog catalog, ISettingsStore settings)
        : this(catalog, new GraphicsSettingsStore(settings, catalog)) { }

    internal QualityService(QualityCatalog catalog, IGraphicsSettingsStore store)
    {
        _catalog = catalog;
        _durableProfile = catalog.DefaultProfile;
        _state = new(_catalog);
        _store = store;
    }

    public QualityProfile Requested => _state.Requested;
    public QualityView Effective => _state.Effective;
    public IReadOnlyList<KnobSpec> Knobs => _catalog.Knobs;
    public bool CaptureMode => _state.CaptureMode;
    public float GrassRadius => _state.GrassRadius;
    public float FoliageTransitionSeconds => _catalog.FoliageTransitionSeconds;
    public event Action<QualityChange>? Changed;
    public string Id => "quality";
    public int Order => 100;
    public float PresetValue(QualityPreset preset, QualityKnob knob) => _catalog.PresetValue(preset, knob);
    public ApplyPlan Plan(QualityProfile next) => _state.Plan(next);

    public CancellationToken Bind(IQualityRuntime runtime)
    {
        if (_binding is not null) throw new InvalidOperationException("Quality runtime is already mounted.");
        _binding = new(runtime);
        return _binding.Token;
    }

    public void Unbind(IQualityRuntime runtime)
    {
        if (_binding is not { } binding || !ReferenceEquals(binding.Runtime, runtime)) return;
        var pending = _pending;
        var active = _activeRequest;
        _binding = null;
        _pending = null;
        _active = null;
        _activeRequest = null;
        _initialized = false;
        _initializing = false;
        _lastSave = Task.CompletedTask;
        binding.Lifetime.Cancel();
        binding.Lifetime.Dispose();
        pending?.Completion?.TrySetCanceled(binding.Token);
        active?.Completion?.TrySetCanceled(binding.Token);
    }

    public Task Initialize(BootPhase phase, BootContext context) => phase switch
    {
        BootPhase.BeforeMap => Task.CompletedTask,
        BootPhase.AfterMapWarm => InitializeAsync(context),
        BootPhase.BeforeReveal => _binding is { } binding ? _lastSave.WaitAsync(binding.Token) : Task.FromCanceled(new CancellationToken(true)),
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };

    /// <summary>Boot calls this after map/camera and prewarm are ready, while its veil is opaque.</summary>
    private async Task InitializeAsync(BootContext context)
    {
        if (_initialized) return;
        if (_initializing) throw new InvalidOperationException("Quality initialization is already running.");
        var binding = _binding ?? throw new InvalidOperationException("Quality runtime must be mounted before boot.");
        var runtime = binding.Runtime;
        var cancellationToken = binding.Token;
        _initializing = true;
        try
        {
            _state.CaptureMode = context.CaptureMode;
            _catalog.PreserveOutputSize = CaptureMode;
            var size = CaptureMode ? runtime.OutputSize : runtime.ScreenSize;
            _catalog.OutputWidth = Math.Max(1, size.X);
            _catalog.OutputHeight = Math.Max(1, size.Y);
            _state.RefreshRate = runtime.RefreshRate;
            // settings.json > catalog default; captures never read user://. The command line is applied after this.
            var profile = (CaptureMode ? null : _store.Read()) ?? _catalog.DefaultProfile;
            _durableProfile = QualityCatalog.Freeze(profile);
            await runtime.NextFrame(cancellationToken).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Publish(binding, _state.Commit(profile, ApplyOrigin.Startup, true));
            cancellationToken.ThrowIfCancellationRequested();
            _initialized = true;
            runtime.ReportInfo($"[WP-32] preset={Requested.Preset}; overrides={Requested.Overrides.Count}");
        }
        finally { if (ReferenceEquals(_binding, binding)) _initializing = false; }
    }

    public Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin)
    {
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        var plan = Plan(next);
        if (plan.Rejected.Count != 0) return Task.FromResult(Result(ApplyStatus.Rejected, plan.Rejected));
        var binding = _binding ?? throw new InvalidOperationException("Quality runtime is not mounted.");
        var runtime = binding.Runtime;
        // The boot caller already owns an opaque veil; no second veil or transition wait is needed.
        if (origin == ApplyOrigin.Startup || origin == ApplyOrigin.CommandLine && runtime.Booting)
            return ApplyDuringBoot(binding, plan.Next, origin);
        var deferred = runtime.Transitioning || _active is not null;
        _pending?.Completion?.TrySetResult(Result(ApplyStatus.Deferred));
        var completion = deferred ? null : new TaskCompletionSource<ApplyResult>();
        _pending = new(plan.Next, origin, completion);
        _debounce = origin == ApplyOrigin.User && plan.Heaviest is ApplyClass.Immediate or ApplyClass.Amortized ? 0.2 : 0;
        return completion?.Task ?? Task.FromResult(Result(ApplyStatus.Deferred));
    }

    public void Tick(double delta)
    {
        if (_binding is not { } binding) return;
        if (_active is { IsCompleted: true })
        {
            if (_active.IsFaulted) binding.Runtime.ReportError($"[WP-32] Quality apply failed: {_active.Exception!.GetBaseException().Message}");
            _active = null;
            _activeRequest = null;
        }
        if (_pending is null || _active is not null || _initializing || binding.Runtime.Transitioning) return;
        _debounce -= Math.Max(0, delta);
        if (_debounce > 0) return;
        var request = _pending;
        _pending = null;
        _activeRequest = request;
        _active = Execute(binding, request);
    }

    private async Task<ApplyResult> ApplyDuringBoot(Binding binding, QualityProfile profile, ApplyOrigin origin)
    {
        await binding.Runtime.NextFrame(binding.Token).WaitAsync(binding.Token);
        binding.Token.ThrowIfCancellationRequested();
        Publish(binding, _state.Commit(profile, origin, true));
        binding.Token.ThrowIfCancellationRequested();
        return Result(ApplyStatus.Applied);
    }

    private async Task Execute(Binding binding, Request request)
    {
        var runtime = binding.Runtime;
        var cancellationToken = binding.Token;
        var snapshot = _state.Capture();
        var previousDurable = _durableProfile;
        var applied = false;
        IVeilLease? veil = null;
        var persistenceAttempted = false;
        try
        {
            var plan = Plan(request.Profile);
            var veiled = plan.Deltas.Any(delta => delta.Apply == ApplyClass.UnderVeil);
            if (veiled)
            {
                veil = runtime.Cover(cancellationToken);
                await veil.Covered.WaitAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await runtime.NextFrame(cancellationToken).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (veil is { IsCurrent: false }) throw new InvalidOperationException("Quality veil was taken over before application.");
            applied = true;
            var change = _state.Commit(request.Profile, request.Origin, veiled);
            if (change.Knobs != 0) runtime.Dispatch(Effective, change);
            cancellationToken.ThrowIfCancellationRequested();
            if (veiled)
            {
                await runtime.WaitForDestination(cancellationToken).WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!veil!.IsCurrent) throw new InvalidOperationException("Quality veil was taken over during application.");
                await veil.Reveal(VeilSeconds).WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            // No failed dispatch/readiness/reveal operation is ever written to settings.json.
            if (request.Origin == ApplyOrigin.User && MergeUserIntoDurable(snapshot.Profile, request.Profile))
            {
                persistenceAttempted = !CaptureMode;
                QueueSave();
                await _lastSave.WaitAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            Notify(binding, change);
            cancellationToken.ThrowIfCancellationRequested();
            request.Completion?.TrySetResult(Result(ApplyStatus.Applied));
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            request.Completion?.TrySetCanceled(cancellationToken);
        }
        catch (Exception error)
        {
            var recoveryErrors = new List<Exception>();
            _durableProfile = previousDurable;
            if (applied)
            {
                var rollback = _state.Restore(snapshot, request.Origin, veil?.IsCurrent == true);
                try { runtime.Dispatch(Effective, rollback); }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
                if (cancellationToken.IsCancellationRequested) return;
                Notify(binding, rollback);
            }
            if (cancellationToken.IsCancellationRequested) return;
            if (veil is not null)
            {
                try { await veil.Reveal(VeilSeconds).WaitAsync(cancellationToken); }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
            }
            if (cancellationToken.IsCancellationRequested) return;
            if (persistenceAttempted)
            {
                try { QueueSave(); await _lastSave.WaitAsync(cancellationToken); }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
            }
            if (cancellationToken.IsCancellationRequested) return;
            if (recoveryErrors.Count != 0)
            {
                var failure = new AggregateException("Quality application and recovery failed.", new[] { error }.Concat(recoveryErrors));
                request.Completion?.TrySetException(failure);
                throw failure;
            }
            request.Completion?.TrySetException(error);
            throw;
        }
        finally { veil?.Dispose(); }
    }

    private void Publish(Binding binding, in QualityChange change)
    {
        binding.Token.ThrowIfCancellationRequested();
        if (change.Knobs == 0) return;
        binding.Runtime.Dispatch(Effective, change);
        Notify(binding, change);
    }

    private void Notify(Binding binding, in QualityChange change)
    {
        if (binding.Token.IsCancellationRequested) return;
        if (change.Knobs == 0 || Changed is null) return;
        foreach (Action<QualityChange> observer in Changed.GetInvocationList())
        {
            if (binding.Token.IsCancellationRequested) return;
            try { observer(change); }
            catch (Exception error)
            {
                if (binding.Token.IsCancellationRequested) return;
                binding.Runtime.ReportError($"[WP-32] Quality observer failed: {error.Message}");
            }
        }
    }

    /// <summary>Folds only the user's own edit into the saved profile, so a command-line overlay never reaches the file.</summary>
    private bool MergeUserIntoDurable(QualityProfile beforeLive, QualityProfile input)
    {
        var before = _catalog.Resolve(beforeLive, beforeLive.Preset);
        var after = _catalog.Resolve(Requested, Requested.Preset);
        var prior = _durableProfile;
        var presetChosen = input.Preset != QualityPreset.Custom && (input.Preset != beforeLive.Preset || input.Overrides.Count == 0);
        var baseline = presetChosen ? QualityCatalog.Profile(Requested.Preset) : prior;
        var deltas = new Dictionary<QualityKnob, float>();
        foreach (var knob in QualityCatalog.All)
            if (presetChosen || before[(int)knob] != after[(int)knob]) deltas[knob] = after[(int)knob];
        _durableProfile = MergeDurable(baseline, deltas);
        return !SameProfile(prior, _durableProfile);
    }

    private QualityProfile MergeDurable(QualityProfile baseline, IReadOnlyDictionary<QualityKnob, float> deltas)
    {
        var overrides = baseline.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var pair in deltas.OrderBy(pair => pair.Key)) overrides[pair.Key] = pair.Value;
        // Reuse the existing canonicalization (including output-dependent render scale) without touching live state.
        var canonical = new QualityState(_catalog);
        canonical.Commit(new(baseline.Preset, overrides), ApplyOrigin.Startup, false);
        return canonical.Requested;
    }

    private static bool SameProfile(QualityProfile left, QualityProfile right) => left.Preset == right.Preset &&
        left.Overrides.Count == right.Overrides.Count &&
        left.Overrides.All(pair => right.Overrides.TryGetValue(pair.Key, out var value) && pair.Value == value);

    private ApplyResult Result(ApplyStatus status, IReadOnlyList<QualityKnob>? rejected = null) =>
        new(status, Effective, false, rejected ?? []);

    private void QueueSave()
    {
        if (CaptureMode) return;
        _lastSave = _store.Write(_durableProfile);
    }
}

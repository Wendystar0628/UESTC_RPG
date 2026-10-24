using System.Runtime.ExceptionServices;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;

namespace Campus.Kernel.Quality.Settings;

// Local engine boundary for the game/kernel/quality/ application state machine, not a shared package contract.
public interface IQualityRuntime
{
    Task<double> NextFrame(CancellationToken cancellationToken);
    bool Transitioning { get; }
    bool Booting { get; }
    IAppLoadingLease ShowLoading(CancellationToken cancellationToken);
    Task WaitForDestination(CancellationToken cancellationToken);
    void Dispatch(QualityView view, in QualityChange change);
    void ReportError(string context, Exception error);
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

[KernelService(typeof(IQualityFrame))]
[KernelService(typeof(IQualityService))]
public sealed class QualityService : IQualityFrame, IBootParticipant, IAsyncDisposable
{
    private sealed class Binding
    {
        public IQualityRuntime Runtime { get; }
        public CancellationTokenSource Lifetime { get; } = new();
        public CancellationToken Token { get; }
        public Task? Active { get; set; }

        public Binding(IQualityRuntime runtime) { Runtime = runtime; Token = Lifetime.Token; }
    }

    private sealed record Request(long Operation, QualityRequest Edit, ApplyOrigin Origin, TaskCompletionSource<ApplyResult>? Completion);
    private readonly QualityCatalog _catalog;
    private readonly QualityState _state;
    private readonly IGraphicsSettingsStore _store;
    private Binding? _binding;
    private QualityProfile _durableProfile;
    private bool _initialized;
    private bool _initializing;
    private Request? _pending;
    private long _applyOperation;
    private double _debounce;
    private Task _lastSave = Task.CompletedTask;
    private readonly HashSet<Task> _applications = [];
    private readonly HashSet<Task> _deferredApplications = [];
    private Task? _disposal;
    private bool _disposed;

    public QualityService(ISettingsStore settings, IRequiredResources resources)
        : this(new QualityCatalog(resources.ReadText("res://data/quality-presets.json")), settings) { }

    private QualityService(QualityCatalog catalog, ISettingsStore settings)
        : this(catalog, new GraphicsSettingsStore(settings, catalog)) { }

    internal QualityService(QualityCatalog catalog, IGraphicsSettingsStore store)
    {
        _catalog = catalog;
        _durableProfile = catalog.DefaultProfile;
        _state = new(_catalog);
        _store = store;
    }

    public QualityProfile Default => _catalog.DefaultProfile;
    public QualityProfile Requested => _state.Requested;
    public QualityView Effective => _state.Effective;
    public IReadOnlyList<KnobSpec> Knobs => _catalog.Knobs;
    public bool CaptureMode => _state.CaptureMode;
    public float GrassRadius => _state.GrassRadius;
    public float FoliageTransitionSeconds => _catalog.FoliageTransitionSeconds;
    public event Action<QualityChange>? Changed;
    public event Action<QualityApplyEvent>? ApplyCommand;
    public string Id => "quality";
    public int Order => 100;
    public float PresetValue(QualityPreset preset, QualityKnob knob) => _catalog.PresetValue(preset, knob);
    public ApplyPlan Plan(QualityRequest next) => _state.Plan(next);

    public CancellationToken Bind(IQualityRuntime runtime)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_binding is not null) throw new InvalidOperationException("Quality runtime is already mounted.");
        _binding = new(runtime);
        return _binding.Token;
    }

    public void Unbind(IQualityRuntime runtime)
    {
        if (_binding is not { } binding || !ReferenceEquals(binding.Runtime, runtime)) return;
        var pending = _pending;
        _binding = null;
        _pending = null;
        _initialized = false;
        _initializing = false;
        binding.Lifetime.Cancel();
        binding.Lifetime.Dispose();
        if (pending is not null)
            ApplyCommand?.Invoke(new(pending.Operation, QualityApplyPhase.Canceled, pending.Edit, pending.Origin, null));
        pending?.Completion?.TrySetCanceled(binding.Token);
        if (binding.Active is { } active && (active.IsCompletedSuccessfully || active.IsCanceled))
            _deferredApplications.Remove(active);
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return new(_disposal ??= DrainApplications());
    }

    private async Task DrainApplications()
    {
        // Snapshot before cancellation can synchronously finish and retire an application.
        var applications = _applications.Concat(_deferredApplications).ToArray();
        if (_binding is { } binding) Unbind(binding.Runtime);
        var failures = new List<Exception>();
        foreach (var application in applications)
        {
            try { await application; }
            catch (OperationCanceledException) when (application.IsCanceled) { }
            catch (Exception error) { failures.Add(error); }
            finally { _deferredApplications.Remove(application); }
        }
        if (failures.Count > 0) throw new AggregateException("Quality shutdown failed.", failures);
    }

    public Task Initialize(BootPhase phase, BootContext context) => phase switch
    {
        BootPhase.BeforeMap => Task.CompletedTask,
        BootPhase.AfterMapWarm => InitializeAsync(context),
        BootPhase.BeforeReveal => _binding is { } binding ? _lastSave.WaitAsync(binding.Token) : Task.FromCanceled(new CancellationToken(true)),
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };

    /// <summary>Boot calls this after map/camera and prewarm are ready, while its screen is opaque.</summary>
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
            // settings.json > catalog default; captures never read user://. The command line is applied after this.
            var profile = (CaptureMode ? null : _store.Read()) ?? _catalog.DefaultProfile;
            _durableProfile = QualityCatalog.Freeze(profile);
            await runtime.NextFrame(cancellationToken).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Publish(binding, _state.Commit(profile, ApplyOrigin.Startup, true));
            cancellationToken.ThrowIfCancellationRequested();
            _initialized = true;
            runtime.ReportInfo($"[SYS-GRAPHICS] preset={Requested.Preset}; overrides={Requested.Overrides.Count}");
        }
        finally { if (ReferenceEquals(_binding, binding)) _initializing = false; }
    }

    public Task<ApplyResult> Apply(QualityRequest next, ApplyOrigin origin)
    {
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        var operation = ++_applyOperation;
        ApplyPlan plan;
        Binding binding;
        try
        {
            plan = Plan(next);
            if (plan.Rejected.Count != 0)
            {
                var rejected = Result(ApplyStatus.Rejected, plan.Rejected);
                ApplyCommand?.Invoke(new(operation, QualityApplyPhase.Rejected, next, origin, rejected));
                return Task.FromResult(rejected);
            }
            next = next with
            {
                Profile = QualityCatalog.Freeze(next.Profile.Preset == QualityPreset.Custom
                    ? next.Profile with { Preset = Requested.Preset } : next.Profile),
                EditedKnobs = Array.AsReadOnly(next.EditedKnobs.Distinct().Order().ToArray()),
            };
            binding = _binding ?? throw new InvalidOperationException("Quality runtime is not mounted.");
        }
        catch
        {
            ApplyCommand?.Invoke(new(operation, QualityApplyPhase.Faulted, next, origin, null));
            throw;
        }
        var runtime = binding.Runtime;
        // The boot caller already owns an opaque screen; no second screen or transition wait is needed.
        if (origin == ApplyOrigin.Startup || origin == ApplyOrigin.CommandLine && runtime.Booting)
            return ApplyDuringBoot(binding, new(operation, next, origin, null), plan.Next);
        var deferred = runtime.Transitioning || binding.Active is not null;
        if (_pending is { } superseded)
        {
            var replaced = Result(ApplyStatus.Deferred);
            ApplyCommand?.Invoke(new(superseded.Operation, QualityApplyPhase.Superseded, superseded.Edit, superseded.Origin, replaced));
            superseded.Completion?.TrySetResult(replaced);
        }
        var completion = deferred ? null : new TaskCompletionSource<ApplyResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = new(operation, next, origin, completion);
        _debounce = origin == ApplyOrigin.User && plan.Heaviest is ApplyClass.Immediate or ApplyClass.Amortized ? 0.2 : 0;
        var result = deferred ? Result(ApplyStatus.Deferred) : null;
        ApplyCommand?.Invoke(new(operation, QualityApplyPhase.Queued, next, origin, result));
        return completion?.Task ?? Task.FromResult(result!);
    }

    public void Tick(double delta)
    {
        if (_binding is not { } binding) return;
        if (binding.Active is { IsCompleted: true } completed)
        {
            binding.Active = null;
            _deferredApplications.Remove(completed);
            if (!completed.IsCanceled) completed.GetAwaiter().GetResult();
        }
        if (_pending is null || binding.Active is not null || _initializing || binding.Runtime.Transitioning) return;
        _debounce -= Math.Max(0, delta);
        if (_debounce > 0) return;
        var request = _pending;
        _pending = null;
        ApplyCommand?.Invoke(new(request.Operation, QualityApplyPhase.Started, request.Edit, request.Origin, null));
        var active = Execute(binding, request);
        binding.Active = active;
        if (request.Completion is null && !active.IsCompletedSuccessfully && !active.IsCanceled)
            _deferredApplications.Add(active);
    }

    private async Task<ApplyResult> ApplyDuringBoot(Binding binding, Request request, QualityProfile profile)
    {
        ApplyCommand?.Invoke(new(request.Operation, QualityApplyPhase.Started, request.Edit, request.Origin, null));
        var phase = QualityApplyPhase.Faulted;
        ApplyResult? result = null;
        try
        {
            await binding.Runtime.NextFrame(binding.Token).WaitAsync(binding.Token);
            binding.Token.ThrowIfCancellationRequested();
            Publish(binding, _state.Commit(profile, request.Origin, true));
            binding.Token.ThrowIfCancellationRequested();
            result = Result(ApplyStatus.Applied);
            phase = QualityApplyPhase.Completed;
            return result;
        }
        catch (OperationCanceledException) when (binding.Token.IsCancellationRequested)
        {
            phase = QualityApplyPhase.Canceled;
            throw;
        }
        finally { ApplyCommand?.Invoke(new(request.Operation, phase, request.Edit, request.Origin, result)); }
    }

    private async Task Execute(Binding binding, Request request)
    {
        var application = ApplyRequest(binding, request);
        if (request.Completion is not null && !application.IsCompleted) _applications.Add(application);
        var deferredFailed = false;
        try
        {
            await application;
            var result = Result(ApplyStatus.Applied);
            ApplyCommand?.Invoke(new(request.Operation, QualityApplyPhase.Completed, request.Edit, request.Origin, result));
            request.Completion?.TrySetResult(result);
        }
        catch (OperationCanceledException) when (binding.Token.IsCancellationRequested)
        {
            ApplyCommand?.Invoke(new(request.Operation, QualityApplyPhase.Canceled, request.Edit, request.Origin, null));
            request.Completion?.TrySetCanceled(binding.Token);
        }
        catch (Exception error)
        {
            ApplyCommand?.Invoke(new(request.Operation, QualityApplyPhase.Faulted, request.Edit, request.Origin, null));
            // This observer belongs to the binding, so unmounting cannot discard a late failure.
            if (request.Completion is { } completion)
            {
                completion.TrySetException(error);
                binding.Runtime.ReportError("[SYS-GRAPHICS] Quality apply failed:", error);
            }
            else
            {
                deferredFailed = true;
                binding.Runtime.ReportError("[SYS-GRAPHICS] Quality apply failed:", error);
                throw;
            }
        }
        finally
        {
            _applications.Remove(application);
            if (!deferredFailed && !ReferenceEquals(_binding, binding) && binding.Active is { } active)
                _deferredApplications.Remove(active);
        }
    }

    private async Task ApplyRequest(Binding binding, Request request)
    {
        var runtime = binding.Runtime;
        var cancellationToken = binding.Token;
        var snapshot = _state.Capture();
        var previousDurable = _durableProfile;
        var applied = false;
        IAppLoadingLease? screen = null;
        var persistenceAttempted = false;
        Exception? failure = null;
        try
        {
            var plan = Plan(request.Edit);
            var loading = plan.Deltas.Any(delta => delta.Apply == ApplyClass.UnderLoading);
            if (loading)
            {
                screen = runtime.ShowLoading(cancellationToken);
                await screen.Covered.WaitAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await runtime.NextFrame(cancellationToken).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            applied = true;
            var change = CommitRequest(binding, request, plan.Next, loading);
            cancellationToken.ThrowIfCancellationRequested();
            if (loading)
            {
                await runtime.WaitForDestination(cancellationToken).WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                screen!.Report(1, 1);
                await screen.Hide().WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            // No failed dispatch/readiness/loading operation is ever written to settings.json.
            if (request.Origin == ApplyOrigin.User && MergeUserIntoDurable(request.Edit))
            {
                persistenceAttempted = !CaptureMode;
                QueueSave();
                await _lastSave.WaitAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            Notify(binding, change);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        {
            failure = error;
        }
        catch (Exception error)
        {
            failure = await RecoverApplication(binding, request, snapshot, previousDurable, applied, screen, persistenceAttempted, error);
        }
        finally
        {
            if (screen is not null)
            {
                try { await screen.DisposeAsync(); }
                catch (Exception cleanup)
                {
                    failure = failure is null ? cleanup :
                        new AggregateException("Quality application and loading release failed.", failure, cleanup);
                }
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<Exception> RecoverApplication(Binding binding, Request request, QualityState.Snapshot snapshot,
        QualityProfile previousDurable, bool applied, IAppLoadingLease? screen, bool persistenceAttempted, [FailureHandler("SYS-GRAPHICS")] Exception error)
    {
        var cancellationToken = binding.Token;
        var recoveryErrors = new List<Exception>();
        if (!cancellationToken.IsCancellationRequested)
        {
            _durableProfile = previousDurable;
            if (applied)
            {
                try { RestoreAppliedState(binding, request, snapshot, screen is not null, recoveryErrors); }
                catch (Exception recovery) { recoveryErrors.Add(recovery); }
            }
        }
        if (!cancellationToken.IsCancellationRequested && screen is not null)
        {
            try { await screen.Hide().WaitAsync(cancellationToken); }
            catch (Exception recovery) { recoveryErrors.Add(recovery); }
        }
        if (!cancellationToken.IsCancellationRequested && persistenceAttempted)
        {
            try { QueueSave(); await _lastSave.WaitAsync(cancellationToken); }
            catch (Exception recovery) { recoveryErrors.Add(recovery); }
        }
        return recoveryErrors.Count == 0 ? error :
            new AggregateException("Quality application and recovery failed.", new[] { error }.Concat(recoveryErrors));
    }

    private QualityChange CommitRequest(Binding binding, Request request, QualityProfile profile, bool loading)
    {
        var change = _state.Commit(profile, request.Origin, loading);
        if (change.Knobs != 0) binding.Runtime.Dispatch(Effective, change);
        return change;
    }

    private void RestoreAppliedState(Binding binding, Request request, QualityState.Snapshot snapshot,
        bool loading, [FailureHandler("SYS-GRAPHICS")] List<Exception> recoveryErrors)
    {
        var cancellationToken = binding.Token;
        var rollback = _state.Restore(snapshot, request.Origin, loading);
        try { binding.Runtime.Dispatch(Effective, rollback); }
        catch (Exception recovery) { recoveryErrors.Add(recovery); }
        if (cancellationToken.IsCancellationRequested) return;
        Notify(binding, rollback);
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
        var failures = new List<Exception>();
        foreach (Action<QualityChange> observer in Changed.GetInvocationList())
        {
            if (binding.Token.IsCancellationRequested) break;
            try { observer(change); }
            catch (OperationCanceledException) when (binding.Token.IsCancellationRequested) { break; }
            catch (Exception error) { failures.Add(error); }
        }
        if (failures.Count > 0) throw new AggregateException("Quality observers failed.", failures);
    }

    /// <summary>Folds only the user's own edit into the saved profile, so a command-line overlay never reaches the file.</summary>
    private bool MergeUserIntoDurable(QualityRequest request)
    {
        var prior = _durableProfile;
        // Reuse the existing canonicalization (including scope defaults) without touching live state.
        var canonical = new QualityState(_catalog);
        canonical.Commit(_state.Compose(prior, request), ApplyOrigin.Startup, false);
        _durableProfile = canonical.Requested;
        return !SameProfile(prior, _durableProfile);
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

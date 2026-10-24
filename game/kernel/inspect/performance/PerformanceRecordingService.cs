using Campus.Contracts;
using Campus.Contracts.Diagnostics;
using Campus.Contracts.Perf;
using Campus.Contracts.Quality;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Campus.Kernel.Diagnostics;
using Godot;
using Environment = System.Environment;

namespace Campus.Kernel.Inspect;

/// <summary>ENG-PERF-DATA application lifetime. This service owns intent; the settings store owns its file.</summary>
[KernelService(typeof(IPerformanceRecorder))]
[KernelService(typeof(PerformanceRecordingService))]
public sealed class PerformanceRecordingService : IPerformanceRecorder, IRegistryExitParticipant
{
    private sealed record Request(bool Enabled, PerfEndReason Reason, TaskCompletionSource<PerfRecordingSnapshot>? Completion);
    private readonly IFrameStatistics _frames;
    private readonly ISettingsStore _settings;
    private readonly IDiagnosticLog _diagnostics;
    private readonly IInputRouter _input;
    private readonly IRequiredResources _resources;
    private readonly IQualityService _quality;
    private readonly Queue<Request> _requests = new();
    private readonly List<Task> _saves = [];
    private Task _processingTask = Task.CompletedTask;
    private bool _processing, _initialized, _exiting, _requested, _subscribed;
    private PerfRecordingState _state;
    private PerfFaultCode _fault;
    private PerfOrigin _origin;
    private PerfRecordingSession? _session;
    private ISettingsSection? _ui;
    private BootRunOptions _options = null!;
    private string _root = "";
    private string? _sessionId;
    private DateTimeOffset? _startedAt;
    private double _recordedSeconds, _lastAnchorMs;
    private Viewport? _viewport;
    private PerfEventCapture? _capture;

    internal void AttachViewport(Viewport viewport) => _viewport = viewport;
    internal void MapLoaded() => _capture?.BindMap();
    internal void MapUnloading() => _capture?.UnbindMap();
    internal void Readback(string phase, string purpose, ulong frame) => _capture?.Readback(phase, purpose, frame);
    internal void ObserveRequestFailure([FailureHandler("ENG-PERF-DATA")] Exception error) => ReceiveFailure(error);

    public PerformanceRecordingService(IFrameStatistics frames, ISettingsStore settings, IDiagnosticLog diagnostics,
        IInputRouter input, IRequiredResources resources, IQualityService quality)
    {
        _frames = frames;
        _settings = settings;
        _diagnostics = diagnostics;
        _input = input;
        _resources = resources;
        _quality = quality;
    }

    public PerfRecordingSnapshot Snapshot
    {
        get
        {
            RequireMainThread();
            double seconds = _state == PerfRecordingState.Recording && _session is { } current
                ? Math.Max(0, Now() - current.RecordingStart) / 1000 : _recordedSeconds;
            return new(_requested, _state, _fault, _origin, _sessionId, _startedAt, seconds);
        }
    }
    public event Action<PerfRecordingSnapshot>? StateChanged;
    public PerfSessionEndResult? LastEndResult { get; private set; }
    public event Action<PerfSessionEndResult>? SessionEnded;

    // Composition calls once, before exposing recording controls. No author settings access in AI mode.
    public Task<PerfRecordingSnapshot> InitializeAsync(BootRunOptions options)
    {
        RequireMainThread();
        if (_initialized) throw new InvalidOperationException("Performance recorder is already initialized.");
        _initialized = true;
        _options = options;
        _origin = options.PerfOutputDirectory is null ? PerfOrigin.Play : PerfOrigin.Ai;
        _root = options.PerfOutputDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UESTC", "perf");
        if (_origin == PerfOrigin.Play)
        {
            _ui = _settings.Section("ui");
            _requested = _ui.Get(PerfSettingDefinitions.RecordPerformance);
        }
        else _requested = true;
        return Enqueue(_requested, PerfEndReason.Toggle);
    }

    public Task<PerfRecordingSnapshot> SetRecordingAsync(bool requested, CancellationToken cancellationToken = default)
    {
        RequireMainThread();
        if (!_initialized) throw new InvalidOperationException("Performance recorder has not been initialized.");
        if (_exiting) return Task.FromResult(Snapshot);
        _requested = requested;
        if (_origin == PerfOrigin.Play)
        {
            _ui!.Set(PerfSettingDefinitions.RecordPerformance.Key, requested);
            _saves.RemoveAll(task => task.IsCompletedSuccessfully);
            _saves.Add(_settings.SaveAsync());
        }
        Task<PerfRecordingSnapshot> task = Enqueue(requested, PerfEndReason.Toggle);
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }

    public bool TryMark(in PerfMarker marker)
    {
        RequireMainThread();
        if (_state != PerfRecordingState.Recording || _session is null) return false;
        if (string.IsNullOrWhiteSpace(marker.Id) || marker.Id == "null") throw new ArgumentException("Marker ID is required.", nameof(marker));
        string phase = marker.Phase switch
        {
            PerfMarkerPhase.Instant => "instant", PerfMarkerPhase.Begin => "begin", PerfMarkerPhase.End => "end",
            _ => throw new ArgumentOutOfRangeException(nameof(marker))
        };
        return _session.TryEvent(new("segment", Engine.GetProcessFrames(), Now(), new { id = marker.Id, phase }));
    }

    // Host housekeeping only; frame samples arrive exclusively through SamplePublished.
    public void Poll()
    {
        RequireMainThread();
        if (_session is not { } current) return;
        if (_state == PerfRecordingState.Recording)
        {
            try { _capture?.Poll(); }
            catch (Exception error) { ProducerFailure(error); }
        }
        if (current.Fault != PerfFaultCode.None && _state is PerfRecordingState.Starting or PerfRecordingState.Recording)
        {
            StopAccepting(PerfEndReason.Failure);
            _fault = current.Fault;
            ChangeState(PerfRecordingState.Faulted);
            QueueCleanup(PerfEndReason.Failure);
        }
        else if (_state == PerfRecordingState.Recording && Now() - _lastAnchorMs >=
                 _dictionary!.AnchorIntervalMilliseconds)
        {
            var anchor = CaptureAnchor();
            if (current.TryEvent(new("clock.anchor", null, anchor.MonotonicMs, anchor, anchor)))
                _lastAnchorMs = anchor.MonotonicMs;
        }
    }

    private PerfFieldDictionary? _dictionary;

    private Task<PerfRecordingSnapshot> Enqueue(bool enabled, PerfEndReason reason)
    {
        var completion = new TaskCompletionSource<PerfRecordingSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        _requests.Enqueue(new(enabled, reason, completion));
        StartRequests();
        return completion.Task;
    }

    private void QueueCleanup(PerfEndReason reason)
    {
        _requests.Enqueue(new(false, reason, null));
        StartRequests();
    }

    private void StartRequests()
    {
        if (_processing) return;
        _processing = true;
        _processingTask = ProcessRequestsAsync();
    }

    private async Task ProcessRequestsAsync()
    {
        try
        {
            while (_requests.TryDequeue(out var request))
            {
                try
                {
                    if (request.Enabled && !_exiting) await StartAsync();
                    else await StopAsync(request.Reason);
                    request.Completion?.TrySetResult(Snapshot);
                }
                catch (Exception error)
                {
                    var failure = await EndRequestFailureAsync(error);
                    request.Completion?.TrySetException(failure);
                }
            }
        }
        finally { _processing = false; }
    }

    private async Task<Exception> EndRequestFailureAsync([FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        Exception failure = error;
        _session?.AddFailure(PerfFaultCode.ProducerFailed, error);
        try { await StopAsync(PerfEndReason.Failure); }
        catch (Exception cleanupError)
        {
            failure = RetainedFailure.Append(failure, cleanupError);
        }
        if (_fault == PerfFaultCode.None) _fault = PerfFaultCode.ProducerFailed;
        try { ChangeState(PerfRecordingState.Faulted); }
        catch (Exception notificationError)
        {
            failure = RetainedFailure.Append(failure, notificationError);
        }
        var ended = LastEndResult?.SessionId == _sessionId ? LastEndResult : null;
        LastEndResult = new(_sessionId ?? "", PerfEndReason.Failure, ended?.Closed ?? false,
            false, _fault, failure, _recordedSeconds, ended?.WrittenFrames ?? 0, ended?.DroppedFrames ?? 0);
        try { ReceiveFailure(failure); }
        catch (Exception diagnosticError)
        {
            failure = new AggregateException(failure, diagnosticError);
            LastEndResult = LastEndResult with { Error = failure };
        }
        return failure;
    }

    private async Task StartAsync()
    {
        if (_state == PerfRecordingState.Recording) return;
        if (_session is not null) await StopAsync(PerfEndReason.Failure);
        _fault = PerfFaultCode.None;
        _sessionId = null;
        _startedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)(Godot.Time.GetUnixTimeFromSystem() * 1000));
        _recordedSeconds = 0;
        ChangeState(PerfRecordingState.Starting);
        try { _dictionary = new(_resources.ReadText("res://data/perf-fields.json")); }
        catch (Exception error)
        {
            EndStartupFailure(PerfFaultCode.InvalidDictionary, error);
            return;
        }
        try
        {
            var manifest = PerfManifest.Capture(_options, _input.DesktopState, _quality);
            _session = new(_root, _origin, _startedAt.Value, _dictionary, manifest);
        }
        catch (Exception error)
        {
            EndStartupFailure(PerfFaultCode.OpenFailed, error);
            return;
        }
        var current = _session!;
        bool ready = await current.Ready;
        _sessionId = current.Id;
        if (!ready || _exiting)
        {
            if (!ready) { _fault = current.Fault; ChangeState(PerfRecordingState.Faulted); }
            await StopAsync(!ready ? PerfEndReason.Failure : PerfEndReason.Exit);
            return;
        }
        var anchor = CaptureAnchor();
        try
        {
            var samples = current.AddProducer("main", "samples");
            var watchdog = current.AddProducer("watchdog");
            _capture = new(current, _dictionary!, samples, _viewport ?? throw new InvalidOperationException("Recording viewport is not attached."));
            _capture.Start();
            current.StartWatchdog(_frames, watchdog);
            current.AttachCapture();
            current.Begin(anchor);
        }
        catch (Exception error)
        {
            ProducerFailure(error);
            return;
        }
        bool startedWriting = await current.StartedWriting;
        if (!startedWriting || _exiting || current.Fault != PerfFaultCode.None)
        {
            if (!startedWriting || current.Fault != PerfFaultCode.None)
            {
                _fault = current.Fault;
                ChangeState(PerfRecordingState.Faulted);
            }
            await StopAsync(_exiting ? PerfEndReason.Exit : PerfEndReason.Failure);
            return;
        }
        _lastAnchorMs = anchor.MonotonicMs;
        _frames.SamplePublished += OnFrame;
        _subscribed = true;
        ChangeState(PerfRecordingState.Recording);
    }

    private void OnFrame(in FrameSample sample, ReadOnlySpan<ViewportFrameSample> viewports)
    {
        if (_state != PerfRecordingState.Recording || _session is not { } current) return;
        try
        {
            var context = _capture!.OnFrame(in sample);
            current.Frames.TryWrite(in sample, viewports, context.Flags, context.Complete);
        }
        catch (Exception error)
        {
            ProducerFailure(error);
        }
    }

    private void ProducerFailure([FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        if (_session is not { } current) throw new InvalidOperationException("Producer failed without an owning session.", error);
        current.TryEvent(new("producer.failure", null, Now(), new { producer = "main", exceptionType = error.GetType().FullName }));
        current.AddFailure(PerfFaultCode.ProducerFailed, error);
        StopAccepting(PerfEndReason.Failure);
        _fault = current.Fault;
        ChangeState(PerfRecordingState.Faulted);
        QueueCleanup(PerfEndReason.Failure);
    }

    private void StopAccepting(PerfEndReason reason)
    {
        if (_subscribed) { _frames.SamplePublished -= OnFrame; _subscribed = false; }
        if (_session is null) return;
        try { _capture?.Dispose(); }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
        _capture = null;
        _session.StopProducers();
        _recordedSeconds = Math.Max(0, Now() - _session.RecordingStart) / 1000;
    }

    private async Task StopAsync(PerfEndReason reason)
    {
        if (_session is not { } current)
        {
            if (reason == PerfEndReason.Toggle) { _fault = PerfFaultCode.None; ChangeState(PerfRecordingState.Off); }
            return;
        }
        bool acknowledgingFault = _state == PerfRecordingState.Faulted && reason == PerfEndReason.Toggle;
        StopAccepting(reason);
        if (_state != PerfRecordingState.Faulted) ChangeState(PerfRecordingState.Stopping);
        await current.DrainProducersAsync();
        current.Stop(reason, Now());
        var result = await current.Completion;
        _session = null;
        _sessionId = result.SessionId;
        _recordedSeconds = result.RecordedSeconds;
        _fault = result.Fault;
        LastEndResult = result;
        if (result.Error is { } error) ReceiveFailure(error);
        ChangeState(result.Fault == PerfFaultCode.None ? PerfRecordingState.Off : PerfRecordingState.Faulted);
        SessionEnded?.Invoke(result);
        if (acknowledgingFault)
        {
            _fault = PerfFaultCode.None;
            ChangeState(PerfRecordingState.Off);
        }
    }

    private void EndStartupFailure(PerfFaultCode fault, [FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        _fault = fault;
        ReceiveFailure(error);
        var result = new PerfSessionEndResult(_sessionId ?? "", PerfEndReason.Failure, false, false, fault, error, 0, 0, 0);
        LastEndResult = result;
        ChangeState(PerfRecordingState.Faulted);
        SessionEnded?.Invoke(result);
    }

    private void ReceiveFailure([FailureHandler("ENG-PERF-DATA")] Exception error) =>
        _diagnostics.Error("Performance recording failed; the application continues.", error);

    private void ChangeState(PerfRecordingState state)
    {
        if (_state == state) return;
        _state = state;
        if (StateChanged is not { } changed) return;
        var snapshot = Snapshot;
        Exception? failure = null;
        foreach (Action<PerfRecordingSnapshot> subscriber in changed.GetInvocationList())
        {
            try { subscriber(snapshot); }
            catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void PrepareForExit()
    {
        RequireMainThread();
        if (_exiting) return;
        _exiting = true;
        StopAccepting(PerfEndReason.Exit);
        if (_session is not null && _state != PerfRecordingState.Faulted) ChangeState(PerfRecordingState.Stopping);
        QueueCleanup(PerfEndReason.Exit);
    }

    public async Task DrainForExitAsync()
    {
        PrepareForExit();
        await _processingTask;
        await Task.WhenAll(_saves);
    }

    private static double Now() => FrameClock.MonotonicMilliseconds;
    private static PerfClockAnchor CaptureAnchor()
    {
        double before = Now();
        long unix = (long)(Godot.Time.GetUnixTimeFromSystem() * 1000);
        double after = Now();
        return new(unix, (before + after) / 2, after - before);
    }
    private static void RequireMainThread()
    {
        if (OS.GetThreadCallerId() != OS.GetMainThreadId()) throw new InvalidOperationException("Performance recorder requires the main thread.");
    }
}

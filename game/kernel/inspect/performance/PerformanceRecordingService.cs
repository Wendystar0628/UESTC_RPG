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
    private readonly CommandNotification _notifications = new();
    private readonly CancellationTokenSource _exit = new();
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
    private PerfGpuStepsCapture? _gpuSteps;

    internal void AttachViewport(Viewport viewport) => _viewport = viewport;
    internal void MapLoaded() => _capture?.BindMap();
    internal void MapUnloading() => _capture?.UnbindMap();
    internal void Readback(string phase, string purpose, ulong frame) => _capture?.Readback(phase, purpose, frame);
    internal void AppPhase(string phase, ulong frame, double time) => _capture?.AppPhase(phase, frame, time);

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
    public PerfSamplingFacts SamplingFacts
    {
        get
        {
            RequireMainThread();
            return _state is PerfRecordingState.Starting or PerfRecordingState.Recording or PerfRecordingState.Stopping
                ? _session?.SamplingFacts ?? default : default;
        }
    }
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
        try { PollSession(current); }
        finally { current.FlushEvents(); }
    }

    private void PollSession(PerfRecordingSession current)
    {
        if (_state == PerfRecordingState.Recording)
        {
            try { _capture?.Poll(); }
            catch (Exception error) { ProducerFailure(error); }
        }
        if (current.Fault != PerfFaultCode.None && _state is PerfRecordingState.Starting or PerfRecordingState.Recording)
        {
            QueueFailureCleanup(current);
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
                    else ReceiveEndFailure(await StopAsync(request.Reason));
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
        ChangeState(PerfRecordingState.Faulted);
        var ended = LastEndResult?.SessionId == _sessionId ? LastEndResult : null;
        if (ended?.Error is { } sessionError) failure = RetainedFailure.Append(failure, sessionError);
        LastEndResult = new(_sessionId ?? "", PerfEndReason.Failure, ended?.Closed ?? false,
            false, _fault, failure, _recordedSeconds, ended?.WrittenFrames ?? 0, ended?.DroppedFrames ?? 0);
        try { ReceiveFailure(failure); }
        catch (Exception diagnosticError)
        {
            failure = RetainedFailure.Append(failure, diagnosticError);
            LastEndResult = LastEndResult with { Error = failure };
        }
        return failure;
    }

    private async Task StartAsync()
    {
        if (_state == PerfRecordingState.Recording) return;
        if (_session is not null) ReceiveEndFailure(await StopAsync(PerfEndReason.Failure));
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
            ReceiveEndFailure(await StopAsync(!ready ? PerfEndReason.Failure : PerfEndReason.Exit));
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
            var adapter = await RenderAdapterIdentity.CaptureAsync(_exit.Token);
            if (_exiting && adapter.Status.Error is { } captureError)
                current.AddFailure(PerfFaultCode.ProducerFailed, captureError);
            _exit.Token.ThrowIfCancellationRequested();
            current.StartHardware(adapter);
            current.AttachCapture();
            if (_origin == PerfOrigin.Ai && PerfGpuStepsCapture.Requested)
                _gpuSteps = new(Path.Combine(_root, current.Id!));
            current.Begin(anchor);
        }
        catch (OperationCanceledException error) when (_exit.IsCancellationRequested && error.CancellationToken == _exit.Token)
        {
            ReceiveEndFailure(await StopAsync(PerfEndReason.Exit));
            return;
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
            ReceiveEndFailure(await StopAsync(_exiting ? PerfEndReason.Exit : PerfEndReason.Failure));
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
            long flags = _capture!.OnFrame(in sample);
            _gpuSteps?.CaptureFrame();
            current.TryFrame(in sample, viewports, flags);
        }
        catch (Exception error)
        {
            ProducerFailure(error);
        }
        finally { current.FlushEvents(); }
    }

    private void ProducerFailure([FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        if (_session is not { } current) throw new InvalidOperationException("Producer failed without an owning session.", error);
        current.AddFailure(PerfFaultCode.ProducerFailed, error);
        try { current.TryEvent(new("producer.failure", null, Now(), new { producer = "main", exceptionType = error.GetType().FullName })); }
        catch (Exception recordingError) { current.AddFailure(PerfFaultCode.ProducerFailed, recordingError); }
        QueueFailureCleanup(current);
    }

    private void QueueFailureCleanup(PerfRecordingSession current)
    {
        StopAccepting(PerfEndReason.Failure);
        _fault = current.Fault;
        _requests.Enqueue(new(false, PerfEndReason.Failure, null));
        ChangeState(PerfRecordingState.Faulted);
        StartRequests();
    }

    private void StopAccepting(PerfEndReason reason)
    {
        if (_subscribed) { _frames.SamplePublished -= OnFrame; _subscribed = false; }
        if (_session is null) return;
        try { _capture?.Dispose(); }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
        _capture = null;
        _session.FlushEvents();
        _gpuSteps?.RequestStop();
        _session.StopProducers();
        _recordedSeconds = Math.Max(0, Now() - _session.RecordingStart) / 1000;
    }

    private async Task<PerfSessionEndResult?> StopAsync(PerfEndReason reason)
    {
        if (_session is not { } current)
        {
            if (reason == PerfEndReason.Toggle) { _fault = PerfFaultCode.None; ChangeState(PerfRecordingState.Off); }
            return null;
        }
        bool acknowledgingFault = _state == PerfRecordingState.Faulted && reason == PerfEndReason.Toggle;
        StopAccepting(reason);
        if (_state != PerfRecordingState.Faulted) ChangeState(PerfRecordingState.Stopping);
        if (_gpuSteps is { } gpuSteps)
        {
            try { await gpuSteps.DrainAsync(); }
            catch (Exception error) { current.AddFailure(PerfFaultCode.ProducerFailed, error); }
            finally { _gpuSteps = null; }
        }
        await current.DrainProducersAsync();
        current.Stop(reason, Now());
        var result = await current.Completion;
        _session = null;
        _sessionId = result.SessionId;
        _recordedSeconds = result.RecordedSeconds;
        _fault = result.Fault;
        LastEndResult = result;
        ChangeState(result.Fault == PerfFaultCode.None ? PerfRecordingState.Off : PerfRecordingState.Faulted);
        _notifications.Publish(SessionEnded, result);
        if (acknowledgingFault)
        {
            _fault = PerfFaultCode.None;
            ChangeState(PerfRecordingState.Off);
        }
        return result;
    }

    private void ReceiveEndFailure(PerfSessionEndResult? result)
    {
        if (result?.Error is not { } error) return;
        if (result.Fault == PerfFaultCode.None)
            _diagnostics.Error("Hardware performance sampling was degraded; other recording streams continued.", error);
        else ReceiveFailure(error);
    }

    private void EndStartupFailure(PerfFaultCode fault, [FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        _fault = fault;
        ReceiveFailure(error);
        var result = new PerfSessionEndResult(_sessionId ?? "", PerfEndReason.Failure, false, false, fault, error, 0, 0, 0);
        LastEndResult = result;
        ChangeState(PerfRecordingState.Faulted);
        _notifications.Publish(SessionEnded, result);
    }

    private void ReceiveFailure([FailureHandler("ENG-PERF-DATA")] Exception error) =>
        _diagnostics.Error("Performance recording failed; the application continues.", error);

    private void ChangeState(PerfRecordingState state)
    {
        if (_state == state) return;
        _state = state;
        _notifications.Publish(StateChanged, Snapshot);
    }

    public void PrepareForExit()
    {
        RequireMainThread();
        if (_exiting) return;
        _exiting = true;
        StopAccepting(PerfEndReason.Exit);
        _exit.Cancel();
        _requests.Enqueue(new(false, PerfEndReason.Exit, null));
        if (_session is not null && _state != PerfRecordingState.Faulted) ChangeState(PerfRecordingState.Stopping);
        StartRequests();
    }

    public async Task DrainForExitAsync()
    {
        PrepareForExit();
        Exception? failure = null;
        try { await _processingTask; }
        catch (Exception error) { failure = error; }
        _exit.Dispose();
        try { await Task.WhenAll(_saves); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (_notifications.TakeFailure() is { } notification)
            failure = RetainedFailure.Append(failure, notification);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
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

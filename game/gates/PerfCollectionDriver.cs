using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Perf;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Owns sample markers and validity over existing production state; never samples hardware or computes costs.</summary>
internal sealed class PerfCollectionDriver(IStageGateHost host, PerfCollectionOptions options)
{
    private readonly StartupSampleInput _input = new(host);
    private readonly PerfCollectionConditions _conditions = new(options);
    private readonly List<string> _reasons = [];
    private readonly List<PollutionObservation> _pollution = new(6);
    private FramePipelineCounts _samplePipelines;
    private string? _session;
    private double _begin, _end;
    private bool _markedBegin, _markedEnd, _gpuObserved;
    private Pose? _fixedPose;
    private ViewMode _fixedMode;
    private bool _samplePartial;
    private SaveObservation? _save;
    private bool _movingSample;
    private bool _pipelineCompilationObserved;
    internal IReadOnlyList<PerfTravelSegment> TravelSegments { get; set; } = [];

    public async Task<bool> Run(Func<Task<string?>>? activity = null, bool verifyFixedStart = false)
    {
        _movingSample = activity is not null;
        var recorder = KernelServices.Get<IPerformanceRecorder>();
        // Title NewGame uses its canonical world. Apply the explicit collection flow through its sole owner.
        if (Boot.Options.World.FlowRate is { } flow) KernelServices.Get<IWorldControl>().SetFlow(flow);
        if (await WaitForStable(recorder) is { } rejected) return Finish(recorder, rejected.Status, rejected.Reason);
        if (verifyFixedStart && VerifyFixedStart() is { } startFailure) return Finish(recorder, "invalid", startFailure);
        _session = recorder.Snapshot.SessionId;
        _samplePipelines = KernelServices.Get<IFrameStatistics>().Latest!.Value.Pipelines;
        if (!recorder.TryMark(new(options.SampleSegmentId, PerfMarkerPhase.Begin)))
            return Finish(recorder, "partial", "Recording refused the sample Begin marker.");
        _markedBegin = true;
        _begin = FrameClock.MonotonicMilliseconds;
        if (activity is null) await SampleIdle(recorder);
        else
        {
            void Observe(in FrameSample sample, ReadOnlySpan<ViewportFrameSample> viewports) => ObserveSample(recorder);
            var frames = KernelServices.Get<IFrameStatistics>();
            frames.SamplePublished += Observe;
            try { if (await activity() is { } failure) _reasons.Add(failure); }
            finally { frames.SamplePublished -= Observe; }
            if (_reasons.Count == 0 && FrameClock.MonotonicMilliseconds - _begin < options.SampleSeconds * 1000) await SampleIdle(recorder);
        }
        ObserveSample(recorder);
        if (_reasons.Count > 0) return Finish(recorder, _samplePartial ? "partial" : "invalid", null);
        if (!_gpuObserved)
            return Finish(recorder, "partial", "Sample has no fresh GPU query after its accepted Begin.");
        End(recorder);
        if (!_markedEnd) return Finish(recorder, "partial", null);
        if (options.SaveAfterSample)
        {
            var started = FrameClock.MonotonicMilliseconds;
            SaveResult saved;
            try { saved = await KernelServices.Get<ISaveService>().SaveNow(SaveReason.Manual); }
            catch (Exception error)
            {
                _save = new(nameof(SaveReason.Manual), "Faulted", FrameClock.MonotonicMilliseconds - started);
                Finish(recorder, "partial", error.ToString());
                throw;
            }
            _save = new(nameof(SaveReason.Manual), saved.Status.ToString(), FrameClock.MonotonicMilliseconds - started);
            if (saved.Status != SaveStatus.Saved)
                return Finish(recorder, "partial", "Manual save did not complete: " + saved.Status + "; " + saved.Error);
        }
        return Finish(recorder, "valid", null);
    }

    public void RejectInput(string reason)
    {
        Finish(KernelServices.Get<IPerformanceRecorder>(), "invalid", reason);
    }

    private bool Finish(IPerformanceRecorder recorder, string status, string? reason)
    {
        _session ??= recorder.Snapshot.SessionId;
        if (reason is not null) _reasons.Add(reason);
        End(recorder);
        Publish(status);
        return status == "valid" && _markedEnd;
    }

    private async Task<(string Status, string Reason)?> WaitForStable(IPerformanceRecorder recorder)
    {
        var frames = KernelServices.Get<IFrameStatistics>();
        var stream = KernelServices.Get<IStreamStatistics>();
        var camera = KernelServices.Get<ICameraRig>();
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        var progressAt = FrameClock.MonotonicMilliseconds;
        var previous = (host.Ready, host.Flow.State, Pending(stream.Statistics), _conditions.StableFrames);
        string? missing = "Awaiting the real recording state.";
        while (true)
        {
            host.CheckActive();
            var snapshot = recorder.Snapshot;
            if (snapshot.State is PerfRecordingState.Off or PerfRecordingState.Stopping or PerfRecordingState.Faulted)
                return ("partial", "Performance recorder is not active: " + snapshot.State);
            var facts = recorder.SamplingFacts;
            var now = FrameClock.MonotonicMilliseconds;
            var stats = stream.Statistics;
            var ready = host.Ready && host.Flow.State == AppState.Playing && camera.ActiveTransit is null && !loading.Active;
            var drained = stats.PendingRequests == 0 && stats.PendingBakes == 0 && stats.PendingPreparations == 0 &&
                stats.PendingMounts == 0 && stats.PendingRetirements == 0;
            var stable = frames.Latest is { } frame && _conditions.Stable(ready, drained, frame.Pipelines, now);
            missing = facts.SessionId != snapshot.SessionId ? "Sampling facts belong to another recording session."
                : _conditions.Missing(facts, now);
            if (ObservePollution(facts, "before-sample") is { } pollution) return ("invalid", "Before sample: " + pollution);
            if (stable && snapshot.State == PerfRecordingState.Recording && missing is null && _conditions.PerformanceReady(facts)) return null;
            var next = (host.Ready, host.Flow.State, Pending(stats), _conditions.StableFrames);
            if (next != previous) { progressAt = now; previous = next; }
            if (now - progressAt > BootTuning.StalledSeconds * 1000)
                return ("partial", missing ?? "Ready/stream/compiler/P0 stability stopped making progress.");
            await _input.NextFrame();
        }
    }

    private static (int, int, int, int, int) Pending(in StreamStatistics stats) =>
        (stats.PendingRequests, stats.PendingBakes, stats.PendingPreparations, stats.PendingMounts, stats.PendingRetirements);

    private async Task SampleIdle(IPerformanceRecorder recorder)
    {
        do
        {
            await _input.NextFrame();
            ObserveSample(recorder);
        } while (FrameClock.MonotonicMilliseconds - _begin < options.SampleSeconds * 1000);
    }

    private void ObserveSample(IPerformanceRecorder recorder)
    {
        if (_reasons.Count > 0) return;
        var facts = recorder.SamplingFacts;
        var now = FrameClock.MonotonicMilliseconds;
        if (_fixedPose is { } fixedPose)
        {
            var observer = KernelServices.Get<IObserverState>();
            var camera = KernelServices.Get<ICameraRig>();
            if (observer.Mode != _fixedMode || camera.Mode != _fixedMode)
                _reasons.Add("The fixed scene changed its requested mode during sample.");
            else
            {
                if (StartupRouteProgress.StartFailure(fixedPose,
                    _fixedMode == ViewMode.Walk ? observer.Feet : camera.Pose.Position, observer.YawDeg) is { } failure)
                    _reasons.Add(failure);
            }
        }
        if (recorder.Snapshot.State != PerfRecordingState.Recording || recorder.Snapshot.SessionId != _session || facts.SessionId != _session)
        {
            _samplePartial = true;
            _reasons.Add("The accepted recording session changed during sample.");
        }
        else if (_conditions.Missing(facts, now) is { } missing)
        {
            _samplePartial = true;
            _reasons.Add(missing);
        }
        else if (!_conditions.PerformanceReady(facts)) _reasons.Add("The matched rendering GPU left P0 during sample.");
        else if (ObservePollution(facts, "during-sample") is { } pollution) _reasons.Add("During sample: " + pollution);
        if (KernelServices.Get<IFrameStatistics>().Latest?.Pipelines is { } pipelines && pipelines != _samplePipelines)
        {
            _pipelineCompilationObserved = true;
            if (!_movingSample) _reasons.Add("Pipeline compilation increased during the sample.");
        }
        _gpuObserved |= facts.OtherProcessesGpuUtilFraction.QueryEndMs > _begin;
    }

    private string? ObservePollution(in PerfSamplingFacts facts, string phase)
    {
        var reject = _conditions.RejectPollution(facts, out var reason);
        if (reason is null) return null;
        var recorded = false;
        foreach (var observation in _pollution)
            if (observation.Phase == phase && observation.Reason == reason) { recorded = true; break; }
        if (!recorded) _pollution.Add(new(phase, reason, facts));
        return reject ? reason : null;
    }

    private readonly record struct PollutionObservation(string Phase, string Reason, PerfSamplingFacts SamplingFacts);
    private sealed record SaveObservation(string Reason, string Status, double DurationMs);

    private void End(IPerformanceRecorder recorder)
    {
        if (!_markedBegin || _markedEnd) return;
        _markedEnd = recorder.TryMark(new(options.SampleSegmentId, PerfMarkerPhase.End));
        _end = FrameClock.MonotonicMilliseconds;
        if (!_markedEnd) _reasons.Add("Recording refused the sample End marker.");
    }

    private string? VerifyFixedStart()
    {
        var boot = Boot.Options;
        var maps = KernelServices.Get<IMapService>();
        var observer = KernelServices.Get<IObserverState>();
        var camera = KernelServices.Get<ICameraRig>();
        var mode = boot.World.ViewMode ?? ViewMode.Walk;
        if (observer.Mode != mode || camera.Mode != mode)
            return "Actual fixed-scene mode differs from the requested mode.";
        var requested = boot.World.Pose is { } pose ? ReviewPoseResolver.Resolve(pose, maps.Context!)
            : maps.ResolveSpawn(maps.Current).ViewPose(mode);
        var actual = mode == ViewMode.Walk ? observer.Feet : camera.Pose.Position;
        if (mode == ViewMode.Walk) requested = requested with { Position = requested.Position - System.Numerics.Vector3.UnitY * WalkRules.StandingEye };
        if (StartupRouteProgress.StartFailure(requested, actual, observer.YawDeg) is { } failure) return failure;
        _fixedPose = requested;
        _fixedMode = mode;
        if (mode == ViewMode.Walk)
        {
            var walk = KernelServices.Get<IWalkController>();
            if (!walk.Active || !walk.Grounded || !walk.CollisionReady || !walk.IsSettled)
                return "Default new-game landing is not settled on its production walk controller.";
        }
        return null;
    }

    private void Publish(string status) => GD.Print("PERF_COLLECTION_RESULT " + JsonSerializer.Serialize(new
    {
        status = !_markedEnd && _markedBegin ? "partial" : status,
        reasons = _reasons, scene = options.SceneId, sessionId = _session,
        functionalSmoke = options.FunctionalSmoke, save = _save,
        pipelineCompilationObserved = _pipelineCompilationObserved, travelSegments = TravelSegments,
        polluted = _pollution.Count > 0 ? true : status == "valid" ? false : (bool?)null, pollution = _pollution,
        samplingFacts = KernelServices.Get<IPerformanceRecorder>().SamplingFacts,
        sampleSegmentId = options.SampleSegmentId, sampleSeconds = _markedEnd ? (_end - _begin) / 1000 : 0,
        coverage = new { sampleBegin = new { observed = _markedBegin ? 1 : 0, required = 1 },
            sampleEnd = new { observed = _markedEnd ? 1 : 0, required = 1 },
            gpu = new { observed = _gpuObserved ? 1 : 0, required = 1 },
            sample = new { observed = _markedEnd ? (_end - _begin) / 1000 : 0, required = options.SampleSeconds } }
    }, ContractJson.Options));

}

using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Perf;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Marks one sample around the production activity and reports its raw observations.</summary>
internal sealed class PerfCollectionDriver(IStageGateHost host, PerfCollectionOptions options)
{
    private readonly StartupSampleInput _input = new(host);
    internal IReadOnlyList<PerfTravelSegment> TravelSegments { get; set; } = [];

    public async Task Run(Func<Task<string?>>? activity = null)
    {
        var recorder = KernelServices.Get<IPerformanceRecorder>();
        // Title NewGame uses its canonical world. Apply the explicit collection flow through its sole owner.
        if (Boot.Options.World.FlowRate is { } flow) KernelServices.Get<IWorldControl>().SetFlow(flow);
        var camera = KernelServices.Get<ICameraRig>();
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        await _input.Wait(() => host.Ready && host.Flow.State == AppState.Playing &&
            camera.ActiveTransit is null && !loading.Active,
            "The production scene did not reach Playing readiness.");
        var session = recorder.Snapshot.SessionId;
        if (!recorder.TryMark(new(options.SampleSegmentId, PerfMarkerPhase.Begin)))
            throw new InvalidOperationException("Recording refused the sample Begin marker.");
        var begin = FrameClock.MonotonicMilliseconds;
        if (activity is not null && await activity() is { } failure)
            throw new InvalidOperationException(failure);
        while (FrameClock.MonotonicMilliseconds - begin < options.SampleSeconds * 1000)
            await _input.NextFrame();
        if (!recorder.TryMark(new(options.SampleSegmentId, PerfMarkerPhase.End)))
            throw new InvalidOperationException("Recording refused the sample End marker.");
        var end = FrameClock.MonotonicMilliseconds;
        SaveObservation? save = null;
        if (options.SaveAfterSample)
        {
            var started = FrameClock.MonotonicMilliseconds;
            var saved = await KernelServices.Get<ISaveService>().SaveNow(SaveReason.Manual);
            save = new(nameof(SaveReason.Manual), saved.Status.ToString(), FrameClock.MonotonicMilliseconds - started);
            if (saved.Status != SaveStatus.Saved)
                throw new InvalidOperationException("Manual save did not complete: " + saved.Status + "; " + saved.Error);
        }
        GD.Print("PERF_COLLECTION_RESULT " + JsonSerializer.Serialize(new
        {
            scene = options.SceneId, sessionId = session, save, travelSegments = TravelSegments,
            samplingFacts = recorder.SamplingFacts,
            sampleSegmentId = options.SampleSegmentId, sampleSeconds = (end - begin) / 1000
        }, ContractJson.Options));
    }

    private sealed record SaveObservation(string Reason, string Status, double DurationMs);
}

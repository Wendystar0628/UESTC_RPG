using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Flow;

public sealed partial class AppFlowService
{
    private ResumeEndpoint? _resumeEndpoint;
    private bool _resumeRequested;

    private async Task PrepareResumeEndpoint()
    {
        if (_resumeEndpoint is not null) { RequirePreparedResume(); return; }
        var slot = _request!.Slot!.Value;
        var context = Context(); var instance = world.Instance;
        var savedFlow = _resumeFlow;
        control.SetFlow(0);
        var frozenHours = world.WorldHours;
        camera.SetPose(_destination);
        IMapArrival arrival;
        if (saves.HasPendingPose)
        {
            // Save owns the pending pose's arrival and restore transaction.
            await playback.WaitForPresentedCoverage();
            await saves.CompleteResume();
            arrival = mapRuntime.Arrival ?? throw new InvalidOperationException("Resume preparation has no arrival.");
        }
        else
        {
            using var readinessWait = veil.ObserveReadinessWait();
            var switched = await view.SetMode(_destinationMode, ModeSwitchVia.Ui);
            switch (switched)
            {
                case ModeSwitchResult.Ok: case ModeSwitchResult.Relocated: break;
                case ModeSwitchResult.Refused: throw new InvalidOperationException("Saved view mode was refused.");
                default: throw new ArgumentOutOfRangeException(nameof(switched));
            }
            _destination = camera.Pose;
            arrival = mapRuntime.Arrive(_destination, _destinationMode, Info(slot)?.Meta?.Space);
            // Save resumption retains its covered world without the ordinary arrival limit.
            await WaitForArrival(arrival);
        }
        RequireHealthyWorld();
        if (!ReferenceEquals(maps.Context, context) || maps.Current != context.Descriptor.Id || world.Instance != instance)
            throw new InvalidOperationException("World or map changed while preparing the resume destination.");
        if (world.Flow.Rate != 0 || world.WorldHours != frozenHours || _resumeFlow != savedFlow)
            throw new InvalidOperationException("Continue preparation changed the frozen world time or saved flow.");
        if (saves.HasPendingPose) throw new InvalidOperationException("Resume preparation left the saved pose pending.");
        _destination = camera.Pose; _destinationMode = camera.Mode;
        RequireArrival(arrival, _destination, _destinationMode);
        _resumeEndpoint = new(slot, context, instance, _destination, _destinationMode, arrival);
    }

    private async Task PreparePlayableDestination()
    {
        using var readinessWait = veil.ObserveReadinessWait();
        var pose = camera.Pose; var mode = camera.Mode;
        var arrival = mapRuntime.Arrival;
        if (arrival is null || !Matches(arrival, pose, mode) || !arrival.Done.IsCompletedSuccessfully)
        {
            arrival = mapRuntime.Arrive(pose, mode);
            await WaitForArrival(arrival);
        }
        RequireHealthyWorld();
        RequireArrival(arrival, pose, mode);
    }

    private async Task PrepareTitleDestination()
    {
        if (TitlePrepared) return;
        using var readinessWait = veil.ObserveReadinessWait();
        await veil.Cover(VeilReason.Cinematic, 0);
        var pose = TitlePose();
        camera.SetPose(pose);
        var arrival = mapRuntime.Arrive(pose, ViewMode.Fly);
        await WaitForArrival(arrival);
        RequireHealthyWorld();
        RequireArrival(arrival, pose, ViewMode.Fly);
        _titleArrival = arrival;
    }

    private void RequirePreparedResume()
    {
        var endpoint = _resumeEndpoint ?? throw new InvalidOperationException("Continue reached handoff without preparing its covered destination.");
        if (_request?.Slot != endpoint.Slot || !ReferenceEquals(maps.Context, endpoint.Context) ||
            maps.Current != endpoint.Context.Descriptor.Id || world.Instance != endpoint.Instance ||
            _destination != endpoint.Pose || _destinationMode != endpoint.Mode || saves.HasPendingPose)
            throw new InvalidOperationException("Prepared resume destination was invalidated before handoff.");
        RequireArrival(endpoint.Arrival, endpoint.Pose, endpoint.Mode);
    }

    private async Task WaitForArrival(IMapArrival arrival)
    {
        try
        {
            await playback.WaitUntil(() =>
            {
                if (!ReferenceEquals(mapRuntime.Arrival, arrival)) throw new OperationCanceledException("Flow arrival was replaced.");
                return arrival.Done.IsCompleted;
            }, double.PositiveInfinity, "Flow destination did not become ready.");
            await arrival.Done;
        }
        catch { arrival.Cancel(); throw; }
    }

    private void RequireArrival(IMapArrival arrival, Pose pose, ViewMode mode)
    {
        if (!ReferenceEquals(mapRuntime.Arrival, arrival) || !Matches(arrival, pose, mode))
            throw new InvalidOperationException("Readiness belongs to a different arrival.");
        if (!arrival.Done.IsCompleted) throw new InvalidOperationException("Arrival is still pending.");
        arrival.Done.GetAwaiter().GetResult();
    }
    private static bool Matches(IMapArrival arrival, Pose pose, ViewMode mode) =>
        arrival.Destination.Mode == mode && SameLocation(arrival.Destination.Pose, pose);
    private static bool SameLocation(Pose a, Pose b) => a.Position == b.Position &&
        Math.Abs(CinematicPoseMath.AngleDelta(a.YawDeg, b.YawDeg)) < 0.0001f && a.PitchDeg == b.PitchDeg;
    private sealed record ResumeEndpoint(SlotId Slot, IMapContext Context, long Instance, Pose Pose,
        ViewMode Mode, IMapArrival Arrival);
}

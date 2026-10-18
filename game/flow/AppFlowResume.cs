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
        // Covers both the real opaque draw and the destination camera update.
        // Numeric veil opacity alone cannot authorize visible pipeline samples.
        if (saves.HasPendingPose)
        {
            // WP-50 is the sole prefetch/warm/restore owner for a pending save pose.
            await playback.WaitForPresentedCoverage();
            await saves.CompleteResume();
        }
        else
        {
            using var readinessWait = veil.ObserveReadinessWait();
            // Returning from this same live world has no pending save pose. Prepare
            // that authored destination once without reloading or changing Instance.
            var switched = await view.SetMode(_destinationMode, ModeSwitchVia.Ui);
            switch (switched)
            {
                case ModeSwitchResult.Ok: case ModeSwitchResult.Relocated: break;
                case ModeSwitchResult.Refused: throw new InvalidOperationException("Saved view mode was refused.");
                default: throw new ArgumentOutOfRangeException(nameof(switched));
            }
            _destination = camera.Pose;
            mapRuntime.Prefetch(_destination, _destinationMode, Info(slot)?.Meta?.Space);
            await playback.WaitForPresentedCoverage();
            mapRuntime.BeginWarm();
            // Save resumption is the ENG-PERF read/restore barrier exception:
            // keep the cover until ready without the ordinary arrival time limit.
            await playback.WaitUntil(() => DestinationReady(_destination, _destinationMode), double.PositiveInfinity,
                "Resume destination did not become resident, warm and collision-ready.");
        }
        RequireHealthyWorld();
        if (!ReferenceEquals(maps.Context, context) || maps.Current != context.Descriptor.Id || world.Instance != instance)
            throw new InvalidOperationException("World or map changed while preparing the resume destination.");
        if (world.Flow.Rate != 0 || world.WorldHours != frozenHours || _resumeFlow != savedFlow)
            throw new InvalidOperationException("Continue preparation changed the frozen world time or saved flow.");
        if (saves.HasPendingPose) throw new InvalidOperationException("Resume preparation left the saved pose pending.");
        _destination = camera.Pose; _destinationMode = camera.Mode;
        var prefetched = streamState.Destination
            ?? throw new InvalidOperationException("Resume preparation has no destination identity.");
        if (prefetched.Mode != _destinationMode || !SameLocation(prefetched.Pose, _destination) || !DestinationReady(_destination, _destinationMode))
            throw new InvalidOperationException("Resume readiness belongs to a different destination.");
        _resumeEndpoint = new(slot, context, instance, _destination, _destinationMode, prefetched);
    }

    private async Task PreparePlayableDestination()
    {
        using var readinessWait = veil.ObserveReadinessWait();
        var pose = camera.Pose; var mode = camera.Mode;
        var destination = streamState.Destination;
        if (destination is null || destination.Mode != mode || !SameLocation(destination.Pose, pose) || !DestinationReady(pose, mode))
        {
            mapRuntime.Prefetch(pose, mode);
            await playback.WaitForPresentedCoverage();
            mapRuntime.BeginWarm();
            await playback.WaitUntil(() => DestinationReady(pose, mode), double.PositiveInfinity,
                "New-game destination did not become resident, warm and collision-ready.");
        }
        RequireHealthyWorld();
        if (streamState.Destination is not { } prepared || prepared.Mode != mode || !SameLocation(prepared.Pose, pose))
            throw new InvalidOperationException("Playable readiness belongs to a different destination.");
    }

    private async Task PrepareTitleDestination()
    {
        if (_titlePrepared) return;
        using var readinessWait = veil.ObserveReadinessWait();
        await veil.Cover(VeilReason.Cinematic, 0);
        var pose = TitlePose();
        camera.SetPose(pose);
        mapRuntime.Prefetch(pose, ViewMode.Fly);
        await playback.WaitForPresentedCoverage();
        mapRuntime.BeginWarm();
        await playback.WaitUntil(() => DestinationReady(pose, ViewMode.Fly), double.PositiveInfinity,
            "Title destination did not become resident and warm.");
        RequireHealthyWorld();
        _titlePrepared = true;
    }

    private void RequirePreparedResume()
    {
        var endpoint = _resumeEndpoint ?? throw new InvalidOperationException("Continue reached handoff without preparing its covered destination.");
        if (_request?.Slot != endpoint.Slot || !ReferenceEquals(maps.Context, endpoint.Context) ||
            maps.Current != endpoint.Context.Descriptor.Id || world.Instance != endpoint.Instance ||
            _destination != endpoint.Pose || _destinationMode != endpoint.Mode ||
            streamState.Destination != endpoint.Prefetched || saves.HasPendingPose || !DestinationReady(endpoint.Pose, endpoint.Mode))
            throw new InvalidOperationException("Prepared resume destination was invalidated before handoff.");
    }
    private bool DestinationReady(Pose pose, ViewMode mode)
    {
        if (mapRuntime.Error is { } error) throw new InvalidOperationException(error);
        if (collision.Error is { } collisionError) throw new InvalidOperationException(collisionError);
        if (!mapRuntime.ResidentReady || !mapRuntime.DestinationReady || !mapRuntime.WarmReady) return false;
        return mode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => collision.Ready(pose.Position, 2),
            _ => throw new InvalidOperationException("Unknown saved view mode."),
        };
    }
    private static bool SameLocation(Pose a, Pose b) => a.Position == b.Position &&
        Math.Abs(CinematicPoseMath.AngleDelta(a.YawDeg, b.YawDeg)) < 0.0001f && a.PitchDeg == b.PitchDeg;
    private sealed record ResumeEndpoint(SlotId Slot, IMapContext Context, long Instance, Pose Pose,
        ViewMode Mode, MapStreamDestination Prefetched);
}

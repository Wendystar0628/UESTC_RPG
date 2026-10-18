using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Godot;
using N = System.Numerics;

namespace Campus.Camera;

/// <summary>
/// Veil teleport (SYS-ATLAS-TRAVEL): cover, move, wait for destination readiness, reveal.
/// The camera resolves this driver after registration (parameterless constructor), so the camera
/// must never be handed this driver through constructor injection.
/// </summary>
[KernelService(typeof(ITransitDriver), Owner = "WP-36")]
public sealed class TransitDriver(ICameraRig camera, IVeilService veil, IMapRuntime runtime,
    ICinematicPlayback waits, IWalkController? walk = null) : ITransitDriver
{
    private TransitRun? _active;

    public bool IsReady => true;
    public Pose? Destination => _active is { Finished: false } active ? active.To : null;

    public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null)
    {
        // Slide and arc flights are not implemented; an automatic transit also takes the veil jump.
        TransitKind kind = (options?.Prefer ?? TransitPreference.Auto) switch
        {
            TransitPreference.Auto => TransitKind.Veil,
            TransitPreference.Teleport => TransitKind.Teleport,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        _active?.Cancel();
        var run = new TransitRun(kind, to, options?.LandOn);
        _active = run;
        _ = Execute(run);
        return run;
    }

    private async Task Execute(TransitRun run)
    {
        var reason = VeilReason.Boot;
        var covered = false;
        try
        {
            reason = run.Kind switch
            {
                TransitKind.Teleport => VeilReason.Teleport,
                TransitKind.Veil => VeilReason.Transit,
                TransitKind.Slide or TransitKind.Arc => throw new NotSupportedException("Slide and arc transit are not implemented."),
                _ => throw new ArgumentOutOfRangeException(nameof(run))
            };
            var body = camera.Mode == ViewMode.Walk && walk is { Active: true } ? walk : null;
            body?.SetInput(default);

            await veil.Cover(reason, TransitTuning.CoverSeconds);
            covered = true;
            run.Progress = 0.25f;
            if (run.Cancelled) return;
            // The move must follow a presented opaque frame, not just a veil opacity of one.
            await waits.WaitForPresentedCoverage();
            if (run.Cancelled) return;

            Pose arrival;
            if (body is not null)
            {
                arrival = new Pose(run.LandOn ?? run.To.Position, run.To.YawDeg, 0);
                body.Warp(arrival);
                camera.SetPose(body.EyePose);
            }
            else
            {
                arrival = run.To;
                camera.SetPose(arrival);
            }
            run.Progress = 0.5f;
            var destination = runtime.Arrive(arrival, body is not null ? ViewMode.Walk : ViewMode.Fly);
            run.Arrival = destination;

            using (veil.ObserveReadinessWait())
            {
                await waits.WaitUntil(() => run.Cancelled || destination.Done.IsCompleted, double.PositiveInfinity,
                    "Transit destination did not become resident, warm and collision-ready.");
                if (run.Cancelled) return;
                await destination.Done;
                RequireCurrentArrival(destination);
                var frames = 0;
                await waits.WaitUntil(() => run.Cancelled || ++frames > TransitTuning.MinSettleFrames,
                    double.PositiveInfinity, "Transit settle frames.");
                if (run.Cancelled) return;
                if (body is not null)
                {
                    try
                    {
                        await waits.WaitUntil(() => run.Cancelled || !body.Active || body.CollisionReady && body.Grounded,
                            TransitTuning.WalkSettleSeconds, "Transit walking feet did not settle.");
                    }
                    catch (TimeoutException) { }
                    if (run.Cancelled) return;
                }
            }

            RequireCurrentArrival(destination);
            if (body is not null) camera.SetPose(body.EyePose);
            run.Progress = 0.75f;
            // Another veil owner (map travel, quality) has taken over; leave the veil to it.
            if (veil.Reason == reason)
            {
                // A newer veil request replaces this reveal; the observer has still arrived.
                try { await veil.Reveal(reason, TransitTuning.RevealSeconds); }
                catch (OperationCanceledException) { }
            }
            if (run.Cancelled) return;
            run.Progress = 1;
            run.Finish(TransitResult.Arrived);
        }
        catch (Exception error)
        {
            if (error is not OperationCanceledException) GD.PushError($"[WP-36] veil teleport failed: {error}");
            run.Finish(TransitResult.Cancelled);
        }
        finally
        {
            if (run.Arrival is { } pending && !pending.Done.IsCompleted) pending.Cancel();
            run.Finish(TransitResult.Cancelled);
            await ReleaseVeil(run, reason, covered);
            if (ReferenceEquals(_active, run)) _active = null;
        }
    }

    // A cancelled or failed run lifts its own veil, unless a newer run or another veil owner holds it.
    private async Task ReleaseVeil(TransitRun run, VeilReason reason, bool covered)
    {
        if (!covered || !ReferenceEquals(_active, run) || veil.Reason != reason || veil.Opacity <= 0) return;
        try { await veil.Reveal(reason, TransitTuning.RevealSeconds); }
        catch (Exception error) { GD.PushError($"[WP-36] veil reveal failed: {error}"); }
    }

    private void RequireCurrentArrival(IMapArrival arrival)
    {
        if (!ReferenceEquals(runtime.Arrival, arrival)) throw new OperationCanceledException("Transit arrival was replaced.");
    }

    private sealed class TransitRun(TransitKind kind, Pose to, N.Vector3? landOn) : ITransitHandle
    {
        private readonly TaskCompletionSource<TransitResult> _done = new();
        public TransitKind Kind => kind;
        public float Progress { get; set; }
        public Task<TransitResult> Done => _done.Task;
        public Pose To => to;
        public N.Vector3? LandOn => landOn;
        public bool Cancelled { get; private set; }
        public bool Finished => _done.Task.IsCompleted;
        public IMapArrival? Arrival { get; set; }

        public void Cancel()
        {
            if (_done.Task.IsCompleted) return;
            Cancelled = true;
            Arrival?.Cancel();
            _done.TrySetResult(TransitResult.Cancelled);
        }

        public void Finish(TransitResult result) => _done.TrySetResult(result);
    }
}

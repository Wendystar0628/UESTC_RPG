using Campus.Contracts;
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
    IMapService maps, ITerrainPreparation terrain, ICameraPoseValidator validator,
    IWalkController? walk = null) : ITransitDriver
{
    private TransitRun? _active;

    public bool IsReady => true;
    public Pose? Destination => _active is { Finished: false, TerrainReady: true } active ? active.To : null;

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
        IVeilLease? cover = null;
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Transit requires a running scene tree.");
            var reason = run.Kind switch
            {
                TransitKind.Teleport => VeilReason.Teleport,
                TransitKind.Veil => VeilReason.Transit,
                TransitKind.Slide or TransitKind.Arc => throw new NotSupportedException("Slide and arc transit are not implemented."),
                _ => throw new ArgumentOutOfRangeException(nameof(run))
            };
            var body = camera.Mode == ViewMode.Walk && walk is { Active: true } ? walk : null;
            body?.SetInput(default);

            cover = veil.Cover(reason, TransitTuning.CoverSeconds);
            run.Cover = cover;
            await cover.Covered;
            run.Progress = 0.25f;
            if (run.Cancelled) return;
            // The move must follow a presented opaque frame, not just a veil opacity of one.
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (run.Cancelled) return;

            var context = maps.Context ?? throw new InvalidOperationException("Transit has no mounted map.");
            var target = new N.Vector2(run.To.Position.X, run.To.Position.Z);
            await terrain.PrepareRegion(context, new(target, target));
            if (run.LandOn is { } land)
            {
                var feet = new N.Vector2(land.X, land.Z);
                await terrain.PrepareRegion(context, new(feet, feet));
            }
            if (run.Cancelled) return;
            if (!ReferenceEquals(maps.Context, context)) throw new OperationCanceledException("Transit map was replaced.");
            if (!validator.IsLegal(run.To, false)) throw new ArgumentOutOfRangeException(nameof(run), "Transit destination is outside the reachable volume.");
            run.TerrainReady = true;

            Pose arrival;
            if (body is not null)
            {
                arrival = new Pose(run.LandOn ?? run.To.Position, run.To.YawDeg, 0);
                body.Warp(arrival);
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
                await Task.WhenAny(destination.Done, run.Done);
                if (run.Cancelled) return;
                await destination.Done;
                RequireCurrentArrival(destination);
                for (var frame = 0; frame < TransitTuning.MinSettleFrames; frame++)
                {
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    if (run.Cancelled) return;
                }
                if (body is not null)
                {
                    var elapsed = 0d;
                    while (body.Active && !body.ExitRequested && !(body.CollisionReady && body.Grounded) &&
                        elapsed < TransitTuning.WalkSettleSeconds)
                    {
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                        if (run.Cancelled) return;
                        elapsed += tree.Root.GetProcessDeltaTime();
                    }
                }
            }

            RequireCurrentArrival(destination);
            run.Progress = 0.75f;
            // Releasing this run leaves all other veil owners in place.
            try { await cover.Reveal(TransitTuning.RevealSeconds); }
            catch (OperationCanceledException) { }
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
            cover?.Dispose();
            run.Cover = null;
            if (ReferenceEquals(_active, run)) _active = null;
        }
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
        public bool TerrainReady { get; set; }
        public bool Finished => _done.Task.IsCompleted;
        public IMapArrival? Arrival { get; set; }
        public IVeilLease? Cover { get; set; }

        public void Cancel()
        {
            if (_done.Task.IsCompleted) return;
            Cancelled = true;
            Arrival?.Cancel();
            Cover?.Dispose();
            _done.TrySetResult(TransitResult.Cancelled);
        }

        public void Finish(TransitResult result) => _done.TrySetResult(result);
    }
}

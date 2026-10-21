using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;
using N = System.Numerics;

namespace Campus.Camera;

/// <summary>
/// Loading-screen teleport (SYS-ATLAS-TRAVEL): show, move, wait for destination readiness, hide.
/// The camera resolves this driver after registration (parameterless constructor), so the camera
/// must never be handed this driver through constructor injection.
/// </summary>
[KernelService(typeof(ITransitDriver))]
public sealed class TransitDriver(ICameraRig camera, IAppLoadingPresentation loading, IMapRuntime runtime,
    IMapService maps, ITerrainPreparation terrain, ICameraPoseValidator validator,
    IWalkController? walk = null) : ITransitDriver
{
    private TransitRun? _active;

    public bool IsReady => true;
    public Pose? Destination => _active is { Finished: false } active
        ? active.Arrival?.Destination.Pose ?? (active.TerrainReady ? active.To : null) : null;

    public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null)
    {
        var kind = (options?.Prefer ?? TransitPreference.Auto) switch
        {
            TransitPreference.Auto or TransitPreference.Teleport => TransitKind.Teleport,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        _active?.Cancel();
        var run = new TransitRun(kind, to, options?.LandOn);
        _active = run;
        _ = Execute(run);
        return run;
    }

    public ITransitHandle SwitchMode(Func<CancellationToken, Task<ModeSwitchResult>> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        _active?.Cancel();
        var run = new TransitRun(TransitKind.Teleport, camera.Pose, null) { ApplyMode = apply };
        _active = run;
        _ = Execute(run);
        return run;
    }

    private async Task Execute(TransitRun run)
    {
        IAppLoadingLease? screen = null;
        var result = TransitResult.Cancelled;
        List<Exception>? failures = null;
        void CollectFailure([FailureHandler("UI-NAV-WAYPOINTS")] Exception error) => (failures ??= []).Add(error);
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Transit requires a running scene tree.");
            var body = camera.Mode == ViewMode.Walk && walk is { Active: true } ? walk : null;
            body?.SetInput(default);

            screen = loading.Show();
            await screen.Covered.WaitAsync(run.Token);
            run.Progress = 0.25f;
            if (run.Cancelled) return;

            IMapArrival? destination;
            if (run.ApplyMode is { } apply)
            {
                // The camera owns mode and presentation writes. The driver retains the same
                // covered-screen, arrival, cancellation and failure lifecycle for every entry.
                var applied = await apply(run.Token);
                run.Token.ThrowIfCancellationRequested();
                destination = applied switch
                {
                    ModeSwitchResult.Ok or ModeSwitchResult.Relocated => camera.Mode switch
                    {
                        ViewMode.Walk => walk?.PlacementArrival ?? throw new InvalidOperationException("Mode entry has no walking arrival."),
                        ViewMode.Fly => runtime.Arrive(camera.Pose, ViewMode.Fly),
                        _ => throw new InvalidOperationException("Unknown view mode.")
                    },
                    ModeSwitchResult.Refused => null,
                    _ => throw new ArgumentOutOfRangeException(nameof(applied))
                };
            }
            else
            {
                if (!await PrepareDestinationTerrain(run)) return;
                destination = await PlaceDestination(tree, body, run);
            }
            run.Progress = 0.5f;
            run.Arrival = destination;
            if (destination is not null)
            {
                await destination.Done.WaitAsync(run.Token);
                RequireCurrentArrival(destination);
                if (body is not null && run.ApplyMode is null && !await WaitForWalkSettle(tree, body, run)) return;
                RequireCurrentArrival(destination);
            }
            run.Progress = 0.75f;
            screen.Report(1, 1);
            await screen.Hide().WaitAsync(run.Token);
            if (run.Cancelled) return;
            run.Progress = 1;
            result = TransitResult.Arrived;
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { CollectFailure(error); }
        finally
        {
            try { if (run.Arrival is { } pending && !pending.Done.IsCompleted) pending.Cancel(); }
            catch (Exception error) { CollectFailure(error); }
            try { if (screen is not null) await screen.DisposeAsync(); }
            catch (OperationCanceledException) { result = TransitResult.Cancelled; }
            catch (Exception error) { CollectFailure(error); }
            if (ReferenceEquals(_active, run)) _active = null;
            var failure = failures is null ? null : failures.Count == 1 ? failures[0] : new AggregateException(failures);
            run.Finish(result, failure);
        }
    }

    private async Task<IMapArrival> PlaceDestination(SceneTree tree, IWalkController? body, TransitRun run)
    {
        if (body is null)
        {
            camera.SetPose(run.To);
            return runtime.Arrive(run.To, ViewMode.Fly);
        }
        body.Warp(new Pose(run.LandOn ?? run.To.Position, run.To.YawDeg, 0));
        while (!body.CollisionReady && !body.ExitRequested)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            run.Token.ThrowIfCancellationRequested();
        }
        return body.PlacementArrival ?? throw new InvalidOperationException("Transit could not place its walking destination.");
    }

    private async Task<bool> PrepareDestinationTerrain(TransitRun run)
    {
        var context = maps.Context ?? throw new InvalidOperationException("Transit has no mounted map.");
        _ = await validator.LegalizePrepared(run.To, run.Token);
        if (!validator.IsLegal(run.To, false)) throw new ArgumentOutOfRangeException(nameof(run), "Transit destination is outside the reachable volume.");
        var target = new N.Vector2(run.To.Position.X, run.To.Position.Z);
        await terrain.PrepareRegion(context, new(target, target));
        if (run.LandOn is { } land)
        {
            var standing = new Pose(land + N.Vector3.UnitY * WalkRules.StandingEye, run.To.YawDeg, 0);
            _ = await validator.LegalizePrepared(standing, run.Token);
            if (!validator.IsLegal(standing, false)) throw new ArgumentOutOfRangeException(nameof(run), "Transit stand is outside the reachable volume.");
            var feet = new N.Vector2(land.X, land.Z);
            await terrain.PrepareRegion(context, new(feet, feet));
        }
        if (run.Cancelled) return false;
        if (!ReferenceEquals(maps.Context, context)) throw new OperationCanceledException("Transit map was replaced.");
        run.TerrainReady = true;
        return true;
    }

    private static async Task<bool> WaitForWalkSettle(SceneTree tree, IWalkController body, TransitRun run)
    {
        var elapsed = 0d;
        while (body.Active && !body.ExitRequested && !(body.CollisionReady && body.Grounded) &&
            elapsed < TransitTuning.WalkSettleSeconds)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (run.Cancelled) return false;
            elapsed += tree.Root.GetProcessDeltaTime();
        }
        return true;
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
        public Func<CancellationToken, Task<ModeSwitchResult>>? ApplyMode { get; init; }
        private readonly CancellationTokenSource _cancellation = new();
        public CancellationToken Token => _cancellation.Token;

        public void Cancel()
        {
            if (_done.Task.IsCompleted) return;
            Cancelled = true;
            Arrival?.Cancel();
            _cancellation.Cancel();
            _done.TrySetResult(TransitResult.Cancelled);
        }

        public void Finish(TransitResult result, [FailureHandler("UI-NAV-WAYPOINTS")] Exception? failure = null)
        {
            if (failure is not null) _done.TrySetException(failure);
            else _done.TrySetResult(result);
        }
    }
}

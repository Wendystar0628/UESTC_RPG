using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Diagnostics;
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
    IWalkController? walk = null) : ITransitDriver, IAsyncDisposable
{
    private TransitRun? _active;
    private readonly List<TransitRun> _executions = [];
    private Task? _drain;
    private bool _disposed;

    public bool IsReady => !_disposed;
    public ObserverSaveSnapshot? SaveSnapshot => _active is { Finished: false } active ? active.SaveSnapshot : null;

    public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null)
    {
        var kind = (options?.Prefer ?? TransitPreference.Auto) switch
        {
            TransitPreference.Auto or TransitPreference.Teleport => TransitKind.Teleport,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        var run = new TransitRun(kind, to, options?.LandOn);
        return Begin(run);
    }

    public ITransitHandle SwitchMode(Func<CancellationToken, Task<ModeSwitchResult>> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var run = new TransitRun(TransitKind.Teleport, camera.Pose, null) { ApplyMode = apply };
        return Begin(run);
    }

    private ITransitHandle Begin(TransitRun run)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_drain is not null) throw new InvalidOperationException("Transit execution is draining.");
        // This is the request boundary, before any display or placement can move. Preserve
        // the current accepted state once, including normal movement since the previous request.
        run.SaveSnapshot = camera.CaptureSettledObserver(maps.Current);
        _active?.Cancel();
        _executions.RemoveAll(execution => execution.Execution.IsCompleted && execution.Done.IsCompleted);
        _executions.Add(run);
        _active = run;
        run.Execution = Execute(run);
        return run;
    }

    public Task CancelAsync()
    {
        if (_drain is not null) return _drain;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _drain = completion.Task;
        _ = Drain(completion);
        return completion.Task;
    }

    private async Task Drain(TaskCompletionSource completion)
    {
        _executions.RemoveAll(execution => execution.Execution.IsCompleted && execution.Done.IsCompleted);
        var runs = _executions.ToArray();
        Exception? failure = null;
        foreach (var run in runs)
            try { run.Cancel(); }
            catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        // Execute finishes only after arrival/loading cleanup and delivers those failures through
        // Done to the caller. Drain owns cancellation and execution failures, not that result again.
        foreach (var task in runs.Select(run => run.Execution))
        {
            try { await task; }
            catch (Exception error) { failure = RetainedFailure.Append(failure, task.Exception ?? error); }
        }
        foreach (var run in runs) _executions.Remove(run);
        _drain = null;
        if (failure is not null) completion.TrySetException(failure);
        else completion.TrySetResult();
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return new(CancelAsync());
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
                if (applied is ModeSwitchResult.Ok or ModeSwitchResult.Relocated)
                    AcceptModeSnapshot(run);
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
                RequireCurrentArrival(run);
                if (body is not null && run.ApplyMode is null && !await WaitForWalkSettle(tree, body, run)) return;
                RequireCurrentArrival(run);
            }
            run.Progress = 0.75f;
            screen.Report(1, 1);
            await screen.Hide().WaitAsync(run.Token);
            if (run.Cancelled) return;
            run.Progress = 1;
            result = TransitResult.Arrived;
        }
        catch (OperationCanceledException) when (run.Token.IsCancellationRequested ||
            (run.Arrival is { } arrival && arrival.Done.IsCanceled && !ReferenceEquals(runtime.Arrival, arrival)))
        {
            run.Cancel();
        }
        catch (Exception error) { CollectFailure(error); }
        finally
        {
            try { if (run.Arrival is { } pending && !pending.Done.IsCompleted) pending.Cancel(); }
            catch (Exception error) { CollectFailure(error); }
            try { if (screen is not null) await screen.DisposeAsync(); }
            catch (Exception error) { CollectFailure(error); }
            if (ReferenceEquals(_active, run)) _active = null;
            var failure = failures is null ? null : failures.Count == 1 ? failures[0] : new AggregateException(failures);
            run.Finish(result, failure);
        }
    }

    private void AcceptModeSnapshot(TransitRun run)
    {
        var map = maps.Context ?? throw new InvalidOperationException("Mode entry has no mounted map.");
        int speed = camera.Mode switch
        {
            ViewMode.Fly => run.SaveSnapshot.View.FlySpeedStep,
            ViewMode.Walk => walk?.SpeedStep ?? throw new InvalidOperationException("Mode entry has no walking owner."),
            _ => throw new InvalidOperationException("Unknown view mode.")
        };
        run.SaveSnapshot = ObserverViewSave.Capture(camera, map.Descriptor.Id, speed,
            run.SaveSnapshot.View.FlySpeedStep, walk, map);
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
            await FrameAwaiter.ProcessAsync(tree.Root, run.Token);
            run.Token.ThrowIfCancellationRequested();
        }
        return body.PlacementArrival ?? throw new InvalidOperationException("Transit could not place its walking destination.");
    }

    private async Task<bool> PrepareDestinationTerrain(TransitRun run)
    {
        var context = maps.Context ?? throw new InvalidOperationException("Transit has no mounted map.");
        _ = await validator.LegalizePrepared(run.To, run.Token);
        run.Token.ThrowIfCancellationRequested();
        if (!validator.IsLegal(run.To, false)) throw new ArgumentOutOfRangeException(nameof(run), "Transit destination is outside the reachable volume.");
        var target = new N.Vector2(run.To.Position.X, run.To.Position.Z);
        await terrain.PrepareRegion(context, new(target, target), run.Token);
        run.Token.ThrowIfCancellationRequested();
        if (run.LandOn is { } land)
        {
            var standing = new Pose(land + N.Vector3.UnitY * WalkRules.StandingEye, run.To.YawDeg, 0);
            _ = await validator.LegalizePrepared(standing, run.Token);
            run.Token.ThrowIfCancellationRequested();
            if (!validator.IsLegal(standing, false)) throw new ArgumentOutOfRangeException(nameof(run), "Transit stand is outside the reachable volume.");
            var feet = new N.Vector2(land.X, land.Z);
            await terrain.PrepareRegion(context, new(feet, feet), run.Token);
        }
        if (run.Cancelled) return false;
        if (!ReferenceEquals(maps.Context, context))
        {
            run.Cancel();
            return false;
        }
        var walking = run.SaveSnapshot.Walk;
        if (camera.Mode == ViewMode.Walk)
        {
            if (walk is null) throw new InvalidOperationException("Walking transit requires its state owner.");
            walking = walk.CaptureSettled(new(run.LandOn ?? run.To.Position, run.To.YawDeg, 0));
        }
        var pose = camera.Mode == ViewMode.Walk && walking is not null ? walking.EyePose() : run.To with { FovDeg = null };
        var view = run.SaveSnapshot.View with { Mode = camera.Mode, Pose = pose, Space = ObserverViewSave.SpaceAt(context, pose) };
        // Acceptance is atomic and happens only after every spatial query can use prepared data.
        run.SaveSnapshot = new(context.Descriptor.Id, view, walking);
        return true;
    }

    private static async Task<bool> WaitForWalkSettle(SceneTree tree, IWalkController body, TransitRun run)
    {
        var elapsed = 0d;
        while (body.Active && !body.ExitRequested && !(body.CollisionReady && body.Grounded) &&
            elapsed < TransitTuning.WalkSettleSeconds)
        {
            await FrameAwaiter.ProcessAsync(tree.Root, run.Token);
            if (run.Cancelled) return false;
            elapsed += tree.Root.GetProcessDeltaTime();
        }
        return true;
    }

    private void RequireCurrentArrival(TransitRun run)
    {
        if (!ReferenceEquals(runtime.Arrival, run.Arrival))
        {
            run.Cancel();
            run.Token.ThrowIfCancellationRequested();
        }
    }

    private sealed class TransitRun(TransitKind kind, Pose to, N.Vector3? landOn) : ITransitHandle
    {
        private readonly TaskCompletionSource<TransitResult> _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TransitKind Kind => kind;
        public float Progress { get; set; }
        public Task<TransitResult> Done => _done.Task;
        public Task Execution { get; set; } = Task.CompletedTask;
        public Pose To => to;
        public N.Vector3? LandOn => landOn;
        public bool Cancelled { get; private set; }
        public ObserverSaveSnapshot SaveSnapshot { get; set; } = null!;
        public bool Finished => _done.Task.IsCompleted;
        public IMapArrival? Arrival { get; set; }
        public Func<CancellationToken, Task<ModeSwitchResult>>? ApplyMode { get; init; }
        private readonly CancellationTokenSource _cancellation = new();
        public CancellationToken Token => _cancellation.Token;

        public void Cancel()
        {
            if (_done.Task.IsCompleted) return;
            Cancelled = true;
            _cancellation.Cancel();
        }

        public void Finish(TransitResult result, [FailureHandler("UI-NAV-WAYPOINTS")] Exception? failure = null)
        {
            _cancellation.Dispose();
            if (failure is not null) _done.TrySetException(failure);
            else _done.TrySetResult(Cancelled ? TransitResult.Cancelled : result);
        }
    }
}

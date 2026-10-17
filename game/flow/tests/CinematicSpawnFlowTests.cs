using System.Numerics;
using System.Text.Json;
using Campus.Camera.Cinematic;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Flow;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class CinematicSpawnFlowTests
{
    [TestCase]
    public void CinematicCatalogueReadsTheFrozenMapVersionWithoutResourceFiles()
    {
        var source = new CinematicSpawnMapStub().Context;
        var definition = new CinematicDef(1, "frozen", source.Descriptor.Id, CinematicKind.EnterContinue, 2,
            [new(0, 2, CameraPath.Hold, [new(Anchor: "$current")], new(CameraLookMode.Fixed), [new(0, 60)])],
            [], [], [], [], [], new(new(Anchor: "$current")));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(definition, ContractJson.Options);
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        { ["cinematics/frozen.json"] = bytes });
        Array.Fill(bytes, (byte)0);
        var context = new CinematicSpawnMapStub.MapContext(source.Descriptor, source.World, inputs);
        var loaded = new CinematicCatalog(context).Read("frozen");
        if (loaded.Id != definition.Id || loaded.Map != definition.Map || loaded.Duration != definition.Duration)
            throw new InvalidOperationException("Cinematic data did not come from the frozen map version.");
    }

    [TestCase]
    public async Task TitleAndNewGameResolveDefaultWaypointWithNoInlineSpawns()
    {
        var rules = new Rules(); var world = new TestWorldState(new TestWeatherProcess(), rules);
        var camera = new TestCameraRig(world); var maps = new CinematicSpawnMapStub();
        var veil = new ImmediateVeil(); var saves = new SaveService();
        var presentation = new CinematicTestPresentation();
        var runtime = new CinematicRuntimeTestStub(() => presentation.IsCovered);
        var playback = new CinematicPlayback(camera, veil, rules, presentation);
        var diagnostics = new List<string>();
        var flow = new AppFlowService(camera, veil, saves, maps, world, world, rules, new Motion(), world,
            new TestCollisionWorld(), playback, world, new ExitService(), runtime, runtime)
        { DiagnosticSink = diagnostics.Add };
        if (maps.Context.Descriptor.Spawns.Count != 0) throw new InvalidOperationException("Fixture must use waypoint-only spawns.");
        await flow.PrepareBoot(new()); await flow.PrepareWorld(); await flow.CompleteBoot();
        if (flow.State != AppState.Title || camera.Pose != CinematicSpawnMapStub.DefaultPose)
            throw new InvalidOperationException("Title fallback did not use the catalogue's default waypoint.");
        var entering = flow.Enter(new(EnterKind.NewGame, new("s1")));
        await CinematicTestFrames.Complete(entering, playback, presentation, veil);
        var result = await entering;
        if (diagnostics.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostics));
        if (result != EnterResult.Entered || flow.State != AppState.Playing || camera.Pose != CinematicSpawnMapStub.DefaultPose || saves.NewGames != 1 || saves.Saves != 1)
            throw new InvalidOperationException("New game failed or lost the authored default waypoint.");
        if (maps.DefaultRequests < 2 || camera.InputLocked)
            throw new InvalidOperationException("The flow bypassed spawn resolution or failed to hand off input.");
    }

    [TestCase]
    public void NamedCinematicAnchorUsesCataloguePrecedenceAndRejectsUnknownIds()
    {
        var authored = new Pose(new(12, 18, -25), 45, -10, 65);
        var maps = new CinematicSpawnMapStub([new("named", authored, null)]);
        var anchors = new CinematicAnchorSet(default, default, default, 60, maps, CinematicSpawnMapStub.Map);
        if (anchors.Resolve("$waypoint:named") != authored || maps.NamedRequests != 1)
            throw new InvalidOperationException("Named cinematic lookup bypassed the map catalogue's inline-spawn precedence.");
        try { anchors.Resolve("$waypoint:missing"); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Unknown cinematic waypoint silently fell back to an origin or default.");
    }

    [TestCase]
    public async Task PendingResumeWarmsOnceUnderCoverAndRestoresSavedFlowAtHandoff() => await ResumeCase(pending: true);

    [TestCase]
    public async Task LiveResumeWarmsOnceWithoutReloadingOrChangingInstance() => await ResumeCase(pending: false);

    [TestCase]
    public async Task LiveResumeRetainsItsCoveredWorldBeyondTheOrdinaryArrivalLimit() =>
        await ResumeCase(pending: false, delayDestination: true);

    [TestCase]
    public async Task LiveResumeStillFailsOnARuntimeErrorAfterTheOrdinaryArrivalLimit() =>
        await ResumeCase(pending: false, delayDestination: true, failDestination: true);

    [TestCase]
    public async Task LiveResumeFailsOnAnExplicitCollisionErrorWithoutWaitingForever() =>
        await ResumeCase(pending: false, delayDestination: true, failCollision: true);

    [TestCase]
    public async Task QuitDuringDelayedLiveResumeWaitsForItsReadyEndpointBeforeExiting() =>
        await ResumeCase(pending: false, delayDestination: true, quitDuringWait: true);

    [TestCase]
    public async Task ClearingDelayedLiveResumeCancelsItsWaitAndAllowsEntryFailureToClearAgain() =>
        await ResumeCase(pending: false, delayDestination: true, clearDuringWait: true);

    private static async Task ResumeCase(bool pending, bool delayDestination = false,
        bool failDestination = false, bool quitDuringWait = false, bool clearDuringWait = false, bool failCollision = false)
    {
        var rules = new Rules(); var world = new TestWorldState(new TestWeatherProcess(), rules);
        world.SetFlow(60);
        var camera = new TestCameraRig(world); var maps = new CinematicSpawnMapStub();
        var veil = new ImmediateVeil(); var presentation = new CinematicTestPresentation();
        var runtime = new CinematicRuntimeTestStub(() => presentation.IsCovered)
        { CompletePrefetchImmediately = !delayDestination };
        var playback = new CinematicPlayback(camera, veil, rules, presentation);
        var saves = new ResumeSaveService(camera, world, runtime, () => presentation.IsCovered);
        var diagnostics = new List<string>();
        var exit = new ExitService(quitDuringWait);
        var collision = new TestCollisionWorld();
        var flow = new AppFlowService(camera, veil, saves, maps, world, world, rules, new Motion(), world,
            collision, playback, world, exit, runtime, runtime)
        { DiagnosticSink = diagnostics.Add };
        await flow.PrepareBoot(new(ShowTitle: pending)); await flow.PrepareWorld(); await flow.CompleteBoot();
        if (!pending)
        {
            camera.SetPose(saves.Meta.Pose);
            runtime.CompletePrefetchImmediately = true;
            var leaving = flow.ReturnToTitle();
            await CinematicTestFrames.Complete(leaving, playback, presentation, veil);
            runtime.CompletePrefetchImmediately = !delayDestination;
        }
        var previousPrefetches = runtime.Prefetches; var previousWarms = runtime.Warms;
        var previousObservations = veil.ObservationCount;
        var instance = world.Instance; var hours = world.WorldHours;
        var entering = flow.Enter(new(EnterKind.Continue));
        if (delayDestination)
        {
            for (var frame = 0; frame < 100 && runtime.Warms == previousWarms; frame++)
            {
                presentation.Draw(playback, veil); playback.Tick(1d / 60);
                await Task.Yield();
            }
            if (runtime.Warms != previousWarms + 1 || runtime.Destination is not { } destination)
                throw new InvalidOperationException("The delayed fixture never reached its covered resume barrier.");
            if (veil.ActiveObservations != 1)
                throw new InvalidOperationException("Live resume did not acquire exactly one preparation observation.");
            // Two simulated seconds exceed both the 1.2 s fly and 1.5 s walk arrival limits.
            for (var frame = 0; frame < 120; frame++)
            {
                presentation.Draw(playback, veil); playback.Tick(1d / 60);
                await Task.Yield();
                if (entering.IsCompleted || flow.State != AppState.Entering || !camera.InputLocked ||
                    veil.Opacity != 1 || !presentation.IsCovered || world.Instance != instance ||
                    world.WorldHours != hours || world.Flow.Rate != 0 || runtime.Destination != destination ||
                    runtime.Prefetches != previousPrefetches + 1 || runtime.Warms != previousWarms + 1 || saves.Loads != 0 || exit.Calls != 0)
                    throw new InvalidOperationException("A delayed resume timed out, uncovered, replaced its world or duplicated preparation.");
            }
            if (failDestination || failCollision)
            {
                const string runtimeError = "Delayed resume runtime failure.";
                if (failCollision) collision.Error = runtimeError; else runtime.Error = runtimeError;
                await CinematicTestFrames.Complete(entering, playback, presentation, veil);
                if (await entering != EnterResult.Failed || flow.State != AppState.Title ||
                    !camera.InputLocked || veil.Opacity != 1 || world.Instance != instance ||
                    world.WorldHours != hours || world.Flow.Rate != 0 || diagnostics.Count != 1 ||
                    !diagnostics[0].Contains(runtimeError, StringComparison.Ordinal))
                    throw new InvalidOperationException("An unbounded resume ignored a runtime error or exposed the failed endpoint.");
                if (veil.ActiveObservations != 0)
                    throw new InvalidOperationException("Resume completion retained a readiness observation.");
                return;
            }
            if (clearDuringWait)
            {
                playback.Clear();
                await CinematicTestFrames.Complete(entering, playback, presentation, veil);
                if (await entering != EnterResult.Failed || flow.State != AppState.Title ||
                    !camera.InputLocked || veil.Opacity != 1 || world.Instance != instance ||
                    world.WorldHours != hours || world.Flow.Rate != 0 || diagnostics.Count != 1 ||
                    !diagnostics[0].Contains(nameof(TaskCanceledException), StringComparison.Ordinal))
                    throw new InvalidOperationException("Clearing an unbounded resume hung, exposed its world or duplicated failure handling.");
                if (veil.ActiveObservations != 0)
                    throw new InvalidOperationException("Resume completion retained a readiness observation.");
                return;
            }
            if (quitDuringWait)
            {
                var quitting = flow.Quit();
                for (var frame = 0; frame < 3; frame++)
                {
                    presentation.Draw(playback, veil); playback.Tick(1d / 60);
                    await Task.Yield();
                }
                if (quitting.IsCompleted || entering.IsCompleted || exit.Calls != 0 ||
                    flow.State != AppState.Entering || !camera.InputLocked || veil.Opacity != 1)
                    throw new InvalidOperationException("Quit bypassed the delayed resume endpoint.");
                runtime.CompleteDestination();
                await CinematicTestFrames.Complete(Task.WhenAll(entering, quitting), playback, presentation, veil);
                if (await entering != EnterResult.Entered || flow.State != AppState.Quitting || exit.Calls != 1 ||
                    diagnostics.Count != 0 || !camera.InputLocked || veil.Opacity != 1 ||
                    world.Instance != instance || world.WorldHours != hours || world.Flow.Rate != 60)
                    throw new InvalidOperationException("Quit failed to join the ready resume or lost its saved world state.");
                if (veil.ActiveObservations != 0)
                    throw new InvalidOperationException("Resume completion retained a readiness observation.");
                return;
            }
            runtime.CompleteDestination();
        }
        await CinematicTestFrames.Complete(entering, playback, presentation, veil);
        if (diagnostics.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostics));
        if (await entering != EnterResult.Entered || runtime.Prefetches != previousPrefetches + 1 || runtime.Warms != previousWarms + 1 ||
            saves.Completions != (pending ? 1 : 0) || saves.Loads != (pending ? 1 : 0))
            throw new InvalidOperationException("Resume duplicated or bypassed its covered preparation owner.");
        if (veil.ActiveObservations != 0 || veil.ObservationCount != previousObservations + (pending ? 0 : 1))
            throw new InvalidOperationException("Flow duplicated WP-50's pending-pose observation or leaked its own live observation.");
        if (world.Instance != instance || world.WorldHours != hours || world.Flow.Rate != 60 ||
            camera.Pose != saves.Meta.Pose || camera.InputLocked || saves.HasPendingPose)
            throw new InvalidOperationException("Continue lost the saved flow, world identity, endpoint or input handoff.");
    }

    private sealed class Rules : IRuleset
    {
        public RulesetDescriptor Current { get; } = new(new("test"), [ViewMode.Fly], WorldControlMask.All, "none", [], [AppState.Title, AppState.Entering, AppState.Leaving]);
        public long Rev => 1;
    }
    private sealed class Motion : IMotionSettings
    {
        public bool ReduceMotion => false;
        public event Action? Changed { add { } remove { } }
    }
    private sealed class ImmediateVeil : IVeilService
    {
        public float Opacity { get; private set; }
        public VeilReason Reason { get; private set; }
        public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog) { Reason = reason; Opacity = 1; return Task.CompletedTask; }
        public Task Reveal(VeilReason reason, float seconds) { Reason = reason; Opacity = 0; return Task.CompletedTask; }
        // Test-only lifecycle recording. Timing belongs to VeilTimeline tests, not this fixture.
        public int ActiveObservations;
        public int ObservationCount;
        public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness)
        {
            Interlocked.Increment(ref ActiveObservations);
            Interlocked.Increment(ref ObservationCount);
            return new Observation(() => Interlocked.Decrement(ref ActiveObservations));
        }
        private sealed class Observation(Action release) : IDisposable
        {
            private int _disposed;
            public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) release(); }
        }
        public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds) { }
    }
    private sealed class ExitService(bool allowQuit = false) : IAppExit
    {
        public int Calls { get; private set; }
        public Task FinishQuit()
        {
            if (!allowQuit) throw new InvalidOperationException("This entry regression must never quit.");
            Calls++; return Task.CompletedTask;
        }
    }
    private sealed class SaveService : ISaveService
    {
        public int NewGames { get; private set; }
        public int Saves { get; private set; }
        public bool Enabled => true;
        public IReadOnlyList<SlotInfo> Slots { get; } = [new(new("s1"), SlotKind.Auto, SlotHealth.Empty, null, 0)];
        public SlotId? Continue => null;
        public SlotId? Active { get; private set; }
        public bool Busy => false;
        public SaveState State => new(Active, null, null, null, ReadOnlyReason.None);
        public event Action? SlotsChanged { add { } remove { } }
        public bool HasPendingPose => false;
        public void Request(SaveReason reason) { }
        public Task<SaveResult> SaveNow(SaveReason reason) { Saves++; return Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1)); }
        public Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full) => throw new InvalidOperationException("Unexpected load during new game.");
        public Task CompleteResume() => throw new InvalidOperationException("Unexpected resume during new game.");
        public Task<LoadResult> LoadFile(string path) => throw new InvalidOperationException("Unexpected file load.");
        public Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null) { NewGames++; Active = slot; return Task.FromResult(new LoadResult(LoadStatus.Loaded, slot, 0, [])); }
        public Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => throw new InvalidOperationException("Unexpected quit during entry.");
        public byte[]? Thumbnail(SlotId slot) => null;
    }
    private sealed class ResumeSaveService(ICameraRig camera, IWorldState world, CinematicRuntimeTestStub runtime,
        Func<bool> covered) : ISaveService
    {
        public SaveMeta Meta { get; } = new(1, "test", new("s1"), SlotKind.Auto, 1, 1, 0, CinematicSpawnMapStub.Map,
            17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly, new(new(30, 45, -15), 25, -20), null, null, null, 0, SaveReason.Test);
        public int Loads { get; private set; }
        public int Completions { get; private set; }
        public bool Enabled => true;
        public IReadOnlyList<SlotInfo> Slots => [new(Meta.Slot, SlotKind.Auto, SlotHealth.Ok, Meta, 1)];
        public SlotId? Continue => Meta.Slot;
        public SlotId? Active => Meta.Slot;
        public bool Busy => false;
        public SaveState State => new(Active, null, null, null, ReadOnlyReason.None);
        public event Action? SlotsChanged { add { } remove { } }
        public bool HasPendingPose { get; private set; }
        public void Request(SaveReason reason) { }
        public Task<SaveResult> SaveNow(SaveReason reason) => throw new InvalidOperationException("Continue must not write a new generation.");
        public Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full)
        {
            Loads++; HasPendingPose = scope is LoadScope.Preview;
            return Task.FromResult(new LoadResult(LoadStatus.Loaded, slot, 1, []));
        }
        public Task CompleteResume()
        {
            if (!HasPendingPose || !covered() || world.Flow.Rate != 0) throw new InvalidOperationException("Resume was not prepared under frozen, presented cover.");
            Completions++; runtime.Prefetch(Meta.Pose, Meta.Mode, Meta.Space); runtime.BeginWarm();
            camera.SetPose(Meta.Pose); HasPendingPose = false;
            return Task.CompletedTask;
        }
        public Task<LoadResult> LoadFile(string path) => throw new InvalidOperationException("Unexpected file load.");
        public Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null) => throw new InvalidOperationException("Unexpected new game.");
        public Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1));
        public byte[]? Thumbnail(SlotId slot) => null;
    }
}

// A real catalogue resolver backs this test double. It does not return a fixed pose
// for arbitrary IDs or silently inherit IMapService's NotSupported default method.
internal sealed class CinematicSpawnMapStub : IMapService
{
    public static readonly MapId Map = new("cinematic-spawn-test");
    public static readonly Pose DefaultPose = new(new(120, 35, -70), 20, -35);
    private readonly MapCatalogEntry _entry;
    public int DefaultRequests { get; private set; }
    public int NamedRequests { get; private set; }
    public long Rev => 1;
    public MapId Default => Map;
    public IReadOnlyList<MapDescriptor> Maps { get; }
    public IReadOnlyList<MapVisit> Visits => [];
    public MapId Current => Map;
    public IMapContext Context { get; }
    public IMapTransition? Transition => null;
    public event Action<MapId, MapTravelCause>? Arrived { add { } remove { } }
    public CinematicSpawnMapStub(IReadOnlyList<MapSpawn>? inlineSpawns = null)
    {
        var descriptor = new MapDescriptor(Map, new("测试地图", "Test map"), MapKind.Outdoor, 1, 1,
            new(0, 0, 0, new(-500, -500), new(500, 500)), Vector2.Zero,
            new(new(-500, -50, -500), new(500, 500, 500)), new(new(-512, -512), new(512, 512)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-8, -8), new(7, 7), "start", inlineSpawns ?? [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("默认", "Default"), true, "default"),
            new("sky", "backdrop", []), new Dictionary<string, string>(), []);
        Waypoint[] waypoints =
        [
            new("start", new("起点", "Start"), WaypointGroup.Overview, DefaultPose, null, null, new(17.5, Season.Autumn)),
            new("named", new("另一个点", "Other point"), WaypointGroup.Core, new(new(-30, 80, 50), 90, -25), null, null, new(17.5, Season.Autumn)),
        ];
        // This path intentionally has no files. Entry must not re-read waypoints.json.
        _entry = new(descriptor, "res://cinematic-test-no-files/", "res://cinematic-test-no-files/", waypoints);
        Maps = [descriptor];
        Context = new MapContext(descriptor, new TestWorldModel(),
            BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>()));
    }
    public MapSpawn ResolveSpawn(MapId map, string? spawn = null)
    {
        if (map != Map) throw new ArgumentException($"Unknown map: {map.Value}", nameof(map));
        if (spawn is null) DefaultRequests++; else NamedRequests++;
        return _entry.ResolveSpawn(spawn ?? _entry.Descriptor.DefaultSpawn);
    }
    public IMapTransition Travel(MapTravelRequest request) => throw new InvalidOperationException("The save fixture owns entry; no extra travel is expected.");
    internal sealed record MapContext(MapDescriptor Descriptor, IWorldModel World, BuildInputSnapshot SourceInputs) : IMapContext
    {
        public string DataRoot => "res://cinematic-test-no-files/";
        public string BakeRoot => "res://cinematic-test-no-files/";
    }
}

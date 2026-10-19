using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Flow;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AppLoadingFlowTests
{
    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task TitleAndNewGameResolveDefaultWaypointWithNoInlineSpawns()
    {
        var rig = new FlowTestRig(); var maps = Maps(out var entry, out var requests);
        var saves = new SaveService(rig.Camera);
        var flow = rig.CreateFlow(saves, maps, new TestAppExit(allowQuit: false));
        if (maps.Context!.Descriptor.Spawns.Count != 0) throw new InvalidOperationException("Fixture must use waypoint-only spawns.");
        await flow.PrepareBoot(new()); await flow.PrepareWorld(); await flow.CompleteBoot();
        if (flow.State != AppState.Title || flow.IsPlaying || !rig.Title.Visible || rig.Camera.Pose != entry.ResolveSpawn(entry.Descriptor.DefaultSpawn).Fly)
            throw new InvalidOperationException("Title fallback did not use the catalogue's default waypoint.");
        var spawnPose = maps.ResolveSpawn(maps.Default).ViewPose(ViewMode.Walk);
        if (rig.Runtime.PinnedDestinations is not [{ Mode: ViewMode.Walk } prefetched] || prefetched.Pose != spawnPose ||
            rig.Runtime.Arrivals != 0 || saves.NewGames != 0 || saves.Active is not null)
            throw new InvalidOperationException("Title spawn prefetch changed the world or requested a real arrival.");
        var result = await flow.Enter(new(EnterKind.NewGame, new("s1")));
        if (rig.Diagnostics.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, rig.Diagnostics));
        if (result != EnterResult.Entered || !flow.IsPlaying || saves.NewGames != 1 || saves.Saves != 1)
            throw new InvalidOperationException("New game failed or lost its first saved generation.");
        if (rig.Camera.Mode != ViewMode.Walk || rig.Camera.Pose.Position != SaveService.Landing.Position ||
            rig.Runtime.Arrival?.Destination is not { Mode: ViewMode.Walk } landing || landing.Pose.Position != SaveService.Landing.Position)
            throw new InvalidOperationException("New game did not hand control back walking on the restored spawn stand.");
        if (requests.Count(spawn => spawn is null) < 1 || rig.Camera.InputLocked || rig.Loading.Active || rig.Loading.Progress != 1 || rig.Title.Visible ||
            rig.Runtime.PinnedDestinations.Count != 0)
            throw new InvalidOperationException("The flow bypassed spawn resolution or failed to finish loading.");
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task PendingResumeArrivesOnceUnderLoadingAndRestoresSavedFlowAtHandoff() => await ResumeCase(pending: true);

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task LiveResumeArrivesOnceWithoutReloadingOrChangingInstance() => await ResumeCase(pending: false);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task LiveResumeWaitsForItsRealDestination() => await ResumeCase(pending: false, delayDestination: true);

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task QuitDuringDelayedLiveResumeWaitsForItsReadyEndpointBeforeExiting() =>
        await ResumeCase(pending: false, delayDestination: true, quitDuringWait: true);

    private static async Task ResumeCase(bool pending, bool delayDestination = false, bool quitDuringWait = false)
    {
        var rig = new FlowTestRig(); var world = rig.World; var camera = rig.Camera; var runtime = rig.Runtime;
        const int savedFlow = 60;
        world.SetFlow(savedFlow);
        var maps = Maps(out _, out _);
        var saves = new ResumeSaveService(camera, world, runtime, maps.Current, () => rig.Loading.Active);
        var exit = new TestAppExit(quitDuringWait);
        var flow = rig.CreateFlow(saves, maps, exit);
        await flow.PrepareBoot(new(ShowTitle: pending)); await flow.PrepareWorld(); await flow.CompleteBoot();
        if (world.BaseFlow.Rate != savedFlow || pending && world.Flow.Rate != 0 || rig.Title.Visible != pending)
            throw new InvalidOperationException("Title preparation changed the saved base flow instead of holding it.");
        if (!pending)
        {
            camera.SetPose(saves.Meta.Pose);
            await flow.ReturnToTitle();
            if (world.BaseFlow.Rate != savedFlow || world.Flow.Rate != 0 || !rig.Title.Visible)
                throw new InvalidOperationException("Returning to title changed the saved base flow instead of holding it.");
        }
        var titlePose = camera.Pose; var titleMode = camera.Mode; var titleArrival = runtime.Arrival;
        var titleInstance = world.Instance; var titleHours = world.WorldHours; var active = saves.Active;
        var pendingPose = saves.HasPendingPose;
        saves.UpdateEndpoint(saves.Meta.Pose with { YawDeg = saves.Meta.Pose.YawDeg + 1 });
        if (runtime.PinnedDestinations.Count != 2 ||
            !runtime.PinnedDestinations.Contains(new(saves.Meta.Pose, saves.Meta.Mode, saves.Meta.Space)) ||
            camera.Pose != titlePose || camera.Mode != titleMode || !ReferenceEquals(runtime.Arrival, titleArrival) ||
            world.Instance != titleInstance || world.WorldHours != titleHours || saves.Active != active ||
            saves.HasPendingPose != pendingPose || saves.Completions != 0 || saves.Loads != (pending ? 1 : 0))
            throw new InvalidOperationException("Title endpoint refresh restored a pose, changed the world, or replaced readiness.");
        runtime.CompleteArrivalImmediately = !delayDestination;
        var previousArrivals = runtime.Arrivals;
        var instance = world.Instance; var hours = world.WorldHours;
        var entering = flow.Enter(new(EnterKind.Continue));
        if (delayDestination)
        {
            if (runtime.Arrivals != previousArrivals + 1 || runtime.Arrival?.Destination is not { } destination)
                throw new InvalidOperationException("Resume never reached its destination barrier.");
            await Task.Yield();
            if (entering.IsCompleted || flow.State != AppState.Entering || !camera.InputLocked ||
                !rig.Loading.Active || rig.Loading.Progress >= 1 || world.Instance != instance ||
                world.WorldHours != hours || world.Flow.Rate != 0 || world.BaseFlow.Rate != savedFlow ||
                runtime.Arrival?.Destination != destination || runtime.Arrivals != previousArrivals + 1 || saves.Loads != 0)
                throw new InvalidOperationException("Resume exposed or duplicated its pending destination.");
            if (runtime.PinnedDestinations.Count == 0)
                throw new InvalidOperationException("Title released prefetch residency before the real destination was ready.");
            Task? quitting = null;
            if (quitDuringWait)
            {
                quitting = flow.Quit();
                if (quitting.IsCompleted || exit.Calls != 0 || entering.IsCompleted)
                    throw new InvalidOperationException("Quit bypassed the pending resume endpoint.");
            }
            runtime.CompleteArrival();
            if (quitting is not null)
            {
                await Task.WhenAll(entering, quitting);
                if (await entering != EnterResult.Entered || flow.State != AppState.Quitting || exit.Calls != 1 ||
                    !camera.InputLocked || !rig.Loading.Active || world.Flow.Rate != savedFlow)
                    throw new InvalidOperationException("Quit failed to join the ready resume.");
                return;
            }
        }
        if (await entering != EnterResult.Entered || runtime.Arrivals != previousArrivals + 1 ||
            saves.Completions != (pending ? 1 : 0) || saves.Loads != (pending ? 1 : 0))
            throw new InvalidOperationException("Resume duplicated or bypassed its preparation owner.");
        if (world.Instance != instance || world.WorldHours != hours || world.Flow.Rate != savedFlow ||
            camera.Pose != saves.Meta.Pose || camera.InputLocked || saves.HasPendingPose || rig.Loading.Active || rig.Loading.Progress != 1 || rig.Title.Visible ||
            runtime.PinnedDestinations.Count != 0)
            throw new InvalidOperationException("Continue lost saved flow, world identity, endpoint or input handoff.");
        flow.Dispose();
        saves.UpdateEndpoint(saves.Meta.Pose with { YawDeg = saves.Meta.Pose.YawDeg + 1 });
        if (runtime.PinnedDestinations.Count != 0)
            throw new InvalidOperationException("Disposed flow kept publishing title prefetch requests.");
    }
    private static TestMapService Maps(out MapCatalogEntry entry, out List<string?> requests, Pose? inline = null)
    {
        var source = RealMapFixture.Entry();
        const string dataRoot = "res://flow-test-no-files/";
        var descriptor = source.Descriptor with
        {
            Title = null,
            Spawns = inline is { } pose ? [new(source.Descriptor.DefaultSpawn, pose, null)] : [],
        };
        var catalogue = new MapCatalogEntry(descriptor, dataRoot, source.Waypoints);
        var recorded = new List<string?>();
        entry = catalogue; requests = recorded;
        return new()
        {
            Rev = 1, Default = descriptor.Id, Current = descriptor.Id, Maps = [descriptor],
            Context = new TestMapContext(descriptor, new FakeWorld { Boundary = new([]) }, DataRoot: dataRoot),
            SpawnResolver = (map, spawn) =>
            {
                if (map != descriptor.Id) throw new ArgumentException("Unknown fixture map.", nameof(map));
                recorded.Add(spawn);
                return catalogue.ResolveSpawn(spawn ?? descriptor.DefaultSpawn);
            },
        };
    }
    private sealed class SaveService(TestCameraRig camera) : TestSaveService
    {
        // What the restored view default is on a map whose default spawn has a stand (D-242).
        public static readonly Pose Landing = new(new(120, 1.7f, -70), 20, 0);
        private SlotId? _active;
        public int NewGames { get; private set; }
        public int Saves { get; private set; }
        public override IReadOnlyList<SlotInfo> Slots { get; } = [new(new("s1"), SlotKind.Auto, SlotHealth.Empty, null, 0)];
        public override SlotId? Active => _active;
        public override Task<SaveResult> SaveNow(SaveReason reason) { Saves++; return Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1)); }
        public override Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null)
        {
            NewGames++; _active = slot;
            camera.Mode = ViewMode.Walk; camera.SetPose(Landing);
            return Task.FromResult(new LoadResult(LoadStatus.Loaded, slot, 0, []));
        }
    }
    private sealed class ResumeSaveService(ICameraRig camera, IWorldState world, FlowRuntimeTestStub runtime, MapId map,
        Func<bool> covered) : TestSaveService, ISaveService
    {
        public SaveMeta Meta { get; private set; } = new(1, "test", new("s1"), SlotKind.Auto, 1, 1, 0, map,
            17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly, new(new(30, 45, -15), 25, -20), null, null, null, 0, SaveReason.Test);
        public int Loads { get; private set; }
        public int Completions { get; private set; }
        public override IReadOnlyList<SlotInfo> Slots => [new(Meta.Slot, SlotKind.Auto, SlotHealth.Ok, Meta, 1)];
        public override SlotId? Continue => Meta.Slot;
        public override SlotId? Active => Meta.Slot;
        public override bool HasPendingPose => _hasPendingPose;
        private bool _hasPendingPose;
        private event Action? _slotsChanged;
        event Action? ISaveService.SlotsChanged { add => _slotsChanged += value; remove => _slotsChanged -= value; }
        public void UpdateEndpoint(Pose pose) { Meta = Meta with { Pose = pose }; _slotsChanged?.Invoke(); }
        public override Task<SaveResult> SaveNow(SaveReason reason) => throw new InvalidOperationException("Continue must not write a new generation.");
        public override Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full)
        {
            Loads++; _hasPendingPose = scope is LoadScope.Preview;
            return Task.FromResult(new LoadResult(LoadStatus.Loaded, slot, 1, []));
        }
        public override Task CompleteResume()
        {
            if (!_hasPendingPose || !covered() || world.Flow.Rate != 0) throw new InvalidOperationException("Resume was not prepared under frozen, presented cover.");
            Completions++; runtime.Arrive(Meta.Pose, Meta.Mode, Meta.Space);
            camera.SetPose(Meta.Pose); _hasPendingPose = false;
            return Task.CompletedTask;
        }
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1));
    }
}


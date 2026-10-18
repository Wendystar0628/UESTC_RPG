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
        var context = new TestMapContext(source.Descriptor, source.World, inputs, CinematicSpawnMapStub.DataRoot);
        var loaded = new CinematicCatalog(context).Read("frozen");
        if (loaded.Id != definition.Id || loaded.Map != definition.Map || loaded.Duration != definition.Duration)
            throw new InvalidOperationException("Cinematic data did not come from the frozen map version.");
    }

    [TestCase]
    public async Task TitleAndNewGameResolveDefaultWaypointWithNoInlineSpawns()
    {
        var rig = new FlowTestRig(); var maps = new CinematicSpawnMapStub(); var saves = new SaveService();
        var camera = rig.Camera; var veil = rig.Veil; var presentation = rig.Presentation;
        var playback = rig.Playback; var diagnostics = rig.Diagnostics;
        var flow = rig.CreateFlow(saves, maps, new TestAppExit(allowQuit: false));
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
        var rig = new FlowTestRig(completePrefetchImmediately: !delayDestination);
        var world = rig.World; var camera = rig.Camera; var veil = rig.Veil; var presentation = rig.Presentation;
        var runtime = rig.Runtime; var playback = rig.Playback; var collision = rig.Collision; var diagnostics = rig.Diagnostics;
        world.SetFlow(60);
        var maps = new CinematicSpawnMapStub();
        var saves = new ResumeSaveService(camera, world, runtime, () => presentation.IsCovered);
        var exit = new TestAppExit(quitDuringWait);
        var flow = rig.CreateFlow(saves, maps, exit);
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

    private sealed class SaveService : TestSaveService
    {
        private SlotId? _active;
        public int NewGames { get; private set; }
        public int Saves { get; private set; }
        public override IReadOnlyList<SlotInfo> Slots { get; } = [new(new("s1"), SlotKind.Auto, SlotHealth.Empty, null, 0)];
        public override SlotId? Active => _active;
        public override Task<SaveResult> SaveNow(SaveReason reason) { Saves++; return Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1)); }
        public override Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null)
        { NewGames++; _active = slot; return Task.FromResult(new LoadResult(LoadStatus.Loaded, slot, 0, [])); }
    }
    private sealed class ResumeSaveService(ICameraRig camera, IWorldState world, CinematicRuntimeTestStub runtime,
        Func<bool> covered) : TestSaveService
    {
        public SaveMeta Meta { get; } = new(1, "test", new("s1"), SlotKind.Auto, 1, 1, 0, CinematicSpawnMapStub.Map,
            17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly, new(new(30, 45, -15), 25, -20), null, null, null, 0, SaveReason.Test);
        public int Loads { get; private set; }
        public int Completions { get; private set; }
        public override IReadOnlyList<SlotInfo> Slots => [new(Meta.Slot, SlotKind.Auto, SlotHealth.Ok, Meta, 1)];
        public override SlotId? Continue => Meta.Slot;
        public override SlotId? Active => Meta.Slot;
        public override bool HasPendingPose => _hasPendingPose;
        private bool _hasPendingPose;
        public override Task<SaveResult> SaveNow(SaveReason reason) => throw new InvalidOperationException("Continue must not write a new generation.");
        public override Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full)
        {
            Loads++; _hasPendingPose = scope is LoadScope.Preview;
            return Task.FromResult(new LoadResult(LoadStatus.Loaded, slot, 1, []));
        }
        public override Task CompleteResume()
        {
            if (!_hasPendingPose || !covered() || world.Flow.Rate != 0) throw new InvalidOperationException("Resume was not prepared under frozen, presented cover.");
            Completions++; runtime.Prefetch(Meta.Pose, Meta.Mode, Meta.Space); runtime.BeginWarm();
            camera.SetPose(Meta.Pose); _hasPendingPose = false;
            return Task.CompletedTask;
        }
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => Task.FromResult(new SaveResult(SaveStatus.Saved, Active, 1));
    }
}


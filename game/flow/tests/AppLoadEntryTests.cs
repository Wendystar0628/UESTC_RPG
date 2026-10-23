using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Flow;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AppLoadEntryTests
{
    [TestCase(EnterKind.NewGame)]
    [TestCase(EnterKind.Continue)]
    [TestCategory(TestReason.Contract)]
    public async Task OnlyLoadSlotAcceptsAnExplicitGeneration(EnterKind kind)
    {
        using var f = new Fixture(); await f.OpenTitle();
        var result = await f.Flow.Enter(new(kind, f.Save.Slot.Slot) { Gen = f.Save.Older.Gen });
        if (result != EnterResult.Rejected || f.Flow.State != AppState.Title || f.Rig.Loading.Active ||
            !f.Rig.Title.Visible || f.Save.Loaded is not null || f.Save.ExactGen is not null)
            throw new InvalidOperationException("A generation was accepted outside the explicit entry load command.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task SelectedEntryOwnsItsPrefetchArrivalAndExistingSaveSlot()
    {
        using var f = new Fixture(completeArrivalImmediately: false); await f.OpenTitle();
        var selected = f.Save.Older; var latest = f.Save.Slot.Meta!;
        if (selected.Gen == latest.Gen || selected.Mode == latest.Mode)
            throw new InvalidOperationException("The fixture must distinguish selected and latest endpoints.");
        var entering = f.Flow.Enter(new(EnterKind.LoadSlot, selected.Slot) { Gen = selected.Gen });
        await f.Save.ArrivalStarted.Task;
        if (entering.IsCompleted || !f.Rig.Loading.Active || !f.Rig.Camera.InputLocked ||
            !f.Rig.Runtime.PinnedDestinations.Any(d => d.Pose == selected.Pose && d.Mode == selected.Mode) ||
            f.Rig.Runtime.PinnedDestinations.Any(d => d.Pose == latest.Pose && d.Mode == latest.Mode))
            throw new InvalidOperationException("Entry retained the latest endpoint instead of the selected entry's endpoint.");
        f.Rig.Runtime.CompleteArrival();
        if (await entering != EnterResult.Entered || f.Save.ExactGen != selected.Gen || f.Save.Loaded != selected ||
            f.Rig.Runtime.Destination?.Pose != selected.Pose || f.Rig.Runtime.Destination?.Mode != selected.Mode ||
            f.Save.Active != selected.Slot || f.Rig.Loading.Active || f.Rig.Camera.InputLocked)
            throw new InvalidOperationException("The selected generation did not own the complete restoration endpoint.");
        await f.Flow.ReturnToTitle();
        if (f.Save.FlushedSlot != selected.Slot || f.Save.Active != selected.Slot || f.Save.Slot.Meta != latest)
            throw new InvalidOperationException("Loading an old entry replaced the active save slot or its latest summary.");
    }

    [TestCase(EnterKind.Continue)]
    [TestCase(EnterKind.LoadSlot)]
    [TestCategory(TestReason.Contract)]
    public async Task NullGenerationKeepsTheLatestEntryShortcut(EnterKind kind)
    {
        using var f = new Fixture(); await f.OpenTitle();
        var request = new EnterRequest(kind, kind is EnterKind.LoadSlot ? f.Save.Slot.Slot : null);
        if (await f.Flow.Enter(request) != EnterResult.Entered || f.Save.Loaded != f.Save.Slot.Meta ||
            f.Save.ExactGen is not null || f.Rig.Runtime.Destination?.Mode != f.Save.Slot.Meta!.Mode)
            throw new InvalidOperationException("A generation-free entry stopped restoring the latest readable entry.");
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Regression)]
    public async Task CorruptOrMissingSelectedEntryCannotFallBackToTheLatest(bool missing)
    {
        using var f = new Fixture();
        var slot = f.Save.Slot; var selected = f.Save.Older;
        f.Save.Slot = slot with
        {
            Entries = missing ? slot.Entries.Where(e => e.Gen != selected.Gen).ToArray()
                : slot.Entries.Select(e => e.Gen == selected.Gen ? e with { Health = SlotHealth.Corrupt } : e).ToArray()
        };
        if (f.Save.Slot.Meta is null || f.Save.Slot.Health != SlotHealth.Ok ||
            f.Save.Slot.Entries.Any(e => e.Gen == selected.Gen && e.Health == SlotHealth.Ok))
            throw new InvalidOperationException("The fixture must expose a readable latest entry and an unreadable selection.");
        await f.OpenTitle();
        if (await f.Flow.Enter(new(EnterKind.LoadSlot, selected.Slot) { Gen = selected.Gen }) != EnterResult.Rejected ||
            f.Flow.State != AppState.Title || f.Save.LatestLoads != 0 || f.Save.ExactGen is not null || f.Rig.Loading.Active)
            throw new InvalidOperationException("An unreadable exact entry fell back to its readable latest sibling.");
    }

    [TestCase(EnterKind.Continue)]
    [TestCase(EnterKind.LoadSlot)]
    [TestCategory(TestReason.Regression)]
    public async Task NewerSlotRejectsEvenAnOtherwiseReadableOldEntry(EnterKind kind)
    {
        using var f = new Fixture();
        f.Save.Slot = f.Save.Slot with { Health = SlotHealth.Newer };
        if (!f.Save.Slot.Entries.Any(e => e.Gen == f.Save.Older.Gen && e.Health == SlotHealth.Ok && e.Meta is not null))
            throw new InvalidOperationException("The future-version slot must still expose an apparently readable old entry.");
        await f.OpenTitle();
        var request = new EnterRequest(kind, f.Save.Slot.Slot)
        { Gen = kind is EnterKind.LoadSlot ? f.Save.Older.Gen : null };
        if (await f.Flow.Enter(request) != EnterResult.Rejected || f.Flow.State != AppState.Title ||
            f.Save.LatestLoads != 0 || f.Save.ExactGen is not null || f.Rig.Loading.Active)
            throw new InvalidOperationException("Future-version protection was bypassed through an old entry.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task FailureOfTheExactLoadDoesNotRetryTheLatestOrExposeExceptionText()
    {
        using var f = new Fixture(); f.Save.FailExactLoad = true; await f.OpenTitle();
        if (await f.Flow.Enter(new(EnterKind.LoadSlot, f.Save.Slot.Slot) { Gen = f.Save.Older.Gen }) != EnterResult.Failed ||
            f.Save.ExactGen != f.Save.Older.Gen || f.Save.LatestLoads != 0 || f.Save.Loaded is not null ||
            f.Flow.State != AppState.Title || f.Flow.Failure is not InvalidDataException ||
            f.Flow.LastError != "title.enter_failed" || f.Rig.Loading.Active || f.Rig.Diagnostics.Count != 1)
            throw new InvalidOperationException("Exact-load failure retried a sibling or leaked its failure text to the title.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task RetryingReturnToTitleClearsThePreviousFlowFailure()
    {
        using var f = new Fixture(); await f.OpenTitle();
        if (await f.Flow.Enter(new(EnterKind.Continue)) != EnterResult.Entered)
            throw new InvalidOperationException("Continue did not prepare a playable world.");
        f.Rig.Transit.Draining = Task.FromException(new IOException("Transit cleanup failed."));
        await f.Flow.ReturnToTitle();
        if (f.Flow.State != AppState.Playing || f.Flow.Failure is not IOException ||
            f.Flow.LastError != "title.enter_failed" || f.Rig.Diagnostics.Count != 1)
            throw new InvalidOperationException("The first return did not retain its new failure while recovering play.");
        var draining = new TaskCompletionSource(); f.Rig.Transit.Draining = draining.Task;
        var leaving = f.Flow.ReturnToTitle();
        if (leaving.IsCompleted || f.Flow.State != AppState.Leaving || f.Flow.LastError is not null || f.Flow.Failure is not null)
            throw new InvalidOperationException("A new return retained the previous operation's failure.");
        draining.SetResult(); await leaving;
        if (f.Flow.State != AppState.Title || f.Flow.LastError is not null || f.Flow.Failure is not null ||
            !f.Rig.Title.Visible || f.Rig.Loading.Active || f.Rig.Diagnostics.Count != 1)
            throw new InvalidOperationException("A successful return still exposed the previous flow failure.");
    }

    private sealed class Fixture : IDisposable
    {
        internal FlowTestRig Rig { get; }
        internal EntrySave Save { get; }
        internal AppFlowService Flow { get; }
        internal Fixture(bool completeArrivalImmediately = true)
        {
            Rig = new(completeArrivalImmediately);
            var source = RealMapFixture.Entry(); var descriptor = source.Descriptor;
            var spawn = source.ResolveSpawn(descriptor.DefaultSpawn);
            var older = new SaveMeta(Campus.Kernel.Save.Format.SaveContainer.CurrentFormat, new("s1"), SlotKind.Auto,
                1, 1, 0, descriptor.Id, 17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Walk,
                spawn.ViewPose(ViewMode.Walk) with { FovDeg = UiSettingDefinitions.WalkFov.Default },
                null, null, null, 0, SaveReason.NewGame);
            var latest = older with { Gen = older.Gen + 1, Mode = ViewMode.Fly, Reason = SaveReason.Manual,
                Pose = spawn.ViewPose(ViewMode.Fly) with { FovDeg = UiSettingDefinitions.FlyFov.Default } };
            Save = new(Rig, older, latest);
            var maps = new TestMapService
            {
                Default = descriptor.Id, Current = descriptor.Id, Maps = [descriptor],
                Context = new TestMapContext(descriptor, new FakeWorld { Boundary = new([]) }),
                SpawnResolver = (_, id) => source.ResolveSpawn(id ?? descriptor.DefaultSpawn)
            };
            Flow = Rig.CreateFlow(Save, maps, new TestAppExit(false));
        }
        internal async Task OpenTitle()
        {
            await Flow.PrepareBoot(new()); await Rig.Title.Show(); await Flow.OpenTitle();
            await Flow.PrepareBackground(Task.CompletedTask, Task.CompletedTask);
        }
        public void Dispose() => Flow.Dispose();
    }

    private sealed class EntrySave : TestSaveService
    {
        private readonly FlowTestRig _rig;
        internal SaveMeta Older { get; }
        internal SlotInfo Slot { get; set; }
        internal SaveMeta? Loaded { get; private set; }
        internal ulong? ExactGen { get; private set; }
        internal int LatestLoads { get; private set; }
        internal bool FailExactLoad { get; set; }
        internal SlotId? FlushedSlot { get; private set; }
        internal TaskCompletionSource ArrivalStarted { get; } = new();
        internal EntrySave(FlowTestRig rig, SaveMeta older, SaveMeta latest)
        {
            _rig = rig; Older = older;
            Slot = new(older.Slot, older.Kind, SlotHealth.Ok, latest, 2)
            { Entries = [new(latest.Gen, SlotHealth.Ok, latest), new(older.Gen, SlotHealth.Ok, older)] };
        }
        public override IReadOnlyList<SlotInfo> Slots => [Slot];
        public override SlotId? Continue => Slot.Slot;
        public override SlotId? Active => Loaded?.Slot;
        public override Task<LoadResult> Load(SlotId slot) { LatestLoads++; return Restore(Slot.Meta!); }
        public override Task<LoadResult> Load(SlotId slot, ulong gen)
        {
            ExactGen = gen;
            return FailExactLoad ? Task.FromResult(new LoadResult(LoadStatus.Corrupt, slot, gen, [], "Private read failure details."))
                : Restore(Slot.Entries.Single(e => e.Gen == gen).Meta!);
        }
        private async Task<LoadResult> Restore(SaveMeta meta)
        {
            Loaded = meta; _rig.Camera.Mode = meta.Mode; _rig.Camera.SetPose(meta.Pose);
            var arrival = _rig.Runtime.Arrive(meta.Pose, meta.Mode, meta.Space);
            ArrivalStarted.TrySetResult(); await arrival.Done;
            return new(LoadStatus.Loaded, meta.Slot, meta.Gen, []);
        }
        public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
        {
            FlushedSlot = Active;
            return Task.FromResult(new SaveResult(SaveStatus.Saved, Active, Loaded!.Gen + 1));
        }
    }
}

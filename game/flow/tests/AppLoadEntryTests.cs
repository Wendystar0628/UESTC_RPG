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
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ThrowingCommandObserverDoesNotStrandEnteringOrChangeTheOutcome()
    {
        using var f = new Fixture();
        await f.OpenTitle();
        var original = new IOException();
        var observed = new List<EnterCommandEvent>();
        f.Flow.EnterCommand += _ => throw original;
        f.Flow.EnterCommand += observed.Add;
        var result = await f.Flow.Enter(new(EnterKind.LoadSlot, f.Save.Slot.Slot) { Gen = f.Save.Older.Gen });
        if (result != EnterResult.Entered || f.Flow.State != AppState.Playing || f.Flow.Failure is not null ||
            observed.Count != 2 || observed[^1].Result != EnterResult.Entered || f.Rig.Camera.InputLocked)
            throw new InvalidOperationException("The command observer changed the real entry result or blocked a later observer.");
        Exception? retained = null;
        try { f.Flow.Dispose(); }
        catch (Exception error) { retained = error; }
        if (retained is not AggregateException aggregate || !aggregate.Flatten().InnerExceptions.Contains(original))
            throw new InvalidOperationException("Flow disposal did not receive the original command exception.");
    }

    [TestCase(EnterKind.NewGame)]
    [TestCase(EnterKind.Continue)]
    [TestCategory(TestReason.Contract)]
    public async Task OnlyLoadSlotAcceptsAnExplicitGeneration(EnterKind kind)
    {
        using var f = new Fixture(); await f.OpenTitle();
        List<EnterCommandEvent> commands = []; f.Flow.EnterCommand += commands.Add;
        var request = new EnterRequest(kind, f.Save.Slot.Slot) { Gen = f.Save.Older.Gen };
        var result = await f.Flow.Enter(request);
        if (result != EnterResult.Rejected || f.Flow.State != AppState.Title || f.Rig.Loading.Active ||
            !f.Rig.Title.Visible || f.Save.Loaded is not null || f.Save.ExactGen is not null)
            throw new InvalidOperationException("A generation was accepted outside the explicit entry load command.");
        if (commands.Count != 1 || commands[0] != new EnterCommandEvent(1, EnterCommandPhase.Rejected,
            request, EnterResult.Rejected, false, null))
            throw new InvalidOperationException("Admission rejection started work or changed its requested target.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task SelectedEntryOwnsItsPrefetchArrivalAndExistingSaveSlot()
    {
        using var f = new Fixture(completeArrivalImmediately: false); await f.OpenTitle();
        List<(EnterCommandEvent Command, AppState State, bool Covered, bool Locked)> commands = [];
        f.Flow.EnterCommand += value => commands.Add((value, f.Flow.State, f.Rig.Loading.Active, f.Rig.Camera.InputLocked));
        var selected = f.Save.Older; var latest = f.Save.Slot.Meta!;
        if (selected.Gen == latest.Gen || selected.Mode == latest.Mode)
            throw new InvalidOperationException("The fixture must distinguish selected and latest endpoints.");
        var entering = f.Flow.Enter(new(EnterKind.LoadSlot, selected.Slot) { Gen = selected.Gen });
        await f.Save.ArrivalStarted.Task;
        if (commands.Count != 1 || commands[0].Command.Phase != EnterCommandPhase.Started ||
            commands[0].State != AppState.Entering || !commands[0].Locked || commands[0].Command.Result is not null)
            throw new InvalidOperationException("Entry did not publish its real accepted work before arrival completion.");
        if (entering.IsCompleted || !f.Rig.Loading.Active || !f.Rig.Camera.InputLocked ||
            !f.Rig.Runtime.PinnedDestinations.Any(d => d.Pose == selected.Pose && d.Mode == selected.Mode) ||
            f.Rig.Runtime.PinnedDestinations.Any(d => d.Pose == latest.Pose && d.Mode == latest.Mode))
            throw new InvalidOperationException("Entry retained the latest endpoint instead of the selected entry's endpoint.");
        f.Rig.Runtime.CompleteArrival();
        if (await entering != EnterResult.Entered || f.Save.ExactGen != selected.Gen || f.Save.Loaded != selected ||
            f.Rig.Runtime.Destination?.Pose != selected.Pose || f.Rig.Runtime.Destination?.Mode != selected.Mode ||
            f.Save.Active != selected.Slot || f.Rig.Loading.Active || f.Rig.Camera.InputLocked)
            throw new InvalidOperationException("The selected generation did not own the complete restoration endpoint.");
        var finished = commands.Last();
        if (commands.Count != 2 || finished.Command != new EnterCommandEvent(commands[0].Command.Operation,
            EnterCommandPhase.Completed, new(EnterKind.LoadSlot, selected.Slot) { Gen = selected.Gen }, EnterResult.Entered, false, null) ||
            finished.State != AppState.Playing || finished.Covered || finished.Locked)
            throw new InvalidOperationException("Entry completed its command before the existing input/loading handoff.");
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
        List<EnterCommandEvent> commands = []; f.Flow.EnterCommand += commands.Add;
        if (await f.Flow.Enter(request) != EnterResult.Entered || f.Save.Loaded != f.Save.Slot.Meta ||
            f.Save.ExactGen is not null || f.Rig.Runtime.Destination?.Mode != f.Save.Slot.Meta!.Mode)
            throw new InvalidOperationException("A generation-free entry stopped restoring the latest readable entry.");
        if (commands.Count != 2 || commands.Any(value => value.Target != (request with { Slot = f.Save.Slot.Slot })))
            throw new InvalidOperationException("Accepted entry events lost the owner's resolved slot.");
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
        List<EnterCommandEvent> commands = []; f.Flow.EnterCommand += commands.Add;
        var request = new EnterRequest(EnterKind.LoadSlot, selected.Slot) { Gen = selected.Gen };
        if (await f.Flow.Enter(request) != EnterResult.Rejected ||
            f.Flow.State != AppState.Title || f.Save.LatestLoads != 0 || f.Save.ExactGen is not null || f.Rig.Loading.Active)
            throw new InvalidOperationException("An unreadable exact entry fell back to its readable latest sibling.");
        if (commands.Count != 1 || commands[0] != new EnterCommandEvent(1, EnterCommandPhase.Rejected,
            request, EnterResult.Rejected, false, null))
            throw new InvalidOperationException("An unreadable selected entry published an accepted-work interval.");
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
        List<(EnterCommandEvent Command, AppState State, bool Covered)> commands = [];
        f.Flow.EnterCommand += value => commands.Add((value, f.Flow.State, f.Rig.Loading.Active));
        if (await f.Flow.Enter(new(EnterKind.LoadSlot, f.Save.Slot.Slot) { Gen = f.Save.Older.Gen }) != EnterResult.Failed ||
            f.Save.ExactGen != f.Save.Older.Gen || f.Save.LatestLoads != 0 || f.Save.Loaded is not null ||
            f.Flow.State != AppState.Title || f.Flow.Failure is not InvalidDataException ||
            f.Flow.LastError != "title.enter_failed" || f.Rig.Loading.Active || f.Rig.Diagnostics.Count != 1)
            throw new InvalidOperationException("Exact-load failure retried a sibling or leaked its failure text to the title.");
        if (commands.Count != 2 || commands[0].Command.Phase != EnterCommandPhase.Started ||
            commands[1].Command != (commands[0].Command with { Phase = EnterCommandPhase.Completed, Result = EnterResult.Failed, ErrorKey = f.Flow.LastError }) ||
            commands[1].State != AppState.Title || commands[1].Covered)
            throw new InvalidOperationException("Failed entry closed its command before title recovery completed.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task RejectedRequestsRetainTheirTargetsAndAdvanceTheOwnerOperation()
    {
        using var f = new Fixture(); List<EnterCommandEvent> commands = []; f.Flow.EnterCommand += commands.Add;
        var request = new EnterRequest(EnterKind.NewGame, f.Save.Slot.Slot);
        if (await f.Flow.Enter(request) != EnterResult.Rejected || f.Flow.State != AppState.Boot)
            throw new InvalidOperationException("The fixture must reject title entry while booting.");
        await f.OpenTitle();
        var missing = new EnterRequest(EnterKind.NewGame);
        if (await f.Flow.Enter(missing) != EnterResult.Rejected)
            throw new InvalidOperationException("The fixture must reject new game without a selected slot.");
        if (commands.Count != 2 || commands[0] != new EnterCommandEvent(1, EnterCommandPhase.Rejected,
            request, EnterResult.Rejected, false, null) || commands[1] != new EnterCommandEvent(2,
            EnterCommandPhase.Rejected, missing, EnterResult.Rejected, false, null))
            throw new InvalidOperationException("Admission rejection lost its target or reused an operation number.");
    }

    [TestCase(false), TestCase(true), TestCategory(TestReason.Regression)]
    public async Task DirectFileFailureClosesOnceAndPreservesTheOriginalException(bool fatal)
    {
        using var f = new Fixture();
        Exception failure = fatal ? new OutOfMemoryException() : new IOException();
        f.Save.FileFailure = failure;
        if (await f.Flow.Enter(new(EnterKind.LoadSlot)) != EnterResult.Rejected)
            throw new InvalidOperationException("The fixture must reject Enter before selecting its direct boot intent.");
        List<EnterCommandEvent> commands = []; f.Flow.EnterCommand += commands.Add;
        await f.Flow.PrepareBoot(new(ShowTitle: false, LoadFile: Path.Combine(RepositoryFixture.Root, "direct-entry.sav")));
        await f.Flow.PrepareBackground(Task.CompletedTask, Task.CompletedTask);
        Exception? actual = null;
        try { await f.Flow.EnterDirect(); }
        catch (Exception error) { actual = error; }
        if (!ReferenceEquals(actual, failure) || f.Flow.State != AppState.Entering ||
            !f.Rig.Loading.Active || !f.Rig.Camera.InputLocked || f.Save.Loaded is not null ||
            !ReferenceEquals(f.Flow.Failure, fatal ? null : failure))
            throw new InvalidOperationException("Direct entry changed the original failure or its existing cover ownership.");
        var target = new EnterRequest(EnterKind.LoadSlot);
        if (commands.Count != 2 || commands[0] != new EnterCommandEvent(2, EnterCommandPhase.Started, target, null, true, null) ||
            commands[1] != new EnterCommandEvent(2, fatal ? EnterCommandPhase.Faulted : EnterCommandPhase.Completed,
                target, fatal ? null : EnterResult.Failed, true, f.Flow.LastError))
            throw new InvalidOperationException("Direct file failure fabricated a slot/result or duplicated its terminal event.");
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
        internal Exception? FileFailure { get; set; }
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
        public override Task<LoadResult> LoadFile(string path) => FileFailure is { } error
            ? Task.FromException<LoadResult>(error) : base.LoadFile(path);
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

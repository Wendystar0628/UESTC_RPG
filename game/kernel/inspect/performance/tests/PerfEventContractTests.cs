using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class PerfEventContractTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void WatchdogTimesHeartbeatAndClosesOnlyAfterProgressOrStop()
    {
        double timeout = KernelBudgets.PerfHangWatchdogSeconds * 1000;
        var detector = new PerfHangDetector(timeout);
        var events = new List<PerfCapturedEvent>();
        var stage = new FrameStageSnapshot(FrameBoundary.B3, 10, timeout);
        detector.Observe(null, timeout, events.Add);
        detector.Observe(stage, timeout, events.Add);
        detector.Observe(stage, timeout + timeout / 2, events.Add);
        if (events.Count != 0) throw new InvalidOperationException("A progressing heartbeat was reported as hung.");
        detector.Observe(stage, timeout * 2, events.Add);
        detector.Observe(stage, timeout * 3, events.Add);
        if (events.Count != 1 || events[0].Name != "hang.begin" || events[0].Frame != stage.CollectionFrame)
            throw new InvalidOperationException("A stalled heartbeat did not emit exactly one begin.");
        var payload = JsonSerializer.SerializeToElement(events[0].Payload, PerfFieldDictionary.JsonOptions);
        if (payload.GetProperty("boundary").GetString() != stage.Boundary.ToString())
            throw new InvalidOperationException("The stalled boundary was lost.");
        var resumed = new FrameStageSnapshot(FrameBoundary.B4, stage.CollectionFrame, timeout * 3);
        detector.Observe(resumed, timeout * 3, events.Add);
        detector.Close(timeout * 3, events.Add);
        if (events.Count != 2 || events[1].Name != "hang.end")
            throw new InvalidOperationException("Recovery did not close the hang exactly once.");
        detector.Observe(resumed, timeout * 4, events.Add);
        detector.Close(timeout * 4, events.Add);
        if (events.Count != 4 || events[^1].Name != "hang.end")
            throw new InvalidOperationException("Stopping a recording left its active hang open.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ContextUsesDictionaryForDrawRecoveryAndDoesNotInventOwnerCoverage()
    {
        var dictionary = new PerfFieldDictionary(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
        var context = new PerfContextCapture(dictionary);
        var rule = dictionary.Snapshot["nonSteadyRules"]!["rules"]!.AsArray()
            .Single(item => item!["name"]!.GetValue<string>() == "drawResume")!;
        int recoveryFrames = rule["settleFrames"]!.GetValue<int>();
        long flag = dictionary.Snapshot["contextFlags"]!.AsArray()
            .Single(item => item!["name"]!.GetValue<string>() == "drawResuming")!["value"]!.GetValue<long>();
        var frame = new FrameSample { Context = new(Campus.Contracts.AppFlow.AppState.Playing,
            Campus.Contracts.AppFlow.AppActivity.None, true, false, false) };
        context.Capture(in frame, false, WorldTransition.None, false);
        frame = frame with { Context = frame.Context with { WindowCanDraw = true } };
        for (int index = 0; index < recoveryFrames; index++)
        {
            var captured = context.Capture(in frame, false, WorldTransition.None, false);
            if ((captured.Flags & flag) == 0 || !captured.Complete)
                throw new InvalidOperationException("Draw recovery or attached owner coverage was misclassified.");
        }
        if ((context.Capture(in frame, false, WorldTransition.None, false).Flags & flag) != 0)
            throw new InvalidOperationException("Draw recovery exceeded its dictionary rule.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ProducerQueueKeepsAcceptedValuesAndTailLossUntilDrained()
    {
        var channel = new PerfValueChannel("main", "samples");
        var row = new PerfCapturedRow("samples", 3, 100, new Dictionary<string, object?> { ["xM"] = null });
        long accepted = 0;
        while (channel.TryRow(row)) accepted++;
        channel.Stop();
        long read = 0;
        while (channel.TryRead(out var message))
        {
            read++;
            if (message!.Row != row || message.Sequence != read)
                throw new InvalidOperationException("The producer queue changed an accepted value or its order.");
        }
        if (read != accepted || !channel.Complete || channel.TailGap.Rows != channel.Dropped || channel.Dropped == 0)
            throw new InvalidOperationException("Producer drain lost an accepted value or the final gap.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SameOperationNumbersRemainSeparateAcrossOwnersAndNoChangeApplyCloses()
    {
        var dictionary = Dictionary();
        var context = new PerfContextCapture(dictionary);
        var enter = new EnterCommandEvent(1, EnterCommandPhase.Started, new(EnterKind.NewGame), null, false, null);
        var save = new SaveLoadEvent(1, SaveLoadKind.NewGame, SaveLoadPhase.Queued, null, null, null, null);
        var map = new MapPrepareEvent(1, MapPreparePhase.Started, default, default, null);
        var quality = new QualityApplyEvent(1, QualityApplyPhase.Started, QualityTarget(), ApplyOrigin.User, null);
        context.Observe(in enter, 10);
        context.Observe(in save, 10);
        context.Observe(in map, 10);
        context.Observe(in quality, 10);
        long all = Flag(dictionary, "newGameOrLoad") | Flag(dictionary, "mapTransition") | Flag(dictionary, "qualityApplying");
        var frame = PlayingFrame(10);
        if ((context.Capture(in frame, false, WorldTransition.None, false).Flags & all) != all)
            throw new InvalidOperationException("The colliding owner-operation trigger did not open all command windows.");

        enter = enter with { Phase = EnterCommandPhase.Completed, Result = EnterResult.Entered };
        context.Observe(in enter, 20);
        map = map with { Phase = MapPreparePhase.Joined };
        if (!context.Observe(in map, 20)) throw new InvalidOperationException("Joining a known preparation lost its start.");
        map = map with { Phase = MapPreparePhase.Completed, Result = MapTravelResult.Arrived };
        context.Observe(in map, 20);
        frame = PlayingFrame(20 + Settling(dictionary));
        long flags = context.Capture(in frame, false, WorldTransition.None, false).Flags;
        if ((flags & Flag(dictionary, "newGameOrLoad")) == 0 || (flags & Flag(dictionary, "mapTransition")) != 0)
            throw new InvalidOperationException("One owner's completion closed another owner or a joined window stayed open.");

        save = save with { Phase = SaveLoadPhase.Completed, Result = new(LoadStatus.Loaded, null, 0, []) };
        context.Observe(in save, frame.MonotonicMilliseconds);
        // No QualityChange is published: a no-change command still has an authoritative completion.
        quality = quality with { Phase = QualityApplyPhase.Completed };
        context.Observe(in quality, frame.MonotonicMilliseconds);
        var completed = context.Capture(in frame, false, WorldTransition.None, false);
        if ((completed.Flags & (Flag(dictionary, "newGameOrLoad") | Flag(dictionary, "qualityApplying"))) != 0 ||
            (completed.Flags & Flag(dictionary, "qualitySettling")) == 0 || !completed.Complete || context.NotAttached.Length == 0)
            throw new InvalidOperationException("No-change completion or optional coverage prevented the required context from closing.");
        frame = PlayingFrame(frame.MonotonicMilliseconds + Settling(dictionary));
        if (context.Capture(in frame, false, WorldTransition.None, false).Flags != 0)
            throw new InvalidOperationException("A completed command exceeded its dictionary settling interval.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ReuseRejectionSupersessionAndQueueCancellationDoNotInventWorkOrSettling()
    {
        var dictionary = Dictionary();
        var context = new PerfContextCapture(dictionary);
        var rejected = new EnterCommandEvent(1, EnterCommandPhase.Rejected, new(EnterKind.Continue), EnterResult.Rejected, false, null);
        var reused = new MapPrepareEvent(1, MapPreparePhase.Reused, default, default, MapTravelResult.Arrived);
        if (context.Observe(in rejected, 10) || context.Observe(in reused, 10))
            throw new InvalidOperationException("A rejected or reused operation fabricated a start.");
        var joined = reused with { Operation = 2, Phase = MapPreparePhase.Joined, Result = null };
        if (context.Observe(in joined, 10)) throw new InvalidOperationException("A pre-recording preparation fabricated its missing start.");
        var pending = new QualityApplyEvent(1, QualityApplyPhase.Queued, QualityTarget(), ApplyOrigin.User, null);
        context.Observe(in pending, 10);
        pending = pending with { Phase = QualityApplyPhase.Superseded };
        context.Observe(in pending, 10);
        pending = pending with { Operation = 2, Phase = QualityApplyPhase.Queued };
        context.Observe(in pending, 10);
        pending = pending with { Phase = QualityApplyPhase.Canceled };
        context.Observe(in pending, 10);
        var frame = PlayingFrame(10);
        if (context.Capture(in frame, false, WorldTransition.None, false).Flags != 0)
            throw new InvalidOperationException("A non-working command opened a work or settling window.");
        joined = joined with { Phase = MapPreparePhase.Completed, Result = MapTravelResult.Arrived };
        if (context.Observe(in joined, 20)) throw new InvalidOperationException("A terminal boundary invented its missing start.");
        frame = PlayingFrame(20);
        long flags = context.Capture(in frame, false, WorldTransition.None, false).Flags;
        if ((flags & Flag(dictionary, "mapTransition")) != 0 || (flags & Flag(dictionary, "transitionSettling")) == 0)
            throw new InvalidOperationException("An actual completion did not settle independently of its unknown start.");
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task SaveWorkerCallbacksPreserveRequestAndResultUntilMainDrainAndStop()
    {
        var queue = new PerfSaveCommandQueue();
        var save = new TestSaveService();
        save.LoadCommand += queue.Enqueue;
        var requested = new SlotId("s1");
        var actual = new SlotId("s2");
        var value = new SaveLoadEvent(1, SaveLoadKind.Slot, SaveLoadPhase.Started, requested, 3, null, null);
        var result = new LoadResult(LoadStatus.Loaded, actual, 3, []);
        await Task.Run(() =>
        {
            save.PublishLoadCommand(value);
            save.PublishLoadCommand(value with { Phase = SaveLoadPhase.Completed, Result = result });
        });
        queue.Stop();
        // A previously captured delegate can arrive after unsubscription; Stop also rejects that callback.
        await Task.Run(() => save.PublishLoadCommand(value with { Operation = 2 }));
        save.LoadCommand -= queue.Enqueue;
        if (!queue.TryDequeue(out var started) || started.Value != value || started.Frame is not null || started.Time <= 0 || !queue.TryDequeue(out var completed) ||
            completed.Value.Phase != SaveLoadPhase.Completed || completed.Value.Slot != requested || completed.Value.Result != result ||
            completed.Value.Result.Slot != actual || completed.Frame is not null || completed.Time < started.Time || queue.TryDequeue(out _))
            throw new InvalidOperationException("Worker handoff changed source request/actual result, order, or accepted after stop.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ThumbnailExclusionUsesEveryActualCollectionFrameAndDoesNotIncludePhoto()
    {
        var dictionary = Dictionary();
        var context = new PerfContextCapture(dictionary);
        context.Readback("saveThumbnail", 10);
        context.Readback("saveThumbnail", 12);
        context.Readback("photo", 13);
        for (ulong collectionFrame = 10; collectionFrame <= 13; collectionFrame++)
        {
            var frame = PlayingFrame(10) with { CollectionFrame = collectionFrame };
            bool excluded = (context.Capture(in frame, false, WorldTransition.None, false).Flags & Flag(dictionary, "captureFrame")) != 0;
            if (excluded != (collectionFrame is 10 or 12))
                throw new InvalidOperationException("Thumbnail exclusion drifted from the real readback collection frame.");
        }
    }

    private static PerfFieldDictionary Dictionary() => new(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
    private static long Flag(PerfFieldDictionary dictionary, string name) => dictionary.Snapshot["contextFlags"]!.AsArray()
        .Single(item => item!["name"]!.GetValue<string>() == name)!["value"]!.GetValue<long>();
    private static double Settling(PerfFieldDictionary dictionary) => dictionary.Snapshot["nonSteadyRules"]!["rules"]!.AsArray()
        .Max(item => item!["settleMs"]!.GetValue<double>()) + 1;
    private static FrameSample PlayingFrame(double time) => new() { MonotonicMilliseconds = time,
        Context = new(AppState.Playing, AppActivity.None, true, true, false) };
    private static QualityRequest QualityTarget() => new(new(QualityPreset.Balanced, new Dictionary<QualityKnob, float>()),
        QualityEditIntent.EditKnobs, []);
}

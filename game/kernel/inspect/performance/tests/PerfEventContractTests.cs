using System.Text.Json;
using Campus.Contracts;
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
        var context = new PerfContextCapture(dictionary, requiredOwnerBoundariesAttached: false);
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
            if ((captured.Flags & flag) == 0 || captured.Complete)
                throw new InvalidOperationException("Draw recovery or missing owner coverage was misclassified.");
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
}

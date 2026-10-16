using Campus.Kernel.Post;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class PostDiagnosticsTests
{
    private static CdlSample Sample(float shift = 0f) =>
        new(1.01f + shift, 0.99f, 1f, 0.01f, -0.02f, 0.003f, 0.98f, 1f, 1.02f, 1.05f);

    private static void True(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    private static void Capture(PostDiagSink sink, long instance, ulong frame, in CdlSample current, in CdlSample before,
        in CdlSample after, bool rebaked, bool beforeValid = true) =>
        sink.Capture(instance, frame, 7, current, beforeValid, before, after, rebaked, 32, 1.02f, 1f);

    [TestCase]
    public void DisabledSinkPublishesNothingAndAllocatesNothing()
    {
        var sink = new PostDiagSink(false, false);
        var s = Sample();
        Capture(sink, 1, 1, s, s, s, true);   // warm up the call path
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++) Capture(sink, 1, (ulong)i, s, s, s, true);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        True(allocated == 0, $"disabled capture allocated {allocated} bytes.");
        True(sink.Latest is null, "disabled sink published a snapshot.");
        True(sink.NextLogLine() is null, "disabled sink produced a log line.");
    }

    [TestCase]
    public void SnapshotHoldsEveryFieldWithMatchingBits()
    {
        var sink = new PostDiagSink(true, false);
        var current = Sample(0.00005f);
        var baked = Sample();
        Capture(sink, 3, 120, current, baked, baked, false);
        var snap = sink.Latest ?? throw new InvalidOperationException("no snapshot.");
        True(snap.WorldInstance == 3 && snap.MainFrame == 120 && snap.DrawFrame == 7 && snap.Sequence == 1, "identity fields.");
        True(snap.Current == current && snap.BakedBefore == baked && snap.BakedAfter == baked, "cdl values.");
        True(!snap.Rebaked && snap.BakedBeforeValid, "bake flags.");
        True(snap.StaleKept, "a different current CDL kept under an older bake must read as stale-kept.");
        var json = snap.ToJson();
        foreach (var v in current.ToArray())
            True(json.Contains("\"0x" + BitConverter.SingleToUInt32Bits(v).ToString("x8") + "\""), $"missing bits of {v}.");
        True(json.Contains("\"worldInstance\":3") && json.Contains("\"gpuTextureRead\":false")
            && json.Contains("\"kind\":\"bake-parameters\"") && json.Contains("\"staleKept\":true"), "json header fields.");
        True(json.Contains("\"slopeX\":{\"value\":" + current.SlopeX.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ","), "json value text.");
        for (var i = 0; i < 10; i++)
        {
            var a = current.ToArray()[i];
            True(CdlSample.Bits(a) == BitConverter.SingleToUInt32Bits(a), "bits helper.");
        }
    }

    [TestCase]
    public void RebakeCountsAndFingerprintFollowTheBakedParameters()
    {
        var sink = new PostDiagSink(true, false);
        var a = Sample();
        var b = Sample(0.5f);
        Capture(sink, 1, 1, a, default, a, true, false);
        var first = sink.Latest!;
        Capture(sink, 1, 2, a, a, a, false);
        var same = sink.Latest!;
        Capture(sink, 1, 3, b, a, b, true);
        var moved = sink.Latest!;
        True(first.LutBakeCount == 1 && same.LutBakeCount == 1 && moved.LutBakeCount == 2, "bake counter.");
        True(!first.BakedBeforeValid && !first.StaleKept, "first bake has no valid previous state.");
        True(first.LutParamFingerprint == same.LutParamFingerprint, "same baked parameters must share a fingerprint.");
        True(first.LutParamFingerprint != moved.LutParamFingerprint, "different baked parameters must differ.");
        True(!same.StaleKept, "bit-equal current and baked must not read as stale.");
    }

    [TestCase]
    public void SnapshotIsImmutableAfterThePublishedSourceChanges()
    {
        var sink = new PostDiagSink(true, false);
        var current = Sample();
        Capture(sink, 1, 1, current, current, current, false);
        var held = sink.Latest!;
        var heldJson = held.ToJson();
        var copy = held with { };
        current = Sample(9f);   // the caller's value mutates after the capture
        Capture(sink, 2, 2, current, current, current, true);
        True(held.ToJson() == heldJson && held == copy, "an already taken snapshot changed.");
        True(held.Current == Sample() && held.WorldInstance == 1, "snapshot followed the source.");
        True(sink.Latest!.Sequence == 2 && !ReferenceEquals(sink.Latest, held), "new capture must be a new object.");
    }

    [TestCase]
    public void LogPolicyIsFirstChangeAndHeartbeat()
    {
        var sink = new PostDiagSink(true, false);
        var s = Sample();
        True(sink.NextLogLine() is null, "no line before a capture.");
        Capture(sink, 1, 1, s, s, s, true);
        True(sink.NextLogLine() is { } l && l.StartsWith("WP05_POST_DIAG {"), "first snapshot logs.");
        Capture(sink, 1, 2, s, s, s, false);
        True(sink.NextLogLine() is null, "unchanged state does not log.");
        Capture(sink, 2, 3, s, s, s, false);
        True(sink.NextLogLine() is not null, "a new world instance logs.");
        Capture(sink, 2, 4, s, s, s, true);
        True(sink.NextLogLine() is not null, "a new bake logs.");
        var every = new PostDiagSink(true, true);
        Capture(every, 1, 1, s, s, s, false);
        True(every.NextLogLine() is not null, "all mode logs the first.");
        Capture(every, 1, 2, s, s, s, false);
        True(every.NextLogLine() is not null, "all mode logs every frame.");
    }
}

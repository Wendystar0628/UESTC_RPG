using Campus.Kernel.Post;
using Campus.Kernel.Renderer;
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

    [TestCase]
    public void PublicationBetweenPassesCannotMixFramesOrReleaseTakenLeases()
    {
        var box = new PostFrameBox();
        var firstReleased = 0;
        var nextReleased = 0;
        var firstPalette = new GlobalDataTextureLease(default, () => firstReleased++);
        var firstTable = new GlobalDataTextureLease(default, () => firstReleased++);
        var first = new PostFrame { DiagMainFrame = 1, VignetteK = 0.1f };
        box.Publish(first, firstPalette, firstTable);
        first.VignetteK = 0.9f;   // publication owns its value copy
        Require(box.TryTake(out var batch), "PostSky must take the first publication.");
        try
        {
            Require(batch.Frame.DiagMainFrame == 1 && batch.Frame.VignetteK == 0.1f,
                "PostSky must receive the published frame value.");
            Require(ReferenceEquals(batch.Palette, firstPalette) && ReferenceEquals(batch.MatTable, firstTable),
                "PostSky must receive the leases published with that frame.");
            // Reproduce the original main/render interleaving between Outline and Finish.
            box.Publish(new PostFrame { DiagMainFrame = 2, VignetteK = 0.2f },
                new GlobalDataTextureLease(default, () => nextReleased++),
                new GlobalDataTextureLease(default, () => nextReleased++));
            Require(box.TryTakeFinish(out var finish) && finish.DiagMainFrame == 1 && finish.VignetteK == 0.1f,
                "Finish must use Outline's frame even after the next publication.");
            Require(!box.TryTakeFinish(out _), "Finish input must be consumed only once.");
            box.Close();
            Require(nextReleased == 2 && firstReleased == 0,
                "Cancellation must release pending leases and leave callback-owned leases alive.");
        }
        finally { batch.Dispose(); box.Close(); }
        Require(firstReleased == 2 && nextReleased == 2, "Each publication must release exactly its own leases.");
    }

    [TestCase]
    public void ReplacementAndCloseReleaseOnlyUnclaimedPublications()
    {
        var box = new PostFrameBox();
        var oldReleased = 0;
        var currentReleased = 0;
        var lateReleased = 0;
        box.Publish(new PostFrame { DiagMainFrame = 1 },
            new GlobalDataTextureLease(default, () => oldReleased++),
            new GlobalDataTextureLease(default, () => oldReleased++));
        box.Publish(new PostFrame { DiagMainFrame = 2 },
            new GlobalDataTextureLease(default, () => currentReleased++),
            new GlobalDataTextureLease(default, () => currentReleased++));
        Require(oldReleased == 2 && currentReleased == 0, "Replacement must dispose the unclaimed older batch.");
        Require(box.TryTake(out var batch) && batch.Frame.DiagMainFrame == 2
            && batch.Palette is not null && batch.MatTable is not null,
            "Only the current frame and both of its producer leases may be taken.");
        try
        {
            box.Close();
            box.Close();
            Require(currentReleased == 0, "Close must not dispose callback-owned input.");
            Require(!box.TryTake(out _) && !box.TryTakeFinish(out _), "Close must cancel both pass inputs.");
            box.Publish(default, new GlobalDataTextureLease(default, () => lateReleased++), null);
            Require(lateReleased == 1 && !box.TryTake(out _), "Late publication must release leases without reopening.");
        }
        finally { batch.Dispose(); box.Close(); }
        Require(oldReleased == 2 && currentReleased == 2 && lateReleased == 1, "Release counts must remain exact.");
    }

    [TestCase]
    public void RepeatedDrawsRetainTheSameProducerLeasesUntilClose()
    {
        var box = new PostFrameBox();
        var released = 0;
        Require(!box.TryTake(out _) && !box.TryTakeFinish(out _), "An empty mailbox has no pass inputs.");
        var palette = new GlobalDataTextureLease(default, () => released++);
        var table = new GlobalDataTextureLease(default, () => released++);
        box.Publish(new PostFrame { DiagMainFrame = 1 }, palette, table);
        Require(!box.TryTakeFinish(out _), "Finish must wait for Outline to select a frame.");
        Require(box.TryTake(out var batch), "Outline must select the publication.");
        batch.Dispose();
        Require(box.TryTakeFinish(out var finish) && finish.DiagMainFrame == 1, "Finish must consume the selected frame.");
        Require(box.TryTake(out batch) && batch.Frame.DiagMainFrame == 1
            && ReferenceEquals(batch.Palette, palette) && ReferenceEquals(batch.MatTable, table),
            "Repeated draws must retain the same complete publication and its real producer leases.");
        batch.Dispose();
        Require(box.TryTakeFinish(out finish) && finish.DiagMainFrame == 1,
            "Finish must also run for repeated draws.");
        Require(released == 0, "The current publication must retain both leases between draws.");
        box.Close();
        Require(released == 2, "Repeated draws and cancellation must not release taken leases again.");
    }

    [TestCase]
    public void MissingProducerCancelsPendingInputAndReleasesPartialLeases()
    {
        var box = new PostFrameBox();
        var released = 0;
        box.Publish(default, new GlobalDataTextureLease(default, () => released++),
            new GlobalDataTextureLease(default, () => released++));
        box.Publish(default, new GlobalDataTextureLease(default, () => released++), null);
        Require(released == 3 && !box.TryTake(out _) && !box.TryTakeFinish(out _),
            "A missing material table must release the partial pair and cancel pending input.");
        box.Publish(default, null, new GlobalDataTextureLease(default, () => released++));
        Require(released == 4 && !box.TryTake(out _), "A missing palette must also cancel publication.");
        box.Publish(default, null, null);
        box.Close();
        Require(released == 4, "Cancelling absent producers must not release any lease twice.");
    }

    [TestCase]
    public void CurrentReplacementAndCancellationKeepActiveCallbacksAlive()
    {
        var box = new PostFrameBox();
        var oldReleased = 0;
        var nextReleased = 0;
        var partialReleased = 0;
        box.Publish(new PostFrame { DiagMainFrame = 1 },
            new GlobalDataTextureLease(default, () => oldReleased++),
            new GlobalDataTextureLease(default, () => oldReleased++));
        Require(box.TryTake(out var oldDraw), "The first callback must retain the current publication.");
        box.Publish(new PostFrame { DiagMainFrame = 2 },
            new GlobalDataTextureLease(default, () => nextReleased++),
            new GlobalDataTextureLease(default, () => nextReleased++));
        Require(box.TryTake(out var nextDraw) && nextDraw.Frame.DiagMainFrame == 2,
            "The next draw must promote the pending publication.");
        try
        {
            Require(oldReleased == 0, "Replacement must not release the older active callback's leases.");
            box.Publish(default, null, new GlobalDataTextureLease(default, () => partialReleased++));
            Require(nextReleased == 0 && partialReleased == 1,
                "Missing producers must retire current ownership and preserve the active callback.");
            Require(box.TryTakeFinish(out var finish) && finish.DiagMainFrame == 2,
                "Cancellation after PostSky must not change this draw's Finish frame.");
            Require(!box.TryTake(out _) && !box.TryTakeFinish(out _),
                "Missing producers must prevent future draws from reusing the retired publication.");
            box.Close();
        }
        finally { oldDraw.Dispose(); nextDraw.Dispose(); box.Close(); }
        Require(oldReleased == 2 && nextReleased == 2 && partialReleased == 1,
            "Each real or partial publication must release its leases once after its final callback.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

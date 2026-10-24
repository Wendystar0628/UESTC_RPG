using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Kernel.Inspect;
using GdUnit4;
using Environment = System.Environment;

namespace Campus.Tests;

[TestSuite]
public sealed class PerformanceRecordingContractTests
{
    private static int _directorySequence;

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task StreamsFollowDictionaryAndFinalManifestWaitsForDrain()
    {
        string root = TemporaryDirectory();
        var dictionary = Dictionary();
        var session = Session(root, dictionary);
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The test session did not open.");
            string directory = Path.Combine(root, session.Id!);
            if (File.Exists(Path.Combine(directory, "manifest.json")) || !File.Exists(Path.Combine(directory, "manifest.pending.json")))
                throw new InvalidOperationException("A writable session was published as closed before drain.");
            session.Begin(new(0, 100, 0));
            var sample = new FrameSample { CollectionFrame = 1, MonotonicMilliseconds = 110, FrameMilliseconds = 10 };
            if (!session.Frames.TryWrite(in sample, ReadOnlySpan<ViewportFrameSample>.Empty))
                throw new InvalidOperationException("The test frame was not accepted.");
            session.Stop(PerfEndReason.Toggle, 120);
            var result = await session.Completion;
            if (result is not { Closed: true, Complete: true, WrittenFrames: 1, Error: null })
                throw new InvalidOperationException("The accepted frame did not drain into a complete closed session.", result.Error);
            foreach (string stream in new[] { "frames", "samples", "hw", "hwfast" })
            {
                string header = File.ReadLines(Path.Combine(directory, stream + ".csv")).First();
                if (header != string.Join(',', dictionary.Columns[stream]))
                    throw new InvalidOperationException("A CSV header differs from the authoritative dictionary.");
            }
            if (File.ReadAllLines(Path.Combine(directory, "frames.csv")).Length != 2)
                throw new InvalidOperationException("The frame row was lost at session stop.");
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")))!;
            if (manifest["status"]!.GetValue<string>() != "closed" ||
                !JsonNode.DeepEquals(manifest["fieldDictionary"], dictionary.Snapshot))
                throw new InvalidOperationException("Final manifest is not closed or lost its dictionary snapshot.");
            using var closedLock = new FileStream(Path.Combine(directory, ".lock"), FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            session.Stop(PerfEndReason.Exit, 120);
            await session.Completion;
            Directory.Delete(root, true);
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task SameSecondSessionsKeepDistinctDirectoriesAndOriginalFiles()
    {
        string root = TemporaryDirectory();
        var dictionary = Dictionary();
        var first = Session(root, dictionary);
        PerfRecordingSession? second = null;
        try
        {
            if (!await first.Ready) throw new InvalidOperationException("The first session did not open.");
            first.Stop(PerfEndReason.Toggle, 0);
            var firstResult = await first.Completion;
            second = Session(root, dictionary);
            if (!await second.Ready) throw new InvalidOperationException("The second session did not open.");
            second.Stop(PerfEndReason.Toggle, 0);
            var secondResult = await second.Completion;
            if (!firstResult.Closed || !secondResult.Closed || firstResult.SessionId == secondResult.SessionId ||
                !File.Exists(Path.Combine(root, firstResult.SessionId, "manifest.json")) ||
                !File.Exists(Path.Combine(root, secondResult.SessionId, "manifest.json")))
                throw new InvalidOperationException("Same-second sessions collided or replaced an earlier session.");
        }
        finally
        {
            first.Stop(PerfEndReason.Exit, 0);
            await first.Completion;
            if (second is not null) { second.Stop(PerfEndReason.Exit, 0); await second.Completion; }
            Directory.Delete(root, true);
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AFullRingPreservesTheNextFrameGapAndTheFinalGap()
    {
        var ring = new PerfFrameRing(0, 1);
        var sample = new FrameSample { CollectionFrame = 10, MonotonicMilliseconds = 100 };
        if (!ring.TryWrite(in sample, ReadOnlySpan<ViewportFrameSample>.Empty)) throw new InvalidOperationException("Empty ring rejected a row.");
        sample = sample with { CollectionFrame = 11, MonotonicMilliseconds = 110 };
        if (ring.TryWrite(in sample, ReadOnlySpan<ViewportFrameSample>.Empty)) throw new InvalidOperationException("Full ring overwrote an unread row.");
        ring.Release();
        sample = sample with { CollectionFrame = 12, MonotonicMilliseconds = 120 };
        if (!ring.TryWrite(in sample, ReadOnlySpan<ViewportFrameSample>.Empty) || ring.Peek() is not { Sequence: 3, Gap.Rows: 1, Gap.FirstCollectionFrame: 11 })
            throw new InvalidOperationException("The next row lost its source gap or attempt sequence.");
        sample = sample with { CollectionFrame = 13, MonotonicMilliseconds = 130 };
        if (ring.TryWrite(in sample, ReadOnlySpan<ViewportFrameSample>.Empty) || ring.TailGap is not { Rows: 1, FirstCollectionFrame: 13 } || ring.DroppedFrames != 2)
            throw new InvalidOperationException("The final gap depended on a later successful enqueue.");
    }

    private static PerfFieldDictionary Dictionary() => new(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
    private static PerfRecordingSession Session(string root, PerfFieldDictionary dictionary) =>
        new(root, PerfOrigin.Ai, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), dictionary, new JsonObject(), 0);
    private static string TemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"uestc-perf-{Environment.ProcessId}-{Interlocked.Increment(ref _directorySequence)}");
        Directory.CreateDirectory(path);
        return path;
    }
}

using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Kernel.Inspect;
using GdUnit4;
using static Campus.Tests.PerformanceTestSupport;
using Environment = System.Environment;

namespace Campus.Tests;

[TestSuite]
public sealed class PerformanceRecordingContractTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public async Task AuthorLatestPointsAtRecordingOnlyAfterStartAndAtFormalManifestAfterClose()
    {
        string root = TemporaryDirectory();
        var dictionary = Dictionary();
        var session = new PerfRecordingSession(root, PerfOrigin.Play,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), dictionary, PerformanceTestSupport.SessionManifest());
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The author test session did not open.");
            string latest = Path.Combine(root, "latest.json");
            if (File.Exists(latest)) throw new InvalidOperationException("A reserved session was advertised as recording.");
            var anchor = new PerfClockAnchor(0, 100, 0);
            session.Begin(anchor);
            if (!await session.StartedWriting) throw new InvalidOperationException("The session did not write its start.");
            var pointer = JsonNode.Parse(File.ReadAllText(latest))!;
            string directory = Path.Combine(root, session.Id!);
            if (pointer["state"]!.GetValue<string>() != "recording" ||
                pointer["manifest"]!.GetValue<string>() != session.Id + "/manifest.pending.json" ||
                pointer["startedAt"]!.GetValue<DateTimeOffset>() != DateTimeOffset.FromUnixTimeMilliseconds(anchor.UnixMs) ||
                File.Exists(Path.Combine(directory, "manifest.json")))
                throw new InvalidOperationException("The recording pointer did not retain its actual state and start anchor.");
            string[] writtenEvents;
            using (var readable = new FileStream(Path.Combine(directory, "events.jsonl"), FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(readable))
                writtenEvents = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => JsonNode.Parse(line)!["event"]!.GetValue<string>()).ToArray();
            if (!writtenEvents.Contains("session.start") || !writtenEvents.Contains("clock.anchor"))
                throw new InvalidOperationException("The active pointer was published before start data reached the stream.");
            session.Stop(PerfEndReason.Toggle, 120);
            var result = await session.Completion;
            pointer = JsonNode.Parse(File.ReadAllText(latest))!;
            if (!result.Closed || pointer["state"]!.GetValue<string>() != "closed" ||
                pointer["manifest"]!.GetValue<string>() != session.Id + "/manifest.json" ||
                !File.Exists(Path.Combine(directory, "manifest.json")))
                throw new InvalidOperationException("The closed pointer preceded or missed its formal manifest.");
        }
        finally
        {
            session.Stop(PerfEndReason.Exit, 120);
            await session.Completion;
            Directory.Delete(root, true);
        }
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true), TestCategory(TestReason.Regression)]
    public async Task StreamsFollowDictionaryAndFinalManifestWaitsForDrain(bool missingBuild, bool missingConditions)
    {
        string root = TemporaryDirectory();
        var dictionary = Dictionary();
        var manifestFacts = PerformanceTestSupport.SessionManifest();
        if (missingBuild) manifestFacts.Remove("build");
        if (missingConditions) manifestFacts["conditionsMissingReason"] = "The requested sample Begin was never accepted.";
        var session = new PerfRecordingSession(root, PerfOrigin.Ai, DateTimeOffset.UnixEpoch, dictionary, manifestFacts);
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The test session did not open.");
            string directory = Path.Combine(root, session.Id!);
            if (File.Exists(Path.Combine(directory, "manifest.json")) || !File.Exists(Path.Combine(directory, "manifest.pending.json")))
                throw new InvalidOperationException("A writable session was published as closed before drain.");
            session.Begin(new(0, 100, 0));
            var streamSample = new StreamStatistics { Revision = 7, MountedTiles = 2, MountedTilesDelta = -1,
                WatchdogTrips = 3, MountMilliseconds = .5, MountedBytes = 64 };
            var sample = new FrameSample { CollectionFrame = 1, MonotonicMilliseconds = 110,
                FrameMilliseconds = 10, Stream = streamSample };
            var missingStream = sample with { CollectionFrame = 2, MonotonicMilliseconds = 115, Stream = null };
            if (!session.Frames.TryWrite(in sample, ReadOnlySpan<ViewportFrameSample>.Empty) ||
                !session.Frames.TryWrite(in missingStream, ReadOnlySpan<ViewportFrameSample>.Empty))
                throw new InvalidOperationException("The test frame was not accepted.");
            session.Stop(PerfEndReason.Toggle, 120);
            var result = await session.Completion;
            if (result is not { Closed: true, WrittenFrames: 2, DroppedFrames: 0, Error: null } ||
                result.Complete != (!missingBuild && !missingConditions) ||
                (!result.Complete && manifestFacts["missingReasons"]!.AsArray().Count == 0))
                throw new InvalidOperationException("The accepted frame did not drain into a complete closed session.", result.Error);
            foreach (string stream in new[] { "frames", "samples", "hw", "hwfast" })
            {
                string header = File.ReadLines(Path.Combine(directory, stream + ".csv")).First();
                if (header != string.Join(',', dictionary.Columns[stream]))
                    throw new InvalidOperationException("A CSV header differs from the authoritative dictionary.");
            }
            var rows = File.ReadAllLines(Path.Combine(directory, "frames.csv"));
            if (rows.Length != result.WrittenFrames + 1)
                throw new InvalidOperationException("The frame row was lost at session stop.");
            // These rows have no viewport payloads; no CSV cell contains a comma.
            var known = rows[1].Split(','); var missing = rows[2].Split(',');
            string Value(string field) => known[Array.IndexOf(dictionary.Columns["frames"], field)];
            if (Value("streamRevision") != PerfFieldDictionary.Csv(streamSample.Revision) ||
                Value("streamMountedTiles") != PerfFieldDictionary.Csv(streamSample.MountedTiles) ||
                Value("streamMountedTilesDelta") != PerfFieldDictionary.Csv(streamSample.MountedTilesDelta) ||
                Value("streamWatchdogTrips") != PerfFieldDictionary.Csv(streamSample.WatchdogTrips) ||
                Value("streamMountMs") != PerfFieldDictionary.Csv(streamSample.MountMilliseconds) ||
                Value("pendingStreamMounts") != PerfFieldDictionary.Csv(streamSample.PendingMounts) ||
                dictionary.Columns["frames"].Select((field, index) => (field, index))
                    .Any(item => (item.field.StartsWith("stream", StringComparison.Ordinal) ||
                        item.field.StartsWith("pendingStream", StringComparison.Ordinal)) && missing[item.index] != "null"))
                throw new InvalidOperationException("Stream serialization changed the owner snapshot or replaced missing facts with zero.");
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
        var ring = new PerfFrameRing(capacity: 1);
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

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task FullNativeBatchQueueKeepsActualRowLossInTheStoppedSession()
    {
        string root = TemporaryDirectory();
        var session = Session(root, Dictionary());
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The overflow writer did not open.");
            var producer = session.AddProducer("nvml.fast", "hwfast");
            var row = new PerfCapturedRow("hwfast", null, 10, new Dictionary<string, object?>());
            long accepted = 0;
            while (producer.TryRow(row)) { accepted++; producer.Flush(); }
            if (accepted == 0 || producer.Dropped != 1) throw new InvalidOperationException("The unread queue did not fill.");
            var fixture = PerformanceTestSupport.BufferedFastFixture();
            long lost = producer.Dropped;
            foreach (var batch in fixture.Batches)
            {
                lost += PerfHardwareCapture.FastRows(fixture.Identity, batch).Count();
                if (producer.TryFastBatch(fixture.Identity, batch)) throw new InvalidOperationException("The full queue accepted a native batch.");
            }
            if (producer.Dropped != lost || producer.TailGap.Rows != lost ||
                producer.TailGap.FirstSequence != accepted + 1 || producer.TailGap.LastSequence != accepted + lost)
                throw new InvalidOperationException("A rejected native batch was counted as messages instead of output rows.");
            session.Begin(new(0, 0, 0));
            session.Stop(PerfEndReason.Toggle, 30);
            var ended = await session.Completion;
            string directory = Path.Combine(root, ended.SessionId);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")))!;
            var stream = manifest["streams"]!.AsArray().Single(item => item!["file"]!.GetValue<string>() == "hwfast.csv")!;
            var events = File.ReadLines(Path.Combine(directory, "events.jsonl")).Select(line => JsonNode.Parse(line)!).ToArray();
            var gap = events.Single(item => item["event"]!.GetValue<string>() == "record.gap")["payload"]!;
            if (!ended.Closed || ended.Complete || ended.Fault != PerfFaultCode.None || manifest["complete"]!.GetValue<bool>() ||
                stream["writtenRows"]!.GetValue<long>() != accepted || stream["droppedRows"]!.GetValue<long>() != lost ||
                File.ReadAllLines(Path.Combine(directory, "hwfast.csv")).LongLength != accepted + 1 ||
                gap["stream"]!.GetValue<string>() != "hwfast" || gap["rows"]!.GetValue<long>() != lost ||
                gap["firstSequence"]!.GetValue<long>() != accepted + 1 || gap["lastSequence"]!.GetValue<long>() != accepted + lost ||
                gap["fromMs"]!.GetValue<double>() != row.Time || gap["toMs"]!.GetValue<double>() != fixture.Batches[^1].QueryEndMs ||
                events.Single(item => item["event"]!.GetValue<string>() == "session.end")["payload"]!["complete"]!.GetValue<bool>())
                throw new InvalidOperationException("Stopped native-batch overflow lost its tail gap or advertised completeness.");
        }
        finally { session.Stop(PerfEndReason.Exit, 30); await session.Completion; Directory.Delete(root, true); }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void HardwareFastRowsKeepQueryMetadataEvenWhenTheDriverHasNoNewSamples()
    {
        var cursor = new NvmlSampleCursor();
        var identity = new NvmlIdentity(3, "test-device", "0000:05:00.0", NvmlStatus.FromCode(NvmlNative.Success),
            NvmlStatus.FromCode(NvmlNative.Success), NvmlStatus.FromCode(NvmlNative.Success));
        var first = cursor.Observe(NvmlMetric.Power, NvmlStatus.FromCode(NvmlNative.Success),
            [new() { TimestampUs = 10_000, Value = new() { UnsignedInt = 2_000 } }], 20, 30);
        var empty = cursor.Observe(NvmlMetric.Power, NvmlStatus.SampleResult(NvmlNative.NotFound), [], 40, 50);
        var rows = PerfHardwareCapture.FastRows(identity, empty).ToArray();
        if (empty.Samples.Count != 0 || rows.Length != 1)
            throw new InvalidOperationException("The fixture or empty-query metadata row is missing.");
        var values = rows[0].Values;
        if (rows[0].Frame.HasValue || values["driverTimestampUs"] is not null || values["value"] is not null ||
            !Equals(values["capability"], first.Status.Capability) || !Equals(values["deviceIndex"], identity.EnumerationIndex) ||
            !Equals(values["gpuUuid"], identity.Uuid) || !Equals(values["pciBusId"], identity.PciBusId) ||
            !Equals(values["cursorBeforeUs"], first.CursorAfterUs) || !Equals(values["cursorAfterUs"], empty.CursorAfterUs) ||
            !Equals(values["previousSuccessfulReadMs"], empty.PreviousSuccessfulReadMs) ||
            !Equals(values["successfulReadSpanMs"], empty.SuccessfulReadSpanMs) || values["lostSampleCount"] is not null)
            throw new InvalidOperationException("Empty driver queries fabricated samples or lost independent cursor and identity facts.");
        if (values.Keys.Except(Dictionary().Columns[rows[0].Stream]).Any())
            throw new InvalidOperationException("The hardware row contains fields the authoritative dictionary would discard.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void HardwareSlowRowsPreserveIndependentCountersAndMissingFieldsWithoutSerializingExceptions()
    {
        var success = NvmlStatus.FromCode(NvmlNative.Success);
        var unavailable = new NvmlStatus("unavailable", "entry point missing", Error: new EntryPointNotFoundException());
        var energy = new NvmlCounterInterval
        {
            Cumulative = 4, Delta = null, IntervalStartMs = 100, ReadStartMs = 150, ReadEndMs = 160,
            Reset = true, ReadGap = true, DriverReferenceUs = null, Status = success
        };
        var policy = energy with { Cumulative = 900_000, Delta = 200_000, IntervalStartMs = 20,
            ReadStartMs = 170, ReadEndMs = 180, Reset = false, DriverReferenceUs = 4_000 };
        var slow = new NvmlSlowSample
        {
            QueryStartMs = 140, QueryEndMs = 190, GpuUtilFraction = new(0.5, success), MemoryUtilFraction = new(null, unavailable),
            WholeGpuMemoryMiB = new(null, unavailable), TotalMemoryMiB = new(null, unavailable), GpuClockMHz = new(null, unavailable),
            GpuMaxClockMHz = new(null, unavailable), MemoryClockMHz = new(null, unavailable), PowerW = new(null, unavailable),
            TemperatureC = new(null, unavailable), PerformanceState = new(null, unavailable), ThrottleReasonBits = new(null, unavailable),
            Energy = energy, Policies = [new(NvmlPolicy.Thermal, policy)]
        };
        var identity = new NvmlIdentity(2, null, "0000:06:00.0", success, unavailable, success);
        var device = new NvmlDeviceSample(identity, slow, []);
        var row = PerfHardwareCapture.SlowRow(new(140, 190, success, [device]), device, 10);
        var values = row.Values;
        if (!Equals(values["energyReset"], true) || values["energyJ"] is not null || values["energyAveragePowerW"] is not null ||
            !Equals(values["energyIntervalStartMs"], energy.IntervalStartMs) || !Equals(values["energyReadEndMs"], energy.ReadEndMs) ||
            !Equals(values["violationThermalMs"], policy.Delta / 1_000_000d) || values["gpuMemoryUtilFraction"] is not null ||
            !Equals(values["intervalStartMs"], 10d) || values["gpuUuid"] is not null || row.Frame.HasValue)
            throw new InvalidOperationException("Slow hardware conversion fabricated values or merged independent counter intervals.");
        var policies = System.Text.Json.JsonSerializer.SerializeToNode(values["policyIntervalsJson"], PerfFieldDictionary.JsonOptions)!;
        var counter = policies[0]!["counter"]!;
        var status = System.Text.Json.JsonSerializer.SerializeToNode(values["statusJson"], PerfFieldDictionary.JsonOptions)!;
        if (counter["driverReferenceUs"]!.GetValue<ulong>() != policy.DriverReferenceUs ||
            counter["intervalStartMs"]!.GetValue<double>() != policy.IntervalStartMs ||
            status["gpuMemoryUtilFraction"]!["exceptionType"]!.GetValue<string>() != unavailable.Error!.GetType().FullName ||
            status["gpuMemoryUtilFraction"]!.AsObject().ContainsKey("error") ||
            values.Keys.Except(Dictionary().Columns[row.Stream]).Any())
            throw new InvalidOperationException("Per-policy provenance, sanitized failure reason or dictionary coverage was lost.");
    }

    private static PerfFieldDictionary Dictionary() => new(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
    private static PerfRecordingSession Session(string root, PerfFieldDictionary dictionary) =>
        new(root, PerfOrigin.Ai, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), dictionary, PerformanceTestSupport.SessionManifest());

}

using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Kernel.Inspect;
using GdUnit4;

namespace Campus.Tests;

// Public counter semantics are tested as values; the recording worker also proves its real stop/join path.
[TestSuite]
public sealed class PerformanceNvmlContractTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void CumulativeCountersKeepIndependentSuccessfulIntervalsAndReportResets()
    {
        var energy = new NvmlCounterTracker();
        var policy = new NvmlCounterTracker();
        var initial = energy.Observe(Reading(100), 10, 20);
        policy.Observe(Reading(700), 30, 40, 1_000);
        var missing = energy.Observe(new(null, NvmlStatus.FromCode(NvmlNative.NotSupported)), 50, 60);
        var next = energy.Observe(Reading(500), 90, 100);
        var policyNext = policy.Observe(Reading(800), 110, 120, 2_000);
        if (initial.Delta.HasValue || missing.Delta.HasValue || !next.ReadGap ||
            next.Delta != 500UL - initial.Cumulative || next.IntervalStartMs != initial.ReadEndMs ||
            policyNext.IntervalStartMs == next.IntervalStartMs || policyNext.ReadGap)
            throw new InvalidOperationException("Cumulative counters lost their own successful-read intervals.");
        var reset = energy.Observe(Reading(4), 130, 140);
        var afterReset = energy.Observe(Reading(9), 150, 160);
        var referenceReset = policy.Observe(Reading(900), 170, 180, 500);
        if (!reset.Reset || reset.Delta.HasValue || afterReset.Delta != 9UL - reset.Cumulative ||
            !referenceReset.Reset || referenceReset.Delta.HasValue)
            throw new InvalidOperationException("Counter or driver-reference reset fabricated an interval delta.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FastTypesDeduplicateIndependentlyAndNoNewSamplesDoNotRemoveCapability()
    {
        var power = new NvmlSampleCursor();
        var utilization = new NvmlSampleCursor();
        var first = power.Observe(NvmlMetric.Power, Success(), [Sample(1_000, 2_000), Sample(3_000, 3_000)], 10, 20);
        var noNew = power.Observe(NvmlMetric.Power, NvmlStatus.SampleResult(NvmlNative.NotFound), [], 30, 40);
        var unavailable = utilization.Observe(NvmlMetric.GpuUtilization,
            NvmlStatus.FromCode(NvmlNative.NotSupported), [], 50, 60);
        var second = power.Observe(NvmlMetric.Power, Success(),
            [Sample(5_000, 4_000), Sample(3_000, 3_000), Sample(5_000, 4_000)], 70, 80);
        if (noNew.Status.Capability != first.Status.Capability || noNew.Samples.Count != 0 ||
            unavailable.Status.Capability == first.Status.Capability || utilization.LastSeenUs != 0 ||
            second.Samples.Count != 1 || second.CursorBeforeUs != first.CursorAfterUs ||
            second.PreviousSuccessfulReadMs != first.QueryEndMs ||
            second.SuccessfulReadSpanMs != second.QueryEndMs - first.QueryEndMs || second.LostSampleCount.HasValue)
            throw new InvalidOperationException("Per-type sample cursor, query span or capability semantics changed.");
        var point = second.Samples[0];
        if (point.ObservedIntervalMs != (point.DriverTimestampUs - first.CursorAfterUs) / 1000d)
            throw new InvalidOperationException("Observed interval was synthesized instead of using driver timestamps.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void DriverTimestampRegressionStartsANewContinuityInterval()
    {
        var cursor = new NvmlSampleCursor();
        var before = cursor.Observe(NvmlMetric.GpuUtilization, Success(),
            [Sample(10_000, 20), Sample(12_000, 30)], 10, 20);
        var after = cursor.Observe(NvmlMetric.GpuUtilization, Success(),
            [Sample(1_000, 40), Sample(2_000, 50)], 30, 40);
        if (after.CursorAfterUs >= before.CursorAfterUs || after.Samples.Count == 0 ||
            after.Samples[0].ObservedIntervalMs.HasValue || after.ContinuityReasons.Count == 0 || after.LostSampleCount.HasValue)
            throw new InvalidOperationException("Clock regression was hidden or converted to a fabricated sample count.");
    }

    private static NvmlStatus Success() => NvmlStatus.FromCode(NvmlNative.Success);
    private static NvmlValue<ulong> Reading(ulong value) => new(value, Success());
    private static NvmlNative.Sample Sample(ulong timestamp, uint value) => new()
    { TimestampUs = timestamp, Value = new() { UnsignedInt = value } };

    internal static (NvmlIdentity Identity, NvmlFastBatch[] Batches, NvmlSampleCursor[] Cursors) BufferedFastFixture()
    {
        NvmlMetric[] metrics = [NvmlMetric.Power, NvmlMetric.GpuUtilization, NvmlMetric.MemoryUtilization];
        int[] counts = [119, 71, 71];
        var cursors = metrics.Select(_ => new NvmlSampleCursor()).ToArray();
        var batches = metrics.Select((metric, index) => cursors[index].Observe(metric, Success(),
            Enumerable.Range(1, counts[index]).Select(point => Sample((ulong)point * 1_000, 50)).ToArray(), 10, 20)).ToArray();
        return (new(0, "fixture-gpu", null, Success(), Success(), Success()), batches, cursors);
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task BufferedNativeBatchesDrainEveryRowAndContinueTheirIndependentCursors()
    {
        string root = PerformanceRecordingContractTests.TemporaryDirectory();
        var dictionary = new PerfFieldDictionary(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
        var session = new PerfRecordingSession(root, PerfOrigin.Ai, DateTimeOffset.UnixEpoch, dictionary, new());
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The native-batch writer did not open.");
            var producer = session.AddProducer("nvml.fast", "hwfast");
            var fixture = BufferedFastFixture();
            var probe = new PerfValueChannel("fixture", "hwfast");
            var row = new PerfCapturedRow("hwfast", null, 10, new Dictionary<string, object?>());
            int slots = 0;
            while (probe.TryRow(row)) { slots++; probe.Flush(); }
            if (fixture.Batches.Sum(batch => batch.Samples.Count) <= slots)
                throw new InvalidOperationException("The buffered first query no longer exceeds the unread row queue.");
            var timestamps = new List<ulong>();
            foreach (var batch in fixture.Batches)
            {
                if (!producer.TryFastBatch(fixture.Identity, batch)) throw new InvalidOperationException("A first native batch was rejected.");
                producer.Flush();
                timestamps.AddRange(batch.Samples.Select(point => point.DriverTimestampUs));
            }
            for (int index = 0; index < fixture.Batches.Length; index++)
            {
                var before = fixture.Batches[index];
                var next = fixture.Cursors[index].Observe(before.Metric, Success(),
                    [Sample(before.CursorAfterUs, 50), Sample(before.CursorAfterUs + 1_000, 50)], 30, 40);
                if (next.CursorBeforeUs != before.CursorAfterUs || next.PreviousSuccessfulReadMs != before.QueryEndMs ||
                    next.Samples.Count != 1 || !producer.TryFastBatch(fixture.Identity, next))
                    throw new InvalidOperationException("The next native query lost its cursor or repeated an old sample.");
                timestamps.Add(next.Samples[0].DriverTimestampUs);
                producer.Flush();
            }
            var empty = fixture.Cursors[0].Observe(NvmlMetric.Power, NvmlStatus.SampleResult(NvmlNative.NotFound), [], 50, 60);
            if (!producer.TryFastBatch(fixture.Identity, empty)) throw new InvalidOperationException("The empty query metadata was rejected.");
            session.Begin(new(0, 0, 0));
            session.Stop(PerfEndReason.Toggle, 60);
            var ended = await session.Completion;
            string directory = Path.Combine(root, ended.SessionId);
            var lines = File.ReadAllLines(Path.Combine(directory, "hwfast.csv")).Skip(1).Select(line => line.Split(',')).ToArray();
            int sequenceColumn = Array.IndexOf(dictionary.Columns["hwfast"], "sequence");
            int timestampColumn = Array.IndexOf(dictionary.Columns["hwfast"], "driverTimestampUs");
            if (!ended.Closed || !ended.Complete || producer.Dropped != 0 || lines.Length != timestamps.Count + 1 ||
                lines.Where((line, index) => line[sequenceColumn] != PerfFieldDictionary.Csv(index + 1)).Any() ||
                timestamps.Where((stamp, index) => lines[index][timestampColumn] != PerfFieldDictionary.Csv(stamp)).Any() ||
                lines[^1][timestampColumn] != "null" ||
                File.ReadLines(Path.Combine(directory, "events.jsonl")).Any(line => JsonSerializer.Deserialize<JsonElement>(line).GetProperty("event").GetString() == "record.gap"))
                throw new InvalidOperationException("Native-batch drain lost a row, its sequence, metadata or completeness.");
        }
        finally { session.Stop(PerfEndReason.Exit, 60); await session.Completion; Directory.Delete(root, true); }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OtherGpuExcludesGameAndForeignAdaptersAndUsesBusiestEngineAfterSummingPids()
    {
        const string actual = "00000000:00000001", foreign = "00000000:00000002";
        GpuEngineRow Row(uint pid, string luid, uint engine, double fraction) => new()
        { Instance = $"fixture-{pid}-{engine}", ProcessId = pid, AdapterLuid = luid,
            PhysicalAdapter = 0, Engine = engine, EngineType = "3D", UtilFraction = fraction };
        GpuEngineRow[] rows = [Row(7, actual, 0, .75), Row(8, actual, 0, .25), Row(9, actual, 0, .125),
            Row(8, actual, 1, .25), Row(10, foreign, 0, 1)];
        var fact = WindowsGpuEngineSampler.Aggregate(rows, actual, 7, 0, 100, 110, 0, 105);
        if (fact.Capability != PerfFactCapability.Supported || fact.Value != .25 + .125 ||
            fact.QueryStartMs != 100 || fact.QueryEndMs != 110 || fact.Reason is not null)
            throw new InvalidOperationException("Other-process GPU aggregation lost adapter/PID/engine semantics.");
        var recorded = WindowsGpuEngineSampler.ForRecording(rows, actual, 7);
        if (recorded.TopProcesses.Any(process => process.ProcessId is 7 or 10) ||
            recorded.EngineTypes.Single().OtherProcessesUtilFraction != fact.Value ||
            recorded.InstanceCount != rows.Count(row => row.AdapterLuid == actual && row.ProcessId != 7))
            throw new InvalidOperationException("Recorded engine totals lost adapter/PID filtering or complete aggregation.");
        var invalid = WindowsGpuEngineSampler.Aggregate([rows[1] with { CStatus = 0xc0000bba, UtilFraction = null }],
            actual, 7, 0, 100, 110, 0, 105);
        var initial = WindowsGpuEngineSampler.Aggregate(rows, actual, 7, 0, 100, 110, null, 105);
        var unavailable = WindowsGpuEngineSampler.ForRecording([rows[1] with { CStatus = 0xc0000bba, UtilFraction = null }], actual, 7);
        if (invalid.Value.HasValue || initial.Value.HasValue || invalid.Reason is null || initial.Reason is null ||
            unavailable.InvalidInstanceCount != unavailable.InstanceCount || unavailable.EngineTypes.Single().OtherProcessesUtilFraction.HasValue)
            throw new InvalidOperationException("Unknown GPU counter intervals became zero or supported values.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void EngineRecordingRetainsCompleteTypeTotalsAndOnlyBusiestPidsWithRawWindows()
    {
        const string luid = "00000000:00000001";
        const int instanceCount = 1_000;
        var rows = Enumerable.Range(100, instanceCount).Select(pid => new GpuEngineRow
        {
            Instance = $"pid_{pid}_luid_0x00000000_0x00000001_phys_0_eng_0_engtype_3D",
            ProcessId = (uint)pid, AdapterLuid = luid, PhysicalAdapter = 0, Engine = 0, EngineType = "3D",
            UtilFraction = .5 / instanceCount, RawStatus = 0, RawFirstValue = pid, RawSecondValue = 10_000,
            RawTimestamp100ns = 9_007_199_254_740_993, RawMultiCount = 1,
            AverageStartTimestamp100ns = 9_007_199_254_730_993, AverageEndTimestamp100ns = 9_007_199_254_740_993
        }).ToArray();
        if (rows.Select(row => row.ProcessId).Distinct().Count() <= WindowsGpuEngineSampler.TopProcessCount)
            throw new InvalidOperationException("The fixture does not reproduce the excess process payload.");
        var recorded = WindowsGpuEngineSampler.ForRecording(rows, luid, 7);
        var fact = WindowsGpuEngineSampler.Aggregate(rows, luid, 7, 0, 100, 110, 0, 105);
        if (fact.Capability != PerfFactCapability.Supported || recorded.TopProcesses.Count != WindowsGpuEngineSampler.TopProcessCount ||
            recorded.EngineTypes.Single().OtherProcessesUtilFraction != fact.Value ||
            recorded.TopProcesses.Sum(process => process.UtilFraction) >= fact.Value ||
            recorded.TopProcesses.SelectMany(process => process.Engines).Any(row => !rows.Contains(row)))
            throw new InvalidOperationException("Recording selection changed complete totals or current raw windows.");
        string all = PerfFieldDictionary.Csv(JsonSerializer.Serialize(rows, PerfFieldDictionary.JsonOptions));
        string compact = PerfFieldDictionary.Csv(JsonSerializer.Serialize(recorded, PerfFieldDictionary.JsonOptions));
        if (compact.Length >= all.Length) throw new InvalidOperationException("Engine recording kept the full instance payload.");
        Console.WriteLine($"GAME245 serialized fixture: instances={rows.Length}, retainedPids={recorded.TopProcesses.Count}, allCsvBytes={all.Length}, compactCsvBytes={compact.Length}");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OtherCpuSubtractsGameFromSystemBusyWithoutAddingIdleTwiceAndRetainsCoverageFailure()
    {
        var previous = new CpuCounterRead { Idle100ns = 100, Kernel100ns = 300, User100ns = 200,
            ProcessKernel100ns = 20, ProcessUser100ns = 30, SystemQueryStartMs = 9, SystemQueryEndMs = 10,
            ProcessQueryStartMs = 11, ProcessQueryEndMs = 12, ProcessorGroups = 1, LogicalProcessors = 4 };
        var current = previous with { Idle100ns = 125, Kernel100ns = 380, User100ns = 220,
            ProcessKernel100ns = 35, ProcessUser100ns = 40, SystemQueryStartMs = 109, SystemQueryEndMs = 110,
            ProcessQueryStartMs = 111, ProcessQueryEndMs = 112 };
        var result = WindowsCpuSampler.Calculate(current, previous);
        if (result.Fact.Capability != PerfFactCapability.Supported || result.Fact.Value != .5 ||
            result.Previous != previous || result.Current != current || result.Fact.AverageStartMs != previous.SystemQueryEndMs)
            throw new InvalidOperationException("CPU subtraction, raw counters or independent read windows changed.");
        var grouped = WindowsCpuSampler.Calculate(current with { ProcessorGroups = 2, LogicalProcessors = 128 }, previous);
        var failed = WindowsCpuSampler.Calculate(current with { SystemError = 5, Idle100ns = null }, previous);
        var initial = WindowsCpuSampler.Calculate(current, null);
        if (grouped.Fact.Value.HasValue || failed.Fact.Value.HasValue || initial.Fact.Value.HasValue ||
            failed.Fact.NativeCode != 5 || grouped.Fact.Reason is null || initial.Fact.Reason is null)
            throw new InvalidOperationException("Incomplete CPU source coverage was reported as a usable zero.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void RenderGpuRequiresUniqueBdfAndHardwareIdsInsteadOfEnumerationOrder()
    {
        var adapter = new RenderAdapterIdentity { Luid = 1, Bus = 5, Device = 0, Function = 0,
            VendorId = 0x10de, DeviceId = 0x1234, SubSystemId = 3, Status = Success(), AddressStatus = Success() };
        var unavailable = NvmlStatus.FromCode(NvmlNative.NotSupported);
        var counter = new NvmlCounterInterval { Cumulative = null, Delta = null, IntervalStartMs = null,
            ReadStartMs = 10, ReadEndMs = 20, Reset = false, ReadGap = true, DriverReferenceUs = null, Status = unavailable };
        var slow = new NvmlSlowSample
        {
            QueryStartMs = 10, QueryEndMs = 20, TotalMemoryMiB = new(8192, Success()),
            GpuUtilFraction = new(null, unavailable), MemoryUtilFraction = new(null, unavailable),
            WholeGpuMemoryMiB = new(null, unavailable), GpuClockMHz = new(null, unavailable), GpuMaxClockMHz = new(null, unavailable),
            MemoryClockMHz = new(null, unavailable), PowerW = new(null, unavailable), TemperatureC = new(null, unavailable),
            PerformanceState = new(null, unavailable), ThrottleReasonBits = new(null, unavailable), Energy = counter, Policies = []
        };
        NvmlDeviceSample Device(uint index, uint bus) => new(new NvmlIdentity(index, $"gpu-{index}", null, Success(), Success(), Success())
        { PciBus = bus, PciDevice = 0, PciFunction = 0, PciVendorId = adapter.VendorId,
            PciDeviceId = adapter.DeviceId, PciSubSystemId = adapter.SubSystemId }, slow, []);
        var first = Device(0, 6) with { Slow = slow with { TotalMemoryMiB = new(4096, Success()) } };
        var actual = Device(1, 5);
        if (adapter.Match([first, actual]) != actual || adapter.Match([actual, Device(2, 5)]) is not null ||
            (adapter with { Bus = null }).Match([actual]) is not null)
            throw new InvalidOperationException("Rendering-adapter association guessed an index or an ambiguous PCI identity.");
        var manifest = new JsonObject { ["machine"] = new JsonObject { ["vramMiB"] = null }, ["missingReasons"] = new JsonArray("other") };
        foreach (var devices in new NvmlDeviceSample[][] { [first], [actual, Device(2, 5)],
            [actual with { Slow = slow with { TotalMemoryMiB = new(null, unavailable) } }],
            [actual with { Slow = slow with { TotalMemoryMiB = new(slow.TotalMemoryMiB.Value, unavailable) } }],
            [actual with { Slow = slow with { TotalMemoryMiB = new(null, Success()) } }], [actual with { Slow = null }] })
        {
            var missing = PerfHardwareCapture.RenderMemory(adapter, new(10, 20, Success(), devices), out _);
            PerfRecordingSession.SetHardwareCapacity(manifest, missing);
            var saved = JsonNode.Parse(manifest.ToJsonString(PerfFieldDictionary.JsonOptions))!;
            if (saved["machine"]!["vramMiB"] is not null || missing.Status.Reason is null ||
                saved["missingReasons"]!.AsArray().Count != 2 ||
                !saved["missingReasons"]!.AsArray().Any(reason => reason?.GetValue<string>() == "machine.vramMiB: " + missing.Status.Reason))
                throw new InvalidOperationException("Missing or ambiguous hardware became a capacity, or accumulated stale reasons.");
        }
        var memory = PerfHardwareCapture.RenderMemory(adapter, new(10, 20, Success(), [first, actual]), out var matched);
        if (matched != actual || memory.Value is null || first.Slow!.TotalMemoryMiB.Value == memory.Value)
            throw new InvalidOperationException("The fixture did not exercise a positive capacity on the non-first rendering GPU.");
        PerfRecordingSession.SetHardwareCapacity(manifest, memory);
        var recorded = JsonNode.Parse(manifest.ToJsonString(PerfFieldDictionary.JsonOptions))!;
        if (recorded["machine"]!["vramMiB"]!.GetValue<double>() != actual.Slow!.TotalMemoryMiB.Value ||
            recorded["missingReasons"]!.AsArray().Count != 1)
            throw new InvalidOperationException("The production manifest writer lost the matched NVML total or kept a stale missing reason.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task HardwareWorkerStopsJoinsAndReleasesBeforeReportingCompletion()
    {
        var observed = new TaskCompletionSource<HardwareSamplingBatch>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new HardwareSampler(() => System.Environment.TickCount64, value => observed.TrySetResult(value),
            1_000, new RenderAdapterIdentity());
        try
        {
            var sample = await observed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            worker.RequestStop();
            var stopped = await worker.StopAsync();
            var repeated = await worker.StopAsync();
            if (!worker.Completion.IsCompleted || !ReferenceEquals(stopped, repeated) || stopped.Error is not null ||
                sample.Cpu.Fact.Source != WindowsCpuSampler.Source || sample.GpuEngines.Fact.Source != WindowsGpuEngineSampler.Source)
                throw new InvalidOperationException("Hardware producer did not finish its owned source cleanup and join.");
        }
        finally { await worker.StopAsync(); }
    }
}

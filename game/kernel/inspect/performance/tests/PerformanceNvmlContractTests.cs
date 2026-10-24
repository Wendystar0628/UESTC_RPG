using Campus.Contracts;
using Campus.Kernel.Inspect;
using GdUnit4;

namespace Campus.Tests;

// Pure value tests: constructing the native producer is deliberately outside the CLR test host.
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
}

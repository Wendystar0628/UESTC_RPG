using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Kernel.Inspect;

namespace Campus.Tests;

/// <summary>Synthetic native batches and temporary recording directories shared by performance and stream suites.</summary>
internal static class PerformanceTestSupport
{
    private static int _directorySequence;
    internal static string TemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"uestc-perf-{Environment.ProcessId}-{Interlocked.Increment(ref _directorySequence)}");
        Directory.CreateDirectory(path);
        return path;
    }

    internal static NvmlStatus Success() => NvmlStatus.FromCode(NvmlNative.Success);
    internal static NvmlValue<ulong> Reading(ulong value) => new(value, Success());
    internal static NvmlNative.Sample Sample(ulong timestamp, uint value) => new()
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

}

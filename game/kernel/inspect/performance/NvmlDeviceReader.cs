namespace Campus.Kernel.Inspect;

/// <summary>One worker owns every counter and cursor for one physical GPU.</summary>
internal sealed class NvmlDeviceReader(IntPtr handle, NvmlIdentity identity)
{
    private readonly NvmlCounterTracker _energy = new();
    private readonly Dictionary<NvmlPolicy, NvmlCounterTracker> _policies =
        Enum.GetValues<NvmlPolicy>().ToDictionary(policy => policy, _ => new NvmlCounterTracker());
    private readonly Dictionary<NvmlMetric, NvmlSampleCursor> _cursors =
        Enum.GetValues<NvmlMetric>().ToDictionary(metric => metric, _ => new NvmlSampleCursor());

    public NvmlDeviceSample Read(Func<double> clock, Func<bool> stopping)
    {
        if (handle == IntPtr.Zero) return new(identity, null, []);
        var slow = ReadSlow(clock, stopping);
        var fast = new List<NvmlFastBatch>();
        foreach (var metric in Enum.GetValues<NvmlMetric>())
        {
            if (stopping()) break;
            double start = clock();
            var cursor = _cursors[metric];
            var raw = NvmlNative.Samples(handle, metric, cursor.LastSeenUs, stopping);
            fast.Add(cursor.Observe(metric, raw.Status, raw.Samples, start, clock()));
        }
        return new(identity, slow, fast.ToArray());
    }

    private NvmlSlowSample ReadSlow(Func<double> clock, Func<bool> stopping)
    {
        NvmlValue<T> Read<T>(Func<NvmlValue<T>> query) where T : struct => stopping()
            ? new(null, new("unavailable", "Sampling stopped before query")) : query();
        double start = clock();
        var utilization = Read(() => NvmlNative.UtilizationInfo(handle));
        var memory = Read(() => NvmlNative.MemoryInfo(handle));
        var graphicsClock = Read(() => NvmlNative.Clock(handle, 0));
        var maximumClock = Read(() => NvmlNative.Clock(handle, 0, maximum: true));
        var memoryClock = Read(() => NvmlNative.Clock(handle, 2));
        var power = Read(() => NvmlNative.Power(handle));
        var temperature = Read(() => NvmlNative.Temperature(handle));
        var state = Read(() => NvmlNative.State(handle));
        var reasons = Read(() => NvmlNative.ClockReasons(handle));
        double energyStart = clock();
        var energy = Read(() => NvmlNative.Energy(handle));
        var energyInterval = _energy.Observe(energy, energyStart, clock());
        var policies = new List<NvmlPolicyInterval>();
        foreach (var policy in Enum.GetValues<NvmlPolicy>())
        {
            if (stopping()) break;
            double policyStart = clock();
            var value = NvmlNative.Policy(handle, policy);
            var reading = new NvmlValue<ulong>(value.Value?.Nanoseconds, value.Status);
            policies.Add(new(policy, _policies[policy].Observe(reading, policyStart, clock(), value.Value?.ReferenceUs)));
        }
        return new()
        {
            QueryStartMs = start, QueryEndMs = clock(),
            GpuUtilFraction = new(utilization.Value?.Gpu / 100d, utilization.Status),
            MemoryUtilFraction = new(utilization.Value?.Memory / 100d, utilization.Status),
            WholeGpuMemoryMiB = new(memory.Value?.Used / 1048576d, memory.Status),
            TotalMemoryMiB = new(memory.Value?.Total / 1048576d, memory.Status),
            GpuClockMHz = graphicsClock, GpuMaxClockMHz = maximumClock, MemoryClockMHz = memoryClock,
            PowerW = new(power.Value / 1000d, power.Status), TemperatureC = temperature,
            PerformanceState = state, ThrottleReasonBits = reasons,
            Energy = energyInterval, Policies = policies.ToArray()
        };
    }
}

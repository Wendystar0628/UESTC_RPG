namespace Campus.Kernel.Inspect;

// ENG-PERF-DATA: detached hardware values; only the recording writer serializes them.
internal sealed record NvmlStatus(string Capability, string? Reason = null, int? NativeCode = null,
    Exception? Error = null)
{
    public bool IsSuccess => NativeCode == NvmlNative.Success;
    public static NvmlStatus FromCode(int code) => code switch
    {
        NvmlNative.Success => new("supported", NativeCode: code),
        NvmlNative.NotSupported => new("unsupported", "NVML_ERROR_NOT_SUPPORTED", code),
        _ => new("unavailable", "NVML query failed", code)
    };
    public static NvmlStatus SampleResult(int code) => code == NvmlNative.NotFound
        ? new("supported", "NVML_ERROR_NOT_FOUND: no new samples", code) : FromCode(code);
    public static NvmlStatus Missing([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] Exception error) =>
        new("unavailable", error.GetType().Name, Error: error);
}

internal readonly record struct NvmlValue<T>(T? Value, NvmlStatus Status) where T : struct;
internal sealed record NvmlIdentity(uint EnumerationIndex, string? Uuid, string? PciBusId,
    NvmlStatus HandleStatus, NvmlStatus UuidStatus, NvmlStatus PciStatus)
{
    public uint? PciBus { get; init; }
    public uint? PciDevice { get; init; }
    public uint? PciFunction { get; init; }
    public uint? PciVendorId { get; init; }
    public uint? PciDeviceId { get; init; }
    public uint? PciSubSystemId { get; init; }
}
internal enum NvmlMetric { Power = 0, GpuUtilization = 1, MemoryUtilization = 2 }
internal enum NvmlPolicy { Power = 0, Thermal = 1, SyncBoost = 2, BoardLimit = 3, LowUtilization = 4, Reliability = 5 }

// Each cumulative counter has its own successful-read interval; policies may overlap.
internal sealed record NvmlCounterInterval
{
    public required ulong? Cumulative { get; init; }
    public required ulong? Delta { get; init; }
    public required double? IntervalStartMs { get; init; }
    public required double ReadStartMs { get; init; }
    public required double ReadEndMs { get; init; }
    public required bool Reset { get; init; }
    public required bool ReadGap { get; init; }
    public required ulong? DriverReferenceUs { get; init; }
    public required NvmlStatus Status { get; init; }
    public double? ElapsedMs => IntervalStartMs is { } start ? ReadEndMs - start : null;
}
internal sealed record NvmlPolicyInterval(NvmlPolicy Policy, NvmlCounterInterval Counter)
{
    public double? ViolationMs => Counter.Delta / 1_000_000d;
}
internal sealed record NvmlSlowSample
{
    public required double QueryStartMs { get; init; }
    public required double QueryEndMs { get; init; }
    public required NvmlValue<double> GpuUtilFraction { get; init; }
    public required NvmlValue<double> MemoryUtilFraction { get; init; }
    public required NvmlValue<double> WholeGpuMemoryMiB { get; init; }
    public required NvmlValue<double> TotalMemoryMiB { get; init; }
    public required NvmlValue<uint> GpuClockMHz { get; init; }
    public required NvmlValue<uint> GpuMaxClockMHz { get; init; }
    public required NvmlValue<uint> MemoryClockMHz { get; init; }
    public required NvmlValue<double> PowerW { get; init; }
    public required NvmlValue<uint> TemperatureC { get; init; }
    public required NvmlValue<uint> PerformanceState { get; init; }
    public required NvmlValue<ulong> ThrottleReasonBits { get; init; }
    public required NvmlCounterInterval Energy { get; init; }
    public required IReadOnlyList<NvmlPolicyInterval> Policies { get; init; }
    public double? EnergyJ => Energy.Delta / 1000d;
    public double? EnergyAveragePowerW => Energy.ElapsedMs is > 0 ? Energy.Delta / Energy.ElapsedMs : null;
}

internal readonly record struct NvmlFastPoint(ulong DriverTimestampUs, double Value, double? ObservedIntervalMs);
internal sealed record NvmlFastBatch
{
    public required NvmlMetric Metric { get; init; }
    public required NvmlStatus Status { get; init; }
    public required double QueryStartMs { get; init; }
    public required double QueryEndMs { get; init; }
    public required double? PreviousSuccessfulReadMs { get; init; }
    public required double? SuccessfulReadSpanMs { get; init; }
    public required ulong CursorBeforeUs { get; init; }
    public required ulong CursorAfterUs { get; init; }
    public required double? ObservedBufferSpanMs { get; init; }
    public required IReadOnlyList<NvmlFastPoint> Samples { get; init; }
    public required IReadOnlyList<string> ContinuityReasons { get; init; }
    // No NVML API here supplies a lost-sample count or a promised sampling period.
    public long? LostSampleCount => null;
}
internal sealed record NvmlDeviceSample(NvmlIdentity Identity, NvmlSlowSample? Slow,
    IReadOnlyList<NvmlFastBatch> Fast);
internal sealed record NvmlBatch(double QueryStartMs, double QueryEndMs, NvmlStatus Status,
    IReadOnlyList<NvmlDeviceSample> Devices);

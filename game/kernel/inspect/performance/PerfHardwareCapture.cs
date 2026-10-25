using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Kernel.Frame;
using Campus.Kernel.Diagnostics;
using Campus.Contracts.Perf;

namespace Campus.Kernel.Inspect;

/// <summary>ENG-PERF-DATA: NVML supplies counters Godot cannot expose. This owner joins the native
/// producer before its two value channels close; only the session writer touches files or JSON manifests.</summary>
internal sealed class PerfHardwareCapture
{
    private readonly object _gate = new();
    private readonly PerfValueChannel _slow, _fast;
    private readonly Dictionary<uint, double> _previousQuery = new();
    private readonly Dictionary<uint, object> _devices = new();
    private readonly Dictionary<string, Exception> _errors = new();
    private readonly HardwareSampler _sampler;
    private readonly string _sessionId;
    private PerfSamplingFacts _facts;
    private RenderAdapterIdentity _adapter;
    private double? _processCpuFraction;
    private bool _renderDeviceMatched;
    private NvmlStatus _status = new("starting", "Awaiting NVML initialization");
    private NvmlValue<double> _renderVramMiB = new(null, new("starting", "Awaiting hardware sample"));
    private NvmlStatus? _shutdown;
    private int? _pdhCloseCode;
    private string _slowState = "starting", _fastState = "starting";
    private bool _slowRecorded, _fastRecorded, _finished;
    private int _revision;
    public Task Completion { get; }
    public bool Joined { get { lock (_gate) return _finished; } }
    public int Revision => Volatile.Read(ref _revision);

    public PerfHardwareCapture(PerfValueChannel slow, PerfValueChannel fast, double pollMilliseconds,
        string sessionId, RenderAdapterIdentity adapter)
    {
        _slow = slow;
        _fast = fast;
        _sessionId = sessionId;
        _adapter = adapter;
        var starting = new PerfSamplingFact { Capability = PerfFactCapability.Starting, Reason = "Awaiting hardware sample" };
        _facts = new(sessionId, adapter.Luid, null, starting, starting, starting);
        _sampler = new HardwareSampler(() => FrameClock.MonotonicMilliseconds, Publish, pollMilliseconds, adapter);
        Completion = ObserveAsync(_sampler);
    }

    public void RequestStop() => _sampler.RequestStop();
    public PerfSamplingFacts SamplingFacts { get { lock (_gate) return _facts; } }
    public double? ProcessCpuFraction { get { lock (_gate) return _processCpuFraction; } }

    public async Task StopAsync()
    {
        // Native calls are not cancellable: StopAsync joins after the worker's own shutdown.
        await _sampler.StopAsync().ConfigureAwait(false);
        await Completion.ConfigureAwait(false);
        lock (_gate) { _finished = true; Interlocked.Increment(ref _revision); }
    }

    public Exception? Error
    {
        get
        {
            lock (_gate)
            {
                Exception? failure = null;
                foreach (var error in _errors.Values) failure = RetainedFailure.Append(failure, error);
                return failure;
            }
        }
    }

    public string StreamState(string stream)
    {
        lock (_gate)
        {
            if (stream == "hw" ? _slowRecorded : _fastRecorded) return "recorded";
            return _status.Capability == "supported" ? stream == "hw" ? _slowState : _fastState : _status.Capability;
        }
    }

    // Called exclusively by the IO owner, which receives a detached snapshot under the producer lock.
    public JsonNode Snapshot(out NvmlValue<double> renderVramMiB)
    {
        lock (_gate)
        {
            renderVramMiB = _renderVramMiB;
            return JsonSerializer.SerializeToNode(new
            {
                state = _status.Capability, status = StatusValue(_status), shutdown = _shutdown is null ? null : StatusValue(_shutdown),
                joined = _finished, pdhCloseCode = _pdhCloseCode, slowRecorded = _slowRecorded, fastRecorded = _fastRecorded,
                renderDeviceAssociation = _renderDeviceMatched ? "matched" : "unknown",
                renderDeviceAssociationReason = !_renderDeviceMatched ? "Actual rendering adapter requires a unique BDF and hardware-ID NVML match." : null,
                renderAdapter = AdapterValue(_adapter), samplingFacts = _facts,
                devices = _devices.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray(),
                retainedExceptionCount = _errors.Count
            }, PerfFieldDictionary.JsonOptions)!;
        }
    }

    private async Task ObserveAsync(HardwareSampler sampler)
    {
        var result = await sampler.Completion.ConfigureAwait(false);
        lock (_gate)
        {
            _shutdown = result.NvmlShutdownStatus;
            _pdhCloseCode = result.PdhCloseCode;
            if (result.NvmlShutdownStatus?.Error is { } shutdownError) RetainFailure("shutdown", shutdownError);
            if (result.Error is { } error)
            {
                RetainFailure("worker", error);
                _status = new("unavailable", "Hardware worker failed", Error: error);
                _renderVramMiB = new(null, _status);
                var unavailable = new PerfSamplingFact { Capability = PerfFactCapability.Unavailable,
                    Reason = "Hardware worker failed", Source = "recording.hardware" };
                _facts = new(_sessionId, _adapter.Luid, null, unavailable, unavailable, unavailable);
            }
            else if (_status.Capability == "starting")
            {
                _status = new("unavailable", "Sampling stopped before initialization completed");
                _renderVramMiB = new(null, _status);
            }
            Interlocked.Increment(ref _revision);
        }
    }

    private void Publish(HardwareSamplingBatch sampling)
    {
        lock (_gate)
        {
            try { PublishBatch(sampling); }
            finally { _slow.Flush(); _fast.Flush(); }
        }
    }

    private void PublishBatch(HardwareSamplingBatch sampling)
    {
        var batch = sampling.Nvml;
        _adapter = sampling.Adapter;
        ObserveStatus("renderAdapter", _adapter.Status);
        ObserveStatus("renderAdapterAddress", _adapter.AddressStatus);
        _renderVramMiB = RenderMemory(_adapter, batch, out var renderDevice);
        _renderDeviceMatched = renderDevice is not null;
        _processCpuFraction = sampling.Cpu.ProcessFraction;
        _facts = new(_sessionId, _adapter.Luid, renderDevice?.Identity.Uuid,
            NvmlFact(renderDevice, batch.Status, performanceState: true), NvmlFact(renderDevice, batch.Status, performanceState: false),
            sampling.GpuEngines.Fact);
        _status = batch.Status;
        ObserveStatus("session", batch.Status);
        if (batch.Devices.Count == 0)
        {
            var values = new Dictionary<string, object?>
            {
                ["rowKind"] = "nvml",
                ["queryStartMs"] = batch.QueryStartMs, ["queryEndMs"] = batch.QueryEndMs,
                ["statusJson"] = new { session = StatusValue(batch.Status) }
            };
            _slow.TryRow(new("hw", null, batch.QueryEndMs, values));
            _fast.TryRow(new("hwfast", null, batch.QueryEndMs, new Dictionary<string, object?>(values)
            { ["capability"] = batch.Status.Capability }));
        }
        // Reads are sequential, but a missing handle has no slow query and uses the batch end.
        // Keep equal-time devices stable and publish the completed batch facts last.
        foreach (var device in batch.Devices.OrderBy(device => device.Slow?.QueryEndMs ?? batch.QueryEndMs))
            PublishDevice(batch, device, renderDevice?.Identity.EnumerationIndex == device.Identity.EnumerationIndex);
        bool factsAccepted = _slow.TryRow(FactsRow(sampling, _facts, renderDevice?.Identity));
        _slowRecorded |= factsAccepted && (_processCpuFraction.HasValue || _facts.OtherProcessesGpuUtilFraction.Value.HasValue);
        _slowState = Capability(batch.Devices.SelectMany(device => DeviceStatuses(device))
            .Where(pair => !pair.Key.StartsWith("fast.", StringComparison.Ordinal) &&
                pair.Key is not ("handle" or "gpuUuid" or "pciBusId")).Select(pair => pair.Value));
        _fastState = Capability(batch.Devices.SelectMany(device => device.Fast).Select(fast => fast.Status));
        Interlocked.Increment(ref _revision);
    }

    internal static NvmlValue<double> RenderMemory(RenderAdapterIdentity adapter, NvmlBatch batch,
        out NvmlDeviceSample? device)
    {
        device = adapter.Match(batch.Devices);
        if (!batch.Status.IsSuccess) return new(null, batch.Status);
        if (!adapter.Status.IsSuccess) return new(null, adapter.Status);
        if (!adapter.AddressStatus.IsSuccess) return new(null, adapter.AddressStatus);
        if (device is null)
            return new(null, new("unavailable", "Actual rendering adapter has no unique NVML BDF/hardware-ID match"));
        if (!device.Identity.HandleStatus.IsSuccess) return new(null, device.Identity.HandleStatus);
        if (!device.Identity.PciStatus.IsSuccess) return new(null, device.Identity.PciStatus);
        if (device.Slow is not { } slow)
            return new(null, new("unavailable", "Matched NVML device has no memory sample"));
        var memory = slow.TotalMemoryMiB;
        if (!memory.Status.IsSuccess || memory.Status.Capability != "supported") return new(null, memory.Status);
        return memory.Value is > 0 && double.IsFinite(memory.Value.Value) ? memory :
            new(null, new("unavailable", "NVML total-memory query returned no positive finite capacity", memory.Status.NativeCode));
    }

    private void PublishDevice(NvmlBatch batch, NvmlDeviceSample device, bool isRenderAdapter)
    {
        var identity = device.Identity;
        var statuses = DeviceStatuses(device);
        foreach (var (field, status) in statuses) ObserveStatus($"{identity.EnumerationIndex}.{field}", status);
        _devices[identity.EnumerationIndex] = new
        {
            deviceIndex = identity.EnumerationIndex, gpuUuid = identity.Uuid, pciBusId = identity.PciBusId,
            totalMemoryMiB = device.Slow?.TotalMemoryMiB.Value,
            fields = statuses.ToDictionary(pair => pair.Key, pair => StatusValue(pair.Value))
        };
        _previousQuery.TryGetValue(identity.EnumerationIndex, out double previous);
        var row = SlowRow(batch, device, _previousQuery.ContainsKey(identity.EnumerationIndex) ? previous : null, statuses);
        var values = new Dictionary<string, object?>(row.Values) { ["rowKind"] = "nvml", ["isRenderAdapter"] = isRenderAdapter };
        bool accepted = _slow.TryRow(row with { Values = values });
        _previousQuery[identity.EnumerationIndex] = device.Slow?.QueryStartMs ?? batch.QueryStartMs;
        _slowRecorded |= accepted && device.Slow is { } slow && HasSlowValues(slow);
        foreach (var fast in device.Fast)
        {
            ObserveStatus($"{identity.EnumerationIndex}.{MetricName(fast.Metric)}", fast.Status);
            if (_fast.TryFastBatch(identity, fast) && fast.Samples.Count != 0) _fastRecorded = true;
        }
    }

    private static PerfSamplingFact NvmlFact(NvmlDeviceSample? device, NvmlStatus session, bool performanceState)
    {
        var slow = device?.Slow;
        NvmlStatus status = slow is null ? session : performanceState ? slow.PerformanceState.Status : slow.GpuUtilFraction.Status;
        double? value = slow is null ? null : performanceState ? slow.PerformanceState.Value : slow.GpuUtilFraction.Value;
        return new()
        {
            Value = value,
            Capability = value.HasValue ? PerfFactCapability.Supported : status.Capability == "unsupported"
                ? PerfFactCapability.Unsupported : PerfFactCapability.Unavailable,
            Reason = value.HasValue ? null : device is null && session.Capability == "supported"
                ? "Actual rendering adapter has no unique NVML BDF/hardware-ID match" : status.Reason,
            Source = performanceState ? "NVML.nvmlDeviceGetPerformanceState" : "NVML.nvmlDeviceGetUtilizationRates",
            NativeCode = status.NativeCode, QueryStartMs = slow?.QueryStartMs, QueryEndMs = slow?.QueryEndMs
            // NVML does not expose the actual utilization averaging window; it must stay null.
        };
    }

    private static PerfCapturedRow FactsRow(HardwareSamplingBatch sampling, PerfSamplingFacts facts, NvmlIdentity? identity) =>
        new("hw", null, sampling.Nvml.QueryEndMs, new Dictionary<string, object?>
        {
            ["rowKind"] = "samplingFacts", ["deviceIndex"] = identity?.EnumerationIndex,
            ["gpuUuid"] = identity?.Uuid, ["pciBusId"] = identity?.PciBusId,
            ["queryStartMs"] = sampling.Nvml.QueryStartMs, ["queryEndMs"] = sampling.Nvml.QueryEndMs,
            ["wholeGpuUtilFraction"] = facts.WholeGpuUtilFraction.Value,
            ["otherProcessesGpuUtilFraction"] = facts.OtherProcessesGpuUtilFraction.Value,
            ["samplingFactsJson"] = facts,
            ["cpuCountersJson"] = new { current = sampling.Cpu.Current, previous = sampling.Cpu.Previous,
                sampling.Cpu.ProcessFraction, sampling.Cpu.Reason, source = WindowsCpuSampler.Source },
            ["gpuEnginesJson"] = new { sampling.GpuEngines.CollectTimestamp100ns, sampling.GpuEngines.PreviousCollectMs, sampling.GpuEngines.RawQueryCode,
                sampling.GpuEngines.Recording },
            ["renderAdapterJson"] = AdapterValue(sampling.Adapter)
        });

    private static object AdapterValue(RenderAdapterIdentity adapter) => new
    {
        adapter.Luid, adapter.LuidText, adapter.VendorId, adapter.DeviceId, adapter.SubSystemId,
        adapter.Bus, adapter.Device, adapter.Function, status = StatusValue(adapter.Status),
        addressStatus = StatusValue(adapter.AddressStatus), adapter.CloseStatus
    };

    internal static PerfCapturedRow SlowRow(NvmlBatch batch, NvmlDeviceSample device, double? previousQuery,
        IReadOnlyDictionary<string, NvmlStatus>? statuses = null)
    {
        var values = IdentityValues(device.Identity);
        values["rowKind"] = "nvml";
        var slow = device.Slow;
        values["intervalStartMs"] = previousQuery;
        values["queryStartMs"] = slow?.QueryStartMs ?? batch.QueryStartMs;
        values["queryEndMs"] = slow?.QueryEndMs ?? batch.QueryEndMs;
        var statusValues = (statuses ?? DeviceStatuses(device)).ToDictionary(pair => pair.Key, pair => StatusValue(pair.Value));
        if (slow is not null) statusValues["energyCounter"] = CounterValue(slow.Energy);
        values["statusJson"] = statusValues;
        if (slow is null) return new("hw", null, batch.QueryEndMs, values);
        values["gpuUtilFraction"] = slow.GpuUtilFraction.Value;
        values["gpuMemoryUtilFraction"] = slow.MemoryUtilFraction.Value;
        values["wholeGpuMemoryMiB"] = slow.WholeGpuMemoryMiB.Value;
        values["gpuClockMHz"] = slow.GpuClockMHz.Value;
        values["gpuMaxClockMHz"] = slow.GpuMaxClockMHz.Value;
        values["memoryClockMHz"] = slow.MemoryClockMHz.Value;
        values["powerW"] = slow.PowerW.Value;
        values["temperatureC"] = slow.TemperatureC.Value;
        values["pstate"] = slow.PerformanceState.Value is { } state ? $"P{state}" : null;
        values["throttleReasonBits"] = slow.ThrottleReasonBits.Value;
        values["energyJ"] = slow.EnergyJ;
        values["energyCumulativeJ"] = slow.Energy.Cumulative / 1000d;
        values["energyAveragePowerW"] = slow.EnergyAveragePowerW;
        values["energyIntervalStartMs"] = slow.Energy.IntervalStartMs;
        values["energyReadStartMs"] = slow.Energy.ReadStartMs;
        values["energyReadEndMs"] = slow.Energy.ReadEndMs;
        values["energyIntervalMs"] = slow.Energy.ElapsedMs;
        values["energyReset"] = slow.Energy.Reset;
        values["energyReadGap"] = slow.Energy.ReadGap;
        values["policyIntervalsJson"] = slow.Policies.Select(policy => new
        {
            policy = PolicyName(policy.Policy), counter = CounterValue(policy.Counter), policy.ViolationMs
        }).ToArray();
        values["violationPowerMs"] = slow.Policies.FirstOrDefault(policy => policy.Policy == NvmlPolicy.Power)?.ViolationMs;
        values["violationThermalMs"] = slow.Policies.FirstOrDefault(policy => policy.Policy == NvmlPolicy.Thermal)?.ViolationMs;
        values["violationLowUtilMs"] = slow.Policies.FirstOrDefault(policy => policy.Policy == NvmlPolicy.LowUtilization)?.ViolationMs;
        return new("hw", null, slow.QueryEndMs, values);
    }

    internal static IEnumerable<PerfCapturedRow> FastRows(NvmlIdentity identity, NvmlFastBatch batch)
    {
        // Even NOT_FOUND and unsupported queries retain their cursor and query-span metadata.
        if (batch.Samples.Count == 0) yield return FastRow(identity, batch, null);
        else foreach (var point in batch.Samples) yield return FastRow(identity, batch, point);
    }

    private static PerfCapturedRow FastRow(NvmlIdentity identity, NvmlFastBatch batch, NvmlFastPoint? point)
    {
        var values = IdentityValues(identity);
        values["driverTimestampUs"] = point?.DriverTimestampUs;
        values["queryStartMs"] = batch.QueryStartMs;
        values["queryEndMs"] = batch.QueryEndMs;
        values["observedIntervalMs"] = point?.ObservedIntervalMs;
        values["metric"] = MetricName(batch.Metric);
        values["value"] = point?.Value;
        values["capability"] = batch.Status.Capability;
        values["statusJson"] = new { handle = StatusValue(identity.HandleStatus), uuid = StatusValue(identity.UuidStatus),
            pci = StatusValue(identity.PciStatus), metric = StatusValue(batch.Status) };
        values["previousSuccessfulReadMs"] = batch.PreviousSuccessfulReadMs;
        values["successfulReadSpanMs"] = batch.SuccessfulReadSpanMs;
        values["observedBufferSpanMs"] = batch.ObservedBufferSpanMs;
        values["cursorBeforeUs"] = batch.CursorBeforeUs;
        values["cursorAfterUs"] = batch.CursorAfterUs;
        values["lostSampleCount"] = batch.LostSampleCount;
        values["continuityReasonsJson"] = batch.ContinuityReasons.ToArray();
        return new("hwfast", null, batch.QueryEndMs, values);
    }

    private static Dictionary<string, object?> IdentityValues(NvmlIdentity identity) => new()
    {
        ["deviceIndex"] = identity.EnumerationIndex, ["gpuUuid"] = identity.Uuid, ["pciBusId"] = identity.PciBusId
    };

    private static Dictionary<string, NvmlStatus> DeviceStatuses(NvmlDeviceSample device)
    {
        var statuses = new Dictionary<string, NvmlStatus>
        {
            ["handle"] = device.Identity.HandleStatus, ["gpuUuid"] = device.Identity.UuidStatus, ["pciBusId"] = device.Identity.PciStatus,
            ["processVideoMemoryMiB"] = new("notAttached", "NVML process video-memory authority is not attached"),
            ["availableRamMiB"] = new("notAttached", "System RAM sampler is not attached")
        };
        if (device.Slow is { } slow)
        {
            statuses["gpuUtilFraction"] = slow.GpuUtilFraction.Status;
            statuses["gpuMemoryUtilFraction"] = slow.MemoryUtilFraction.Status;
            statuses["wholeGpuMemoryMiB"] = slow.WholeGpuMemoryMiB.Status;
            statuses["totalMemoryMiB"] = slow.TotalMemoryMiB.Status;
            statuses["gpuClockMHz"] = slow.GpuClockMHz.Status;
            statuses["gpuMaxClockMHz"] = slow.GpuMaxClockMHz.Status;
            statuses["memoryClockMHz"] = slow.MemoryClockMHz.Status;
            statuses["powerW"] = slow.PowerW.Status;
            statuses["temperatureC"] = slow.TemperatureC.Status;
            statuses["pstate"] = slow.PerformanceState.Status;
            statuses["throttleReasonBits"] = slow.ThrottleReasonBits.Status;
            statuses["energy"] = slow.Energy.Status;
            foreach (var policy in slow.Policies) statuses[PolicyName(policy.Policy)] = policy.Counter.Status;
        }
        foreach (var fast in device.Fast) statuses["fast." + MetricName(fast.Metric)] = fast.Status;
        return statuses;
    }

    private static bool HasSlowValues(NvmlSlowSample slow) => slow.GpuUtilFraction.Value.HasValue ||
        slow.MemoryUtilFraction.Value.HasValue || slow.WholeGpuMemoryMiB.Value.HasValue || slow.GpuClockMHz.Value.HasValue ||
        slow.GpuMaxClockMHz.Value.HasValue || slow.MemoryClockMHz.Value.HasValue || slow.PowerW.Value.HasValue ||
        slow.TemperatureC.Value.HasValue || slow.PerformanceState.Value.HasValue || slow.ThrottleReasonBits.Value.HasValue ||
        slow.Energy.Cumulative.HasValue || slow.Policies.Any(policy => policy.Counter.Cumulative.HasValue);

    private static string Capability(IEnumerable<NvmlStatus> statuses)
    {
        var capabilities = statuses.Select(status => status.Capability).Where(value => value != "notAttached").ToArray();
        if (capabilities.Contains("supported")) return "supported";
        return capabilities.Length > 0 && capabilities.All(value => value == "unsupported") ? "unsupported" : "unavailable";
    }

    private void ObserveStatus(string field, NvmlStatus status)
    {
        if (status.Error is { } error && !NvmlNative.IsLoadFailure(error)) RetainFailure(field, error);
    }

    // Keep the first original per query site; repeated missing entry points cannot grow this without bound.
    private void RetainFailure(string field, [FailureHandler("ENG-PERF-DATA")] Exception error) => _errors.TryAdd(field, error);

    private static object StatusValue(NvmlStatus status) => new
    { status.Capability, status.Reason, status.NativeCode, exceptionType = status.Error?.GetType().FullName };

    private static object CounterValue(NvmlCounterInterval counter) => new
    {
        counter.Cumulative, counter.Delta, counter.IntervalStartMs, counter.ReadStartMs, counter.ReadEndMs,
        counter.ElapsedMs, counter.Reset, counter.ReadGap, counter.DriverReferenceUs, status = StatusValue(counter.Status)
    };

    private static string MetricName(NvmlMetric metric) => metric switch
    {
        NvmlMetric.Power => "powerW", NvmlMetric.GpuUtilization => "gpuUtilFraction",
        NvmlMetric.MemoryUtilization => "gpuMemoryUtilFraction", _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };

    private static string PolicyName(NvmlPolicy policy) => policy switch
    {
        NvmlPolicy.Power => "power", NvmlPolicy.Thermal => "thermal", NvmlPolicy.SyncBoost => "syncBoost",
        NvmlPolicy.BoardLimit => "boardLimit", NvmlPolicy.LowUtilization => "lowUtilization",
        NvmlPolicy.Reliability => "reliability", _ => throw new ArgumentOutOfRangeException(nameof(policy))
    };
}

using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Inspect;

/// <summary>
/// Application recording session owns this producer and must await StopAsync before draining its writer.
/// ENG-PERF-DATA: DLL/device/field absence is reported as nullable hardware, not a frame-stream fault.
/// The supplied clock is the recording authority (FrameClock); callbacks receive detached values only.
/// </summary>
internal sealed record HardwareSamplingBatch(NvmlBatch Nvml, CpuSamplingRead Cpu,
    GpuEngineSamplingRead GpuEngines, RenderAdapterIdentity Adapter);
internal sealed record HardwareSamplingCompletion(NvmlStatus? NvmlShutdownStatus, int? PdhCloseCode, Exception? Error);

internal sealed class HardwareSampler
{
    private readonly double _pollMilliseconds;
    private readonly Func<double> _clock;
    private readonly Action<HardwareSamplingBatch> _publish;
    private readonly RenderAdapterIdentity _adapter;
    private readonly ManualResetEventSlim _stop = new(false);
    private readonly object _stopGate = new();
    private readonly Thread _thread;
    private readonly TaskCompletionSource<HardwareSamplingCompletion> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<HardwareSamplingCompletion>? _joined;
    private bool _stopRequested;

    public HardwareSampler(Func<double> monotonicMilliseconds, Action<HardwareSamplingBatch> publish, double pollMilliseconds,
        RenderAdapterIdentity adapter)
    {
        ArgumentNullException.ThrowIfNull(monotonicMilliseconds);
        ArgumentNullException.ThrowIfNull(publish);
        _pollMilliseconds = pollMilliseconds;
        _clock = monotonicMilliseconds;
        _publish = publish;
        _adapter = adapter;
        _thread = new Thread(Run) { IsBackground = true, Name = "Performance hardware" };
        try { _thread.Start(); }
        catch { _stop.Dispose(); throw; }
    }

    // A session observes this result even if it has not yet received a user stop request.
    // It still calls StopAsync to join and release the wait handle.
    public Task<HardwareSamplingCompletion> Completion => _completion.Task;

    public void RequestStop()
    {
        lock (_stopGate)
        {
            if (_stopRequested) return;
            _stopRequested = true;
            _stop.Set();
        }
    }

    public Task<HardwareSamplingCompletion> StopAsync()
    {
        RequestStop();
        lock (_stopGate)
            return _joined ??= Task.Run(() =>
            {
                // Native calls cannot be cancelled safely. Join waits for their real return and shutdown.
                _thread.Join();
                _stop.Dispose();
                return _completion.Task.GetAwaiter().GetResult();
            });
    }

    private void Run()
    {
        bool initialized = false;
        NvmlStatus? shutdown = null;
        Exception? failure = null;
        WindowsGpuEngineSampler? gpu = null;
        try
        {
            if (_stop.IsSet) return;
            var cpu = new WindowsCpuSampler();
            gpu = new WindowsGpuEngineSampler();
            RenderAdapterIdentity adapter;
            try { adapter = _adapter.ResolveAddress(); }
            catch (Exception error) when (NvmlNative.IsLoadFailure(error))
            { adapter = _adapter with { AddressStatus = NvmlStatus.Missing(error) }; }
            var devices = InitializeNvml(out var status, out initialized);
            if (_stop.IsSet) return;
            while (!_stop.IsSet)
            {
                double queryStart = _clock();
                var cpuRead = cpu.Read(_clock);
                var gpuRead = gpu.Read(adapter.LuidText, (uint)System.Environment.ProcessId, _clock);
                var samples = new List<NvmlDeviceSample>();
                foreach (var device in devices)
                {
                    if (_stop.IsSet) break;
                    samples.Add(device.Read(_clock, () => _stop.IsSet));
                }
                double queryEnd = _clock();
                _publish(new(new(queryStart, queryEnd, status, samples.ToArray()), cpuRead, gpuRead, adapter));
                // Slow queries do not create catch-up bursts or fictitious 1-second intervals.
                double remaining = _pollMilliseconds - (queryEnd - queryStart);
                if (_stop.Wait(TimeSpan.FromMilliseconds(Math.Max(0, remaining)))) break;
            }
        }
        catch (Exception error) { failure = ReceiveFailure(error); }
        finally
        {
            if (gpu is not null)
            {
                try
                {
                    gpu.Dispose();
                    if (gpu.CloseCode is not (null or 0))
                        failure = RetainedFailure.Append(failure, new InvalidOperationException($"PdhCloseQuery failed: {gpu.CloseCode}"));
                }
                catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
            }
            if (initialized)
            {
                try { shutdown = NvmlStatus.FromCode(NvmlNative.Shutdown()); }
                catch (Exception error)
                {
                    shutdown = NvmlStatus.Missing(error);
                    failure = RetainedFailure.Append(failure, error);
                }
            }
            _completion.TrySetResult(new(shutdown, gpu?.CloseCode, failure));
        }
    }

    private static Exception ReceiveFailure([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] Exception error) => error;

    private List<NvmlDeviceReader> InitializeNvml(out NvmlStatus status, out bool initialized)
    {
        try { status = NvmlStatus.FromCode(NvmlNative.Init()); }
        catch (Exception error) when (NvmlNative.IsLoadFailure(error)) { status = NvmlStatus.Missing(error); }
        initialized = status.IsSuccess;
        if (!initialized || _stop.IsSet) return [];
        var count = NvmlNative.Read(() => { int code = NvmlNative.GetCount(out uint value); return (code, value); });
        if (count.Value is > 0) return Enumerate(count.Value.Value, () => _stop.IsSet);
        status = count.Value == 0 ? new("unavailable", "No NVIDIA device enumerated") : count.Status;
        return [];
    }

    private static List<NvmlDeviceReader> Enumerate(uint count, Func<bool> stopping)
    {
        var devices = new List<NvmlDeviceReader>();
        for (uint index = 0; index < count; index++)
        {
            if (stopping()) break;
            var handle = NvmlNative.Read(() => { int code = NvmlNative.GetHandle(index, out var value); return (code, value); });
            NvmlIdentity identity = handle.Value is { } device
                ? NvmlNative.Identity(index, device)
                : new(index, null, null, handle.Status, handle.Status, handle.Status);
            devices.Add(new(handle.Value ?? IntPtr.Zero, identity));
        }
        return devices;
    }
}

namespace Campus.Kernel.Inspect;

/// <summary>
/// Application recording session owns this producer and must await StopAsync before draining its writer.
/// ENG-PERF-DATA: DLL/device/field absence is reported as nullable hardware, not a frame-stream fault.
/// The supplied clock is the recording authority (FrameClock); callbacks receive detached values only.
/// </summary>
internal sealed class NvmlSampler
{
    // ENG-PERF-DATA: both slow reads and driver-buffer queries occur at 1 Hz. This is not driver resolution.
    private const int PollMilliseconds = 1000;
    private readonly Func<double> _clock;
    private readonly Action<NvmlBatch> _publish;
    private readonly ManualResetEventSlim _stop = new(false);
    private readonly object _stopGate = new();
    private readonly Thread _thread;
    private readonly TaskCompletionSource<NvmlCompletion> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<NvmlCompletion>? _joined;
    private bool _stopRequested;

    public NvmlSampler(Func<double> monotonicMilliseconds, Action<NvmlBatch> publish)
    {
        ArgumentNullException.ThrowIfNull(monotonicMilliseconds);
        ArgumentNullException.ThrowIfNull(publish);
        _clock = monotonicMilliseconds;
        _publish = publish;
        _thread = new Thread(Run) { IsBackground = true, Name = "Performance NVML" };
        try { _thread.Start(); }
        catch { _stop.Dispose(); throw; }
    }

    // A session observes this result even if it has not yet received a user stop request.
    // It still calls StopAsync to join and release the wait handle.
    public Task<NvmlCompletion> Completion => _completion.Task;

    public void RequestStop()
    {
        lock (_stopGate)
        {
            if (_stopRequested) return;
            _stopRequested = true;
            _stop.Set();
        }
    }

    public Task<NvmlCompletion> StopAsync()
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
        try
        {
            if (_stop.IsSet) return;
            double start = _clock();
            NvmlStatus status;
            try { status = NvmlStatus.FromCode(NvmlNative.Init()); }
            catch (Exception error) when (NvmlNative.IsLoadFailure(error)) { status = NvmlStatus.Missing(error); }
            initialized = status.IsSuccess;
            if (!initialized)
            {
                _publish(new(start, _clock(), status, []));
                return;
            }
            if (_stop.IsSet) return;
            var count = NvmlNative.Read(() => { int code = NvmlNative.GetCount(out uint value); return (code, value); });
            if (count.Value is not > 0)
            {
                status = count.Value == 0 ? new("unavailable", "No NVIDIA device enumerated") : count.Status;
                _publish(new(start, _clock(), status, []));
                return;
            }
            var devices = Enumerate(count.Value.Value, () => _stop.IsSet);
            while (!_stop.IsSet)
            {
                double queryStart = _clock();
                var samples = new List<NvmlDeviceSample>();
                foreach (var device in devices)
                {
                    if (_stop.IsSet) break;
                    samples.Add(device.Read(_clock, () => _stop.IsSet));
                }
                double queryEnd = _clock();
                if (samples.Count > 0) _publish(new(queryStart, queryEnd, status, samples.ToArray()));
                // Slow queries do not create catch-up bursts or fictitious 1-second intervals.
                double remaining = PollMilliseconds - (queryEnd - queryStart);
                if (_stop.Wait(TimeSpan.FromMilliseconds(Math.Max(0, remaining)))) break;
            }
        }
        catch (Exception error) { failure = ReceiveFailure(error); }
        finally
        {
            if (initialized)
            {
                try { shutdown = NvmlStatus.FromCode(NvmlNative.Shutdown()); }
                catch (Exception error)
                {
                    shutdown = NvmlStatus.Missing(error);
                    failure = failure is null ? error : new AggregateException(failure, error);
                }
            }
            _completion.TrySetResult(new(shutdown, failure));
        }
    }

    private static Exception ReceiveFailure([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] Exception error) => error;

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

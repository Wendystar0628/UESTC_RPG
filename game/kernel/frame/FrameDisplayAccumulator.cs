using Campus.Contracts;

namespace Campus.Kernel.Frame;

/// <summary>Unclipped observation windows; rendering averages count confirmed measurements only.</summary>
internal sealed class FrameDisplayAccumulator
{
    internal const double WindowMilliseconds = 500;
    private ulong _sequence;
    private int _frames, _cpuCount, _gpuCount;
    private double _elapsed, _slowest, _cpu, _gpu;

    internal bool Add(in FrameSample sample, out FrameDisplayWindow window)
    {
        window = default;
        if (sample.FrameMilliseconds is not { } interval || !double.IsFinite(interval) || interval < 0) return false;
        _frames++;
        _elapsed += interval;
        _slowest = Math.Max(_slowest, interval);
        if (sample.MainViewport.CpuRenderMilliseconds is { } cpu) { _cpu += cpu; _cpuCount++; }
        if (sample.MainViewport.GpuRenderMilliseconds is { } gpu) { _gpu += gpu; _gpuCount++; }
        if (_elapsed < WindowMilliseconds) return false;
        window = new()
        {
            Sequence = ++_sequence, FrameCount = _frames, IntervalMilliseconds = _elapsed,
            FramesPerSecond = 1000 * _frames / _elapsed, AverageFrameMilliseconds = _elapsed / _frames,
            SlowestFrameMilliseconds = _slowest,
            MainViewportCpuMilliseconds = _cpuCount == 0 ? null : _cpu / _cpuCount,
            MainViewportGpuMilliseconds = _gpuCount == 0 ? null : _gpu / _gpuCount
        };
        _frames = _cpuCount = _gpuCount = 0;
        _elapsed = _slowest = _cpu = _gpu = 0;
        return true;
    }
}

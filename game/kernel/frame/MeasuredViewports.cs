using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Frame;

/// <summary>Owns measurement leases and current-row draw reports, not the viewport resources.</summary>
internal sealed class MeasuredViewports(Rid mainViewport) : IMeasuredViewportRegistry, IDisposable
{
    private sealed class Registration(MeasuredViewports owner, Rid viewport, string role) : IDisposable
    {
        internal readonly Rid Viewport = viewport;
        internal readonly string Role = role;
        internal ulong? ReportFrame;
        internal bool ReportDrawn;
        internal bool Removed;
        public void Dispose() => owner.Remove(this);
    }

    private readonly List<Registration> _registrations = [];
    private ViewportFrameSample[] _samples = [];
    private int _sampleCount;
    private bool _disposed;
    internal ReadOnlySpan<ViewportFrameSample> Samples => _samples.AsSpan(0, _sampleCount);
    internal ViewportFrameSample MainSample { get; private set; }

    public IDisposable Register(Rid viewport, string role)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        foreach (var item in _registrations)
            if (item.Viewport == viewport) throw new InvalidOperationException("Viewport is already measured.");
        RenderingServer.ViewportSetMeasureRenderTime(viewport, true);
        var registration = new Registration(this, viewport, role);
        _registrations.Add(registration);
        if (_samples.Length < _registrations.Count) Array.Resize(ref _samples, _registrations.Count);
        return registration;
    }

    public void ReportDraw(Rid viewport, ulong collectionFrame, bool drawn)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var item in _registrations)
        {
            if (item.Viewport != viewport) continue;
            item.ReportFrame = collectionFrame;
            item.ReportDrawn = drawn;
            return;
        }
        throw new InvalidOperationException("Draw report requires a measured viewport registration.");
    }

    internal void Sample(ulong collectionFrame)
    {
        _sampleCount = _registrations.Count;
        MainSample = default;
        for (var i = 0; i < _sampleCount; i++)
        {
            var item = _registrations[i];
            bool? report = item.ReportFrame == collectionFrame ? item.ReportDrawn : null;
            // DECISION-342: only the continuously drawn main viewport supplies approximate costs.
            // An owner request never establishes exact readback freshness or a source draw frame.
            var measure = item.Viewport == mainViewport && report is true;
            var sample = new ViewportFrameSample
            {
                ViewportId = item.Viewport.Id, Role = item.Role,
                DrewThisFrame = report is false ? false : null,
                CpuRenderMilliseconds = measure ? Cost(RenderingServer.ViewportGetMeasuredRenderTimeCpu(item.Viewport)) : null,
                GpuRenderMilliseconds = measure ? Cost(RenderingServer.ViewportGetMeasuredRenderTimeGpu(item.Viewport)) : null
            };
            _samples[i] = sample;
            if (item.Viewport == mainViewport) MainSample = sample;
        }
    }

    private static double? Cost(double value) => double.IsFinite(value) && value >= 0 ? value : null;

    private void Remove(Registration registration)
    {
        if (registration.Removed) return;
        registration.Removed = true;
        RenderingServer.ViewportSetMeasureRenderTime(registration.Viewport, false);
        _registrations.Remove(registration);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (var i = _registrations.Count - 1; i >= 0; i--) Remove(_registrations[i]);
        _sampleCount = 0;
    }
}

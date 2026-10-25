using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Stage;

/// <summary>Application-lifetime callback wiring; measurement facts remain owned by Kernel.Frame.</summary>
internal sealed class StageFrameSampling : IDisposable
{
    private readonly FrameStatistics _statistics;
    private readonly SceneTree _tree;
    private readonly FrameClockNode _clock;
    private readonly IAppFlow _flow;
    private readonly IObserverState _observer;
    private readonly ICameraRig _camera;
    private readonly IInputRouter _input;
    private readonly IStreamStatistics _stream;
    private bool _disposed;

    internal StageFrameSampling(FrameStatistics statistics, SceneTree tree, FrameClockNode clock,
        IAppFlow flow, IObserverState observer, ICameraRig camera, IInputRouter input)
    {
        _statistics = statistics; _tree = tree; _clock = clock;
        _flow = flow; _observer = observer; _camera = camera; _input = input;
        _stream = KernelServices.Get<IStreamStatistics>();
        tree.PhysicsFrame += Physics;
        clock.ProcessStarted += statistics.Begin;
        RenderingServer.FramePreDraw += PreDraw;
        RenderingServer.FramePostDraw += PostDraw;
    }

    internal void Sample()
    {
        if (_disposed) return;
        var frame = Engine.GetProcessFrames();
        var now = FrameClock.MonotonicMilliseconds;
        var context = new FrameContextSample(_flow.State, _flow.Activity,
            _input.DesktopState.WindowFocused, DisplayServer.WindowCanDraw(), _tree.Paused);
        _statistics.Sample(frame, now, in context, _observer.YawDeg, _camera.Pose.PitchDeg, _stream.Statistics);
    }

    private void Physics() => _statistics.Physics(Engine.GetProcessFrames(), FrameClock.MonotonicMilliseconds);
    private void PreDraw() => _statistics.PreDraw(Engine.GetProcessFrames(), FrameClock.MonotonicMilliseconds);
    private void PostDraw() => _statistics.PostDraw(Engine.GetProcessFrames(), FrameClock.MonotonicMilliseconds);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tree.PhysicsFrame -= Physics;
        _clock.ProcessStarted -= _statistics.Begin;
        RenderingServer.FramePreDraw -= PreDraw;
        RenderingServer.FramePostDraw -= PostDraw;
    }
}

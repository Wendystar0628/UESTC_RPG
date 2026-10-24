using System.Numerics;
using Campus.Contracts;

namespace Campus.Gates;

/// <summary>Checks observed route samples; native input owns motion, so this type never writes a pose.</summary>
internal sealed class StartupRouteProgress
{
    // Readback allows a small support correction, not a different starting location.
    internal const float PositionTolerance = .1f;
    private readonly Pose _requested;
    private readonly Vector3 _forward;
    private readonly ViewMode _mode;
    private readonly double _durationMilliseconds;
    private Vector3 _previous;
    private double _previousMilliseconds;

    public float Distance { get; private set; }
    public float ForwardDistance { get; private set; }
    public double ElapsedMilliseconds => _previousMilliseconds;

    public StartupRouteProgress(Pose requested, ViewMode mode, double durationMilliseconds,
        Vector3 actual, float actualYaw, ViewMode actualMode)
    {
        if (!double.IsFinite(durationMilliseconds) || durationMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        _ = mode switch { ViewMode.Walk or ViewMode.Fly => true, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        _requested = requested;
        _forward = Angles.Forward(requested.YawDeg, 0);
        _mode = mode;
        _durationMilliseconds = durationMilliseconds;
        RequireMode(actualMode);
        RequireFinite(actual);
        if (Vector3.Distance(requested.Position, actual) > PositionTolerance)
            throw new InvalidOperationException("Route actual start does not match the requested map point.");
        var yawDelta = MathF.IEEERemainder(actualYaw - requested.YawDeg, 360);
        if (!float.IsFinite(yawDelta) || MathF.Abs(yawDelta) > .1f)
            throw new InvalidOperationException("Route actual heading does not match the requested map point.");
        _previous = actual;
    }

    public void Observe(Vector3 actual, ViewMode mode, double elapsedMilliseconds)
    {
        RequireMode(mode);
        RequireFinite(actual);
        if (!double.IsFinite(elapsedMilliseconds) || elapsedMilliseconds <= _previousMilliseconds)
            throw new InvalidOperationException("Route trajectory did not advance in time.");
        var step = actual - _previous;
        var forward = Vector3.Dot(step, _forward);
        if (forward <= PositionTolerance)
            throw new InvalidOperationException("Route forward input did not sustain positive movement between trajectory samples.");
        Distance += step.Length();
        ForwardDistance = Vector3.Dot(actual - _requested.Position, _forward);
        _previous = actual;
        _previousMilliseconds = elapsedMilliseconds;
    }

    public void Complete()
    {
        if (_previousMilliseconds < _durationMilliseconds || Distance <= PositionTolerance || ForwardDistance <= PositionTolerance)
            throw new InvalidOperationException("Route ended before its requested duration and positive displacement were observed.");
    }

    public void RequireMode(ViewMode actual)
    {
        if (actual != _mode) throw new InvalidOperationException($"Route mode changed: requested {_mode}, actual {actual}.");
    }

    private static void RequireFinite(Vector3 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
            throw new InvalidOperationException("Route observed a non-finite position.");
    }
}

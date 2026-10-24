using System.Numerics;
using Campus.Contracts;

namespace Campus.Gates;

/// <summary>Checks observed route samples; native input owns motion, so this type never writes a pose.</summary>
internal sealed class StartupRouteProgress
{
    internal const float PositionTolerance = .1f;
    private const float HeadingTolerance = .1f;
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
        RequireStart(requested, actual, actualYaw);
        _previous = actual;
    }

    internal static string? StartFailure(Pose requested, Vector3 actual, float actualYaw)
    {
        if (!VectorMath.Finite(actual)) return "Route observed a non-finite position.";
        if (Vector3.Distance(requested.Position, actual) > PositionTolerance)
            return "Route actual start does not match the requested map point.";
        var yawDelta = MathF.IEEERemainder(actualYaw - requested.YawDeg, 360);
        return !float.IsFinite(yawDelta) || MathF.Abs(yawDelta) > HeadingTolerance
            ? "Route actual heading does not match the requested map point." : null;
    }

    internal static void RequireStart(Pose requested, Vector3 actual, float actualYaw)
    {
        if (StartFailure(requested, actual, actualYaw) is { } failure) throw new InvalidOperationException(failure);
    }

    public string? ObservationFailure(Vector3 actual, ViewMode mode, double elapsedMilliseconds)
    {
        if (mode != _mode) return $"Route mode changed: requested {_mode}, actual {mode}.";
        if (!VectorMath.Finite(actual)) return "Route observed a non-finite position.";
        if (!double.IsFinite(elapsedMilliseconds) || elapsedMilliseconds <= _previousMilliseconds)
            return "Route trajectory did not advance in time.";
        return Vector3.Dot(actual - _previous, _forward) <= PositionTolerance
            ? "Route forward input did not sustain positive movement between trajectory samples." : null;
    }

    public void Observe(Vector3 actual, ViewMode mode, double elapsedMilliseconds)
    {
        if (ObservationFailure(actual, mode, elapsedMilliseconds) is { } failure) throw new InvalidOperationException(failure);
        Distance += (actual - _previous).Length();
        ForwardDistance = Vector3.Dot(actual - _requested.Position, _forward);
        _previous = actual;
        _previousMilliseconds = elapsedMilliseconds;
    }

    public string? CompletionFailure() => _previousMilliseconds < _durationMilliseconds ||
        Distance <= PositionTolerance || ForwardDistance <= PositionTolerance
        ? "Route ended before its requested duration and positive displacement were observed." : null;

    public void Complete()
    {
        if (CompletionFailure() is { } failure) throw new InvalidOperationException(failure);
    }

    public void RequireMode(ViewMode actual)
    {
        if (actual != _mode) throw new InvalidOperationException($"Route mode changed: requested {_mode}, actual {actual}.");
    }
}

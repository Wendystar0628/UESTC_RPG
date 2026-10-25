using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;

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
    private readonly bool _allowTurns;
    private Vector3 _previous;
    private double _previousMilliseconds;
    private readonly HashSet<TileKey> _tiles = [];
    private readonly HashSet<LodLevel> _mountedLevels = [];
    public float Distance { get; private set; }
    public float ForwardDistance { get; private set; }
    public double ElapsedMilliseconds => _previousMilliseconds;
    public int TileCount => _tiles.Count;
    public IReadOnlyCollection<LodLevel> MountedLevels => _mountedLevels;
    public bool EnteredInterior { get; private set; }

    public StartupRouteProgress(Pose requested, ViewMode mode, double durationMilliseconds,
        Vector3 actual, float actualYaw, ViewMode actualMode, bool allowTurns = false)
    {
        if (!double.IsFinite(durationMilliseconds) || durationMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        _ = mode switch { ViewMode.Walk or ViewMode.Fly => true, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        _requested = requested;
        _forward = Angles.Forward(requested.YawDeg, 0);
        _mode = mode;
        _durationMilliseconds = durationMilliseconds;
        _allowTurns = allowTurns;
        RequireMode(actualMode);
        RequireStart(requested, actual, actualYaw);
        _previous = actual;
        _tiles.Add(TileGrid.At(actual));
    }

    public void ObserveContext(Vector3 actual, NavSpace? space, IWorldModel world)
    {
        _tiles.Add(TileGrid.At(actual));
        if (space is { Building: { } building } indoor && world.Interiors.IsOpen(building, indoor.Floor, actual))
            EnteredInterior = true;
    }

    public void ObserveTile(StreamTileChange change)
    {
        if (change.Kind == StreamMountKind.Visual && change.Change == StreamTileChangeKind.Mounted &&
            change.Key.Detail.StartsWith('L')) _mountedLevels.Add(BakeRequestKeys.Parse(change.Key).Level);
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
        return (_allowTurns ? Vector3.Distance(actual, _previous) : Vector3.Dot(actual - _previous, _forward)) <= PositionTolerance
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
        Distance <= PositionTolerance || !_allowTurns && ForwardDistance <= PositionTolerance
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

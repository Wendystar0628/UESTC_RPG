using System.Numerics;
using Campus.Contracts;

namespace Campus.Camera.Walk;

/// <summary>Latest-first snapshots. Clearing on map unload prevents returning to a previous map.</summary>
public sealed class WalkSafetyRing
{
    private readonly Pose[] _points = new Pose[WalkTuning.SafePointCapacity];
    private int _next;
    public int Count { get; private set; }

    public void Clear() { _next = 0; Count = 0; }

    public void Add(Pose point)
    {
        if (!WalkMath.Finite(point.Position) || !float.IsFinite(point.YawDeg) || !float.IsFinite(point.PitchDeg))
            throw new ArgumentOutOfRangeException(nameof(point));
        _points[_next] = point;
        _next = (_next + 1) % _points.Length;
        Count = Math.Min(Count + 1, _points.Length);
    }

    public IReadOnlyList<Pose> Snapshot()
    {
        var result = new Pose[Count];
        for (int i = 0; i < Count; i++) result[i] = Latest(i);
        return result;
    }

    public void Restore(IReadOnlyList<Pose> points)
    {
        Clear();
        for (int i = Math.Min(points.Count, _points.Length) - 1; i >= 0; i--) Add(points[i]);
    }

    public Pose? Nearest(Vector3 from, float minimumDistance, int skipLatest, Func<Pose, bool> accept)
    {
        Pose? nearest = null;
        float best = float.PositiveInfinity;
        for (int i = Math.Max(0, skipLatest); i < Count; i++)
        {
            var candidate = Latest(i);
            float distance = Vector3.DistanceSquared(from, candidate.Position);
            if (distance < minimumDistance * minimumDistance || distance >= best || !accept(candidate)) continue;
            best = distance;
            nearest = candidate;
        }
        return nearest;
    }

    public int IndexOf(Pose point)
    {
        for (int i = 0; i < Count; i++)
            if (Latest(i) == point) return i;
        return -1;
    }

    private Pose Latest(int offset) => _points[(_next - 1 - offset + _points.Length) % _points.Length];
}

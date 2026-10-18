using System.Numerics;
using Campus.Contracts;

namespace Campus.Camera.Walk;

/// <summary>Bounded chronological history without a second saved cursor.</summary>
public sealed class WalkSafePoints
{
    private readonly List<Pose> _points = new(WalkTuning.SafetyPointCapacity);

    public IEnumerable<Pose> Points => _points;
    public int Count => _points.Count;
    public void Clear() => _points.Clear();

    public void Record(Pose point)
    {
        if (!WalkSafetyQueries.ValidPose(point)) return;
        // Repeated samples of the same support must not consume history while standing still,
        // including the native collision margin settling after a restore.
        if (_points.Count > 0 && Vector3.DistanceSquared(_points[^1].Position, point.Position)
            <= WalkTuning.SafeMargin * WalkTuning.SafeMargin) return;
        if (_points.Count == WalkTuning.SafetyPointCapacity) _points.RemoveAt(0);
        _points.Add(point);
    }

    public Pose[] Capture() => _points.ToArray();

    public Pose? Nearest(Vector3 from, float minimumDistance = 0, int skipNewest = 0)
    {
        Pose? result = null;
        float distance = float.PositiveInfinity;
        int remaining = _points.Count - skipNewest;
        foreach (var point in _points)
        {
            if (--remaining < 0) break;
            float candidate = Vector3.DistanceSquared(from, point.Position);
            if (candidate < minimumDistance * minimumDistance || candidate >= distance) continue;
            result = point;
            distance = candidate;
        }
        return result;
    }
}

using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Water;

namespace Campus.Systems.Water;

/// <summary>One reflection candidate: lake number, water level and area-weighted sample points on its surface.</summary>
public sealed record ReflectionLake(int Slot, FeatureId Id, float WaterY, IReadOnlyList<Vector3> Samples, float SampleArea);

/// <summary>
/// Picks the lake that gets the planar reflection this frame: the candidate with the largest estimated screen area,
/// switching only when another lake is clearly larger (WLD-WATER: 换湖带回差).
/// </summary>
public sealed class ReflectionLakes
{
    public const float Hysteresis = 1.3f;
    public const int AreaSamples = 48;
    public ReflectionLakes(IReadOnlyList<ReflectionLake> lakes) => Lakes = lakes;

    public IReadOnlyList<ReflectionLake> Lakes { get; }

    public static ReflectionLakes FromBodies(IReadOnlyList<WaterReflectionSurface> bodies, int samples)
    {
        var lakes = new List<ReflectionLake>();
        foreach (var body in bodies.Where(b => b.ReflectSlot >= 0).OrderBy(b => b.ReflectSlot))
        {
            var points = SamplePoints(body, samples);
            if (points.Count == 0) continue;
            lakes.Add(new(body.ReflectSlot, body.Id, body.SurfaceY, points, body.Area / points.Count));
        }
        return new(lakes);
    }

    /// <summary>
    /// Solid-angle estimate per lake: each sample counts area × |cos| / distance² when the camera sees it.
    /// The caller owns the applied lake; querying another view never changes that selection.
    /// Frustum planes point outward and use Normal dot point + D at most zero inside.
    /// </summary>
    public int Select(Vector3 camera, ReadOnlySpan<Plane> frustum, int previousSlot)
    {
        var best = -1;
        var bestScore = 0f;
        var currentScore = 0f;
        foreach (var lake in Lakes)
        {
            var score = 0f;
            foreach (var s in lake.Samples)
            {
                if (camera.Y <= lake.WaterY || !Visible(s, frustum)) continue;
                var d = s - camera;
                var d2 = MathF.Max(d.LengthSquared(), 1f);
                score += lake.SampleArea * (camera.Y - lake.WaterY) / (d2 * MathF.Sqrt(d2));
            }
            if (lake.Slot == previousSlot) currentScore = score;
            if (score > bestScore) { bestScore = score; best = lake.Slot; }
        }
        if (best < 0)
        {
            foreach (var lake in Lakes)
                if (lake.Slot == previousSlot) return previousSlot;
            return Lakes.Count > 0 ? Lakes[0].Slot : -1;
        }
        return previousSlot < 0 || currentScore <= 0f || bestScore > currentScore * Hysteresis ? best : previousSlot;
    }

    private static bool Visible(Vector3 point, ReadOnlySpan<Plane> frustum)
    {
        foreach (var plane in frustum)
            if (Plane.DotCoordinate(plane, point) > 0f) return false;
        return true;
    }

    /// <summary>Shared planar mirror geometry for both rendering and streaming demand.</summary>
    public static Vector3 ReflectPoint(Vector3 point, float waterY) => new(point.X, 2f * waterY - point.Y, point.Z);
    public static Vector3 ReflectDirection(Vector3 direction) => new(direction.X, -direction.Y, direction.Z);
    public static Plane ReflectPlane(Plane plane, float waterY) =>
        new(ReflectDirection(plane.Normal), plane.D + 2f * waterY * plane.Normal.Y);

    public float WaterY(int slot)
    {
        foreach (var lake in Lakes)
            if (lake.Slot == slot) return lake.WaterY;
        return float.NegativeInfinity;
    }

    private static List<Vector3> SamplePoints(WaterReflectionSurface body, int target)
    {
        var points = new List<Vector3>();
        var outer = body.Rings.Outer.Select(p => new Godot.Vector2(p.X, p.Y)).ToArray();
        var holes = body.Rings.Holes.Select(ring => ring.Select(p => new Godot.Vector2(p.X, p.Y)).ToArray()).ToArray();
        var spacing = MathF.Sqrt(MathF.Max(body.Area, 1f) / MathF.Max(target, 1));
        for (var step = spacing; points.Count == 0 && step > 0.25f; step *= 0.5f)
            for (var z = body.Min.Y + step * 0.5f; z < body.Max.Y; z += step)
                for (var x = body.Min.X + step * 0.5f; x < body.Max.X; x += step)
                {
                    var p = new Godot.Vector2(x, z);
                    var inside = Godot.Geometry2D.IsPointInPolygon(p, outer);
                    foreach (var hole in holes) inside ^= Godot.Geometry2D.IsPointInPolygon(p, hole);
                    if (inside) points.Add(new(x, body.SurfaceY, z));
                }
        return points;
    }
}

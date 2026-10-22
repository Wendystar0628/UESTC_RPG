using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace Campus.Gen.Ground;

/// <summary>
/// World-level preprocessing shared by every tile: source shapes per surface class, a static bounds index and the road
/// centre lines. Built once per world model (read only) and cached; thread safe after construction.
/// </summary>
internal sealed class GroundLayout
{
    private readonly STRtree<int> _index = new();

    public IReadOnlyList<GroundShape> Shapes { get; }
    public IReadOnlyList<RoadLine> Roads { get; }
    /// <summary>Plaza and walk sources that carry a surface pattern and their metre frames (GAME-015); empty when none qualifies.</summary>
    public GroundPaving Paving { get; private init; } = GroundPaving.Empty;
    public Vector2 Min { get; }
    public Vector2 Max { get; }
    public bool IsEmpty => Shapes.Count == 0;

    private GroundLayout(List<GroundShape> shapes, List<RoadLine> roads)
    {
        Shapes = shapes;
        Roads = roads;
        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        for (var s = 0; s < shapes.Count; s++)
        {
            var shape = shapes[s];
            min = Vector2.Min(min, shape.Min);
            max = Vector2.Max(max, shape.Max);
            _index.Insert(new Envelope(shape.Min.X, shape.Max.X, shape.Min.Y, shape.Max.Y), s);
        }
        _index.Build();
        Min = shapes.Count > 0 ? min : Vector2.Zero;
        Max = shapes.Count > 0 ? max : Vector2.Zero;
    }

    public static GroundLayout Of(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return DerivedMemo.ForWorld(world, typeof(GroundLayout), () => Create(world));
    }

    public static GroundLayout Of(BuildContext context) => Of(context.World);

    /// <summary>Shapes whose bounds overlap the box. Order is the stable shape order (sorted by feature ID).</summary>
    public List<GroundShape> Near(Vector2 min, Vector2 max)
    {
        var indices = _index.Query(new Envelope(min.X, max.X, min.Y, max.Y)).ToList();
        indices.Sort();
        return indices.Select(s => Shapes[s]).ToList();
    }

    /// <summary>Exact signed distance (negative inside) to the union of one surface class, clamped to ±limit.</summary>
    public float SignedDistance(SurfaceKind kind, Vector2 p, float limit)
    {
        var best = limit;
        foreach (var shape in Near(p - new Vector2(limit), p + new Vector2(limit)))
        {
            if (shape.Kind != kind) continue;
            best = MathF.Min(best, Sdf.Shape(shape, p, limit));
        }
        return best;
    }

    private static GroundLayout Create(IWorldModel world)
    {
        var (shapes, roads) = GroundSources.Read(world);
        return new GroundLayout(shapes, roads) { Paving = GroundPaving.Build(world, shapes) };
    }
}

/// <summary>Exact point queries against one shape; the lattice rasteriser in <see cref="TileField"/> must agree with these.</summary>
internal static class Sdf
{
    public static float Shape(GroundShape shape, Vector2 p, float limit)
    {
        if (shape.Line is { } line)
        {
            var d = double.MaxValue;
            for (var i = 0; i + 1 < line.Length; i++) d = Math.Min(d, PlanLines.SegmentDistance(p.X, p.Y, line[i], line[i + 1]));
            return Math.Clamp((float)(d - shape.HalfWidth), -limit, limit);
        }
        var rings = shape.Rings!;
        var best = double.MaxValue;
        var inside = false;
        foreach (var ring in rings)
            for (var i = 0; i < ring.Length; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Length];
                best = Math.Min(best, PlanLines.SegmentDistance(p.X, p.Y, a, b));
                if ((a.Y <= p.Y) != (b.Y <= p.Y))
                {
                    var x = a.X + (p.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                    if (x < p.X) inside = !inside;
                }
            }
        return Math.Clamp(inside ? (float)-best : (float)best, -limit, limit);
    }

}

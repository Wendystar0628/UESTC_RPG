using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>
/// World-level preprocessing shared by every tile: source shapes per surface class, a 64 m bin index and the road
/// centre lines. Built once per world model (read only) and cached; thread safe after construction.
/// </summary>
internal sealed class GroundLayout
{
    private const float BinMargin = 32f;
    private static readonly ConditionalWeakTable<IWorldModel, GroundLayout> Cache = new();

    private readonly Dictionary<long, int[]> _bins;
    private readonly object _markLock = new();
    private RoadMarkings? _markings;

    public IReadOnlyList<GroundShape> Shapes { get; }
    public IReadOnlyList<RoadLine> Roads { get; }
    /// <summary>Plaza sources that carry a surface pattern and their metre frames (Q-015); empty when none qualifies.</summary>
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
        var bins = new Dictionary<long, List<int>>();
        for (var s = 0; s < shapes.Count; s++)
        {
            var shape = shapes[s];
            min = Vector2.Min(min, shape.Min);
            max = Vector2.Max(max, shape.Max);
            var b0 = Bin(shape.Min - new Vector2(BinMargin));
            var b1 = Bin(shape.Max + new Vector2(BinMargin));
            for (var bz = b0.Z; bz <= b1.Z; bz++)
                for (var bx = b0.X; bx <= b1.X; bx++)
                {
                    var key = Key(bx, bz);
                    if (!bins.TryGetValue(key, out var list)) bins.Add(key, list = []);
                    list.Add(s);
                }
        }
        _bins = bins.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        Min = shapes.Count > 0 ? min : Vector2.Zero;
        Max = shapes.Count > 0 ? max : Vector2.Zero;
    }

    public static GroundLayout Of(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Cache.GetValue(world, Create);
    }

    /// <summary>Markings are derived from the network once per world; they need exact distance queries.</summary>
    public RoadMarkings Markings(GroundSection section)
    {
        lock (_markLock)
            return _markings ??= RoadMarkings.Build(this, section);
    }



    /// <summary>Shapes whose bounds overlap the box. Order is the stable shape order (sorted by feature ID).</summary>
    public List<GroundShape> Near(Vector2 min, Vector2 max)
    {
        var result = new List<GroundShape>();
        var b0 = Bin(min);
        var b1 = Bin(max);
        var seen = new HashSet<int>();
        var indices = new List<int>();
        for (var bz = b0.Z; bz <= b1.Z; bz++)
            for (var bx = b0.X; bx <= b1.X; bx++)
                if (_bins.TryGetValue(Key(bx, bz), out var list))
                    foreach (var s in list)
                        if (seen.Add(s)) indices.Add(s);
        indices.Sort();
        foreach (var s in indices)
        {
            var shape = Shapes[s];
            if (shape.Max.X < min.X || shape.Min.X > max.X || shape.Max.Y < min.Y || shape.Min.Y > max.Y) continue;
            result.Add(shape);
        }
        return result;
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

    private static (int X, int Z) Bin(Vector2 p)
    {
        var cell = TileGrid.At(p);
        return (cell.X, cell.Z);
    }
    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
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

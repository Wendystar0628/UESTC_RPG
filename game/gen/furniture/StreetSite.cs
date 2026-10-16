using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

/// <summary>
/// The world as the furniture placer sees it, indexed on a coarse grid: blocking outlines (buildings, water, courts, parking),
/// road centre lines with their widths, bridges, network trees and the things already placed. Pure and deterministic: every
/// list is filled in feature-ID order and grid lookups never depend on dictionary iteration order.
/// </summary>
internal sealed class StreetSite
{
    private const float Cell = 32f;
    private static readonly string[] Blocking = ["building", "water", "sport", "parking"];

    private readonly Dictionary<(int, int), List<Outline>> _outlines = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _roads = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _bridges = [];
    private readonly Dictionary<(int, int), List<Vector2>> _trees = [];
    private readonly Dictionary<(int, int), List<(Vector2 P, float R)>> _occupied = [];

    public StreetSite(IWorldModel world)
    {
        World = world;
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            foreach (var kind in Blocking)
                foreach (var rings in Polygons(feature, kind))
                {
                    var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                    var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                    Index(_outlines, min, max, new Outline(rings, min, max));
                }
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal)) IndexLine(_roads, road);
        foreach (var bridge in world.Network.Bridges.OrderBy(r => r.Id.Value, StringComparer.Ordinal)) IndexLine(_bridges, bridge);
        foreach (var tree in world.Network.Trees.OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            var p = new Vector2(tree.Position.X, tree.Position.Z);
            Index(_trees, p, p, p);
        }
    }

    public IWorldModel World { get; }

    /// <summary>Half width of a network line's surface (the width WP-15 paves; never below a footpath's).</summary>
    public static float HalfWidth(NetworkLine line) => MathF.Max(line.Width, 2f) * 0.5f;

    public static string? RoadName(NetworkLine line)
    {
        if (line.Props is not { ValueKind: JsonValueKind.Object } props) return null;
        foreach (var name in new[] { "name:zh", "name" })
            if (props.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }

    /// <summary>
    /// True when a disc of <paramref name="radius"/> at <paramref name="p"/> is free: inside the campus, off every outline by
    /// <see cref="FurnitureStyle.OutlineClear"/>, off every road surface (the own road's included, so bends cannot swallow it) and
    /// bridge, clear of trees and of the things already placed.
    /// </summary>
    public bool Free(Vector2 p, float radius)
    {
        if (!PlanGeometry.Contains(World.Boundary, p)) return false;
        var keep = radius + FurnitureStyle.OutlineClear;
        foreach (var o in Near(_outlines, p))
        {
            if (p.X < o.Min.X - keep || p.X > o.Max.X + keep || p.Y < o.Min.Y - keep || p.Y > o.Max.Y + keep) continue;
            if (PlanGeometry.Contains(o.Rings, p) || Edge(o.Rings, p) < keep) return false;
        }
        foreach (var road in Near(_roads, p))
        {
            var r = HalfWidth(road) + FurnitureStyle.RoadClear + radius;
            if (LineDistanceSquared(p, road.Line.Points) < r * r) return false;
        }
        foreach (var bridge in Near(_bridges, p))
        {
            var r = HalfWidth(bridge) + 2f + radius;
            if (LineDistanceSquared(p, bridge.Line.Points) < r * r) return false;
        }
        foreach (var tree in Near(_trees, p))
            if (Vector2.Distance(p, tree) < radius + FurnitureStyle.TreeClear) return false;
        var (cx, cz) = Key(p);
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                if (_occupied.TryGetValue((cx + dx, cz + dz), out var list) &&
                    list.Any(o => Vector2.Distance(p, o.P) < radius + o.R + FurnitureStyle.ItemGap))
                    return false;
        return true;
    }

    /// <summary>A long footprint (bench, bin) as a capsule from <paramref name="a"/> to <paramref name="b"/>: both ends and the middle are free.</summary>
    public bool Free(Vector2 a, Vector2 b, float radius) => Free(a, radius) && Free((a + b) * 0.5f, radius) && Free(b, radius);

    public void Occupy(Vector2 a, Vector2 b, float radius)
    {
        Occupy(a, radius);
        Occupy((a + b) * 0.5f, radius);
        Occupy(b, radius);
    }

    public void Occupy(Vector2 p, float radius)
    {
        var key = Key(p);
        if (!_occupied.TryGetValue(key, out var list)) _occupied.Add(key, list = []);
        list.Add((p, radius));
    }

    /// <summary>The nearest point on any carriageway (not a footpath) to <paramref name="p"/> within <paramref name="reach"/>, with its line.</summary>
    public (Vector2 Point, Vector2 Along, NetworkLine Line)? NearestCarriageway(Vector2 p, float reach)
    {
        (Vector2, Vector2, NetworkLine)? best = null;
        var bestD = reach * reach;
        foreach (var road in World.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            if (!Carriageway(road)) continue;
            var points = road.Line.Points;
            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1]; var b = points[i];
                var ab = b - a;
                var len2 = ab.LengthSquared();
                if (len2 < 1e-6f) continue;
                var t = Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
                var q = a + ab * t;
                var d = Vector2.DistanceSquared(p, q);
                if (d >= bestD) continue;
                bestD = d;
                best = (q, ab / MathF.Sqrt(len2), road);
            }
        }
        return best;
    }

    /// <summary>Vehicle roads WP-15 paves as carriageway (GroundClassifier); footpaths, steps and cycleways are not.</summary>
    public static bool Carriageway(NetworkLine line)
    {
        var type = line.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("highway", out var h) && h.ValueKind == JsonValueKind.String
            ? h.GetString() : null;
        return type is "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street";
    }

    /// <summary>Joins lines that share end points (within 1 m) into chains, in ID order, so a road split by a bridge stays one run.</summary>
    public static List<List<Vector2>> Chains(IEnumerable<NetworkLine> lines)
    {
        var left = lines.OrderBy(l => l.Id.Value, StringComparer.Ordinal).Select(l => l.Line.Points.ToList()).Where(p => p.Count >= 2).ToList();
        var chains = new List<List<Vector2>>();
        while (left.Count > 0)
        {
            var chain = left[0];
            left.RemoveAt(0);
            for (var grown = true; grown;)
            {
                grown = false;
                for (var i = 0; i < left.Count; i++)
                {
                    var l = left[i];
                    if (Vector2.Distance(chain[^1], l[0]) < 1f) chain.AddRange(l.Skip(1));
                    else if (Vector2.Distance(chain[^1], l[^1]) < 1f) chain.AddRange(Enumerable.Reverse(l).Skip(1));
                    else if (Vector2.Distance(chain[0], l[^1]) < 1f) chain.InsertRange(0, l.Take(l.Count - 1));
                    else if (Vector2.Distance(chain[0], l[0]) < 1f) chain.InsertRange(0, Enumerable.Reverse(l).Take(l.Count - 1));
                    else continue;
                    left.RemoveAt(i);
                    grown = true;
                    break;
                }
            }
            chains.Add(chain);
        }
        return chains;
    }

    /// <summary>A polyline with its running lengths: points at an arc length, with the unit left normal from a 4 m chord.</summary>
    public sealed class Run
    {
        private readonly List<Vector2> _points;
        private readonly float[] _lengths;

        public Run(List<Vector2> points)
        {
            _points = points;
            _lengths = new float[points.Count];
            for (var i = 1; i < points.Count; i++) _lengths[i] = _lengths[i - 1] + Vector2.Distance(points[i - 1], points[i]);
        }

        public float Length => _lengths[^1];

        public Vector2 At(float s)
        {
            var i = 1;
            while (i < _points.Count - 1 && _lengths[i] < s) i++;
            var span = _lengths[i] - _lengths[i - 1];
            return Vector2.Lerp(_points[i - 1], _points[i], span > 1e-6f ? Math.Clamp((s - _lengths[i - 1]) / span, 0f, 1f) : 0f);
        }

        /// <summary>Unit left normal at <paramref name="s"/> (the tangent turned towards +X×−Z), averaged over a 4 m chord so bends stay smooth.</summary>
        public Vector2? Normal(float s)
        {
            var t = At(MathF.Min(Length, s + 2f)) - At(MathF.Max(0f, s - 2f));
            if (t.LengthSquared() < 1e-6f) return null;
            t = Vector2.Normalize(t);
            return new Vector2(-t.Y, t.X);
        }
    }

    private sealed record Outline(PolygonRings Rings, Vector2 Min, Vector2 Max);

    private static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));

    private static float LineDistanceSquared(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line[i - 1], line[i]));
        return best;
    }

    /// <summary>The polygons of a feature of one kind (a part's own kind wins over its feature's), as the world model groups them.</summary>
    private static IEnumerable<PolygonRings> Polygons(WorldFeature feature, string kind)
    {
        IEnumerable<OverlayGeometry> geometries;
        if (feature.Parts.Count == 0)
            geometries = feature.Kind.Value == kind ? [feature.Geometry] : [];
        else
            geometries = feature.Parts.Where(part =>
                (part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared) ? declared.GetString() : feature.Kind.Value) == kind)
                .Select(part => part.Geometry);
        foreach (var geometry in geometries)
        {
            IEnumerable<PolygonRings> polygons = geometry switch
            {
                PolygonGeometry polygon => [polygon.Rings],
                MultiPolygonGeometry multi => multi.Polygons,
                _ => [],
            };
            foreach (var rings in polygons)
                if (rings.Outer.Count >= 3) yield return rings;
        }
    }

    private void IndexLine(Dictionary<(int, int), List<NetworkLine>> grid, NetworkLine line)
    {
        if (line.Line.Points.Count < 2) return;
        var pad = new Vector2(HalfWidth(line) + 2f);
        var min = line.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad;
        var max = line.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad;
        Index(grid, min, max, line);
    }

    private static (int, int) Key(Vector2 p) => ((int)MathF.Floor(p.X / Cell), (int)MathF.Floor(p.Y / Cell));

    private static void Index<T>(Dictionary<(int, int), List<T>> grid, Vector2 min, Vector2 max, T item)
    {
        var (x0, z0) = Key(min - new Vector2(Cell * 0.5f));
        var (x1, z1) = Key(max + new Vector2(Cell * 0.5f));
        for (var x = x0; x <= x1; x++)
            for (var z = z0; z <= z1; z++)
            {
                if (!grid.TryGetValue((x, z), out var list)) grid.Add((x, z), list = []);
                list.Add(item);
            }
    }

    private static IEnumerable<T> Near<T>(Dictionary<(int, int), List<T>> grid, Vector2 p) =>
        grid.TryGetValue(Key(p), out var list) ? list : [];
}

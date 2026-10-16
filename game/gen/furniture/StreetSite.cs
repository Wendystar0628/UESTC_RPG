using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

/// <summary>What an anchor's point lands on (<see cref="StreetSite.LandingAt"/>).</summary>
internal enum Landing : byte { Open, Carriageway, Water, Building }

/// <summary>
/// The world as the furniture placer sees it, indexed on a coarse grid: blocking outlines (buildings, water, courts, parking),
/// road centre lines with their widths, the walks a bench can line (footways, paths, pedestrian streets), pedestrian paving
/// (those walks and steps, and the paved road areas of the features), vehicle road areas, the planted tree rows along the
/// furnished roads, bridges, network trees and the things already placed. Pure and deterministic: every list is filled in
/// feature-ID order and grid lookups never depend on dictionary iteration order.
/// </summary>
internal sealed class StreetSite
{
    private const float Cell = 32f;
    private static readonly string[] Blocking = ["building", "water", "sport", "parking"];

    private readonly Dictionary<(int, int), List<Outline>> _outlines = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _roads = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _walks = [];
    private readonly Dictionary<(int, int), List<Paving>> _paving = [];
    private readonly Dictionary<(int, int), List<PolygonRings>> _lanes = [];
    private readonly Dictionary<(int, int), List<(NetworkLine Line, float Offset)>> _rows = [];
    private readonly Dictionary<(int, int), List<NetworkLine>> _bridges = [];
    private readonly Dictionary<(int, int), List<Vector2>> _trees = [];
    private readonly Dictionary<(int, int), List<(Vector2 P, float R)>> _occupied = [];
    private readonly List<PolygonRings> _parking = [];

    public StreetSite(IWorldModel world)
    {
        World = world;
        // The effective network (WP-08) publishes a design walk as a network line under its feature's ID and keeps the feature: the
        // line is the walk, so the feature's polyline stays out of the paving below and the walk is filed once, as the line it is.
        var published = world.Network.Roads.Select(r => r.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            foreach (var kind in Blocking)
                foreach (var rings in Polygons(feature, kind))
                {
                    var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                    var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                    Index(_outlines, min, max, new Outline(kind, rings, min, max));
                    if (kind == "parking") _parking.Add(rings);
                }
            foreach (var (props, geometry) in RoadSources(feature))
            {
                var type = RoadType(props);
                if (Pedestrian(type))
                {
                    if (geometry is not PolylineGeometry || !published.Contains(feature.Id.Value)) IndexPaving(geometry, props);
                }
                else if (CarriagewayType(type))
                    foreach (var rings in geometry switch { PolygonGeometry polygon => [polygon.Rings], MultiPolygonGeometry multi => multi.Polygons, _ => [] })
                        if (rings.Outer.Count >= 3)
                            Index(_lanes, rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max), rings);
            }
        }
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            IndexLine(_roads, road);
            if (Strollable(road)) IndexLine(_walks, road);
            if (Pedestrian(RoadType(road.Props)) && road.Line.Points.Count >= 2)
                IndexPaving(new PolylineGeometry(road.Line), null, HalfWidth(road), road);
        }
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

    /// <summary>Walks a bench may line and face (WP-15 paves them as walks or pedestrian plazas); steps and cycleways are not.</summary>
    public static bool Strollable(NetworkLine line) => RoadType(line.Props) is "footway" or "path" or "pedestrian";

    /// <summary>True for a pedestrian network line (a walk or steps), the lines a plaza may carry.</summary>
    public static bool IsPedestrian(NetworkLine line) => Pedestrian(RoadType(line.Props));

    /// <summary>Pedestrian paving types: the strollable walks and steps.</summary>
    private static bool Pedestrian(string? type) => type is "footway" or "path" or "pedestrian" or "steps";

    /// <summary>Road type as WP-15 reads it (GroundClassifier): <c>roadType</c>, else <c>highway</c>.</summary>
    private static string? RoadType(JsonElement? props)
    {
        if (props is not { ValueKind: JsonValueKind.Object } p) return null;
        foreach (var name in new[] { "roadType", "highway" })
            if (p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }

    /// <summary>
    /// Registers the planted tree rows along <paramref name="lines"/> (a furnished road), <paramref name="offsets"/> metres off its
    /// centre line on both sides. Rows are kept clear as whole lines: a gap the planting leaves is not known here.
    /// </summary>
    public void AddRows(IEnumerable<NetworkLine> lines, IReadOnlyList<float> offsets)
    {
        foreach (var line in lines.OrderBy(l => l.Id.Value, StringComparer.Ordinal))
            foreach (var offset in offsets)
            {
                if (line.Line.Points.Count < 2) continue;
                var pad = new Vector2(offset + 2f);
                var min = line.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad;
                var max = line.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad;
                Index(_rows, min, max, (line, offset));
            }
    }

    /// <summary>True when a disc keeps <see cref="FurnitureStyle.RowClear"/> off every registered tree row.</summary>
    public bool OffRows(Vector2 p, float radius)
    {
        foreach (var (line, offset) in Near(_rows, p))
            if (MathF.Abs(MathF.Sqrt(LineDistanceSquared(p, line.Line.Points)) - offset) < radius + FurnitureStyle.RowClear) return false;
        return true;
    }

    /// <summary>True when a disc keeps <paramref name="clear"/> off every carriageway surface (beyond its half width).</summary>
    public bool OffCarriageways(Vector2 p, float radius, float clear)
    {
        foreach (var road in Near(_roads, p))
        {
            if (!Carriageway(road)) continue;
            var r = HalfWidth(road) + clear + radius;
            if (LineDistanceSquared(p, road.Line.Points) < r * r) return false;
        }
        return true;
    }

    /// <summary>True when a carriageway surface lies within <paramref name="reach"/> ahead of <paramref name="p"/> along <paramref name="facing"/>.</summary>
    public bool CarriagewayAhead(Vector2 p, Vector2 facing, float reach)
    {
        for (var d = 0f; d <= reach + 1e-3f; d += 0.5f)
            if (!OffCarriageways(p + facing * d, 0f, 0f)) return true;
        return false;
    }

    /// <summary>A walk near a point: the line, the nearest point on it and that point's arc length along the line.</summary>
    public readonly record struct WalkPoint(NetworkLine Line, Vector2 Point, float Along, float Distance);

    /// <summary>Strollable walks within <paramref name="reach"/> (≤ 32 m) of <paramref name="p"/>, nearest first (ties by ID).</summary>
    public List<WalkPoint> Walks(Vector2 p, float reach)
    {
        var found = new List<WalkPoint>();
        foreach (var line in NearAll(_walks, p))
        {
            var points = line.Line.Points;
            WalkPoint? best = null;
            var run = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1]; var ab = points[i] - a;
                var len = ab.Length();
                if (len > 1e-4f)
                {
                    var t = Math.Clamp(Vector2.Dot(p - a, ab) / (len * len), 0f, 1f);
                    var q = a + ab * t;
                    var d = Vector2.Distance(p, q);
                    if (best is null || d < best.Value.Distance) best = new(line, q, run + t * len, d);
                }
                run += len;
            }
            if (best is { } b && b.Distance <= reach) found.Add(b);
        }
        return found.OrderBy(w => w.Distance).ThenBy(w => w.Line.Id.Value, StringComparer.Ordinal).ToList();
    }

    /// <summary>Distance from <paramref name="p"/> to the nearest strollable walk other than <paramref name="except"/> (a junction nearby).</summary>
    public float OtherWalk(Vector2 p, NetworkLine except)
    {
        var best = float.MaxValue;
        foreach (var line in NearAll(_walks, p))
            if (!ReferenceEquals(line, except)) best = MathF.Min(best, MathF.Sqrt(LineDistanceSquared(p, line.Line.Points)));
        return best;
    }

    /// <summary>
    /// The nearest pedestrian paving to <paramref name="p"/> (walks, steps, paved road areas): the gap to it (0 on it) and the
    /// nearest point of its edge (<paramref name="p"/> itself on it). Null when none lies within about 32 m.
    /// </summary>
    public (float Gap, Vector2 Edge)? PavingEdge(Vector2 p) => PavingEdgeFacing(p) is { } hit ? (hit.Gap, hit.Edge) : null;

    /// <summary>
    /// <see cref="PavingEdge"/> with the paving edge's direction: <c>Inward</c> is the unit normal of the edge at <c>Edge</c> pointing
    /// into the paving, from the side <paramref name="p"/> is on (the zero vector when <paramref name="p"/> is on paving). A polygon's
    /// edge gives its segment's normal, a walk's the line's normal at the nearest point; <c>Road</c> is the network walk whose strip it is
    /// (null for a polygon or a paved road area).
    /// </summary>
    public (float Gap, Vector2 Edge, Vector2 Inward, NetworkLine? Road)? PavingEdgeFacing(Vector2 p)
    {
        (float, Vector2, Vector2, NetworkLine?)? best = null;
        foreach (var paving in NearAll(_paving, p))
        {
            float gap; Vector2 edge, inward;
            if (paving.Line is { } line)
            {
                var (q, _) = NearestOn(p, line);
                var d = Vector2.Distance(p, q);
                if (d <= paving.Half) return (0f, p, Vector2.Zero, paving.Road);
                gap = d - paving.Half;
                edge = q + (p - q) / d * paving.Half;
                inward = (q - p) / d;
            }
            else
            {
                var rings = paving.Rings!;
                if (PlanGeometry.Contains(rings, p)) return (0f, p, Vector2.Zero, null);
                (Vector2 Point, Vector2 Tangent) hit = default;
                var bestD = float.MaxValue;
                foreach (var ring in rings.Holes.Prepend(rings.Outer))
                {
                    var near = NearestOn(p, ring, true);
                    var d = Vector2.DistanceSquared(p, near.Point);
                    if (d >= bestD) continue;
                    bestD = d;
                    hit = near;
                }
                edge = hit.Point;
                gap = MathF.Sqrt(bestD);
                // The segment's normal on the far side from p: the way into the paving from where p stands.
                var normal = new Vector2(-hit.Tangent.Y, hit.Tangent.X);
                inward = Vector2.Dot(normal, edge - p) >= 0f ? normal : -normal;
            }
            if (best is null || gap < best.Value.Item1) best = (gap, edge, inward, paving.Road);
        }
        return best;
    }

    /// <summary>
    /// True when <paramref name="p"/> lies on plaza paving: a pedestrian polygon (a square, a paved road area), not a walk's strip, which
    /// belongs to walkers (and whose round end the strip test allows is lawn where WP-15 cuts the walk square).
    /// </summary>
    public bool OnPlaza(Vector2 p)
    {
        foreach (var paving in NearAll(_paving, p))
            if (paving.Rings is { } rings && PlanGeometry.Contains(rings, p)) return true;
        return false;
    }

    /// <summary>
    /// True when <paramref name="p"/> lies on pedestrian paving that WP-15 paves for certain: a plaza polygon, or a walk's strip short of
    /// the line's ends (a strip's round end is lawn where the walk is cut square; a walk going on past a junction paves it itself).
    /// </summary>
    public bool OnPavedGround(Vector2 p)
    {
        foreach (var paving in NearAll(_paving, p))
        {
            if (paving.Rings is { } rings) { if (PlanGeometry.Contains(rings, p)) return true; continue; }
            var line = paving.Line!;
            var (q, _) = NearestOn(p, line);
            if (Vector2.DistanceSquared(q, line[0]) < 1e-6f || Vector2.DistanceSquared(q, line[^1]) < 1e-6f) continue;
            if (Vector2.Distance(p, q) <= paving.Half) return true;
        }
        return false;
    }

    /// <summary>
    /// Spots on the edges of pedestrian paving within <paramref name="reach"/> of <paramref name="p"/>: the edge point and the unit normal
    /// into the paving, found by sampling the ground every <paramref name="step"/> and keeping the points just off the paving (no
    /// further than a step), nearest first (ties by position), one per 0.5 m of edge.
    /// </summary>
    public List<(Vector2 Edge, Vector2 Inward, NetworkLine? Road)> PavingEdgesNear(Vector2 p, float reach, float step)
    {
        var found = new SortedDictionary<(int, int), (float Distance, Vector2 Edge, Vector2 Inward, NetworkLine? Road)>();
        var n = (int)MathF.Floor(reach / step);
        for (var i = -n; i <= n; i++)
            for (var k = -n; k <= n; k++)
            {
                var q = p + new Vector2(i * step, k * step);
                if (PavingEdgeFacing(q) is not { Gap: > 0f } hit || hit.Gap > step) continue;
                var distance = Vector2.Distance(hit.Edge, p);
                if (distance > reach) continue;
                var key = ((int)MathF.Round(hit.Edge.X * 2f), (int)MathF.Round(hit.Edge.Y * 2f));
                if (!found.TryGetValue(key, out var old) || distance < old.Distance) found[key] = (distance, hit.Edge, hit.Inward, hit.Road);
            }
        return found.Values.OrderBy(f => f.Distance).ThenBy(f => f.Edge.X).ThenBy(f => f.Edge.Y).Select(f => (f.Edge, f.Inward, f.Road)).ToList();
    }

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
    /// <remarks>
    /// <paramref name="beside"/> is a walk the thing stands at the edge of: it only has to stay off that walk's surface; with
    /// <paramref name="onBeside"/> it stands on that walk (a node bench at a wide walk's edge) and the walk does not block it at all.
    /// </remarks>
    public bool Free(Vector2 p, float radius, NetworkLine? beside = null) => Blocker(p, radius, beside) is null;

    /// <summary>What keeps a disc from being <see cref="Free(Vector2, float, NetworkLine?)"/>: the first of "outside the campus", "an outline", "a road", "a bridge", "a tree" or "another item"; null when nothing does.</summary>
    /// <remarks>
    /// <paramref name="outlineClear"/> replaces <see cref="FurnitureStyle.OutlineClear"/> for building outlines (a plaza bench backed onto
    /// a wall); with <paramref name="walksPass"/> pedestrian lines (walks, steps) do not block: a plaza carrying a walk line (Opus review
    /// 13c2b rework).
    /// </remarks>
    public string? Blocker(Vector2 p, float radius, NetworkLine? beside = null, bool onBeside = false, float? outlineClear = null, bool walksPass = false)
    {
        if (!PlanGeometry.Contains(World.Boundary, p)) return "the campus edge";
        foreach (var o in Near(_outlines, p))
        {
            var keep = radius + (o.Kind == "building" && outlineClear is { } wall ? wall : FurnitureStyle.OutlineClear);
            if (p.X < o.Min.X - keep || p.X > o.Max.X + keep || p.Y < o.Min.Y - keep || p.Y > o.Max.Y + keep) continue;
            if (PlanGeometry.Contains(o.Rings, p) || Edge(o.Rings, p) < keep) return $"a {o.Kind} outline";
        }
        foreach (var road in Near(_roads, p))
        {
            if (onBeside && ReferenceEquals(road, beside)) continue;
            if (walksPass && IsPedestrian(road)) continue;
            var r = HalfWidth(road) + (ReferenceEquals(road, beside) ? 0f : FurnitureStyle.RoadClear) + radius;
            if (LineDistanceSquared(p, road.Line.Points) < r * r) return "a road surface";
        }
        foreach (var bridge in Near(_bridges, p))
        {
            var r = HalfWidth(bridge) + 2f + radius;
            if (LineDistanceSquared(p, bridge.Line.Points) < r * r) return "a bridge";
        }
        foreach (var tree in Near(_trees, p))
            if (Vector2.Distance(p, tree) < radius + FurnitureStyle.TreeClear) return "a tree";
        var (cx, cz) = Key(p);
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                if (_occupied.TryGetValue((cx + dx, cz + dz), out var list) &&
                    list.Any(o => Vector2.Distance(p, o.P) < radius + o.R + FurnitureStyle.ItemGap))
                    return "another item";
        return null;
    }

    /// <summary>True when a pedestrian line's centre line passes within <paramref name="clear"/> of any of <paramref name="points"/>.</summary>
    public bool NearWalkLine(IReadOnlyList<Vector2> points, float clear)
    {
        foreach (var p in points)
            foreach (var road in Near(_roads, p))
                if (IsPedestrian(road) && LineDistanceSquared(p, road.Line.Points) < clear * clear) return true;
        return false;
    }

    /// <summary>Parking areas (their outlines), in feature-ID order: where parked cars stand.</summary>
    public IReadOnlyList<PolygonRings> ParkingLots => _parking;

    /// <summary>What crowds a disc inside an area the outlines do not cover (a parking lot): "a tree" or "another item", or null.</summary>
    public string? Clutter(Vector2 p, float radius)
    {
        foreach (var tree in Near(_trees, p))
            if (Vector2.Distance(p, tree) < radius + FurnitureStyle.TreeClear) return "a tree";
        var (cx, cz) = Key(p);
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                if (_occupied.TryGetValue((cx + dx, cz + dz), out var list) && list.Any(o => Vector2.Distance(p, o.P) < radius + o.R + FurnitureStyle.ItemGap))
                    return "another item";
        return null;
    }

    /// <summary>Pedestrian network lines (footways, paths, pedestrian streets, steps), in ID order: where walkers reach a kerb.</summary>
    public IEnumerable<NetworkLine> PedestrianLines() =>
        World.Network.Roads.Where(r => Pedestrian(RoadType(r.Props)) && r.Line.Points.Count >= 2).OrderBy(r => r.Id.Value, StringComparer.Ordinal);

    /// <summary>A long footprint (bench, bin) as a capsule from <paramref name="a"/> to <paramref name="b"/>: both ends and the middle are free.</summary>
    public bool Free(Vector2 a, Vector2 b, float radius, NetworkLine? beside = null) => Blocker(a, b, radius, beside) is null;

    /// <summary>What keeps a capsule from being <see cref="Free(Vector2, Vector2, float, NetworkLine?)"/> (see <see cref="Blocker(Vector2, float, NetworkLine?)"/>).</summary>
    public string? Blocker(Vector2 a, Vector2 b, float radius, NetworkLine? beside = null, bool onBeside = false, float? outlineClear = null, bool walksPass = false) =>
        Blocker(a, radius, beside, onBeside, outlineClear, walksPass) ?? Blocker((a + b) * 0.5f, radius, beside, onBeside, outlineClear, walksPass) ?? Blocker(b, radius, beside, onBeside, outlineClear, walksPass);

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
        return CarriagewayType(type);
    }

    private static bool CarriagewayType(string? type) =>
        type is "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street";

    /// <summary>
    /// What a point lands on, for an anchor's fallback (SYS-ATLAS §6 data still under review): a building's or water's outline,
    /// a carriageway (a vehicle road's surface or paved area), else open ground. Checked in that order.
    /// </summary>
    public Landing LandingAt(Vector2 p)
    {
        foreach (var kind in new[] { "building", "water" })
            foreach (var o in Near(_outlines, p))
                if (o.Kind == kind && p.X >= o.Min.X && p.X <= o.Max.X && p.Y >= o.Min.Y && p.Y <= o.Max.Y && PlanGeometry.Contains(o.Rings, p))
                    return kind == "building" ? Landing.Building : Landing.Water;
        if (!OffCarriageways(p, 0f, 0f) || Near(_lanes, p).Any(rings => PlanGeometry.Contains(rings, p))) return Landing.Carriageway;
        return Landing.Open;
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

    private sealed record Outline(string Kind, PolygonRings Rings, Vector2 Min, Vector2 Max);

    /// <summary>A pedestrian paving surface: a polygon, or a centre line with a half width (<c>Road</c>: the network walk it is, when it is one).</summary>
    private sealed record Paving(PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half, NetworkLine? Road = null);

    private static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));

    /// <summary>The nearest point to <paramref name="p"/> on a polyline (or ring) with the unit direction of the segment it lies on.</summary>
    private static (Vector2 Point, Vector2 Tangent) NearestOn(Vector2 p, IReadOnlyList<Vector2> line, bool closed = false)
    {
        var best = line[0];
        var tangent = Vector2.UnitX;
        var bestD = float.MaxValue;
        var count = closed ? line.Count + 1 : line.Count;
        for (var i = 1; i < count; i++)
        {
            var a = line[i - 1]; var ab = line[i % line.Count] - a;
            var len2 = ab.LengthSquared();
            var q = len2 < 1e-8f ? a : a + ab * Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
            var d = Vector2.DistanceSquared(p, q);
            if (d >= bestD) continue;
            bestD = d;
            best = q;
            if (len2 >= 1e-8f) tangent = ab / MathF.Sqrt(len2);
        }
        return (best, tangent);
    }

    /// <summary>Road areas of a feature, part by part (a part's own kind and props win over its feature's, as WP-15 reads them).</summary>
    private static IEnumerable<(JsonElement? Props, OverlayGeometry Geometry)> RoadSources(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            if (feature.Kind.Value == "road") yield return (feature.Props, feature.Geometry);
            yield break;
        }
        foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
        {
            var props = part.Props ?? feature.Props;
            var kind = part.Props is { ValueKind: JsonValueKind.Object } own && own.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String
                ? k.GetString() : feature.Kind.Value;
            if (kind == "road") yield return (props, part.Geometry);
        }
    }

    private void IndexPaving(OverlayGeometry geometry, JsonElement? props, float half = 0f, NetworkLine? road = null)
    {
        switch (geometry)
        {
            case PolygonGeometry polygon:
                IndexPaving(polygon.Rings);
                break;
            case MultiPolygonGeometry multi:
                foreach (var rings in multi.Polygons) IndexPaving(rings);
                break;
            case PolylineGeometry { Line.Points: { Count: >= 2 } points }:
                if (half <= 0f)
                    half = (props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number
                        ? MathF.Max(w.GetSingle(), 2f) : 2.5f) * 0.5f;
                var pad = new Vector2(half);
                Index(_paving, points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad,
                    points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad, new Paving(null, points, half, road));
                break;
        }
    }

    private void IndexPaving(PolygonRings rings)
    {
        if (rings.Outer.Count < 3) return;
        Index(_paving, rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min), rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max),
            new Paving(rings, null, 0f));
    }

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

    /// <summary>Everything filed in the 3 × 3 cells around <paramref name="p"/>, each once, in cell then filing order.</summary>
    private static List<T> NearAll<T>(Dictionary<(int, int), List<T>> grid, Vector2 p) where T : class
    {
        var (cx, cz) = Key(p);
        var seen = new HashSet<T>(ReferenceEqualityComparer.Instance);
        var result = new List<T>();
        for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
                if (grid.TryGetValue((cx + dx, cz + dz), out var list))
                    foreach (var item in list)
                        if (seen.Add(item)) result.Add(item);
        return result;
    }
}

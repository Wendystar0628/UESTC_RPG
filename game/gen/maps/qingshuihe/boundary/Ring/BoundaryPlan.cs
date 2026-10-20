using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Maps.Qingshuihe.Boundary.Fence;
using Campus.Gen.Maps.Qingshuihe.Boundary.Framework;
using Campus.Gen.Maps.Qingshuihe.Boundary.Gates;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.South;
using Campus.Gen.Sites;
using F = Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryTune.Fence;
using H = Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryTune.HedgeClear;

namespace Campus.Gen.Maps.Qingshuihe.Boundary.Ring;

/// <summary>What keeps one stretch of the ring open for something other than the fence.</summary>
public enum OpeningKind { Gate, SideGate, Grate }

/// <summary>
/// One stretch of the ring the fence leaves open, from arc <see cref="Start"/> forward for <see cref="Length"/> metres.
/// <see cref="StartPost"/> stands at the end of the run arriving at it, <see cref="EndPost"/> at the start of the run leaving it.
/// Side gates and grates hang between the two end pillars; gates close their own line. <see cref="Width"/> is the road width
/// of a side gate.
/// </summary>
public sealed record Opening(string Key, OpeningKind Kind, float Start, float Length, RunEnd StartPost, RunEnd EndPost, float Width = 0f);

/// <summary>
/// The campus boundary as one closed plan (WLD-OUTSKIRTS, MAP-QSH-ADDITIONS): the ring from world/geo.json opened at
/// every gate (the five of <see cref="BoundaryTune.Gates"/> and WP-18's south gate), at every water crossing (a grate) and at
/// every other road crossing (a closed side gate); the fence runs fill the rest, each with its hedge pieces where the strip
/// inside is clear. Built once per world, from the world model only.
/// </summary>
public sealed class BoundaryPlan
{

    private BoundaryPlan(BoundaryRing ring, IReadOnlyList<Opening> openings, IReadOnlyList<FenceRun> runs, IReadOnlyList<GateLayout> gates,
        IReadOnlyList<(float Start, float Length, IReadOnlyList<Vector2> Line)> detours)
    {
        Ring = ring;
        Detours = detours;
        Openings = openings;
        Runs = runs;
        Gates = gates;
    }

    public BoundaryRing Ring { get; }

    /// <summary>Openings in arc order.</summary>
    public IReadOnlyList<Opening> Openings { get; }

    /// <summary>Ring runs in arc order, then the gates' own pieces (closure legs, 便门 panels).</summary>
    public IReadOnlyList<FenceRun> Runs { get; }

    public IReadOnlyList<GateLayout> Gates { get; }

    /// <summary>Stretches of ring the fence leaves for a line along a road's campus side (<see cref="BoundaryTune.Detours"/>).</summary>
    public IReadOnlyList<(float Start, float Length, IReadOnlyList<Vector2> Line)> Detours { get; }

    /// <summary>Inset plan points of an opening's two ends (on the fence line).</summary>
    public (Vector2 A, Vector2 B) Ends(Opening o) => (Ring.Slice(o.Start, 0f, F.Inset)[0], Ring.Slice(o.Start + o.Length, 0f, F.Inset)[0]);

    public static BoundaryPlan? Of(IWorldModel world) => world.Boundary.Outer.Count < 3 ? null : Build(world);

    public static BoundaryPlan? Of(BuildContext context) =>
        context.Memo(typeof(BoundaryPlan), () => Of(context.World));

    private static BoundaryPlan Build(IWorldModel world)
    {
        var ring = new BoundaryRing(world.Boundary.Outer);
        var openings = new List<Opening>();
        var gates = new List<GateLayout>();
        var gateRoads = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spec in BoundaryTune.Gates)
        {
            if (GateSite.Of(world, spec) is not { } g) continue;
            gates.Add(g);
            gateRoads.Add(spec.Road);
            openings.Add(new(spec.Key, OpeningKind.Gate, g.OpeningStart, g.OpeningLength, g.StartPost, g.EndPost));
        }
        if (South(world, ring) is { } south) openings.Add(south);
        var detours = FindDetours(world, ring);
        openings.AddRange(Grates(world, ring, openings));
        openings.AddRange(SideGates(world, ring, openings, detours, gateRoads));
        openings.Sort((a, b) => a.Start.CompareTo(b.Start));
        var obstacles = new Obstacles(world, ring, gates);
        var runs = new List<FenceRun>();
        for (var i = 0; i < openings.Count; i++)
        {
            var from = openings[i];
            var to = openings[(i + 1) % openings.Count];
            var s0 = ring.Fold(from.Start + from.Length);
            var length = ring.Forward(s0, to.Start);
            if (openings.Count == 1) length = ring.Perimeter - from.Length;
            if (length < 0.3f) continue;
            var line = Simplify(Line(ring, s0, length, detours));
            var hedges = length >= H.MinRun ? obstacles.Clip(BoundaryFence.Offset(line, F.HedgeOffset, ring.Contains, H.Trim, H.Trim)) : [];
            runs.Add(new FenceRun(line, from.EndPost, to.StartPost, hedges));
        }
        foreach (var g in gates) runs.AddRange(g.Closures);
        return new BoundaryPlan(ring, openings, runs, gates, detours);
    }

    // ---- Openings ----

    /// <summary>WP-18's south gate: the ring stays open between the outer walls of its two source-identified guard houses.</summary>
    private static Opening? South(IWorldModel world, BoundaryRing ring)
    {
        if (!world.Features.TryGetValue(new(SouthIds.Gate.Entity), out var gate)) return null;
        Vector2 House(string sourceId) => PlanPolygons.Centroid(gate.Part(sourceId).Contour());
        var (west, east) = (House(SouthIds.WestHouse.Part), House(SouthIds.EastHouse.Part));
        var u = Vector2.Normalize(east - west);
        var reach = gate.Number("guardHouseSize") * 0.5f - 0.05f;
        var a = ring.Arc(west - u * reach);
        var b = ring.Arc(east + u * reach);
        if (Vector2.Distance(ring.At(a), west) > 12f) return null;
        var mid = ring.Arc((west + east) * 0.5f);
        return ring.Forward(a, mid) <= ring.Forward(a, b)
            ? new("south", OpeningKind.Gate, a, ring.Forward(a, b), RunEnd.None, RunEnd.None)
            : new("south", OpeningKind.Gate, b, ring.Forward(b, a), RunEnd.None, RunEnd.None);
    }

    /// <summary>
    /// A grate wherever water crosses the ring: the crossings of every water polygon with the ring, gathered into channels
    /// (crossings within <see cref="BoundaryTune.Grate.Cluster"/> along the ring), each opened <see cref="BoundaryTune.Grate.Bank"/>
    /// beyond its outer water edges.
    /// </summary>
    private static IEnumerable<Opening> Grates(IWorldModel world, BoundaryRing ring, IReadOnlyList<Opening> taken)
    {
        var hits = new List<float>();
        var waters = world.Features.Values.Where(f => f.Kind.Value == "water" && (f.Geometry is PolygonGeometry or MultiPolygonGeometry))
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .SelectMany(f => f.Polygons().Select(p => p.Outer)).Where(o => o.Count >= 3).ToArray();
        foreach (var outer in waters)
            for (var i = 0; i < outer.Count; i++)
                foreach (var s in ring.Crossings(outer[i], outer[(i + 1) % outer.Count])) hits.Add(s);
        // Water that only meets the ring (a channel ending on it, an edge along it) shows on the fence line itself.
        var boxes = waters.Select(o => (Ring: o, Min: o.Aggregate(new Vector2(float.MaxValue), Vector2.Min), Max: o.Aggregate(new Vector2(float.MinValue), Vector2.Max))).ToArray();
        for (var s = 0f; s < ring.Perimeter; s += BoundaryTune.Grate.Sample)
        {
            var p = ring.At(s) + ring.InwardAt(s) * F.Inset;
            foreach (var (outer, min, max) in boxes)
                if (p.X >= min.X && p.Y >= min.Y && p.X <= max.X && p.Y <= max.Y && PlanRings.InsideByCrossings(outer, p)) { hits.Add(s); break; }
        }
        if (hits.Count == 0) yield break;
        hits.Sort();
        var groups = new List<(float Start, float End)>();
        foreach (var s in hits)
        {
            if (groups.Count > 0 && s - groups[^1].End <= BoundaryTune.Grate.Cluster) groups[^1] = (groups[^1].Start, s);
            else groups.Add((s, s));
        }
        // A channel straddling arc 0 is split across the two ends of the sorted list: join them.
        if (groups.Count > 1 && groups[0].Start + ring.Perimeter - groups[^1].End <= BoundaryTune.Grate.Cluster)
        {
            groups[0] = (groups[^1].Start, groups[0].End + ring.Perimeter);
            groups.RemoveAt(groups.Count - 1);
        }
        var bank = BoundaryTune.Grate.Bank;
        var n = 0;
        foreach (var (start, end) in groups)
        {
            var s0 = ring.Fold(start - bank);
            var length = end - start + 2 * bank;
            if (taken.Any(o => Overlaps(ring, o.Start, o.Length, s0, length))) continue;
            yield return new($"grate-{n++}", OpeningKind.Grate, s0, length, RunEnd.EndPillar, RunEnd.EndPillar);
        }
    }

    /// <summary>A closed side gate at every other road crossing; crossings whose spans overlap share one gate.</summary>
    private static IEnumerable<Opening> SideGates(IWorldModel world, BoundaryRing ring, IReadOnlyList<Opening> taken,
        IReadOnlyList<(float Start, float Length, IReadOnlyList<Vector2> Line)> detours, HashSet<string> gateRoads)
    {
        var detourRoads = new HashSet<string>(BoundaryTune.Detours.Select(d => d.Road), StringComparer.Ordinal);
        var spans = new List<(float Start, float Length, float Width)>();
        foreach (var road in world.Network.Roads)
        {
            if (gateRoads.Contains(road.Id.Value) || detourRoads.Contains(road.Id.Value)) continue;
            var points = road.Line.Points;
            var width = road.Width > 0 ? road.Width : 3f;
            for (var k = 0; k + 1 < points.Count; k++)
                foreach (var s in ring.Crossings(points[k], points[k + 1]))
                {
                    // The span along the ring grows as the road crosses it obliquely.
                    var d = Vector2.Normalize(points[k + 1] - points[k]);
                    var t = ring.Direction(s);
                    var sin = MathF.Max(MathF.Abs(d.X * t.Y - d.Y * t.X), 0.5f);
                    var half = (width * 0.5f + BoundaryTune.SideGate.Clear) / sin + F.EndPillarSize * 0.5f;
                    var start = ring.Fold(s - half);
                    if (taken.Any(o => Overlaps(ring, o.Start, o.Length, start, 2 * half))) continue;
                    if (detours.Any(o => Overlaps(ring, o.Start, o.Length, start, 2 * half))) continue;
                    spans.Add((start, 2 * half, width));
                }
        }
        spans.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<(float Start, float Length, float Width)>();
        foreach (var span in spans)
        {
            if (merged.Count > 0 && ring.Forward(merged[^1].Start, span.Start) <= merged[^1].Length)
            {
                var last = merged[^1];
                var end = MathF.Max(last.Length, ring.Forward(last.Start, span.Start) + span.Length);
                merged[^1] = (last.Start, end, MathF.Max(last.Width, span.Width));
            }
            else merged.Add(span);
        }
        var n = 0;
        foreach (var (start, length, width) in merged)
            yield return new($"side-{n++}", OpeningKind.SideGate, start, length, RunEnd.EndPillar, RunEnd.EndPillar, width);
    }

    /// <summary>
    /// Detours: the fence follows a road's campus side where the road runs across a corner of the ring (the north tip), so the
    /// road stays outside. Each is the stretch of ring it replaces and the plan line laid along the road.
    /// </summary>
    private static IReadOnlyList<(float Start, float Length, IReadOnlyList<Vector2> Line)> FindDetours(IWorldModel world, BoundaryRing ring)
    {
        var result = new List<(float, float, IReadOnlyList<Vector2>)>();
        foreach (var detour in BoundaryTune.Detours)
        {
            var road = world.Network.Roads.FirstOrDefault(r => r.Id.Value == detour.Road);
            if (road is null) continue;
            var points = road.Line.Points;
            var off = (road.Width > 0 ? road.Width : 3f) * 0.5f + detour.Clear + F.PillarSize * 0.5f;
            var line = Shifted(points, off, ring);
            // The offset line enters and leaves the campus: keep it between its first and last crossing of the ring.
            var cuts = new List<(int Segment, float S, Vector2 Point)>();
            for (var k = 0; k + 1 < line.Count; k++)
                foreach (var s in ring.Crossings(line[k], line[k + 1]))
                {
                    var p = ring.At(s);
                    cuts.Add((k, s, p));
                }
            if (cuts.Count < 2) continue;
            var first = cuts[0];
            var last = cuts[^1];
            var path = new List<Vector2> { first.Point + ring.InwardAt(first.S) * F.Inset };
            for (var k = first.Segment + 1; k <= last.Segment; k++) path.Add(line[k]);
            path.Add(last.Point + ring.InwardAt(last.S) * F.Inset);
            // Arc order: the detour replaces the ring between its two ends, the short way round.
            var forward = ring.Forward(first.S, last.S);
            if (forward <= ring.Perimeter * 0.5f) result.Add((first.S, forward, path));
            else
            {
                path.Reverse();
                result.Add((last.S, ring.Perimeter - forward, path));
            }
        }
        return result;
    }

    /// <summary>A road line offset to the side away from the ring corner it cuts (the side whose middle lies deeper inside).</summary>
    private static IReadOnlyList<Vector2> Shifted(IReadOnlyList<Vector2> points, float off, BoundaryRing ring)
    {
        IReadOnlyList<Vector2> Side(float sign)
        {
            var result = new Vector2[points.Count];
            for (var k = 0; k < points.Count; k++)
            {
                var before = VectorMath.Unit(points[Math.Max(k, 1)] - points[Math.Max(k, 1) - 1], Vector2.UnitX, minimumLength: 1e-6);
                var after = VectorMath.Unit(points[Math.Min(k + 1, points.Count - 1)] - points[Math.Min(k + 1, points.Count - 1) - 1], Vector2.UnitX, minimumLength: 1e-6);
                var n = VectorMath.Unit(new Vector2(-before.Y, before.X) + new Vector2(-after.Y, after.X), Vector2.UnitX, minimumLength: 1e-6);
                var cos = MathF.Max(Vector2.Dot(n, new Vector2(-after.Y, after.X)), 0.5f);
                result[k] = points[k] + n * (sign * off / cos);
            }
            return result;
        }
        float Depth(IReadOnlyList<Vector2> line)
        {
            var best = 0f;
            foreach (var p in line)
                if (ring.Contains(p))
                {
                    var (e, t) = ring.Project(p);
                    best = MathF.Max(best, Vector2.Distance(ring.At(e, t), p));
                }
            return best;
        }
        var left = Side(1f);
        var right = Side(-1f);
        return Depth(left) >= Depth(right) ? left : right;
    }

    // ---- Run lines ----

    /// <summary>The fence line of one run: the ring slice, with any detour inside it spliced in.</summary>
    private static List<Vector2> Line(BoundaryRing ring, float s0, float length, IReadOnlyList<(float Start, float Length, IReadOnlyList<Vector2> Line)> detours)
    {
        foreach (var d in detours)
        {
            var into = ring.Forward(s0, d.Start);
            if (into + d.Length > length) continue;
            var line = new List<Vector2>(ring.Slice(s0, into, F.Inset));
            line.RemoveAt(line.Count - 1);
            line.AddRange(d.Line);
            var tail = ring.Slice(d.Start + d.Length, length - into - d.Length, F.Inset);
            line.AddRange(tail.Skip(1));
            return line;
        }
        return [.. ring.Slice(s0, length, F.Inset)];
    }

    /// <summary>Drops vertices closer than <see cref="F.MinSegment"/> to the last kept one, and nearly straight ones.</summary>
    internal static IReadOnlyList<Vector2> Simplify(List<Vector2> line)
    {
        if (line.Count <= 2) return line;
        var kept = new List<Vector2> { line[0] };
        for (var k = 1; k + 1 < line.Count; k++)
        {
            var p = line[k];
            if (Vector2.Distance(p, kept[^1]) < F.MinSegment) continue;
            var a = VectorMath.Unit(p - kept[^1], Vector2.UnitX, minimumLength: 1e-6);
            var b = VectorMath.Unit(line[k + 1] - p, Vector2.UnitX, minimumLength: 1e-6);
            if (Vector2.Dot(a, b) > 0.99985f) continue;
            kept.Add(p);
        }
        if (kept.Count > 1 && Vector2.Distance(kept[^1], line[^1]) < F.MinSegment) kept.RemoveAt(kept.Count - 1);
        kept.Add(line[^1]);
        return kept;
    }

    private static bool Overlaps(BoundaryRing ring, float a, float la, float b, float lb) =>
        ring.Forward(a, b) < la || ring.Forward(b, a) < lb;

    /// <summary>
    /// What a hedge must keep off: the network roads (by their half width) and the area features of <see cref="H.Kinds"/>
    /// near the ring, plus each gate's own keep-outs (the locker). Clips a hedge line into the clear pieces.
    /// </summary>
    private sealed class Obstacles
    {
        private readonly List<(Vector2 A, Vector2 B, float Reach)> _segments = [];
        private readonly List<(IReadOnlyList<Vector2> Ring, Vector2 Min, Vector2 Max)> _areas = [];

        public Obstacles(IWorldModel world, BoundaryRing ring, IReadOnlyList<GateLayout> gates)
        {
            var reachHedge = F.HedgeOffset + F.HedgeWidth * 0.5f + F.Inset + 2f;
            bool NearRing(Vector2 min, Vector2 max)
            {
                for (var i = 0; i < ring.Count; i++)
                {
                    var (a, b) = ring.Edge(i);
                    if (MathF.Min(a.X, b.X) - reachHedge <= max.X && MathF.Max(a.X, b.X) + reachHedge >= min.X &&
                        MathF.Min(a.Y, b.Y) - reachHedge <= max.Y && MathF.Max(a.Y, b.Y) + reachHedge >= min.Y) return true;
                }
                return false;
            }
            foreach (var road in world.Network.Roads)
            {
                var reach = (road.Width > 0 ? road.Width : 3f) * 0.5f + H.Road + F.HedgeWidth * 0.5f;
                var pts = road.Line.Points;
                for (var k = 0; k + 1 < pts.Count; k++)
                    if (NearRing(Vector2.Min(pts[k], pts[k + 1]) - new Vector2(reach), Vector2.Max(pts[k], pts[k + 1]) + new Vector2(reach)))
                        _segments.Add((pts[k], pts[k + 1], reach));
            }
            var kinds = new HashSet<string>(H.Kinds, StringComparer.Ordinal);
            foreach (var feature in world.Features.Values.Where(f => kinds.Contains(f.Kind.Value) && f.Geometry is PolygonGeometry or MultiPolygonGeometry).OrderBy(f => f.Id.Value, StringComparer.Ordinal))
                foreach (var outer in feature.Polygons().Select(p => p.Outer))
                {
                    if (outer.Count < 3) continue;
                    var min = new Vector2(float.MaxValue);
                    var max = new Vector2(float.MinValue);
                    foreach (var p in outer) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                    var pad = new Vector2(H.Area + F.HedgeWidth * 0.5f);
                    if (NearRing(min - pad, max + pad)) _areas.Add((outer, min - pad, max + pad));
                }
            foreach (var g in gates)
                if (g.Spec.Locker is { } locker)
                {
                    var hw = GateSite.LockerSize.Width * 0.5f + 0.6f;
                    Vector2[] box = [g.P(locker.X - hw, 0f), g.P(locker.X + hw, 0f), g.P(locker.X + hw, -3f), g.P(locker.X - hw, -3f)];
                    _areas.Add((box, Vector2.Min(Vector2.Min(box[0], box[1]), Vector2.Min(box[2], box[3])), Vector2.Max(Vector2.Max(box[0], box[1]), Vector2.Max(box[2], box[3]))));
                }
        }

        private bool Blocked(Vector2 p)
        {
            foreach (var (a, b, reach) in _segments)
                if (MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, a, b)) < reach) return true;
            var pad = H.Area + F.HedgeWidth * 0.5f;
            foreach (var (outer, min, max) in _areas)
            {
                if (p.X < min.X || p.Y < min.Y || p.X > max.X || p.Y > max.Y) continue;
                if (PlanRings.InsideByCrossings(outer, p) || PlanRings.DistanceToRing(p, outer) < pad) return true;
            }
            return false;
        }

        /// <summary>The clear pieces of a hedge line, sampled every <see cref="H.Sample"/>, each at least <see cref="H.MinLength"/> long.</summary>
        public IReadOnlyList<IReadOnlyList<Vector2>> Clip(IReadOnlyList<Vector2> line)
        {
            var pieces = new List<IReadOnlyList<Vector2>>();
            var current = new List<Vector2>();
            Vector2? lastClear = null;
            void Close()
            {
                if (lastClear is { } end && current.Count > 0 && current[^1] != end) current.Add(end);
                // Each piece's ends pull back a little from what stopped it.
                if (current.Count >= 2 && PlanLines.Length(current) >= H.MinLength)
                    pieces.Add(BoundaryFence.Offset(current, 0f, _ => true, H.Trim * 0.5f, H.Trim * 0.5f));
                current = [];
                lastClear = null;
            }
            for (var i = 0; i + 1 < line.Count; i++)
            {
                Vector2 p = line[i], q = line[i + 1];
                var length = Vector2.Distance(p, q);
                var steps = Math.Max(1, (int)MathF.Ceiling(length / H.Sample));
                for (var k = i == 0 ? 0 : 1; k <= steps; k++)
                {
                    var at = p + (q - p) * ((float)k / steps);
                    if (Blocked(at)) { Close(); continue; }
                    // Keep the line's vertices and the first sample after a gap; the last clear sample closes a piece.
                    if (current.Count == 0 || k == steps) current.Add(at);
                    lastClear = at;
                }
            }
            Close();
            return pieces;
        }

    }
}

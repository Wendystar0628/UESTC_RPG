using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Zones.North.Framework;
using F = Campus.Gen.Zones.North.NorthTune.Fence;
using G = Campus.Gen.Zones.North.NorthTune.Gate;

namespace Campus.Gen.Zones.North.Clusters;

public enum GateKind { Vehicle, Footway }

/// <summary>
/// A gate where a road crosses the cluster polygon. Frame: origin on the fence line, X along the fence, +Z out of the
/// cluster. RoadWidth is the stretch of fence line the road covers; Span is the local x range the whole gate occupies (the
/// fence stops at both ends).
/// </summary>
public sealed record GateSpot(PlanFrame Frame, GateKind Kind, float RoadWidth, bool Main, bool Open, float SpanMinus, float SpanPlus)
{
    public float LaneHalf => RoadWidth * 0.5f + G.LaneMargin;
    public float PassageWidth => Open ? G.OpenPassage : G.Passage;
    /// <summary>Local x of the turnstile bank's start (the island's outer edge) and end.</summary>
    public float BankStart => LaneHalf + G.IslandWidth;
    public float BankEnd => BankStart + G.Passages * PassageWidth + (G.Passages + 1) * G.CabinetWidth;
    public float HouseStart => BankEnd + G.HouseGap;
    public float HouseEnd => HouseStart + G.HouseWidth;

    /// <summary>Cabinet centres along local x, outer cabinets flush with the bank ends.</summary>
    public IEnumerable<float> Cabinets()
    {
        for (var k = 0; k <= G.Passages; k++) yield return BankStart + G.CabinetWidth * 0.5f + k * (PassageWidth + G.CabinetWidth);
    }
}

/// <summary>One unbroken stretch of fence: posts on the fence line and the hedge line just inside it.</summary>
public sealed record FenceRun(IReadOnlyList<Vector2> Posts, IReadOnlyList<Vector2> Hedge);

/// <summary>
/// The fence line of one cluster, computed once per world: the OSM polygon (which traces the kerb) resampled at the post
/// pitch and pulled inside by <see cref="F.Inset"/>, further where a road runs alongside; gates where roads (or the
/// cluster's design drive) cross it; the
/// runs left after cutting the gates and the stretches inside buildings (their walls close the cluster there).
/// </summary>
public sealed class ClusterLayout
{
    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<string, ClusterLayout?>> Cache = new();
    private static readonly object Sync = new();

    private ClusterLayout(NorthTune.Cluster cluster, IReadOnlyList<GateSpot> gates, IReadOnlyList<FenceRun> runs)
    {
        Cluster = cluster;
        Gates = gates;
        Runs = runs;
    }

    public NorthTune.Cluster Cluster { get; }
    public IReadOnlyList<GateSpot> Gates { get; }
    public IReadOnlyList<FenceRun> Runs { get; }
    public GateSpot? MainGate => Gates.FirstOrDefault(g => g.Main);

    /// <summary>The layout of a cluster, or null when the world has no such polygon (a test world, another map).</summary>
    public static ClusterLayout? Of(IWorldModel world, NorthTune.Cluster cluster)
    {
        ArgumentNullException.ThrowIfNull(world);
        lock (Sync)
        {
            var byKey = Cache.GetValue(world, _ => []);
            if (!byKey.TryGetValue(cluster.Key, out var layout)) byKey[cluster.Key] = layout = Compute(world, cluster);
            return layout;
        }
    }

    private readonly record struct Road(Vector2 A, Vector2 B, Vector2 Dir, float Half);
    /// <summary>Width is the stretch of fence line the road covers (wider than the road when it crosses skewed).</summary>
    private readonly record struct Crossing(float S, Vector2 Point, float Width, float RoadWidth, Vector2 Along, Vector2 Inward);
    private readonly record struct Sample(float S, Vector2 Ring, Vector2 Inward, Vector2 Tangent);

    private static ClusterLayout? Compute(IWorldModel world, NorthTune.Cluster cluster)
    {
        if (!world.Features.TryGetValue(new(cluster.Id), out var feature)) return null;
        var ring = Plan.OuterRing(feature);
        if (ring is null || ring.Count < 3) return null;
        var sign = Plan.SignedArea2(ring) > 0 ? 1f : -1f;
        float minX = ring.Min(p => p.X) - 20, maxX = ring.Max(p => p.X) + 20, minZ = ring.Min(p => p.Y) - 20, maxZ = ring.Max(p => p.Y) + 20;
        bool Near(Vector2 p) => p.X >= minX && p.X <= maxX && p.Y >= minZ && p.Y <= maxZ;

        var roads = new List<Road>();
        void AddRoad(IReadOnlyList<Vector2> pts, float width)
        {
            var half = (width > 0 ? width : F.DefaultRoadWidth) * 0.5f;
            for (var i = 0; i + 1 < pts.Count; i++)
                if (Near(pts[i]) || Near(pts[i + 1]))
                {
                    var dir = Plan.Unit(pts[i + 1] - pts[i]);
                    if (dir != Vector2.Zero) roads.Add(new(pts[i], pts[i + 1], dir, half));
                }
        }
        foreach (var line in world.Network.Roads) AddRoad(line.Line.Points, line.Width);
        // A cluster no network road enters gets a design drive in the overlay, tagged with the cluster (props.cluster).
        foreach (var drive in world.Features.Values.Where(f => f.Kind.Value == "road" && f.Geometry is PolylineGeometry && Drives(f, cluster.Id))
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            AddRoad(((PolylineGeometry)drive.Geometry).Line.Points, Width(drive.Props));

        // Resample the ring; inward normals are mitred at the original corners.
        var samples = new List<Sample>();
        var perimeter = 0f;
        for (var e = 0; e < ring.Count; e++)
        {
            Vector2 a = ring[e], b = ring[(e + 1) % ring.Count];
            var length = Vector2.Distance(a, b);
            var t = (b - a) / length;
            var inward = new Vector2(-t.Y, t.X) * sign;
            var n = Math.Max(1, (int)MathF.Ceiling(length / F.PostPitch));
            for (var k = 0; k < n; k++)
            {
                var normal = inward;
                if (k == 0)
                {
                    var prev = Plan.Unit(a - ring[(e + ring.Count - 1) % ring.Count]);
                    var prevInward = new Vector2(-prev.Y, prev.X) * sign;
                    var mid = Plan.Unit(prevInward + inward);
                    var cos = Vector2.Dot(mid, inward);
                    normal = mid == Vector2.Zero ? inward : mid / MathF.Max(cos, 0.5f);
                }
                samples.Add(new(perimeter + length * k / n, a + (b - a) * ((float)k / n), normal, t));
            }
            perimeter += length;
        }

        // Inset per sample (signed, along the inward normal): roads alongside push the fence away from themselves, inward
        // when the road is outside the fence, outward when it runs just inside the polygon.
        var inset = new float[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            var d = F.Inset;
            for (var pass = 0; pass < 2; pass++)
                foreach (var road in roads)
                {
                    if (MathF.Abs(Vector2.Dot(road.Dir, samples[i].Tangent)) < 0.5f) continue;
                    var p = samples[i].Ring + samples[i].Inward * d;
                    var need = road.Half + F.RoadClearance;
                    var closest = Plan.Closest(p, road.A, road.B);
                    if (Vector2.Distance(p, closest) >= need) continue;
                    var across = Vector2.Dot(closest - samples[i].Ring, samples[i].Inward);
                    d = across < d ? across + need : across - need;
                }
            inset[i] = Math.Clamp(d, -F.MaxInset * 0.5f, F.MaxInset);
        }
        // A push reaches two posts either side, so the fence steps around a road instead of notching.
        var smooth = new float[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            float deeper = inset[i], shallower = inset[i];
            for (var k = -2; k <= 2; k++)
            {
                var v = inset[(i + k + samples.Count) % samples.Count];
                deeper = MathF.Max(deeper, v);
                shallower = MathF.Min(shallower, v);
            }
            var pushedIn = deeper > F.Inset + 0.01f;
            var pulledOut = shallower < F.Inset - 0.01f;
            smooth[i] = pushedIn && !pulledOut ? deeper : pulledOut && !pushedIn ? shallower : inset[i];
        }
        var posts = new Vector2[samples.Count];
        var hedge = new Vector2[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            posts[i] = samples[i].Ring + samples[i].Inward * smooth[i];
            hedge[i] = samples[i].Ring + samples[i].Inward * (smooth[i] + F.HedgeOffset);
        }

        // Road crossings on the fence line itself, merged when closer than 3 m along the ring (the wider road wins).
        var crossings = new List<Crossing>();
        for (var i = 0; i < samples.Count; i++)
        {
            var j = (i + 1) % samples.Count;
            Vector2 p = posts[i], q = posts[j];
            var length = Vector2.Distance(p, q);
            if (length < 1e-3f) continue;
            var along = (q - p) / length;
            var step = j == 0 ? perimeter - samples[i].S : samples[j].S - samples[i].S;
            foreach (var road in roads)
            {
                var cos = MathF.Abs(Vector2.Dot(road.Dir, along));
                if (Plan.Intersect(p, q, road.A, road.B) is not { } hit || cos >= 0.85f) continue;
                // A skewed road cuts a wider stretch of the fence line than its own width.
                var width = road.Half * 2 / MathF.Max(MathF.Sqrt(1 - cos * cos), 0.6f);
                var crossing = new Crossing(Wrap(samples[i].S + hit.T * step, perimeter), hit.Point, width, road.Half * 2, along, samples[i].Inward);
                var near = crossings.FindIndex(c => Arc(c.S, crossing.S, perimeter) < 3f);
                if (near < 0) crossings.Add(crossing);
                else if (crossings[near].Width < crossing.Width) crossings[near] = crossing;
            }
        }
        crossings.Sort((x, y) => x.S.CompareTo(y.S));

        // Gates: the frame sits on the fence line at the crossing; X follows the ring direction or its reverse so +Z is outward.
        var gates = new List<GateSpot>();
        var spans = new List<(float From, float To, Vector2 FromEnd, Vector2 ToEnd, Vector2 Inward)>();
        var mainIndex = -1;
        var best = 15f;
        for (var c = 0; c < crossings.Count; c++)
        {
            var d = Vector2.Distance(crossings[c].Point, cluster.MainGate);
            if (crossings[c].RoadWidth >= G.VehicleRoad && d < best) { best = d; mainIndex = c; }
        }
        for (var c = 0; c < crossings.Count; c++)
        {
            var crossing = crossings[c];
            var normal = new Vector2(-crossing.Along.Y, crossing.Along.X);
            var inward = Vector2.Dot(normal, crossing.Inward) > 0 ? normal : -normal;
            var frame = PlanFrame.Along(crossing.Point, crossing.Along);
            var forward = Vector2.Dot(frame.W, inward) < 0;
            if (!forward) frame = PlanFrame.Along(crossing.Point, -crossing.Along);
            var kind = crossing.RoadWidth >= G.VehicleRoad ? GateKind.Vehicle : GateKind.Footway;
            var main = c == mainIndex;
            var open = main && cluster.Open;
            var probe = new GateSpot(frame, kind, crossing.Width, main, open, 0, 0);
            float minus, plus;
            if (kind == GateKind.Footway) { minus = plus = crossing.Width * 0.5f + 0.15f + G.PierSize; }
            else
            {
                minus = probe.LaneHalf + G.PierSize;
                plus = (main ? probe.HouseEnd : probe.BankEnd + G.PierSize) + 0.05f;
            }
            gates.Add(probe with { SpanMinus = minus, SpanPlus = plus });
            Vector2 lo = frame.P(-minus, 0), hi = frame.P(plus, 0);
            spans.Add(forward ? (crossing.S - minus, crossing.S + plus, lo, hi, inward) : (crossing.S - plus, crossing.S + minus, hi, lo, inward));
        }

        // Building rings near the cluster close the fence where it runs into them.
        var buildings = new List<IReadOnlyList<Vector2>>();
        foreach (var other in world.Features.Values)
            foreach (var r in Plan.BuildingRings(other))
                if (r.Count >= 3 && r.Any(Near)) buildings.Add(r);

        var keep = new bool[samples.Count];
        for (var i = 0; i < samples.Count; i++)
            keep[i] = !spans.Any(sp => InSpan(samples[i].S, sp.From, sp.To, perimeter)) && !buildings.Any(r => Plan.Contains(r, posts[i]));

        // Runs of kept samples; a run that meets a gate ends exactly on the gate's end, one into a building ends inside it.
        var runs = new List<FenceRun>();
        var startAt = Array.IndexOf(keep, false);
        if (startAt < 0)
        {
            runs.Add(Divided([.. posts, posts[0]], [.. hedge, hedge[0]]));
            return new(cluster, gates, runs);
        }
        var current = new List<int>();
        for (var k = 1; k <= samples.Count; k++)
        {
            var i = (startAt + k) % samples.Count;
            if (keep[i]) { current.Add(i); continue; }
            Flush(current, i);
            current = [];
        }
        Flush(current, startAt);
        return new(cluster, gates, runs);

        void Flush(List<int> run, int next)
        {
            if (run.Count == 0) return;
            var p = new List<Vector2>();
            var h = new List<Vector2>();
            var before = (run[0] - 1 + samples.Count) % samples.Count;
            End(before, run[0], p, h);
            foreach (var i in run) { p.Add(posts[i]); h.Add(hedge[i]); }
            End(next, run[^1], p, h);
            if (p.Count >= 2) runs.Add(Divided(p, h));
        }

        // The end of a run at a cut sample: the gate's end point when the cut was a gate, else the cut sample itself.
        void End(int cut, int kept, List<Vector2> p, List<Vector2> h)
        {
            foreach (var sp in spans)
            {
                if (!InSpan(samples[cut].S, sp.From, sp.To, perimeter)) continue;
                // The nearer gate end: where the offset line folds at a tight corner, arc order and plan order can disagree.
                var end = Vector2.Distance(posts[kept], sp.FromEnd) <= Vector2.Distance(posts[kept], sp.ToEnd) ? sp.FromEnd : sp.ToEnd;
                p.Add(end);
                h.Add(end + sp.Inward * F.HedgeOffset);
                return;
            }
            p.Add(posts[cut]);
            h.Add(hedge[cut]);
        }
    }

    /// <summary>Extra posts on stretches longer than the post pitch (a gate end or a corner chord), hedge points alongside.</summary>
    private static FenceRun Divided(List<Vector2> posts, List<Vector2> hedge)
    {
        var p = new List<Vector2> { posts[0] };
        var h = new List<Vector2> { hedge[0] };
        for (var i = 1; i < posts.Count; i++)
        {
            var n = (int)MathF.Ceiling(Vector2.Distance(posts[i - 1], posts[i]) / F.PostPitch);
            for (var k = 1; k < n; k++)
            {
                var t = (float)k / n;
                p.Add(posts[i - 1] + (posts[i] - posts[i - 1]) * t);
                h.Add(hedge[i - 1] + (hedge[i] - hedge[i - 1]) * t);
            }
            p.Add(posts[i]);
            h.Add(hedge[i]);
        }
        return new(p, h);
    }

    private static bool Drives(WorldFeature f, string cluster) =>
        f.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("cluster", out var c) && c.ValueKind == JsonValueKind.String && c.GetString() == cluster;

    private static float Width(JsonElement? props) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetSingle() : 0f;

    private static float Wrap(float s, float perimeter) => ((s % perimeter) + perimeter) % perimeter;

    /// <summary>Shortest distance along a closed ring between two arc positions.</summary>
    private static float Arc(float a, float b, float perimeter)
    {
        var d = MathF.Abs(Wrap(a, perimeter) - Wrap(b, perimeter));
        return MathF.Min(d, perimeter - d);
    }

    private static bool InSpan(float s, float from, float to, float perimeter) => Wrap(s - from, perimeter) <= to - from;
}

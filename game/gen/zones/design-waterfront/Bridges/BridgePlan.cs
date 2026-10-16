using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront.Bridges;

/// <summary>What a strip of the deck carries.</summary>
internal enum BandKind { Carriage, Walk }

/// <summary>One strip of the deck across the bridge, from lateral offset U0 to U1 (U0 &lt; U1).</summary>
internal readonly record struct Band(float U0, float U1, BandKind Kind);

/// <summary>An abutment wall in plan: the four corners of its parallelogram, bank side first.</summary>
internal readonly record struct Abutment(Vector2 A, Vector2 B, Vector2 C, Vector2 D);

/// <summary>
/// The plan of one bridge, resolved from its overlay registration and the network lines it names (MAP-QSH-ADDITIONS §2.12).
/// Bridge frame: <c>P(s, u) = Origin + Axis * s + Normal * u</c>, s along the axis line, u across (Normal = Axis turned so
/// that X × Y = Z holds for the local frame). Each deck end is a straight line <c>s = A + K * u</c>: first fitted through the
/// end points of every line the bridge carries, then pulled in to the water it crosses (<see cref="FitToWater"/>), parallel to
/// the bank and a bearing margin behind the abutment; the remaining land part of each line is ordinary ground. Several network lines (中东区的车行桥与两侧人行桥) become one deck; the lines and their topology stay as they are.
/// </summary>
internal sealed class BridgePlan
{
    private BridgePlan(FeatureId id, Vector2 origin, Vector2 axis, IReadOnlyList<Band> bands, float startA, float startK,
        float endA, float endK, float roadY, IReadOnlyList<FeatureId> lines)
    {
        Id = id; Origin = origin; Axis = axis; Normal = new(-axis.Y, axis.X); Bands = bands;
        StartA = startA; StartK = startK; EndA = endA; EndK = endK; RoadY = roadY; Lines = lines;
        Left = bands.Min(b => b.U0); Right = bands.Max(b => b.U1);
    }

    public FeatureId Id { get; }
    public Vector2 Origin { get; }
    public Vector2 Axis { get; }
    public Vector2 Normal { get; }
    public IReadOnlyList<Band> Bands { get; }
    /// <summary>Start end line s = StartA + StartK * u; end line s = EndA + EndK * u.</summary>
    public float StartA { get; private set; }
    public float StartK { get; private set; }
    public float EndA { get; private set; }
    public float EndK { get; private set; }
    /// <summary>Carriageway surface; walking bands are <see cref="WalkY"/>.</summary>
    public float RoadY { get; }
    public float WalkY => RoadY + T.PavingRise;
    public bool HasCarriage => Bands.Any(b => b.Kind == BandKind.Carriage);
    /// <summary>Outer edges of the walking surface; the parapet bases stand outside them.</summary>
    public float Left { get; }
    public float Right { get; }
    /// <summary>Outer faces of the deck slab (fascia).</summary>
    public float OuterLeft => Left - T.BaseWidth;
    public float OuterRight => Right + T.BaseWidth;
    public IReadOnlyList<FeatureId> Lines { get; }
    public IReadOnlyList<Abutment> Abutments { get; private set; } = [];

    public float StartS(float u) => StartA + StartK * u;
    public float EndS(float u) => EndA + EndK * u;
    public Vector2 P(float s, float u) => Origin + Axis * s + Normal * u;
    public Vector3 P(float s, float u, float y) { var p = P(s, u); return new(p.X, y, p.Y); }
    public Vector2 Centre => P((StartS(0) + EndS(0)) * 0.5f, (OuterLeft + OuterRight) * 0.5f);

    /// <summary>Corners of a strip from u0 to u1 over the whole deck length, counterclockwise in (X, Z).</summary>
    public Vector2[] Strip(float u0, float u1) => Ccw([P(StartS(u0), u0), P(StartS(u1), u1), P(EndS(u1), u1), P(EndS(u0), u0)]);

    /// <summary>The deck footprint, slab faces included (not the base overhang).</summary>
    public Vector2[] Footprint => Strip(OuterLeft, OuterRight);

    internal static Vector2[] Ccw(Vector2[] ring)
    {
        var area = 0.0;
        for (var i = 0; i < ring.Length; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Length];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        if (area < 0) Array.Reverse(ring);
        return ring;
    }

    /// <summary>Resolves every bridge registered in the overlay (kind <see cref="T.BridgeKind"/>), in ID order.</summary>
    public static IReadOnlyList<BridgePlan> All(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var lines = world.Network.Bridges.Concat(world.Network.Roads).GroupBy(l => l.Id.Value, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var plans = new List<BridgePlan>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == T.BridgeKind).OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var plan = Resolve(world, feature, lines);
            plan.FitToWater(world);
            plans.Add(plan);
        }
        return plans;
    }

    private static BridgePlan Resolve(IWorldModel world, WorldFeature feature, IReadOnlyDictionary<string, NetworkLine> lines)
    {
        if (feature.Props is not { ValueKind: JsonValueKind.Object } props)
            throw new InvalidOperationException($"{feature.Id.Value} needs props naming its axis line.");
        NetworkLine Line(string id) => lines.TryGetValue(id, out var line) ? line
            : throw new InvalidOperationException($"{feature.Id.Value} names {id}, which is not a network bridge or road.");
        var axisLine = Line(Text(props, "axis") ?? throw new InvalidOperationException($"{feature.Id.Value} has no axis line."));
        var carriage = props.TryGetProperty("carriage", out var c) && c.ValueKind == JsonValueKind.True;
        var walks = props.TryGetProperty("walks", out var w) && w.ValueKind == JsonValueKind.Array
            ? w.EnumerateArray().Select(e => Line(e.GetString()!)).ToArray() : [];
        var sidewalk = props.TryGetProperty("sidewalk", out var sw) && sw.ValueKind == JsonValueKind.Number ? sw.GetSingle() : T.DefaultSidewalk;

        var points = axisLine.Line.Points;
        var origin = points[0];
        var axis = Vector2.Normalize(points[^1] - points[0]);
        var normal = new Vector2(-axis.Y, axis.X);
        (float S, float U) Local(Vector2 p) { var d = p - origin; return (Vector2.Dot(d, axis), Vector2.Dot(d, normal)); }

        var half = Width(axisLine) * 0.5f;
        var bands = new List<Band>();
        var starts = new List<(float S, float U)> { Local(points[0]) };
        var ends = new List<(float S, float U)> { Local(points[^1]) };
        if (carriage)
        {
            bands.Add(new(-half, half, BandKind.Carriage));
            float? left = null, right = null;
            foreach (var walk in walks)
            {
                var a = Local(walk.Line.Points[0]); var b = Local(walk.Line.Points[^1]);
                if (a.S > b.S) (a, b) = (b, a);
                starts.Add(a); ends.Add(b);
                var u = (a.U + b.U) * 0.5f; var h = Width(walk) * 0.5f;
                if (u < 0) left = Math.Min(left ?? 0f, u - h); else right = Math.Max(right ?? 0f, u + h);
            }
            bands.Add(new(left ?? -half - sidewalk, -half, BandKind.Walk));
            bands.Add(new(half, right ?? half + sidewalk, BandKind.Walk));
        }
        else bands.Add(new(-half, half, BandKind.Walk));

        var (startA, startK) = Fit(starts, min: true);
        var (endA, endK) = Fit(ends, min: false);
        var terrain = world.Terrain;
        var roadY = (terrain.HeightAt(points[0].X, points[0].Y) + terrain.HeightAt(points[^1].X, points[^1].Y)) * 0.5f;
        var ids = new[] { axisLine.Id }.Concat(walks.Select(l => l.Id)).ToArray();
        return new(feature.Id, origin, axis, [.. bands.OrderBy(b => b.U0)], startA, startK, endA, endK, roadY, ids);
    }

    /// <summary>
    /// Least-squares line s = A + K u through the end points, then A shifted so every point lies on the deck
    /// (the start line at or before each start, the end line at or after each end).
    /// </summary>
    private static (float A, float K) Fit(List<(float S, float U)> points, bool min)
    {
        var k = 0f;
        if (points.Count >= 2)
        {
            double mu = points.Average(p => p.U), ms = points.Average(p => p.S), suu = 0, sus = 0;
            foreach (var p in points) { suu += (p.U - mu) * (p.U - mu); sus += (p.U - mu) * (p.S - ms); }
            if (suu > 1e-6) k = (float)(sus / suu);
        }
        var a = min ? points.Min(p => p.S - k * p.U) : points.Max(p => p.S - k * p.U);
        return (a, k);
    }

    /// <summary>
    /// Fits the deck to the water it crosses (MAP-QSH-ADDITIONS §2.12; the network lines often run on well past the banks):
    /// along two sample lines near the deck's outer edges, the first and last crossing of a water outline give the two bank
    /// lines. An abutment stands just behind each, and the deck ends a bearing margin behind the abutment, its end line
    /// parallel to the bank; the rest of each line is ordinary ground (WP-15). No water under the deck: it keeps the line ends
    /// and has no abutments.
    /// </summary>
    private void FitToWater(IWorldModel world)
    {
        var rings = new List<IReadOnlyList<Vector2>>();
        foreach (var f in world.Features.Values.Where(f => f.Kind.Value == "water").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            switch (f.Geometry)
            {
                case PolygonGeometry p: rings.Add(p.Rings.Outer); rings.AddRange(p.Rings.Holes); break;
                case MultiPolygonGeometry m: foreach (var r in m.Polygons) { rings.Add(r.Outer); rings.AddRange(r.Holes); } break;
                default: break;
            }
        const float inset = 0.2f;
        var samples = new[] { OuterLeft + inset, OuterRight - inset };
        var first = new float?[2]; var last = new float?[2];
        for (var i = 0; i < samples.Length; i++)
        {
            var u = samples[i];
            // Searched well past the line ends: on a skewed crossing the far bank can lie beyond them.
            float s0 = StartS(u) - SearchReach, s1 = EndS(u) + SearchReach;
            var a = P(s0, u); var b = P(s1, u);
            foreach (var ring in rings)
                for (var j = 0; j < ring.Count; j++)
                {
                    if (Cross(a, b, ring[j], ring[(j + 1) % ring.Count]) is not { } t) continue;
                    var s = s0 + (s1 - s0) * t;
                    first[i] = Math.Min(first[i] ?? float.MaxValue, s);
                    last[i] = Math.Max(last[i] ?? float.MinValue, s);
                }
        }
        if (first[0] is null && first[1] is null) { Abutments = []; return; }
        float F(int i) => first[i] ?? first[1 - i]!.Value;
        float L(int i) => last[i] ?? last[1 - i]!.Value;
        float uA = samples[0], uB = samples[1];
        // Bank lines s = a + k u through the two samples; water faces of the abutments sit the set-back behind them.
        var kStart = (F(1) - F(0)) / (uB - uA); var aStart = F(0) - kStart * uA - T.AbutmentSetback;
        var kEnd = (L(1) - L(0)) / (uB - uA); var aEnd = L(0) - kEnd * uA + T.AbutmentSetback;
        var behind = T.AbutmentThick + T.BearingMargin;
        // New deck ends: a bearing margin behind the abutments. Where the bank lies past a line's mapped end the deck runs on to it.
        (StartA, StartK) = (aStart - behind, kStart);
        (EndA, EndK) = (aEnd + behind, kEnd);
        float uL = OuterLeft, uR = OuterRight;
        float W(float a, float k, float u) => a + k * u;
        float fl = Math.Max(W(aStart, kStart, uL), StartS(uL) + T.AbutmentThick), fr = Math.Max(W(aStart, kStart, uR), StartS(uR) + T.AbutmentThick);
        float ll = Math.Min(W(aEnd, kEnd, uL), EndS(uL) - T.AbutmentThick), lr = Math.Min(W(aEnd, kEnd, uR), EndS(uR) - T.AbutmentThick);
        Abutments =
        [
            new(P(fl, uL), P(fr, uR), P(fr - T.AbutmentThick, uR), P(fl - T.AbutmentThick, uL)),
            new(P(ll, uL), P(lr, uR), P(lr + T.AbutmentThick, uR), P(ll + T.AbutmentThick, uL)),
        ];
    }

    /// <summary>Parameter t on a→b where it crosses segment c→d, or null.</summary>
    private static float? Cross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var r = b - a; var q = d - c;
        var den = (double)r.X * q.Y - (double)r.Y * q.X;
        if (Math.Abs(den) < 1e-9) return null;
        var ac = c - a;
        var t = ((double)ac.X * q.Y - (double)ac.Y * q.X) / den;
        var v = ((double)ac.X * r.Y - (double)ac.Y * r.X) / den;
        return t is >= 0 and <= 1 && v is >= 0 and <= 1 ? (float)t : null;
    }

    /// <summary>How far past the mapped line ends the banks are looked for.</summary>
    private const float SearchReach = 12f;

    private static float Width(NetworkLine line) => line.Width > 0 ? line.Width : 2.5f;

    private static string? Text(JsonElement props, string name) =>
        props.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

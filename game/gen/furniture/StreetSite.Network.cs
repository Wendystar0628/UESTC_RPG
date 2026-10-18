using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

internal sealed partial class StreetSite
{
    public static string? RoadName(NetworkLine line)
    {
        if (line.Props is not { ValueKind: JsonValueKind.Object } props) return null;
        foreach (var name in new[] { "name:zh", "name" })
            if (props.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
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
    /// What a point lands on, for an anchor's fallback (SYS-ATLAS data still under review): a building's or water's outline,
    /// a carriageway (a vehicle road's surface or paved area), else open ground. Checked in that order.
    /// </summary>
    public Landing LandingAt(Vector2 p)
    {
        foreach (var kind in new[] { "building", "water" })
            foreach (var o in _outlines.In(p))
                if (o.Kind == kind && p.X >= o.Min.X && p.X <= o.Max.X && p.Y >= o.Min.Y && p.Y <= o.Max.Y && PlanGeometry.Contains(o.Rings, p))
                    return kind == "building" ? Landing.Building : Landing.Water;
        if (!OffCarriageways(p, 0f, 0f) || _lanes.In(p).Any(rings => PlanGeometry.Contains(rings, p))) return Landing.Carriageway;
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
}

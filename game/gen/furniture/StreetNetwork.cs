using System.Text.Json;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

/// <summary>Classifies street lines and samples joined road runs without placement state.</summary>
internal static class StreetNetwork
{
    /// <summary>Half width of a network line's surface (the width WP-15 paves; never below a footpath's).</summary>
    public static float HalfWidth(NetworkLine line) => MathF.Max(line.Width, 2f) * 0.5f;

    /// <summary>Walks a bench may line and face (WP-15 paves them as walks or pedestrian plazas); steps and cycleways are not.</summary>
    public static bool Strollable(NetworkLine line) => RoadType(line.Props) is "footway" or "path" or "pedestrian";

    /// <summary>True for a pedestrian network line (a walk or steps), the lines a plaza may carry.</summary>
    public static bool IsPedestrian(NetworkLine line) => Pedestrian(RoadType(line.Props));

    /// <summary>Pedestrian paving types: the strollable walks and steps.</summary>
    internal static bool Pedestrian(string? type) => type is "footway" or "path" or "pedestrian" or "steps";

    /// <summary>Road type as WP-15 reads it (GroundClassifier): <c>highway</c>.</summary>
    internal static string? RoadType(JsonElement? props) =>
        WorldProps.TryText(props, "highway");

    public static string? RoadName(NetworkLine line) =>
        WorldProps.TryText(line.Props, "name:zh") ?? WorldProps.TryText(line.Props, "name");

    /// <summary>A network line's road class as WP-15 reads it: <c>highway</c> (the field <see cref="FurnitureStyle.TierOf"/> grades by).</summary>
    public static string? RoadClass(NetworkLine line) => RoadType(line.Props);

    /// <summary>True for a line that runs under cover (a tunnel, a building passage, a covered way): nothing stands along it.</summary>
    public static bool Covered(NetworkLine line) =>
        WorldProps.TryText(line.Props, "tunnel") is not null || WorldProps.TryText(line.Props, "covered") == "yes";

    /// <summary>The nearest point on any carriageway (not a footpath) to <paramref name="p"/> within <paramref name="reach"/>, with its line.</summary>
    public static (Vector2 Point, Vector2 Along, NetworkLine Line)? NearestCarriageway(IWorldModel world, Vector2 p, float reach)
    {
        (Vector2, Vector2, NetworkLine)? best = null;
        var bestD = reach * reach;
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            if (!Carriageway(road)) continue;
            var near = PlanLines.Project(road.Line.Points, p);
            var d = Vector2.DistanceSquared(p, near.Point);
            if (d >= bestD) continue;
            bestD = d;
            best = (near.Point, near.Tangent, road);
        }
        return best;
    }

    /// <summary>Vehicle roads WP-15 paves as carriageway (GroundClassifier); footpaths, steps and cycleways are not.</summary>
    public static bool Carriageway(NetworkLine line) => CarriagewayType(WorldProps.TryText(line.Props, "highway"));

    internal static bool CarriagewayType(string? type) =>
        type is "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street";

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

        public Run(List<Vector2> points)
        {
            _points = points;
            Length = (float)PlanLines.Length(points);
        }

        public float Length { get; }

        public Vector2 At(float s) => PlanLines.Sample(_points, s).Point;

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

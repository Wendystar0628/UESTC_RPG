using Campus.Contracts;
using Campus.Contracts.Maps;
using System.Numerics;

namespace Campus.Gen.World;

/// <summary>Continuous extension of the prescribed exposed-shore profile into the water interior.</summary>
internal sealed class TerrainWaterJoin
{
    private readonly record struct Contribution(TerrainDifferential Distance, TerrainDifferential Weight);
    private sealed record Transition(Vector2 A, Vector2 B, double Start, double End, double RunLength);
    private readonly TerrainRingIndex? _lakeEdges;
    private readonly TerrainRingIndex? _channelEdges;
    private readonly Transition[] _transitions;
    private readonly TerrainRegion _domain;
    private readonly TerrainRegion _lakeArea;

    public TerrainWaterJoin(TerrainRegion domain, TerrainRegion lakeArea)
    {
        _domain = domain; _lakeArea = lakeArea;
        var lake = new List<(Vector2 A, Vector2 B)>(); var channel = new List<(Vector2 A, Vector2 B)>();
        var transitions = new List<Transition>();
        foreach (var polygon in domain.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
            {
                var count = ring.Count; var labels = new bool[count];
                for (var i = 0; i < count; i++)
                {
                    var a = ring[i]; var c = ring[(i + 1) % count];
                    // A source boundary midpoint can round to the exterior in the integer
                    // containment predicate. Millimetre topology still identifies that same
                    // exposed edge as lake shore; it must not become an invented Hard segment.
                    labels[i] = lakeArea.Distance(((double)a.X + c.X) / 2, ((double)a.Y + c.Y) / 2).Height <= 0.001;
                    if (labels[i]) lake.Add((a, c));
                }
                if (!labels.Any(value => value))
                { for (var i = 0; i < count; i++) channel.Add((ring[i], ring[(i + 1) % count])); continue; }
                for (var start = 0; start < count; start++)
                {
                    if (labels[start] || !labels[(start + count - 1) % count]) continue;
                    var run = new List<(Vector2 A, Vector2 B, double Start, double End)>(); var station = 0d; var index = start;
                    do
                    {
                        var a = ring[index]; var c = ring[(index + 1) % count];
                        var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y; var end = station + Math.Sqrt(dx * dx + dz * dz);
                        run.Add((a, c, station, end)); station = end; index = (index + 1) % count;
                    } while (!labels[index]);
                    foreach (var edge in run)
                    {
                        var cuts = new[] { edge.Start, edge.End, TerrainBridgeBanks.TransitionLength, station - TerrainBridgeBanks.TransitionLength }
                            .Where(s => s >= edge.Start && s <= edge.End).Distinct().OrderBy(s => s).ToArray();
                        for (var i = 0; i + 1 < cuts.Length; i++)
                        {
                            var a = At(cuts[i]); var c = At(cuts[i + 1]);
                            if (a == c) continue;
                            if (cuts[i] >= TerrainBridgeBanks.TransitionLength && cuts[i + 1] <= station - TerrainBridgeBanks.TransitionLength) channel.Add((a, c));
                            else transitions.Add(new(a, c, cuts[i], cuts[i + 1], station));
                        }
                        Vector2 At(double s)
                        {
                            if (s == edge.Start) return edge.A; if (s == edge.End) return edge.B;
                            var t = (s - edge.Start) / (edge.End - edge.Start);
                            return new((float)(edge.A.X + ((double)edge.B.X - edge.A.X) * t), (float)(edge.A.Y + ((double)edge.B.Y - edge.A.Y) * t));
                        }
                    }
                }
            }
        _lakeEdges = lake.Count == 0 ? null : new(lake); _channelEdges = channel.Count == 0 ? null : new(channel);
        _transitions = transitions.ToArray();
    }

    public TerrainDifferential Weight(double x, double z, double width)
    {
        var near = ShoreWeight(x, z, width); var domainDistance = _domain.Distance(x, z);
        if (domainDistance.Height >= 0) return near;
        var distance = _lakeArea.Distance(x, z);
        TerrainDifferential interior;
        if (distance.Height <= 0) interior = TerrainDifferential.Constant(1);
        else if (distance.Height >= width) interior = TerrainDifferential.Constant(0);
        else
        {
            var t = distance.Height / width; var slope = -6 * t * (1 - t) / width;
            interior = new(1 - TerrainMath.Hermite(t), distance.Dx * slope, distance.Dz * slope);
        }
        // The existing minimum 2 m fillet connects the fixed boundary extension to the
        // interior field. Widening the interior's horizontal profile must not also preserve
        // its narrow boundary pattern deeper and deeper into the bed.
        const double normalFillet = 2;
        var inward = -domainDistance; if (inward.Height >= normalFillet) return interior;
        var ratio = inward.Height / normalFillet; var derivative = 6 * ratio * (1 - ratio) / normalFillet;
        var depthWeight = new TerrainDifferential(TerrainMath.Hermite(ratio), derivative * inward.Dx, derivative * inward.Dz);
        return near + TerrainDifferential.Product(interior - near, depthWeight);
    }

    public IReadOnlyList<TerrainBankInterval> HardIntervals(bool lakeHard, bool channelHard)
    {
        if (lakeHard && channelHard) return TerrainBankGeometry.WholeRings(_domain);
        var result = new List<TerrainBankInterval>();
        if (!lakeHard && !channelHard) return result;
        foreach (var item in TerrainBankGeometry.Rings(_domain))
        {
            var points = item.Path.Line.Points; var count = points.Count - 1; var stations = new double[count + 1]; var lake = new bool[count];
            for (var i = 0; i < count; i++)
            {
                var a = points[i]; var c = points[i + 1]; var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                stations[i + 1] = stations[i] + Math.Sqrt(dx * dx + dz * dz);
                lake[i] = _lakeArea.Distance(((double)a.X + c.X) / 2, ((double)a.Y + c.Y) / 2).Height <= 0.001;
            }
            if (lake.All(value => value == lake[0]))
            {
                if (lake[0] ? lakeHard : channelHard) result.Add(new(item.Polygon, item.Ring, 0, item.Path.Length, item.Path.Length, [], []));
                continue;
            }
            for (var start = 0; start < count; start++)
            {
                if (lake[start] == lake[(start + count - 1) % count] || !(lake[start] ? lakeHard : channelHard)) continue;
                var length = 0d; var edge = start;
                do { length += stations[edge + 1] - stations[edge]; edge = (edge + 1) % count; } while (lake[edge] == lake[start]);
                var from = stations[start]; var to = from + length;
                if (!lake[start]) { from += TerrainBridgeBanks.TransitionLength; to -= TerrainBridgeBanks.TransitionLength; }
                if (to > from) result.Add(new(item.Polygon, item.Ring, from, to, item.Path.Length, [], []));
            }
        }
        return result;
    }

    private TerrainDifferential ShoreWeight(double x, double z, double width)
    {
        var capacity = _transitions.Length + 2;
        Span<Contribution> values = capacity <= 64 ? stackalloc Contribution[capacity] : new Contribution[capacity];
        var count = 0;
        if (_lakeEdges is not null) values[count++] = Constant(_lakeEdges.Nearest(x, z), 1, x, z);
        if (_channelEdges is not null) values[count++] = Constant(_channelEdges.Nearest(x, z), 0, x, z);
        foreach (var edge in _transitions)
        {
            var dx = (double)edge.B.X - edge.A.X; var dz = (double)edge.B.Y - edge.A.Y;
            var lengthSquared = dx * dx + dz * dz; var length = Math.Sqrt(lengthSquared);
            var raw = ((x - edge.A.X) * dx + (z - edge.A.Y) * dz) / lengthSquared;
            var t = Math.Max(0, Math.Min(1, raw)); var px = edge.A.X + dx * t; var pz = edge.A.Y + dz * t;
            var qx = x - px; var qz = z - pz; var distance = Math.Sqrt(qx * qx + qz * qz);
            var station = edge.Start + (edge.End - edge.Start) * t;
            var (left, leftSlope) = EndWeight(station); var (right, rightSlope) = EndWeight(edge.RunLength - station);
            var weight = 1 - (1 - left) * (1 - right);
            var along = leftSlope * (1 - right) - rightSlope * (1 - left);
            var scale = raw > 0 && raw < 1 ? (edge.End - edge.Start) / length : 0;
            values[count++] = new(distance == 0 ? TerrainDifferential.Constant(0) : new(distance, qx / distance, qz / distance),
                new(weight, along * scale * dx / length, along * scale * dz / length));
        }
        var closest = 0;
        for (var i = 1; i < count; i++) if (values[i].Distance.Height < values[closest].Distance.Height) closest = i;
        var minimum = values[closest].Distance;
        if (minimum.Height <= 0.0000001) return values[closest].Weight; // Floating projection limit, not a geometric band.
        var denominator = TerrainDifferential.Constant(0); var numerator = TerrainDifferential.Constant(0);
        for (var i = 0; i < count; i++)
        {
            var distance = values[i].Distance;
            var taper = TerrainDifferential.Constant(1) - (distance - minimum) * (1 / width);
            if (taper.Height <= 0) continue;
            // Normalized inverse-square coordinates remain finite near a shore and equal its
            // prescribed boundary value exactly. Compact support gives exact terminal profiles.
            var ratio = TerrainDifferential.Divide(minimum, distance); var weight = TerrainDifferential.Product(TerrainDifferential.Product(taper, taper), TerrainDifferential.Product(ratio, ratio));
            numerator += TerrainDifferential.Product(weight, values[i].Weight); denominator += weight;
        }
        return TerrainDifferential.Divide(numerator, denominator);
    }

    private static Contribution Constant(TerrainRingIndex.Hit hit, double weight, double x, double z)
    {
        var distance = Math.Sqrt(hit.Squared);
        return new(distance == 0 ? TerrainDifferential.Constant(0) : new(distance, (x - hit.X) / distance, (z - hit.Z) / distance), TerrainDifferential.Constant(weight));
    }
    private static (double Value, double Slope) EndWeight(double station)
    {
        if (station <= 0) return (1, 0); if (station >= TerrainBridgeBanks.TransitionLength) return (0, 0);
        var t = station / TerrainBridgeBanks.TransitionLength;
        return (1 - TerrainMath.Hermite(t), -6 * t * (1 - t) / TerrainBridgeBanks.TransitionLength);
    }
}

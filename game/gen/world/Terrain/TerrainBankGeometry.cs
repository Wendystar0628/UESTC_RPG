using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static class TerrainBankGeometry
{
    public static IEnumerable<(int Polygon, int Ring, TerrainPath Path)> Rings(TerrainRegion region)
    {
        for (var polygon = 0; polygon < region.Polygons.Length; polygon++)
        {
            var index = 0;
            foreach (var ring in new[] { region.Polygons[polygon].Outer }.Concat(region.Polygons[polygon].Holes))
                yield return (polygon, index++, new TerrainPath(new(ring.Append(ring[0]).ToArray())));
        }
    }

    public static IReadOnlyList<TerrainBankInterval> WholeRings(TerrainRegion region) => Rings(region)
        .Select(ring => new TerrainBankInterval(ring.Polygon, ring.Ring, 0, ring.Path.Length, ring.Path.Length, [], [])).ToArray();

    public static IReadOnlyList<TerrainBankInterval> Merge(IEnumerable<TerrainBankInterval> intervals)
    {
        var result = new List<TerrainBankInterval>();
        foreach (var group in intervals.GroupBy(span => (span.Polygon, span.Ring)).OrderBy(group => group.Key.Polygon).ThenBy(group => group.Key.Ring))
        {
            var merged = new List<TerrainBankInterval>();
            foreach (var interval in group.Select(Normalize).OrderBy(span => span.From).ThenBy(span => span.To))
            {
                if (merged.Count > 0 && interval.From <= merged[^1].To) merged[^1] = Combine(merged[^1], interval, interval.To);
                else merged.Add(interval);
            }
            while (merged.Count > 1 && merged[^1].To >= merged[0].From + merged[0].Length)
            {
                var combined = Combine(merged[^1], merged[0], merged[0].To + merged[0].Length);
                merged.RemoveAt(merged.Count - 1); merged.RemoveAt(0); merged.Add(combined);
            }
            result.AddRange(merged.Select(Normalize).OrderBy(span => span.From));
        }
        return result;

        static TerrainBankInterval Normalize(TerrainBankInterval value)
        {
            var extent = value.To - value.From;
            if (extent >= value.Length) return value with { From = 0, To = value.Length };
            var from = value.From % value.Length; if (from < 0) from += value.Length;
            return value with { From = from, To = from + extent };
        }
        static TerrainBankInterval Combine(TerrainBankInterval a, TerrainBankInterval c, double end) => a with
        {
            To = Math.Max(a.To, end),
            Roads = a.Roads.Concat(c.Roads).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray(),
            Bridges = a.Bridges.Concat(c.Bridges).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray()
        };
    }

    public static TerrainBankSpan Create(string id, IReadOnlyList<FeatureId> waters, TerrainRegion region, TerrainBankInterval interval)
    {
        var path = Rings(region).Single(ring => ring.Polygon == interval.Polygon && ring.Ring == interval.Ring).Path;
        if (interval.To - interval.From >= path.Length)
            return new(id, waters, interval.Roads, interval.Bridges, path.Line, 0, path.Length, 0, TerrainBankKind.Hard);
        var transition = TerrainBridgeBanks.TransitionLength;
        if (interval.To - interval.From + 2 * transition >= path.Length)
            throw new WorldDataException("world", id,
                "A partial Hard ring has overlapping end transitions around its entire seam; author an explicit complete bank or split before exporting structural geometry.");
        var start = interval.From - transition; var end = interval.To + transition;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var stations = new SortedSet<double> { start, interval.From, interval.To, end };
            var cumulative = 0d;
            for (var i = 0; i < path.Line.Points.Count; i++)
            {
                if (i > 0)
                {
                    var a = path.Line.Points[i - 1]; var c = path.Line.Points[i]; var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    cumulative += Math.Sqrt(dx * dx + dz * dz);
                }
                for (var turn = (int)Math.Floor(start / path.Length); turn <= (int)Math.Ceiling(end / path.Length); turn++)
                {
                    var candidate = cumulative + turn * path.Length;
                    if (candidate > start && candidate < end) stations.Add(candidate);
                }
            }
            var points = new List<(Vector2 Point, bool From, bool To)>();
            foreach (var station in stations)
            {
                var wrapped = station % path.Length; if (wrapped < 0) wrapped += path.Length;
                var point = path.At(wrapped); var from = station == interval.From; var to = station == interval.To;
                if (points.Count > 0 && points[^1].Point == point)
                    points[^1] = (point, points[^1].From || from, points[^1].To || to);
                else points.Add((point, from, to));
            }
            var coreFrom = 0d; var coreTo = 0d; var length = 0d;
            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    var dx = (double)points[i].Point.X - points[i - 1].Point.X; var dz = (double)points[i].Point.Y - points[i - 1].Point.Y;
                    length += Math.Sqrt(dx * dx + dz * dz);
                }
                if (points[i].From) coreFrom = length;
                if (points[i].To) coreTo = length;
            }
            if (coreFrom >= transition && coreTo <= length - transition)
                return new(id, waters, interval.Roads, interval.Bridges, new(points.Select(point => point.Point).ToArray()),
                    coreFrom, coreTo, transition, TerrainBankKind.Hard);
            // Only compensate final Vector2 rounding: an exported 2 m margin must not become
            // 1.99999 m and violate the shared exact-station contract. The field is unchanged.
            var padding = points.Max(point => 4 * (Math.Abs((double)MathF.BitIncrement(point.Point.X) - point.Point.X)
                + Math.Abs((double)MathF.BitIncrement(point.Point.Y) - point.Point.Y)));
            if (coreFrom < transition) start -= transition - coreFrom + padding;
            if (coreTo > length - transition) end += transition - (length - coreTo) + padding;
        }
        throw new WorldDataException("world", id, "Final shore coordinates cannot retain both exact 2 m transition margins.");
    }
}

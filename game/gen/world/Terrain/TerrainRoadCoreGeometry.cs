using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static class TerrainRoadCoreGeometry
{
    internal readonly record struct CorridorContact(TerrainDifferential Distance, double X, double Z, double Jxx, double Jxz, double Jzz);

    internal static TerrainDifferential CorridorOffsetAt(TerrainRoad road, CorridorContact contact, double x, double z)
    {
        var hit = road.Path.Project(x, z);
        if (contact.Distance.Height <= 0) return new(hit.Offset, hit.OffsetDx, hit.OffsetDz);
        // Use the same continuous exterior extension through both source-end planes.
        // Switching to the nearest core point at a terminal station can jump when the
        // actual footprint cap is not exactly perpendicular to the centreline.
        var sign = hit.Offset < 0 ? -1 : hit.Offset > 0 ? 1 : 0;
        var lateral = new TerrainDifferential(hit.Offset * sign, hit.OffsetDx * sign, hit.OffsetDz * sign) - contact.Distance;
        return lateral.Height <= 0 ? TerrainDifferential.Constant(0) : lateral * sign;
    }

    internal static CorridorContact CorridorContactAt(TerrainRoad road, double shoulder, double x, double z)
    {
        // Source centreline endpoints and their rounded paving polygons join at demo
        // centimetre precision. Exterior shoulders retain their authored geometric width.
        var contactOffset = shoulder == 0 ? TerrainMath.RoadCoreJoinTolerance : shoulder;
        TerrainDifferential distance; double tx; double tz; double curvatureScale;
        if (road.Footprint is { } footprint)
        {
            var sourceDistance = footprint.Distance(x, z);
            distance = sourceDistance - TerrainDifferential.Constant(contactOffset);
            if (distance.Height <= 0) return new(distance, x, z, 1, 0, 1);
            var boundary = footprint.NearestBoundary(x, z); tx = boundary.Tx; tz = boundary.Tz;
            curvatureScale = contactOffset / sourceDistance.Height;
        }
        else
        {
            var hit = road.Path.Project(x, z); var width = road.Width / 2 + contactOffset;
            distance = new(hit.Distance - width, hit.DistanceDx, hit.DistanceDz);
            if (distance.Height <= 0) return new(distance, x, z, 1, 0, 1);
            tx = hit.StationDx; tz = hit.StationDz; curvatureScale = width / hit.Distance;
        }
        var sx = x - distance.Height * distance.Dx; var sz = z - distance.Height * distance.Dz;
        if (tx != 0 || tz != 0) return new(distance, sx, sz, tx * tx, tx * tz, tz * tz);
        // At a convex source vertex the offset shoulder is a circular arc. Differentiate its
        // nearest-point projection, not a newly invented centreline end cap.
        return new(distance, sx, sz, curvatureScale * (1 - distance.Dx * distance.Dx),
            -curvatureScale * distance.Dx * distance.Dz, curvatureScale * (1 - distance.Dz * distance.Dz));
    }

    internal static double RoadCorePathRadius(TerrainRoad road, string file = "world")
    {
        if (road.Footprint is not { } core) return road.Width / 2;
        const double cell = 0.25; var radius = Math.Sqrt(2) * cell / 2; var maximum = 0d; var found = false;
        // Every core point lies in a grid cell whose centre is at most half a diagonal away.
        // Distance-to-path is 1-Lipschitz, so centre distance + that radius covers the ENTIRE
        // real core, including interiors/holes/concave corners; vertex-only tests cannot do so.
        var minX = (long)Math.Floor(core.Bounds.Min.X / cell) - 1; var maxX = (long)Math.Ceiling(core.Bounds.Max.X / cell);
        var minZ = (long)Math.Floor(core.Bounds.Min.Y / cell) - 1; var maxZ = (long)Math.Ceiling(core.Bounds.Max.Y / cell);
        for (var iz = minZ; iz <= maxZ; iz++)
            for (var ix = minX; ix <= maxX; ix++)
            {
                var x = (ix + 0.5) * cell; var z = (iz + 0.5) * cell;
                if (core.Distance(x, z).Height > radius) continue;
                maximum = Math.Max(maximum, road.Path.Project(x, z).Distance + radius); found = true;
            }
        if (!found || !double.IsFinite(maximum)) throw UnsupportedFreeboard($"Real road core '{road.Source.Value}' has no finite coverage radius.", file);
        return maximum;
    }

    internal static IReadOnlyList<int> AutomaticFreeboardSupport(TerrainRoad road, TerrainRoadProfile profile,
        (double X, double Z) a, (double X, double Z) c)
    {
        var indices = new SortedSet<int>();
        foreach (var interval in RoadSurfaceSupportIntervals(road, a, c))
            foreach (var index in profile.SupportIndices(interval.From, interval.To)) indices.Add(index);
        return indices.ToArray();
    }

    private static IEnumerable<(double From, double To)> RoadSurfaceSupportIntervals(TerrainRoad road,
        (double X, double Z) a, (double X, double Z) c)
    {
        var mx = (a.X + c.X) / 2; var mz = (a.Z + c.Z) / 2;
        var dx = c.X - a.X; var dz = c.Z - a.Z; var radius = Math.Sqrt(dx * dx + dz * dz) / 2;
        var nearest = road.Path.Project(mx, mz).Distance;
        for (var segment = 0; segment + 1 < road.Path.Line.Points.Count; segment++)
        {
            var middle = road.Path.ProjectStationSegment(segment, mx, mz);
            // Both segment-distance and whole-path-distance are 1-Lipschitz. A segment
            // beyond width + 2r cannot enter the existing positive taper-squared support anywhere.
            if (middle.Distance.Height - nearest > road.Width + 2 * radius + TerrainMath.HeightTolerance) continue;
            var left = road.Path.ProjectStationSegment(segment, a.X, a.Z).Station;
            var right = road.Path.ProjectStationSegment(segment, c.X, c.Z).Station;
            var from = Math.Min(left, right); var to = Math.Max(left, right);
            yield return (from, to);
        }
    }

    internal static IEnumerable<((double X, double Z) A, (double X, double Z) C)> ShoreSupportSegments(TerrainPath path, double from, double to)
    {
        // Split at every actual source vertex and at the existing 0.25 m shore lattice.
        // Each emitted piece is straight, so endpoint projections bound its full station range.
        var stations = new SortedSet<double> { from, to };
        var firstTurn = (long)Math.Floor(from / path.Length); var lastTurn = (long)Math.Floor(to / path.Length);
        for (var turn = firstTurn; turn <= lastTurn; turn++)
        {
            var offset = turn * path.Length;
            foreach (var station in path.VertexStations)
                if (station + offset > from && station + offset < to) stations.Add(station + offset);
            for (var station = 0.25; station < path.Length; station += 0.25)
                if (station + offset > from && station + offset < to) stations.Add(station + offset);
        }
        var values = stations.ToArray();
        for (var i = 1; i < values.Length; i++) yield return (Point(values[i - 1]), Point(values[i]));
        (double X, double Z) Point(double station)
        {
            station %= path.Length; if (station < 0) station += path.Length;
            return path.AtDouble(station);
        }
    }

    internal static WorldDataException UnsupportedFreeboard(string detail, string file = "world")
    {
        var error = new WorldDataException(file, "automatic-profiles", "UNSUPPORTED: " + detail);
        error.Data["TerrainConstraintKind"] = "unsupported"; return error;
    }
}

using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal sealed class TerrainRegion
{
    public PolygonRings[] Polygons { get; }
    public MapRect Bounds { get; }
    private readonly (TerrainRingIndex Outer, TerrainRingIndex[] Holes)[] _indices;

    public TerrainRegion(IEnumerable<PolygonRings> polygons)
    {
        Polygons = polygons.Select(p => new PolygonRings(PlanRings.OpenRing(p.Outer),
            p.Holes.Select(h => PlanRings.OpenRing(h, false)).ToArray())).ToArray();
        if (Polygons.Length == 0) throw new ArgumentException("Terrain region has no area.");
        foreach (var polygon in Polygons) ValidatePolygon(polygon);
        _indices = Polygons.Select(p => (new TerrainRingIndex(p.Outer), p.Holes.Select(h => new TerrainRingIndex(h)).ToArray())).ToArray();
        var points = Polygons.SelectMany(p => p.Outer).ToArray();
        Bounds = new(new(points.Min(p => p.X), points.Min(p => p.Y)), new(points.Max(p => p.X), points.Max(p => p.Y)));
    }

    public bool Contains(double x, double z)
    {
        if (x < Bounds.Min.X || x > Bounds.Max.X || z < Bounds.Min.Y || z > Bounds.Max.Y) return false;
        foreach (var polygon in _indices) if (Contains(polygon, x, z)) return true;
        return false;
    }

    public TerrainDifferential Distance(double x, double z)
    {
        var best = new TerrainDifferential(double.PositiveInfinity, 0, 0);
        foreach (var polygon in _indices)
        {
            var closest = polygon.Outer.Nearest(x, z);
            foreach (var hole in polygon.Holes)
            {
                var candidate = hole.Nearest(x, z); if (candidate.Squared < closest.Squared) closest = candidate;
            }
            var length = Math.Sqrt(closest.Squared); var sign = ContainsGeometry(polygon, x, z) ? -1d : 1d;
            var signedLength = length == 0 ? 0d : length * sign;
            // Canonical outer rings are CCW and holes CW: the right-hand edge normal
            // always points out of the filled region. Use it directly on segment interiors.
            // Re-normalizing a projected point's ~1e-14 residual invents an arbitrary unit
            // direction, which can turn a flat bank tangent into a spurious near-vertical slope.
            var distance = closest.Tx != 0 || closest.Tz != 0
                ? new TerrainDifferential(signedLength, closest.Tz, -closest.Tx)
                : length == 0 ? TerrainDifferential.Constant(0)
                : new TerrainDifferential(signedLength, (x - closest.X) / length * sign, (z - closest.Z) / length * sign);
            if (distance.Height < best.Height) best = distance;
        }
        return best;
    }

    private static bool Contains((TerrainRingIndex Outer, TerrainRingIndex[] Holes) polygon, double x, double z)
    {
        if (!polygon.Outer.Contains(x, z)) return false;
        foreach (var hole in polygon.Holes) if (hole.Contains(x, z)) return false;
        return true;
    }

    private static bool ContainsGeometry((TerrainRingIndex Outer, TerrainRingIndex[] Holes) polygon, double x, double z)
    {
        if (!polygon.Outer.ContainsGeometry(x, z)) return false;
        foreach (var hole in polygon.Holes) if (hole.ContainsGeometry(x, z)) return false;
        return true;
    }

    public (double X, double Z, double Tx, double Tz) NearestBoundary(double x, double z)
    {
        var best = double.PositiveInfinity;
        var result = (X: 0d, Z: 0d, Tx: 0d, Tz: 0d);
        foreach (var polygon in _indices)
        {
            Visit(polygon.Outer);
            foreach (var hole in polygon.Holes) Visit(hole);
        }
        return result;
        void Visit(TerrainRingIndex ring)
        {
            var candidate = ring.Nearest(x, z); if (candidate.Squared >= best) return;
            best = candidate.Squared; result = (candidate.X, candidate.Z, candidate.Tx, candidate.Tz);
        }
    }

    public static MapRect Expand(MapRect bounds, double distance) => new(
        new((float)(bounds.Min.X - distance), (float)(bounds.Min.Y - distance)),
        new((float)(bounds.Max.X + distance), (float)(bounds.Max.Y + distance)));

    public bool InteriorOverlaps(TerrainRegion other)
    {
        if (Bounds.Max.X <= other.Bounds.Min.X || other.Bounds.Max.X <= Bounds.Min.X
            || Bounds.Max.Y <= other.Bounds.Min.Y || other.Bounds.Max.Y <= Bounds.Min.Y) return false;
        foreach (var polygon in Polygons)
            foreach (var point in polygon.Outer)
                if (other.Distance(point.X, point.Y).Height < 0) return true;
        foreach (var polygon in other.Polygons)
            foreach (var point in polygon.Outer)
                if (Distance(point.X, point.Y).Height < 0) return true;
        foreach (var a in Polygons)
            foreach (var c in other.Polygons)
                foreach (var ar in new[] { a.Outer }.Concat(a.Holes))
                    foreach (var cr in new[] { c.Outer }.Concat(c.Holes))
                        for (var i = 0; i < ar.Count; i++)
                            for (var j = 0; j < cr.Count; j++)
                            {
                                var p = ar[i]; var q = ar[(i + 1) % ar.Count]; var r = cr[j]; var s = cr[(j + 1) % cr.Count];

                                var a0 = VectorMath.Cross(p, q, r); var a1 = VectorMath.Cross(p, q, s); var b0 = VectorMath.Cross(r, s, p); var b1 = VectorMath.Cross(r, s, q);
                                if (a0 * a1 < 0 && b0 * b1 < 0) return true;
                            }
        // Coincident boundaries contain no strict vertex or proper edge crossing. An actual interior
        // point resolves that case without a raster scan that could miss a sub-metre overlap.
        foreach (var polygon in Polygons)
        {
            var sample = TerrainGeometry.InteriorPoint(new TerrainRegion([polygon]));
            if (other.Distance(sample.X, sample.Y).Height < 0) return true;
        }
        return false;
    }

    private static void ValidatePolygon(PolygonRings polygon)
    {
        CheckRing(polygon.Outer);
        for (var i = 0; i < polygon.Holes.Count; i++)
        {
            var hole = polygon.Holes[i]; CheckRing(hole);
            if (!TerrainMath.PointInRing(hole[0].X, hole[0].Y, polygon.Outer) || RingsTouch(hole, polygon.Outer))
                throw new ArgumentException("Terrain polygon hole is outside or touches its outer boundary.");
            for (var j = 0; j < i; j++)
                if (RingsTouch(hole, polygon.Holes[j]) || TerrainMath.PointInRing(hole[0].X, hole[0].Y, polygon.Holes[j])
                    || TerrainMath.PointInRing(polygon.Holes[j][0].X, polygon.Holes[j][0].Y, hole))
                    throw new ArgumentException("Terrain polygon holes overlap or contain one another.");
        }
    }

    private static void CheckRing(IReadOnlyList<Vector2> ring)
    {
        for (var i = 0; i < ring.Count; i++)
            for (var j = i + 1; j < ring.Count; j++)
            {
                if (j == i + 1 || i == 0 && j == ring.Count - 1) continue;
                if (SegmentsTouch(ring[i], ring[(i + 1) % ring.Count], ring[j], ring[(j + 1) % ring.Count]))
                    throw new ArgumentException("Terrain polygon has intersecting nonadjacent edges.");
            }
    }

    private static bool RingsTouch(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
    {
        for (var i = 0; i < a.Count; i++)
            for (var j = 0; j < b.Count; j++)
                if (SegmentsTouch(a[i], a[(i + 1) % a.Count], b[j], b[(j + 1) % b.Count])) return true;
        return false;
    }

    private static bool SegmentsTouch(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        static bool On(Vector2 p, Vector2 q, Vector2 r) => r.X >= Math.Min(p.X, q.X) && r.X <= Math.Max(p.X, q.X)
            && r.Y >= Math.Min(p.Y, q.Y) && r.Y <= Math.Max(p.Y, q.Y);
        var a0 = VectorMath.Cross(a, b, c); var a1 = VectorMath.Cross(a, b, d); var b0 = VectorMath.Cross(c, d, a); var b1 = VectorMath.Cross(c, d, b);
        if (a0 == 0 && On(a, b, c) || a1 == 0 && On(a, b, d) || b0 == 0 && On(c, d, a) || b1 == 0 && On(c, d, b)) return true;
        return (a0 > 0) != (a1 > 0) && (b0 > 0) != (b1 > 0);
    }
}

internal readonly record struct TerrainPathHit(double Station, double Distance, double Offset,
    double StationDx, double StationDz, double DistanceDx, double DistanceDz,
    double OffsetDx, double OffsetDz, int Segment);

internal readonly record struct TerrainIntersection(double X, double Z, double Station, double OtherStation);

internal sealed class TerrainPath
{
    public Polyline Line { get; }
    public double Length => _stations[^1];
    public MapRect Bounds { get; }
    private readonly double[] _stations;
    private readonly TerrainRingIndex? _segmentIndex;
    private readonly Func<int, double, double, double>? _squaredDistance;

    public TerrainPath(Polyline line)
    {
        if (line.Points.Count < 2) throw new ArgumentException("Terrain path requires at least two points.");
        Line = new(line.Points.ToArray()); _stations = new double[line.Points.Count];
        for (var i = 0; i < line.Points.Count; i++)
        {
            var p = line.Points[i];
            if (!VectorMath.Finite(p)) throw new ArgumentException("Non-finite terrain path point.");
            if (i == 0) continue;
            var a = line.Points[i - 1]; var dx = (double)p.X - a.X; var dz = (double)p.Y - a.Y;
            var length = Math.Sqrt(dx * dx + dz * dz);
            if (length == 0) throw new ArgumentException("Zero-length terrain path segment.");
            _stations[i] = _stations[i - 1] + length;
        }
        Bounds = new(new(line.Points.Min(p => p.X), line.Points.Min(p => p.Y)), new(line.Points.Max(p => p.X), line.Points.Max(p => p.Y)));
        if (Line.Points.Count > 2)
        {
            _segmentIndex = new(Enumerable.Range(0, Line.Points.Count - 1)
                .Select(i => (Line.Points[i], Line.Points[i + 1])).ToArray());
            _squaredDistance = SquaredDistance;
        }
    }

    public TerrainPathHit Project(double x, double z)
    {
        // A single segment needs no nearest-edge query.
        var i = _segmentIndex is null ? 0 : _segmentIndex.NearestEdge(x, z, _squaredDistance!);
        var (length, tx, tz, raw, along, dx, dz) = ProjectSegment(i, x, z);
        var a = Line.Points[i];
        var distance = Math.Sqrt(dx * dx + dz * dz);
        var offset = -tz * (x - a.X) + tx * (z - a.Y); var offsetDx = -tz; var offsetDz = tx;
        var corner = raw <= 0 && i > 0 ? i : raw >= length && i + 1 < Line.Points.Count - 1 ? i + 1 : -1;
        if (corner >= 0)
        {
            var vertex = Line.Points[corner]; var before = Line.Points[corner - 1]; var after = Line.Points[corner + 1];
            var incomingLength = _stations[corner] - _stations[corner - 1]; var outgoingLength = _stations[corner + 1] - _stations[corner];
            var sumX = ((double)vertex.X - before.X) / incomingLength + ((double)after.X - vertex.X) / outgoingLength;
            var sumZ = ((double)vertex.Y - before.Y) / incomingLength + ((double)after.Y - vertex.Y) / outgoingLength;
            var side = sumX * (z - vertex.Y) - sumZ * (x - vertex.X); var sign = side < 0 ? -1 : side > 0 ? 1 : 0;
            // In an internal vertex's Voronoi wedge the signed radial distance joins both
            // adjacent lateral offsets. Reusing one segment's perpendicular projection
            // would jump when the nearest segment changes. Actual open ends stay planar.
            offset = sign * distance; offsetDx = distance == 0 ? 0 : sign * dx / distance; offsetDz = distance == 0 ? 0 : sign * dz / distance;
        }
        return new(along == length ? _stations[i + 1] : _stations[i] + along, distance, offset,
            raw > 0 && raw < length ? tx : 0, raw > 0 && raw < length ? tz : 0,
            distance == 0 ? 0 : dx / distance, distance == 0 ? 0 : dz / distance, offsetDx, offsetDz, i);
    }

    private double SquaredDistance(int segment, double x, double z)
    {
        var projection = ProjectSegment(segment, x, z);
        return projection.Dx * projection.Dx + projection.Dz * projection.Dz;
    }

    private (double Length, double Tx, double Tz, double Raw, double Along, double Dx, double Dz) ProjectSegment(int i, double x, double z)
    {
        var a = Line.Points[i]; var b = Line.Points[i + 1];
        var length = _stations[i + 1] - _stations[i];
        var tx = ((double)b.X - a.X) / length; var tz = ((double)b.Y - a.Y) / length;
        var raw = (x - a.X) * tx + (z - a.Y) * tz;
        var along = x == b.X && z == b.Y ? length : x == a.X && z == a.Y ? 0 : Math.Max(0, Math.Min(length, raw));
        return (length, tx, tz, raw, along, x - (a.X + tx * along), z - (a.Y + tz * along));
    }

    internal (double Station, double Dx, double Dz, TerrainDifferential Distance) ProjectStationSegment(int index, double x, double z)
    {
        var a = Line.Points[index]; var c = Line.Points[index + 1]; var length = _stations[index + 1] - _stations[index];
        var tx = ((double)c.X - a.X) / length; var tz = ((double)c.Y - a.Y) / length;
        var raw = (x - a.X) * tx + (z - a.Y) * tz; var along = Math.Max(0, Math.Min(length, raw));
        var dx = x - (a.X + along * tx); var dz = z - (a.Y + along * tz); var distance = Math.Sqrt(dx * dx + dz * dz);
        return (_stations[index] + along, raw > 0 && raw < length ? tx : 0, raw > 0 && raw < length ? tz : 0,
            distance == 0 ? TerrainDifferential.Constant(0) : new(distance, dx / distance, dz / distance));
    }

    // Proof/coverage calculations retain double coordinates; the public source geometry
    // remains Vector2 and existing sampling/objective stations are unchanged.
    internal (double X, double Z) AtDouble(double station)
    {
        if (station < 0 || station > Length) throw new ArgumentOutOfRangeException(nameof(station));
        for (var i = 0; i + 1 < _stations.Length; i++)
            if (station <= _stations[i + 1])
            {
                var t = (station - _stations[i]) / (_stations[i + 1] - _stations[i]);
                var a = Line.Points[i]; var c = Line.Points[i + 1];
                return (a.X + ((double)c.X - a.X) * t, a.Y + ((double)c.Y - a.Y) * t);
            }
        return (Line.Points[^1].X, Line.Points[^1].Y);
    }

    internal IReadOnlyList<double> VertexStations => _stations;

    public Vector2 At(double station)
    {
        if (station < 0 || station > Length) throw new ArgumentOutOfRangeException(nameof(station), station, $"Station must be within the actual path [0,{Length:R}].");
        for (var i = 0; i < _stations.Length - 1; i++)
            if (station <= _stations[i + 1])
            {
                var t = (station - _stations[i]) / (_stations[i + 1] - _stations[i]);
                var a = Line.Points[i]; var b = Line.Points[i + 1];
                return new((float)(a.X + ((double)b.X - a.X) * t), (float)(a.Y + ((double)b.Y - a.Y) * t));
            }
        return Line.Points[^1];
    }

    public IReadOnlyList<TerrainIntersection> Intersections(TerrainPath other, double distanceTolerance = 0)
    {
        var intersections = new List<TerrainIntersection>();
        for (var i = 0; i < Line.Points.Count - 1; i++)
            for (var j = 0; j < other.Line.Points.Count - 1; j++)
            {
                var a = Line.Points[i]; var b = Line.Points[i + 1]; var c = other.Line.Points[j]; var d = other.Line.Points[j + 1];
                if (PlanLines.SegmentContact(a, b, c, d, distanceTolerance) is not { } hit) continue;
                var station = _stations[i] + hit.T * (_stations[i + 1] - _stations[i]);
                if (intersections.Any(p => Math.Abs(p.Station - station) < 0.000001)) continue;
                intersections.Add(new(hit.X, hit.Y, station,
                    other._stations[j] + hit.U * (other._stations[j + 1] - other._stations[j])));
            }
        return intersections.OrderBy(p => p.Station).ThenBy(p => p.OtherStation).ToArray();
    }
}

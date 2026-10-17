using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Deterministic planar union with exact final Vector2 node identity; public terrain queries keep their own precision.</summary>
internal static class TerrainUnion
{
    private readonly record struct Point(float X, float Z)
    {
        public static Point From(Vector2 p)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Terrain union node is not representable as a finite Vector2.");
            return new(p.X == 0 ? 0 : p.X, p.Y == 0 ? 0 : p.Y);
        }
        public static Point From(double x, double z) => From(new Vector2((float)x, (float)z));
        public Vector2 Vector => new(X, Z);
    }
    private readonly record struct Cut(double T, Point Node);
    private sealed record Edge(Vector2 A, Vector2 B, int Polygon)
    {
        public List<Cut> Cuts { get; } = [new(0, Point.From(A)), new(1, Point.From(B))];
        public (double X, double Z) At(double t) => (A.X + ((double)B.X - A.X) * t, A.Y + ((double)B.Y - A.Y) * t);
    }

    public static TerrainRegion Build(IEnumerable<TerrainRegion> regions)
    {
        var polygons = regions.SelectMany(region => region.Polygons).Select(CanonicalPolygon)
            .Order(Comparer<PolygonRings>.Create(ComparePolygon)).ToArray();
        if (polygons.Length == 1) return new(polygons);
        var edges = new List<Edge>();
        for (var p = 0; p < polygons.Length; p++)
            foreach (var ring in new[] { polygons[p].Outer }.Concat(polygons[p].Holes))
                for (var i = 0; i < ring.Count; i++) edges.Add(new(ring[i], ring[(i + 1) % ring.Count], p));
        for (var i = 0; i < edges.Count; i++)
            for (var j = i + 1; j < edges.Count; j++)
                if (edges[i].Polygon != edges[j].Polygon) Split(edges[i], edges[j]);
        var exposed = new HashSet<(Point A, Point B)>();
        foreach (var edge in edges)
        {
            var cuts = edge.Cuts.OrderBy(c => c.T).ThenBy(c => c.Node.X).ThenBy(c => c.Node.Z).Distinct().ToArray();
            for (var i = 0; i + 1 < cuts.Length; i++)
            {
                var start = cuts[i]; var end = cuts[i + 1];
                if (start.T == end.T)
                {
                    if (start.Node != end.Node) throw new ArgumentException("Distinct terrain-union nodes have the same edge parameter.");
                    continue;
                }
                if (start.Node == end.Node) continue;
                var middleT = (start.T + end.T) / 2;
                if (middleT <= start.T || middleT >= end.T) throw new ArgumentException("Terrain union edge has no representable interior parameter.");
                // Classification remains on the original edge, not on rounded output nodes.
                var middle = edge.At(middleT); var hidden = false;
                for (var p = 0; p < polygons.Length; p++)
                    if (p != edge.Polygon && StrictContains(polygons[p], middle.X, middle.Z)) { hidden = true; break; }
                if (hidden) continue;
                exposed.Add((start.Node, end.Node));
            }
        }
        var ordered = exposed.Where(edge => !exposed.Contains((edge.B, edge.A)))
            .OrderBy(edge => edge.A.X).ThenBy(edge => edge.A.Z).ThenBy(edge => edge.B.X).ThenBy(edge => edge.B.Z).ToArray();
        var outgoing = ordered.GroupBy(edge => edge.A).ToDictionary(group => group.Key, group => group.Select(edge => edge.B).ToArray());
        var incoming = ordered.GroupBy(edge => edge.B).ToDictionary(group => group.Key, group => group.Count());
        if (outgoing.Values.Any(destinations => destinations.Length != 1) || incoming.Values.Any(count => count != 1)
            || outgoing.Keys.Any(point => !incoming.ContainsKey(point)) || incoming.Keys.Any(point => !outgoing.ContainsKey(point)))
            throw new ArgumentException("Terrain union has a non-manifold point contact; an explicit source boundary is required.");
        var unused = new HashSet<(Point A, Point B)>(ordered); var loops = new List<IReadOnlyList<Vector2>>();
        foreach (var start in ordered)
        {
            if (!unused.Contains(start)) continue;
            var points = new List<Vector2>(); var cursor = start.A;
            do
            {
                if (!outgoing.TryGetValue(cursor, out var next) || !unused.Remove((cursor, next[0])))
                    throw new ArgumentException("Water union contains an open or inconsistent boundary.");
                points.Add(cursor.Vector); cursor = next[0];
            } while (cursor != start.A);
            var area = PlanGeometry.SignedArea(points);
            if (points.Count < 3 || points.Distinct().Count() != points.Count || Math.Abs(area) <= 0.000001)
                throw new ArgumentException("Terrain union boundary degenerates at final Vector2 precision.");
            loops.Add(CanonicalRing(points, area > 0));
        }
        var outers = loops.Where(ring => PlanGeometry.SignedArea(ring) > 0).Order(RingComparer).ToArray();
        var holes = loops.Where(ring => PlanGeometry.SignedArea(ring) < 0).Order(RingComparer).ToArray();
        var assigned = outers.Select(_ => new List<IReadOnlyList<Vector2>>()).ToArray();
        foreach (var hole in holes)
        {
            var owner = Enumerable.Range(0, outers.Length).Where(i => TerrainMath.PointInRing(hole[0].X, hole[0].Y, outers[i]))
                .OrderBy(i => Math.Abs(PlanGeometry.SignedArea(outers[i]))).ThenBy(i => i).FirstOrDefault(-1);
            if (owner < 0) throw new ArgumentException("Water union hole has no outer boundary.");
            assigned[owner].Add(hole);
        }
        return new(outers.Select((outer, i) => new PolygonRings(outer, assigned[i])));
    }

    private static void Split(Edge a, Edge b)
    {
        if (Math.Max(Math.Min(a.A.X, a.B.X), Math.Min(b.A.X, b.B.X)) > Math.Min(Math.Max(a.A.X, a.B.X), Math.Max(b.A.X, b.B.X))
            || Math.Max(Math.Min(a.A.Y, a.B.Y), Math.Min(b.A.Y, b.B.Y)) > Math.Min(Math.Max(a.A.Y, a.B.Y), Math.Max(b.A.Y, b.B.Y))) return;
        var ax = (double)a.B.X - a.A.X; var az = (double)a.B.Y - a.A.Y;
        var bx = (double)b.B.X - b.A.X; var bz = (double)b.B.Y - b.A.Y;
        var dx = (double)b.A.X - a.A.X; var dz = (double)b.A.Y - a.A.Y;
        var determinant = ax * bz - az * bx;
        if (determinant != 0)
        {
            // An endpoint known from either source must not be rebuilt from a floating parameter.
            foreach (var endpoint in new[] { a.A, a.B, b.A, b.B })
                if (On(a, endpoint) && On(b, endpoint))
                { AddProjection(a, endpoint); AddProjection(b, endpoint); return; }
            var t = (dx * bz - dz * bx) / determinant; var u = (dx * az - dz * ax) / determinant;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                var at = a.At(t);
                var node = t == 0 ? Point.From(a.A) : t == 1 ? Point.From(a.B)
                    : u == 0 ? Point.From(b.A) : u == 1 ? Point.From(b.B) : Point.From(at.X, at.Z);
                a.Cuts.Add(new(t, node)); b.Cuts.Add(new(u, node));
            }
            return;
        }
        if (dx * az - dz * ax != 0) return;
        AddProjection(a, b.A); AddProjection(a, b.B); AddProjection(b, a.A); AddProjection(b, a.B);
    }
    private static void AddProjection(Edge edge, Vector2 p)
    {
        var dx = (double)edge.B.X - edge.A.X; var dz = (double)edge.B.Y - edge.A.Y;
        var t = p == edge.A ? 0 : p == edge.B ? 1
            : Math.Abs(dx) >= Math.Abs(dz) ? ((double)p.X - edge.A.X) / dx : ((double)p.Y - edge.A.Y) / dz;
        if (t >= 0 && t <= 1) edge.Cuts.Add(new(t, Point.From(p)));
    }

    private static bool On(Edge edge, Vector2 p) =>
        ((double)edge.B.X - edge.A.X) * ((double)p.Y - edge.A.Y) - ((double)edge.B.Y - edge.A.Y) * ((double)p.X - edge.A.X) == 0
        && p.X >= Math.Min(edge.A.X, edge.B.X) && p.X <= Math.Max(edge.A.X, edge.B.X)
        && p.Y >= Math.Min(edge.A.Y, edge.B.Y) && p.Y <= Math.Max(edge.A.Y, edge.B.Y);

    private static readonly IComparer<IReadOnlyList<Vector2>> RingComparer = Comparer<IReadOnlyList<Vector2>>.Create(CompareRing);
    private static PolygonRings CanonicalPolygon(PolygonRings polygon) => new(CanonicalRing(polygon.Outer, true),
        polygon.Holes.Select(h => CanonicalRing(h, false)).Order(RingComparer).ToArray());
    private static IReadOnlyList<Vector2> CanonicalRing(IReadOnlyList<Vector2> input, bool outer)
    {
        var ring = PlanGeometry.OpenRing(input.Select(p => Point.From(p).Vector), outer);
        var first = 0;
        for (var i = 1; i < ring.Count; i++)
            if (ComparePoint(ring[i], ring[first]) < 0) first = i;
        return Enumerable.Range(0, ring.Count).Select(i => ring[(first + i) % ring.Count]).ToArray();
    }
    private static int ComparePoint(Vector2 a, Vector2 b)
    {
        var x = a.X.CompareTo(b.X); return x != 0 ? x : a.Y.CompareTo(b.Y);
    }
    private static int CompareRing(IReadOnlyList<Vector2>? a, IReadOnlyList<Vector2>? b)
    {
        if (a is null || b is null) throw new ArgumentException("Terrain union ring is missing.");
        for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
        { var point = ComparePoint(a[i], b[i]); if (point != 0) return point; }
        return a.Count.CompareTo(b.Count);
    }
    private static int ComparePolygon(PolygonRings? a, PolygonRings? b)
    {
        if (a is null || b is null) throw new ArgumentException("Terrain union polygon is missing.");
        var outer = CompareRing(a.Outer, b.Outer); if (outer != 0) return outer;
        for (var i = 0; i < Math.Min(a.Holes.Count, b.Holes.Count); i++)
        { var hole = CompareRing(a.Holes[i], b.Holes[i]); if (hole != 0) return hole; }
        return a.Holes.Count.CompareTo(b.Holes.Count);
    }
    private static bool StrictContains(PolygonRings polygon, double x, double z)
    {
        if (RingLocation(polygon.Outer, x, z) != 1) return false;
        foreach (var hole in polygon.Holes) if (RingLocation(hole, x, z) != -1) return false;
        return true;
    }
    private static int RingLocation(IReadOnlyList<Vector2> ring, double x, double z)
    {
        var inside = false;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count];
            var cross = ((double)b.X - a.X) * (z - a.Y) - ((double)b.Y - a.Y) * (x - a.X);
            if (Math.Abs(cross) < 0.00000001 && x >= Math.Min(a.X, b.X) && x <= Math.Max(a.X, b.X)
                && z >= Math.Min(a.Y, b.Y) && z <= Math.Max(a.Y, b.Y)) return 0;
            if ((a.Y > z) != (b.Y > z) && x < a.X + ((double)b.X - a.X) * (z - a.Y) / (b.Y - a.Y)) inside = !inside;
        }
        return inside ? 1 : -1;
    }
}

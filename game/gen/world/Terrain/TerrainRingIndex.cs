using System.Numerics;

namespace Campus.Gen.World;

/// <summary>Immutable exact edge BVH. It indexes source geometry, never sampled terrain heights.</summary>
internal sealed class TerrainRingIndex
{
    internal readonly record struct Hit(double X, double Z, double Squared, double Tx, double Tz, int Edge);
    private sealed record Edge(Vector2 A, Vector2 B, int Id)
    {
        public long Ax { get; } = Mm(A.X);
        public long Az { get; } = Mm(A.Y);
        public long Bx { get; } = Mm(B.X);
        public long Bz { get; } = Mm(B.Y);
    }
    private sealed class Node
    {
        public required double MinX, MaxX, MinZ, MaxZ;
        public required long MinMmZ, MaxMmZ, MaxMmX;
        public Edge[]? Edges;
        public Node? Left, Right;
    }
    private readonly Node _root;

    public TerrainRingIndex(IReadOnlyList<Vector2> ring) => _root = Build(
        ring.Select((point, index) => new Edge(point, ring[(index + 1) % ring.Count], index)).ToArray());

    internal TerrainRingIndex(IReadOnlyList<(Vector2 A, Vector2 B)> segments)
    {
        if (segments.Count == 0) throw new ArgumentException("An edge index needs at least one segment.");
        _root = Build(segments.Select((segment, index) => new Edge(segment.A, segment.B, index)).ToArray());
    }

    public bool Contains(double x, double z)
    {
        var inside = false; var boundary = false; var px = Mm(x); var pz = Mm(z);
        Visit(_root); return boundary || inside;
        void Visit(Node node)
        {
            if (boundary || pz < node.MinMmZ || pz > node.MaxMmZ || px > node.MaxMmX) return;
            if (node.Edges is not { } edges) { Visit(node.Left!); Visit(node.Right!); return; }
            foreach (var edge in edges)
            {
                var ax = edge.Ax; var az = edge.Az; var bx = edge.Bx; var bz = edge.Bz;
                var cross = (Int128)(bx - ax) * (pz - az) - (Int128)(bz - az) * (px - ax);
                if (cross == 0 && px >= Math.Min(ax, bx) && px <= Math.Max(ax, bx) && pz >= Math.Min(az, bz) && pz <= Math.Max(az, bz))
                { boundary = true; return; }
                if ((az > pz) != (bz > pz) && ((cross > 0) == (bz > az))) inside = !inside;
            }
        }
    }

    internal bool ContainsGeometry(double x, double z)
    {
        var inside = false; var boundary = false;
        Visit(_root); return boundary || inside;
        void Visit(Node node)
        {
            if (boundary || z < node.MinZ || z > node.MaxZ || x > node.MaxX) return;
            if (node.Edges is not { } edges) { Visit(node.Left!); Visit(node.Right!); return; }
            foreach (var edge in edges)
            {
                var ax = (double)edge.A.X; var az = (double)edge.A.Y;
                var bx = (double)edge.B.X; var bz = (double)edge.B.Y;
                var cross = (bx - ax) * (z - az) - (bz - az) * (x - ax);
                if (cross == 0 && x >= Math.Min(ax, bx) && x <= Math.Max(ax, bx) && z >= Math.Min(az, bz) && z <= Math.Max(az, bz))
                { boundary = true; return; }
                if ((az > z) != (bz > z) && ((cross > 0) == (bz > az))) inside = !inside;
            }
        }
    }

    public Hit Nearest(double x, double z) => Nearest(x, z, null);

    internal int NearestEdge(double x, double z, Func<int, double, double, double> squaredDistance) =>
        Nearest(x, z, squaredDistance).Edge;

    private Hit Nearest(double x, double z, Func<int, double, double, double>? squaredDistance)
    {
        var best = new Hit(0, 0, double.PositiveInfinity, 0, 0, int.MaxValue);
        Visit(_root); return best;
        void Visit(Node node)
        {
            if (LowerBound(node, x, z) > best.Squared) return;
            if (node.Edges is { } edges)
            {
                foreach (var edge in edges)
                {
                    if (squaredDistance is not null)
                    {
                        var pathSquared = squaredDistance(edge.Id, x, z);
                        if (pathSquared > best.Squared || pathSquared == best.Squared && edge.Id >= best.Edge) continue;
                        best = new(0, 0, pathSquared, 0, 0, edge.Id);
                        continue;
                    }
                    var a = edge.A; var dx = (double)edge.B.X - a.X; var dz = (double)edge.B.Y - a.Y;
                    var squared = dx * dx + dz * dz;
                    var raw = ((x - a.X) * dx + (z - a.Y) * dz) / squared;
                    var t = Math.Max(0, Math.Min(1, raw)); var px = a.X + t * dx; var pz = a.Y + t * dz;
                    var qx = x - px; var qz = z - pz; var candidate = qx * qx + qz * qz;
                    if (candidate > best.Squared || candidate == best.Squared && edge.Id >= best.Edge) continue;
                    var length = Math.Sqrt(squared);
                    best = new(px, pz, candidate, raw > 0 && raw < 1 ? dx / length : 0, raw > 0 && raw < 1 ? dz / length : 0, edge.Id);
                }
                return;
            }
            var left = node.Left!; var right = node.Right!;
            if (LowerBound(left, x, z) <= LowerBound(right, x, z)) { Visit(left); Visit(right); }
            else { Visit(right); Visit(left); }
        }
    }

    private static Node Build(Edge[] edges)
    {
        var node = new Node
        {
            MinX = edges.Min(e => Math.Min(e.A.X, e.B.X)), MaxX = edges.Max(e => Math.Max(e.A.X, e.B.X)),
            MinZ = edges.Min(e => Math.Min(e.A.Y, e.B.Y)), MaxZ = edges.Max(e => Math.Max(e.A.Y, e.B.Y)),
            MinMmZ = edges.Min(e => Math.Min(e.Az, e.Bz)), MaxMmZ = edges.Max(e => Math.Max(e.Az, e.Bz)),
            MaxMmX = edges.Max(e => Math.Max(e.Ax, e.Bx))
        };
        if (edges.Length <= 8) { node.Edges = edges.OrderBy(e => e.Id).ToArray(); return node; }
        var splitX = node.MaxX - node.MinX >= node.MaxZ - node.MinZ;
        var sorted = edges.OrderBy(e => splitX ? (double)e.A.X + e.B.X : (double)e.A.Y + e.B.Y).ThenBy(e => e.Id).ToArray();
        var middle = sorted.Length / 2; node.Left = Build(sorted[..middle]); node.Right = Build(sorted[middle..]); return node;
    }
    private static double LowerBound(Node node, double x, double z)
    {
        var dx = x < node.MinX ? node.MinX - x : x > node.MaxX ? x - node.MaxX : 0;
        var dz = z < node.MinZ ? node.MinZ - z : z > node.MaxZ ? z - node.MaxZ : 0;
        return dx * dx + dz * dz;
    }
    private static long Mm(double value)
    {
        TerrainMath.RequireFinite(value, nameof(value));
        var mm = Math.Round(value * 1000, MidpointRounding.ToEven);
        if (mm < long.MinValue / 4d || mm > long.MaxValue / 4d) throw new ArgumentOutOfRangeException(nameof(value));
        return checked((long)mm);
    }
}

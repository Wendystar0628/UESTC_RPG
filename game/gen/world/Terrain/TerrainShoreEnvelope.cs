using Campus.Contracts;
using Campus.Contracts.Maps;
using System.Numerics;

namespace Campus.Gen.World;

/// <summary>Simultaneous fixed shore-height constraints on land, with exact 1:2 Lipschitz cones.</summary>
internal sealed class TerrainShoreEnvelope
{
    private const double Grade = 0.5;
    private sealed record Edge(double Ax, double Az, double Bx, double Bz, double Ay, double By, int Id);
    private sealed class Node
    {
        public required double MinX, MaxX, MinZ, MaxZ, MinY, MaxY;
        public Edge[]? Edges;
        public Node? Left, Right;
    }
    private readonly TerrainRegion _domain;
    private readonly Func<double, double, double> _shoreHeight;
    private readonly string _file, _id;
    private readonly Func<double, double, string>? _explain;
    private Node _root = null!;

    public TerrainShoreEnvelope(TerrainRegion domain, Func<double, double, double> shoreHeight, string file, string id,
        Func<double, double, string>? explain = null)
    { _domain = domain; _shoreHeight = shoreHeight; _file = file; _id = id; _explain = explain; Refresh(); }

    public void Refresh()
    {
        var edges = new List<Edge>();
        foreach (var polygon in _domain.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                for (var i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var c = ring[(i + 1) % ring.Count];
                    var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    var count = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz) / 0.25));
                    var x0 = (double)a.X; var z0 = (double)a.Y; var y0 = _shoreHeight(x0, z0);
                    for (var j = 1; j <= count; j++)
                    {
                        var x1 = a.X + dx * j / count; var z1 = a.Y + dz * j / count; var y1 = _shoreHeight(x1, z1);
                        Add(x0, z0, x1, z1, y0, y1, 0); x0 = x1; z0 = z1; y0 = y1;
                    }
                }
        _root = Build(edges.ToArray());

        void Add(double ax, double az, double bx, double bz, double ay, double by, int depth)
        {
            var mx = (ax + bx) / 2; var mz = (az + bz) / 2; var my = _shoreHeight(mx, mz);
            var error = Math.Max(Math.Abs(my - (ay + by) / 2), Math.Max(
                Math.Abs(_shoreHeight((3 * ax + bx) / 4, (3 * az + bz) / 4) - (3 * ay + by) / 4),
                Math.Abs(_shoreHeight((ax + 3 * bx) / 4, (az + 3 * bz) / 4) - (ay + 3 * by) / 4)));
            // These are vector boundary constraints, not a sampled height field. Adaptive
            // chords resolve the prescribed curve to 1 mm, below the 1 cm terrain quantum.
            if (error > 0.001)
            {
                if (depth >= 24) throw new WorldDataException(_file, _id,
                    $"Shore height is discontinuous or cannot be resolved by the deterministic boundary envelope: a=({ax:R},{az:R},{ay:R}), b=({bx:R},{bz:R},{by:R}), mid={my:R}, error={error:R}. A: {_explain?.Invoke(ax, az)} B: {_explain?.Invoke(bx, bz)}");
                Add(ax, az, mx, mz, ay, my, depth + 1); Add(mx, mz, bx, bz, my, by, depth + 1); return;
            }
            edges.Add(new(ax, az, bx, bz, ay, by, edges.Count));
        }
    }

    public TerrainDifferential Constrain(TerrainDifferential baseline, double x, double z)
    {
        var lower = new TerrainDifferential(double.NegativeInfinity, 0, 0);
        var upper = new TerrainDifferential(double.PositiveInfinity, 0, 0);
        Visit(_root);
        if (lower.Height > upper.Height + 0.001)
            throw new WorldDataException(_file, _id, $"Fixed exposed shore heights cannot coexist within the 1:2 land-grade limit at ({x:R},{z:R}); lower={lower.Height:R}, upper={upper.Height:R}.");
        var result = baseline.Height > upper.Height ? upper : baseline;
        return result.Height < lower.Height ? lower : result;

        void Visit(Node node)
        {
            var distance = Distance(node, x, z);
            if (node.MaxY - Grade * distance <= lower.Height && node.MinY + Grade * distance >= upper.Height) return;
            if (node.Edges is { } edges)
            {
                foreach (var edge in edges)
                {
                    var a = Bound(edge, x, z, false); var c = Bound(edge, x, z, true);
                    if (a.Height > lower.Height) lower = a;
                    if (c.Height < upper.Height) upper = c;
                }
                return;
            }
            if (Distance(node.Left!, x, z) <= Distance(node.Right!, x, z)) { Visit(node.Left!); Visit(node.Right!); }
            else { Visit(node.Right!); Visit(node.Left!); }
        }
    }

    private static TerrainDifferential Bound(Edge edge, double x, double z, bool upper)
    {
        var dx = edge.Bx - edge.Ax; var dz = edge.Bz - edge.Az; var length = Math.Sqrt(dx * dx + dz * dz);
        var tx = dx / length; var tz = dz / length; var slope = (edge.By - edge.Ay) / length;
        var along = (x - edge.Ax) * tx + (z - edge.Az) * tz;
        var across = Math.Abs(-(x - edge.Ax) * tz + (z - edge.Az) * tx);
        var station = along;
        if (Math.Abs(slope) < Grade)
            station += (upper ? -1 : 1) * slope * across / Math.Sqrt(Grade * Grade - slope * slope);
        else station = upper ? slope > 0 ? 0 : length : slope > 0 ? length : 0;
        station = Math.Max(0, Math.Min(length, station));
        var qx = x - (edge.Ax + tx * station); var qz = z - (edge.Az + tz * station);
        var distance = Math.Sqrt(qx * qx + qz * qz); var sign = upper ? 1 : -1;
        var height = edge.Ay + slope * station + sign * Grade * distance;
        return distance <= 0.0000000001 ? new(height, slope * tx, slope * tz)
            : new(height, sign * Grade * qx / distance, sign * Grade * qz / distance);
    }
    private static Node Build(Edge[] edges)
    {
        var node = new Node { MinX = edges.Min(e => Math.Min(e.Ax, e.Bx)), MaxX = edges.Max(e => Math.Max(e.Ax, e.Bx)),
            MinZ = edges.Min(e => Math.Min(e.Az, e.Bz)), MaxZ = edges.Max(e => Math.Max(e.Az, e.Bz)),
            MinY = edges.Min(e => Math.Min(e.Ay, e.By)), MaxY = edges.Max(e => Math.Max(e.Ay, e.By)) };
        if (edges.Length <= 8) { node.Edges = edges.OrderBy(e => e.Id).ToArray(); return node; }
        var splitX = node.MaxX - node.MinX >= node.MaxZ - node.MinZ;
        var sorted = edges.OrderBy(e => splitX ? e.Ax + e.Bx : e.Az + e.Bz).ThenBy(e => e.Id).ToArray();
        var middle = sorted.Length / 2; node.Left = Build(sorted[..middle]); node.Right = Build(sorted[middle..]); return node;
    }
    private static double Distance(Node node, double x, double z)
    {
        var dx = Math.Max(0, Math.Max(node.MinX - x, x - node.MaxX)); var dz = Math.Max(0, Math.Max(node.MinZ - z, z - node.MaxZ));
        return Math.Sqrt(dx * dx + dz * dz);
    }
}

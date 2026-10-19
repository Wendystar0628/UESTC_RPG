using Campus.Contracts;
using Campus.Contracts.Maps;
using System.Numerics;

namespace Campus.Gen.World;

/// <summary>Simultaneous fixed shore-height constraints on land, with bounded Lipschitz cones.</summary>
internal sealed class TerrainShoreEnvelope
{
    private readonly double _grade;
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
    private Node _root = null!;

    public TerrainShoreEnvelope(TerrainRegion domain, Func<double, double, double> shoreHeight, string file, string id, double grade = 0.5)
    { _domain = domain; _shoreHeight = shoreHeight; _file = file; _id = id; _grade = grade; Refresh(); }

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
                        edges.Add(new(x0, z0, x1, z1, y0, y1, edges.Count)); x0 = x1; z0 = z1; y0 = y1;
                    }
                }
        _root = Build(edges.ToArray());

    }

    public TerrainDifferential Constrain(TerrainDifferential baseline, double x, double z)
    {
        var (lower, upper) = BoundsAt(x, z);
        if (lower.Height > upper.Height + TerrainMath.HeightTolerance)
            throw new WorldDataException(_file, _id, $"Fixed exposed shore heights cannot coexist within the 1:2 land-grade limit at ({x:R},{z:R}); lower={lower.Height:R}, upper={upper.Height:R}.");
        var result = baseline.Height > upper.Height ? upper : baseline;
        return result.Height < lower.Height ? lower : result;
    }

    public TerrainDifferential Restrict(TerrainDifferential baseline, TerrainDifferential proposed, double x, double z)
    {
        if (proposed.Height == baseline.Height) return baseline;
        var distance = Distance(_root, x, z);
        if (proposed.Height > baseline.Height && proposed.Height <= _root.MinY + _grade * distance
            || proposed.Height < baseline.Height && proposed.Height >= _root.MaxY - _grade * distance) return proposed;
        var (lower, upper) = BoundsAt(x, z);
        // A road edge yields to an existing bank even where that bank already uses more
        // than the road's 1:3 budget. Only its new fill/cut is restricted, never the bank.
        if (proposed.Height > baseline.Height)
        {
            var result = proposed.Height > upper.Height ? upper : proposed;
            return result.Height <= baseline.Height ? baseline : result;
        }
        else
        {
            var result = proposed.Height < lower.Height ? lower : proposed;
            return result.Height >= baseline.Height ? baseline : result;
        }
    }

    private (TerrainDifferential Lower, TerrainDifferential Upper) BoundsAt(double x, double z)
    {
        var lower = new TerrainDifferential(double.NegativeInfinity, 0, 0);
        var upper = new TerrainDifferential(double.PositiveInfinity, 0, 0);
        Visit(_root);
        return (lower, upper);

        void Visit(Node node)
        {
            var distance = Distance(node, x, z);
            if (node.MaxY - _grade * distance <= lower.Height && node.MinY + _grade * distance >= upper.Height) return;
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

    private TerrainDifferential Bound(Edge edge, double x, double z, bool upper)
    {
        var dx = edge.Bx - edge.Ax; var dz = edge.Bz - edge.Az; var length = Math.Sqrt(dx * dx + dz * dz);
        var tx = dx / length; var tz = dz / length; var slope = (edge.By - edge.Ay) / length;
        var along = (x - edge.Ax) * tx + (z - edge.Az) * tz;
        var across = Math.Abs(-(x - edge.Ax) * tz + (z - edge.Az) * tx);
        var station = along;
        if (Math.Abs(slope) < _grade)
            station += (upper ? -1 : 1) * slope * across / Math.Sqrt(_grade * _grade - slope * slope);
        else station = upper ? slope > 0 ? 0 : length : slope > 0 ? length : 0;
        station = Math.Max(0, Math.Min(length, station));
        var qx = x - (edge.Ax + tx * station); var qz = z - (edge.Az + tz * station);
        var distance = Math.Sqrt(qx * qx + qz * qz); var sign = upper ? 1 : -1;
        var height = edge.Ay + slope * station + sign * _grade * distance;
        return distance <= 0.0000000001 ? new(height, slope * tx, slope * tz)
            : new(height, sign * _grade * qx / distance, sign * _grade * qz / distance);
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

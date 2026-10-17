using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

public readonly record struct TerrainLineProjection(double X, double Z, double Station, double SignedOffset,
    double Distance, int Segment);

/// <summary>Deterministic geometry kernels shared by future vector-operation compilers.</summary>
public static class TerrainGeometry
{
    /// <summary>Distance is evaluated in double. Exact ties keep the lower source segment index.</summary>
    public static TerrainLineProjection ProjectToLine(double x, double z, Polyline line)
    {
        TerrainMath.RequireFinite(x, nameof(x)); TerrainMath.RequireFinite(z, nameof(z));
        if (line.Points.Count < 2) throw new ArgumentException("A centreline requires at least two points.", nameof(line));
        var station = 0d; var bestSquared = double.PositiveInfinity;
        var answer = default(TerrainLineProjection);
        for (var index = 0; index < line.Points.Count - 1; index++)
        {
            var a = line.Points[index]; var b = line.Points[index + 1];
            Check(a); Check(b);
            var dx = (double)b.X - a.X; var dz = (double)b.Y - a.Y;
            var squaredLength = dx * dx + dz * dz;
            if (squaredLength == 0) throw new ArgumentException("Zero-length centreline segment.", nameof(line));
            var length = Math.Sqrt(squaredLength);
            var t = Math.Max(0, Math.Min(1, ((x - a.X) * dx + (z - a.Y) * dz) / squaredLength));
            var px = a.X + dx * t; var pz = a.Y + dz * t;
            var offsetX = x - px; var offsetZ = z - pz;
            var distanceSquared = offsetX * offsetX + offsetZ * offsetZ;
            if (distanceSquared < bestSquared)
            {
                bestSquared = distanceSquared;
                answer = new(px, pz, station + t * length, (dx * offsetZ - dz * offsetX) / length,
                    Math.Sqrt(distanceSquared), index);
            }
            station += length;
        }
        return answer;
    }

    /// <summary>Negative in the polygon interior, positive outside (including holes), zero on its nearest edge.</summary>
    public static TerrainDifferential PolygonDistance(double x, double z, PolygonRings polygon)
    {
        var bestSquared = double.PositiveInfinity; var closestX = 0d; var closestZ = 0d;
        Visit(polygon.Outer);
        foreach (var hole in polygon.Holes) Visit(hole);
        var distance = Math.Sqrt(bestSquared);
        if (distance == 0) return TerrainDifferential.Constant(0);
        var sign = TerrainMath.Contains(x, z, polygon) ? -1d : 1d;
        return new(sign * distance, sign * (x - closestX) / distance, sign * (z - closestZ) / distance);

        void Visit(IReadOnlyList<Vector2> ring)
        {
            if (ring.Count < 3) throw new ArgumentException("A polygon ring requires three vertices.", nameof(polygon));
            for (var index = 0; index < ring.Count; index++)
            {
                var a = ring[index]; var b = ring[(index + 1) % ring.Count];
                Check(a); Check(b);
                var dx = (double)b.X - a.X; var dz = (double)b.Y - a.Y;
                var lengthSquared = dx * dx + dz * dz;
                if (lengthSquared == 0) throw new ArgumentException("Zero-length polygon edge.", nameof(polygon));
                var t = Math.Max(0, Math.Min(1, ((x - a.X) * dx + (z - a.Y) * dz) / lengthSquared));
                var px = a.X + dx * t; var pz = a.Y + dz * t;
                var qx = x - px; var qz = z - pz;
                var squared = qx * qx + qz * qz;
                if (squared < bestSquared) { bestSquared = squared; closestX = px; closestZ = pz; }
            }
        }
    }

    private static void Check(Vector2 point)
    { if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentException("Plan coordinates must be finite."); }
}

using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Pipeline.Map;

/// <summary>Scalar distance to original vector edges; holes remain outside regardless of winding.</summary>
public static class MapDistance
{
    public const double RangeMetres = 8;

    public static byte Encode(double signedMetres)
    {
        if (!double.IsFinite(signedMetres)) throw new ArgumentOutOfRangeException(nameof(signedMetres));
        if (signedMetres <= -RangeMetres) return 0;
        if (signedMetres >= RangeMetres) return 255;
        return (byte)Math.Clamp((int)Math.Round(128 + signedMetres * 16, MidpointRounding.AwayFromZero), 0, 255);
    }

    public static double Polygon(PolygonRings polygon, double x, double z)
    {
        double squared = RingDistanceSquared(polygon.Outer, x, z);
        bool inside = Contains(polygon.Outer, x, z);
        foreach (var hole in polygon.Holes)
        {
            squared = Math.Min(squared, RingDistanceSquared(hole, x, z));
            if (Contains(hole, x, z)) inside = false;
        }
        double distance = Math.Sqrt(squared);
        return inside ? -distance : distance;
    }

    public static bool Contains(IReadOnlyList<Vector2> ring, double x, double z)
    {
        bool inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[j];
            var b = ring[i];
            if ((a.Y > z) != (b.Y > z) && x < ((double)b.X - a.X) * (z - a.Y) / ((double)b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    public static double SegmentSquared(Vector2 a, Vector2 b, double x, double z)
    {
        double dx = (double)b.X - a.X, dz = (double)b.Y - a.Y;
        double lengthSquared = dx * dx + dz * dz;
        double t = lengthSquared == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (z - a.Y) * dz) / lengthSquared, 0, 1);
        double px = x - (a.X + t * dx), pz = z - (a.Y + t * dz);
        return px * px + pz * pz;
    }

    internal static void ValidateRing(IReadOnlyList<Vector2> ring)
    {
        if (ring.Count < 3) throw new ArgumentException("A map polygon needs at least three vertices.");
        foreach (var p in ring)
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Non-finite map vertex.");
    }

    private static double RingDistanceSquared(IReadOnlyList<Vector2> ring, double x, double z)
    {
        double distance = double.MaxValue;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            distance = Math.Min(distance, SegmentSquared(ring[j], ring[i], x, z));
        return distance;
    }
}

internal enum MapChannel { Water, Green, Road, Building, Plaza, Block, OuterRoad, OuterWater, Edge }

internal sealed class MapShape
{
    internal MapChannel Channel { get; }
    internal MapFeatureClass Class { get; }
    internal double MinX { get; }
    internal double MinZ { get; }
    internal double MaxX { get; }
    internal double MaxZ { get; }
    private readonly PolygonRings? _polygon;
    private readonly Vector2 _a, _b;
    private readonly double _radius;

    internal MapShape(PolygonRings polygon, MapChannel channel, MapFeatureClass featureClass = MapFeatureClass.None)
    {
        MapDistance.ValidateRing(polygon.Outer);
        foreach (var hole in polygon.Holes) MapDistance.ValidateRing(hole);
        _polygon = polygon;
        Channel = channel;
        Class = featureClass;
        MinX = polygon.Outer.Min(p => (double)p.X);
        MaxX = polygon.Outer.Max(p => (double)p.X);
        MinZ = polygon.Outer.Min(p => (double)p.Y);
        MaxZ = polygon.Outer.Max(p => (double)p.Y);
    }

    internal MapShape(Vector2 a, Vector2 b, double radius, MapChannel channel, MapFeatureClass featureClass = MapFeatureClass.None)
    {
        if (!float.IsFinite(a.X) || !float.IsFinite(a.Y) || !float.IsFinite(b.X) || !float.IsFinite(b.Y) || !double.IsFinite(radius) || radius < 0)
            throw new ArgumentException("Invalid map line.");
        _a = a; _b = b; _radius = radius; Channel = channel; Class = featureClass;
        MinX = Math.Min(a.X, b.X) - radius; MaxX = Math.Max(a.X, b.X) + radius;
        MinZ = Math.Min(a.Y, b.Y) - radius; MaxZ = Math.Max(a.Y, b.Y) + radius;
    }

    internal double Distance(double x, double z) => _polygon is not null
        ? MapDistance.Polygon(_polygon, x, z)
        : Math.Sqrt(MapDistance.SegmentSquared(_a, _b, x, z)) - _radius;
}

/// <summary>Insertion order is stable. Dictionary order is never used for sampling.</summary>
internal sealed class MapBuckets
{
    private readonly Dictionary<(int X, int Z), List<MapShape>> _buckets = [];
    private readonly double _size;

    internal MapBuckets(IEnumerable<MapShape> shapes, double size, double margin, MapRect sampleRect)
    {
        _size = size;
        foreach (var shape in shapes)
        {
            if (shape.MaxX + margin < sampleRect.Min.X || shape.MinX - margin > sampleRect.Max.X ||
                shape.MaxZ + margin < sampleRect.Min.Y || shape.MinZ - margin > sampleRect.Max.Y) continue;
            for (int z = Cell(Math.Max(shape.MinZ - margin, sampleRect.Min.Y)); z <= Cell(Math.Min(shape.MaxZ + margin, sampleRect.Max.Y)); z++)
                for (int x = Cell(Math.Max(shape.MinX - margin, sampleRect.Min.X)); x <= Cell(Math.Min(shape.MaxX + margin, sampleRect.Max.X)); x++)
                {
                    if (!_buckets.TryGetValue((x, z), out var bucket)) _buckets[(x, z)] = bucket = [];
                    bucket.Add(shape);
                }
        }
    }

    internal IReadOnlyList<MapShape> At(double x, double z) =>
        _buckets.TryGetValue((Cell(x), Cell(z)), out var bucket) ? bucket : Array.Empty<MapShape>();
    private int Cell(double p) => checked((int)Math.Floor(p / _size));
}

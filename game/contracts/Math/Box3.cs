using System.Numerics;

namespace Campus.Contracts;

public readonly record struct Box3(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;
    public Vector3 Center => (Min + Max) * 0.5f;
    public bool Contains(Vector3 point) =>
        point.X >= Min.X && point.X <= Max.X &&
        point.Y >= Min.Y && point.Y <= Max.Y &&
        point.Z >= Min.Z && point.Z <= Max.Z;
    public bool Intersects(Box3 other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
        Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    public float DistanceSquared(Vector3 point) => Vector3.DistanceSquared(point, Vector3.Clamp(point, Min, Max));

    public float HorizontalDistanceSquared(Vector3 point)
    {
        var nearest = Vector2.Clamp(new(point.X, point.Z), new(Min.X, Min.Z), new(Max.X, Max.Z));
        return Vector2.DistanceSquared(new(point.X, point.Z), nearest);
    }

    /// <summary>Intersects an outward-facing convex volume. Inside is dot(normal, point) + D &lt;= 0; an empty volume has no visible extent.</summary>
    public bool Intersects(ReadOnlySpan<Plane> planes)
    {
        if (planes.IsEmpty) return false;
        foreach (var plane in planes)
        {
            var normal = plane.Normal;
            var nearest = new Vector3(normal.X >= 0 ? Min.X : Max.X,
                normal.Y >= 0 ? Min.Y : Max.Y, normal.Z >= 0 ? Min.Z : Max.Z);
            if (Vector3.Dot(normal, nearest) + plane.D > 0) return false;
        }
        return true;
    }
}

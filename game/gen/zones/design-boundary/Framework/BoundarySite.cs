using Campus.Contracts;

namespace Campus.Gen.Zones.DesignBoundary.Framework;

/// <summary>
/// One piece of the boundary package (a gate with its fence wings, a fence run, the waterside plaza by the north gate). Each
/// sub-ticket appends its sites to <see cref="BoundarySites.All"/>; the module does the cell filing, caching and merging.
/// Sites are pure: they read only the world model and terrain through the builder and never perform IO.
/// </summary>
public abstract class BoundarySite
{
    /// <summary>Stable site key, "30a.north-gate" style; used in test messages.</summary>
    public abstract string Id { get; }

    /// <summary>Geometry and placed parts for builder.Lod, in world space.</summary>
    public abstract void Build(SiteBuilder builder);

    /// <summary>Collision primitives built from the same parameters as the geometry (SYS-WALK §13).</summary>
    public virtual void Collide(SiteColliders colliders) { }

    /// <summary>Bearing in degrees of a plan direction (world x east, z south; 0 north, 90 east).</summary>
    public static float Bearing(System.Numerics.Vector2 direction)
    {
        var degrees = DetMath.Atan2(direction.X, -direction.Y) * (180f / MathF.PI);
        return degrees < 0 ? degrees + 360f : degrees;
    }

    /// <summary>Plan direction of a bearing in degrees (0 north = −z, 90 east = +x).</summary>
    public static System.Numerics.Vector2 Direction(float bearingDeg)
    {
        var a = bearingDeg * (MathF.PI / 180f);
        return new(DetMath.Sin(a), -DetMath.Cos(a));
    }
}

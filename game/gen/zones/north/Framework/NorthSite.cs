using Campus.Gen.Geometry;
using Campus.Contracts;

namespace Campus.Gen.Zones.North.Framework;

/// <summary>
/// One piece of the north zone (a dormitory cluster's gates and fence, an entrance, a canteen). Each sub-ticket adds its
/// own sites to <see cref="NorthSites.All"/>; the module does the cell filing, caching and merging for all of them.
/// Sites are pure: they read only the world model and terrain through the builder and never perform IO.
/// </summary>
public abstract class NorthSite
{
    /// <summary>Stable site key, "20a.xz1-gate" style; used in seeds and test messages.</summary>
    public abstract string Id { get; }

    /// <summary>Features this site builds instead of the generic producers (WP-14 skips claimed Massing).</summary>
    public virtual IReadOnlyList<Claim> Claims => [];

    /// <summary>Geometry and placed parts for builder.Lod, in world space.</summary>
    public abstract void Build(SiteBuilder builder);

    /// <summary>Collision primitives built from the same parameters as the geometry (SYS-WALK).</summary>
    public virtual void Collide(SiteColliders colliders) { }

    /// <summary>Inspection probes this site contributes (MAP-QSH-NORTH Q1–Q8).</summary>
    public virtual IReadOnlyList<InspectionProbe> Probes => [];

    /// <summary>A probe looking along plan direction <paramref name="look"/>; yaw is the bearing (0 north, 90 east).</summary>
    protected static InspectionProbe Probe(string id, string feature, System.Numerics.Vector3 eye, System.Numerics.Vector2 look, float pitch, float distance) =>
        new(id, new(feature), new(eye, Bearing(look), pitch, 60f), distance);

    /// <summary>Bearing in degrees of a plan direction (world x east, z south).</summary>
    public static float Bearing(System.Numerics.Vector2 direction)
    {
        var degrees = MathF.Atan2(direction.X, -direction.Y) * (180f / MathF.PI);
        return degrees < 0 ? degrees + 360f : degrees;
    }

    /// <summary>Plan direction of a bearing in degrees (0 north = −z, 90 east = +x).</summary>
    public static System.Numerics.Vector2 Direction(float bearingDeg)
    {
        var a = bearingDeg * (MathF.PI / 180f);
        return new(MathF.Sin(a), -MathF.Cos(a));
    }
}

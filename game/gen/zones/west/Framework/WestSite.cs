using Campus.Gen.Geometry;
using Campus.Contracts;

namespace Campus.Gen.Zones.West.Framework;

/// <summary>
/// One piece of the west zone (a landmark, a stone, a plaza). Each sub-ticket (19a–19c) adds its own sites to
/// <see cref="WestSites.All"/>; the module does the cell filing, caching and merging for all of them.
/// Sites are pure: they read only the world model and terrain through the builder and never perform IO.
/// </summary>
public abstract class WestSite
{
    /// <summary>Stable site key, "19a.six" style; used in seeds and test messages.</summary>
    public abstract string Id { get; }

    /// <summary>Features this site builds instead of the generic producers (WP-14 skips claimed Massing).</summary>
    public virtual IReadOnlyList<Claim> Claims => [];

    /// <summary>Geometry and placed parts for builder.Lod, in world space.</summary>
    public abstract void Build(SiteBuilder builder);

    /// <summary>Collision primitives built from the same parameters as the geometry (SYS-WALK §13).</summary>
    public virtual void Collide(SiteColliders colliders) { }

    /// <summary>Inspection probes this site contributes (MAP-QSH-WEST Q1–Q8).</summary>
    public virtual IReadOnlyList<InspectionProbe> Probes => [];

    /// <summary>Named window grids of this site's Facade-claimed features (contract-29), sized from the same layout the geometry uses.</summary>
    public virtual IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) => [];

    /// <summary>Positions of those windows, numbered from ctx.WindowNumbering; never builds geometry.</summary>
    public virtual void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows) { }

    /// <summary>A probe looking along plan direction <paramref name="look"/>; yaw is the bearing (0 north, 90 east).</summary>
    protected static InspectionProbe Probe(string id, string feature, System.Numerics.Vector3 eye, System.Numerics.Vector2 look, float pitch, float distance) =>
        new(id, new(feature), new(eye, Bearing(look), pitch, 60f), distance);

    /// <summary>Bearing in degrees of a plan direction (world x east, z south).</summary>
    public static float Bearing(System.Numerics.Vector2 direction)
    {
        var degrees = DetMath.Atan2(direction.X, -direction.Y) * (180f / MathF.PI);
        return degrees < 0 ? degrees + 360f : degrees;
    }
}

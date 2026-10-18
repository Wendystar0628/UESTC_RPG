using Campus.Gen.Geometry;
using Campus.Contracts;

namespace Campus.Gen.Zones.South.Framework;

/// <summary>
/// One piece of the south zone (a gate, a landmark, a park). Each sub-ticket adds its own sites to
/// <see cref="SouthSites.All"/>; the module does the cell filing, caching and merging for all of them.
/// Sites are pure: they read only the world model and terrain through the builder and never perform IO.
/// </summary>
public abstract class SouthSite
{
    /// <summary>Stable site key, "18a.gate" style; used in seeds and test messages.</summary>
    public abstract string Id { get; }

    /// <summary>Features this site builds instead of the generic producers (WP-14 skips claimed Massing).</summary>
    public virtual IReadOnlyList<Claim> Claims => [];

    /// <summary>Geometry and placed parts for builder.Lod, in world space.</summary>
    public abstract void Build(SiteBuilder builder);

    /// <summary>Collision primitives built from the same parameters as the geometry (SYS-WALK).</summary>
    public virtual void Collide(SiteColliders colliders) { }

    /// <summary>Complete sources, sampled before the module files or merges their output into cells.</summary>
    public virtual IEnumerable<ContentSourceTrace> TraceSources(BuildContext ctx, LodLevel lod)
    {
        var feature = Claims.OfType<FeatureClaim>().Select(c => (FeatureId?)c.Feature).SingleOrDefault();
        yield return Trace(ctx, lod, Id, feature, Build, Collide, OccupiedContours(ctx.World, lod));
    }

    /// <summary>Material plans of the components this site actually builds; output projection supplies their edge detail.</summary>
    protected virtual IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod) => [];

    protected static ContentSourceTrace Trace(BuildContext ctx, LodLevel lod, string id, FeatureId? feature,
        Action<SiteBuilder> build, Action<SiteColliders> collide, IReadOnlyList<PolygonRings> contours) =>
        SiteSourceTrace.Build(ctx, lod, id, feature, build, collide, contours);

    /// <summary>Inspection probes this site contributes (MAP-QSH-SOUTH Q1–Q8).</summary>
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
        var degrees = MathF.Atan2(direction.X, -direction.Y) * (180f / MathF.PI);
        return degrees < 0 ? degrees + 360f : degrees;
    }
}

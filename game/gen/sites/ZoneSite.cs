using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.Sites;

/// <summary>
/// One piece of a zone (a landmark, a gate with its fence, a forecourt, a group of courts). Each zone lists its sites in its
/// module's All; the module does the cell filing and merging for all of them. Sites are pure: they read only the
/// world model and terrain through the builder and never perform IO.
/// </summary>
public abstract class ZoneSite
{
    /// <summary>Stable site key, "21a.pinxue-frame" style; used in seeds and test messages.</summary>
    public abstract string Id { get; }

    /// <summary>Conservative plan ranges covering output anchors, geometry and height reads; reads source data only, without building or terrain queries.</summary>
    public abstract IReadOnlyList<MapRect> Ranges(IWorldModel world);

    /// <summary>Features this site builds instead of the generic producers (WP-14 skips claimed Massing).</summary>
    public virtual IReadOnlyList<Claim> Claims => [];

    /// <summary>Geometry and placed parts for builder.Lod, in world space.</summary>
    public abstract void Build(SiteBuilder builder);

    /// <summary>Collision primitives built from the same parameters as the geometry (SYS-WALK).</summary>
    public virtual void Collide(SiteColliders colliders) { }

    /// <summary>Complete production sources, before the module files their output into cells.</summary>
    public virtual IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod)
    {
        var feature = Claims.OfType<FeatureClaim>().Select(c => (FeatureId?)c.Feature).SingleOrDefault();
        yield return SiteSourceTrace.Build(context, lod, Id, feature, Build, Collide, OccupiedContours(context.World, lod));
    }

    /// <summary>Material plans of this site's generated solids; visible output supplies edge details.</summary>
    protected virtual IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod) => [];

    /// <summary>Named window grids of this site's Facade-claimed features (contract-29), sized from the same layout the geometry uses.</summary>
    public virtual IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) => [];

    /// <summary>Positions of those windows, numbered from ctx.WindowNumbering; never builds geometry.</summary>
    public virtual void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows) { }

}

using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Assembles one complete source through its production site builders, before cell filing and merging.</summary>
public static class SiteSourceTrace
{
    public static ContentSourceTrace Build(BuildContext context, LodLevel lod, string id, FeatureId? feature,
        Action<SiteBuilder> build, Action<SiteColliders> collide, IReadOnlyList<PolygonRings> contours)
    {
        var builder = new SiteBuilder(context, lod, lod switch
        {
            LodLevel.L0 => SiteDetail.Full,
            LodLevel.L1 => SiteDetail.Plain,
            LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => SiteDetail.Mass,
            _ => throw new ArgumentOutOfRangeException(nameof(lod)),
        });
        build(builder);
        var items = builder.Items;
        var collision = new SiteColliders(context);
        collide(collision);
        return new(id, feature, new ContentOutput(
            items.Where(i => i.Mesh is not null).Select(i => GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)).ToArray(),
            items.Where(i => i.Part is not null).Select(i => i.Part!).ToArray()),
            collision.Items.Select(i => i.Collider).ToArray(), contours);
    }
}

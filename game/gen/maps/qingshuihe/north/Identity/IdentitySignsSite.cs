using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.North.Identity;

/// <summary>Registered facade names and shop signs, using the shared frozen scene-text atlas.</summary>
public sealed class IdentitySignsSite : ZoneSite
{
    public override string Id => "20c.identity-signs";

    private static IEnumerable<WorldFeature> Records(IWorldModel world) => world.Features.Values
        .Where(f => f.Id.Value.StartsWith(NorthFeatureIds.IdentitySignPrefix, StringComparison.Ordinal))
        .OrderBy(f => f.Id.Value, StringComparer.Ordinal);

    private static Campus.Contracts.Maps.MapRect Range(WorldFeature record) => SiteRanges.Bounds(
        PlanFrame.Along(record.Point(), record.Pair("direction")).Rect(-record.Number("width") * .5f,
            -record.Number("depth") * .5f, record.Number("width") * .5f, record.Number("depth")), PartStyleDefaults.TrimEdgeBevel);

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) => [.. Records(world).Select(Range)];

    public override void Build(SiteBuilder b)
    {
        foreach (var record in Records(b.Context.World))
            if (b.Includes(Range(record))) NorthIdentityGeometry.Sign(b, record);
    }
}

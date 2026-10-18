using Campus.Contracts;
using Campus.Contracts.Fauna;

namespace Campus.Gen.Fauna;

/// <summary>Produces complete latched animal states; runtime owns visibility changes and mounting.</summary>
[ContentModule("fauna", Owner = "WP-25")]
public sealed class FaunaModule : IFaunaContentSource
{
    public IReadOnlyList<Claim> Claims => [];

    // Animals are mounted from their prepared states, with no static streamed tile geometry.
    public ModuleBudget Budget { get; } = new(0, 0, 0);
    public IEnumerable<TileKey> Tiles(BuildContext context) => [];
    public ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod) => ContentOutput.Empty;

    public FaunaContent PrepareFauna(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var layout = FaunaPlanner.Build(context.World, context.WorldSeed, context.Map.Env.ReflectLakes);
        var scene = new FaunaScene(context.Catalog);
        var sites = layout.Sites.Select(site => new FaunaSiteContent(site.Key, site.Kind, site.Bounds,
            Enum.GetValues<FaunaSet>().Select(set => scene.Build(site.Set(set))).ToArray())).ToArray();
        return new(sites, layout.Log);
    }
}

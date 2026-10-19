using Campus.Contracts;
using Campus.Contracts.Fauna;

namespace Campus.Gen.Fauna;

/// <summary>Produces complete latched animal states; runtime owns visibility changes and mounting.</summary>
[ContentModule("fauna", Owner = "WP-25")]
public sealed class FaunaModule : IFaunaContentSource
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<BuildContext, FaunaLayout> _layouts = new();
    public IReadOnlyList<Claim> Claims => [];

    // Animals are mounted from their prepared states, with no static streamed tile geometry.
    public ModuleBudget Budget { get; } = new(0, 0, 0);
    public IEnumerable<TileKey> Tiles(BuildContext context) => [];
    public ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod) => ContentOutput.Empty;

    private FaunaLayout Layout(BuildContext context) => _layouts.GetValue(context,
        ctx => FaunaPlanner.Plan(ctx.World, ctx.WorldSeed, ctx.Map.Env.ReflectLakes));

    public IReadOnlyDictionary<TileKey, Box3> FaunaTiles(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var tiles = new Dictionary<TileKey, Box3>();
        foreach (var site in Layout(context).Sites)
        {
            var tile = TileGrid.At(site.Bounds.Center);
            tiles[tile] = tiles.TryGetValue(tile, out var bounds)
                ? new(System.Numerics.Vector3.Min(bounds.Min, site.Bounds.Min), System.Numerics.Vector3.Max(bounds.Max, site.Bounds.Max))
                : site.Bounds;
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<TileKey, Box3>(tiles);
    }

    public FaunaContent PrepareFauna(BuildContext context, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(context);
        var layout = Layout(context);
        var scene = new FaunaScene(context.Catalog);
        var sites = layout.Sites.Where(site => TileGrid.At(site.Bounds.Center) == tile)
            .Select(site => FaunaPlanner.Resolve(site, context.World))
            .Select(site => new FaunaSiteContent(site.Key, site.Kind, site.Bounds,
                Enum.GetValues<FaunaSet>().Select(set => scene.Build(site.Set(set))).ToArray())).ToArray();
        return new(sites, layout.Log);
    }
}

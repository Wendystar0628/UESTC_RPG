using Campus.Contracts;

namespace Campus.Gen.Water;

/// <summary>
/// Water surfaces of every map (WLD-WATER): lakes, ponds and channels as one opaque surface per cell.
/// Vertex layout: UV = flow velocity (m/s, x and z), UV2 = (shore distance in m, reflection lake number or -1),
/// COLOR flag 8 = never casts. Banks, bridges and culvert structures belong to the zone packages.
/// </summary>
[ContentModule("water", Owner = "WP-24")]
public sealed class WaterModule : IContentModule
{
    private readonly object _gate = new();
    private (IWorldModel World, MapId Map, WaterSurfaces Surfaces)? _cache;

    public IReadOnlyList<Claim> Claims => [];

    // ENG-BUDGET water budget: 50 k main triangles, 45 draw calls, no shadow casting.
    public ModuleBudget Budget { get; } = new(50_000, 50_000, 45);

    public IEnumerable<TileKey> Tiles(BuildContext ctx) => Surfaces(ctx).Tiles();

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod) =>
        Surfaces(ctx).Bundle(tile, lod) is { } bundle ? new([bundle], []) : ContentOutput.Empty;

    private WaterSurfaces Surfaces(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        lock (_gate)
        {
            if (_cache is { } c && ReferenceEquals(c.World, ctx.World) && c.Map == ctx.Map.Id) return c.Surfaces;
            var surfaces = new WaterSurfaces(ctx.World, ctx.Map);
            _cache = (ctx.World, ctx.Map.Id, surfaces);
            return surfaces;
        }
    }
}

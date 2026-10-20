using Campus.Gen.World;
using Campus.Contracts.Bake;
using Campus.Contracts;
using Campus.Contracts.Water;

namespace Campus.Gen.Water;

/// <summary>
/// Water surfaces of every map (WLD-WATER): lakes, ponds and channels as one opaque surface per cell.
/// Vertex layout: UV = flow velocity (m/s, x and z), UV2 = (shore distance in m, reflection lake number or -1),
/// Pools use UV.x for line width; CUSTOM0 = (pool flag, along m, across m, length),
/// CUSTOM1 = (shallow depth, deep depth, shallow fraction, lane count),
/// CUSTOM2 = (first lane centre, lane spacing, end-bar length, end inset).
/// COLOR flag 8 = never casts. Banks, bridges and culvert structures belong to the zone packages.
/// </summary>

[ContentModule("water", Owner = "WP-24")]
public sealed class WaterModule : IWaterContentSource
{
    public BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.ReflectLakes, [], []);
    public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;

    public IReadOnlyList<Claim> Claims => [];

    // ENG-BUDGET water budget: 50 k main triangles, 45 draw calls, no shadow casting.
    public ModuleBudget Budget { get; } = new(50_000, 50_000, 45);

    public IEnumerable<TileKey> Tiles(BuildContext ctx) => Surfaces(ctx).Tiles();

    public IReadOnlyList<WaterReflectionSurface> GetWaterBodies(BuildContext context) =>
        Surfaces(context).Bodies.Select(body => new WaterReflectionSurface(body.Id, body.Rings, body.SurfaceY,
            body.ReflectSlot, body.Min, body.Max, body.Area)).ToArray();

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod) =>
        Surfaces(ctx).Bundle(tile, lod) is { } bundle ? new([bundle], []) : ContentOutput.Empty;

    private static WaterSurfaces Surfaces(BuildContext ctx) =>
        ctx.Memo(typeof(WaterModule), () => new WaterSurfaces(ctx.World, ctx.Map));
}

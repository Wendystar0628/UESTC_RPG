using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// WP-23's content module (contract-32): reads its inputs through <see cref="VegetationInputs"/>, gives the trunk
/// colliders per 64 m cell (WLD-VEGETATION §10) and owns the canopy channels of the ground masks — ground_mask R
/// (ginkgo leaves), G (other leaves and petals) and ground_mask2 B (canopy shelter), painted from one crown projection
/// (§9.5, §11.7). The claim is exclusive: WP-15 skips its tree fallback for these channels and this contributor
/// overwrites them, so the painted values are the projection's own, not a maximum with anything underneath.
/// Build gives no tile geometry yet: the near pool is the vegetation runtime's (≤ 150 m), and the far pool's static
/// instance streams (150–400 m, <see cref="ContentOutput.Instances"/>) come with the L2 stand-ins.
/// </summary>
[ContentModule("vegetation", Owner = "WP-23")]
public sealed class VegetationModule : IContentModule, IGroundMaskContributor
{
    private readonly object _gate = new();
    private Cache? _cache;

    public string Id => "vegetation";

    public GroundMaskChannels Channels => GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter;

    public IReadOnlyList<Claim> Claims => [];

    // No tile geometry yet (see above); the far pool declares its budget with its instance streams.
    public ModuleBudget Budget { get; } = new(0, 0, 0);

    public IEnumerable<TileKey> Tiles(BuildContext ctx) => Layout(ctx).Layout.Trees
        .Select(t => new TileKey((int)MathF.Floor(t.Position.X / TreeColliders.TileSize), (int)MathF.Floor(t.Position.Z / TreeColliders.TileSize)))
        .Distinct().OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ContentOutput.Empty;
    }

    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => TreeColliders.For(Layout(ctx).Layout, tile, ctx.World.Terrain);

    public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2)
    {
        grid.Validate(mask1.Length, mask2.Length);
        // Overwrite our three channels: clear them, then paint the projection (maximum between our own crowns only).
        for (var i = 0; i < mask1.Length; i += 4)
        {
            mask1[i] = 0;
            mask1[i + 1] = 0;
            mask2[i + 2] = 0;
        }
        var (layout, rules) = Layout(context);
        if (rules is null) return;
        CanopyProjection.Paint(layout, rules, context.World, grid.Rect, grid.MetersPerPixel, mask1, mask2);
    }

    /// <summary>One layout per world, seed and input bytes (the frozen snapshots are shared, so the memory compares by reference).</summary>
    private (PlantingLayout Layout, PlantingRules? Rules) Layout(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var species = ctx.GlobalInputs.Require(VegetationInputs.SpeciesKey);
        var planting = ctx.SourceInputs.TryGetValue(VegetationInputs.PlantingKey, out var bytes) ? bytes : ReadOnlyMemory<byte>.Empty;
        lock (_gate)
        {
            if (_cache is { } c && ReferenceEquals(c.World, ctx.World) && c.Seed == ctx.WorldSeed && c.Species.Equals(species) && c.Planting.Equals(planting))
                return (c.Layout, c.Rules);
            var (layout, rules) = VegetationInputs.Layout(ctx);
            _cache = new Cache(ctx.World, ctx.WorldSeed, species, planting, layout, rules);
            return (layout, rules);
        }
    }

    private sealed record Cache(IWorldModel World, uint Seed, ReadOnlyMemory<byte> Species, ReadOnlyMemory<byte> Planting,
        PlantingLayout Layout, PlantingRules? Rules);
}

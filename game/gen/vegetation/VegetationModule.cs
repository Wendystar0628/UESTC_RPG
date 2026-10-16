using System.Globalization;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// WP-23's content module (contract-32): reads its inputs through <see cref="VegetationInputs"/>, gives the trunk
/// colliders per 64 m cell (WLD-VEGETATION §10) and owns the canopy channels of the ground masks — ground_mask R
/// (ginkgo leaves), G (other leaves and petals) and ground_mask2 B (canopy shelter), painted from one crown projection
/// (§9.5, §11.7). The claim is exclusive: WP-15 skips its tree fallback for these channels and this contributor
/// overwrites them, so the painted values are the projection's own, not a maximum with anything underneath.
/// Build gives the trees as static instance streams (<see cref="ContentOutput.Instances"/>), one per mother model (species ×
/// variant) present in the tile, over the shared prototypes of <see cref="VegetationPrototypes"/>: L0 (64 m cells) carries
/// the near pool's trees on the L0 mother model, the source the vegetation runtime buckets into L0+ / L0 / L1 and the
/// visible source of the trunk colliders; L1 and L2 carry the far pool (150–400 m) on its stand-in; L3 and L4 give
/// nothing yet (crown clouds, ENG-PERF §9). Engine instances are made only by the kernel's instance pools.
/// </summary>
[ContentModule("vegetation", Owner = "WP-23")]
public sealed class VegetationModule : IContentModule, IGroundMaskContributor
{
    private readonly object _gate = new();
    private Cache? _cache;

    public string Id => "vegetation";

    public GroundMaskChannels Channels => GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter;

    public IReadOnlyList<Claim> Claims => [];

    /// <summary>Heaviest 64 m cell on the L0 mother models, heaviest 128 m far tile, most streams × surfaces (VegetationBuildTests).</summary>
    public ModuleBudget Budget { get; } = new(BudgetTrianglesL0, BudgetTrianglesL1, BudgetDrawCalls);

    public const int BudgetTrianglesL0 = 340_000;
    public const int BudgetTrianglesL1 = 30_000;
    public const int BudgetDrawCalls = 40;

    public IEnumerable<TileKey> Tiles(BuildContext ctx) => Layout(ctx).Layout.Trees
        .Select(t => new TileKey((int)MathF.Floor(t.Position.X / TreeColliders.TileSize), (int)MathF.Floor(t.Position.Z / TreeColliders.TileSize)))
        .Distinct().OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (PoolOf(lod) is not { } pool) return ContentOutput.Empty;
        var trees = InTile(Layout(ctx).Layout, tile, lod);
        if (trees.Count == 0) return ContentOutput.Empty;
        var palette = RequirePalette(ctx);
        var streams = new List<GeometryInstanceStream>();
        foreach (var group in ByMother(trees))
        {
            var first = group[0];
            var prototype = VegetationPrototypes.Get(first.Species, first.Variant, pool, palette);
            streams.Add(Stream(StreamId(tile, lod, first), prototype, group));
        }
        return ContentOutput.Empty with { Instances = streams.AsReadOnly() };
    }

    /// <summary>
    /// Read-only source trace for the static audits (G15): one entry per tree of the tile, its own one-record stream over the
    /// same prototype as <see cref="Build"/> and, at L0, its own trunk collider. Flattened, the records are exactly Build's and
    /// the colliders exactly Collide's. SourceId is object/vegetation/{tree key}.
    /// </summary>
    public IReadOnlyList<VegetationSourceTrace> TraceSources(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (PoolOf(lod) is not { } pool) return [];
        var trees = InTile(Layout(ctx).Layout, tile, lod);
        if (trees.Count == 0) return [];
        var palette = RequirePalette(ctx);
        var traces = new List<VegetationSourceTrace>(trees.Count);
        foreach (var tree in trees)
        {
            var id = "object/vegetation/" + tree.Key;
            var prototype = VegetationPrototypes.Get(tree.Species, tree.Variant, pool, palette);
            var output = ContentOutput.Empty with { Instances = [Stream(id, prototype, [tree])] };
            var colliders = lod == LodLevel.L0 ? TreeColliders.For(new PlantingLayout([tree]), tile, ctx.World.Terrain) : [];
            traces.Add(new VegetationSourceTrace(id, tree.Key, output, colliders));
        }
        return traces;
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

    /// <summary>L0 cells give the near pool's source, L1 and L2 tiles the far pool; L3/L4 wait for the crown clouds.</summary>
    private static VegPool? PoolOf(LodLevel lod) => lod switch
    {
        LodLevel.L0 => VegPool.L0,
        LodLevel.L1 or LodLevel.L2 => VegPool.Far,
        LodLevel.L3 or LodLevel.L4 => null,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level.")
    };

    /// <summary>The trees whose trunk foot lies in the tile of this level (floor(xz / (64 &lt;&lt; lod))), in layout order.</summary>
    private static List<PlantedTree> InTile(PlantingLayout layout, TileKey tile, LodLevel lod)
    {
        var size = TreeColliders.TileSize * (1 << (int)lod);
        return layout.Trees.Where(t => (int)MathF.Floor(t.Position.X / size) == tile.X && (int)MathF.Floor(t.Position.Z / size) == tile.Z).ToList();
    }

    /// <summary>Trees grouped by mother model, groups in ordinal species then variant order, trees in layout order.</summary>
    private static IEnumerable<List<PlantedTree>> ByMother(List<PlantedTree> trees) => trees
        .GroupBy(t => (t.Species.Id, t.Variant))
        .OrderBy(g => g.Key.Id, StringComparer.Ordinal).ThenBy(g => g.Key.Variant)
        .Select(g => g.ToList());

    private static GeometryInstanceStream Stream(string id, GeometryPrototype prototype, IReadOnlyList<PlantedTree> trees)
    {
        var data = new float[trees.Count * InstanceStreamLayout.FloatsPerInstance];
        Box3? bounds = null;
        for (var i = 0; i < trees.Count; i++)
        {
            var at = VegetationPrototypes.Transform(trees[i]);
            InstanceStreamLayout.Write(data.AsSpan(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance), at, trees[i].Custom);
            var box = VegetationPrototypes.WorldBounds(prototype.Bounds, at);
            bounds = bounds is { } b ? VegetationPrototypes.Union(b, box) : box;
        }
        return new GeometryInstanceStream(id, prototype, data, bounds!.Value);
    }

    private static string StreamId(TileKey tile, LodLevel lod, PlantedTree first) => string.Join('/', "vegetation",
        lod.ToString(), tile.X.ToString(CultureInfo.InvariantCulture) + "_" + tile.Z.ToString(CultureInfo.InvariantCulture),
        first.Species.Id, first.Variant.ToString(CultureInfo.InvariantCulture));

    private static PaletteRegistry RequirePalette(BuildContext ctx) => ctx.Palette
        ?? throw new InvalidOperationException("Vegetation instance streams need BuildContext.Palette: the mother models carry palette colour IDs.");
}

/// <summary>One tree's share of a vegetation tile (<see cref="VegetationModule.TraceSources"/>).</summary>
public sealed record VegetationSourceTrace(string SourceId, string TreeKey, ContentOutput Output, IReadOnlyList<Collider> Colliders);

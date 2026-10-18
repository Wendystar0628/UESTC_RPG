using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Vegetation;

namespace Campus.Gen.Vegetation;

/// <summary>
/// WP-23's content module (contract-32): reads its inputs through <see cref="VegetationInputs"/>, gives the trunk
/// colliders per 64 m cell (WLD-VEGETATION) and owns the canopy channels of the ground masks — ground_mask R
/// (ginkgo leaves), G (other leaves and petals) and ground_mask2 B (canopy shelter), painted from one crown projection
/// (§9.5, §11.7). The claim is exclusive: WP-15 skips its tree fallback for these channels and this contributor
/// overwrites them, so the painted values are the projection's own, not a maximum with anything underneath.
/// Build gives the trees as static instance streams (<see cref="ContentOutput.Instances"/>), one per mother model (species ×
/// variant) present in the tile, over the shared prototypes of <see cref="VegetationPrototypes"/>: L0 (64 m cells) carries
/// the near pool's trees on the L0 mother model, the source the vegetation runtime buckets into L0+ / L0 / L1 and the
/// visible source of the trunk colliders; L1 and L2 carry the far pool (150–400 m) on its stand-in; L3 and L4 give
/// nothing yet (crown clouds, ENG-PERF). Engine instances are made only by the kernel's instance pools.
/// </summary>
[ContentModule("vegetation", Owner = "WP-23")]
public sealed class VegetationModule : IVegetationRuntimeSource, IGroundMaskContributor, IContentSourceTraceProvider
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
        .Select(t => TileGrid.At(t.Position))
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

    /// <summary>Plan-only grass input from the same prepared context used by trees; no grass geometry is implied.</summary>
    public GrassRuntimeData PrepareGrass(BuildContext context) => GrassInputs.Read(context);

    /// <summary>The species table packed for the tree_species texture (<see cref="SpeciesTable.Pack"/>).</summary>
    public float[] PrepareSpeciesTable(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return VegetationInputs.Species(context).Pack(RequirePalette(context));
    }

    /// <summary>
    /// All runtime pools prepared from the same L0 streams, layout cache and shared production prototypes; then the grass pools
    /// (<see cref="GrassClumps.Mothers"/>), which carry one prototype each and are told apart by their key prefix.
    /// </summary>
    public IReadOnlyList<VegetationMotherData> PrepareVegetation(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var species = VegetationInputs.Species(context);
        var groups = new SortedDictionary<string, (SpeciesInfo Info, int Variant, List<float> Records)>(StringComparer.Ordinal);
        foreach (var tile in Tiles(context))
            foreach (var stream in Build(context, tile, LodLevel.L0).Instances)
            {
                var (id, variant, _) = VegetationPrototypes.Parse(stream.Prototype.Id);
                var key = id + "." + variant.ToString(CultureInfo.InvariantCulture);
                if (!groups.TryGetValue(key, out var group)) groups.Add(key, group = (species.Get(id), variant, []));
                group.Records.AddRange(stream.Data.ToArray());
            }
        var mothers = groups.ToArray();
        var built = new VegetationMotherData[mothers.Length];
        for (var i = 0; i < mothers.Length; i++)
        {
            var (key, (info, variant, records)) = (mothers[i].Key, mothers[i].Value);
            var prototypes = Enum.GetValues<VegPool>()
                .Select(pool => VegetationPrototypes.Get(info, variant, pool, context.Catalog)).ToArray();
            var data = records.ToArray();
            var bounds = new Box3[data.Length / InstanceStreamLayout.FloatsPerInstance];
            for (var tree = 0; tree < bounds.Length; tree++)
                bounds[tree] = VegetationPrototypes.WorldBounds(prototypes[(int)VegPool.L0Plus].Bounds,
                    InstanceMatrix(data.AsSpan(tree * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance)));
            built[i] = new(key, new WpId(VegetationPrototypes.Owner), data, prototypes, bounds);
        }
        // Near card grass of the authored grass areas, after the trees (keys GrassClumps.KeyPrefix + style, one prototype each).
        var grass = GrassField.Build(PrepareGrass(context), context.World, Layout(context).Layout, context.WorldSeed);
        return [.. built, .. GrassClumps.Mothers(grass)];
    }

    private static System.Numerics.Matrix4x4 InstanceMatrix(ReadOnlySpan<float> r) =>
        new(r[0], r[4], r[8], 0f, r[1], r[5], r[9], 0f, r[2], r[6], r[10], 0f, r[3], r[7], r[11], 1f);

    /// <summary>Complete trees over the same production prototypes and trunk collision parameters as Build and Collide.</summary>
    public IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel level)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (PoolOf(level) is not { } pool) yield break;
        foreach (var tree in Layout(context).Layout.Trees)
        {
            var id = "object/vegetation/" + tree.Key;
            var prototype = VegetationPrototypes.Get(tree.Species, tree.Variant, pool, RequirePalette(context));
            var output = ContentOutput.Empty with { Instances = [Stream(id, prototype, [tree])] };
            var tile = TileGrid.At(tree.Position);
            var colliders = level == LodLevel.L0 ? TreeColliders.For(new PlantingLayout([tree]), tile, context.World.Terrain) : [];
            FeatureId? feature = context.World.Features.ContainsKey(new(tree.Key)) ? new FeatureId(tree.Key) : null;
            yield return new ContentSourceTrace(id, feature, output, colliders, []);
        }
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
        return layout.Trees.Where(t => TileGrid.At(t.Position, lod) == tile).ToList();
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

    private static PaletteCatalog RequirePalette(BuildContext ctx) => ctx.Catalog;
}

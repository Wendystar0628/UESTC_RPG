using Campus.Gen.World;
using Campus.Contracts.Bake;
using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Vegetation;

namespace Campus.Gen.Vegetation;

/// <summary>
/// WP-23's content module (contract-32): reads its inputs through <see cref="VegetationInputs"/>, gives the trunk
/// colliders per 64 m cell (WLD-VEGETATION) and owns the canopy channels of the ground masks — ground_mask R
/// (ginkgo leaves), G (other leaves and petals), A (the maple subset of G) and ground_mask2 B (canopy shelter), painted from one crown projection.
/// The claim is exclusive: this contributor clears and paints these channels, so the values are the projection's own.
/// Build gives far stand-ins as static instance streams (<see cref="ContentOutput.Instances"/>), one per mother model (species ×
/// variant) at every static geometry level. Near trees and their shadows belong exclusively to the streamed runtime.
/// </summary>

[ContentModule("vegetation")]
public sealed class VegetationModule : IVegetationRuntimeSource, IGroundMaskContributor, IContentSourceTraceProvider
{
    public BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.Vegetation, [], [VegetationInputs.SpeciesKey]);
    public BakeInputDependencies CollisionInputs => BakeInputs;

    public string Id => "vegetation";

    public GroundMaskChannels Channels => GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter | GroundMaskChannels.MapleLeaves;

    public IReadOnlyList<Claim> Claims => [];

    /// <summary>Heaviest 64 m cell on the L0 mother models, heaviest 128 m far tile, most streams × surfaces (VegetationBuildTests).</summary>

    public IEnumerable<TileKey> Tiles(BuildContext ctx) => VegetationTiles.Read(ctx);

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var pool = PoolOf(lod);
        var trees = InTile(Layout(ctx).Layout, tile, lod);
        if (trees.Count == 0) return ContentOutput.Empty;
        var streams = new List<GeometryInstanceStream>();
        foreach (var group in ByMother(trees))
        {
            var first = group[0];
            var prototype = VegetationPrototypes.Get(first.Species, first.Variant, pool, ctx);
            streams.Add(Stream(StreamId(tile, lod, first), prototype, group));
        }
        return ContentOutput.Empty with { Instances = streams.AsReadOnly() };
    }

    public IReadOnlyList<TileKey> GrassTiles(BuildContext context) => GrassField.Tiles(GrassInputs.Read(context));

    public IReadOnlyList<VegetationMotherData> PrepareGrass(BuildContext context, TileKey tile) =>
        GrassClumps.Mothers(GrassField.Build(GrassInputs.Read(context), context.World, Layout(context).Layout, context.WorldSeed, tile));

    /// <summary>The packed small, resident species table.</summary>
    public float[] PrepareSpeciesTable(BuildContext context) =>
        VegetationInputs.Species(context).Pack(RequirePalette(context));

    /// <summary>One owner tile, using the same planting and prototype algorithm as static far geometry.</summary>
    public IReadOnlyList<VegetationMotherData> PrepareTrees(BuildContext context, TileKey tile)
    {
        var built = new List<VegetationMotherData>();
        foreach (var group in ByMother(InTile(Layout(context).Layout, tile, LodLevel.L0)))
        {
            var first = group[0];
            VegPool[] order = [VegPool.L0Plus, VegPool.L0, VegPool.L1, VegPool.Shadow];
            var prototypes = order.Select(pool => VegetationPrototypes.Get(first.Species, first.Variant, pool, context)).ToArray();
            var stream = Stream(StreamId(tile, LodLevel.L0, first), prototypes[0], group);
            var records = stream.Data.ToArray();
            var bounds = new Box3[group.Count];
            for (var i = 0; i < bounds.Length; i++)
                bounds[i] = VegetationPrototypes.WorldBounds(prototypes[0].Bounds,
                    InstanceMatrix(records.AsSpan(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance)));
            built.Add(new(first.Species.Id + "." + first.Variant.ToString(CultureInfo.InvariantCulture),
                records, prototypes, bounds));
        }
        return built.AsReadOnly();
    }

    private static System.Numerics.Matrix4x4 InstanceMatrix(ReadOnlySpan<float> r) =>
        new(r[0], r[4], r[8], 0f, r[1], r[5], r[9], 0f, r[2], r[6], r[10], 0f, r[3], r[7], r[11], 1f);

    /// <summary>Complete trees over the same production prototypes and trunk collision parameters as Build and Collide.</summary>
    public IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel level)
    {
        ArgumentNullException.ThrowIfNull(context);
        var pool = PoolOf(level);
        foreach (var tree in Layout(context).Layout.Trees)
        {
            var id = "object/vegetation/" + tree.Key;
            var prototype = VegetationPrototypes.Get(tree.Species, tree.Variant, pool, context);
            var output = ContentOutput.Empty with { Instances = [Stream(id, prototype, [tree])] };
            var tile = TileGrid.At(tree.Position);
            var colliders = level == LodLevel.L0 ? TreeColliders.For(new PlantingLayout([tree]), tile, context.World.Terrain) : [];
            var source = tree.Source == PlantSource.SportWall ? Planting.SportWallFeature(tree.Key) : tree.Key;
            FeatureId? feature = source is not null && context.World.Features.ContainsKey(new(source)) ? new FeatureId(source) : null;
            yield return new ContentSourceTrace(id, feature, output, colliders, []);
        }
    }

    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => TreeColliders.For(Layout(ctx).Layout, tile, ctx.World.Terrain);

    public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2)
    {
        grid.Validate(mask1.Length, mask2.Length);
        // Clear the claimed channels together, then paint the projection (maximum between our own crowns only).
        for (var i = 0; i < mask1.Length; i += 4)
        {
            mask1[i] = 0;
            mask1[i + 1] = 0;
            mask1[i + 3] = 0;
            mask2[i + 2] = 0;
        }
        var (layout, rules) = Layout(context);
        if (rules is null) return;
        CanopyProjection.Paint(layout, rules, VegetationInputs.Species(context), context.World, grid.Rect, grid.MetersPerPixel, mask1, mask2);
    }

    /// <summary>Immutable planting derived once for this build context.</summary>
    private static (PlantingLayout Layout, PlantingRules? Rules) Layout(BuildContext ctx) =>
        ctx.Memo(typeof(VegetationModule), () => VegetationInputs.Layout(ctx));

    /// <summary>Every selected geometry level covers the far segment, including leaf cells and coarse ancestors.</summary>
    private static VegPool PoolOf(LodLevel lod) => lod switch
    {
        LodLevel.L0 or LodLevel.L1 or LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => VegPool.Far,
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

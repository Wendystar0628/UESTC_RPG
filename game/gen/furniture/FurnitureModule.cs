using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>
/// WP-13 street furniture on the map (13b placement, <see cref="FurniturePlanner"/>). Each 64 m cell gets the catalogue parts
/// whose anchors fall in it, as <see cref="PlacedPart"/>s (the kernel instances them by part ID); pads (rest-stop bays, kiosk
/// aprons, plinths) come as merged meshes, lamp markers as <see cref="ContentOutput.Emitters"/> (L0 only), with the resident
/// inventory reading <see cref="DescribeLights"/>, and colliders through <see cref="Collide"/>. Claims nothing: furniture stands between the features other packages own.
/// Pure .NET; the layout is built once per world model and claims query and shared by every tile and level.
/// </summary>
[ContentModule("furniture", Owner = "WP-13")]
public sealed class FurnitureModule : IContentModule, ILightMetadataSource
{
    private static readonly ConditionalWeakTable<IWorldModel, List<(Func<FeatureId, Aspects, WpId?> Claims, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs, FurnitureLayout Layout)>> Cache = new();
    private static readonly object Sync = new();

    public IReadOnlyList<Claim> Claims { get; } = [];

    /// <summary>The heaviest L0 cell (an avenue cell with lamps on both sides and a rest stop) stays under these bounds.</summary>
    public ModuleBudget Budget { get; } = new(S.BudgetTrianglesL0, S.BudgetTrianglesL1, S.BudgetDrawCalls);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return Layout(ctx).Items.Select(i => TileGrid.At(i.Anchor)).Distinct().OrderBy(c => c.X).ThenBy(c => c.Z)
            .ToArray();
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (lod == LodLevel.L4) return ContentOutput.Empty;
        var parts = new List<PlacedPart>();
        var emitters = new List<LightEmitter>();
        PartBuilder? pads = null;
        foreach (var item in Layout(ctx).Items)
        {
            if (lod > Deepest(item.Kind) || TileGrid.At(item.Anchor, lod) != tile) continue;
            parts.Add(item.Part);
            if (lod == LodLevel.L0) emitters.AddRange(item.Emitters);
            if (item.Pad is { } pad && lod <= LodLevel.L1) (pads ??= new PartBuilder()).Add(pad.Geometry());
        }
        if (parts.Count == 0) return ContentOutput.Empty;
        var meshes = pads?.Finish(new(Vector3.Zero, Vector3.Zero)).Meshes ?? [];
        return new ContentOutput(meshes, parts, emitters.Count > 0 ? emitters : null) { Diagnostics = lod == LodLevel.L0 ? DiagnosticsFor(ctx, tile) : [] };
    }

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context) =>
        Layout(context).Items.SelectMany(item => item.Emitters).ToArray();

    /// <summary>The layout's diagnostics, reported once: with the L0 output of the first cell of <see cref="Tiles"/>.</summary>
    private static IReadOnlyList<BuildDiagnostic> DiagnosticsFor(BuildContext ctx, TileKey tile)
    {
        var layout = Layout(ctx);
        if (layout.Diagnostics.Count == 0) return [];
        var first = layout.Items.Select(i => TileGrid.At(i.Anchor)).OrderBy(c => c.X).ThenBy(c => c.Z).First();
        return first == tile ? layout.Diagnostics : [];
    }

    /// <summary>Colliders live in fixed 64 m cells, filed by their item's anchor.</summary>
    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return Layout(ctx).Items.Where(i => TileGrid.At(i.Anchor) == tile).SelectMany(i => i.Colliders).ToArray();
    }

    /// <summary>The map's layout for this context's world and claims, built once and shared.</summary>
    public static FurnitureLayout Layout(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        lock (Sync)
        {
            var built = Cache.GetValue(ctx.World, _ => []);
            foreach (var (claims, inputs, layout) in built)
                if (ReferenceEquals(claims, ctx.ClaimedBy) && ReferenceEquals(inputs, ctx.SourceInputs)) return layout;
            var fresh = FurniturePlanner.Build(ctx.World, ctx.ClaimedBy, ctx.SourceInputs);
            built.Add((ctx.ClaimedBy, ctx.SourceInputs, fresh));
            return fresh;
        }
    }

    /// <summary>The coarsest level a kind still shows at: the parts that carry an L2 (lamps, kiosks, stops, cars) stay to L2.</summary>
    private static LodLevel Deepest(FurnitureKind kind) => kind switch
    {
        FurnitureKind.StreetLamp or FurnitureKind.AtlasKiosk or FurnitureKind.BusStop or FurnitureKind.ParkedCar or FurnitureKind.Signpost => LodLevel.L2,
        FurnitureKind.Bench or FurnitureKind.Bin or FurnitureKind.AtlasPost or FurnitureKind.EntranceMat
            or FurnitureKind.OpenSign or FurnitureKind.EntranceLamp or FurnitureKind.KerbRamp or FurnitureKind.DrinkingFountain
            or FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker or FurnitureKind.BicycleBay or FurnitureKind.ParkedBicycle or FurnitureKind.Barrier => LodLevel.L1,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown furniture kind."),
    };
}

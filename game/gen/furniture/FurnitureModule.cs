using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>
/// WP-13 street furniture on the map (13b placement, <see cref="FurniturePlanner"/>). Each 64 m cell gets the catalogue parts
/// whose anchors fall in it, as <see cref="PlacedPart"/>s (the kernel instances them by part ID); pads (rest-stop bays, kiosk
/// aprons, plinths) come as merged meshes, lamp markers as <see cref="ContentOutput.Emitters"/> (L0 only, so the lamp inventory
/// sees each lamp once) and colliders through <see cref="Collide"/>. Claims nothing: furniture stands between the features other packages own.
/// Pure .NET; the layout is built once per world model and claims query and shared by every tile and level.
/// </summary>
[ContentModule("furniture", Owner = "WP-13")]
public sealed class FurnitureModule : IContentModule
{
    private static readonly ConditionalWeakTable<IWorldModel, List<(Func<FeatureId, Aspects, WpId?> Claims, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs, FurnitureLayout Layout)>> Cache = new();
    private static readonly object Sync = new();

    public IReadOnlyList<Claim> Claims { get; } = [];

    /// <summary>The heaviest L0 cell (an avenue cell with lamps on both sides and a rest stop) stays under these bounds.</summary>
    public ModuleBudget Budget { get; } = new(S.BudgetTrianglesL0, S.BudgetTrianglesL1, S.BudgetDrawCalls);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return Layout(ctx).Items.Select(i => Cell(i.Anchor, LodLevel.L0)).Distinct().OrderBy(c => c.X).ThenBy(c => c.Z)
            .Select(c => new TileKey(c.X, c.Z)).ToArray();
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
            if (lod > Deepest(item.Kind) || Cell(item.Anchor, lod) != (tile.X, tile.Z)) continue;
            parts.Add(item.Part);
            if (lod == LodLevel.L0) emitters.AddRange(item.Emitters);
            if (item.Pad is { } pad && lod <= LodLevel.L1) (pads ??= new PartBuilder()).Add(pad.Geometry());
        }
        if (parts.Count == 0) return ContentOutput.Empty;
        var meshes = pads?.Finish(new(Vector3.Zero, Vector3.Zero)).Meshes ?? [];
        return new ContentOutput(meshes, parts, emitters.Count > 0 ? emitters : null) { Diagnostics = lod == LodLevel.L0 ? DiagnosticsFor(ctx, tile) : [] };
    }

    /// <summary>The layout's diagnostics, reported once: with the L0 output of the first cell of <see cref="Tiles"/>.</summary>
    private static IReadOnlyList<BuildDiagnostic> DiagnosticsFor(BuildContext ctx, TileKey tile)
    {
        var layout = Layout(ctx);
        if (layout.Diagnostics.Count == 0) return [];
        var first = layout.Items.Select(i => Cell(i.Anchor, LodLevel.L0)).Min();
        return first == (tile.X, tile.Z) ? layout.Diagnostics : [];
    }

    /// <summary>Colliders live in fixed 64 m cells, filed by their item's anchor.</summary>
    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return Layout(ctx).Items.Where(i => Cell(i.Anchor, LodLevel.L0) == (tile.X, tile.Z)).SelectMany(i => i.Colliders).ToArray();
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
            // Anchors left out or left in place, once per layout, for the anchor data's review (WP-48c); the structured diagnostics
            // travel with ContentOutput.Diagnostics, and stay visible here until the collector consumes them.
            foreach (var line in fresh.Log) Console.Error.WriteLine("[WP-13 furniture] " + line);
            foreach (var diagnostic in fresh.Diagnostics) Console.Error.WriteLine($"[WP-13 furniture] {diagnostic.Level.ToString().ToLowerInvariant()} {diagnostic.Code}: {diagnostic.Message}");
            return fresh;
        }
    }

    /// <summary>The coarsest level a kind still shows at: the parts that carry an L2 (lamps, kiosks, stops) stay to L2.</summary>
    private static LodLevel Deepest(FurnitureKind kind) => kind switch
    {
        FurnitureKind.StreetLamp or FurnitureKind.AtlasKiosk or FurnitureKind.BusStop => LodLevel.L2,
        FurnitureKind.Bench or FurnitureKind.Bin or FurnitureKind.AtlasPost or FurnitureKind.EntranceMat
            or FurnitureKind.OpenSign or FurnitureKind.EntranceLamp => LodLevel.L1,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown furniture kind."),
    };

    private static (int X, int Z) Cell(Vector2 p, LodLevel lod)
    {
        var size = 64f * (1 << (int)lod);
        return ((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size));
    }
}

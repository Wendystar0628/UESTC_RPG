using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.Core.Axis;
using Campus.Gen.Zones.Core.Framework;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// WP-16 核心区 (MAP-QSH-CORE): the library, the time plaza and sundial, the museum and the West Lake structures.
/// The module is a list of pieces (<see cref="ICorePiece"/>), each a pure function of the world model; the module
/// only routes them to tiles, merges their output and unions their claims. Sub-packages 16b onward add their pieces
/// to <see cref="CoreSite.Pieces"/>, their entries to core.overlay.json and their terrain to core.terrain.json.
/// </summary>
[ContentModule("core", Owner = "WP-16")]
public sealed class CoreModule : IContentModule
{
    /// <summary>Union of every piece's claims; static because the kernel reads claims before any world is loaded.</summary>
    public IReadOnlyList<Claim> Claims { get; } = CoreSite.Claims;

    /// <summary>Upper bounds per 64 m cell for the whole zone (16a measures about 12k L0 triangles in the plaza cell).</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: 60_000, TrianglesL1: 30_000, DrawCalls: 12);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return CoreSite.Pieces(ctx.World).Select(p => CoreTiles.Of(p.Anchor, LodLevel.L0)).Distinct()
            .OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var build = new CoreBuild(ctx.Palette);
        foreach (var piece in CoreSite.Pieces(ctx.World))
            if (CoreTiles.Of(piece.Anchor, lod) == tile) piece.Build(build, lod);
        return build.Finish();
    }

    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return CoreSite.Pieces(ctx.World).Where(p => CoreTiles.Of(p.Anchor, LodLevel.L0) == tile).SelectMany(p => p.Collide()).ToArray();
    }

    public IReadOnlyList<InspectionProbe> Probes => [];
}

/// <summary>
/// The pieces of the core zone in a fixed order. Pieces whose source feature is absent from the world are skipped
/// (a test world, or a map variant), so every piece's From returns null rather than throwing.
/// </summary>
public static class CoreSite
{
    private static readonly ConditionalWeakTable<IWorldModel, IReadOnlyList<ICorePiece>> Cache = new();

    /// <summary>Every claim the zone makes. Keep in step with the pieces' own Claims (a unit test checks).</summary>
    public static IReadOnlyList<Claim> Claims { get; } =
    [
        new FeatureClaim(CoreFeatures.TimePlaza, Aspects.Props),
        new FeatureClaim(CoreFeatures.PlazaRingSteps, Aspects.Massing),
        new FeatureClaim(CoreFeatures.Sundial, Aspects.Massing | Aspects.Props),
    ];

    public static IReadOnlyList<ICorePiece> Pieces(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Cache.GetValue(world, Create);
    }

    private static IReadOnlyList<ICorePiece> Create(IWorldModel world)
    {
        ICorePiece?[] pieces =
        [
            // 16a: the axis and the time plaza.
            TimePlaza.From(world),
            Sundial.From(world),
            // 16b onward: library shell, surrounding blocks, museum, West Lake structures.
        ];
        return pieces.OfType<ICorePiece>().ToArray();
    }
}

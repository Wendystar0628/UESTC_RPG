using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Gen.Zones.Core.Annex;
using Campus.Gen.Zones.Core.Axis;
using Campus.Gen.Zones.Core.Entrance;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using Campus.Gen.Zones.Core.Museum;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// WP-16 核心区 (MAP-QSH-CORE): the library, the time plaza and sundial, the museum and the West Lake structures.
/// The module is a list of pieces (<see cref="ICorePiece"/>), each a pure function of the world model; the module
/// only routes them to tiles, merges their output and unions their claims. Sub-packages 16b onward add their pieces
/// to <see cref="CoreSite.Pieces"/>, their entries to overlay/core.d/ and their terrain to core.terrain.json.
/// </summary>
[ContentModule("core", Owner = "WP-16")]
public sealed class CoreModule() : ZoneModuleBase([new CoreZoneSite()],
    new(TrianglesL0: 60_000, TrianglesL1: 30_000, DrawCalls: 12), cacheResults: false),
    IWindowMetadataSource, ILightMetadataSource, IContentSourceTraceProvider { }

/// <summary>Keeps the core's shared stone and glass sinks together while common owns routing and metadata.</summary>
internal sealed class CoreZoneSite : ZoneSite, ILightMetadataSource
{
    public override string Id => "core";
    public override IReadOnlyList<Claim> Claims => CoreSite.Claims;

    public override void Build(SiteBuilder builder)
    {
        foreach (var group in CoreSite.Pieces(builder.Context.World).GroupBy(piece => TileGrid.At(piece.Anchor, builder.Lod)))
        {
            var build = CreateBuild(builder.Context);
            foreach (var piece in group) piece.Build(build, builder.Lod);
            var output = build.Finish(builder.Lod);
            var anchor = group.First().Anchor;
            foreach (var mesh in output.Meshes) builder.Add(mesh, System.Numerics.Matrix4x4.Identity, anchor);
            foreach (var part in output.Parts) builder.Place(part, anchor);
            foreach (var emitter in output.Emitters ?? []) builder.Light(emitter, anchor);
        }
    }

    public override void Collide(SiteColliders colliders)
    {
        foreach (var piece in CoreSite.Pieces(colliders.Context.World))
            foreach (var collider in piece.Collide()) colliders.Add(collider, piece.Anchor);
    }

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel level)
    {
        foreach (var piece in CoreSite.Pieces(context.World))
        {
            var build = CreateBuild(context);
            piece.Build(build, level);
            var feature = piece is TimePlaza ? CoreIds.TimePlaza : piece.Claims.OfType<FeatureClaim>().SingleOrDefault()?.Feature;
            yield return new($"core/{piece.Id}", feature, build.Finish(level), piece.Collide(), []);
        }
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) =>
        CoreSite.Pieces(world).OfType<IWindowedPiece>()
            .SelectMany(piece => piece.WindowLayout.Grids.Select(grid => (piece.WindowLayout.Feature, grid)));

    public override void DescribeWindows(BuildContext context, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        foreach (var piece in CoreSite.Pieces(context.World).OfType<IWindowedPiece>())
            foreach (var window in piece.Windows(context.WindowNumbering!)) windows.Add(window);
    }

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context) =>
        CoreSite.Pieces(context.World).OfType<TimePlaza>().SelectMany(plaza => plaza.Lights(LodLevel.L0)).ToArray();

    private static CoreBuild CreateBuild(BuildContext context) => new(context.Catalog)
    {
        WindowNumbering = context.WindowNumbering,
        PartLods = context.PartLods,
    };
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
        new FeatureClaim(CoreIds.TimePlaza, Aspects.Props),
        new FeatureClaim(CoreIds.PlazaRingSteps, Aspects.Massing),
        new FeatureClaim(CoreIds.Sundial, Aspects.Massing | Aspects.Props),
        new FeatureClaim(CoreIds.Library, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.LibraryStair, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.Museum, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.NorthAnnex, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.CourtWing, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.LowBlock, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.NorthWestLink, Aspects.Massing | Aspects.Facade | Aspects.Roof),
        new FeatureClaim(CoreIds.NorthWestJoint, Aspects.Massing | Aspects.Facade | Aspects.Roof),
    ];

    public static IReadOnlyList<ICorePiece> Pieces(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Cache.GetValue(world, Create);
    }

    private static IReadOnlyList<ICorePiece> Create(IWorldModel world)
    {
        var library = LibraryShell.From(world);
        var annex = AnnexSite.From(world, library);
        ICorePiece?[] pieces =
        [
            // 16a: the axis and the time plaza.
            TimePlaza.From(world),
            Sundial.From(world),
            // 16b onward: library shell, surrounding blocks, museum, West Lake structures.
            library,
            // 16c: the grand stair (8398) and the 2F front door in the shell's glass slot.
            GrandStair.From(world, library),
            // 16d-2: the museum (8401 hall body inside the 8402 colonnade ring).
            MuseumShell.From(world),
            // 16e: the library's north blocks (8403, 8406, 8408, 8409, 8410) and the 宁静苑 court they close.
            .. AnnexShell.From(world, annex),
            QuietGarden.From(world, annex),
        ];
        return pieces.OfType<ICorePiece>().ToArray();
    }

}

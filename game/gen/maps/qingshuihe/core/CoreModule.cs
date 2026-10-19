using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Maps.Qingshuihe.Core.Annex;
using Campus.Gen.Maps.Qingshuihe.Core.Axis;
using Campus.Gen.Maps.Qingshuihe.Core.Entrance;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using Campus.Gen.Maps.Qingshuihe.Core.Museum;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>
/// WP-16 核心区 (MAP-QSH-CORE): the library, the time plaza and sundial, the museum and the West Lake structures.
/// The module is a list of pieces (<see cref="ICorePiece"/>), each a pure function of the world model; the module
/// only routes them to tiles, merges their output and unions their claims. Sub-packages 16b onward add their pieces
/// to <see cref="CoreSite.Pieces"/>, their entries to world/core.json and their terrain to core.terrain.json.
/// </summary>
[ContentModule("zone.core", Owner = "WP-16")]
public sealed class CoreModule() : ZoneModuleBase([new CoreZoneSite()],
    new(TrianglesL0: 60_000, TrianglesL1: 30_000, DrawCalls: 12)),
    IWindowMetadataSource, ILightMetadataSource, IContentSourceTraceProvider { }

/// <summary>Keeps the core's shared stone and glass sinks together while common owns routing and metadata.</summary>
internal sealed class CoreZoneSite : ZoneSite, ILightMetadataSource
{
    public override string Id => "core";
    public override IReadOnlyList<Claim> Claims => CoreSite.Claims;
    public override IReadOnlyList<MapRect> Ranges(IWorldModel world) => CoreRanges.All(world);

    public override void Build(SiteBuilder builder)
    {
        foreach (var group in CoreSite.Pieces(builder.Context.World, builder.Includes)
            .Where(piece => builder.Includes(piece.Anchor))
            .GroupBy(piece => TileGrid.At(piece.Anchor, builder.Lod)))
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
        foreach (var piece in CoreSite.Pieces(colliders.Context.World, colliders.Includes)
            .Where(piece => colliders.Includes(piece.Anchor)))
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

    public static IReadOnlyList<ICorePiece> Pieces(IWorldModel world, Func<MapRect, bool>? includes = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        bool Selected(FeatureId feature) => CoreRanges.ForFeature(world, feature) is { } range && (includes?.Invoke(range) ?? true);
        var plazaSelected = Selected(CoreIds.TimePlaza);
        var sundialSelected = Selected(CoreIds.Sundial);
        var librarySelected = Selected(CoreIds.Library);
        var stairSelected = Selected(CoreIds.LibraryStair);
        var museumSelected = Selected(CoreIds.Museum);
        var annexSelected = AnnexSite.Members.Where(member => Selected(member.Feature)).Select(member => member.Feature).ToHashSet();
        var gardenSelected = CoreRanges.Garden(world) is { } gardenRange && (includes?.Invoke(gardenRange) ?? true);
        // The stair and north blocks share the library plan; construct that dependency only for selected sources.
        var library = librarySelected || stairSelected || annexSelected.Count > 0 || gardenSelected ? LibraryShell.From(world) : null;
        var annex = annexSelected.Count > 0 || gardenSelected ? AnnexSite.From(world, library) : null;
        ICorePiece?[] pieces =
        [
            // 16a: the axis and the time plaza.
            plazaSelected ? TimePlaza.From(world) : null,
            sundialSelected ? Sundial.From(world) : null,
            // 16b onward: library shell, surrounding blocks, museum, West Lake structures.
            librarySelected ? library : null,
            // 16c: the grand stair (8398) and the 2F front door in the shell's glass slot.
            stairSelected ? GrandStair.From(world, library) : null,
            // 16d-2: the museum (8401 hall body inside the 8402 colonnade ring).
            museumSelected ? MuseumShell.From(world) : null,
            // 16e: the library's north blocks (8403, 8406, 8408, 8409, 8410) and the 宁静苑 court they close.
            .. AnnexShell.From(world, annex, annexSelected.Contains),
            gardenSelected ? QuietGarden.From(world, annex) : null,
        ];
        return pieces.OfType<ICorePiece>().ToArray();
    }

}

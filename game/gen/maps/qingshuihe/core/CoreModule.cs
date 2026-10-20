using Campus.Gen.World;
using Campus.Gen.Parts.Street;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Circulation;
using Campus.Contracts.Bake;
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

/// <summary>The core's directly registered zone sites; source selection happens before layout construction.</summary>
[ContentModule("zone.core")]
public sealed class CoreModule() : ZoneModuleBase(CoreSite.Pieces, CoreRanges.All, CoreSite.Claims),
    IWindowMetadataSource, ILightMetadataSource, IContentSourceTraceProvider
{
    public override BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.Windows, [], []);
}

public static class CoreSite
{
    public static IReadOnlyList<Claim> Claims { get; } =
    [
        .. TimePlaza.DeclaredClaims, .. Sundial.DeclaredClaims, .. LibraryShell.DeclaredClaims,
        .. GrandStair.DeclaredClaims, .. MuseumShell.DeclaredClaims, .. AnnexShell.DeclaredClaims,
        .. QuietGarden.DeclaredClaims,
    ];

    public static IReadOnlyList<ZoneSite> Pieces(IWorldModel world, Func<MapRect, bool>? includes = null)
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
        ZoneSite?[] pieces =
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
        return pieces.OfType<ZoneSite>().ToArray();
    }

}

using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Maps.Qingshuihe.Core;
using Campus.Gen.Maps.Qingshuihe.Core.Axis;

namespace Campus.Gen.Maps.Qingshuihe.Core.Tests;

/// <summary>Shared real-map contexts and anchors for the core-zone suites.</summary>
internal static class CoreTestSupport
{
    internal static BuildContext Context(IWorldModel world, PaletteCatalog? palette = null) =>
        Campus.Tests.RealMapFixture.ZoneContext(new CoreModule(), world, palette, partLods: PartLodFixture.Catalog);

    internal static Vector2 Anchor(Campus.Gen.Sites.ZoneSite site) => site switch
    {
        TimePlaza value => value.Anchor,
        Sundial value => value.Anchor,
        Library.LibraryShell value => value.Anchor,
        Entrance.GrandStair value => value.Anchor,
        Museum.MuseumShell value => value.Anchor,
        Annex.AnnexShell value => value.Anchor,
        Annex.QuietGarden value => value.Anchor,
        _ => throw new ArgumentOutOfRangeException(nameof(site)),
    };

}

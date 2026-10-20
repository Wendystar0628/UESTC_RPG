using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Tests;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Tests;

internal static class CenterEastTestFixture
{
    internal static BuildContext Context()
    {
        // The center-east sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { CodeHash = "fixture", WindowNumbering = new((Descriptor()).Id, new string('0', 64), []), GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = Descriptor(), WorldSeed = 1 };
    }

    internal static BuildInputSnapshot TextInputs() => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    {
        [SceneTextInputs.AtlasKey] = File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
    }, "fixture");

    internal static MapDescriptor Descriptor() => RealMapFixture.Entry().Descriptor;

    // Geometry, pads and pool water levels all come from the same registered production world.
    internal static IWorldModel World() => RealMapFixture.Load().World;
}

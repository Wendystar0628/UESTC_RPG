using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Tests;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Tests;

internal static class CenterEastTestFixture
{
    internal static BuildContext Context() => RealMapFixture.ZoneContext(new CenterEastModule(), globalInputs: TextInputs());

    internal static BuildInputSnapshot TextInputs() => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    {
        [SceneTextInputs.AtlasKey] = File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
    }, "fixture");

    internal static MapDescriptor Descriptor() => RealMapFixture.Entry().Descriptor;

    // Geometry, pads and pool water levels all come from the same registered production world.
    internal static IWorldModel World() => RealMapFixture.Load().World;
}

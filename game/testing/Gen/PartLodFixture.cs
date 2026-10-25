using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.BakeInput;

namespace Campus.Gen.Maps.Qingshuihe.Core.Tests;

/// <summary>
/// The part LOD catalogue a fixture's BuildContext carries, captured the way the kernel does (BuildInputAssembly.CapturePartLods):
/// each part selected by production registration discovery, created once and read only through
/// <see cref="IPartDef"/>.
/// </summary>
internal static class PartLodFixture
{
    public static PartLodCatalog Catalog { get; } = Capture();

    private static PartLodCatalog Capture() => BuildInputAssembly.CapturePartLods(
        RegistrationDiscovery.Discover(typeof(PartSolids).Assembly.GetTypes(), _ => "fixture"),
        type => RegistrationDiscovery.Construct(type, parameter => parameter.HasDefaultValue ? parameter.DefaultValue
            : throw new InvalidOperationException("Fixture registration has an unresolved dependency.")));
}

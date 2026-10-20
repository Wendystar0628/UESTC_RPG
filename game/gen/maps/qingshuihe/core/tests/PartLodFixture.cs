using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Pipeline.Map;

namespace Campus.Gen.Maps.Qingshuihe.Core.Tests;

/// <summary>
/// The part LOD catalogue a fixture's BuildContext carries, captured the way the kernel does (BuildInputAssembly.CapturePartLods):
/// each part selected by production registration discovery, created once and read only through
/// <see cref="IPartDef"/>.
/// </summary>
internal static class PartLodFixture
{
    public static PartLodCatalog Catalog { get; } = Capture();

    private static PartLodCatalog Capture()
    {
        var definitions = new List<KeyValuePair<PartId, IPartDef>>();
        foreach (var entry in RegistrationDiscovery.Query<IPartDef>(RegistrationDiscovery.Discover([typeof(PartGeometryKernel).Assembly]), RegistrationKind.Part))
            definitions.Add(new(new PartId(entry.Id), (IPartDef)RegistrationDiscovery.Construct(entry.Type, parameter => parameter.HasDefaultValue ? parameter.DefaultValue : throw new InvalidOperationException("Fixture registration has an unresolved dependency."))));
        return new PartLodCatalog(definitions);
    }
}

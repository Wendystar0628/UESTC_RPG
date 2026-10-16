using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Zones.Core.Tests;

/// <summary>
/// The part LOD catalogue a fixture's BuildContext carries, captured the way the kernel does (BuildInputAssembly.CapturePartLods):
/// every <see cref="PartAttribute"/> type of the part packages the zone references, created once and read only through
/// <see cref="IPartDef"/>.
/// </summary>
internal static class PartLodFixture
{
    private static readonly Type[] Packages =
        [typeof(PartGeometryKernel), typeof(OpeningPlacement), typeof(CirculationGallery), typeof(RoofGallery), typeof(StreetGallery)];

    public static PartLodCatalog Catalog { get; } = Capture();

    private static PartLodCatalog Capture()
    {
        var definitions = new List<KeyValuePair<PartId, IPartDef>>();
        foreach (var assembly in Packages.Select(t => t.Assembly).Distinct())
            foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && t.GetCustomAttribute<PartAttribute>() is not null))
                definitions.Add(new(new PartId(type.GetCustomAttribute<PartAttribute>()!.Id), (IPartDef)Activator.CreateInstance(type)!));
        return new PartLodCatalog(definitions);
    }
}

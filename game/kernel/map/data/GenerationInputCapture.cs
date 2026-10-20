using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Kernel.Registry;
using Campus.Gen.BakeInput;

namespace Campus.Kernel.Maps;

/// <summary>Capture registered CPU objects on the engine thread before publishing worker input.</summary>
public static class GenerationInputCapture
{
    public static PartLodCatalog CapturePartLods(ServiceRegistry registry, MapId map) =>
        BuildInputAssembly.CapturePartLods(registry.ForMap(map.Value), type => registry.Create(type, map));

    public static IReadOnlyList<IGroundMaskContributor> CaptureGroundMaskContributors(ServiceRegistry registry, MapId map) =>
        BuildInputAssembly.CaptureGroundMaskContributors(registry.ForMap(map.Value), type => registry.Create(type, map));
}

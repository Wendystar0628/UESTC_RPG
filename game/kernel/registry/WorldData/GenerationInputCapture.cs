using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Kernel.Registry;
using Campus.Pipeline.Map;

namespace Campus.Kernel.Maps;

/// <summary>Capture registered CPU objects on the engine thread before publishing worker input.</summary>
public static class GenerationInputCapture
{
    public static MapGenerationInputs Capture(ServiceRegistry registry, MapId map, PaletteCatalog catalog,
        BuildInputSnapshot globalInputs) => GenerationInputAssembly.Capture(map, Registrations(registry), registry.Create,
            type => registry.CreateInteriorProvider(type, map), CodeHash, catalog, globalInputs);

    public static PartLodCatalog CapturePartLods(ServiceRegistry registry, MapId map) =>
        GenerationInputAssembly.CapturePartLods(RegistrationDiscovery.ForMap(Registrations(registry), entry => entry.Map, map.Value), registry.Create);

    public static IReadOnlyList<IGroundMaskContributor> CaptureGroundMaskContributors(ServiceRegistry registry, MapId map) =>
        GenerationInputAssembly.CaptureGroundMaskContributors(RegistrationDiscovery.ForMap(Registrations(registry), entry => entry.Map, map.Value), registry.Create);

    private static IReadOnlyList<GenerationRegistration> Registrations(ServiceRegistry registry) => registry.Entries
        .Where(entry => entry.Kind is "content" or "part" or "interior")
        .Select(entry => new GenerationRegistration(entry.Kind, entry.Id, entry.Owner, entry.Map, entry.Type, entry.IsStub)).ToArray();

    private static string CodeHash(Type type) => BuildInfo.ModuleCodeHashes.TryGetValue(type.Assembly.GetName().Name!, out var hash)
        ? hash : throw new InvalidDataException($"BuildInfo has no code fingerprint for {type.Assembly.GetName().Name}.");
}

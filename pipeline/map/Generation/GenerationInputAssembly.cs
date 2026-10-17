using Campus.Contracts;
using Campus.Contracts.Parts;
using System.Collections.Concurrent;
using System.Reflection;

namespace Campus.Pipeline.Map;

/// <summary>Compose already discovered map registrations with the host's CPU-only factories.</summary>
public static class GenerationInputAssembly
{
    private static readonly ConcurrentDictionary<Assembly, string> CodeVersions = new();

    public static string CodeVersion(Type type) => CodeVersion(type.Assembly);

    /// <summary>Assembly identities cover the generator and its actual Campus references without reading source files.</summary>
    public static string CodeVersion(Assembly assembly) => CodeVersions.GetOrAdd(assembly, source =>
    {
        // The runtime is a composition root; its content registrations are tracked per consuming cell by the baker.
        if (source.GetName().Name == "Campus") return source.ManifestModule.ModuleVersionId.ToString("N");
        var versions = new SortedDictionary<string, string>(StringComparer.Ordinal);
        Visit(source);
        return string.Join(";", versions.Select(pair => pair.Key + "@" + pair.Value));

        void Visit(Assembly current)
        {
            if (!versions.TryAdd(current.GetName().Name!, current.ManifestModule.ModuleVersionId.ToString("N"))) return;
            foreach (var reference in current.GetReferencedAssemblies())
                if (reference.Name?.StartsWith("Campus.", StringComparison.Ordinal) == true) Visit(Assembly.Load(reference));
        }
    });

    public static MapGenerationInputs Capture(MapId map, IReadOnlyList<GenerationRegistration> registrations,
        Func<Type, object> create, Func<Type, IInteriorProvider> createInterior,
        Func<Type, string> codeHash, PaletteCatalog catalog, BuildInputSnapshot globalInputs)
    {
        var entries = RegistrationDiscovery.ForMap(registrations, entry => entry.Map, map.Value);
        var modules = entries.Where(entry => entry.Kind == "content").ToDictionary(entry => entry.Id,
            entry => new ContentModuleInput((IContentModule)create(entry.Type), codeHash(entry.Type),
                new WpId(entry.Owner), entry.Map is null), StringComparer.Ordinal);
        var interiors = entries.Where(entry => entry.Kind == "interior").ToDictionary(entry => entry.Id,
            entry => createInterior(entry.Type), StringComparer.Ordinal);
        var interiorCodeHashes = entries.Where(entry => entry.Kind == "interior").ToDictionary(entry => entry.Id,
            entry => codeHash(entry.Type), StringComparer.Ordinal);
        var definitions = CapturePartDefinitions(entries, create);
        var collision = new PartCollisionBinding(definitions);
        return new(catalog, modules, new PartLodCatalog(definitions), interiors, interiorCodeHashes, globalInputs,
            CaptureGroundMaskContributors(entries, create), collision.Collision);
    }

    /// <summary>
    /// Capture the current map's registered part capabilities once on the composition/main thread.
    /// This creates only existing pure CPU part definitions, reads no reflected properties and runs no builders.
    /// Publish the returned immutable catalog in every BuildContext for this map; workers never use the registry.
    /// </summary>
    public static PartLodCatalog CapturePartLods(IReadOnlyList<GenerationRegistration> entries, Func<Type, object> create) =>
        new(CapturePartDefinitions(entries, create));

    private static IReadOnlyList<KeyValuePair<PartId, IPartDef>> CapturePartDefinitions(
        IReadOnlyList<GenerationRegistration> entries, Func<Type, object> create)
    {
        var definitions = new List<KeyValuePair<PartId, IPartDef>>();
        foreach (var entry in entries.Where(entry => entry.Kind == "part")
            .OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            if (create(entry.Type) is not IPartDef definition)
                throw new InvalidOperationException($"Registered part '{entry.Id}' ({entry.Type.FullName}) must implement IPartDef.");
            definitions.Add(new(new PartId(entry.Id), definition));
        }
        return definitions.AsReadOnly();
    }

    /// <summary>Capture existing content-module capabilities after map filtering, on the composition thread.</summary>
    public static IReadOnlyList<IGroundMaskContributor> CaptureGroundMaskContributors(IReadOnlyList<GenerationRegistration> entries, Func<Type, object> create)
    {
        var selected = entries
            .Where(entry => entry.Kind == "content" && typeof(IGroundMaskContributor).IsAssignableFrom(entry.Type))
            .OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray();
        var contributors = new List<IGroundMaskContributor>();
        foreach (var entry in selected)
        {
            if (!typeof(IContentModule).IsAssignableFrom(entry.Type))
                throw new InvalidOperationException($"Ground-mask contributor '{entry.Id}' must also implement IContentModule.");
            contributors.Add((IGroundMaskContributor)create(entry.Type));
        }
        return BuildInputAssembly.FreezeGroundMaskContributors(contributors);
    }

}

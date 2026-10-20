using Campus.Pipeline.Map;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Pipeline.Map;

/// <summary>Freeze global resources and discovered ground-mask capabilities at the loading boundary.</summary>
public static class BuildInputAssembly
{
    public static IReadOnlyList<string> RequiredGlobalKeys { get; } = Array.AsReadOnly(new[] { "vegetation/species.json", SceneTextInputs.AtlasKey });

    /// <summary>Read an explicit global asset manifest and freeze its bytes once. Keys are relative to game/data.</summary>
    public static BuildInputSnapshot ReadRequiredGlobalInputs(IEnumerable<string> requiredGlobalKeys,
        Func<string, ReadOnlyMemory<byte>> read, string version)
    {
        ArgumentNullException.ThrowIfNull(requiredGlobalKeys);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var keys = requiredGlobalKeys.Order(StringComparer.Ordinal).ToArray();
        foreach (var key in keys) ValidateKey("global", key);
        if (keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new ArgumentException("The global build-input manifest contains duplicate keys.", nameof(requiredGlobalKeys));
        if (keys.Length == 0) return BuildInputSnapshot.Empty;
        var bytes = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            ReadOnlyMemory<byte> input;
            try { input = read(key); }
            catch (Exception error) when (error is IOException or KeyNotFoundException)
            { throw new InvalidDataException($"Missing required build input: scope=global key='{key}'.", error); }
            if (input.IsEmpty) throw new InvalidDataException($"Missing or empty required build input: scope=global key='{key}'.");
            bytes.Add(key, input);
        }
        return BuildInputSnapshot.Freeze(bytes, version);
    }

    /// <summary>Publish a stable read-only capability list, rejecting duplicate IDs/channel owners before any Paint call.</summary>
    public static IReadOnlyList<IGroundMaskContributor> FreezeGroundMaskContributors(IEnumerable<IGroundMaskContributor> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var items = source.ToArray();
        foreach (var item in items) ArgumentNullException.ThrowIfNull(item);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var owned = GroundMaskChannels.None;
        const GroundMaskChannels known = GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter | GroundMaskChannels.MapleLeaves;
        foreach (var item in items.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id))
                throw new InvalidOperationException($"Empty or duplicate ground-mask contributor ID: '{item.Id}'.");
            if (item.Channels == GroundMaskChannels.None || (item.Channels & ~known) != GroundMaskChannels.None)
                throw new InvalidOperationException($"Ground-mask contributor '{item.Id}' declares empty or unknown channels.");
            if ((owned & item.Channels) != GroundMaskChannels.None)
                throw new InvalidOperationException($"Ground-mask contributor '{item.Id}' conflicts on channels {owned & item.Channels}.");
            owned |= item.Channels;
        }
        return Array.AsReadOnly(items.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray());
    }

    private static void ValidateKey(string scope, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key != key.Trim() || key.StartsWith('/') || key.Contains('\\') || key.Contains(':') ||
            key.Split('/').Any(part => part.Length == 0 || part is "." or ".."))
            throw new ArgumentException($"Invalid build-input path: scope={scope} key='{key}'. Expected a canonical relative key.", nameof(key));
    }
    public static MapGenerationInputs Capture(MapId map, IReadOnlyList<Registration> registrations,
        Func<Type, object> create,
        Func<Type, string> codeHash, PaletteCatalog catalog, BuildInputSnapshot globalInputs)
    {
        var entries = RegistrationDiscovery.ForMap(registrations, map.Value);
        var modules = RegistrationDiscovery.Query<IContentModule>(entries, RegistrationKind.Content).ToDictionary(entry => entry.Id,
            entry => new ContentModuleInput((IContentModule)create(entry.Type), codeHash(entry.Type),
                new WpId(entry.Owner), entry.Map is null), StringComparer.Ordinal);
        var interiors = RegistrationDiscovery.Query<IInteriorProvider>(entries, RegistrationKind.Interior).ToDictionary(entry => entry.Id,
            entry => (IInteriorProvider)create(entry.Type), StringComparer.Ordinal);
        var interiorCodeHashes = RegistrationDiscovery.Query<IInteriorProvider>(entries, RegistrationKind.Interior).ToDictionary(entry => entry.Id,
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
    public static PartLodCatalog CapturePartLods(IReadOnlyList<Registration> entries, Func<Type, object> create) =>
        new(CapturePartDefinitions(entries, create));

    private static IReadOnlyList<KeyValuePair<PartId, IPartDef>> CapturePartDefinitions(
        IReadOnlyList<Registration> entries, Func<Type, object> create)
    {
        var definitions = new List<KeyValuePair<PartId, IPartDef>>();
        foreach (var entry in RegistrationDiscovery.Query<IPartDef>(entries, RegistrationKind.Part))
        {
            if (create(entry.Type) is not IPartDef definition)
                throw new InvalidOperationException($"Registered part '{entry.Id}' ({entry.Type.FullName}) must implement IPartDef.");
            definitions.Add(new(new PartId(entry.Id), definition));
        }
        return definitions.AsReadOnly();
    }

    /// <summary>Capture existing content-module capabilities after map filtering, on the composition thread.</summary>
    public static IReadOnlyList<IGroundMaskContributor> CaptureGroundMaskContributors(IReadOnlyList<Registration> entries, Func<Type, object> create)
    {
        var selected = RegistrationDiscovery.Query<IGroundMaskContributor>(entries, RegistrationKind.Content);
        var contributors = new List<IGroundMaskContributor>();
        foreach (var entry in selected)
        {
            if (!typeof(IContentModule).IsAssignableFrom(entry.Type))
                throw new InvalidOperationException($"Ground-mask contributor '{entry.Id}' must also implement IContentModule.");
            contributors.Add((IGroundMaskContributor)create(entry.Type));
        }
        return FreezeGroundMaskContributors(contributors);
    }

}

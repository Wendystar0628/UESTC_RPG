using System.Collections.Frozen;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Registry;

/// <summary>The immutable packaged build index shared by registration discovery and CPU input identity.</summary>
internal sealed class BuildCodeIndex
{
    internal const string AssetPath = "res://data/build-code-fingerprints.json";
    private readonly FrozenDictionary<string, string> _roots;
    private readonly FrozenDictionary<string, string> _owners;
    private static readonly Lazy<BuildCodeIndex> Packaged = new(Load);
    internal static BuildCodeIndex Shared => Packaged.Value;

    internal BuildCodeIndex(BakeCodeManifest manifest)
    {
        if (manifest.Schema != BakeCodeManifest.CurrentSchema || manifest.Roots is null || manifest.Roots.Count == 0 || manifest.Registrations is null)
            throw new InvalidDataException("Missing or unsupported build code fingerprint manifest.");
        foreach (var entry in manifest.Roots)
            if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                throw new InvalidDataException("Build code fingerprint entries must have a key and hash.");
        _roots = manifest.Roots.ToFrozenDictionary(StringComparer.Ordinal);
        foreach (var entry in manifest.Registrations)
            if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                throw new InvalidDataException("Registration owners must have a type key and owner.");
        _owners = manifest.Registrations.ToFrozenDictionary(StringComparer.Ordinal);
    }

    internal static BuildCodeIndex Load()
    {
        if (!Godot.FileAccess.FileExists(AssetPath))
            throw new InvalidDataException($"Missing build code fingerprints: {AssetPath}.");
        return Parse(Godot.FileAccess.GetFileAsBytes(AssetPath));
    }

    internal static BuildCodeIndex Parse(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var fields = new HashSet<string>(ContractJson.Options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var field in document.RootElement.EnumerateObject())
            if (!fields.Add(field.Name)) throw new InvalidDataException($"Duplicate build manifest field: {field.Name}.");
        var rootsName = ContractJson.Options.PropertyNamingPolicy?.ConvertName(nameof(BakeCodeManifest.Roots)) ?? nameof(BakeCodeManifest.Roots);
        var roots = document.RootElement.GetProperty(rootsName);
        var ownersName = ContractJson.Options.PropertyNamingPolicy?.ConvertName(nameof(BakeCodeManifest.Registrations)) ?? nameof(BakeCodeManifest.Registrations);
        var owners = document.RootElement.GetProperty(ownersName);
        var ownerKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in owners.EnumerateObject())
            if (!ownerKeys.Add(entry.Name)) throw new InvalidDataException($"Duplicate registration owner: {entry.Name}.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in roots.EnumerateObject())
            if (!keys.Add(entry.Name)) throw new InvalidDataException($"Duplicate build code fingerprint: {entry.Name}.");
        return new(JsonSerializer.Deserialize<BakeCodeManifest>(bytes.Span, ContractJson.Options)
            ?? throw new InvalidDataException("Missing build code fingerprint manifest."));
    }

    internal string Owner(Type type)
    {
        var key = BakeCodeManifest.TypeKey(type);
        return _owners.TryGetValue(key, out var owner) ? owner
            : throw new InvalidDataException($"Missing build registration owner: {key}.");
    }

    internal string Type(Type type) => Require(BakeCodeManifest.TypeKey(type));
    internal string Chain(string key) => Require(BakeCodeManifest.Chain(key));
    private string Require(string key) => _roots.TryGetValue(key, out var value) ? value
        : throw new InvalidDataException($"Missing build code fingerprint: {key}.");
}

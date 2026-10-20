using System.Collections.Frozen;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>The immutable build asset, read once by the existing terrain/bake service lifetime.</summary>
internal sealed class BakeCodeFingerprints
{
    internal const string AssetPath = "res://data/build-code-fingerprints.json";
    private readonly FrozenDictionary<string, string> _roots;

    internal BakeCodeFingerprints(BakeCodeManifest manifest)
    {
        if (manifest.Schema != BakeCodeManifest.CurrentSchema || manifest.Roots is null || manifest.Roots.Count == 0)
            throw new InvalidDataException("Missing or unsupported build code fingerprint manifest.");
        foreach (var entry in manifest.Roots)
            if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                throw new InvalidDataException("Build code fingerprint entries must have a key and hash.");
        _roots = manifest.Roots.ToFrozenDictionary(StringComparer.Ordinal);
    }

    internal static BakeCodeFingerprints Load()
    {
        if (!Godot.FileAccess.FileExists(AssetPath))
            throw new InvalidDataException($"Missing build code fingerprints: {AssetPath}.");
        return Parse(Godot.FileAccess.GetFileAsBytes(AssetPath));
    }

    internal static BakeCodeFingerprints Parse(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var fields = new HashSet<string>(ContractJson.Options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var field in document.RootElement.EnumerateObject())
            if (!fields.Add(field.Name)) throw new InvalidDataException($"Duplicate build manifest field: {field.Name}.");
        var rootsName = ContractJson.Options.PropertyNamingPolicy?.ConvertName(nameof(BakeCodeManifest.Roots)) ?? nameof(BakeCodeManifest.Roots);
        var roots = document.RootElement.GetProperty(rootsName);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in roots.EnumerateObject())
            if (!keys.Add(entry.Name)) throw new InvalidDataException($"Duplicate build code fingerprint: {entry.Name}.");
        return new(JsonSerializer.Deserialize<BakeCodeManifest>(bytes.Span, ContractJson.Options)
            ?? throw new InvalidDataException("Missing build code fingerprint manifest."));
    }

    internal string Type(Type type) => Require(BakeCodeManifest.TypeKey(type));
    internal string Chain(string key) => Require(BakeCodeManifest.Chain(key));
    private string Require(string key) => _roots.TryGetValue(key, out var value) ? value
        : throw new InvalidDataException($"Missing build code fingerprint: {key}.");
}

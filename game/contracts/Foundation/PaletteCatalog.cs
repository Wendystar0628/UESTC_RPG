using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

/// <summary>Complete metadata view; Albedo remains normalized sRGB, never a current-frame evaluated colour.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PaletteEntryV2(
    [property: JsonRequired] PaletteToken Token,
    [property: JsonRequired] PaletteIndex Index,
    [property: JsonRequired] Vector3 Albedo,
    [property: JsonRequired] string MaterialClass,
    [property: JsonRequired] bool Seasonal,
    [property: JsonRequired, JsonConverter(typeof(PaletteRoleV2JsonConverter))] PaletteRole Role)
{
    /// <summary>Only copied legacy entries can have a seasonal row. Schema-2 extensions require null.</summary>
    public int? Row { get; init; }
}

// Local to PaletteEntryV2.Role. Do not change the legacy enum converter or its saved-data casing compatibility.
internal sealed class PaletteRoleV2JsonConverter : JsonConverter<PaletteRole>
{
    public PaletteRoleV2JsonConverter() { }

    public override PaletteRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected one canonical palette role string.");
        return reader.GetString() switch
        {
            "frame" => PaletteRole.Frame,
            "retired" => PaletteRole.Retired,
            "lit" => PaletteRole.Lit,
            "shade" => PaletteRole.Shade,
            "weather" => PaletteRole.Weather,
            "emissive" => PaletteRole.Emissive,
            _ => throw new JsonException("Expected one canonical palette role; combined, repeated or noncanonical values are invalid.")
        };
    }

    public override void Write(Utf8JsonWriter writer, PaletteRole value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            PaletteRole.Frame => "frame",
            PaletteRole.Retired => "retired",
            PaletteRole.Lit => "lit",
            PaletteRole.Shade => "shade",
            PaletteRole.Weather => "weather",
            PaletteRole.Emissive => "emissive",
            _ => throw new JsonException("Unknown palette role.")
        });
}

/// <summary>Schema-2 extension file only: indices 256..511, non-seasonal Lit/Shade pairs. Does not replace schema 1.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PaletteExtensionData(
    [property: JsonRequired] IReadOnlyList<PaletteEntryV2> Entries,
    [property: JsonRequired] int Schema = PaletteLayout.ExtensionSchema);

/// <summary>Resolves a token to its logical palette index. Does not load files or allocate resources.</summary>
public interface IPaletteIndexResolver
{
    PaletteIndex Resolve(PaletteToken token);
}

/// <summary>
/// Immutable union of schema-1 base-bank data and explicitly supplied schema-2 entries.
/// ExtensionCount describes metadata in this snapshot, not texture/upload readiness.
/// </summary>
public sealed class PaletteCatalog : IPaletteIndexResolver
{
    private readonly IReadOnlyDictionary<PaletteToken, PaletteEntryV2> _byToken;
    private readonly PaletteEntryV2?[] _byIndex = new PaletteEntryV2?[PaletteLayout.Capacity];

    public PaletteCatalog(PaletteRegistry legacyRegistry, IEnumerable<PaletteEntryV2> extensions)
    {
        ArgumentNullException.ThrowIfNull(legacyRegistry);
        ArgumentNullException.ThrowIfNull(extensions);
        var byToken = new Dictionary<PaletteToken, PaletteEntryV2>();
        foreach (var entry in legacyRegistry.Entries)
            Add(new(entry.Token, new(entry.Id), entry.Albedo, entry.MaterialClass, entry.Seasonal, entry.Role) { Row = entry.Row });
        foreach (var entry in extensions)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ValidateExtension(entry);
            Add(entry);
        }
        for (var index = PaletteLayout.LegacyCapacity; index < PaletteLayout.Capacity; index += 2)
            if ((_byIndex[index] is null) != (_byIndex[index + 1] is null))
                throw new ArgumentException($"Palette extension pair {index}/{index + 1} is incomplete.", nameof(extensions));

        _byToken = new ReadOnlyDictionary<PaletteToken, PaletteEntryV2>(byToken);
        Entries = Array.AsReadOnly(byToken.Values.OrderBy(entry => entry.Index.Value).ToArray());
        ExtensionCount = Entries.Count - legacyRegistry.Count;

        void Add(PaletteEntryV2 entry)
        {
            if (!byToken.TryAdd(entry.Token, entry))
                throw new ArgumentException($"Duplicate palette token '{entry.Token.Value}'.", nameof(extensions));
            if (_byIndex[entry.Index.Value] is not null)
                throw new ArgumentException($"Duplicate palette index {entry.Index.Value}.", nameof(extensions));
            _byIndex[entry.Index.Value] = entry;
        }
    }

    public IReadOnlyList<PaletteEntryV2> Entries { get; }
    public int Count => Entries.Count;
    public int ExtensionCount { get; }

    /// <summary>Assembles the complete catalog from caller-read schema-1 and schema-2 files, without IO or engine resources.</summary>
    public static PaletteCatalog FromSources(IEnumerable<(string Path, byte[] Json)> bank0Files,
        IEnumerable<(string Path, byte[] Json)> extensionFiles)
    {
        ArgumentNullException.ThrowIfNull(bank0Files);
        ArgumentNullException.ThrowIfNull(extensionFiles);
        var bank0 = new List<PaletteEntry>();
        var extensions = new List<PaletteEntryV2>();
        foreach (var (path, json) in bank0Files)
        {
            var data = Parse<PaletteRegistryData>(path, json);
            if (data.Schema != 1) throw new InvalidDataException($"Unsupported palette registry schema {data.Schema}: {path}");
            bank0.AddRange(data.Entries);
        }
        foreach (var (path, json) in extensionFiles)
        {
            var data = Parse<PaletteExtensionData>(path, json);
            if (data.Schema != PaletteLayout.ExtensionSchema)
                throw new InvalidDataException($"Unsupported palette extension schema {data.Schema}: {path}");
            extensions.AddRange(data.Entries);
        }
        try { return new(new PaletteRegistry(bank0), extensions); }
        catch (ArgumentException error) { throw new InvalidDataException($"Invalid palette catalog: {error.Message}", error); }

        static T Parse<T>(string path, byte[] json) where T : class
        {
            try { return JsonSerializer.Deserialize<T>(json, ContractJson.Options) ?? throw new InvalidDataException($"Empty palette data: {path}"); }
            catch (JsonException error) { throw new InvalidDataException($"Invalid palette data {path}: {error.Message}", error); }
        }
    }

    public static PaletteCatalog FromData(PaletteRegistry legacyRegistry, PaletteExtensionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Schema != PaletteLayout.ExtensionSchema)
            throw new ArgumentException($"Expected palette extension schema {PaletteLayout.ExtensionSchema}.", nameof(data));
        return new(legacyRegistry, data.Entries);
    }

    public PaletteIndex Resolve(PaletteToken token) => Get(token).Index;
    public PaletteEntryV2 Get(PaletteToken token) => TryGet(token, out var entry) ? entry
        : throw new KeyNotFoundException($"Palette token '{token.Value ?? "<null>"}' is not registered.");
    public PaletteEntryV2 Get(PaletteIndex index) => TryGet(index, out var entry) ? entry
        : throw new KeyNotFoundException($"Palette index {index.Value} is not registered.");
    public bool TryGet(PaletteToken token, [NotNullWhen(true)] out PaletteEntryV2? entry) => _byToken.TryGetValue(token, out entry);
    public bool TryGet(PaletteIndex index, [NotNullWhen(true)] out PaletteEntryV2? entry)
    {
        entry = _byIndex[index.Value];
        return entry is not null;
    }

    /// <summary>Checks an even Lit index and its adjacent Shade; legacy token/colour meanings stay unchanged.</summary>
    public void Validate(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.ValidateSurfacePattern();
        var lit = Get(spec.Lit);
        var shade = Get(spec.Shade);
        if (lit.Role != PaletteRole.Lit || shade.Role != PaletteRole.Shade ||
            (lit.Index.Value & 1) != 0 || shade.Index.Value != lit.Index.Value + 1)
            throw new ArgumentException("ToonSpec requires an even Lit index and its adjacent Shade.", nameof(spec));
        if (spec.Deep is { } deep) _ = Get(deep);
        if (spec.Backface is { } backface) _ = Get(backface);
    }

    private static void ValidateExtension(PaletteEntryV2 entry)
    {
        if (entry.Index.Value < PaletteLayout.LegacyCapacity || entry.Seasonal || entry.Row is not null)
            throw new ArgumentException("Extensions require indices 256..511 and no seasonal row.", nameof(entry));
        var validRole = entry.Role switch
        {
            PaletteRole.Lit => (entry.Index.Value & 1) == 0,
            PaletteRole.Shade => (entry.Index.Value & 1) != 0,
            PaletteRole.Frame or PaletteRole.Retired or PaletteRole.Weather or PaletteRole.Emissive => false,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Role, "Unknown palette role.")
        };
        if (!validRole) throw new ArgumentException("Extensions only contain even Lit / odd Shade entries.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Token.Value) || entry.Token.Value != entry.Token.Value.Trim() ||
            string.IsNullOrWhiteSpace(entry.MaterialClass) || entry.MaterialClass != entry.MaterialClass.Trim())
            throw new ArgumentException("Palette token and material class must be non-empty without surrounding whitespace.", nameof(entry));
        var color = entry.Albedo;
        if (!float.IsFinite(color.X) || !float.IsFinite(color.Y) || !float.IsFinite(color.Z) ||
            color.X is < 0f or > 1f || color.Y is < 0f or > 1f || color.Z is < 0f or > 1f)
            throw new ArgumentException("Albedo requires finite normalized sRGB components.", nameof(entry));
    }
}

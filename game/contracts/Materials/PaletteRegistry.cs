using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public enum PaletteRole : byte { Frame, Retired, Lit, Shade, Weather, Emissive }

/// <summary>
/// Albedo is normalized sRGB in [0, 1], not a current-frame evaluated colour.
/// Seasonal Albedo is reference-only; seasonal values come from WP-37 keys/vegetation.json.
/// MaterialClass is an extensible data key. Token names are case-sensitive.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PaletteEntry(
    [property: JsonRequired] PaletteToken Token,
    [property: JsonRequired] PaletteIndex Index,
    [property: JsonRequired] Vector3 Albedo,
    [property: JsonRequired] string MaterialClass,
    [property: JsonRequired] bool Seasonal,
    [property: JsonRequired, JsonConverter(typeof(PaletteRoleJsonConverter))] PaletteRole Role)
{
    /// <summary>Bank-0 seasonal row 4-63 when Seasonal is true; otherwise null.</summary>
    public int? Row { get; init; }
}

// Canonical roles are shared by every scene palette source.
internal sealed class PaletteRoleJsonConverter : JsonConverter<PaletteRole>
{
    public PaletteRoleJsonConverter() { }

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

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PaletteRegistryData(
    [property: JsonRequired] IReadOnlyList<PaletteEntry> Entries,
    [property: JsonRequired] int Schema = PaletteRegistryData.CurrentSchema)
{
    public const int CurrentSchema = 2;
}

/// <summary>Immutable, engine-independent registration of palette columns and surface-colour pairs.</summary>
public sealed class PaletteRegistry
{
    private readonly IReadOnlyDictionary<PaletteToken, PaletteEntry> _byToken;
    private readonly PaletteEntry?[] _byId = new PaletteEntry?[256];

    public PaletteRegistry(IEnumerable<PaletteEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var byToken = new Dictionary<PaletteToken, PaletteEntry>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ValidateEntry(entry);
            if (!byToken.TryAdd(entry.Token, entry))
                throw new ArgumentException($"Duplicate palette token '{entry.Token.Value}'.", nameof(entries));
            if (_byId[entry.Index.Value] is { } previous)
                throw new ArgumentException($"Duplicate palette ID {entry.Index.Value}: '{previous.Token.Value}' and '{entry.Token.Value}'.", nameof(entries));
            _byId[entry.Index.Value] = entry;
        }
        for (var id = 64; id < _byId.Length; id += 2)
        {
            var lit = _byId[id];
            var shade = _byId[id + 1];
            if (lit is null && shade is null) continue;
            if (lit is null || shade is null)
                throw new ArgumentException($"Palette surface pair {id}/{id + 1} is incomplete: Lit and adjacent Shade must both be registered.", nameof(entries));
        }
        _byToken = new ReadOnlyDictionary<PaletteToken, PaletteEntry>(byToken);
        Entries = Array.AsReadOnly(byToken.Values.OrderBy(entry => entry.Index.Value).ToArray());
    }

    public IReadOnlyList<PaletteEntry> Entries { get; }
    public int Count => Entries.Count;

    public static PaletteRegistry FromData(PaletteRegistryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Schema != PaletteRegistryData.CurrentSchema) throw new ArgumentException($"Unsupported palette registry schema {data.Schema}; expected {PaletteRegistryData.CurrentSchema}.", nameof(data));
        return new(data.Entries);
    }

    public PaletteEntry Get(PaletteToken token) => TryGet(token, out var entry) ? entry :
        throw new KeyNotFoundException($"Palette token '{token.Value ?? "<null>"}' is not registered.");
    public PaletteEntry Get(byte id) => TryGet(id, out var entry) ? entry :
        throw new KeyNotFoundException($"Palette ID {id} is not registered.");
    public bool TryGet(PaletteToken token, [NotNullWhen(true)] out PaletteEntry? entry) => _byToken.TryGetValue(token, out entry);
    public bool TryGet(byte id, [NotNullWhen(true)] out PaletteEntry? entry)
    {
        entry = _byId[id];
        return entry is not null;
    }

    /// <summary>Checks that the named surface colours resolve to an even Lit ID and its adjacent Shade ID.</summary>
    public void Validate(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.ValidateSurfacePattern();
        var lit = Get(spec.Lit);
        var shade = Get(spec.Shade);
        if (lit.Role != PaletteRole.Lit || shade.Role != PaletteRole.Shade || shade.Index.Value != lit.Index.Value + PaletteLayout.ShadeOffset)
            throw new ArgumentException($"ToonSpec pair '{lit.Token.Value}' ({lit.Index.Value}, {lit.Role}) / '{shade.Token.Value}' ({shade.Index.Value}, {shade.Role}) must be Lit/adjacent Shade.", nameof(spec));
        if (spec.Deep is { } deep) _ = Get(deep);
        if (spec.Backface is { } backface) _ = Get(backface);
    }

    private static void ValidateEntry(PaletteEntry entry)
    {
        if (entry.Index.Value >= PaletteLayout.Bank0Capacity)
            throw new ArgumentException("Bank 0 requires indices 0..255.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Token.Value) || entry.Token.Value != entry.Token.Value.Trim())
            throw new ArgumentException("Palette token must be non-empty and have no surrounding whitespace.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.MaterialClass) || entry.MaterialClass != entry.MaterialClass.Trim())
            throw new ArgumentException($"Palette token '{entry.Token.Value}' needs a non-empty material class.", nameof(entry));
        if (entry.Seasonal && entry.Row is not (>= 4 and <= 63))
            throw new ArgumentException($"Seasonal palette token '{entry.Token.Value}' needs a row in [4, 63].", nameof(entry));
        if (!entry.Seasonal && entry.Row is not null)
            throw new ArgumentException($"Non-seasonal palette token '{entry.Token.Value}' must have a null row.", nameof(entry));
        var color = entry.Albedo;
        if (!float.IsFinite(color.X) || !float.IsFinite(color.Y) || !float.IsFinite(color.Z) ||
            color.X < 0f || color.X > 1f || color.Y < 0f || color.Y > 1f || color.Z < 0f || color.Z > 1f)
            throw new ArgumentException($"Palette token '{entry.Token.Value}' has invalid Albedo; expected finite sRGB components in [0, 1].", nameof(entry));
        var allowed = entry.Role switch
        {
            PaletteRole.Frame => entry.Index.Value <= 31,
            PaletteRole.Retired => entry.Index.Value is >= 32 and <= 39,
            PaletteRole.Lit => entry.Index.Value >= 64 && (entry.Index.Value & 1) == 0,
            PaletteRole.Shade => entry.Index.Value >= 65 && (entry.Index.Value & 1) == 1,
            PaletteRole.Weather => entry.Index.Value is >= 40 and <= 63,
            PaletteRole.Emissive => entry.Index.Value is >= 40 and <= 63,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Role, "Unknown palette role.")
        };
        if (!allowed)
            throw new ArgumentException($"Palette token '{entry.Token.Value}' uses ID {entry.Index.Value} outside its {entry.Role} range; IDs 40-63 are for Weather and Emissive.", nameof(entry));
    }
}

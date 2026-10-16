using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Campus.Contracts;

public enum PaletteRole : byte { Frame, Retired, Lit, Shade, Weather, Emissive }

/// <summary>
/// Albedo is normalized sRGB in [0, 1], not a current-frame evaluated colour.
/// Seasonal Albedo is reference-only; seasonal values come from WP-37 keys/vegetation.json.
/// MaterialClass is an extensible data key. Token names are case-sensitive.
/// </summary>
public sealed record PaletteEntry(PaletteToken Token, byte Id, Vector3 Albedo,
    string MaterialClass, bool Seasonal, PaletteRole Role)
{
    /// <summary>Seasonal row 4-63 when Seasonal is true; otherwise null.</summary>
    public int? Row { get; init; }
}

public sealed record PaletteRegistryData(IReadOnlyList<PaletteEntry> Entries, int Schema = 1);

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
            if (_byId[entry.Id] is { } previous)
                throw new ArgumentException($"Duplicate palette ID {entry.Id}: '{previous.Token.Value}' and '{entry.Token.Value}'.", nameof(entries));
            _byId[entry.Id] = entry;
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
        Entries = Array.AsReadOnly(byToken.Values.OrderBy(entry => entry.Id).ToArray());
    }

    public IReadOnlyList<PaletteEntry> Entries { get; }
    public int Count => Entries.Count;

    public static PaletteRegistry FromData(PaletteRegistryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Schema != 1) throw new ArgumentException($"Unsupported palette registry schema {data.Schema}; expected 1.", nameof(data));
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
        if (lit.Role != PaletteRole.Lit || shade.Role != PaletteRole.Shade || shade.Id != lit.Id + 1)
            throw new ArgumentException($"ToonSpec pair '{lit.Token.Value}' ({lit.Id}, {lit.Role}) / '{shade.Token.Value}' ({shade.Id}, {shade.Role}) must be Lit/adjacent Shade.", nameof(spec));
        if (spec.Deep is { } deep) _ = Get(deep);
        if (spec.Backface is { } backface) _ = Get(backface);
    }

    private static void ValidateEntry(PaletteEntry entry)
    {
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
            PaletteRole.Frame => entry.Id <= 31,
            PaletteRole.Retired => entry.Id is >= 32 and <= 39,
            PaletteRole.Lit => entry.Id >= 64 && (entry.Id & 1) == 0,
            PaletteRole.Shade => entry.Id >= 65 && (entry.Id & 1) == 1,
            PaletteRole.Weather => entry.Id is >= 40 and <= 63,
            PaletteRole.Emissive => entry.Id is >= 40 and <= 63,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Role, "Unknown palette role.")
        };
        if (!allowed)
            throw new ArgumentException($"Palette token '{entry.Token.Value}' uses ID {entry.Id} outside its {entry.Role} range; IDs 40-63 are for Weather and Emissive.", nameof(entry));
    }
}

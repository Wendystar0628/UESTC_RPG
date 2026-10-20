using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Campus.Contracts;

/// <summary>Resolves a token to its logical palette index. Does not load files or allocate resources.</summary>
public interface IPaletteIndexResolver
{
    PaletteIndex Resolve(PaletteToken token);
}

/// <summary>
/// Immutable union of bank-0 data and explicitly supplied bank-1 entries.
/// ExtensionCount describes metadata in this snapshot, not texture/upload readiness.
/// </summary>
public sealed class PaletteCatalog : IPaletteIndexResolver
{
    private readonly IReadOnlyDictionary<PaletteToken, PaletteEntry> _byToken;
    private readonly PaletteEntry?[] _byIndex = new PaletteEntry?[PaletteLayout.Capacity];

    public PaletteCatalog(PaletteRegistry bank0Registry, IEnumerable<PaletteEntry> extensions)
    {
        ArgumentNullException.ThrowIfNull(bank0Registry);
        ArgumentNullException.ThrowIfNull(extensions);
        var byToken = new Dictionary<PaletteToken, PaletteEntry>();
        foreach (var entry in bank0Registry.Entries)
            Add(entry);
        foreach (var entry in extensions)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ValidateExtension(entry);
            Add(entry);
        }
        for (var index = PaletteLayout.Bank0Capacity; index < PaletteLayout.Capacity; index += 2)
            if ((_byIndex[index] is null) != (_byIndex[index + 1] is null))
                throw new ArgumentException($"Palette extension pair {index}/{index + 1} is incomplete.", nameof(extensions));

        _byToken = new ReadOnlyDictionary<PaletteToken, PaletteEntry>(byToken);
        Entries = Array.AsReadOnly(byToken.Values.OrderBy(entry => entry.Index.Value).ToArray());
        ExtensionCount = Entries.Count - bank0Registry.Count;

        void Add(PaletteEntry entry)
        {
            if (!byToken.TryAdd(entry.Token, entry))
                throw new ArgumentException($"Duplicate palette token '{entry.Token.Value}'.", nameof(extensions));
            if (_byIndex[entry.Index.Value] is not null)
                throw new ArgumentException($"Duplicate palette index {entry.Index.Value}.", nameof(extensions));
            _byIndex[entry.Index.Value] = entry;
        }
    }

    public IReadOnlyList<PaletteEntry> Entries { get; }
    public int Count => Entries.Count;
    public int ExtensionCount { get; }

    /// <summary>Reads one registration format for core and content extension sources, without IO or engine resources.</summary>
    public static PaletteCatalog FromSources(IEnumerable<(string Path, byte[] Json)> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var entries = new List<PaletteEntry>();
        foreach (var (path, json) in files)
        {
            PaletteRegistryData data;
            try { data = JsonSerializer.Deserialize<PaletteRegistryData>(json, ContractJson.Options)
                ?? throw new InvalidDataException($"Empty palette data: {path}"); }
            catch (JsonException error) { throw new InvalidDataException($"Invalid palette data {path}: {error.Message}", error); }
            if (data.Schema != PaletteRegistryData.CurrentSchema)
                throw new InvalidDataException($"Unsupported palette registry schema {data.Schema}: {path}");
            entries.AddRange(data.Entries);
        }
        try { return FromData(new(entries)); }
        catch (ArgumentException error) { throw new InvalidDataException($"Invalid palette catalog: {error.Message}", error); }
    }

    public static PaletteCatalog FromData(PaletteRegistryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Schema != PaletteRegistryData.CurrentSchema)
            throw new ArgumentException($"Expected palette registry schema {PaletteRegistryData.CurrentSchema}.", nameof(data));
        return new(new PaletteRegistry(data.Entries.Where(entry => entry.Index.Value < PaletteLayout.Bank0Capacity)),
            data.Entries.Where(entry => entry.Index.Value >= PaletteLayout.Bank0Capacity));
    }

    public PaletteIndex Resolve(PaletteToken token) => Get(token).Index;
    public PaletteEntry Get(PaletteToken token) => TryGet(token, out var entry) ? entry
        : throw new KeyNotFoundException($"Palette token '{token.Value ?? "<null>"}' is not registered.");
    public PaletteEntry Get(PaletteIndex index) => TryGet(index, out var entry) ? entry
        : throw new KeyNotFoundException($"Palette index {index.Value} is not registered.");
    public bool TryGet(PaletteToken token, [NotNullWhen(true)] out PaletteEntry? entry) => _byToken.TryGetValue(token, out entry);
    public bool TryGet(PaletteIndex index, [NotNullWhen(true)] out PaletteEntry? entry)
    {
        entry = _byIndex[index.Value];
        return entry is not null;
    }

    /// <summary>Checks an even Lit index and its adjacent Shade; bank-0 token/colour meanings stay unchanged.</summary>
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

    private static void ValidateExtension(PaletteEntry entry)
    {
        if (entry.Index.Value < PaletteLayout.Bank0Capacity || entry.Seasonal || entry.Row is not null)
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

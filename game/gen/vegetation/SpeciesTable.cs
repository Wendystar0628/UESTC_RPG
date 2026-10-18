using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>Crown family (WLD-VEGETATION). The numeric value is the tree_species code read by the veg_* shaders.</summary>
public enum CrownType : byte { Card = 0, Cloud = 1, Mixed = 2 }

/// <summary>
/// Leaf-card layer: one shape per layer of the leaf-card array (WLD-VEGETATION). The numeric value is the
/// layer number in tree_species; the mainline veg_card shader draws each layer procedurally under the same number.
/// </summary>
public enum CardShape : byte { Fan = 0, Needle = 1, Strand = 2, Palmate = 3, Ovate = 4, Pinnate = 5, Blossom = 6, Spray = 7, Twig = 8, Frond = 9 }

/// <summary>One row of game/data/vegetation/species.json (WLD-VEGETATION).</summary>
public sealed record SpeciesInfo(
    string Id,
    string Name,
    int Row,
    CrownType Crown,
    string Form,
    float HeightMin,
    float HeightMax,
    string Leaf,
    string? Bloom,
    CardShape Card,
    CardShape BloomCard,
    bool Deciduous,
    bool BloomOnBlobs,
    IReadOnlyList<float> LeafAmount,
    IReadOnlyList<float> BloomAmount,
    float SwayDeg,
    string Sound)
{
    public PaletteToken LeafLit => new(Leaf + ".lit");
    public PaletteToken LeafShade => new(Leaf + ".shade");
    public PaletteToken? BloomLit => Bloom is null ? null : new(Bloom + ".lit");
}

/// <summary>
/// The species table: parsed from species.json (callers read the bytes; generators never perform IO) and packed into
/// the tree_species global texture that WP-38 uploads (WLD-VEGETATION). Packed layout, one row per species,
/// <see cref="Width"/> RGBA32F texels per row:
/// x0 leaf amount A at s = 0..3; x1 bloom B at s = 0..3; x2 (sway°, leaf lit row, bloom lit row or −1, bloom lit colour ID or −1);
/// x3 (leaf card, bloom card, twig card, flags: 1 deciduous, 2 bloom on blobs); x4 (crown, leaf lit colour ID, twig-stroke lit colour ID, 0);
/// x5–x6 reserved; x7.w = <see cref="Tag"/> marks a written row (an unset or default texture never carries it).
/// </summary>
public sealed class SpeciesTable
{
    public const int Width = 8;
    public const int Rows = 32;
    public const float Tag = 23f;
    public const int FlagDeciduous = 1;
    public const int FlagBloomOnBlobs = 2;

    private readonly Dictionary<string, SpeciesInfo> _byId;

    private SpeciesTable(IReadOnlyList<SpeciesInfo> species)
    {
        Species = species;
        _byId = species.ToDictionary(s => s.Id, StringComparer.Ordinal);
    }

    /// <summary>Species in file order; rows are unique and below <see cref="Rows"/>.</summary>
    public IReadOnlyList<SpeciesInfo> Species { get; }

    public SpeciesInfo Get(string id) => _byId.TryGetValue(id, out var info) ? info
        : throw new KeyNotFoundException($"Unknown vegetation species '{id}'.");

    public bool TryGet(string id, out SpeciesInfo? info) => _byId.TryGetValue(id, out info);

    public static SpeciesTable Parse(ReadOnlySpan<byte> json)
    {
        var file = VegetationJson.Read(json, VegetationJson.Context.SpeciesFile, "species.json");
        if (file.Format != "campus.vegetation.species/1") throw new InvalidDataException("species.json: unsupported format.");
        var list = new List<SpeciesInfo>();
        var rows = new HashSet<int>();
        foreach (var (e, index) in file.Species.Select((row, i) => (row, i)))
        {
            var at = $"species[{index}]";
            if (e.Height.Count != 2) throw new InvalidDataException($"species.json: {at}.height needs 2 values.");
            if (e.LeafAmount.Count != 4) throw new InvalidDataException($"species.json: {at}.leafAmount needs 4 values.");
            if (e.BloomAmount.Count != 4) throw new InvalidDataException($"species.json: {at}.bloomAmount needs 4 values.");
            var info = new SpeciesInfo(
                Text(e.Id, $"{at}.id"), Text(e.Name, $"{at}.name"), e.Row, e.Crown, Text(e.Form, $"{at}.form"), e.Height[0], e.Height[1],
                Text(e.Leaf, $"{at}.leaf"), e.Bloom is null ? null : Text(e.Bloom, $"{at}.bloom"), e.Card, e.BloomCard,
                e.Deciduous, e.BloomOnBlobs, e.LeafAmount.ToArray(), e.BloomAmount.ToArray(), e.SwayDeg, Text(e.Sound, $"{at}.sound"));
            if (info.Row is < 0 or >= Rows || !rows.Add(info.Row))
                throw new InvalidDataException($"species.json: '{info.Id}' has an invalid or duplicate row {info.Row}.");
            if (info.HeightMin <= 0f || info.HeightMax < info.HeightMin)
                throw new InvalidDataException($"species.json: '{info.Id}' has an invalid height range.");
            if (info.LeafAmount.Concat(info.BloomAmount).Any(v => !float.IsFinite(v) || v < 0f || v > 1f))
                throw new InvalidDataException($"species.json: '{info.Id}' has an anchor outside [0, 1].");
            if (!TreeForms.Exists(info.Form))
                throw new InvalidDataException($"species.json: '{info.Id}' uses unknown form '{info.Form}'.");
            list.Add(info);
        }
        if (list.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != list.Count)
            throw new InvalidDataException("species.json: duplicate species id.");
        return new SpeciesTable(list.AsReadOnly());
    }

    /// <summary>Packs the RGBA32F texels (row-major, <see cref="Width"/> × <see cref="Rows"/>) for the tree_species texture.</summary>
    public float[] Pack(PaletteCatalog palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        var data = new float[Width * Rows * 4];
        foreach (var s in Species)
        {
            var leaf = palette.Get(s.LeafLit);
            var bloom = s.BloomLit is { } token ? palette.Get(token) : null;
            var flags = (s.Deciduous ? FlagDeciduous : 0) | (s.BloomOnBlobs ? FlagBloomOnBlobs : 0);
            Put(data, s.Row, 0, s.LeafAmount[0], s.LeafAmount[1], s.LeafAmount[2], s.LeafAmount[3]);
            Put(data, s.Row, 1, s.BloomAmount[0], s.BloomAmount[1], s.BloomAmount[2], s.BloomAmount[3]);
            Put(data, s.Row, 2, s.SwayDeg, leaf.Row ?? -1, bloom?.Row ?? -1, bloom is null ? -1 : bloom.Index.Value);
            Put(data, s.Row, 3, (float)s.Card, (float)s.BloomCard, (float)CardShape.Twig, flags);
            Put(data, s.Row, 4, (float)s.Crown, leaf.Index.Value, palette.Resolve(MotherModels.Twig).Value, 0f);
            Put(data, s.Row, 7, 0f, 0f, 0f, Tag);
        }
        return data;
    }

    private static void Put(float[] data, int row, int x, float r, float g, float b, float a)
    {
        var i = (row * Width + x) * 4;
        data[i] = r;
        data[i + 1] = g;
        data[i + 2] = b;
        data[i + 3] = a;
    }

    private static string Text(string value, string at) =>
        value.Length > 0 ? value : throw new InvalidDataException($"species.json: empty '{at}'.");
}

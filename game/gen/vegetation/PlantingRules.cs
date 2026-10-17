using System.Text.Json;

namespace Campus.Gen.Vegetation;

/// <summary>One tree row parallel to a road: its distance from the centre line on both sides; Stagger shifts it half a spacing.</summary>
public sealed record PlantingRow(float Offset, bool Stagger);

/// <summary>The spring band outside the outermost row (WLD-VEGETATION): species alternate along it.</summary>
public sealed record PlantingBand(IReadOnlyList<string> Species, float Offset, float Spacing);

/// <summary>Linear planting along every road of one name (WLD-VEGETATION); species alternate along a row.</summary>
public sealed record RoadPlanting(string Name, IReadOnlyList<string> Species, IReadOnlyList<PlantingRow> Rows, float Spacing, bool Wash,
    PlantingBand? Band);

/// <summary>A named place: within Radius of its plan envelope, groves take Mix instead of the default (WLD-VEGETATION).</summary>
public sealed record PlantingArea(string Name, float Radius, IReadOnlyList<(string Species, float Weight)> Mix);

public sealed record GrovePlanting(int Target, float AreaPerTree, IReadOnlyList<(string Species, float Weight)> Mix,
    IReadOnlyList<PlantingArea> Areas, IReadOnlyList<PlantingArea> WashNear);

/// <summary>Species of the trees planted in empty tree pits: square pits and strips, and round beds (WLD-VEGETATION 栾树: 方树池).</summary>
public sealed record PitPlanting(string Square, string Round);

/// <summary>Ground cover in the tree pits (max-011 18:52: WP-23 plants it): low clipped cushions round the trunk, Height m tall.</summary>
public sealed record PitCover(string Species, float Height, float Spacing);

/// <summary>
/// Shrub borders along the green edges that face a road or a walk (WLD-VEGETATION 绿篱, §5.3 林缘与花境): the edge is
/// sampled every Spacing m, Inset m inside the green; runs of Run m are planted where their hash is below Share; HedgeShare
/// of the planted runs are clipped hedges (modules every Spacing m along the edge), the others loose clumps from Mix.
/// Reach: an edge point counts as facing a road or walk when one lies within this distance.
/// </summary>
public sealed record BorderPlanting(float Spacing, float Inset, float Run, float Share, float HedgeShare, float Reach,
    IReadOnlyList<string> Hedge, IReadOnlyList<(string Species, float Weight)> Mix);

/// <summary>
/// Grass tussocks that break up the lawns (WLD-VEGETATION; the near grass ring of ENG-PERF is the detail version):
/// along the green edges (Edge m deep, one try per Cell m, kept below Share) and round the trunks standing in green (PerTree).
/// </summary>
public sealed record TuftPlanting(string Species, float Cell, float Edge, float Share, int PerTree);

/// <summary>The understorey rules (max-011 23c2b); all optional, a map without them has no understorey.</summary>
public sealed record UnderstoreyRules(PitCover? Pits, BorderPlanting? Borders, TuftPlanting? Tufts)
{
    public static UnderstoreyRules None { get; } = new(null, null, null);
}

/// <summary>
/// One map's planting rules: <c>game/data/maps/&lt;map&gt;/vegetation/planting.json</c>, read through
/// <see cref="VegetationInputs.PlantingKey"/>. Places are found by name in the world model (road names, feature names),
/// so generic code never carries an entity ID. Callers read the bytes; generators never perform IO.
/// </summary>
public sealed record PlantingRules(IReadOnlyList<RoadPlanting> Roads, float OsmNear, string OsmDefault, GrovePlanting Groves)
{
    /// <summary>Species for empty tree pits; without a "pits" entry both are the OSM default.</summary>
    public PitPlanting Pits { get; init; } = new("camphor", "camphor");

    public UnderstoreyRules Understorey { get; init; } = UnderstoreyRules.None;

    public static PlantingRules Parse(ReadOnlySpan<byte> json, SpeciesTable species)
    {
        ArgumentNullException.ThrowIfNull(species);
        using var document = JsonDocument.Parse(json.ToArray());
        var root = document.RootElement;
        if (root.GetProperty("format").GetString() != "campus.vegetation.planting/1")
            throw new InvalidDataException("planting: unsupported format.");
        string Known(string? id) => id is not null && species.TryGet(id, out _) ? id
            : throw new InvalidDataException($"planting: unknown species '{id}'.");
        IReadOnlyList<string> Ids(JsonElement e) => e.EnumerateArray().Select(v => Known(v.GetString())).ToArray();
        IReadOnlyList<(string, float)> Mix(JsonElement e)
        {
            var mix = e.EnumerateObject().Select(p => (Known(p.Name), p.Value.GetSingle())).ToArray();
            if (mix.Length == 0 || mix.Any(m => !(m.Item2 > 0f))) throw new InvalidDataException("planting: a mix needs positive weights.");
            return mix;
        }
        IReadOnlyList<PlantingArea> Areas(JsonElement e, bool withMix) => e.EnumerateArray().Select(a => new PlantingArea(
            a.GetProperty("name").GetString() ?? throw new InvalidDataException("planting: unnamed area."),
            a.GetProperty("radius").GetSingle(), withMix ? Mix(a.GetProperty("mix")) : [])).ToArray();

        var roads = root.GetProperty("roads").EnumerateArray().Select(r => new RoadPlanting(
            r.GetProperty("name").GetString() ?? throw new InvalidDataException("planting: unnamed road."),
            Ids(r.GetProperty("species")),
            r.GetProperty("rows").EnumerateArray().Select(row => new PlantingRow(row.GetProperty("offset").GetSingle(),
                row.TryGetProperty("stagger", out var s) && s.GetBoolean())).ToArray(),
            r.GetProperty("spacing").GetSingle(),
            r.GetProperty("wash").GetBoolean(),
            r.TryGetProperty("band", out var band)
                ? new PlantingBand(Ids(band.GetProperty("species")), band.GetProperty("offset").GetSingle(), band.GetProperty("spacing").GetSingle())
                : null)).ToArray();
        foreach (var road in roads)
            if (road.Species.Count == 0 || road.Rows.Count == 0 || !(road.Spacing > 0.5f) || road.Band is { Spacing: <= 0.5f })
                throw new InvalidDataException($"planting: road '{road.Name}' needs species, rows and a spacing.");
        var osm = root.GetProperty("osmTrees");
        var groves = root.GetProperty("groves");
        var osmDefault = Known(osm.GetProperty("default").GetString());
        var pits = root.TryGetProperty("pits", out var p)
            ? new PitPlanting(Known(p.GetProperty("square").GetString()), Known(p.GetProperty("round").GetString()))
            : new PitPlanting(osmDefault, osmDefault);
        var under = UnderstoreyRules.None;
        if (root.TryGetProperty("understorey", out var u))
        {
            float Positive(JsonElement e, string name) => e.GetProperty(name).GetSingle() is var v && v > 0f ? v
                : throw new InvalidDataException($"planting: understorey '{name}' must be positive.");
            float Share(JsonElement e, string name) => e.GetProperty(name).GetSingle() is var v && v is >= 0f and <= 1f ? v
                : throw new InvalidDataException($"planting: understorey '{name}' must lie in [0, 1].");
            under = new UnderstoreyRules(
                u.TryGetProperty("pitCover", out var c)
                    ? new PitCover(Known(c.GetProperty("species").GetString()), Positive(c, "height"), Positive(c, "spacing")) : null,
                u.TryGetProperty("borders", out var b)
                    ? new BorderPlanting(Positive(b, "spacing"), Positive(b, "inset"), Positive(b, "run"), Share(b, "share"), Share(b, "hedgeShare"),
                        Positive(b, "reach"), Ids(b.GetProperty("hedge")), Mix(b.GetProperty("mix"))) : null,
                u.TryGetProperty("tufts", out var t)
                    ? new TuftPlanting(Known(t.GetProperty("species").GetString()), Positive(t, "cell"), Positive(t, "edge"), Share(t, "share"),
                        t.GetProperty("perTree").GetInt32()) : null);
            if (under.Borders is { Hedge.Count: 0 }) throw new InvalidDataException("planting: border hedges need a species.");
        }
        return new PlantingRules(roads, osm.GetProperty("near").GetSingle(), osmDefault,
            new GrovePlanting(groves.GetProperty("target").GetInt32(), groves.GetProperty("areaPerTree").GetSingle(),
                Mix(groves.GetProperty("mix")), Areas(groves.GetProperty("areas"), true), Areas(groves.GetProperty("washNear"), false)))
        { Pits = pits, Understorey = under };
    }
}

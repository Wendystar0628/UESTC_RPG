using System.Text.Json;

namespace Campus.Gen.Vegetation;

/// <summary>One tree row parallel to a road: its distance from the centre line on both sides; Stagger shifts it half a spacing.</summary>
public sealed record PlantingRow(float Offset, bool Stagger);

/// <summary>The spring band outside the outermost row (WLD-VEGETATION §3): species alternate along it.</summary>
public sealed record PlantingBand(IReadOnlyList<string> Species, float Offset, float Spacing);

/// <summary>Linear planting along every road of one name (WLD-VEGETATION §3); species alternate along a row.</summary>
public sealed record RoadPlanting(string Name, IReadOnlyList<string> Species, IReadOnlyList<PlantingRow> Rows, float Spacing, bool Wash,
    PlantingBand? Band);

/// <summary>A named place: within Radius of its plan envelope, groves take Mix instead of the default (WLD-VEGETATION §4).</summary>
public sealed record PlantingArea(string Name, float Radius, IReadOnlyList<(string Species, float Weight)> Mix);

public sealed record GrovePlanting(int Target, float AreaPerTree, IReadOnlyList<(string Species, float Weight)> Mix,
    IReadOnlyList<PlantingArea> Areas, IReadOnlyList<PlantingArea> WashNear);

/// <summary>
/// One map's planting rules: <c>game/data/maps/&lt;map&gt;/vegetation/planting.json</c>, read through
/// <see cref="VegetationInputs.PlantingKey"/>. Places are found by name in the world model (road names, feature names),
/// so generic code never carries an entity ID. Callers read the bytes; generators never perform IO.
/// </summary>
public sealed record PlantingRules(IReadOnlyList<RoadPlanting> Roads, float OsmNear, string OsmDefault, GrovePlanting Groves)
{
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
        return new PlantingRules(roads, osm.GetProperty("near").GetSingle(), Known(osm.GetProperty("default").GetString()),
            new GrovePlanting(groves.GetProperty("target").GetInt32(), groves.GetProperty("areaPerTree").GetSingle(),
                Mix(groves.GetProperty("mix")), Areas(groves.GetProperty("areas"), true), Areas(groves.GetProperty("washNear"), false)));
    }
}

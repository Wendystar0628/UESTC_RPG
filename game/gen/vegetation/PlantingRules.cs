using Campus.Gen.World;

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

public sealed record GrovePlanting(IReadOnlyList<(string Species, float Weight)> Mix,
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
        var file = PlantingJson.Read(json);
        if (file.Format != "campus.vegetation.planting/1") throw new InvalidDataException("planting: unsupported format.");
        string Known(string id, string at) => species.TryGet(id, out _) ? id
            : throw new InvalidDataException($"planting: {at}: unknown species '{id}'.");
        IReadOnlyList<string> Ids(IReadOnlyList<string> ids, string at) => ids.Select((id, i) => Known(id, $"{at}[{i}]")).ToArray();
        IReadOnlyList<(string, float)> Mix(Dictionary<string, float> mix, string at)
        {
            if (mix.Count == 0 || mix.Values.Any(w => !(w > 0f))) throw new InvalidDataException($"planting: {at}: a mix needs positive weights.");
            return mix.Select(m => (Known(m.Key, $"{at}.{m.Key}"), m.Value)).ToArray();
        }
        float Positive(float v, string at) => v > 0f ? v : throw new InvalidDataException($"planting: {at} must be positive.");
        float Share(float v, string at) => v is >= 0f and <= 1f ? v : throw new InvalidDataException($"planting: {at} must lie in [0, 1].");

        var roads = file.Roads.Select((r, i) => new RoadPlanting(r.Name, Ids(r.Species, $"roads[{i}].species"),
            r.Rows.Select(row => new PlantingRow(row.Offset, row.Stagger)).ToArray(), r.Spacing, r.Wash,
            r.Band is { } band ? new PlantingBand(Ids(band.Species, $"roads[{i}].band.species"), band.Offset, band.Spacing) : null)).ToArray();
        foreach (var road in roads)
            if (road.Species.Count == 0 || road.Rows.Count == 0 || !(road.Spacing > 0.5f) || road.Band is { Spacing: <= 0.5f })
                throw new InvalidDataException($"planting: road '{road.Name}' needs species, rows and a spacing.");
        var osmDefault = Known(file.OsmTrees.Default, "osmTrees.default");
        var pits = file.Pits is { } p
            ? new PitPlanting(Known(p.Square, "pits.square"), Known(p.Round, "pits.round"))
            : new PitPlanting(osmDefault, osmDefault);
        var under = UnderstoreyRules.None;
        if (file.Understorey is { } u)
        {
            under = new UnderstoreyRules(
                u.PitCover is { } c
                    ? new PitCover(Known(c.Species, "understorey.pitCover.species"), Positive(c.Height, "understorey.pitCover.height"),
                        Positive(c.Spacing, "understorey.pitCover.spacing")) : null,
                u.Borders is { } b
                    ? new BorderPlanting(Positive(b.Spacing, "understorey.borders.spacing"), Positive(b.Inset, "understorey.borders.inset"),
                        Positive(b.Run, "understorey.borders.run"), Share(b.Share, "understorey.borders.share"),
                        Share(b.HedgeShare, "understorey.borders.hedgeShare"), Positive(b.Reach, "understorey.borders.reach"),
                        Ids(b.Hedge, "understorey.borders.hedge"), Mix(b.Mix, "understorey.borders.mix")) : null,
                u.Tufts is { } t
                    ? new TuftPlanting(Known(t.Species, "understorey.tufts.species"), Positive(t.Cell, "understorey.tufts.cell"),
                        Positive(t.Edge, "understorey.tufts.edge"), Share(t.Share, "understorey.tufts.share"), t.PerTree) : null);
            if (under.Borders is { Hedge.Count: 0 }) throw new InvalidDataException("planting: border hedges need a species.");
        }
        var groves = file.Groves;
        return new PlantingRules(roads, file.OsmTrees.Near, osmDefault,
            new GrovePlanting(Mix(groves.Mix, "groves.mix"),
                groves.Areas.Select((a, i) => new PlantingArea(a.Name, a.Radius, Mix(a.Mix, $"groves.areas[{i}].mix"))).ToArray(),
                groves.WashNear.Select(a => new PlantingArea(a.Name, a.Radius, [])).ToArray()))
        { Pits = pits, Understorey = under };
    }
}

using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public enum FurnitureKind : byte { StreetLamp, Bench, Bin, BusStop, AtlasKiosk, AtlasPost, EntranceMat, OpenSign, EntranceLamp, ParkedCar, KerbRamp, Signpost, DrinkingFountain, VendingMachine, DeliveryLocker, BicycleBay, ParkedBicycle, Barrier }

/// <summary>
/// One placed piece of street furniture: the catalogue part with its world matrix, its lamp-inventory markers, its world-space
/// colliders (the part's own, plus its pad's) and the pad it brings, if any (a rest stop's bay, a kiosk's apron, a plinth).
/// </summary>
public sealed record FurnitureItem(string Key, FurnitureKind Kind, PlacedPart Part)
{
    public IReadOnlyList<LightEmitter> Emitters { get; init; } = [];
    public IReadOnlyList<Collider> Colliders { get; init; } = [];
    public Pad? Pad { get; init; }
    public Vector2 Anchor => new(Part.At.M41, Part.At.M43);
}

/// <summary>All street furniture of one map, in a stable order (anchors, entrances, stops, then roads in rule order).</summary>
public sealed record FurnitureLayout(IReadOnlyList<FurnitureItem> Items)
{
    /// <summary>Deterministic placement and planting diagnostics, returned through ContentOutput.Diagnostics.</summary>
    public IReadOnlyList<BuildDiagnostic> Diagnostics { get; init; } = [];

    public int Count(FurnitureKind kind) => Items.Count(i => i.Kind == kind);
}

/// <summary>
/// WP-13 placement (13b): pure and deterministic from the world model and the map's claims.
/// <list type="bullet">
/// <item>Atlas anchors (SYS-ATLAS): a kiosk or a post on every <c>anchor</c> feature of WP-48c's <c>overlay/atlas.d/</c>,
/// at its footprint, facing its <c>yawDeg</c>; the kiosk on its pad (or the deck the overlay names), the post landed as a point.
/// A footprint more than <see cref="FurnitureStyle.AnchorPavingGap"/> off pedestrian paving moves the shortest way to its edge.</item>
/// <item>Open entrances (WLD-INTERIORS): mat, standing open sign and door lamp at every open entrance of the interior index,
/// on the entrance floor, facing out; skipped where another package claims the building's <see cref="Aspects.Props"/>.</item>
/// <item>Shuttle stops (WLD-LIFE): a stop sign at the kerb of the carriageway nearest each <c>bus</c> feature.</item>
/// <item>Roads (WLD-LIFE, MAP-QSH-ADDITIONS): lamps along every lit road of the network, by its class (<see cref="FurnitureStyle.Rule"/>:
/// the avenue, trunk roads, service lanes, pedestrian streets) at the class's spacing,
/// and rest stops midway between lamps: a bench lining the nearest walk on that side, facing the walk on a bay joined to it,
/// never facing a carriageway or in the strip between the kerb and the tree rows; at every other stop a four-stream bin on its
/// own bay 2–4 m along the same walk, towards a junction where there is one (Opus review 12:15).</item>
/// <item>Barriers (WLD-LIFE-STREET §2): a row of white stone spheres across each wide walk's mouth at a carriageway (<see cref="FurnitureStyle.BarrierWalkMin"/>).
/// The south gate's own forecourt spheres and bollards belong to its zone site.</item>
/// </list>
/// Everything lands on <see cref="IWorldModel.Terrain"/> (<see cref="Land"/>) and keeps clear of buildings, water, courts,
/// parking, other roads, bridges, trees and each other (<see cref="StreetSite"/>).
/// </summary>
public static partial class FurniturePlanner
{
    private static readonly StreetLampPart Lamp = new();
    private static readonly BenchPart BenchDef = new();
    private static readonly BinFourPart BinDef = new();
    private static readonly BusStopPart StopDef = new();
    private static readonly AtlasKioskPart KioskDef = new();
    private static readonly AtlasPostPart PostDef = new();
    private static readonly OpenSignPart SignDef = new();
    private static readonly ParkedCarPart CarDef = new();
    private static readonly KerbRampPart RampDef = new();
    private static readonly SignpostPart SignpostDef = new();
    private static readonly DrinkingFountainPart FountainDef = new();
    private static readonly WpId Self = new("WP-13");

    /// <summary>Pole footprint radius used for clearances and landing (the lamp's base flange).</summary>
    private const float PoleRadius = 0.25f;

    /// <summary>Half the stop board's thickness plus its clamp, for its clearance capsule.</summary>
    private const float StopHalfDepth = 0.15f;

    /// <param name="world">The world model.</param>
    /// <param name="claimedBy">The claims query (<see cref="BuildContext.ClaimedBy"/>); none means nothing is claimed.</param>
    /// <param name="inputs">The map's preloaded source inputs (<see cref="BuildContext.SourceInputs"/>): the tree rows come from its <see cref="S.PlantingKey"/>.</param>
    public static FurnitureLayout Build(BuildInputSnapshot globalInputs, PaletteCatalog catalog, IWorldModel world,
        Func<FeatureId, Aspects, WpId?>? claimedBy = null, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        claimedBy ??= (_, _) => null;
        var atlas = new SceneTextAtlas(globalInputs.Require(SceneTextInputs.AtlasKey));
        var openSign = S.OpenSign with { PrintedText = atlas.Layout(StreetStyle.OpenTitle), PrintedTextBaseColor = catalog.Resolve(StreetStyle.Lit(StreetStyle.SignGreen)) };
        var stop = S.Stop with { PrintedText = atlas.Layout(StreetStyle.StopTitle), PrintedTextBaseColor = catalog.Resolve(StreetStyle.Lit(StreetStyle.SignBlue)) };
        var fountain = S.Fountain with { PrintedText = atlas.Layout(StreetStyle.FountainTitle), PrintedTextBaseColor = catalog.Resolve(StreetStyle.Lit(StreetStyle.PaintWhite)) };
        var (rows, plantingDiagnostics) = PlantingRows.Resolve(inputs);
        var site = new StreetSite(world);
        foreach (var road in rows) site.AddRows(world.Network.Roads.Where(l => StreetSite.RoadName(l) == road.Name), road.Offsets);
        var roads = RoadGroups(world, rows);
        var items = new List<FurnitureItem>();
        var diagnostics = new List<BuildDiagnostic>(plantingDiagnostics);
        Anchors(site, items, diagnostics);
        Entrances(site, items, claimedBy, openSign);
        Stops(site, items, stop);
        foreach (var road in roads) Road(site, items, diagnostics, road);
        foreach (var road in roads.Where(r => r.KerbRamps)) KerbRamps(site, items, diagnostics, road);
        Cars(site, items);
        Fountains(site, items, diagnostics, fountain);
        Signposts(site, items, diagnostics);
        Services(site, items, diagnostics, roads);
        Barriers(site, items, diagnostics);
        return new(items.AsReadOnly()) { Diagnostics = diagnostics.AsReadOnly() };
    }
}

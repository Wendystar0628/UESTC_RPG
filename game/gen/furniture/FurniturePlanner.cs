using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

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
/// <item>Barriers (WLD-LIFE-STREET): a row of white stone spheres across each wide walk's mouth at a carriageway (<see cref="FurnitureStyle.BarrierWalkMin"/>).
/// The south gate's own forecourt spheres and bollards belong to its zone site.</item>
/// </list>
/// Everything lands on <see cref="IWorldModel.Terrain"/> (<see cref="Land"/>) and keeps clear of buildings, water, courts,
/// parking, other roads, bridges, trees and each other (<see cref="StreetSite"/>).
/// </summary>
public static class FurniturePlanner
{
    /// <param name="world">The world model.</param>
    /// <param name="claimedBy">The claims query (<see cref="BuildContext.ClaimedBy"/>); none means nothing is claimed.</param>
    /// <param name="inputs">The map's preloaded source inputs (<see cref="BuildContext.SourceInputs"/>): the tree rows come from its <see cref="S.PlantingKey"/>.</param>
    public static FurnitureLayout Build(BuildInputSnapshot globalInputs, PaletteCatalog catalog, IWorldModel world,
        Func<FeatureId, Aspects, string?>? claimedBy = null, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        claimedBy ??= (_, _) => null;
        var atlas = new SceneTextAtlas(globalInputs.Require(SceneTextInputs.AtlasKey));
        var openSign = S.OpenSign with { PrintedText = atlas.Layout(StreetStyle.OpenTitle), PrintedTextBaseColor = catalog.Resolve(StreetStyle.Lit(StreetStyle.SignGreen)) };
        var stop = S.Stop with { PrintedText = atlas.Layout(StreetStyle.StopTitle), PrintedTextBaseColor = catalog.Resolve(StreetStyle.Lit(StreetStyle.SignBlue)) };
        var fountain = S.Fountain with { PrintedText = atlas.Layout(StreetStyle.FountainTitle), PrintedTextBaseColor = catalog.Resolve(StreetStyle.Lit(StreetStyle.PaintWhite)) };
        var (rows, plantingDiagnostics) = PlantingRows.Resolve(inputs);
        var site = new StreetSite(world);
        foreach (var road in rows) site.AddRows(world.Network.Roads.Where(l => StreetNetwork.RoadName(l) == road.Name), road.Offsets);
        var roads = FurnitureRoads.Groups(world, rows);
        var items = new List<FurnitureItem>();
        var diagnostics = new List<BuildDiagnostic>(plantingDiagnostics);
        FurnitureAnchors.Plan(site, items, diagnostics);
        FurnitureEntrances.Plan(site, items, claimedBy, openSign);
        FurnitureStops.Plan(site, items, stop);
        foreach (var road in roads) FurnitureRoads.Plan(site, items, diagnostics, road);
        foreach (var road in roads.Where(r => r.KerbRamps)) FurnitureKerbRamps.Plan(site, items, diagnostics, road);
        FurnitureCars.Plan(site, items);
        FurnitureWayfinding.Fountains(site, items, diagnostics, fountain);
        FurnitureWayfinding.Signposts(site, items, diagnostics);
        FurnitureServices.Plan(site, items, diagnostics, roads);
        FurnitureBarriers.Plan(site, items, diagnostics);
        return new(items.AsReadOnly()) { Diagnostics = diagnostics.AsReadOnly() };
    }

    /// <summary>
    /// Rigid frame whose local −Z (every part's front) points along <paramref name="facing"/> and whose origin is (p.x, y, p.z).
    /// Rows are the images of local X, Y, Z (System.Numerics row vectors).
    /// </summary>
    public static Matrix4x4 Frame(Vector2 p, Vector2 facing, float y)
    {
        var f = Vector2.Normalize(facing);
        return new(-f.Y, 0, f.X, 0, 0, 1, 0, 0, -f.X, 0, -f.Y, 0, p.X, y, p.Y, 1);
    }

    /// <summary>Moves a part-local collider by a rigid placement matrix.</summary>
    public static Collider Placed(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = box.At.Then(at) },
        PostCollider post => post with
        {
            C = new(post.C.X * at.M11 + post.C.Y * at.M31 + at.M41, post.C.X * at.M13 + post.C.Y * at.M33 + at.M43),
            Y0 = post.Y0 + at.M42,
            Y1 = post.Y1 + at.M42,
        },
        RampCollider ramp => ramp with { P0 = Xf.Point(at, ramp.P0), P1 = Xf.Point(at, ramp.P1), P2 = Xf.Point(at, ramp.P2), P3 = Xf.Point(at, ramp.P3) },
        _ => throw new ArgumentException("Unsupported collider kind for a placed street part.", nameof(collider)),
    };
}

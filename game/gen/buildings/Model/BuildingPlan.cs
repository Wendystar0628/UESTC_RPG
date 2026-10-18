using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Model;

public enum BayKind : byte { Window, Stair, Toilet, Corridor, Door, Blank }

/// <summary>One window or door opening; U is measured along the skin edge (wall plane at s = 0), Y absolute.</summary>
public readonly record struct Opening(int Bay, int Row, float U0, float U1, float Y0, float Y1, BayKind Kind);

/// <summary>
/// One entrance door: the Door opening at (Bay, Row) of Openings[Facade]. Anchor is the interior index's EntranceDecl it
/// was opened for (WLD-INTERIORS), null for the generic door. With Landing, WP-14 builds the WP-11 landing at the door
/// floor, the connection down to GroundY in front of it (WLD-TERRAIN) and the canopy at CanopyY; without, the
/// outside landing is a deck or porch of another package and the door stands alone.
/// </summary>
public sealed record PlanEntrance(int Facade, int Bay, int Row, string? Anchor, bool Landing, float GroundY, float CanopyY);

/// <summary>
/// The bays of one footprint edge, read from the WP-51a facade grid (FacadeGridGenerator). Bay indices are contiguous
/// over all rings (ColumnStart + bay = FacadeBay.Index) and are the columns of the building-wide window grid.
/// Bounds are the bay boundaries in footprint u (glass plane); Widths the kernel's opening widths (0 when blank).
/// SkinShift converts footprint u to skin u (wall plane): uSkin = uFoot - SkinShift.
/// </summary>
public sealed record FacadeLayout(int Ring, int Edge, int Bays, float BayWidth, float Width, int ColumnStart, BayKind[] Kinds,
    float SkinShift, float[] Bounds, float[] Widths)
{
    public float BayWidthAt(int bay) => Bounds[bay + 1] - Bounds[bay];
    public float CentreFoot(int bay) => (Bounds[bay] + Bounds[bay + 1]) * 0.5f;
    public float CentreSkin(int bay) => CentreFoot(bay) - SkinShift;
    /// <summary>Boundary m (0..Bays) in skin u.</summary>
    public float BoundarySkin(int m) => Bounds[m] - SkinShift;
}

/// <summary>Everything about one building (one source part) that does not depend on the detail level.</summary>
public sealed class BuildingPlan
{
    public const float WallChamfer = 0.035f;

    public required FeatureId Id { get; init; }
    /// <summary>Stable key of this part inside the entity (the entity ID itself when it has a single footprint).</summary>
    public required string Key { get; init; }
    public required WorldFeature Feature { get; init; }
    /// <summary>Footprint rings (canonical, mm): the glass plane, shared with the WP-51a plan kernel.</summary>
    public required PlanRing Outer { get; init; }
    public required IReadOnlyList<PlanRing> Holes { get; init; }
    /// <summary>Wall skin rings: footprint offset outward by Reveal (same vertex count and edge order).</summary>
    public required PlanRing SkinOuter { get; init; }
    public required IReadOnlyList<PlanRing> SkinHoles { get; init; }
    /// <summary>Depth from the wall plane to the glass plane.</summary>
    public required float Reveal { get; init; }
    public required Vector2 Centroid { get; init; }
    /// <summary>The WP-51a facade grid this plan's bays and window columns come from.</summary>
    public required Campus.Gen.Interior.FacadeGrid Grid { get; init; }
    public required float Area { get; init; }
    public required BuildingStyle Style { get; init; }
    public required FacadeProfile Profile { get; init; }
    public required uint Seed { get; init; }
    public required bool Small { get; init; }
    /// <summary>The pad height everything is measured from (D-180): walls start 0.3 m below it.</summary>
    public required float BaseY { get; init; }
    public required float Height { get; init; }
    public required int Storeys { get; init; }
    public required float StoreyHeight { get; init; }
    /// <summary>Window sill and head heights per storey (absolute Y).</summary>
    public required float[] SillY { get; init; }
    public required float[] HeadY { get; init; }
    public required IReadOnlyList<FacadeLayout> Facades { get; init; }
    /// <summary>Openings per facade (same order as Facades), in skin u.</summary>
    public required IReadOnlyList<Opening[]> Openings { get; init; }
    /// <summary>
    /// Precision zone of each opening (parallel to Openings), read from the exported fidelity data (IFidelityModel):
    /// the ground-floor row takes its facade segment's ground band. Empty without fidelity data: everything is K3.
    /// </summary>
    public IReadOnlyList<FidelityZone[]> OpeningZones { get; init; } = [];
    /// <summary>The exported facade segments of this building (IFidelityModel.Facades), for the zone of a wall panel; empty without fidelity data.</summary>
    public IReadOnlyList<FacadeSegment> FacadeSegments { get; init; } = [];
    /// <summary>Precision zone of the roof from the fidelity data (IFidelityModel.Roof); without data the default K4 (a).</summary>
    public FidelityZone RoofZone { get; init; } = FidelityZone.K4;
    /// <summary>Columns of the building window grid (all bays of all rings, blank ones included).</summary>
    public required int TotalColumns { get; init; }
    public required Vector2 Axis { get; init; }
    /// <summary>The entrance doors: one at each interior-index anchor the building takes, else the generic door (or none).</summary>
    public required IReadOnlyList<PlanEntrance> Entrances { get; init; }

    /// <summary>The door opening of an entrance.</summary>
    public Opening DoorOf(PlanEntrance e) => Openings[e.Facade].First(o => o.Kind == BayKind.Door && o.Bay == e.Bay && o.Row == e.Row);

    /// <summary>Index of an entrance's door in Openings[e.Facade] (for its precision zone).</summary>
    public int DoorIndex(PlanEntrance e) => Array.FindIndex(Openings[e.Facade], o => o.Kind == BayKind.Door && o.Bay == e.Bay && o.Row == e.Row);

    public TileKey Tile(LodLevel lod) => TileGrid.At(Centroid, lod);

    public FidelityZone ZoneOf(int facade, int opening) =>
        facade < OpeningZones.Count ? OpeningZones[facade][opening] : FidelityZone.K3;

    /// <summary>Precision zone of the wall at skin u of a facade (the segment's ground band for the ground-floor row); no segment: K3.</summary>
    public FidelityZone WallZoneAt(int facade, float uSkin, bool groundRow)
    {
        var f = Facades[facade];
        var segment = FacadeSegments.Count == 0 ? null : PlanFacades.SegmentAt(FacadeSegments, FootAt(f.Ring), f, uSkin);
        return segment is null ? FidelityZone.K3 : groundRow ? segment.GroundBand : segment.Zone;
    }

    public PlanRing SkinAt(int ring) => ring == 0 ? SkinOuter : SkinHoles[ring - 1];
    public PlanRing FootAt(int ring) => ring == 0 ? Outer : Holes[ring - 1];

    public IEnumerable<PlanRing> SkinRings
    {
        get
        {
            yield return SkinOuter;
            foreach (var h in SkinHoles) yield return h;
        }
    }

    public int RingCount => 1 + Holes.Count;

    public float RoofY => BaseY + Height;
    public float SunkY => BaseY - FacadeRules.WallBelowPad;
    public float PlinthTopY => BaseY + FacadeRules.PlinthTop;
    /// <summary>Top of the ground-floor row of the wall (the first floor level; a single-storey building: the wall top).</summary>
    public float GroundRowTopY => Storeys > 1 ? FloorY(1) : WallTopY;
    /// <summary>Floor level of storey k; storey 0 is the ground floor at pad + 0.15 m.</summary>
    public float FloorY(int k) => BaseY + FacadeRules.FloorAbovePad + k * StoreyHeight;
    public float CorniceDrop => Small ? 0.3f : 0.5f;
    public float ParapetRise => Small ? 0.45f : 0.8f;

    /// <summary>Top of the plain wall: the cornice soffit, or the parapet top below the coping.</summary>
    public float WallTopY => Profile.RoofEdge switch
    {
        RoofEdge.Cornice => RoofY - CorniceDrop,
        RoofEdge.Parapet => RoofY + ParapetRise,
        _ => throw new InvalidOperationException("Unknown roof edge."),
    };

    /// <summary>Highest point of the roof edge (coping top).</summary>
    public float EdgeTopY => Profile.RoofEdge switch
    {
        RoofEdge.Cornice => RoofY + (Small ? 0.55f : 0.97f),
        RoofEdge.Parapet => RoofY + ParapetRise + 0.15f,
        _ => throw new InvalidOperationException("Unknown roof edge."),
    };

    /// <summary>Global horizontal break lines of the wall grid, shared by every facade (no T-junctions at corners).</summary>
    public float[] WallRows()
    {
        var ys = new List<float> { PlinthTopY, WallTopY };
        for (var k = 1; k < Storeys; k++) ys.Add(FloorY(k));
        for (var k = 0; k < Storeys; k++) { ys.Add(SillY[k]); ys.Add(HeadY[k]); }
        foreach (var e in Entrances)
        {
            var door = DoorOf(e);
            ys.Add(door.Y0);
            ys.Add(door.Y1);
        }
        if (Profile.RoofEdge == RoofEdge.Parapet) ys.Add(RoofY);
        ys.RemoveAll(y => y < PlinthTopY - 1e-4f || y > WallTopY + 1e-4f);
        ys.Sort();
        var result = new List<float>(ys.Count);
        foreach (var y in ys)
            if (result.Count == 0 || y - result[^1] > 0.02f) result.Add(y);
        if (result[^1] < WallTopY - 1e-4f) result.Add(WallTopY);
        result[^1] = WallTopY;
        return [.. result];
    }

    /// <summary>Card type of one window (WLD-INTERIORS): skeleton columns first, then the family mix by window hash.</summary>
    public WindowCard CardOf(BayKind kind, uint windowHash) => kind switch
    {
        BayKind.Stair => WindowCard.Stair,
        BayKind.Toilet => WindowCard.Toilet,
        BayKind.Corridor => WindowCard.Corridor,
        BayKind.Door => WindowCard.Corridor,
        BayKind.Window => Mix(windowHash),
        BayKind.Blank => WindowCard.None,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown bay kind."),
    };

    private WindowCard Mix(uint hash)
    {
        var mix = Profile.CardMix;
        if (mix.Length == 0) return Profile.Card;
        var total = 0;
        foreach (var (_, w) in mix) total += w;
        var pick = (int)(hash % (uint)total);
        foreach (var (card, w) in mix)
        {
            if (pick < w) return card;
            pick -= w;
        }
        return Profile.Card;
    }
}

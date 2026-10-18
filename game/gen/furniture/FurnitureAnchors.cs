using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public static partial class FurniturePlanner
{
    // ---------------------------------------------------------------------------------------------------------
    // Atlas anchors: kiosks and posts on WP-48c's overlay features.
    // ---------------------------------------------------------------------------------------------------------

    private static void Anchors(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring }) continue;
            var form = Text(feature.Props, "form");
            if (form is not ("kiosk" or "post")) continue;
            var yaw = Number(feature.Props, "yawDeg") ?? 0f;
            var facing = Facing(yaw);
            var right = new Vector2(-facing.Y, facing.X);
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            var width = Extent(ring, right);
            var depth = Extent(ring, facing);
            var key = "anchor:" + feature.Id.Value;
            // Fallback while anchor data is under review: nothing stands on a carriageway, in water or in a building (A01 and A05
            // wait for the gate plazas of 18c3 and WP-30).
            var landing = site.LandingAt(centre);
            if (landing != Landing.Open)
            {
                diagnostics.Add(new(DiagnosticLevel.Warning, "wp13.anchor.not-placed", $"anchor {feature.Id.Value} ({form}) not placed: its point lands {Where(landing)}", feature.Id));
                continue;
            }
            switch (form)
            {
                case "kiosk":
                    {
                        var p = S.Kiosk with { Width = width >= 1.4f ? width : S.Kiosk.Width, Depth = depth >= 0.4f ? depth : S.Kiosk.Depth, Height = feature.Height is float h && h >= 2.2f ? h : S.Kiosk.Height };
                        // Large entity (WLD-TERRAIN): on its pad, with a paved apron; a deck the height field does not carry (A06)
                        // comes from the overlay's groundY. An apron off the paving moves to its edge (a deck is paving already).
                        var ground = terrain.HeightAt(centre.X, centre.Y);
                        var deck = Number(feature.Props, "groundY") is { } y && y > ground + S.DeckAbove ? y : (float?)null;
                        if (deck is null) (centre, facing) = KioskOnPaving(site, centre, facing, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, diagnostics, feature.Id);
                        var level = deck ?? terrain.PadY(feature.Id);
                        var plan = Frame(centre, facing, 0f);
                        var apron = Land.Pad(terrain, plan, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, level)!;
                        var at = Frame(centre, facing, apron.Top);
                        var kiosk = Item(key, FurnitureKind.AtlasKiosk, PartCatalog.AtlasKiosk, p, at, KioskDef.Collision(p), [AtlasKioskPart.Emitter(p, at)]);
                        items.Add(kiosk with { Pad = apron, Colliders = [.. kiosk.Colliders, apron.Collider()] });
                        site.Occupy(centre, MathF.Max(p.Width, p.Depth) * 0.5f + S.ApronMargin);
                        break;
                    }
                case "post":
                    {
                        var p = S.Post with { Width = width >= 0.16f ? width : S.Post.Width, Depth = depth >= 0.16f ? depth : S.Post.Depth, Height = feature.Height is float h && h >= 0.6f ? h : S.Post.Height };
                        var radius = MathF.Max(p.Width, p.Depth) * 0.5f;
                        centre = ToPaving(site, centre, facing, radius, radius, diagnostics, feature.Id, form);
                        if (Land.Point(terrain, centre, radius) is not { } y) continue;
                        var at = Frame(centre, facing, y);
                        items.Add(Item(key, FurnitureKind.AtlasPost, PartCatalog.AtlasPost, p, at, PostDef.Collision(p), [AtlasPostPart.Emitter(p, at)]));
                        site.Occupy(centre, radius);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// A kiosk's apron (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/> along <paramref name="facing"/>) that is not whole
    /// on pedestrian paving yet (Opus review 14:20, 16:36, 17:28): it stands whole on the nearest paving (for one partly on it, the edge its
    /// outline crosses farthest), its back edge <see cref="FurnitureStyle.ApronPavingClear"/>
    /// inside the paving edge and parallel to it, its face towards the paving, where people stand: the data's yaw gives way. It slides
    /// along the edge, in steps up to <see cref="FurnitureStyle.ApronSlide"/>, until the whole apron is on paving; where none fits (or
    /// no paving lies within <see cref="FurnitureStyle.AnchorPavingReach"/>) it only moves to touch it, as <see cref="ToPaving"/> does, facing
    /// its data yaw. The new centre and yaw are logged for the anchor data (WP-48c), which takes them over; an apron whole on paving
    /// already stays as the data has it.
    /// </summary>
    private static (Vector2 Centre, Vector2 Facing) KioskOnPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<BuildDiagnostic> diagnostics, FeatureId id)
    {
        // An apron whole on paving stays; one that only partly is (13c2a: A03, about half its outline off) turns onto the edge
        // its outline crosses, as one wholly off does onto the nearest edge (Opus review 17:28).
        if (OnPaving(site, centre, facing, halfWidth, halfDepth)) return (centre, facing);
        if ((site.PavingEdgeFacing(centre) is { Gap: > 0f } off ? off : FarthestOff(site, centre, facing, halfWidth, halfDepth)) is { } hit && hit.Gap <= S.AnchorPavingReach)
        {
            var inward = hit.Inward;
            var along = new Vector2(-inward.Y, inward.X);
            foreach (var slide in Shifts(S.ApronSlide))
            {
                var at = hit.Edge + along * slide + inward * (S.ApronPavingClear + halfDepth);
                if (Vector2.Distance(at, centre) > S.AnchorPavingReach || !OnPaving(site, at, inward, halfWidth, halfDepth)) continue;
                diagnostics.Add(new(DiagnosticLevel.Info, "wp13.anchor.turned", $"anchor {id.Value} (kiosk) turned onto paving: yawDeg {Fixed(YawDeg(inward))} (data {Fixed(YawDeg(facing))}), centre ({Fixed(at.X)}, {Fixed(at.Y)}) (data ({Fixed(centre.X)}, {Fixed(centre.Y)}))", id));
                return (at, inward);
            }
        }
        return (ToPaving(site, centre, facing, halfWidth, halfDepth, diagnostics, id, "kiosk"), facing);
    }

    /// <summary>
    /// For a rectangle partly on paving: the paving edge seen from the outline point farthest off it (every <see cref="FurnitureStyle.ApronSample"/>),
    /// with its gap measured from the centre's side, so the apron can turn its back onto that edge. Null when no outline point is off.
    /// </summary>
    private static (float Gap, Vector2 Edge, Vector2 Inward, NetworkLine? Road)? FarthestOff(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth)
    {
        var right = new Vector2(-facing.Y, facing.X);
        (float Gap, Vector2 Edge, Vector2 Inward, NetworkLine? Road)? far = null;
        for (var u = -halfWidth; u <= halfWidth + 1e-3f; u += S.ApronSample)
            for (var v = -halfDepth; v <= halfDepth + 1e-3f; v += S.ApronSample)
                if ((MathF.Abs(u) >= halfWidth - 1e-3f || MathF.Abs(v) >= halfDepth - 1e-3f) && site.PavingEdgeFacing(centre + right * u + facing * v) is { Gap: > 0f } hit
                    && (far is null || hit.Gap > far.Value.Gap))
                    far = hit;
        return far is { } f ? (Vector2.Distance(centre, f.Edge), f.Edge, f.Inward, f.Road) : null;
    }

    /// <summary>True when every point of a rectangle's outline (every <see cref="FurnitureStyle.ApronSample"/>) lies on pedestrian paving.</summary>
    private static bool OnPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth)
    {
        var right = new Vector2(-facing.Y, facing.X);
        for (var u = -halfWidth; u <= halfWidth + 1e-3f; u += S.ApronSample)
            for (var v = -halfDepth; v <= halfDepth + 1e-3f; v += S.ApronSample)
                if ((MathF.Abs(u) >= halfWidth - 1e-3f || MathF.Abs(v) >= halfDepth - 1e-3f) && site.PavingEdgeFacing(centre + right * u + facing * v) is not { Gap: <= 0f })
                    return false;
        return true;
    }

    /// <summary>
    /// The anchor's centre, moved the shortest way until its footprint (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/>
    /// along <paramref name="facing"/>) meets the nearest pedestrian paving, when it does not touch it yet; the facing stays. The footprint ends <see cref="FurnitureStyle.PadJoin"/> over the paving edge, so the two join. Paving
    /// further than <see cref="FurnitureStyle.AnchorPavingReach"/> is a data question (WP-48c), not a move: the anchor stays, diagnosticsged.
    /// </summary>
    private static Vector2 ToPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<BuildDiagnostic> diagnostics, FeatureId id, string form)
    {
        var right = new Vector2(-facing.Y, facing.X);
        var start = centre;
        for (var pass = 0; pass < 3; pass++)
        {
            // The footprint's rim point nearest to paving, and the paving edge it looks at.
            (float Gap, Vector2 From, Vector2 Edge)? near = null;
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                {
                    var q = centre + right * (i * halfWidth) + facing * (k * halfDepth);
                    if (site.PavingEdge(q) is { } e && (near is null || e.Gap < near.Value.Gap)) near = (e.Gap, q, e.Edge);
                }
            if (near is { Gap: <= 0f }) break;
            if (near is { } n) centre += Vector2.Normalize(n.Edge - n.From) * (n.Gap + S.PadJoin);
            if (near is null || Vector2.Distance(start, centre) > S.AnchorPavingReach)
            {
                diagnostics.Add(new(DiagnosticLevel.Warning, "wp13.anchor.no-paving", $"anchor {id.Value} ({form}) left in place: no pedestrian paving within {Num(S.AnchorPavingReach)} m", id));
                return start;
            }
        }
        return centre;
    }

    private static string Where(Landing landing) => landing switch
    {
        Landing.Carriageway => "on a carriageway",
        Landing.Water => "in water",
        Landing.Building => "in a building",
        _ => throw new ArgumentOutOfRangeException(nameof(landing), landing, "Not a fallback landing."),
    };
}

using System.Numerics;
using Campus.Contracts;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;

namespace Campus.Gen.Furniture;

/// <summary>Fits authored anchor footprints to nearby pedestrian paving.</summary>
internal static class FurniturePavingFit
{
    /// <summary>
    /// A kiosk's apron (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/> along <paramref name="facing"/>) that is not whole
    /// on pedestrian paving yet (Opus review 14:20, 16:36, 17:28): it stands whole on the nearest paving (for one partly on it, the edge its
    /// outline crosses farthest), its back edge <see cref="FurnitureStyle.ApronPavingClear"/>
    /// inside the paving edge and parallel to it, its face towards the paving, where people stand: the data's yaw gives way. It slides
    /// along the edge, in steps up to <see cref="FurnitureStyle.ApronSlide"/>, until the whole apron is on paving; where none fits (or
    /// no paving lies within <see cref="FurnitureStyle.AnchorPavingReach"/>) it only moves to touch it, as <see cref="ToPaving"/> does, facing
    /// its data yaw. The new centre and yaw are logged for the anchor data (SYS-ATLAS-TRAVEL), which takes them over; an apron whole on paving
    /// already stays as the data has it.
    /// </summary>
    internal static (Vector2 Centre, Vector2 Facing) KioskOnPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<BuildDiagnostic> diagnostics, FeatureId id)
    {
        // An apron whole on paving stays; one that only partly is (13c2a: A03, about half its outline off) turns onto the edge
        // its outline crosses, as one wholly off does onto the nearest edge (Opus review 17:28).
        if (OnPaving(site, centre, facing, halfWidth, halfDepth)) return (centre, facing);
        if ((site.Paving.PavingEdgeFacing(centre) is { Gap: > 0f } off ? off : FarthestOff(site, centre, facing, halfWidth, halfDepth)) is { } hit && hit.Gap <= S.AnchorPavingReach)
        {
            var inward = hit.Inward;
            var along = new Vector2(-inward.Y, inward.X);
            foreach (var slide in Shifts(S.ApronSlide))
            {
                var at = hit.Edge + along * slide + inward * (S.ApronPavingClear + halfDepth);
                if (Vector2.Distance(at, centre) > S.AnchorPavingReach || !OnPaving(site, at, inward, halfWidth, halfDepth)) continue;
                diagnostics.Add(new(DiagnosticLevel.Info, "furniture.anchor.turned", $"anchor {id.Value} (kiosk) turned onto paving: yawDeg {Fixed(Angles.Bearing(inward))} (data {Fixed(Angles.Bearing(facing))}), centre ({Fixed(at.X)}, {Fixed(at.Y)}) (data ({Fixed(centre.X)}, {Fixed(centre.Y)}))", id));
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
                if ((MathF.Abs(u) >= halfWidth - 1e-3f || MathF.Abs(v) >= halfDepth - 1e-3f) && site.Paving.PavingEdgeFacing(centre + right * u + facing * v) is { Gap: > 0f } hit
                    && (far is null || hit.Gap > far.Value.Gap))
                    far = hit;
        return far is { } f ? (Vector2.Distance(centre, f.Edge), f.Edge, f.Inward, f.Road) : null;
    }

    /// <summary>True when every point of a rectangle's outline (every <see cref="FurnitureStyle.ApronSample"/>) lies on pedestrian paving.</summary>
    internal static bool OnPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth)
    {
        var right = new Vector2(-facing.Y, facing.X);
        for (var u = -halfWidth; u <= halfWidth + 1e-3f; u += S.ApronSample)
            for (var v = -halfDepth; v <= halfDepth + 1e-3f; v += S.ApronSample)
                if ((MathF.Abs(u) >= halfWidth - 1e-3f || MathF.Abs(v) >= halfDepth - 1e-3f) && site.Paving.PavingEdgeFacing(centre + right * u + facing * v) is not { Gap: <= 0f })
                    return false;
        return true;
    }

    /// <summary>
    /// The anchor's centre, moved the shortest way until its footprint (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/>
    /// along <paramref name="facing"/>) meets the nearest pedestrian paving, when it does not touch it yet; the facing stays. The footprint ends <see cref="FurnitureStyle.PadJoin"/> over the paving edge, so the two join. Paving
    /// further than <see cref="FurnitureStyle.AnchorPavingReach"/> is a data question (SYS-ATLAS-TRAVEL), not a move: the anchor stays, diagnosticsged.
    /// </summary>
    internal static Vector2 ToPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<BuildDiagnostic> diagnostics, FeatureId id, string form)
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
                    if (site.Paving.PavingEdge(q) is { } e && (near is null || e.Gap < near.Value.Gap)) near = (e.Gap, q, e.Edge);
                }
            if (near is { Gap: <= 0f }) break;
            if (near is { } n) centre += Vector2.Normalize(n.Edge - n.From) * (n.Gap + S.PadJoin);
            if (near is null || Vector2.Distance(start, centre) > S.AnchorPavingReach)
            {
                diagnostics.Add(new(DiagnosticLevel.Warning, "furniture.anchor.no-paving", $"anchor {id.Value} ({form}) left in place: no pedestrian paving within {Num(S.AnchorPavingReach)} m", id));
                return start;
            }
        }
        return centre;
    }
}

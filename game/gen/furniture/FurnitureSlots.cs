using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurniturePavingFit;

namespace Campus.Gen.Furniture;

/// <summary>Admits bench, bin and amenity footprints on or beside pedestrian paving.</summary>
internal static class FurnitureSlots
{
    /// <summary>
    /// A small entity at arc length <paramref name="at"/> of a walk, on side <paramref name="bside"/> of it (along the walk's left
    /// normal), its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing the walk, if it fits: free of
    /// outlines, roads (only off this walk's surface), trees, the planted rows and placed things; <see cref="FurnitureStyle.RoadSetback"/>
    /// off carriageways; a bench (<paramref name="facesWalk"/>) with no carriageway <see cref="FurnitureStyle.FaceClear"/> ahead.
    /// Its bay reaches <see cref="FurnitureStyle.PadJoin"/> over the walk's edge and <see cref="FurnitureStyle.BayBehind"/> behind it.
    /// </summary>
    internal static WalkFit? WalkSlot(StreetSite site, StreetNetwork.Run path, float at, float bside, NetworkLine walk, FurnitureParams p, bool facesWalk)
    {
        if (path.Normal(at) is not { } n) return null;
        var whalf = StreetNetwork.HalfWidth(walk);
        var away = n * bside;
        var centre = path.At(at) + away * (whalf + S.WalkGap + p.Depth * 0.5f);
        return Slot(site, centre, -away, p, facesWalk, walk).Fit;
    }

    /// <summary>
    /// <see cref="WalkSlot"/> at a spot on the edge of any pedestrian paving (a plaza polygon, a footway's strip): the entity stands
    /// just off <paramref name="edge"/>, <paramref name="inward"/> being the unit normal into the paving, and faces it
    /// (<paramref name="walk"/>: the network walk the paving is, which the entity only has to stay off the surface of). A refusal
    /// names the rule that refused it.
    /// </summary>
    internal static (WalkFit? Fit, string? Reason) PavingSlot(StreetSite site, Vector2 edge, Vector2 inward, NetworkLine? walk, BoxPartParams p, bool facesWalk) =>
        Slot(site, edge - inward * (S.WalkGap + p.Depth * 0.5f), inward, p, facesWalk, walk);

    /// <summary>
    /// The entity's slot at <paramref name="centre"/> facing <paramref name="facing"/> (towards the walk or paving), if it fits: free
    /// of outlines, roads (<paramref name="beside"/> only off its surface), trees, the planted rows and placed things;
    /// <see cref="FurnitureStyle.RoadSetback"/> off carriageways; a bench (<paramref name="facesWalk"/>) with no carriageway
    /// <see cref="FurnitureStyle.FaceClear"/> ahead. Its bay reaches <see cref="FurnitureStyle.PadJoin"/> over the walk's edge and
    /// <see cref="FurnitureStyle.BayBehind"/> behind it.
    /// </summary>
    private static (WalkFit? Fit, string? Reason) Slot(StreetSite site, Vector2 centre, Vector2 facing, BoxPartParams p, bool facesWalk, NetworkLine? beside)
    {
        var tangent = new Vector2(facing.Y, -facing.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        if (site.Blocker(ends.Item1, ends.Item2, ends.Item3, beside) is { } blocker) return (null, $"blocked by {blocker}");
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
        {
            if (!site.OffRows(q, ends.Item3)) return (null, "on a planted tree row");
            if (!site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return (null, $"closer than {Num(S.RoadSetback)} m to a carriageway");
        }
        if (facesWalk && site.CarriagewayAhead(centre + facing * (p.Depth * 0.5f), facing, S.FaceClear)) return (null, $"facing a carriageway within {Num(S.FaceClear)} m");
        // The bay, in the entity's frame (−Z towards the walk): from just over the walk's edge to behind the entity.
        var front = p.Depth * 0.5f + S.WalkGap + S.PadJoin;
        var back = p.Depth * 0.5f + S.BayBehind;
        var middle = centre + facing * ((front - back) * 0.5f);
        if (Land.Pad(site.World.Terrain, Frame(middle, facing, 0f), p.Width * 0.5f + S.BayEnds, (front + back) * 0.5f) is not { } bay) return (null, "no ground for its bay");
        return (new WalkFit(centre, facing, ends, bay, bay.Top), null);
    }

    /// <summary>
    /// Behind a plaza bench's back (Opus review 13c2c): open ground, not paving, road, water or a building within the wall clearance,
    /// that is lawn or planting (the ground the plaza's outer edge meets), checked 1 m and <see cref="FurnitureStyle.PlazaBackPlanting"/> m out;
    /// or a tree or tree row right there.
    /// </summary>
    private static bool BackedOnGreen(StreetSite site, Vector2 back, Vector2 inward)
    {
        if (site.PlantingNear(back, S.PlazaBackPlanting)) return true;
        var out1 = back - inward * 1f;
        return !site.Paving.OnPavedGround(out1) && site.Blocker(out1, 0.2f, outlineClear: S.PlazaWallClear) is null;
    }

    /// <summary>
    /// A bench standing on a plaza's paving (Opus review 17:28), the way a kiosk does: its back <see cref="FurnitureStyle.PlazaBenchClear"/>
    /// inside the paving edge at <paramref name="edge"/> and parallel to it, facing into the paving (<paramref name="inward"/>). Its footprint
    /// and its sitters' feet are wholly on paving and off every walk's strip (benches leave the walk to walkers), and the walk benches'
    /// rules hold: free of outlines, trees, rows and placed things, <see cref="FurnitureStyle.RoadSetback"/> off carriageways, no carriageway
    /// <see cref="FurnitureStyle.FaceClear"/> ahead. It stands on the paving, a plinth only where the paving is steeper than 5%.
    /// </summary>
    internal static (WalkFit? Fit, string? Reason) PlazaSlot(StreetSite site, Vector2 edge, Vector2 inward, FurnitureParams p, int tier = 0)
    {
        var centre = edge + inward * (S.PlazaBenchClear + p.Depth * 0.5f);
        var tangent = new Vector2(inward.Y, -inward.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        // The footprint and the sitters' feet ahead of it.
        var feet = centre + inward * (S.FeetReach * 0.5f);
        if (!OnPaving(site, feet, inward, p.Width * 0.5f, (p.Depth + S.FeetReach) * 0.5f)) return (null, "not whole on the plaza's paving");
        // On a plaza's own paving, never on a walk's strip outside it (walks belong to walkers; a strip's round end is lawn where WP-15
        // cuts it square). A walk line the plaza carries does not count against the bench (Opus review 13c2b rework), but the bench
        // never sits across the line itself.
        for (var u = -1; u <= 1; u++)
            for (var v = -1; v <= 1; v++)
                if (!site.Paving.OnPlaza(feet + tangent * (u * p.Width * 0.5f) + inward * (v * (p.Depth + S.FeetReach) * 0.5f))) return (null, "on a walk, not a plaza");
        // Backed onto planting, never onto bare paving or a wall (Opus review 13c2c): a tree or tree row behind either end or the middle.
        var behind = -inward * (p.Depth * 0.5f + S.PlazaBenchClear);
        // Tier 0: backed on lawn or planting. Tier 1 (no such edge): beside a tree, a tree or row within PlazaBesideTree. Tier 2 (no tree
        // either): any plaza edge, which is still 1.5 m off every wall. The bench never backs onto a building.
        if (tier == 0 && !new[] { ends.Item1, centre, ends.Item2 }.All(q => BackedOnGreen(site, q + behind, inward))) return (null, "no planting or lawn behind its back");
        if (tier == 1 && !site.PlantingNear(centre, S.PlazaBesideTree)) return (null, "no tree beside it");
        if (site.NearWalkLine([ends.Item1, centre, ends.Item2], ends.Item3 + S.PlazaWalkLineClear)) return (null, "across a walk line");
        if (site.Blocker(ends.Item1, ends.Item2, ends.Item3, outlineClear: S.PlazaWallClear, walksPass: true) is { } blocker) return (null, $"blocked by {blocker}");
        // At least PlazaPass of free paving ahead of the sitters' feet: people pass in front.
        for (var d = 0.5f; d <= S.PlazaPass + 1e-3f; d += 0.5f)
            for (var u = -1; u <= 1; u++)
            {
                var q = centre + inward * (p.Depth * 0.5f + S.FeetReach + d) + tangent * (u * p.Width * 0.5f);
                if (!site.Paving.OnPavedGround(q)) return (null, $"less than {Num(S.PlazaPass)} m of paving in front");
                if (site.Blocker(q, 0f, walksPass: true) is { } ahead) return (null, $"{ahead} within {Num(S.PlazaPass)} m in front");
            }
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
        {
            if (!site.OffRows(q, ends.Item3)) return (null, "on a planted tree row");
            if (!site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return (null, $"closer than {Num(S.RoadSetback)} m to a carriageway");
        }
        if (site.CarriagewayAhead(centre + inward * (p.Depth * 0.5f), inward, S.FaceClear)) return (null, $"facing a carriageway within {Num(S.FaceClear)} m");
        if (Land.Pad(site.World.Terrain, Frame(centre, inward, 0f), p.Width * 0.5f, p.Depth * 0.5f) is not { } ground) return (null, "no ground for its feet");
        // Not against a bank: the ground round it (the back included) keeps within PlazaBankRise of the paving it stands on.
        var terrain = site.World.Terrain;
        var paving = ground.Top - GroundLevels.Paving - S.PadProud;
        for (var i = -1; i <= 1; i++)
            for (var k = -1; k <= 1; k++)
            {
                var q = centre + tangent * (i * (p.Width * 0.5f + S.PlazaBankReach)) + inward * (k * (p.Depth * 0.5f + S.PlazaBankReach));
                if (terrain.HeightAt(q.X, q.Y) > paving + S.PlazaBankRise) return (null, "against a bank");
            }
        return ground.Plinth ? (new WalkFit(centre, inward, ends, ground, ground.Top), null) : (new WalkFit(centre, inward, ends, null, ground.Top - S.PadProud), null);
    }

    /// <summary>
    /// A node bench standing on a walk at least <see cref="FurnitureStyle.NodeWalkMin"/> wide (Opus review 13c2b 21:05): its back
    /// <see cref="FurnitureStyle.WalkBenchClear"/> inside the walk's edge at <paramref name="edge"/> and parallel to it, facing across the
    /// walk (<paramref name="inward"/>), with at least <see cref="FurnitureStyle.NodeWalkPass"/> of the walk clear beyond its sitters' feet.
    /// Its footprint and the feet are on paved ground; the walk benches' rules hold (free of outlines, other roads, trees, rows and placed
    /// things, <see cref="FurnitureStyle.RoadSetback"/> off carriageways, none <see cref="FurnitureStyle.FaceClear"/> ahead). It stands on
    /// the paving, a plinth only where the paving is steeper than 5%.
    /// </summary>
    internal static (WalkFit? Fit, string? Reason) WalkEdgeSlot(StreetSite site, Vector2 edge, Vector2 inward, NetworkLine? walk, FurnitureParams p)
    {
        if (walk is null) return (null, "not a walk's edge");
        var width = 2f * StreetNetwork.HalfWidth(walk);
        if (width < S.NodeWalkMin) return (null, $"on a walk {Fixed(width)} m wide, under {Num(S.NodeWalkMin)} m");
        if (width - (S.WalkBenchClear + p.Depth + S.FeetReach) < S.NodeWalkPass) return (null, $"less than {Num(S.NodeWalkPass)} m left to pass");
        var centre = edge + inward * (S.WalkBenchClear + p.Depth * 0.5f);
        var tangent = new Vector2(inward.Y, -inward.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        var feet = centre + inward * (S.FeetReach * 0.5f);
        for (var u = -1; u <= 1; u++)
            for (var v = -1; v <= 1; v++)
                if (!site.Paving.OnPavedGround(feet + tangent * (u * p.Width * 0.5f) + inward * (v * (p.Depth + S.FeetReach) * 0.5f))) return (null, "not whole on paved ground");
        if (site.Blocker(ends.Item1, ends.Item2, ends.Item3, walk, onBeside: true) is { } blocker) return (null, $"blocked by {blocker}");
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
        {
            if (!site.OffRows(q, ends.Item3)) return (null, "on a planted tree row");
            if (!site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return (null, $"closer than {Num(S.RoadSetback)} m to a carriageway");
        }
        if (site.CarriagewayAhead(centre + inward * (p.Depth * 0.5f), inward, S.FaceClear)) return (null, $"facing a carriageway within {Num(S.FaceClear)} m");
        if (Land.Pad(site.World.Terrain, Frame(centre, inward, 0f), p.Width * 0.5f, p.Depth * 0.5f) is not { } ground) return (null, "no ground for its feet");
        var terrain = site.World.Terrain;
        var paving = ground.Top - GroundLevels.Paving - S.PadProud;
        for (var i = -1; i <= 1; i++)
            for (var k = -1; k <= 1; k++)
            {
                var q = centre + tangent * (i * (p.Width * 0.5f + S.PlazaBankReach)) + inward * (k * (p.Depth * 0.5f + S.PlazaBankReach));
                if (terrain.HeightAt(q.X, q.Y) > paving + S.PlazaBankRise) return (null, "against a bank");
            }
        return ground.Plinth ? (new WalkFit(centre, inward, ends, ground, ground.Top), null) : (new WalkFit(centre, inward, ends, null, ground.Top - S.PadProud), null);
    }
}

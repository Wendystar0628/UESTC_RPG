using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Interior;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Model;

/// <summary>The entrance steps of BuildingPlanner.Plan: anchor doors from the interior index, then the entrance records with their landings.</summary>
internal static class PlanEntrances
{
    /// <summary>An anchor door before the openings exist: facade, bay, storey row and its clear opening in skin u and Y.</summary>
    internal readonly record struct AnchorDoor(int Facade, int Bay, int Row, float U0, float U1, float Y0, float Y1, string Id, bool Deck);

    /// <summary>Entrances: the generic door at the building's floor with its landing, or the anchor doors.</summary>
    public static List<PlanEntrance> Entrances(IWorldModel world, IReadOnlyList<FacadeLayout> facades, IReadOnlyList<Opening[]> openings,
        IReadOnlyList<PlanRing> skins, IReadOnlyList<AnchorDoor> anchors, int entranceFacade, int entranceBay, float canopy, bool small)
    {
        var entrances = new List<PlanEntrance>();
        if (entranceFacade >= 0)
        {
            var door = openings[entranceFacade].First(o => o.Kind == BayKind.Door);
            entrances.Add(new PlanEntrance(entranceFacade, entranceBay, 0, null, true,
                GroundInFront(world, skins[facades[entranceFacade].Ring], facades[entranceFacade].Edge, door), canopy));
        }
        foreach (var a in anchors)
        {
            var door = openings[a.Facade].First(o => o.Kind == BayKind.Door && o.Bay == a.Bay && o.Row == a.Row);
            entrances.Add(new PlanEntrance(a.Facade, a.Bay, a.Row, a.Id, a.Row == 0 && !a.Deck,
                GroundInFront(world, skins[facades[a.Facade].Ring], facades[a.Facade].Edge, door), a.Y1 + (small ? 0.15f : 0.25f)));
        }
        return entrances;
    }

    /// <summary>
    /// The doors this footprint opens for the open entrances its feature has in the interior index (WLD-INTERIORS): on
    /// the facade edge the anchor stands on and faces out of, in the bay under its door centre (centred on the anchor unless
    /// the bay centre is within EntranceRules.BaySnap), at the storey of its floor. An anchor is left alone (to another part
    /// of the feature, or to the zone pack that builds that door) when no edge of this footprint takes it, its outside lies in
    /// another footprint, its floor leaves no door height under the next slab, or the door cannot stand within
    /// EntranceRules.AnchorTolerance of it. The door top is the anchor's HeadY when the index gives one (else the family's
    /// door rise), never above the next slab. Deck: the outside landing stands well above the terrain (a porch or deck).
    /// </summary>
    public static List<AnchorDoor> AnchorDoors(IWorldModel world, WorldFeature feature, IReadOnlyList<PlanRing> rings,
        IReadOnlyList<FacadeLayout> facades, IReadOnlyList<(float Min, float Max)> uRange, Func<int, float> floorY, int storeys,
        float wallTop, float doorRise)
    {
        var doors = new List<AnchorDoor>();
        if (world.Interiors.Get(feature.Id) is not { } entry) return doors;
        foreach (var e in entry.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            var facing = Angles.Heading(e.YawDeg);
            var centre = new Vector2(e.Center.X, e.Center.Z);
            int fi = -1;
            float uFoot = 0f, nearest = EntranceRules.AnchorReach;
            for (var i = 0; i < facades.Count; i++)
            {
                var ring = rings[facades[i].Ring];
                var edge = facades[i].Edge;
                if (Vector2.Dot(ring.Normal[edge], facing) < EntranceRules.FacingMin) continue;
                var t = Vector2.Dot(centre - ring.Points[edge], ring.Dir[edge]);
                var d = MathF.Abs(Vector2.Dot(centre - ring.Points[edge], ring.Normal[edge]));
                if (t < 0f || t > ring.Length[edge] || d >= nearest) continue;
                (fi, uFoot, nearest) = (i, t, d);
            }
            if (fi < 0) continue;

            // Storey: the highest whose floor lies at or below the anchor floor; the door stands on that floor when near it.
            var row = 0;
            for (var j = 1; j < storeys; j++) if (e.Center.Y >= floorY(j) - EntranceRules.FloorSnap) row = j;
            var y0 = MathF.Abs(e.Center.Y - floorY(row)) <= EntranceRules.FloorSnap ? floorY(row) : e.Center.Y;
            if (Indoors(world, centre + facing * EntranceRules.ExteriorProbe, y0)) continue;
            var room = (row + 1 < storeys ? floorY(row + 1) - EntranceRules.SlabDepth : wallTop - 0.3f) - y0;
            // The anchor's own doorway top (contract-34 HeadY, absolute; 0 = unknown) wins over the family's door rise.
            var rise = MathF.Min(e.HeadY != 0f ? e.HeadY - y0 : doorRise, room);
            if (rise < MathF.Min(WalkRules.MinimumDoorHeight, doorRise)) continue;

            var f = facades[fi];
            var k = 0;
            while (k + 1 < f.Bays && f.Bounds[k + 1] <= uFoot) k++;
            if (doors.Any(d => d.Facade == fi && d.Bay == k && d.Row == row)) continue;
            var width = MathF.Max(Math.Clamp(f.BayWidthAt(k) - 0.7f, 1.2f, 2.1f), MathF.Min(2.1f, e.ClearWidth + EntranceRules.DoorFrame));
            var anchor = uFoot - f.SkinShift;
            var c = MathF.Abs(f.CentreSkin(k) - anchor) <= EntranceRules.BaySnap ? f.CentreSkin(k) : anchor;
            var (uMin, uMax) = uRange[fi];
            if (uMax - uMin < width) continue;
            c = Math.Clamp(c, uMin + width * 0.5f, uMax - width * 0.5f);
            if (MathF.Abs(c - anchor) > EntranceRules.AnchorTolerance) continue;
            var deck = e.Outside.Y - world.Terrain.HeightAt(e.Outside.X, e.Outside.Z) > EntranceRules.DeckAbove;
            doors.Add(new AnchorDoor(fi, k, row, c - width * 0.5f, c + width * 0.5f, y0, y0 + rise, e.Id, deck));
        }
        return doors;
    }

    /// <summary>
    /// True when a plan point at floor level <paramref name="y"/> lies inside another building: a footprint over it whose
    /// roof stands higher than the floor by EntranceRules.DeckAbove. A lower block there is a deck the door opens onto.
    /// </summary>
    private static bool Indoors(IWorldModel world, Vector2 p, float y)
    {
        var box = new Box3(new Vector3(p.X - 0.5f, -1000f, p.Y - 0.5f), new Vector3(p.X + 0.5f, 1000f, p.Y + 0.5f));
        foreach (var id in world.Spatial.Query(box))
        {
            if (!world.Features.TryGetValue(id, out var f)) continue;
            foreach (var src in BuildingPlanner.Sources(f))
                if (PlanGeometry.InsideByCrossings(src.Rings.Outer, p) && !src.Rings.Holes.Any(h => PlanGeometry.InsideByCrossings(h, p)) &&
                    BuildingPlanner.PadY(world, f) + BuildingPlanner.TopOf(src) > y + EntranceRules.DeckAbove) return true;
        }
        return false;
    }

    /// <summary>
    /// The outside landing level in front of a door (WLD-TERRAIN): the terrain beyond the landing's front edge on the
    /// door's centre line (ITerrain; level ground at the pad while the evaluator is a stub).
    /// </summary>
    private static float GroundInFront(IWorldModel world, PlanRing skin, int edge, Opening door)
    {
        var p = skin.Points[edge] + skin.Dir[edge] * ((door.U0 + door.U1) * 0.5f) +
            skin.Normal[edge] * (EntranceRules.LandingDepth + EntranceRules.GroundProbe);
        return world.Terrain.HeightAt(p.X, p.Y);
    }
}

using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Interior;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Model;

/// <summary>The facade steps of BuildingPlanner.Plan: storey rows, bay layouts per edge, skeleton columns, openings and their zones.</summary>
internal static class PlanFacades
{
    /// <summary>Storey rows: sill and head per storey; tall single-storey halls get a clerestory band.</summary>
    public static (float[] Sill, float[] Head) StoreyRows(FacadeProfile profile, float baseY, float wallTopRel, int storeys, float hs)
    {
        var sill = new float[storeys];
        var head = new float[storeys];
        for (var k = 0; k < storeys; k++)
        {
            var floor = baseY + BuildingPlan.FloorLevel + k * hs;
            float sillRel, win;
            if (hs > 6.5f)
            {
                sillRel = MathF.Max(profile.Sill, hs * 0.55f);
                win = MathF.Min(profile.WindowHeight, hs * 0.3f);
            }
            else
            {
                // Ground-floor sills stay clear of the plinth line.
                var minGround = BuildingPlan.PlinthTop - BuildingPlan.FloorLevel;
                sillRel = k == 0 ? MathF.Max(profile.GroundSill, minGround + 0.3f) : profile.Sill;
                if (hs - 0.55f - sillRel < 0.9f) sillRel = MathF.Max(k == 0 ? minGround + 0.2f : 0.5f, hs - 0.55f - 0.9f);
                win = MathF.Min(profile.WindowHeight, hs - 0.55f - sillRel);
            }
            sill[k] = floor + sillRel;
            head[k] = MathF.Min(floor + sillRel + win, baseY + wallTopRel - 0.3f);
        }
        return (sill, head);
    }

    /// <summary>Facade layouts per footprint edge, read from the kernel's bays (ring -> edge -> bay order).</summary>
    public static List<FacadeLayout> Layouts(FacadeGrid grid, IReadOnlyList<PlanRing> rings, IReadOnlyList<PlanRing> skins)
    {
        var facades = new List<FacadeLayout>();
        var bayList = grid.Bays;
        var next = 0;
        for (var r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];
            var skin = skins[r];
            for (var e = 0; e < ring.Count; e++)
            {
                var first = next;
                while (next < bayList.Count && bayList[next].Ring == r && bayList[next].Edge == e) next++;
                var count = next - first;
                if (count == 0) throw new InvalidOperationException($"Facade grid has no bay on ring {r} edge {e}.");
                var bounds = new float[count + 1];
                var widths = new float[count];
                var kinds = new BayKind[count];
                for (var k = 0; k < count; k++)
                {
                    var bay = bayList[first + k];
                    bounds[k] = Vector2.Dot(bay.A - ring.Points[e], ring.Dir[e]);
                    widths[k] = Vector2.Distance(bay.OpeningA, bay.OpeningB);
                    kinds[k] = bay.WindowKind == FacadeWindowKind.Blank ? BayKind.Blank : BayKind.Window;
                }
                bounds[count] = Vector2.Dot(bayList[next - 1].B - ring.Points[e], ring.Dir[e]);
                var shift = Vector2.Dot(skin.Points[e] - ring.Points[e], ring.Dir[e]);
                facades.Add(new FacadeLayout(r, e, count, ring.Length[e] / count, widths.Max(), bayList[first].Index, kinds, shift, bounds, widths));
            }
        }
        if (next != bayList.Count) throw new InvalidOperationException("Facade grid bays do not match the footprint rings.");
        return facades;
    }

    /// <summary>
    /// Skeleton columns: entrance on the longest outer facade, stair and toilet columns, corridor ends. Returns the
    /// generic entrance's facade and bay (-1 when none).
    /// </summary>
    public static (int Facade, int Bay) SkeletonColumns(List<FacadeLayout> facades, PlanRing outer, int longest, Vector2 axis,
        int storeys, float hs)
    {
        var entranceFacade = -1;
        var entranceBay = -1;
        var main = facades.FindIndex(f => f.Ring == 0 && f.Edge == longest);
        if (main >= 0 && hs >= 2.9f)
        {
            var f = facades[main];
            var mid = f.Bays / 2;
            if (f.Kinds[mid] != BayKind.Blank)
            {
                entranceFacade = main;
                entranceBay = mid;
                f.Kinds[mid] = BayKind.Door;
            }
            if (storeys >= 2 && f.Bays >= 5)
            {
                var a = Math.Max(0, (int)MathF.Round(f.Bays * 0.18f));
                SetIfWindow(f.Kinds, a, BayKind.Stair);
                SetIfWindow(f.Kinds, a + 1, BayKind.Toilet);
                if (f.Bays >= 9) SetIfWindow(f.Kinds, f.Bays - 1 - a, BayKind.Stair);
            }
        }
        if (storeys >= 2)
            foreach (var f in facades)
                if (f.Ring == 0 && MathF.Abs(Vector2.Dot(outer.Dir[f.Edge], axis)) < 0.35f)
                    SetIfWindow(f.Kinds, f.Bays / 2, BayKind.Corridor);
        return (entranceFacade, entranceBay);
    }

    /// <summary>Openings in skin u, kept clear of the skin corners.</summary>
    public static (float Min, float Max)[] OpeningRanges(IReadOnlyList<FacadeLayout> facades, IReadOnlyList<PlanRing> skins) =>
        facades.Select(f => OpeningRange(skins[f.Ring], f.Edge)).ToArray();

    /// <summary>The window and door openings of every facade; anchor doors replace the generic door and the windows beside them.</summary>
    public static List<Opening[]> Openings(IReadOnlyList<FacadeLayout> facades, IReadOnlyList<(float Min, float Max)> uRange,
        IReadOnlyList<PlanEntrances.AnchorDoor> anchors, float[] sill, float[] head, int storeys, float threshold, float doorHead,
        int entranceFacade, int entranceBay)
    {
        var openings = new List<Opening[]>(facades.Count);
        for (var fi = 0; fi < facades.Count; fi++)
        {
            var f = facades[fi];
            var (uMin, uMax) = uRange[fi];
            var list = new List<Opening>();
            for (var k = 0; k < f.Bays; k++)
            {
                var anchored = anchors.Any(a => a.Facade == fi && a.Bay == k);
                if (f.Kinds[k] == BayKind.Blank && !anchored) continue;
                var c = f.CentreSkin(k);
                for (var j = 0; j < storeys; j++)
                {
                    if (anchored && anchors.FirstOrDefault(a => a.Facade == fi && a.Bay == k && a.Row == j) is { Id: not null } door)
                    {
                        list.Add(new Opening(k, j, door.U0, door.U1, door.Y0, door.Y1, BayKind.Door));
                        continue;
                    }
                    if (f.Kinds[k] == BayKind.Blank) continue;
                    float u0, u1, y0, y1;
                    BayKind kind;
                    if (fi == entranceFacade && k == entranceBay && j == 0)
                    {
                        var dw = Math.Clamp(f.BayWidthAt(k) - 0.7f, 1.2f, 2.1f);
                        (u0, u1, y0, y1, kind) = (c - dw * 0.5f, c + dw * 0.5f, threshold, doorHead, BayKind.Door);
                    }
                    else
                    {
                        if (head[j] - sill[j] < 0.6f) continue;
                        kind = f.Kinds[k] == BayKind.Door ? BayKind.Window : f.Kinds[k];
                        var w = kind == BayKind.Stair ? MathF.Min(f.Widths[k], 1.0f) : f.Widths[k];
                        (u0, u1, y0, y1) = (c - w * 0.5f, c + w * 0.5f, sill[j], head[j]);
                    }
                    u0 = MathF.Max(u0, uMin);
                    u1 = MathF.Min(u1, uMax);
                    if (u1 - u0 < 0.5f) continue;
                    list.Add(new Opening(k, j, u0, u1, y0, y1, kind));
                }
            }
            // Windows of the door's storey that come within a pier of an anchor door give way to it (their cells stay empty).
            foreach (var a in anchors.Where(a => a.Facade == fi))
                list.RemoveAll(o => o.Row == a.Row && o.Kind != BayKind.Door &&
                    o.U0 < a.U1 + EntranceRules.Pier && o.U1 > a.U0 - EntranceRules.Pier);
            openings.Add([.. list]);
        }
        return openings;
    }

    /// <summary>
    /// Zone of each opening from the facade segments of the exported fidelity data (ENG-ARCH: one or more segments
    /// per footprint edge, A → B along the ring). An opening takes the segment its centre lies on (parallel, within
    /// OpeningRules.ZoneMatchDistance of the footprint line); row 0 takes the segment's ground band. No segment: K3.
    /// </summary>
    public static FidelityZone[][] Zones(IReadOnlyList<FacadeSegment> segments, IReadOnlyList<PlanRing> rings,
        IReadOnlyList<FacadeLayout> facades, IReadOnlyList<Opening[]> openings)
    {
        var zones = new FidelityZone[facades.Count][];
        for (var fi = 0; fi < facades.Count; fi++)
        {
            var f = facades[fi];
            var ring = rings[f.Ring];
            var dir = ring.Dir[f.Edge];
            zones[fi] = new FidelityZone[openings[fi].Length];
            for (var k = 0; k < openings[fi].Length; k++)
            {
                var o = openings[fi][k];
                var centre = ring.Points[f.Edge] + dir * ((o.U0 + o.U1) * 0.5f + f.SkinShift);
                FacadeSegment? best = null;
                var bestDistance = OpeningRules.ZoneMatchDistance;
                foreach (var s in segments)
                {
                    var ab = s.B - s.A;
                    var length = ab.Length();
                    if (length < 1e-3f || MathF.Abs(Vector2.Dot(ab / length, dir)) < 0.95f) continue;
                    var t = Math.Clamp(Vector2.Dot(centre - s.A, ab) / (length * length), 0f, 1f);
                    var distance = Vector2.Distance(centre, s.A + ab * t);
                    if (distance < bestDistance) { best = s; bestDistance = distance; }
                }
                zones[fi][k] = best is null ? FidelityZone.K3 : o.Row == 0 ? best.GroundBand : best.Zone;
            }
        }
        return zones;
    }

    /// <summary>Where openings may sit on a skin edge: clear of its corner chamfers by 0.2 m.</summary>
    private static (float Min, float Max) OpeningRange(PlanRing skin, int edge) =>
        ((Prims.Chamferable(skin, edge, BuildingPlan.WallChamfer) ? BuildingPlan.WallChamfer : 0f) + 0.2f,
            skin.Length[edge] - (Prims.Chamferable(skin, (edge + 1) % skin.Count, BuildingPlan.WallChamfer) ? BuildingPlan.WallChamfer : 0f) - 0.2f);

    private static void SetIfWindow(BayKind[] kinds, int i, BayKind kind)
    {
        if (i >= 0 && i < kinds.Length && kinds[i] == BayKind.Window) kinds[i] = kind;
    }
}

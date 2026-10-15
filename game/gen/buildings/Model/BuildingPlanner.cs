using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Interior;

namespace Campus.Gen.Buildings.Model;

/// <summary>One footprint to build: a whole feature, or one source part of it (contract WorldFeature.Parts, pending).</summary>
public sealed record BuildingSource(WorldFeature Feature, string Key, PolygonRings Rings, float? Height, int? Storeys, JsonElement? Props);

/// <summary>Turns a footprint into a level-independent plan: rings, wall skin, storeys, heights and the facade bay grid.</summary>
public static class BuildingPlanner
{
    public static readonly DesignKind BuildingKind = new("building");

    public static bool IsBuilding(WorldFeature f) => f.Kind == BuildingKind;

    /// <summary>
    /// The footprints of a building feature: each source part with its own height and storeys when Parts is
    /// non-empty (the aggregate geometry is then not built), else the feature geometry itself.
    /// Multi-polygons yield one footprint per polygon; keys stay stable (source ID, then "#index").
    /// </summary>
    public static IEnumerable<BuildingSource> Sources(WorldFeature f)
    {
        if (!IsBuilding(f)) yield break;
        if (f.Parts.Count > 0)
        {
            foreach (var part in f.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
                foreach (var (key, rings) in Polygons(part.SourceId, part.Geometry))
                    yield return new BuildingSource(f, key, rings, part.Height ?? f.Height, part.Storeys ?? f.Storeys, part.Props ?? f.Props);
            yield break;
        }
        foreach (var (key, rings) in Polygons(f.Id.Value, f.Geometry))
            yield return new BuildingSource(f, key, rings, f.Height, f.Storeys, f.Props);
    }

    private static IEnumerable<(string Key, PolygonRings Rings)> Polygons(string key, OverlayGeometry geometry)
    {
        switch (geometry)
        {
            case PolygonGeometry p:
                yield return (key, p.Rings);
                break;
            case MultiPolygonGeometry m:
                for (var i = 0; i < m.Polygons.Count; i++)
                    yield return (m.Polygons.Count == 1 ? key : key + "#" + i.ToString(CultureInfo.InvariantCulture), m.Polygons[i]);
                break;
            default:
                yield break;
        }
    }

    public static BuildingPlan? Plan(BuildingSource src, ISpatialIndex spatial, uint worldSeed)
    {
        var feature = src.Feature;
        var outerClean = PlanRing.Clean(src.Rings.Outer, hole: false);
        if (outerClean is null) return null;
        var holesClean = new List<PlanRing>();
        foreach (var h in src.Rings.Holes)
        {
            var ring = PlanRing.Clean(h, hole: true);
            if (ring is null || MathF.Abs(PlanRing.SignedArea(ring.Points)) < 8f) continue;
            if (!ring.Points.All(p => PlanRing.Contains(outerClean.Points, p))) continue;
            holesClean.Add(ring);
        }

        var style = FacadeRules.StyleOf(feature);
        var centroidClean = PlanRing.Centroid(outerClean, holesClean);
        var cell = new Vector2(MathF.Floor(centroidClean.X / 160f), MathF.Floor(centroidClean.Y / 160f));
        var cluster = StableHash.Hash32(worldSeed, "wp14.cluster",
            cell.X.ToString(CultureInfo.InvariantCulture), cell.Y.ToString(CultureInfo.InvariantCulture));
        var profile = FacadeRules.ProfileFor(style, cluster);
        var seed = StableHash.Hash32(worldSeed, feature.Id.Value, src.Key, "wp14.building");

        // Facade bay grid from the WP-51a plan kernel: canonical rings (mm, fixed start vertex), bays on the glass
        // plane, indices contiguous over all rings. The same call gives the interiors their partition lines.
        var grid = Grid(outerClean, holesClean, GridOptions(profile));
        if (grid is null) return null;
        var outer = PlanRing.FromPoints([.. grid.Footprint.Outer], hole: false);
        var holes = grid.Footprint.Holes.Select(h => PlanRing.FromPoints([.. h], hole: true)).ToList();
        var area = PlanRing.SignedArea(outer.Points) - holes.Sum(h => MathF.Abs(PlanRing.SignedArea(h.Points)));
        if (area < 6f) return null;
        var centroid = PlanRing.Centroid(outer, holes);

        var height = src.Height is > 0.5f and var h0 ? h0 : (src.Storeys ?? 1) * profile.StoreyHeight;
        height = MathF.Max(height, 2.2f);
        var storeys = src.Storeys is > 0 and var s0 ? s0 : Math.Max(1, (int)MathF.Round(height / profile.StoreyHeight));
        storeys = Math.Clamp(storeys, 1, 40);
        if (height / storeys < 2.6f) storeys = Math.Max(1, (int)MathF.Floor(height / 2.6f));
        var hs = (height - BuildingPlan.FloorLevel) / storeys;
        var small = area < 40f || height < 3.2f;

        // Wall skin: the footprint is the glass plane (WP-51a), the wall face sits one reveal outside it.
        var reveal = profile.Reveal;
        var skinOuter = Skin(outer, reveal);
        var skinHoles = holes.Select(h => Skin(h, reveal)).ToList();
        if (skinOuter is null || skinHoles.Any(h => h is null))
        {
            skinOuter = outer;
            skinHoles = [.. holes];
        }

        var baseY = PadY(feature);

        // Storey rows: sill and head per storey; tall single-storey halls get a clerestory band.
        var sill = new float[storeys];
        var head = new float[storeys];
        var wallTopRel = profile.RoofEdge == RoofEdge.Cornice ? height - (small ? 0.3f : 0.5f) : height + (small ? 0.45f : 0.8f);
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

        // Entrance threshold = ground-floor level (D-180); the plinth is cut at the door, a landing step sits in front.
        var threshold = baseY + BuildingPlan.FloorLevel;
        var doorHead = threshold + Math.Clamp(hs - 1.0f, 2.1f, 2.4f);
        var canopy = doorHead + 0.25f;
        if (small) { doorHead = threshold + Math.Clamp(height - BuildingPlan.FloorLevel - 0.5f, 1.9f, 2.2f); canopy = doorHead + 0.15f; }

        var longest = 0;
        for (var i = 1; i < outer.Count; i++) if (outer.Length[i] > outer.Length[longest]) longest = i;
        var axis = outer.Dir[longest];

        // Facade layouts per footprint edge, read from the kernel's bays (ring -> edge -> bay order).
        var facades = new List<FacadeLayout>();
        var rings = new List<PlanRing> { outer };
        rings.AddRange(holes);
        var bayList = grid.Bays;
        var next = 0;
        for (var r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];
            var skin = r == 0 ? skinOuter : skinHoles[r - 1]!;
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
                var shift = Vector2.Dot(skin!.Points[e] - ring.Points[e], ring.Dir[e]);
                facades.Add(new FacadeLayout(r, e, count, ring.Length[e] / count, widths.Max(), bayList[first].Index, kinds, shift, bounds, widths));
            }
        }
        if (next != bayList.Count) throw new InvalidOperationException("Facade grid bays do not match the footprint rings.");
        var column = bayList.Count;

        // Skeleton columns: entrance on the longest outer facade, stair and toilet columns, corridor ends.
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

        // Openings in skin u, kept clear of the skin corners.
        var openings = new List<Opening[]>(facades.Count);
        for (var fi = 0; fi < facades.Count; fi++)
        {
            var f = facades[fi];
            var skin = f.Ring == 0 ? skinOuter : skinHoles[f.Ring - 1]!;
            var uMin = (Prims.Chamferable(skin, f.Edge, BuildingPlan.WallChamfer) ? BuildingPlan.WallChamfer : 0f) + 0.2f;
            var uMax = skin.Length[f.Edge] - (Prims.Chamferable(skin, (f.Edge + 1) % skin.Count, BuildingPlan.WallChamfer) ? BuildingPlan.WallChamfer : 0f) - 0.2f;
            var list = new List<Opening>();
            for (var k = 0; k < f.Bays; k++)
            {
                if (f.Kinds[k] == BayKind.Blank) continue;
                var c = f.CentreSkin(k);
                for (var j = 0; j < storeys; j++)
                {
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
            openings.Add([.. list]);
        }
        if (entranceFacade >= 0 && !openings[entranceFacade].Any(o => o.Kind == BayKind.Door))
        {
            facades[entranceFacade].Kinds[entranceBay] = BayKind.Window;
            entranceFacade = -1;
            entranceBay = -1;
        }

        return new BuildingPlan
        {
            Id = feature.Id,
            Key = src.Key,
            Feature = feature,
            Outer = outer,
            Holes = holes,
            SkinOuter = skinOuter,
            SkinHoles = skinHoles.Select(h => h!).ToArray(),
            Reveal = reveal,
            Centroid = centroid,
            Area = area,
            Style = style,
            Profile = profile,
            Seed = seed,
            Small = small,
            BaseY = baseY,
            Height = height,
            Storeys = storeys,
            StoreyHeight = hs,
            SillY = sill,
            HeadY = head,
            DoorHeadY = doorHead,
            ThresholdY = threshold,
            CanopyY = canopy,
            Grid = grid,
            Facades = facades,
            Openings = openings,
            TotalColumns = column,
            Axis = axis,
            EntranceFacade = entranceFacade,
            EntranceBay = entranceBay,
        };
    }

    /// <summary>
    /// The one place the building's base height comes from (D-180): the terrain pad under the footprint,
    /// ITerrain.PadY(feature) once contract-15 lands; until then the pad is level ground at 0.
    /// </summary>
    public static float PadY(WorldFeature feature) => 0f;

    /// <summary>Per-family grid options: dormitories keep the kernel defaults (3.6 / 2.8 / 4.2 m, 1.8 m windows).</summary>
    public static FacadeGridOptions GridOptions(FacadeProfile profile) => new()
    {
        TargetBayWidth = profile.Bay,
        MinimumBayWidth = MathF.Min(2.8f, 0.78f * profile.Bay),
        MaximumBayWidth = MathF.Max(4.2f, 1.18f * profile.Bay),
        WindowWidth = profile.Bay * profile.Glazing,
        WindowHeight = profile.WindowHeight,
        SillHeight = profile.Sill,
    };

    /// <summary>The kernel rejects touching or crossing holes; such courtyards are dropped rather than the building.</summary>
    private static FacadeGrid? Grid(PlanRing outer, IReadOnlyList<PlanRing> holes, FacadeGridOptions options)
    {
        try
        {
            return FacadeGridGenerator.Generate(new PolygonRings(outer.Points, holes.Select(h => (IReadOnlyList<Vector2>)h.Points).ToArray()), options);
        }
        catch (ArgumentException) when (holes.Count > 0)
        {
            try { return FacadeGridGenerator.Generate(new PolygonRings(outer.Points), options); }
            catch (ArgumentException) { return null; }
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static PlanRing? Skin(PlanRing ring, float d)
    {
        var pts = ring.Offset(d);
        return pts is null ? null : PlanRing.FromPoints(pts, ring.IsHole);
    }

    private static void SetIfWindow(BayKind[] kinds, int i, BayKind kind)
    {
        if (i >= 0 && i < kinds.Length && kinds[i] == BayKind.Window) kinds[i] = kind;
    }
}

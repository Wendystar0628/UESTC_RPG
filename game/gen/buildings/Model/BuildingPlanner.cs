using Campus.Gen.Geometry;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Interior;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Model;

/// <summary>One footprint to build: a whole feature, or one building part of it (WorldFeature.Parts).</summary>
public sealed record BuildingSource(WorldFeature Feature, string Key, PolygonRings Rings, float? Height, int? Storeys, JsonElement? Props);

/// <summary>Turns a footprint into a level-independent plan: rings, wall skin, storeys, heights and the facade bay grid.</summary>
public static class BuildingPlanner
{
    public static readonly DesignKind BuildingKind = new("building");

    /// <summary>
    /// The building footprints of a feature, chosen per part and never by the parent's kind alone: every part whose
    /// kind is building, with its own height and storeys (the aggregate geometry is then not built); a feature
    /// without parts counts when its own kind is building. Multi-polygons yield one footprint per polygon;
    /// keys stay stable (source ID, then "#index").
    /// </summary>
    public static IEnumerable<BuildingSource> Sources(WorldFeature f)
    {
        if (f.Parts.Count > 0)
        {
            foreach (var part in f.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
            {
                if ((part.Props.TryText("kind") ?? f.Kind.Value) != BuildingKind.Value) continue;
                foreach (var (key, rings) in Polygons(part.SourceId, WorldFeatureGeometry.FacadeGeometryOf(part)))
                    yield return new BuildingSource(f, key, rings, part.Height ?? f.Height, part.Storeys ?? f.Storeys, part.Props ?? f.Props);
            }
            yield break;
        }
        if (f.Kind != BuildingKind) yield break;
        foreach (var (key, rings) in Polygons(f.Id.Value, f.Geometry))
            yield return new BuildingSource(f, key, rings, f.Height, f.Storeys, f.Props);
    }

    private static IEnumerable<(string Key, PolygonRings Rings)> Polygons(string key, WorldGeometry geometry)
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

    /// <summary>
    /// The plan of one footprint. Reads the world model only: the terrain pad and the ground in front of the doors
    /// (ITerrain), the precision zones (IFidelityModel) and the open entrances (IInteriorIndex). The feature's facade
    /// preset (WorldFeature.Facade, from <paramref name="presets"/>, default FacadePresets.All) goes over its family profile.
    /// </summary>
    public static BuildingPlan? Plan(BuildingSource src, IWorldModel world, uint worldSeed, IReadOnlyList<FacadeLook>? presets = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var fidelity = world.Fidelity;
        var feature = src.Feature;
        if (!CleanRings(src.Rings, out var outerClean, out var holesClean)) return null;

        var style = FacadeRules.StyleOf(feature.Family, src.Props, feature.Props);
        var profile = ProfileFor(src, outerClean, holesClean, style, worldSeed, presets);
        var seed = StableHash.Hash32(worldSeed, feature.Id.Value, src.Key, "buildings.building");

        // Facade bay grid from the game/gen/interior/core/ plan kernel: canonical rings (mm, fixed start vertex), bays on the glass
        // plane, indices contiguous over all rings. The same call gives the interiors their partition lines.
        var grid = Grid(outerClean, holesClean, GridOptions(profile));
        if (grid is null) return null;
        var outer = PlanRing.FromPoints([.. grid.Footprint.Outer], hole: false);
        var holes = grid.Footprint.Holes.Select(h => PlanRing.FromPoints([.. h], hole: true)).ToList();
        var area = (float)PlanRings.SignedArea(outer.Points) - holes.Sum(h => MathF.Abs((float)PlanRings.SignedArea(h.Points)));
        if (area < 6f) return null;
        var centroid = PlanPolygons.Centroid(new PolygonRings(outer.Points, [.. holes.Select(h => (IReadOnlyList<Vector2>)h.Points)]));

        var (height, storeys, hs) = Heights(src, profile);
        var small = area < 40f || height < 3.2f;

        // Wall skin: the footprint is the glass plane (game/gen/interior/core/), the wall face sits one reveal outside it.
        var reveal = profile.Reveal;
        var (skinOuter, skinHoles) = Skins(outer, holes, reveal);
        var rings = new List<PlanRing> { outer };
        rings.AddRange(holes);
        var skins = new List<PlanRing> { skinOuter };
        skins.AddRange(skinHoles);

        var baseY = PadY(world, feature);
        var wallTopRel = profile.RoofEdge == RoofEdge.Cornice ? height - (small ? 0.3f : 0.5f) : height + (small ? 0.45f : 0.8f);
        var (sill, head) = PlanFacades.StoreyRows(profile, baseY, wallTopRel, storeys, hs);

        // Entrance threshold = ground-floor level (D-180); the plinth is cut at the door, a landing sits in front.
        var threshold = baseY + FacadeRules.FloorAbovePad;
        var doorHead = threshold + Math.Clamp(hs - 1.0f, 2.1f, 2.4f);
        var canopy = doorHead + 0.25f;
        if (small) { doorHead = threshold + Math.Clamp(height - FacadeRules.FloorAbovePad - 0.5f, 1.9f, 2.2f); canopy = doorHead + 0.15f; }

        var (axis, facades, openings, entrances) = PlanFacadeOpenings(world, feature, grid, rings, skins, outer,
            storeys, hs, height, small, baseY, wallTopRel, sill, head, threshold, doorHead, canopy);

        return new BuildingPlan
        {
            Id = feature.Id,
            Key = src.Key,
            Feature = feature,
            Outer = outer,
            Holes = holes,
            SkinOuter = skinOuter,
            SkinHoles = skinHoles,
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
            Grid = grid,
            Facades = facades,
            Openings = openings,
            OpeningZones = PlanFacades.Zones(fidelity.Facades(feature.Id), rings, facades, openings),
            FacadeSegments = fidelity.Facades(feature.Id),
            RoofZone = fidelity.Roof(feature.Id).Zone,
            TotalColumns = grid.Bays.Count,
            Axis = axis,
            Entrances = entrances,
        };
    }

    private static FacadeProfile ProfileFor(BuildingSource src, PlanRing outerClean, IReadOnlyList<PlanRing> holesClean,
        BuildingStyle style, uint worldSeed, IReadOnlyList<FacadeLook>? presets)
    {
        var feature = src.Feature;
        var centroidClean = PlanPolygons.Centroid(new PolygonRings(outerClean.Points, [.. holesClean.Select(h => (IReadOnlyList<Vector2>)h.Points)]));
        var cell = new Vector2(MathF.Floor(centroidClean.X / 160f), MathF.Floor(centroidClean.Y / 160f));
        var cluster = StableHash.Hash32(worldSeed, "buildings.cluster",
            cell.X.ToString(CultureInfo.InvariantCulture), cell.Y.ToString(CultureInfo.InvariantCulture));
        var profile = FacadePresets.Apply(FacadeRules.ProfileFor(style, cluster), FacadePresets.Find(feature.Facade, presets));
        profile = FacadePresets.ApplyColors(profile, feature);
        return profile;
    }

    private static (Vector2 Axis, List<FacadeLayout> Facades, List<Opening[]> Openings, List<PlanEntrance> Entrances)
        PlanFacadeOpenings(IWorldModel world, WorldFeature feature, FacadeGrid grid, IReadOnlyList<PlanRing> rings,
        IReadOnlyList<PlanRing> skins, PlanRing outer, int storeys, float hs, float height, bool small, float baseY,
        float wallTopRel, float[] sill, float[] head, float threshold, float doorHead, float canopy)
    {
        var longest = 0;
        for (var i = 1; i < outer.Count; i++) if (outer.Length[i] > outer.Length[longest]) longest = i;
        var axis = outer.Dir[longest];

        var facades = PlanFacades.Layouts(grid, rings, skins);
        var (entranceFacade, entranceBay) = PlanFacades.SkeletonColumns(facades, outer, longest, axis, storeys, hs);
        var uRange = PlanFacades.OpeningRanges(facades, skins);

        // Doors at the interior index's anchors replace the generic door; its column keeps its windows.
        var doorRise = small ? Math.Clamp(height - FacadeRules.FloorAbovePad - 0.5f, 1.9f, 2.2f) : Math.Clamp(hs - 1.0f, 2.1f, 2.4f);
        var anchors = PlanEntrances.AnchorDoors(world, feature, rings, facades, uRange, k => baseY + FacadeRules.FloorAbovePad + k * hs, storeys,
            baseY + wallTopRel, doorRise);
        if (anchors.Count > 0 && entranceFacade >= 0)
        {
            facades[entranceFacade].Kinds[entranceBay] = BayKind.Window;
            entranceFacade = -1;
            entranceBay = -1;
        }

        var openings = PlanFacades.Openings(facades, uRange, anchors, sill, head, storeys, threshold, doorHead, entranceFacade, entranceBay);
        if (entranceFacade >= 0 && !openings[entranceFacade].Any(o => o.Kind == BayKind.Door))
        {
            facades[entranceFacade].Kinds[entranceBay] = BayKind.Window;
            entranceFacade = -1;
            entranceBay = -1;
        }
        var entrances = PlanEntrances.Entrances(world, facades, openings, skins, anchors, entranceFacade, entranceBay, canopy, small);

        return (axis, facades, openings, entrances);
    }

    /// <summary>The cleaned outer ring and the holes that survive (big enough and inside the outer ring); false when the outer ring is unusable.</summary>
    private static bool CleanRings(PolygonRings source, out PlanRing outerClean, out List<PlanRing> holesClean)
    {
        holesClean = [];
        var cleanPoints = PlanRings.CleanRing(source.Outer, minEdge: .35f, collinearTolerance: .12f, minimumArea: 4f, reversalTolerance: .05f, maxPasses: 64, duplicateDistanceSquared: .0025f);
        var cleaned = cleanPoints is null ? null : PlanRing.FromPoints(cleanPoints, hole: false);
        outerClean = cleaned!;
        if (cleaned is null) return false;
        foreach (var h in source.Holes)
        {
            var holePoints = PlanRings.CleanRing(h, outer: false, minEdge: .35f, collinearTolerance: .12f, minimumArea: 4f, reversalTolerance: .05f, maxPasses: 64, duplicateDistanceSquared: .0025f);
            var ring = holePoints is null ? null : PlanRing.FromPoints(holePoints, hole: true);
            if (ring is null || MathF.Abs((float)PlanRings.SignedArea(ring.Points)) < 8f) continue;
            if (!ring.Points.All(p => PlanRings.InsideByCrossings(cleaned.Points, p))) continue;
            holesClean.Add(ring);
        }
        return true;
    }

    /// <summary>A footprint's roof height above its pad: its own height, else its storeys (one when unknown) at its style's storey height.</summary>
    public static float TopOf(BuildingSource src) =>
        src.Height ?? (src.Storeys ?? 1) * FacadeRules.ProfileFor(FacadeRules.StyleOf(src.Feature.Family, src.Props, src.Feature.Props), 0).StoreyHeight;

    /// <summary>Total height, storey count and storey height: the source's own values, else the family's storey height.</summary>
    private static (float Height, int Storeys, float StoreyHeight) Heights(BuildingSource src, FacadeProfile profile)
    {
        var height = src.Height is > 0.5f and var h0 ? h0 : (src.Storeys ?? 1) * profile.StoreyHeight;
        height = MathF.Max(height, 2.2f);
        var storeys = src.Storeys is > 0 and var s0 ? s0 : Math.Max(1, (int)MathF.Round(height / profile.StoreyHeight));
        storeys = Math.Clamp(storeys, 1, 40);
        if (height / storeys < 2.6f) storeys = Math.Max(1, (int)MathF.Floor(height / 2.6f));
        return (height, storeys, (height - FacadeRules.FloorAbovePad) / storeys);
    }

    /// <summary>The wall-skin rings; when any ring cannot be offset, the skin falls back to the footprint itself.</summary>
    private static (PlanRing Outer, PlanRing[] Holes) Skins(PlanRing outer, IReadOnlyList<PlanRing> holes, float reveal)
    {
        var skinOuter = Skin(outer, reveal);
        var skinHoles = holes.Select(h => Skin(h, reveal)).ToList();
        if (skinOuter is null || skinHoles.Any(h => h is null)) return (outer, [.. holes]);
        return (skinOuter, [.. skinHoles.Select(h => h!)]);
    }

    /// <summary>
    /// The building a door's outside landing stands on, as a deck: another feature's footprint over plan point
    /// <paramref name="p"/> whose roof lies within EntranceRules.DeckAbove of the landing level <paramref name="y"/>.
    /// </summary>
    public static FeatureId? DeckUnder(IWorldModel world, FeatureId self, Vector2 p, float y)
    {
        ArgumentNullException.ThrowIfNull(world);
        var box = new Box3(new Vector3(p.X - 0.5f, -1000f, p.Y - 0.5f), new Vector3(p.X + 0.5f, 1000f, p.Y + 0.5f));
        foreach (var id in world.Spatial.Query(box).OrderBy(i => i.Value, StringComparer.Ordinal))
        {
            if (id == self || !world.Features.TryGetValue(id, out var f)) continue;
            foreach (var src in Sources(f))
                if (PlanRings.InsideByCrossings(src.Rings.Outer, p) && !src.Rings.Holes.Any(h => PlanRings.InsideByCrossings(h, p)) &&
                    MathF.Abs(PadY(world, f) + TopOf(src) - y) <= EntranceRules.DeckAbove) return id;
        }
        return null;
    }

    /// <summary>
    /// The one place the building's base height comes from (D-180): the terrain pad under the footprint,
    /// ITerrain.PadY(feature) (WLD-TERRAIN); level ground at 0 while the map's evaluator is the flat stub.
    /// </summary>
    public static float PadY(IWorldModel world, WorldFeature feature) => world.Terrain.PadY(feature.Id);

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
}

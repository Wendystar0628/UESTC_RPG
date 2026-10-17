using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
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
    /// Kind of one part: its source record's "kind" (Props), else the parent feature's kind — the same rule as the
    /// world model's part filter. An entity whose own kind is, say, poi can still own building parts.
    /// </summary>
    public static string PartKind(WorldFeature f, WorldFeaturePart part) =>
        part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared) &&
        declared.ValueKind == JsonValueKind.String ? declared.GetString()! : f.Kind.Value;

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
                if (PartKind(f, part) != BuildingKind.Value) continue;
                foreach (var (key, rings) in Polygons(part.SourceId, PlanGeometry.FacadeGeometryOf(part)))
                    yield return new BuildingSource(f, key, rings, part.Height ?? f.Height, part.Storeys ?? f.Storeys, part.Props ?? f.Props);
            }
            yield break;
        }
        if (f.Kind != BuildingKind) yield break;
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

        var style = FacadeRules.StyleOf(feature.Family, src.Props, feature.Props);
        var centroidClean = PlanRing.Centroid(outerClean, holesClean);
        var cell = new Vector2(MathF.Floor(centroidClean.X / 160f), MathF.Floor(centroidClean.Y / 160f));
        var cluster = StableHash.Hash32(worldSeed, "wp14.cluster",
            cell.X.ToString(CultureInfo.InvariantCulture), cell.Y.ToString(CultureInfo.InvariantCulture));
        var profile = FacadePresets.Apply(FacadeRules.ProfileFor(style, cluster), FacadePresets.Find(feature.Facade, presets));
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

        var baseY = PadY(world, feature);

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

        // Entrance threshold = ground-floor level (D-180); the plinth is cut at the door, a landing sits in front.
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
        var skins = new List<PlanRing> { skinOuter };
        skins.AddRange(skinHoles.Select(h => h!));
        var uRange = facades.Select(f => OpeningRange(skins[f.Ring], f.Edge)).ToArray();

        // Doors at the interior index's anchors replace the generic door; its column keeps its windows.
        var doorRise = small ? Math.Clamp(height - BuildingPlan.FloorLevel - 0.5f, 1.9f, 2.2f) : Math.Clamp(hs - 1.0f, 2.1f, 2.4f);
        var anchors = AnchorDoors(world, feature, rings, facades, uRange, k => baseY + BuildingPlan.FloorLevel + k * hs, storeys,
            baseY + wallTopRel, doorRise);
        if (anchors.Count > 0 && entranceFacade >= 0)
        {
            facades[entranceFacade].Kinds[entranceBay] = BayKind.Window;
            entranceFacade = -1;
            entranceBay = -1;
        }

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
        if (entranceFacade >= 0 && !openings[entranceFacade].Any(o => o.Kind == BayKind.Door))
        {
            facades[entranceFacade].Kinds[entranceBay] = BayKind.Window;
            entranceFacade = -1;
            entranceBay = -1;
        }

        // Entrances: the generic door at the building's floor with its landing, or the anchor doors.
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
            Grid = grid,
            Facades = facades,
            Openings = openings,
            OpeningZones = fidelity is null ? [] : Zones(fidelity.Facades(feature.Id), rings, facades, openings),
            RoofZone = fidelity?.Roof(feature.Id).Zone ?? FidelityZone.K4,
            TotalColumns = column,
            Axis = axis,
            Entrances = entrances,
        };
    }

    /// <summary>An anchor door before the openings exist: facade, bay, storey row and its clear opening in skin u and Y.</summary>
    private readonly record struct AnchorDoor(int Facade, int Bay, int Row, float U0, float U1, float Y0, float Y1, string Id, bool Deck);

    /// <summary>
    /// The doors this footprint opens for the open entrances its feature has in the interior index (WLD-INTERIORS): on
    /// the facade edge the anchor stands on and faces out of, in the bay under its door centre (centred on the anchor unless
    /// the bay centre is within EntranceRules.BaySnap), at the storey of its floor. An anchor is left alone (to another part
    /// of the feature, or to the zone pack that builds that door) when no edge of this footprint takes it, its outside lies in
    /// another footprint, its floor leaves no door height under the next slab, or the door cannot stand within
    /// EntranceRules.AnchorTolerance of it. The door top is the anchor's HeadY when the index gives one (else the family's
    /// door rise), never above the next slab. Deck: the outside landing stands well above the terrain (a porch or deck).
    /// </summary>
    private static List<AnchorDoor> AnchorDoors(IWorldModel world, WorldFeature feature, IReadOnlyList<PlanRing> rings,
        IReadOnlyList<FacadeLayout> facades, IReadOnlyList<(float Min, float Max)> uRange, Func<int, float> floorY, int storeys,
        float wallTop, float doorRise)
    {
        var doors = new List<AnchorDoor>();
        if (world.Interiors.Get(feature.Id) is not { } entry) return doors;
        foreach (var e in entry.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            var yaw = e.YawDeg * (float.Pi / 180f);
            var facing = new Vector2(DetMath.Sin(yaw), -DetMath.Cos(yaw));
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
            if (rise < MathF.Min(EntranceRules.MinDoorHeight, doorRise)) continue;

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

    /// <summary>Where openings may sit on a skin edge: clear of its corner chamfers by 0.2 m.</summary>
    private static (float Min, float Max) OpeningRange(PlanRing skin, int edge) =>
        ((Prims.Chamferable(skin, edge, BuildingPlan.WallChamfer) ? BuildingPlan.WallChamfer : 0f) + 0.2f,
            skin.Length[edge] - (Prims.Chamferable(skin, (edge + 1) % skin.Count, BuildingPlan.WallChamfer) ? BuildingPlan.WallChamfer : 0f) - 0.2f);

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
            foreach (var src in Sources(f))
                if (PlanRing.Contains(src.Rings.Outer, p) && !src.Rings.Holes.Any(h => PlanRing.Contains(h, p)) &&
                    PadY(world, f) + (src.Height ?? (src.Storeys ?? 1) * 3f) > y + EntranceRules.DeckAbove) return true;
        }
        return false;
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
                if (PlanRing.Contains(src.Rings.Outer, p) && !src.Rings.Holes.Any(h => PlanRing.Contains(h, p)) &&
                    MathF.Abs(PadY(world, f) + (src.Height ?? (src.Storeys ?? 1) * 3f) - y) <= EntranceRules.DeckAbove) return id;
        }
        return null;
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

    /// <summary>
    /// Zone of each opening from the facade segments of the exported fidelity data (ENG-ARCH: one or more segments
    /// per footprint edge, A → B along the ring). An opening takes the segment its centre lies on (parallel, within
    /// OpeningRules.ZoneMatchDistance of the footprint line); row 0 takes the segment's ground band. No segment: K3.
    /// </summary>
    private static FidelityZone[][] Zones(IReadOnlyList<FacadeSegment> segments, IReadOnlyList<PlanRing> rings,
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

    private static void SetIfWindow(BayKind[] kinds, int i, BayKind kind)
    {
        if (i >= 0 && i < kinds.Length && kinds[i] == BayKind.Window) kinds[i] = kind;
    }
}

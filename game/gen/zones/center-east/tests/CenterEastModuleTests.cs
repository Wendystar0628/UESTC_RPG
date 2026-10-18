using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using Campus.Gen.Zones.CenterEast.Framework;
using Campus.Gen.Zones.CenterEast.Pinxue;
using Campus.Gen.Zones.CenterEast.Research;
using Campus.Gen.Zones.CenterEast.Wenke;
using Campus.Gen.Tests.Support;
using GdUnit4;
using M = Campus.Gen.Zones.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Zones.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Zones.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Zones.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Zones.CenterEast.Tests;
using static GdUnit4.Assertions;

[TestSuite]
public sealed class CenterEastModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void WholeSourcesReproduceProductionAndKeepClaimedPlans()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            var traces = module.TraceSources(ctx, lod).ToArray();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage("source IDs are unique").IsTrue();
            foreach (var claim in module.Claims.OfType<FeatureClaim>())
                AssertThat(traces.Any(t => t.Feature == claim.Feature && t.Output.Meshes.Count > 0 && t.Colliders.Count > 0 && t.OccupiedContours.Count > 0)).OverrideFailureMessage($"{lod}: claimed source {claim.Feature} carries production output, collision and solid contours").IsTrue();
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => new TileKey((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            var built = cells.Select(t => module.Build(ctx, t, lod)).ToArray();
            AssertThat(Triangles(traces.SelectMany(t => t.Output.Meshes)).SequenceEqual(Triangles(built.SelectMany(o => o.Meshes)))).OverrideFailureMessage($"{lod}: source triangles reproduce tiled production").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal)
                .SequenceEqual(built.SelectMany(o => o.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal))).OverrideFailureMessage("source parts reproduce production").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Instances).SequenceEqual(built.SelectMany(o => o.Instances))).OverrideFailureMessage("source instances reproduce production").IsTrue();
            string Dump(Collider collider) => System.Text.Json.JsonSerializer.Serialize<Collider>(collider, ContractJson.Options);
            AssertThat(traces.SelectMany(t => t.Colliders).Select(Dump).Order(StringComparer.Ordinal)
                .SequenceEqual(module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Select(Dump).Order(StringComparer.Ordinal))).OverrideFailureMessage("source colliders reproduce tiled production").IsTrue();
            foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
            {
                var own = site.Footprints(ctx.World);
                var contours = traces.Single(t => t.Feature == new FeatureId(site.Entity)).OccupiedContours;
                AssertThat(contours.Zip(site.GridBlocks(own).Select(b => b.Ring).Append(own.Hall)).All(pair => pair.First.Outer.SequenceEqual(pair.Second)) &&
                    contours.Count == site.GridBlocks(own).Count() + 1).OverrideFailureMessage("research sources use their generated blocks and hall, excluding neighbours").IsTrue();
            }
            var wenke = traces.Single(t => t.Feature == new FeatureId(K.Entity));
            AssertThat(wenke.OccupiedContours.Select(r => r.Outer).SequenceEqual(WenkeSite.Footprints(ctx.World).Blocks.SelectMany(b => b.Outlines))).OverrideFailureMessage("Wenke keeps the actual clipped and rounded storeys").IsTrue();
            var frame = traces.Single(t => t.Feature == new FeatureId(P.Complex));
            foreach (var box in PinxueFrameSite.Bridges(ctx.World))
                AssertThat(frame.OccupiedContours.Any(r => r.Outer.SequenceEqual(box.Frame.Rect(box.X0, box.Z0, box.X1, box.Z1)))).OverrideFailureMessage("the frame uses its actual bridge plans").IsTrue();
        }

        static IEnumerable<string> Triangles(IEnumerable<GeometryBundle> meshes)
        {
            var triangles = new List<string>();
            foreach (var mesh in meshes)
                for (var i = 0; i < mesh.Index.Length; i += 3)
                {
                    string Vertex(int v) => string.Join(",", mesh.Position.Skip(v * 3).Take(3).Concat(mesh.Normal.Skip(v * 3).Take(3)).Select(BitConverter.SingleToInt32Bits));
                    triangles.Add($"{mesh.Material}/{Vertex(mesh.Index[i])}/{Vertex(mesh.Index[i + 1])}/{Vertex(mesh.Index[i + 2])}");
                }
            return triangles.Order(StringComparer.Ordinal);
        }
    }

    [TestCase]
    public void OverlayLoadsAndRegistersTheForecourtAdditions()
    {
        var world = World();
        var c = world.Features[new("qsh:relation:11043354")];
        AssertThat(c.Height is 19.0f && c.Storeys == 5).OverrideFailureMessage("C wing raised to 19 m, 5 storeys").IsTrue();
        AssertThat(c.Parts.All(p => p.Height is 19.0f)).OverrideFailureMessage("every C part follows the HeightOp").IsTrue();
        foreach (var id in new[] { "design:center-east-pinxue-fountain", "design:center-east-pinxue-tree-pits", "design:center-east-pinxue-round-bed",
                     "design:center-east-pinxue-ramp-seatwall-north", "design:center-east-pinxue-ramp-seatwall-south", "design:center-east-pinxue-arc-end",
                     "design:center-east-riverside-walk" })
            AssertThat(world.Features.ContainsKey(new(id))).OverrideFailureMessage($"{id} is registered").IsTrue();
    }

    [TestCase]
    public void ClaimsNameExistingFeaturesAndCoverTheirBuildings()
    {
        var world = World();
        var module = new CenterEastModule();
        var claims = module.Claims.OfType<FeatureClaim>().ToDictionary(c => c.Feature.Value, StringComparer.Ordinal);
        AssertThat(module.Claims.Count == 5 && claims.Count == 5).OverrideFailureMessage("品学楼's frame entity, the three research buildings and 文科楼群 are claimed, once each").IsTrue();
        foreach (var claim in claims.Values)
        {
            AssertThat(world.Features.ContainsKey(claim.Feature)).OverrideFailureMessage($"claimed feature {claim.Feature.Value} exists").IsTrue();
            AssertThat((claim.Aspects & Aspects.Massing) != 0).OverrideFailureMessage("claims cover massing so WP-14 skips the extrusion").IsTrue();
        }
        string[] Parts(string id) => world.Features[new(id)].Parts.Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        AssertThat(Parts(P.Complex).SequenceEqual([P.FrameBar, P.BridgeBC, P.BridgeAC])).OverrideFailureMessage("the 品学楼 claim holds exactly the frame bar and the two bridges").IsTrue();
        // Every part of a claimed research building is built by its site: blocks as gridded blocks, the hall as the lens hall.
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            AssertThat(claims.ContainsKey(site.Entity)).OverrideFailureMessage($"{site.Id} claims its entity").IsTrue();
            var hall = site.Entity switch { R.KeA => R.KeAHall, R.KeB => R.KeBHall, _ => R.KeCHall };
            AssertThat(Parts(site.Entity).SequenceEqual(site.Blocks.Select(b => b.Source).Append(hall).OrderBy(s => s, StringComparer.Ordinal))).OverrideFailureMessage($"{site.Id} builds every part of {site.Entity}").IsTrue();
        }
        // 文科楼群: every building part is one of the site's blocks; the only other part is the site outline itself.
        var wenke = world.Features[new(K.Entity)].Parts;
        AssertThat(wenke.Where(p => p.SourceId != "osm:way:883187548").Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal)
            .SequenceEqual(K.Blocks.Select(b => b.Part).OrderBy(s => s, StringComparer.Ordinal))).OverrideFailureMessage("文科楼群's five buildings are the site's five blocks").IsTrue();
    }

    [TestCase]
    public void ResearchRegistrationsMatchWhatIsBuilt()
    {
        var world = World();
        IReadOnlyList<Vector2> Outline(string id) => ((PolygonGeometry)world.Features[new(id)].Geometry).Rings.Outer;
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs((float)PlanGeometry.SignedArea(ring));
        foreach (var (id, back, sagitta) in new[] { ("design:center-east-ke-a-lens-hall", R.KeAHallBack, R.KeAHallSagitta),
                     ("design:center-east-ke-b-lens-hall", R.KeBHallBack, R.KeBHallSagitta), ("design:center-east-ke-c-lens-hall", R.KeCHallBack, R.KeCHallSagitta) })
        {
            var built = Area(ResearchSite.HallRing(back, sagitta, 16));
            AssertThat(MathF.Abs(Area(Outline(id)) - built) < 0.03f * built).OverrideFailureMessage($"{id} outline matches the built hall").IsTrue();
        }
        AssertThat(Area(Outline("design:center-east-ke-c-roof-garden")) <= 300f).OverrideFailureMessage("the roof garden stays within 300 m²").IsTrue();
    }

    [TestCase]
    public void ResearchBlocksStandFiveStoreysWithOneShutEntranceEach()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var top = ResearchSite.Style.Top;
        AssertThat(MathF.Abs(top - (R.GroundStorey + 4 * R.Storey + 1.3f)) < 0.01f).OverrideFailureMessage($"five storeys to the coping, got {top}").IsTrue();
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            var plan = site.Footprints(ctx.World);
            var walls = colliders.OfType<WallCollider>().Where(w => plan.Blocks.Any(r => NearRing(r, (w.A + w.B) * 0.5f))).ToArray();
            AssertThat(walls.Any(w => w.Tag == SurfaceTag.Wall && MathF.Abs(w.Y1 - top) < 0.01f)).OverrideFailureMessage($"{site.Id} walls reach the coping").IsTrue();
            var door = site.BlockDoor is { } d ? ResearchSite.Frame.P(d.X, d.Y) : site.HallApex().Apex;
            AssertThat(colliders.OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, door) < 3f) is >= 1 and <= 2).OverrideFailureMessage($"{site.Id} has its shut entrance at the door").IsTrue();
        }

        static bool NearRing(IReadOnlyList<Vector2> ring, Vector2 p)
        {
            for (var i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                var t = Math.Clamp(Vector2.Dot(p - a, b - a) / Vector2.Dot(b - a, b - a), 0f, 1f);
                if (Vector2.Distance(a + (b - a) * t, p) < 1f) return true;
            }
            return false;
        }
    }

    [TestCase]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        AssertThat(module.Tiles(ctx).Any()).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var items = module.ItemsFor(ctx, lod);
            AssertThat(items.Count > 0).OverrideFailureMessage($"{lod} has items").IsTrue();
            var size = 64 << (int)lod;
            var cells = items.Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("bundle vertex limit").IsTrue();
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            AssertThat(meshes > 0).OverrideFailureMessage($"{lod} emits meshes").IsTrue();
            AssertThat(items.Count(i => i.Part is not null) == parts).OverrideFailureMessage($"{lod}: every placed part is emitted exactly once").IsTrue();
            // The entrance kit (mat, open sign, door lamp) is placed by WP-13 at every open entrance, never by the zone.
            string[] kit = [Contracts.Parts.PartCatalog.EntranceMat, Contracts.Parts.PartCatalog.OpenSign, Contracts.Parts.PartCatalog.EntranceLamp];
            AssertThat(!items.Any(i => i.Part is { } p && kit.Contains(p.Part.Value))).OverrideFailureMessage($"{lod}: no entrance kit placed by the zone").IsTrue();
        }
    }

    [TestCase]
    public void HeaviestCellsStayInsideTheDeclaredBudget()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        foreach (var (lod, limit) in new[] { (LodLevel.L0, module.Budget.TrianglesL0), (LodLevel.L1, module.Budget.TrianglesL1) })
        {
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            foreach (var (x, z) in cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
                AssertThat(triangles <= limit).OverrideFailureMessage($"{lod} cell ({x}, {z}) has {triangles} triangles, budget {limit}").IsTrue();
                AssertThat(output.Meshes.Select(m => m.Material).Distinct().Count() <= module.Budget.DrawCalls).OverrideFailureMessage($"{lod} cell ({x}, {z}) material count").IsTrue();
            }
        }
    }

    [TestCase]
    public void BuildIsDeterministic()
    {
        var a = Bytes(Context());
        var b = Bytes(Context());
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("two worlds give the same bytes").IsTrue();
    }

    [TestCase]
    public void PassageAndBridgesKeepTheirClearHeights()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var gate = PinxueFrameSite.GateOf(ctx.World);
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every collider fills its foot material").IsTrue();
        var boxes = colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y > 1f).ToArray();
        AssertThat(boxes.Length == 3).OverrideFailureMessage($"the beam and the two bridges are lifted boxes, got {boxes.Length}").IsTrue();
        AssertThat(boxes.All(b => b.At.M42 - b.Half.Y >= gate.BridgeBottom - 0.01f)).OverrideFailureMessage("nothing walls off the passage or the paths under the bridges").IsTrue();
        var frame = gate.F.P(gate.Axis, -gate.Depth * 0.5f);
        var columns = colliders.OfType<BoxCollider>().Count(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y < 0.01f && b.Half.Y * 2 >= gate.BeamBottom - 0.01f
            && Vector2.Distance(new(b.At.M41, b.At.M43), frame) < 25f);
        AssertThat(columns == P.ColumnX.Length).OverrideFailureMessage("the four columns reach the beam soffit").IsTrue();
        // The beam leaves the two-storey passage open between the columns.
        var beam = boxes.OrderByDescending(b => b.At.M42).First();
        AssertThat(MathF.Abs(beam.At.M42 - beam.Half.Y - gate.BeamBottom) < 0.01f).OverrideFailureMessage("the beam soffit sits at two storeys").IsTrue();
    }

    [TestCase]
    public void RampWallsStepWithTheGround()
    {
        var world = World();
        var frame = PinxueFrameSite.GateOf(world).F;
        var walls = PinxueForecourtSite.RampWalls(world);
        // Flat stub: each wall is one run at seat height.
        foreach (var (x, z0, z1) in walls)
        {
            var runs = PinxueForecourtSite.RampRuns(_ => 0f, frame, x, z0, z1);
            AssertThat(runs.Count == 1 && MathF.Abs(runs[0].Top - P.SeatHeight) < 0.01f).OverrideFailureMessage("flat ground gives one continuous wall").IsTrue();
        }
        // The designed 5 % ramp along the frame's z: the wall steps once per segment, each top above the ground under it.
        float Ramp(Vector2 p) => Math.Clamp((walls[0].Z1 - frame.Local(p).Y) * 0.05f, 0f, 1.5f);
        var stepped = PinxueForecourtSite.RampRuns(Ramp, frame, walls[0].X, walls[0].Z0, walls[0].Z1);
        AssertThat(stepped.Count == 5).OverrideFailureMessage($"the ramp wall steps in five runs, got {stepped.Count}").IsTrue();
        for (var i = 1; i < stepped.Count; i++)
            AssertThat(stepped[i - 1].Top - stepped[i].Top is > 0.25f and < 0.35f).OverrideFailureMessage("each step drops about 0.3 m toward the foot").IsTrue();
    }

    [TestCase]
    public void FountainTiersMatchTheRegisteredWaterLevels()
    {
        var world = World();
        var props = world.Features[new("design:center-east-pinxue-fountain")].Props!.Value;
        var registered = props.GetProperty("tiers").EnumerateArray().ToArray();
        var tiers = PinxueForecourtSite.FountainTiers();
        AssertThat(registered.Length == tiers.Count).OverrideFailureMessage("one registered tier per built tier").IsTrue();
        static Vector2 Pair(System.Text.Json.JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle());
        var axis = Pair(props.GetProperty("axis"));
        AssertThat(Vector2.Distance(axis, PinxueForecourtSite.FountainFrame(world).U) < 0.01f).OverrideFailureMessage("the registered long axis runs along the bar").IsTrue();
        for (var i = 0; i < tiers.Count; i++)
        {
            var (half, coping, rim, water) = tiers[i];
            AssertThat(MathF.Abs(registered[i].GetProperty("rimAbove").GetSingle() - rim) < 0.01f).OverrideFailureMessage($"tier {i} rim").IsTrue();
            AssertThat(MathF.Abs(registered[i].GetProperty("waterAbove").GetSingle() - water) < 0.01f).OverrideFailureMessage($"tier {i} water").IsTrue();
            AssertThat(Vector2.Distance(Pair(registered[i].GetProperty("half")), half) < 0.01f).OverrideFailureMessage($"tier {i} stone extent").IsTrue();
            AssertThat(Vector2.Distance(Pair(registered[i].GetProperty("waterHalf")), half - new Vector2(coping)) < 0.01f).OverrideFailureMessage($"tier {i} water extent").IsTrue();
            AssertThat(water < rim).OverrideFailureMessage($"tier {i} water stays below its rim").IsTrue();
            if (i > 0) AssertThat(half.X < tiers[i - 1].Half.X - tiers[i - 1].Coping && half.Y < tiers[i - 1].Half.Y - tiers[i - 1].Coping && rim > tiers[i - 1].Rim).OverrideFailureMessage($"tier {i} steps up inside the pool below").IsTrue();
        }
        AssertThat(tiers[^1].Rim <= 0.9f).OverrideFailureMessage("the cascade stays low, under 0.9 m").IsTrue();
    }

    [TestCase]
    public void TerrainOverlayParsesAndReferencesRealFeatures()
    {
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "terrain", "center-east.terrain.json")));
        AssertThat(file.Zone.Value == "center-east" && file.Owner.Value == "WP-21").OverrideFailureMessage("file zone and owner").IsTrue();
        var world = World();
        var roads = world.Network.Roads.Select(r => r.Id).ToHashSet();
        foreach (var op in file.Ops)
            switch (op)
            {
                case TerrainPadOp pad: AssertThat(world.Features.ContainsKey(pad.Feature)).OverrideFailureMessage($"{op.Id}: pad feature {pad.Feature.Value} exists").IsTrue(); break;
                case TerrainCorridorOp corridor:
                    AssertThat(roads.Contains(corridor.Road)).OverrideFailureMessage($"{op.Id}: road {corridor.Road.Value} exists").IsTrue();
                    foreach (var step in corridor.Steps)
                        AssertThat(world.Features.ContainsKey(step.Structure)).OverrideFailureMessage($"{op.Id}: stair {step.Structure.Value} is registered").IsTrue();
                    break;
                case TerrainPlatformOp or TerrainSlopeOp or TerrainTerraceOp: break;
                default: throw new InvalidOperationException($"{op.Id}: unexpected operation for 21a");
            }
        AssertThat(file.Ops.OfType<TerrainPadOp>().Count(p => p.PadY is 1.5) == 6).OverrideFailureMessage("the three wings, the frame entity and two plazas sit on the +1.5 platform").IsTrue();
    }

    [TestCase]
    public void MonumentsStandAtTheirPointsAndLeaveTheDoorsClear()
    {
        var world = World();
        Vector2 Point(string id) => world.Features[new(id)].Geometry is PointGeometry p ? new(p.Position.X, p.Position.Z) : throw new InvalidOperationException($"{id} is a point");
        // 林为干像 moves less than 4 m off its OSM point (a minor adjustment with no reshape, MAP-QSH-CENTER-EAST "定位").
        AssertThat(Vector2.Distance(Point(M.Lin), M.LinAt) < 4f).OverrideFailureMessage($"{M.Lin} stays within 4 m of its OSM point").IsTrue();
        AssertThat(StatueLawnSite.Island(world) is { } island && PlanGeometry.InsideByCrossings(island, M.LinAt)).OverrideFailureMessage("林为干像 stands on the plaza's tree island").IsTrue();
        var maxwellAt = StatueLawnSite.MaxwellFrame(world).Origin;
        var stoneAt = StatueLawnSite.StoneFrame(world).Origin;
        foreach (var at in new[] { maxwellAt, stoneAt })
            AssertThat(PlanGeometry.InsideByCrossings(StatueLawnSite.ApronRing(world), at)).OverrideFailureMessage($"{at} stands on the apron").IsTrue();
        // Every inscription line lies on the dressed face with stone round it.
        foreach (var (x, y, length) in StatueLawnSite.StoneLines())
            foreach (var (fx, fy) in new[] { (x - length * 0.5f, y), (x + length * 0.5f, y), (x - length * 0.5f, y + M.StoneLineHeight), (x + length * 0.5f, y + M.StoneLineHeight) })
                AssertThat(M.Stone.OnDressedFace(fx, fy, 0.04f)).OverrideFailureMessage($"inscription corner ({fx}, {fy}) is on the dressed face").IsTrue();
        // Colliders: filled foot, a top at every pedestal, and nothing in front of the three research doors.
        var ctx = Context();
        var module = new CenterEastModule([new StatueLawnSite(), new ResearchSteleSite()]);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every monument collider fills its foot material").IsTrue();
        var boxes = colliders.OfType<BoxCollider>().ToArray();
        foreach (var (at, top) in new[] { (M.LinAt, StatueLawnSite.LinPedestalTop), (maxwellAt, M.MaxwellPedestalTop), (stoneAt, M.Stone.Height - 0.08f) })
            AssertThat(boxes.Any(b => Vector2.Distance(new(b.At.M41, b.At.M43), at) < 0.6f && MathF.Abs(b.At.M42 + b.Half.Y - top) < 0.02f)).OverrideFailureMessage($"the box at {at} reaches its top {top}").IsTrue();
        var doors = new[] { ResearchSite.Frame.P(R.KeADoor.X, R.KeADoor.Y), ResearchSite.Frame.P(R.KeBDoor.X, R.KeBDoor.Y), new KeCSite().HallApex().Apex };
        var clear = 2.5f + R.PortalDepth + 3 * R.TreadDepth;
        foreach (var b in boxes)
            AssertThat(doors.All(d => Vector2.Distance(new(b.At.M41, b.At.M43), d) > clear + b.Half.X + b.Half.Z)).OverrideFailureMessage("monuments keep 2.5 m clear outside the doors and their steps").IsTrue();
    }

    [TestCase]
    public void SitesStayInsideTheCenterEastZone()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            AssertThat(item.Anchor.X is > -120 and < 1000 && item.Anchor.Y is > -800 and < 600).OverrideFailureMessage($"item at {item.Anchor} is in the center-east zone").IsTrue();
        foreach (var probe in module.Probes)
            AssertThat(world(probe.Feature)).OverrideFailureMessage($"probe {probe.Id} names an existing feature").IsTrue();

        bool world(FeatureId id) => ctx.World.Features.ContainsKey(id);
    }

    [TestCase]
    public void WenkeStandsTerracedWithOneShutDoorAndAClearForecourt()
    {
        var ctx = Context();
        var plan = WenkeSite.Footprints(ctx.World);
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs((float)PlanGeometry.SignedArea(ring));
        WenkeSite.Block Block(string part) => plan.Blocks.Single(b => b.Part == part);
        AssertThat(Block(K.Tower).Storeys == 15 && WenkeSite.Floor(15) + K.Parapet > 58f).OverrideFailureMessage("the tower stands fifteen storeys, about 60 m").IsTrue();
        // The wings step back toward their tips: each top storey is smaller than its ground storey, with planted terraces between.
        foreach (var part in new[] { K.South, K.West, K.North })
        {
            var block = Block(part);
            AssertThat(Area(block.Outlines[^1]) < 0.8f * Area(block.Outlines[0])).OverrideFailureMessage($"{part} steps back toward its tip").IsTrue();
            AssertThat(Enumerable.Range(1, block.Storeys - 1).Any(k => WenkeSite.Terraces(plan, block, k).Any())).OverrideFailureMessage($"{part} has terraces").IsTrue();
        }
        AssertThat(Block(K.Tower).Rows.Skip(K.PanelStripFrom).All(r => r.Any(c => c.Kind == WenkeSite.CellKind.Panel))).OverrideFailureMessage("the yellow-green strip runs up the tower").IsTrue();
        AssertThat(plan.Blocks.All(b => b.Rows.All(r => r.Count(c => c.Kind == WenkeSite.CellKind.Window) > 0))).OverrideFailureMessage("every storey of every block shows glass").IsTrue();
        // One shut entrance at the atrium porch; nothing but paving within 2.5 m outside it.
        var module = new CenterEastModule([new WenkeSite(), new WenkeForecourtSite()]);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every 文科楼 collider fills its foot material").IsTrue();
        var shut = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.DoorClosed).ToArray();
        AssertThat(shut.Length == 1 && Vector2.Distance((shut[0].A + shut[0].B) * 0.5f, WenkeSite.Door(ctx.World).Origin) < K.PorchDepth + 0.5f).OverrideFailureMessage("one shut glass entrance at the atrium porch").IsTrue();
        var outside = WenkeSite.Door(ctx.World).P(0, K.PorchDepth);
        foreach (var box in colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Prop))
            AssertThat(Vector2.Distance(new(box.At.M41, box.At.M43), outside) > 2.5f + box.Half.X + box.Half.Z).OverrideFailureMessage("nothing stands within 2.5 m outside the door").IsTrue();
        foreach (var post in colliders.OfType<PostCollider>())
            AssertThat(Vector2.Distance(post.C, outside) > 2.5f + post.R).OverrideFailureMessage("no stake within 2.5 m outside the door").IsTrue();
        AssertThat(PlanGeometry.InsideByCrossings(Block(K.Hub).Outlines[^1], WenkeSite.AtriumOf(ctx.World).Frame.Origin)).OverrideFailureMessage("the vault stands on the hub").IsTrue();
        // The emblem sits on the stone's dressed face with stone round it.
        foreach (var (x, y) in new[] { (-F.EmblemRadius, F.EmblemY), (F.EmblemRadius, F.EmblemY), (0f, F.EmblemY - F.EmblemRadius), (0f, F.EmblemY + F.EmblemRadius) })
            AssertThat(F.Stone.OnDressedFace(x, y, 0.03f)).OverrideFailureMessage($"emblem edge ({x}, {y}) is on the dressed face").IsTrue();
    }

    private static byte[] Bytes(BuildContext ctx)
    {
        var module = new CenterEastModule(CenterEastSites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in module.Tiles(ctx))
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var v in mesh.Position) writer.Write(v);
                foreach (var v in mesh.Normal) writer.Write(v);
                foreach (var v in mesh.Index) writer.Write(v);
            }
            foreach (var part in output.Parts) writer.Write(part.Part.Value + part.At.ToString());
        }
        return stream.ToArray();
    }

    private static BuildContext Context()
    {
        // The center-east sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = null!, WorldSeed = 1 };
    }

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    [TestCase]
    public void EveryFacadeClaimDeclaresItsWindowGrids()
    {
        // 品学楼's own parts are the frame beam and the two link bridges (ribbon windows); the three research buildings declare
        // one grid per gridded block. Every grid has cells, and the envelope view declares the same set.
        var world = World();
        var layouts = new CenterEastModule().DescribeWindowLayouts(world, 1).ToDictionary(l => l.Feature.Value, l => l.Grids, StringComparer.Ordinal);
        string Keys(string feature) => string.Join(",", layouts[feature].Select(g => g.Key));
        AssertThat(layouts.Count == 5).OverrideFailureMessage($"five Facade-claimed features declare layouts, got {layouts.Count}").IsTrue();
        AssertThat(Keys(P.Complex) == "beam,bridge-ac,bridge-bc" && layouts[P.Complex].All(g => g.Rows == 2)).OverrideFailureMessage($"品学楼 bridges: {Keys(P.Complex)}").IsTrue();
        AssertThat(Keys(R.KeA) == "main,west" && Keys(R.KeB) == "attic,main,middle,west" && Keys(R.KeC) == "bar,link").OverrideFailureMessage("research grids by block").IsTrue();
        AssertThat(layouts.Values.SelectMany(g => g).All(g => g.Columns > 0 && g.Rows > 0)).OverrideFailureMessage("every grid has cells").IsTrue();
        AssertThat(layouts[R.KeB].Single(g => g.Key == "attic").Rows == 1 && layouts[R.KeA].All(g => g.Rows == R.Storeys)).OverrideFailureMessage("five storeys, one attic row").IsTrue();
        AssertThat(Keys(K.Entity) == "hub,north,south,tower,west" && layouts[K.Entity].Single(g => g.Key == "tower").Rows == 15).OverrideFailureMessage($"文科楼 grids by block: {Keys(K.Entity)}").IsTrue();
        var envelope = new CenterEastModule().DescribeWindowLayouts(FakeWorld.Enveloped(world), 1);
        AssertThat(envelope.All(l => l.Grids.SequenceEqual(layouts[l.Feature.Value])) && envelope.Count == layouts.Count).OverrideFailureMessage("the envelope view declares the same grids").IsTrue();
    }

    [TestCase]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = World();
        var module = new CenterEastModule();
        // A stand-in for WP-14's allocation: the declared grids in canonical order after a block that shifts every base.
        var specs = module.DescribeWindowLayouts(world, 1).SelectMany(l => l.Grids.Select(g => (l.Feature, g)))
            .Prepend((new FeatureId("a-earlier"), new WindowGridSpec("block", 37, 3)))
            .OrderBy(p => p.Item1.Value, StringComparer.Ordinal).ThenBy(p => p.Item2.Key, StringComparer.Ordinal);
        var allocations = new List<WindowAllocation>();
        var next = 0;
        foreach (var (feature, grid) in specs)
        {
            allocations.Add(new(feature, grid.Key, next, grid.Columns, grid.Rows));
            next += grid.Columns * grid.Rows;
        }
        var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = Descriptor(), WorldSeed = 1, WindowNumbering = new(new("qingshuihe"), new string('a', 64), allocations) };
        var exceptionThrown = false;
        try { module.DescribeWindows(Context()); }
        catch (InvalidOperationException) { exceptionThrown = true; }
        AssertThat(exceptionThrown).OverrideFailureMessage("positions need the numbering snapshot").IsTrue();

        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        declared.AddRange(metadata.Windows.Select(w => w.Id));
        AssertThat(declared.Count == declared.Distinct().Count()).OverrideFailureMessage("every window is declared once").IsTrue();
        AssertThat(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier"))).OverrideFailureMessage("declared IDs lie in the zone's grids").IsTrue();
        AssertThat(metadata.Windows.Count > 0 && metadata.Facades.Count > 0).OverrideFailureMessage("bridges give loose windows, research blocks facade runs").IsTrue();
        // The portal cells under the entrance frames carry no window.
        AssertThat(metadata.Facades.Count(f => f.Skip.Count > 0) == 2).OverrideFailureMessage("科 A and 科 B each skip their portal cells").IsTrue();

        HashSet<int> Glass(LodLevel lod)
        {
            var ids = new HashSet<int>();
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
                foreach (var mesh in module.Build(ctx, new(x, z), lod).Meshes.Where(m => m.Uv2 is not null))
                    for (var v = 0; v < mesh.Uv2!.Length / 2; v++)
                    {
                        var columns = (int)MathF.Floor(mesh.Uv2[2 * v + 1] / 32f);
                        ids.Add((int)mesh.Uv2[2 * v] + (int)MathF.Floor(mesh.Uv![2 * v + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * v]));
                    }
            return ids;
        }
        var near = Glass(LodLevel.L0);
        AssertThat(near.SetEquals(declared)).OverrideFailureMessage($"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})").IsTrue();
        AssertThat(Glass(LodLevel.L3).SetEquals(declared)).OverrideFailureMessage("coarse glass keeps the same numbers").IsTrue();
    }

    [TestCase]
    public void WindowTablePublishesWithTheCenterEastDeclarations()
    {
        // The end-to-end reach before WP-02's orchestration: WP-14's provider allocates the whole real map (every authored overlay)
        // with the zone's real declarations, then the zone describes its windows from the published snapshot.
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        var world = WorldModelLoader.Load(new("qingshuihe"), files).World;
        var module = new CenterEastModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)FakeWorld.Enveloped(world)) })
        {
            WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-21") : null;
            var prepass = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner };
            var layouts = module.DescribeWindowLayouts(view, 1);
            var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
            var ours = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            AssertThat(ours.Length == layouts.Sum(l => l.Grids.Count) && ours.All(a => a.Columns > 0 && a.Rows > 0)).OverrideFailureMessage($"{name}: every center-east grid is allocated with cells ({ours.Length} of {layouts.Sum(l => l.Grids.Count)})").IsTrue();
            AssertThat(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity).OverrideFailureMessage($"{name}: the table fits ({snapshot.WindowCount})").IsTrue();
            var metadata = module.DescribeWindows(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            AssertThat(ids.Length > 0 && ids.All(id => ours.Any(a => id >= a.BaseId && id < a.BaseId + a.Count))).OverrideFailureMessage($"{name}: the zone's windows lie in its own grids").IsTrue();
        }
    }

    private static MapDescriptor Descriptor()
    {
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        return new MapDescriptor(new MapId("qingshuihe"), new LocalizedName("清水河", "Qingshuihe"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero, new Box3(new(-1280f, -28f, -1280f), new(1280f, 2000f, 1280f)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f), new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
    }

    private static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "center-east.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

}

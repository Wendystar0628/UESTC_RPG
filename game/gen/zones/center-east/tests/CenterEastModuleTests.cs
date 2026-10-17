using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using Campus.Gen.Zones.CenterEast.Framework;
using Campus.Gen.Zones.CenterEast.Pinxue;
using Campus.Gen.Zones.CenterEast.Research;
using Campus.Gen.Zones.CenterEast.Wenke;
using GdUnit4;
using M = Campus.Gen.Zones.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Zones.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Zones.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Zones.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Zones.CenterEast.Tests;

[TestSuite]
public sealed class CenterEastModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void OverlayLoadsAndRegistersTheForecourtAdditions()
    {
        var world = World();
        var c = world.Features[new("qsh:relation:11043354")];
        Require(c.Height is 19.0f && c.Storeys == 5, "C wing raised to 19 m, 5 storeys");
        Require(c.Parts.All(p => p.Height is 19.0f), "every C part follows the HeightOp");
        foreach (var id in new[] { "design:center-east-pinxue-fountain", "design:center-east-pinxue-tree-pits", "design:center-east-pinxue-round-bed",
                     "design:center-east-pinxue-ramp-seatwall-north", "design:center-east-pinxue-ramp-seatwall-south", "design:center-east-pinxue-arc-end",
                     "design:center-east-riverside-walk" })
            Require(world.Features.ContainsKey(new(id)), $"{id} is registered");
        var fountain = (PointGeometry)world.Features[new("design:center-east-pinxue-fountain")].Geometry;
        Require(Vector2.Distance(new(fountain.Position.X, fountain.Position.Z), P.Fountain) < 0.01f, "overlay and tune agree on the fountain");
        var pits = (MultiPolygonGeometry)world.Features[new("design:center-east-pinxue-tree-pits")].Geometry;
        var centres = PinxueForecourtSite.TreePitCentres().ToArray();
        Require(pits.Polygons.Count == centres.Length, "one registered polygon per tree pit");
        foreach (var polygon in pits.Polygons)
        {
            var mid = polygon.Outer.Aggregate(Vector2.Zero, (s, p) => s + p) / polygon.Outer.Count;
            Require(centres.Any(c2 => Vector2.Distance(c2, mid) < 0.02f), $"registered pit at {mid} is built");
        }
    }

    [TestCase]
    public void ClaimsNameExistingFeaturesAndCoverTheirBuildings()
    {
        var world = World();
        var module = new CenterEastModule();
        var claims = module.Claims.OfType<FeatureClaim>().ToDictionary(c => c.Feature.Value, StringComparer.Ordinal);
        Require(module.Claims.Count == 5 && claims.Count == 5, "品学楼's frame entity, the three research buildings and 文科楼群 are claimed, once each");
        foreach (var claim in claims.Values)
        {
            Require(world.Features.ContainsKey(claim.Feature), $"claimed feature {claim.Feature.Value} exists");
            Require((claim.Aspects & Aspects.Massing) != 0, "claims cover massing so WP-14 skips the extrusion");
        }
        string[] Parts(string id) => world.Features[new(id)].Parts.Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        Require(Parts(P.Complex).SequenceEqual([P.FrameBar, P.BridgeBC, P.BridgeAC]), "the 品学楼 claim holds exactly the frame bar and the two bridges");
        // Every part of a claimed research building is built by its site: blocks as gridded blocks, the hall as the lens hall.
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            Require(claims.ContainsKey(site.Entity), $"{site.Id} claims its entity");
            var hall = site.Entity switch { R.KeA => R.KeAHall, R.KeB => R.KeBHall, _ => R.KeCHall };
            Require(Parts(site.Entity).SequenceEqual(site.Blocks.Select(b => b.Source).Append(hall).OrderBy(s => s, StringComparer.Ordinal)), $"{site.Id} builds every part of {site.Entity}");
        }
        // 文科楼群: every building part is one of the site's blocks; the only other part is the site outline itself.
        var wenke = world.Features[new(K.Entity)].Parts;
        Require(wenke.Where(p => p.SourceId != "osm:way:883187548").Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal)
            .SequenceEqual(K.Blocks.Select(b => b.Part).OrderBy(s => s, StringComparer.Ordinal)), "文科楼群's five buildings are the site's five blocks");
    }

    [TestCase]
    public void ResearchRegistrationsMatchWhatIsBuilt()
    {
        var world = World();
        IReadOnlyList<Vector2> Outline(string id) => ((PolygonGeometry)world.Features[new(id)].Geometry).Rings.Outer;
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs(PlanMath.SignedArea2(ring)) * 0.5f;
        foreach (var (id, back, sagitta) in new[] { ("design:center-east-ke-a-lens-hall", R.KeAHallBack, R.KeAHallSagitta),
                     ("design:center-east-ke-b-lens-hall", R.KeBHallBack, R.KeBHallSagitta), ("design:center-east-ke-c-lens-hall", R.KeCHallBack, R.KeCHallSagitta) })
        {
            var built = Area(ResearchSite.HallRing(back, sagitta, 16));
            Require(MathF.Abs(Area(Outline(id)) - built) < 0.03f * built, $"{id} outline matches the built hall");
        }
        foreach (var (id, min, max) in new[] { ("design:center-east-ke-a-roof", R.KeAFenceMin, R.KeAFenceMax),
                     ("design:center-east-ke-b-roof", R.KeBAtticMin, R.KeBAtticMax), ("design:center-east-ke-c-roof-garden", R.KeCGardenMin, R.KeCGardenMax) })
        {
            var corners = ResearchSite.Frame.Rect(min.X, min.Y, max.X, max.Y);
            Require(Outline(id).Count == 4 && Outline(id).Zip(corners).All(p => Vector2.Distance(p.First, p.Second) < 0.05f), $"{id} outline matches the built rectangle");
        }
        Require(Area(Outline("design:center-east-ke-c-roof-garden")) <= 300f, "the roof garden stays within 300 m²");
    }

    [TestCase]
    public void ResearchBlocksStandFiveStoreysWithOneShutEntranceEach()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var top = ResearchSite.Style.Top;
        Require(MathF.Abs(top - (R.GroundStorey + 4 * R.Storey + 1.3f)) < 0.01f, $"five storeys to the coping, got {top}");
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            var plan = site.Footprints(ctx.World);
            var walls = colliders.OfType<WallCollider>().Where(w => plan.Blocks.Any(r => NearRing(r, (w.A + w.B) * 0.5f))).ToArray();
            Require(walls.Any(w => w.Tag == SurfaceTag.Wall && MathF.Abs(w.Y1 - top) < 0.01f), $"{site.Id} walls reach the coping");
            var door = site.BlockDoor is { } d ? ResearchSite.Frame.P(d.X, d.Y) : site.HallApex().Apex;
            Require(colliders.OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, door) < 3f) is >= 1 and <= 2, $"{site.Id} has its shut entrance at the door");
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
        Require(module.Tiles(ctx).Any(), "the module occupies cells");
        foreach (var lod in Levels)
        {
            var items = module.ItemsFor(ctx, lod);
            Require(items.Count > 0, $"{lod} has items");
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
                    Require(mesh.Position.Length / 3 <= 65536, "bundle vertex limit");
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            Require(meshes > 0, $"{lod} emits meshes");
            Require(items.Count(i => i.Part is not null) == parts, $"{lod}: every placed part is emitted exactly once");
            // The entrance kit (mat, open sign, door lamp) is placed by WP-13 at every open entrance, never by the zone.
            string[] kit = [Contracts.Parts.PartCatalog.EntranceMat, Contracts.Parts.PartCatalog.OpenSign, Contracts.Parts.PartCatalog.EntranceLamp];
            Require(!items.Any(i => i.Part is { } p && kit.Contains(p.Part.Value)), $"{lod}: no entrance kit placed by the zone");
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
                Require(triangles <= limit, $"{lod} cell ({x}, {z}) has {triangles} triangles, budget {limit}");
                Require(output.Meshes.Select(m => m.Material).Distinct().Count() <= module.Budget.DrawCalls, $"{lod} cell ({x}, {z}) material count");
            }
        }
    }

    [TestCase]
    public void BuildIsDeterministic()
    {
        var a = Bytes(Context());
        var b = Bytes(Context());
        Require(a.SequenceEqual(b), "two worlds give the same bytes");
    }

    [TestCase]
    public void PassageAndBridgesKeepTheirClearHeights()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        Require(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto), "every collider fills its foot material");
        var boxes = colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y > 1f).ToArray();
        Require(boxes.Length == 3, $"the beam and the two bridges are lifted boxes, got {boxes.Length}");
        Require(boxes.All(b => b.At.M42 - b.Half.Y >= P.BridgeBottom - 0.01f), "nothing walls off the passage or the paths under the bridges");
        var frame = PinxueFrameSite.F.P(PinxueFrameSite.Axis, -P.FrameDepth * 0.5f);
        var columns = colliders.OfType<BoxCollider>().Count(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y < 0.01f && b.Half.Y * 2 >= P.BeamBottom - 0.01f
            && Vector2.Distance(new(b.At.M41, b.At.M43), frame) < 25f);
        Require(columns == P.ColumnX.Length, "the four columns reach the beam soffit");
        // The beam leaves the two-storey passage open between the columns.
        var beam = boxes.OrderByDescending(b => b.At.M42).First();
        Require(MathF.Abs(beam.At.M42 - beam.Half.Y - P.BeamBottom) < 0.01f, "the beam soffit sits at two storeys");
    }

    [TestCase]
    public void RampWallsStepWithTheGround()
    {
        // Flat stub: each wall is one run at seat height.
        foreach (var x in P.RampWallX)
        {
            var runs = PinxueForecourtSite.RampRuns(_ => 0f, x);
            Require(runs.Count == 1 && MathF.Abs(runs[0].Top - P.SeatHeight) < 1e-4f, "flat ground gives one continuous wall");
        }
        // The designed 5 % ramp along the frame's z: the wall steps once per segment, each top above the ground under it.
        var frame = PinxueFrameSite.F;
        float Ramp(Vector2 p) => Math.Clamp((P.RampWallZ1 - frame.Local(p).Y) * 0.05f, 0f, 1.5f);
        var stepped = PinxueForecourtSite.RampRuns(Ramp, P.RampWallX[0]);
        Require(stepped.Count == 5, $"the ramp wall steps in five runs, got {stepped.Count}");
        for (var i = 1; i < stepped.Count; i++)
            Require(stepped[i - 1].Top - stepped[i].Top is > 0.25f and < 0.35f, "each step drops about 0.3 m toward the foot");
    }

    [TestCase]
    public void FountainTiersMatchTheRegisteredWaterLevels()
    {
        var props = World().Features[new("design:center-east-pinxue-fountain")].Props!.Value;
        var registered = props.GetProperty("tiers").EnumerateArray().ToArray();
        var tiers = PinxueForecourtSite.FountainTiers();
        Require(registered.Length == tiers.Count, "one registered tier per built tier");
        static Vector2 Pair(System.Text.Json.JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle());
        var axis = Pair(props.GetProperty("axis"));
        Require(Vector2.Distance(axis, PinxueForecourtSite.FountainFrame.U) < 1e-3f, "the registered long axis runs along the bar");
        for (var i = 0; i < tiers.Count; i++)
        {
            var (half, coping, rim, water) = tiers[i];
            Require(MathF.Abs(registered[i].GetProperty("rimAbove").GetSingle() - rim) < 1e-3f, $"tier {i} rim");
            Require(MathF.Abs(registered[i].GetProperty("waterAbove").GetSingle() - water) < 1e-3f, $"tier {i} water");
            Require(Vector2.Distance(Pair(registered[i].GetProperty("half")), half) < 1e-3f, $"tier {i} stone extent");
            Require(Vector2.Distance(Pair(registered[i].GetProperty("waterHalf")), half - new Vector2(coping)) < 1e-3f, $"tier {i} water extent");
            Require(water < rim, $"tier {i} water stays below its rim");
            if (i > 0) Require(half.X < tiers[i - 1].Half.X - tiers[i - 1].Coping && half.Y < tiers[i - 1].Half.Y - tiers[i - 1].Coping && rim > tiers[i - 1].Rim, $"tier {i} steps up inside the pool below");
        }
        Require(tiers[^1].Rim <= 0.9f, "the cascade stays low, under 0.9 m");
    }

    [TestCase]
    public void TerrainOverlayParsesAndReferencesRealFeatures()
    {
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "terrain", "center-east.terrain.json")));
        Require(file.Zone.Value == "center-east" && file.Owner.Value == "WP-21", "file zone and owner");
        var world = World();
        var roads = world.Network.Roads.Select(r => r.Id).ToHashSet();
        foreach (var op in file.Ops)
            switch (op)
            {
                case TerrainPadOp pad: Require(world.Features.ContainsKey(pad.Feature), $"{op.Id}: pad feature {pad.Feature.Value} exists"); break;
                case TerrainCorridorOp corridor:
                    Require(roads.Contains(corridor.Road), $"{op.Id}: road {corridor.Road.Value} exists");
                    foreach (var step in corridor.Steps)
                        Require(world.Features.ContainsKey(step.Structure), $"{op.Id}: stair {step.Structure.Value} is registered");
                    break;
                case TerrainPlatformOp or TerrainSlopeOp or TerrainTerraceOp: break;
                default: throw new InvalidOperationException($"{op.Id}: unexpected operation for 21a");
            }
        Require(file.Ops.OfType<TerrainPadOp>().Count(p => p.PadY is 1.5) == 6, "the three wings, the frame entity and two plazas sit on the +1.5 platform");
    }

    [TestCase]
    public void MonumentsStandAtTheirPointsAndLeaveTheDoorsClear()
    {
        var world = World();
        Vector2 Point(string id) => world.Features[new(id)].Geometry is PointGeometry p ? new(p.Position.X, p.Position.Z) : throw new InvalidOperationException($"{id} is a point");
        // The OSM points move by less than 4 m (a minor adjustment with no reshape, MAP-QSH-CENTER-EAST "定位").
        foreach (var (id, at) in new[] { (M.Lin, M.LinAt), (M.Maxwell, M.MaxwellAt), (M.Stele, M.SteleAt) })
            Require(Vector2.Distance(Point(id), at) < 4f, $"{id} stays within 4 m of its OSM point");
        Require(StatueLawnSite.Island(world) is { } island && PlanMath.Inside(island, M.LinAt), "林为干像 stands on the plaza's tree island");
        // The design additions agree with what is built.
        Require(Vector2.Distance(Point("design:center-east-maxwell-equation-stone"), M.StoneAt) < 0.02f, "overlay and tune agree on the equation stone");
        var apron = ((PolygonGeometry)world.Features[new("design:center-east-maxwell-apron")].Geometry).Rings.Outer;
        Require(apron.Count == 4 && apron.Zip(StatueLawnSite.ApronRing).All(p => Vector2.Distance(p.First, p.Second) < 0.05f), "overlay and tune agree on the apron");
        foreach (var at in new[] { M.MaxwellAt, M.StoneAt })
            Require(PlanMath.Inside(StatueLawnSite.ApronRing, at), $"{at} stands on the apron");
        // Every inscription line lies on the dressed face with stone round it.
        foreach (var (x, y, length) in StatueLawnSite.StoneLines())
            foreach (var (fx, fy) in new[] { (x - length * 0.5f, y), (x + length * 0.5f, y), (x - length * 0.5f, y + M.StoneLineHeight), (x + length * 0.5f, y + M.StoneLineHeight) })
                Require(M.Stone.OnDressedFace(fx, fy, 0.04f), $"inscription corner ({fx}, {fy}) is on the dressed face");
        // Colliders: filled foot, a top at every pedestal, and nothing in front of the three research doors.
        var ctx = Context();
        var module = new CenterEastModule([new StatueLawnSite(), new ResearchSteleSite()]);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        Require(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto), "every monument collider fills its foot material");
        var boxes = colliders.OfType<BoxCollider>().ToArray();
        foreach (var (at, top) in new[] { (M.LinAt, StatueLawnSite.LinPedestalTop), (M.MaxwellAt, M.MaxwellPedestalTop), (M.StoneAt, M.Stone.Height - 0.08f) })
            Require(boxes.Any(b => Vector2.Distance(new(b.At.M41, b.At.M43), at) < 0.6f && MathF.Abs(b.At.M42 + b.Half.Y - top) < 0.02f), $"the box at {at} reaches its top {top}");
        var doors = new[] { ResearchSite.Frame.P(R.KeADoor.X, R.KeADoor.Y), ResearchSite.Frame.P(R.KeBDoor.X, R.KeBDoor.Y), new KeCSite().HallApex().Apex };
        var clear = 2.5f + R.PortalDepth + 3 * R.TreadDepth;
        foreach (var b in boxes)
            Require(doors.All(d => Vector2.Distance(new(b.At.M41, b.At.M43), d) > clear + b.Half.X + b.Half.Z), "monuments keep 2.5 m clear outside the doors and their steps");
    }

    [TestCase]
    public void SitesStayInsideTheCenterEastZone()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            Require(item.Anchor.X is > -120 and < 1000 && item.Anchor.Y is > -800 and < 600, $"item at {item.Anchor} is in the center-east zone");
        foreach (var probe in module.Probes)
            Require(world(probe.Feature), $"probe {probe.Id} names an existing feature");

        bool world(FeatureId id) => ctx.World.Features.ContainsKey(id);
    }

    [TestCase]
    public void WenkeStandsTerracedWithOneShutDoorAndAClearForecourt()
    {
        var ctx = Context();
        var plan = WenkeSite.Footprints(ctx.World);
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs(PlanMath.SignedArea2(ring)) * 0.5f;
        WenkeSite.Block Block(string part) => plan.Blocks.Single(b => b.Part == part);
        Require(Block(K.Tower).Storeys == 15 && WenkeSite.Floor(15) + K.Parapet > 58f, "the tower stands fifteen storeys, about 60 m");
        // The wings step back toward their tips: each top storey is smaller than its ground storey, with planted terraces between.
        foreach (var part in new[] { K.South, K.West, K.North })
        {
            var block = Block(part);
            Require(Area(block.Outlines[^1]) < 0.8f * Area(block.Outlines[0]), $"{part} steps back toward its tip");
            Require(Enumerable.Range(1, block.Storeys - 1).Any(k => WenkeSite.Terraces(plan, block, k).Any()), $"{part} has terraces");
        }
        Require(Block(K.Tower).Rows.Skip(K.PanelStripFrom).All(r => r.Any(c => c.Kind == WenkeSite.CellKind.Panel)), "the yellow-green strip runs up the tower");
        Require(plan.Blocks.All(b => b.Rows.All(r => r.Count(c => c.Kind == WenkeSite.CellKind.Window) > 0)), "every storey of every block shows glass");
        // One shut entrance at the atrium porch; nothing but paving within 2.5 m outside it.
        var module = new CenterEastModule([new WenkeSite(), new WenkeForecourtSite()]);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        Require(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto), "every 文科楼 collider fills its foot material");
        var shut = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.DoorClosed).ToArray();
        Require(shut.Length == 1 && Vector2.Distance((shut[0].A + shut[0].B) * 0.5f, K.DoorAt) < K.PorchDepth + 0.5f, "one shut glass entrance at the atrium porch");
        var outside = WenkeSite.Door.P(0, K.PorchDepth);
        foreach (var box in colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Prop))
            Require(Vector2.Distance(new(box.At.M41, box.At.M43), outside) > 2.5f + box.Half.X + box.Half.Z, "nothing stands within 2.5 m outside the door");
        foreach (var post in colliders.OfType<PostCollider>())
            Require(Vector2.Distance(post.C, outside) > 2.5f + post.R, "no stake within 2.5 m outside the door");
        // The overlay registers what is built.
        var world = ctx.World;
        IReadOnlyList<Vector2> Outline(string id) => ((PolygonGeometry)world.Features[new(id)].Geometry).Rings.Outer;
        Vector2 Point(string id) => world.Features[new(id)].Geometry is PointGeometry p ? new(p.Position.X, p.Position.Z) : throw new InvalidOperationException($"{id} is a point");
        foreach (var (id, ring) in new[] { ("design:center-east-wenke-forecourt", WenkeForecourtSite.ForecourtRing), ("design:center-east-wenke-walk", WenkeForecourtSite.WalkRing) })
            Require(Outline(id).Count == 4 && Outline(id).All(p => ring.Any(q => Vector2.Distance(p, q) < 0.05f)), $"{id} outline matches the built paving");
        Require(Vector2.Distance(Point("design:center-east-wenke-friendship-tree"), F.TreeAt) < 0.02f, "overlay and tune agree on the friendship tree");
        Require(Vector2.Distance(Point("design:center-east-wenke-friendship-stone"), F.StoneAt) < 0.02f, "overlay and tune agree on the stone");
        var vault = Outline("design:center-east-wenke-atrium-roof");
        Require(MathF.Abs(Area(vault) - MathF.PI * K.AtriumHalf.X * K.AtriumHalf.Y) < 0.05f * Area(vault), "the registered atrium roof matches the vault");
        Require(PlanMath.Inside(Block(K.Hub).Outlines[^1], K.AtriumCentre), "the vault stands on the hub");
        // The emblem sits on the stone's dressed face with stone round it.
        foreach (var (x, y) in new[] { (-F.EmblemRadius, F.EmblemY), (F.EmblemRadius, F.EmblemY), (0f, F.EmblemY - F.EmblemRadius), (0f, F.EmblemY + F.EmblemRadius) })
            Require(F.Stone.OnDressedFace(x, y, 0.03f), $"emblem edge ({x}, {y}) is on the dressed face");
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
        return new BuildContext { World = World(), Map = null!, WorldSeed = 1 };
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
        Require(layouts.Count == 5, $"five Facade-claimed features declare layouts, got {layouts.Count}");
        Require(Keys(P.Complex) == "beam,bridge-ac,bridge-bc" && layouts[P.Complex].All(g => g.Rows == 2), $"品学楼 bridges: {Keys(P.Complex)}");
        Require(Keys(R.KeA) == "main,west" && Keys(R.KeB) == "attic,main,middle,west" && Keys(R.KeC) == "bar,link", "research grids by block");
        Require(layouts.Values.SelectMany(g => g).All(g => g.Columns > 0 && g.Rows > 0), "every grid has cells");
        Require(layouts[R.KeB].Single(g => g.Key == "attic").Rows == 1 && layouts[R.KeA].All(g => g.Rows == R.Storeys), "five storeys, one attic row");
        Require(Keys(K.Entity) == "hub,north,south,tower,west" && layouts[K.Entity].Single(g => g.Key == "tower").Rows == 15, $"文科楼 grids by block: {Keys(K.Entity)}");
        var envelope = new CenterEastModule().DescribeWindowLayouts(new EnvelopeWorld(world), 1);
        Require(envelope.All(l => l.Grids.SequenceEqual(layouts[l.Feature.Value])) && envelope.Count == layouts.Count, "the envelope view declares the same grids");
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
        var ctx = new BuildContext { World = world, Map = Descriptor(), WorldSeed = 1, WindowNumbering = new(new("qingshuihe"), new string('a', 64), allocations) };
        Throws(() => module.DescribeWindows(Context()), "positions need the numbering snapshot");

        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        declared.AddRange(metadata.Windows.Select(w => w.Id));
        Require(declared.Count == declared.Distinct().Count(), "every window is declared once");
        Require(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier")), "declared IDs lie in the zone's grids");
        Require(metadata.Windows.Count > 0 && metadata.Facades.Count > 0, "bridges give loose windows, research blocks facade runs");
        // The portal cells under the entrance frames carry no window.
        Require(metadata.Facades.Count(f => f.Skip.Count > 0) == 2, "科 A and 科 B each skip their portal cells");

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
        Require(near.SetEquals(declared), $"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})");
        Require(Glass(LodLevel.L3).SetEquals(declared), "coarse glass keeps the same numbers");
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
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)new EnvelopeWorld(world)) })
        {
            WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-21") : null;
            var prepass = new BuildContext { World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner };
            var layouts = module.DescribeWindowLayouts(view, 1);
            var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
            var ours = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            Require(ours.Length == layouts.Sum(l => l.Grids.Count) && ours.All(a => a.Columns > 0 && a.Rows > 0),
                $"{name}: every center-east grid is allocated with cells ({ours.Length} of {layouts.Sum(l => l.Grids.Count)})");
            Require(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity, $"{name}: the table fits ({snapshot.WindowCount})");
            var metadata = module.DescribeWindows(new BuildContext { World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            Require(ids.Length > 0 && ids.All(id => ours.Any(a => id >= a.BaseId && id < a.BaseId + a.Count)), $"{name}: the zone's windows lie in its own grids");
        }
    }

    /// <summary>The loaded world with every multi-source entity's Geometry replaced by its parts' bounding rectangle.</summary>
    private sealed class EnvelopeWorld(IWorldModel inner) : IWorldModel
    {
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = inner.Features.ToDictionary(p => p.Key, p => p.Value.Parts.Count == 0 ? p.Value : p.Value with
        {
            Geometry = Envelope(p.Value.Parts.SelectMany(part => part.Geometry switch
            {
                PolygonGeometry g => g.Rings.Outer,
                MultiPolygonGeometry m => m.Polygons.SelectMany(r => r.Outer),
                _ => [],
            }).ToArray()),
        });
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;

        private static OverlayGeometry Envelope(Vector2[] points)
        {
            if (points.Length == 0) return new PointGeometry(Vector3.Zero);
            float x0 = points.Min(p => p.X), x1 = points.Max(p => p.X), z0 = points.Min(p => p.Y), z1 = points.Max(p => p.Y);
            return new PolygonGeometry(new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]));
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

    private static void Throws(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
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

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

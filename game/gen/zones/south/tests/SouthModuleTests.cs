using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

[TestSuite]
public sealed class SouthModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void ClockTowerFootprintAcceptsExtraAndCollinearPoints()
    {
        var quad = new List<Vector2> { new(0, 0), new(12, 8), new(5, 17), new(-7, 9) };
        var (f0, hx0, hz0) = Xuanbang.ClockTower.FrameOf(quad);
        Require(ReferenceEquals(Xuanbang.ClockTower.Quadrilateral(quad), quad), "four points pass through untouched");
        var padded = new List<Vector2> { quad[0], (quad[0] + quad[1]) * 0.5f, quad[1], quad[2], (quad[2] + quad[3]) * 0.5f, quad[3], (quad[3] + quad[0]) * 0.5f };
        var (f1, hx1, hz1) = Xuanbang.ClockTower.FrameOf(padded);
        Require(f1.U == f0.U && f1.P(0, 0) == f0.P(0, 0) || (Vector2.Distance(f1.P(0, 0), f0.P(0, 0)) < 0.01f && Vector2.Distance(f1.U, f0.U) < 0.01f), "collinear points leave the frame unchanged");
        Require(MathF.Abs(hx1 - hx0) < 0.01f && MathF.Abs(hz1 - hz0) < 0.01f, "collinear points leave the size unchanged");
        var ragged = new List<Vector2> { new(0, 0), new(10, 0), new(11, 4), new(10, 8), new(0, 8), new(-1, 4) };
        var (_, hx, hz) = Xuanbang.ClockTower.FrameOf(ragged);
        var rect = Xuanbang.ClockTower.Quadrilateral(ragged);
        Require(rect.Count == 4 && hx > 0 && hz > 0 && hx * hz * 4 >= 80f && hx * hz * 4 < 110f, $"non-rectangular footprint gets a bounding rectangle ({hx}, {hz})");
    }

    [TestCase]
    public void OverlayLoadsAndSetsTheMainBuildingHeights()
    {
        var world = World();
        Require(world.Features[new("qsh:way:687370085")].Height is 34.4f, "B2 height from the HeightOp");
        Require(world.Features[new("qsh:way:687370085")].Storeys == 8, "B2 storeys");
        Require(world.Features[new("qsh:way:687370086")].Storeys == 7, "B1 storeys");
        Require(world.Features[new("qsh:way:687370092")].Storeys == 5, "A1 storeys");
        Require(world.Features.ContainsKey(new("design:south-hongfeng-pavilion")), "pavilion design addition");
    }

    [TestCase]
    public void BuildingOneKeepsOnlyItsOneStoreySourcePartOnTheRealMap()
    {
        var data = DataRoot();
        var one = new FeatureId("qsh:way:691679140");
        var rawFiles = new Dictionary<string, string>(StringComparer.Ordinal) { ["campus.json"] = File.ReadAllText(Path.Combine(data, "campus.json")) };
        var raw = WorldModelLoader.Load(new("qingshuihe"), rawFiles).World;
        Require(raw.Features[one].Parts.Select(p => p.SourceId).Order(StringComparer.Ordinal)
            .SequenceEqual(["osm:way:691679138", "osm:way:691679140"], StringComparer.Ordinal), "the raw map carries two source parts for building 1");
        // The map as the kernel loads it: every authored overlay.
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        var world = WorldModelLoader.Load(new("qingshuihe"), files).World;
        Require(world.Features.ContainsKey(one), "hidePart keeps the entity itself");
        var building = world.Features[one];
        Require(building.Parts.Count == 1 && building.Parts[0].SourceId == "osm:way:691679140", "only the 1-storey source part is left");
        Require(building.Height == 3.9f, "building 1 stands 3.9 m");
        Require(building.Storeys == 1, "building 1 has one storey");
    }

    [TestCase]
    public void ClaimsNameExistingFeatures()
    {
        var world = World();
        var module = new SouthModule();
        Require(module.Claims.Count == 8, "gate, front steps, Xuanbang, Liren's connector, the gym, the natatorium, the sports platform and the stand are claimed");
        foreach (var claim in module.Claims.OfType<FeatureClaim>())
        {
            Require(world.Features.ContainsKey(claim.Feature), $"claimed feature {claim.Feature.Value} exists");
            Require((claim.Aspects & Aspects.Massing) != 0, "claims cover massing so WP-14 skips the extrusion");
        }
    }

    [TestCase]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new SouthModule();
        var tiles = module.Tiles(ctx).ToArray();
        Require(tiles.Length > 0, "the module occupies cells");
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
        }
    }

    [TestCase]
    public void HeaviestCellsStayInsideTheDeclaredBudget()
    {
        var ctx = Context();
        var module = new SouthModule();
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
    public void GateClosureHasNoGapAboveHalfAMetre()
    {
        var ctx = Context();
        var module = new SouthModule();
        var boundary = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag == SurfaceTag.Boundary).ToArray();
        Require(boundary.Length >= 11, "houses, islands, carriageway and both turnstile rows are boundary colliders");
        // March along the gate line between the guard houses' walls: every 0.25 m sample is inside some boundary primitive.
        var along = Vector2.Normalize(SouthTune.Gate.HouseEast - SouthTune.Gate.HouseWest);
        var west = SouthTune.Gate.HouseWest + along * (SouthTune.Gate.HouseSize * 0.5f);
        var east = SouthTune.Gate.HouseEast - along * (SouthTune.Gate.HouseSize * 0.5f);
        // The barrier arms stand 1.4 m inside the turnstile rows; sample on the arms' line, which crosses every element.
        var inward = new Vector2(-along.Y, along.X) * SouthTune.Gate.BarrierZ;
        for (var s = 0f; s <= 1f; s += 0.25f / Vector2.Distance(west, east))
        {
            var p = Vector2.Lerp(west, east, s) + inward;
            Require(boundary.Any(c => Covers(c, p)), $"gate line covered at {p}");
        }
        Require(boundary.All(c => c is not BoxCollider box || box.Half.Y * 2 >= 1.5f), "boundary boxes reach 1.5 m");
    }

    [TestCase]
    public void EntranceKitsAreLeftToTheStreetFurniture()
    {
        // WLD-INTERIORS §0: WP-13 places the mat, open sign and door lamp on every open entrance anchor; the zone builds the doorway only.
        string[] kit = [PartCatalog.EntranceMat, PartCatalog.OpenSign, PartCatalog.EntranceLamp];
        var placed = new SouthModule().ItemsFor(Context(), LodLevel.L0).Where(i => i.Part is not null).Select(i => i.Part!.Part.Value).ToArray();
        Require(!placed.Any(kit.Contains), $"the zone places no entrance kit ({string.Join(", ", placed.Where(kit.Contains))})");
        Require(!new SouthModule().Claims.OfType<FeatureClaim>().Any(c => (c.Aspects & Aspects.Props) != 0), "no Props claim keeps WP-13 off the zone's entrances");
    }

    [TestCase]
    public void GateBollardsAreCrashBollardsInTheirOwnColours()
    {
        // §1.4: yellow-and-black; a tint or a material would paint the black stripe yellow as well (13b, 12:00).
        var bollards = new SouthModule().ItemsFor(Context(), LodLevel.L0).Select(i => i.Part).Where(p => p?.Part.Value == PartCatalog.Bollard).ToArray();
        Require(bollards.Length == 6, $"three bollards off each island nose ({bollards.Length})");
        Require(bollards.All(p => p!.Tint is null && p.Params is ColumnParams { Variation: 1 } c && c.Material == new ColumnParams().Material), "variation 1, no tint, no material");
    }

    [TestCase]
    public void ClockSideDoorOpensOntoAPavedForecourt()
    {
        // 13b review: an open doorway stands on paving at least the door width + 1 m wide and 2 m deep, tied to the nearest paving.
        var world = World();
        var court = world.Features[new("design:south-clock-forecourt")];
        var walk = world.Features[new("design:south-clock-walk")];
        Require(court.Kind.Value == "road" && Text(court, "roadType") == "pedestrian" && Text(walk, "roadType") == "footway", "plaza paving and a footway, drawn by WP-15");
        var door = world.Interiors.Get(new(Xuanbang.XuanbangSite.Feature))!.Entrances.Single(e => e.Id == "xb.clock");
        var yaw = door.YawDeg * MathF.PI / 180f;
        var outward = new Vector2(DetMath.Sin(yaw), -DetMath.Cos(yaw));
        var across = new Vector2(-outward.Y, outward.X);
        var centre = new Vector2(door.Center.X, door.Center.Z);
        var ring = ((PolygonGeometry)court.Geometry).Rings;
        var u = ring.Outer.Select(p => Vector2.Dot(p - centre, across)).ToArray();
        var v = ring.Outer.Select(p => Vector2.Dot(p - centre, outward)).ToArray();
        var half = SouthTune.Xuanbang.PortalWidth * 0.5f + 0.5f;
        Require(u.Min() <= -half && u.Max() >= half, $"forecourt {u.Max() - u.Min():0.0} m wide, the door {SouthTune.Xuanbang.PortalWidth} m");
        Require(MathF.Abs(v.Min()) < 0.5f && v.Max() >= 2f, $"forecourt from the door face ({v.Min():0.00}) out to {v.Max():0.0} m");
        var line = ((PolylineGeometry)walk.Geometry).Line.Points;
        var parking = (PolygonGeometry)world.Features[new("qsh:way:1383996369")].Geometry;
        Require(PlanGeometry.Contains(ring, line[0]) && PlanGeometry.Contains(parking.Rings, line[^1]), "the walk runs from the forecourt onto the parking by the road");
        Require(SouthTune.Xuanbang.Threshold == 0.15f, "the threshold is level with WP-15's paving top");
    }

    [TestCase]
    public void GatePlazaGivesA01ItsFooting()
    {
        var world = World();
        var plaza = world.Features[new(Gate.GatePlazaSite.Feature)];
        Require(plaza.Kind.Value == "road" && Text(plaza, "roadType") == "pedestrian", "plaza paving, drawn by WP-15 with its kerbs and edging");
        var (frame, across, along) = Gate.GatePlazaSite.FrameOf(world)!.Value;
        Require(MathF.Abs(along * 2f - 18f) < 0.1f && MathF.Abs(across * 2f - 12f) < 0.1f, $"about 18 × 12 m ({along * 2f:0.0} × {across * 2f:0.0})");
        Require(MathF.Abs(SouthSite.Bearing(frame.W) - 36.8f) < 1f, $"the long side runs along the main axis ({SouthSite.Bearing(frame.W):0.0}°)");
        var ring = ((PolygonGeometry)plaza.Geometry).Rings;
        // WP-13's kiosk (2 × 0.8 m) on the spot the plaza keeps for A01: on the paving, at least 3 m from every carriageway edge.
        var yaw = SouthTune.Plaza.KioskYaw * MathF.PI / 180f;
        var face = new Vector2(DetMath.Sin(yaw), -DetMath.Cos(yaw));
        var side = new Vector2(-face.Y, face.X);
        var corners = new[] { -1f, 1f }.SelectMany(u => new[] { -1f, 1f }.Select(v => SouthTune.Plaza.KioskSpot + face * (0.4f * u) + side * (1.0f * v))).ToArray();
        Require(corners.All(p => PlanGeometry.Contains(ring, p)), "the kiosk stands on the plaza");
        var lanes = world.Network.Roads.Where(r => Text(r.Props, "highway") is "residential" or "service" or "tertiary" or "secondary" or "unclassified").ToArray();
        var clearance = corners.Min(p => lanes.Min(r => PolylineDistance(p, r.Line.Points) - r.Width * 0.5f));
        Require(clearance >= 3f, $"the kiosk stands {clearance:0.00} m from the nearest carriageway edge");
        // The planter and benches stay on the paving and clear of the kiosk and the space in front of its panel.
        var items = new SouthModule().ItemsFor(Context(), LodLevel.L0).Where(i => i.Part?.Part.Value is PartCatalog.Bench or PartCatalog.Planter).ToArray();
        Require(items.Length == 3, $"a planter and two benches ({items.Length})");
        Require(items.All(i => PlanGeometry.Contains(ring, i.Anchor) && Vector2.Distance(i.Anchor, SouthTune.Plaza.KioskSpot) > 6f), "furniture on the plaza, clear of the kiosk");
    }

    private static float PolylineDistance(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < line.Count; i++) best = MathF.Min(best, DistanceToSegment(p, line[i], line[i + 1]));
        return best;
    }

    private static string? Text(WorldFeature feature, string name) => Text(feature.Props, name);

    private static string? Text(System.Text.Json.JsonElement? props, string name) =>
        props is { ValueKind: System.Text.Json.JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : null;

    [TestCase]
    public void SitesStayInsideTheSouthZone()
    {
        var ctx = Context();
        var module = new SouthModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            Require(item.Anchor.X is > -1000 and < 1000 && item.Anchor.Y is > 90 and < 1000, $"item at {item.Anchor} is in the south zone");
    }


    [TestCase]
    public void OverlaySetsLirenAndRegistersTheWadeStep()
    {
        var world = World();
        foreach (var id in new[] { Liren.LirenSite.BlockA, Liren.LirenSite.BlockB })
        {
            Require(world.Features[new(id)].Storeys == 4, $"{id} has 4 storeys");
            Require(world.Features[new(id)].Family?.Value == "modern", $"{id} is in the white family");
        }
        Require(world.Features.ContainsKey(new(EastLake.EastLakeSite.WadeStep)), "wade step design addition");
    }

    [TestCase]
    public void XuanbangKeepsItsSkyline()
    {
        var ctx = Context();
        var items = new SouthModule().ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var f = Xuanbang.XuanbangSite.Rings(ctx.World);
        float Top(IReadOnlyList<Vector2> ring) => items.Where(i => Near(ring, i.Anchor, 3f)).Max(i => MaxY(i.Mesh!));
        Require(MathF.Abs(Top(f.Clock) - SouthTune.Xuanbang.Top) < 0.05f, $"clock tower top {Top(f.Clock)}");
        Require(MathF.Abs(Top(f.Tower) - 73.85f) < 0.05f, $"tower top {Top(f.Tower)}");
        var main = items.Where(i => Near(f.Main, i.Anchor, 1f) && !Near(f.Tower, i.Anchor, 3f) && !Near(f.Clock, i.Anchor, 3f)
            && Vector2.Distance(i.Anchor, SouthTune.Xuanbang.Skylight) > 10f).Max(i => MaxY(i.Mesh!));
        Require(main > 35.6f && main < 38.8f, $"main wing roof line {main} (35.7 m, gables up to 38.3 m)");
        var all = items.Max(i => MaxY(i.Mesh!));
        Require(all < 73.9f, $"the Xuanbang tower stays the highest point of the zone ({all})");
    }

    [TestCase]
    public void LirenGatewayIsOpenUnderTheBridge()
    {
        var ctx = Context();
        var module = new SouthModule();
        var f = Liren.LirenSite.Rings(ctx.World);
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag is SurfaceTag.Wall or SurfaceTag.DoorClosed).ToArray();
        var open = Framework.GridFacade.Edges(f.Bridge).Select(e => e.Info).First(e => Liren.LirenSite.Dress(e, f.BridgeNeighbours).Kind != Framework.EdgeKind.Hidden);
        var centre = Framework.PlanMath.Centroid(f.Bridge);
        for (var t = -14f; t <= 14f; t += 0.25f)
        {
            var p = centre + open.Frame.W * t;
            Require(!walls.Any(c => Covers(c, p, 1.0f)), $"the gateway passage is clear at {p}");
        }
        Require(SouthTune.Liren.Soffit >= 10.5f, "the void is about three storeys high");
    }

    [TestCase]
    public void HardBankBalustradeGuardsTheWaterButTheWadeStep()
    {
        var ctx = Context();
        var module = new SouthModule();
        var barriers = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Barrier).ToArray();
        var water = EastLake.EastLakeSite.Water(ctx.World);
        Require(EastLake.EastLakeSite.InWater(water, EastLake.EastLakeSite.WadeFrame.P(EastLake.EastLakeSite.WadeLength * 0.5f, 1f)), "the wade frame looks over the water");
        Require(SouthTune.Lake.Tread2 < -0.5f && SouthTune.Lake.Tread2 > -0.8f, "the wade tread is under the -0.5 m water by less than 0.3 m");
        Require(SouthTune.Lake.RailTop >= 1.05f, "balustrade at least 1.05 m");
        foreach (var (f, l, i) in EastLake.EastLakeSite.BankSegments(water))
        {
            var openings = EastLake.EastLakeSite.Openings(i, l);
            for (var u = 0.3f; u <= l - 0.3f; u += 0.25f)
            {
                var p = f.P(u, -SouthTune.Lake.RailSetback);
                var inOpening = openings.Any(o => u > o.U0 + 0.5f && u < o.U1 - 0.5f);
                var nearOpening = openings.Any(o => u > o.U0 - 0.5f && u < o.U1 + 0.5f);
                var guarded = barriers.Any(w => w.Y1 >= 1.5f && Covers(w, p, 1.0f));
                if (inOpening) Require(!guarded, $"the wade step at {p} is open");
                else if (!nearOpening) Require(guarded, $"the bank at {p} is guarded");
            }
        }
    }


    [TestCase]
    public void TerrainOverlayParsesWithTheStageOneSchema()
    {
        var data = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataRoot(), "overlay", "terrain", "south.terrain.json")));
        Require(data.Zone.Value == "south" && data.Owner.Value == "WP-18", "zone and owner");
        var hill = data.Ops.OfType<TerrainMoundOp>().Single(o => o.Id == "design:terrain:south-hongfeng");
        Require(hill.Shape is TerrainEllipse { YawDeg: 75 } e && e.Center == new Vector2(-352, 238) && e.Radii == new Vector2(42, 34), "hill ellipse");
        Require(data.Ops.OfType<TerrainPlatformOp>().Single().Rings.Outer.Count == 12, "hill-top platform ring");
        Require(data.Ops.Count == 4 && data.Ops.All(o => o.Priority is 50 or 60), "four operations keep their priorities");
        var lawn = data.Ops.OfType<TerrainMoundOp>().Single(o => o.Id == "design:terrain:south-west-lawn");
        Require(lawn.Height <= 1.5 && lawn.Shape is TerrainEllipse le && MathF.Min(le.Radii.X, le.Radii.Y) >= 7.5 * lawn.Height, "the west-lawn mound keeps R6's limits (h ≤ 1.5 m, R ≥ 7.5 h)");
    }

    [TestCase]
    public void WindowLayoutsDeclareEveryFacadeClaimWithItsGrids()
    {
        var layouts = new SouthModule().DescribeWindowLayouts(World(), 1).ToDictionary(l => l.Feature.Value, l => l.Grids, StringComparer.Ordinal);
        Require(layouts.Count == 8, "gate, front steps, Xuanbang, the Liren connector, the two venues, the platform and the stand are declared");
        Require(layouts[Sports.GrandstandSite.Feature].Count == 0, "the stand has no glass: its declaration is empty");
        Require(!layouts.ContainsKey(MainBuilding.MainBuildingSite.B2), "B2 keeps WP-14's generic windows");
        void Grid(string feature, string key, int? columns, int rows)
        {
            var grid = layouts[feature].Single(g => g.Key == key);
            Require(grid.Rows == rows && grid.Columns > 0 && (columns is null || grid.Columns == columns), $"{feature} / {key} is {grid.Columns}x{grid.Rows}");
        }
        Grid(Gate.SouthGateSite.Feature, "guard-houses", 3, 2);
        Grid(MainBuilding.MainBuildingSite.FrontSteps, "front-porch", 5, 1);
        Grid(MainBuilding.MainBuildingSite.FrontSteps, "rear-porch", 1, 1);
        Grid(Xuanbang.XuanbangSite.Feature, "main", null, 8);
        Grid(Xuanbang.XuanbangSite.Feature, "tower", null, 18);
        Grid(Xuanbang.XuanbangSite.Feature, "clock-shaft", 8, SouthTune.Xuanbang.SlitRows);
        Grid(Xuanbang.XuanbangSite.Feature, "clock-portals", 4, 1);
        Grid(Xuanbang.XuanbangSite.Feature, "portico", 3, 2);
        Grid(Liren.LirenSite.Connector, "piers", null, 4);
        Grid(Liren.LirenSite.Connector, "bridge", null, 1);
        Grid(Liren.LirenSite.Connector, "east-link", null, 2);
        Grid(Sports.GymSite.Feature, "ring-hall", null, 2);
        Grid(Sports.NatatoriumSite.Feature, "glass-band", null, 1);
        Grid(Sports.PlatformSite.Feature, "arcade-shops", null, 1);
        Require(layouts.Values.Sum(g => g.Count) == 14, "fourteen grids in all");
    }

    /// <summary>A06 on the deck (SYS-ATLAS §6): x, z and the panel's bearing, the main session's pick, 29 m from the old anchor.</summary>
    private static readonly Vector2 A06Spot = new(-166.5f, 734f);
    private const float A06Yaw = 99f;

    [TestCase]
    public void PlatformDeckCarriesA06AndOpensOnlyWhereStairsLand()
    {
        var ctx = Context();
        var p = Sports.PlatformSite.PlanOf(ctx.World);
        Require(p.Decks.Count == 1, "one deck: A06 moves onto it, no terrace is added");
        Require(SouthTune.Sports.PlatformTop == 5.0f && MathF.Abs(p.Top - p.Y - SouthTune.Sports.PlatformTop) < 1e-4f, "the deck top, A06's y, is the 5.0 m platform storey");
        // A06's kiosk (2 × 0.8 m, WP-13's) on the deck between the gym and the natatorium, its panel toward the stadium: every corner
        // on the deck and at least 3 m from the deck's edges and the parapets' inner faces. The anchor data stays as it is until
        // Astra moves it after the merge.
        var stadium = Sports.GrandstandSite.PlanOf(ctx.World).Centre;
        var a = A06Yaw * MathF.PI / 180f;
        var face = new Vector2(DetMath.Sin(a), -DetMath.Cos(a));
        var side = new Vector2(-face.Y, face.X);
        var corners = new[] { -1f, 1f }.SelectMany(u => new[] { -1f, 1f }.Select(v => A06Spot + face * (0.4f * u) + side * (1.0f * v))).ToArray();
        Require(corners.All(c => Framework.PlanMath.Inside(p.Decks[0], c)), "A06 stands on the deck");
        var parapet = SouthTune.Platform.ParapetInset + SouthTune.Platform.ParapetDepth * 0.5f;
        var clearance = corners.Min(c => p.Edges.Min(e => DistanceToSegment(c, e.A, e.B) - (e.Free ? parapet : 0f)));
        Require(clearance >= 3f, $"A06 keeps {clearance:0.00} m from the deck edges and parapets");
        var turn = Framework.PlanMath.Turn(SouthSite.Bearing(stadium - A06Spot) - A06Yaw);
        Require(MathF.Abs(turn) < 5f, $"A06 faces the stadium ({turn:0.0}° off)");
        Require(p.Stairs.Count == 2 && p.Bays.Count > 20 && p.Columns.Count > 20, $"two stairs, {p.Bays.Count} shop bays, {p.Columns.Count} columns");
        Require(p.Edges.Count(e => !e.Free) > 10 && p.Edges.Count(e => e.Free) > 20, "the edges against the venues stay open, the rest are guarded");
        var module = new SouthModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        Require(colliders.OfType<SlabCollider>().Count(slab => MathF.Abs(slab.Y - p.Top) < 0.01f) == 1, "the deck top is walkable");
        var walls = colliders.OfType<WallCollider>().ToArray();
        foreach (var stair in p.Stairs)
            Require(!walls.Any(w => Covers(w, stair.Frame.P(0, -0.3f), p.Top + 0.8f)), $"a stair lands through an open parapet at {stair.Frame.Origin}");
        var edge = p.Edges.First(e => e.Free && e.Length > 10f && !p.Stairs.Any(s => Vector2.Distance(s.Frame.Origin, e.Mid) < 12f));
        Require(walls.Any(w => Covers(w, edge.Mid - edge.Out * 0.2f, p.Top + 1.2f)), "a free edge carries a parapet to 1.5 m");
    }

    [TestCase]
    public void SportsVenuesKeepTheirProfilesAndTheDeckDoorway()
    {
        var ctx = Context();
        var module = new SouthModule();
        var items = module.ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var gym = Sports.GymSite.PlanOf(ctx.World);
        var pool = Sports.NatatoriumSite.PlanOf(ctx.World);
        Require(gym.Radius is > 57f and < 62f && gym.Bays > 90, $"gym drum radius {gym.Radius}, {gym.Bays} bays");
        Require(pool.A >= pool.B && pool.B > 35f && pool.A < 60f, $"natatorium ellipse {pool.A} × {pool.B}");
        float Top(Vector2 centre, float radius) => items.Where(i => Vector2.Distance(i.Anchor, centre) < radius).Max(i => MaxY(i.Mesh!));
        var gymTop = Top(gym.Centre, gym.Radius);
        Require(gymTop > SouthTune.Sports.GymTop - 0.1f && gymTop < SouthTune.Sports.GymTop + 1.2f, $"gym roof at 26 m, vent caps above it ({gymTop})");
        var poolTop = Top(pool.Frame.Origin, pool.B);
        Require(poolTop is > 14.5f and < 16f, $"natatorium a low dome near 15 m ({poolTop})");
        // The platform-storey doorway (gym.deck) is open between the platform and the door head; the drum is walled elsewhere.
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().ToArray();
        var deck = gym.Centre + Framework.PlanMath.Heading(SouthTune.Sports.GymDoorBearing) * (gym.Radius - SouthTune.Sports.GymBaseInset);
        Require(!walls.Any(w => Covers(w, deck, SouthTune.Sports.PlatformTop + 1.5f)), "the gym's deck doorway is open");
        Require(walls.Any(w => Covers(w, deck, SouthTune.Sports.PlatformTop - 1f)), "the drum is closed below the platform storey");
        var back = gym.Centre - Framework.PlanMath.Heading(SouthTune.Sports.GymDoorBearing) * (gym.Radius - SouthTune.Sports.GymBaseInset);
        Require(walls.Any(w => Covers(w, back, SouthTune.Sports.PlatformTop + 1.5f)), "the drum is closed opposite the door");
    }

    [TestCase]
    public void EveryDeclaredGridIsValidOnTheRealMap()
    {
        // The map as the kernel loads it: every authored overlay, not only the south one. One invalid grid withholds the whole table.
        var data = DataRoot();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        var world = WorldModelLoader.Load(new("qingshuihe"), files).World;
        // The contract only promises an aggregate query envelope in WorldFeature.Geometry (the D0 world model gives a bounding
        // rectangle): the sites must read footprints from the parts, so the same declarations hold over such a view.
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)new EnvelopeWorld(world)) })
        {
            var layouts = new SouthModule().DescribeWindowLayouts(view, 1);
            var grids = layouts.SelectMany(l => l.Grids.Select(g => $"{l.Feature.Value}/{g.Key} {g.Columns}x{g.Rows}")).ToArray();
            Require(layouts.All(l => l.Grids.All(g => g.Columns > 0 && g.Rows > 0)), $"{name}: every declared grid has cells: {string.Join(", ", grids)}");
            Require(layouts.All(l => l.Grids.Select(g => g.Key).Distinct(StringComparer.Ordinal).Count() == l.Grids.Count), $"{name}: keys are unique per feature");
        }
        Require(new SouthModule().DescribeWindowLayouts(new EnvelopeWorld(world), 1).SequenceEqual(new SouthModule().DescribeWindowLayouts(world, 1), LayoutComparer.Instance),
            "the envelope view declares the same grids");
        var lake = ((PolygonGeometry)world.Features[new(EastLake.EastLakeSite.Lake)].Geometry).Rings;
        Require(EastLake.EastLakeSite.Water(new EnvelopeWorld(world)).Outer.SequenceEqual(lake.Outer) && EastLake.EastLakeSite.Water(world).Outer.SequenceEqual(lake.Outer),
            "the east lake's water ring comes from its own part in both views");
    }

    [TestCase]
    public void WindowTablePublishesWithTheSouthDeclarations()
    {
        // The end-to-end reach before WP-02's orchestration: WP-14's provider allocates the whole real map with the zone's
        // real declarations, then the zone describes its windows from the published snapshot. The map loads as above.
        var data = DataRoot();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        var world = WorldModelLoader.Load(new("qingshuihe"), files).World;
        var module = new SouthModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        foreach (var (name, view) in new[] { ("loaded", world), ("envelope", (IWorldModel)new EnvelopeWorld(world)) })
        {
            WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-18") : null;
            var prepass = new BuildContext { World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner };
            var layouts = module.DescribeWindowLayouts(view, 1);
            var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
            var south = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
            Require(south.Length == layouts.Sum(l => l.Grids.Count) && south.All(a => a.Columns > 0 && a.Rows > 0),
                $"{name}: every south grid is allocated with cells ({south.Length} of {layouts.Sum(l => l.Grids.Count)})");
            Require(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity, $"{name}: the table fits ({snapshot.WindowCount})");
            var metadata = module.DescribeWindows(new BuildContext { World = view, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
            var ids = metadata.Windows.Select(w => w.Id).Concat(metadata.Facades.SelectMany(f => Enumerable.Range(0, f.Rows * f.Columns)
                .Where(cell => !f.Skip.Contains(cell)).Select(cell => f.BaseId + cell / f.Columns * f.TotalColumns + f.ColumnStart + cell % f.Columns))).ToArray();
            Require(ids.Length > 0 && ids.All(id => south.Any(a => id >= a.BaseId && id < a.BaseId + a.Count)), $"{name}: the zone's windows lie in its own blocks");
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

    private sealed class LayoutComparer : IEqualityComparer<ClaimedWindowLayout>
    {
        public static readonly LayoutComparer Instance = new();
        public bool Equals(ClaimedWindowLayout? a, ClaimedWindowLayout? b) => a is not null && b is not null && a.Feature == b.Feature && a.Grids.SequenceEqual(b.Grids);
        public int GetHashCode(ClaimedWindowLayout l) => l.Feature.GetHashCode();
    }

    [TestCase]
    public void GuardHouseAndPorticoGlassIsNumberedOutward()
    {
        var world = World();
        var houses = Gate.SouthGateSite.Openings(0f);
        Require(houses.Length == 6 && houses.Select(o => (o.Column, o.Row)).Distinct().Count() == 6, "six guard-house windows, one cell each");
        foreach (var o in houses)
        {
            // One-sided glass: each pane must face away from its own house's centre.
            var house = o.Row == 0 ? SouthTune.Gate.HouseWest : SouthTune.Gate.HouseEast;
            var d = new Vector2(o.Origin.X, o.Origin.Z) - house;
            Require(Vector2.Dot(d, new(o.Outward.X, o.Outward.Z)) > 2.5f, $"guard-house pane ({o.Column}, {o.Row}) faces out of its house");
        }
        var f = Xuanbang.XuanbangSite.Rings(world);
        var porticoes = Xuanbang.XuanbangSite.Porticoes(f);
        Require(porticoes.Count == 1, "one pediment portico on the main front");
        var glass = Xuanbang.XuanbangSite.PorticoOpenings(porticoes[0], 0.45f, 0);
        Require(glass.Length == 6 && glass.Count(o => o.Row == 1) == 3, "three doors and three arched windows");
        Require(glass.All(o => Vector2.Dot(new(o.Outward.X, o.Outward.Z), porticoes[0].W) > 0.99f), "the hall glass faces out through the colonnade");
        Require(glass.Where(o => o.Row == 1).All(o => o.Origin.Y > 0.45f + SouthTune.Xuanbang.DoorSpring), "the windows sit above the doors");
    }

    [TestCase]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = World();
        var module = new SouthModule();
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
        Require(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier")), "declared IDs lie in the zone's blocks");

        HashSet<int> Glass(LodLevel lod)
        {
            var ids = new HashSet<int>();
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var glass = module.Build(ctx, new(x, z), lod).Meshes.Where(m => m.Uv2 is not null).ToArray();
                Require(glass.Length <= 2, $"{lod} cell ({x}, {z}): numbered panes merge into one bundle ({glass.Length})");
                foreach (var mesh in glass)
                    for (var v = 0; v < mesh.Uv2!.Length / 2; v++)
                    {
                        var columns = (int)MathF.Floor(mesh.Uv2[2 * v + 1] / 32f);
                        ids.Add((int)mesh.Uv2[2 * v] + (int)MathF.Floor(mesh.Uv![2 * v + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * v]));
                    }
            }
            return ids;
        }
        var near = Glass(LodLevel.L0);
        Require(near.SetEquals(declared), $"L0 glass carries exactly the declared windows ({near.Count} vs {declared.Count})");
        Require(Glass(LodLevel.L3).IsSubsetOf(declared), "coarse glass keeps the same numbers");
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

    private static bool Covers(Collider c, Vector2 p, float y) => c switch
    {
        BoxCollider box => InBox(box, p) && MathF.Abs(y - box.At.M42) <= box.Half.Y,
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + 0.26f && y >= wall.Y0 && y <= wall.Y1,
        PostCollider post => Vector2.Distance(p, post.C) <= post.R + 0.26f && y >= post.Y0 && y <= post.Y1,
        _ => false,
    };

    private static bool Near(IReadOnlyList<Vector2> ring, Vector2 p, float margin) =>
        p.X >= ring.Min(q => q.X) - margin && p.X <= ring.Max(q => q.X) + margin && p.Y >= ring.Min(q => q.Y) - margin && p.Y <= ring.Max(q => q.Y) + margin;

    private static float MaxY(GeometryBundle mesh)
    {
        var y = float.MinValue;
        for (var i = 1; i < mesh.Position.Length; i += 3) y = MathF.Max(y, mesh.Position[i]);
        return y;
    }

    private static float MinY(GeometryBundle mesh)
    {
        var y = float.MaxValue;
        for (var i = 1; i < mesh.Position.Length; i += 3) y = MathF.Min(y, mesh.Position[i]);
        return y;
    }

    private static bool Covers(Collider c, Vector2 p) => c switch
    {
        BoxCollider box => InBox(box, p),
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + 0.26f,
        _ => false,
    };

    private static bool InBox(BoxCollider box, Vector2 p)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + 0.26f && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + 0.26f;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    private static byte[] Bytes(BuildContext ctx)
    {
        var module = new SouthModule(SouthSites.All.ToArray());
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
        // The south sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { World = World(), Map = null!, WorldSeed = 1 };
    }

    private static string DataRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    private static IWorldModel World()
    {
        var data = DataRoot();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "south.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

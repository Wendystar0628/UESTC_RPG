using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Zones.North.Clusters;
using Campus.Gen.Zones.North.Framework;
using Campus.Gen.Zones.North.Xuezhi;
using GdUnit4;

namespace Campus.Gen.Zones.North.Tests;

[TestSuite]
public sealed class NorthModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void OverlayLoadsStylesAndGateAdditions()
    {
        var world = World();
        foreach (var cluster in NorthTune.Clusters)
        {
            Require(world.Features.ContainsKey(new(cluster.Id)), $"{cluster.Name} polygon exists");
            Require(world.Features.ContainsKey(new("design:north-gate-" + cluster.Key)), $"{cluster.Name} gate addition is registered");
            foreach (var building in cluster.Buildings)
                Require(world.Features[new(building)].Facade?.Value == cluster.Facade, $"{building} carries the {cluster.Name} facade preset");
        }
    }

    [TestCase]
    public void EveryClusterGetsAMainGateAndAClosedFence()
    {
        var world = World();
        foreach (var cluster in NorthTune.Clusters)
        {
            var layout = ClusterLayout.Of(world, cluster) ?? throw new InvalidOperationException($"{cluster.Name} has no layout");
            Require(layout.Gates.Count(g => g.Main) == 1, $"{cluster.Name} has one main gate");
            Require(layout.Gates.Count(g => g.Open) == (cluster.Open ? 1 : 0), $"{cluster.Name} open gates");
            Require(layout.Runs.Count > 0, $"{cluster.Name} has fence runs");
            foreach (var run in layout.Runs)
                for (var i = 0; i + 1 < run.Posts.Count; i++)
                    Require(Vector2.Distance(run.Posts[i], run.Posts[i + 1]) <= NorthTune.Fence.PostPitch + 0.01f, $"{cluster.Name} post gap at {run.Posts[i]}");
        }
    }

    [TestCase]
    public void OpenGateLeavesPassagesAndClosedGatesBarTheLine()
    {
        var ctx = Context();
        var module = new NorthModule();
        var colliders = module.CollidersFor(ctx).Select(i => i.Collider).ToArray();
        foreach (var cluster in NorthTune.Clusters)
        {
            var layout = ClusterLayout.Of(ctx.World, cluster)!;
            foreach (var gate in layout.Gates.Where(g => g.Kind == GateKind.Vehicle))
            {
                var f = gate.Frame;
                var cabinets = gate.Cabinets().ToArray();
                for (var k = 0; k + 1 < cabinets.Length; k++)
                {
                    var middle = f.P((cabinets[k] + cabinets[k + 1]) * 0.5f, 0);
                    var blocked = colliders.Any(c => Covers(c, middle, 0f));
                    Require(blocked != gate.Open, $"{cluster.Name} gate passage {k} is {(gate.Open ? "open" : "closed")}");
                }
                // The lane under the barrier arm is closed either way, and nothing crosses the gate line above 0.5 m wide.
                Require(colliders.Any(c => Covers(c, f.P(0, 0), 0f)), $"{cluster.Name} vehicle lane is barred");
                var tops = colliders.OfType<BoxCollider>().Where(c => Covers(c, f.P(cabinets[0], 0), 0f));
                Require(tops.All(b => b.At.M42 + b.Half.Y >= NorthTune.Gate.ColliderTop - 0.01f), $"{cluster.Name} cabinets reach 1.5 m");
            }
        }
    }

    [TestCase]
    public void HallPorchMeetsTheDeclaredEntrance()
    {
        var world = World();
        var porch = HallEntranceSite.Locate(world) ?? throw new InvalidOperationException("学知苑 1 栋 porch not located");
        var door = new Vector2(porch.Entrance.Center.X, porch.Entrance.Center.Z);
        var local = porch.Frame.Local(door);
        Require(MathF.Abs(local.X) < 0.5f && MathF.Abs(local.Y) < 1.2f, $"porch axis passes the door centre (local {local})");
        Require(Vector2.Dot(porch.Frame.W, NorthSite.Direction(porch.Entrance.YawDeg)) > 0.9f, "porch faces the declared facing");
        Require(porch.Risers is >= NorthTune.Hall.MinRisers and <= NorthTune.Hall.MaxRisers, "6–8 risers");
        Require(MathF.Abs(porch.Floor - porch.Entrance.Center.Y) < 0.01f, "landing at the declared floor level");
        var outside = new Vector2(porch.Entrance.Outside.X, porch.Entrance.Outside.Z);
        var landing = porch.Frame.Local(outside);
        Require(landing.Y > NorthTune.Hall.WallFace && landing.Y < porch.FrontZ, "teleport landing lies on the porch landing");
    }

    [TestCase]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new NorthModule();
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
            string[] entranceSet = [Campus.Contracts.Parts.PartCatalog.EntranceMat, Campus.Contracts.Parts.PartCatalog.OpenSign, Campus.Contracts.Parts.PartCatalog.EntranceLamp];
            Require(!items.Any(i => i.Part is { } p && entranceSet.Contains(p.Part.Value)), $"{lod}: the open-entrance set is WP-13's, not placed by the zone");
        }
    }

    [TestCase]
    public void HeaviestCellsStayInsideTheDeclaredBudget()
    {
        var ctx = Context();
        var module = new NorthModule();
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
    public void SitesStayInsideTheNorthZone()
    {
        var ctx = Context();
        var module = new NorthModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            Require(item.Anchor.X is > -260 and < 760 && item.Anchor.Y is > -1000 and < -150, $"item at {item.Anchor} is in the north zone");
    }

    [TestCase]
    public void TerrainOverlayParsesAndReferencesRealRoads()
    {
        var file = Terrain();
        Require(file.Zone.Value == "north" && file.Owner.Value == "WP-20", "file zone and owner");
        var roads = World().Network.Roads.ToDictionary(r => r.Id);
        foreach (var op in file.Ops)
            switch (op)
            {
                case TerrainPlatformOp p:
                    Require(p.Y is NorthTune.Terrain.Upper or NorthTune.Terrain.Lower, $"{op.Id}: terrace at +3.0 or +1.5"); break;
                case TerrainMoundOp or TerrainBasinOp:
                    var (shape, height) = op is TerrainMoundOp m ? (m.Shape, m.Height) : (((TerrainBasinOp)op).Shape, ((TerrainBasinOp)op).Height);
                    var ellipse = (TerrainEllipse)shape;
                    var steepest = 1.5 * height / Math.Min(ellipse.Radii.X, ellipse.Radii.Y);
                    Require(height <= 1.5 && steepest <= 1.0 / 3 + 1e-6, $"{op.Id}: R6 relief ≤ 1.5 m, no steeper than 1:3");
                    // The courtyard hill is an activity lawn (Opus 13:32, R6): short radius at least 7.5 × height (1:5).
                    if (op.Id == "design:terrain:north-xz1-court-hill")
                        Require(Math.Min(ellipse.Radii.X, ellipse.Radii.Y) >= 7.5 * height - 1e-6, $"{op.Id}: activity hill no steeper than 1:5");
                    break;
                case TerrainSlopeOp s:
                    Require(s.Geometry is PolylineGeometry && Math.Abs(s.Y1 - s.Y0) / ((PolylineGeometry)s.Geometry).Line.Points.Zip(((PolylineGeometry)s.Geometry).Line.Points.Skip(1), Vector2.Distance).Sum() <= 0.0501,
                        $"{op.Id}: the 通学坡 stays at or below 5 %"); break;
                case TerrainCorridorOp c:
                    Require(roads.TryGetValue(c.Road, out var road), $"{op.Id}: road {c.Road.Value} exists");
                    var length = road!.Line.Points.Zip(road.Line.Points.Skip(1), (a, b) => (double)Vector2.Distance(a, b)).Sum();
                    Require(Math.Abs(c.Anchors[^1].S - length) < 0.02, $"{op.Id}: last anchor at the full centreline length {length:0.00}");
                    break;
                default: throw new InvalidOperationException($"{op.Id}: unexpected operation for 20a-2");
            }
        Require(file.Ops.OfType<TerrainPlatformOp>().Count() == 3, "upper terrace and the two lower wings");
    }

    /// <summary>
    /// 20a-3: a road with no corridor keeps the automatic 0 m profile and would cut a trench across the terrace, so every
    /// line road that runs onto a terrace carries anchors; every shared centreline vertex has one elevation; the authored
    /// profile keeps to 5 %, and to 3 % within 10 m of a junction (WLD-TERRAIN §3.8, §4.2).
    /// </summary>
    [TestCase]
    public void EveryRoadOnATerraceHasAnchorsAndJunctionsAgree()
    {
        var file = Terrain();
        var roads = World().Network.Roads.Where(r => r.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("highway", out _)
            && !(p.TryGetProperty("area", out var area) && area.GetString() == "yes")).ToArray();
        var corridors = file.Ops.OfType<TerrainCorridorOp>().ToDictionary(c => c.Road);
        var terraces = file.Ops.OfType<TerrainPlatformOp>().Select(p => p.Rings.Outer).ToArray();
        foreach (var road in roads)
            if (Samples(road.Line.Points).Any(p => terraces.Any(t => Framework.Plan.Contains(t, p))))
                Require(corridors.ContainsKey(road.Id), $"{road.Id.Value} runs onto a terrace without corridor anchors");
        foreach (var c in corridors.Values)
            for (var i = 0; i + 1 < c.Anchors.Count; i++)
                Require(Math.Abs(Grade(c.Anchors, i)) <= 0.05 + 1e-6, $"{c.Id}: piece at s {c.Anchors[i].S} is steeper than 5 %");

        var junctions = new Dictionary<(long, long), List<(NetworkLine Road, double S)>>();
        foreach (var road in roads)
        {
            var s = 0.0;
            for (var i = 0; i < road.Line.Points.Count; i++)
            {
                if (i > 0) s += Vector2.Distance(road.Line.Points[i - 1], road.Line.Points[i]);
                var p = road.Line.Points[i];
                var key = ((long)Math.Round(p.X * 1000d), (long)Math.Round(p.Y * 1000d));
                if (!junctions.TryGetValue(key, out var list)) junctions[key] = list = [];
                if (!list.Any(e => e.Road.Id == road.Id)) list.Add((road, s));
            }
        }
        var checkedJunctions = 0;
        foreach (var (key, list) in junctions.Where(j => j.Value.Count > 1))
        {
            checkedJunctions++;
            var first = Elevation(corridors, list[0].Road, list[0].S);
            foreach (var (road, s) in list)
            {
                Require(Math.Abs(Elevation(corridors, road, s) - first) <= 1e-4, $"junction {key} of {road.Id.Value}: {Elevation(corridors, road, s):0.000} vs {first:0.000}");
                if (!corridors.TryGetValue(road.Id, out var c)) continue;
                for (var i = 0; i + 1 < c.Anchors.Count; i++)
                    if (c.Anchors[i + 1].S > s - 10 && c.Anchors[i].S < s + 10)
                        Require(Math.Abs(Grade(c.Anchors, i)) <= 0.03 + 1e-6, $"{c.Id}: steeper than 3 % within 10 m of the junction at s {s:0.0}");
            }
        }
        Require(checkedJunctions > 1000, "the whole network's junctions were checked");
    }

    /// <summary>
    /// Main session 2026-10-16 15:0x: 未命名建筑 5561 must not straddle a terrace edge (it showed a 1.4 m step, over the
    /// 1.2 m allowed without a structure). Its whole footprint stands on one level and every terrace crest keeps 6 m off it.
    /// </summary>
    [TestCase]
    public void Building5561StandsOnOneLevelClearOfTheTerraceEdge()
    {
        var footprint = Framework.Plan.BuildingRings(World().Features[new("qsh:way:550365561")]).Single();
        foreach (var terrace in Terrain().Ops.OfType<TerrainPlatformOp>())
        {
            var ring = terrace.Rings.Outer;
            var inside = footprint.Count(p => Framework.Plan.Contains(ring, p));
            Require(inside == 0 || inside == footprint.Count, $"5561 straddles {terrace.Id}");
            var clear = float.MaxValue;
            for (var i = 0; i < footprint.Count; i++)
                for (var j = 0; j < ring.Count; j++)
                {
                    Vector2 a0 = footprint[i], a1 = footprint[(i + 1) % footprint.Count], b0 = ring[j], b1 = ring[(j + 1) % ring.Count];
                    clear = Framework.Plan.Intersect(a0, a1, b0, b1) is null ? MathF.Min(clear, MathF.Min(DistanceToSegment(a0, b0, b1), DistanceToSegment(b0, a0, a1))) : 0f;
                }
            Require(clear >= 6f, $"{terrace.Id}: crest {clear:0.00} m from 5561, at least 6 m");
        }
    }

    /// <summary>
    /// max-108 0h-b (Opus 14:20; geometry 14:26/28): the 丹桂路 design walks. The overlay parses strictly; each walk is a 2.5 m
    /// footway 7.5 m off the road centre (3 m outside the 4.5 m osmanthus row); its surface keeps 3 m off every building and
    /// water edge; it lies on the 0 m ground 丹桂路 is pinned to, clear of every terrace's side slope and every raised
    /// corridor (a design road takes no corridor anchors); and each end lies on the surface it names in <c>joins</c>: an
    /// existing walk or pedestrian area, or the kerb of a carriageway that meets 丹桂路 (a junction).
    /// </summary>
    [TestCase]
    public void DanguiWalksKeepOffWallsAndWaterAndJoinTheNetwork()
    {
        const string road = "qsh:source:way:630914103";
        const float offset = 7.5f, half = 1.25f, wallClear = 3f;
        string[] ids = ["design:north-dangui-walk-w1", "design:north-dangui-walk-w2", "design:north-dangui-walk-e1"];
        var strict = new JsonSerializerOptions(ContractJson.Options) { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
        var overlay = JsonSerializer.Deserialize<OverlayFile>(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "north.overlay.json")), strict)
            ?? throw new InvalidOperationException("north.overlay.json is empty");
        Require(overlay.Zone.Value == "north" && overlay.Owner.Value == "WP-20", "overlay zone and owner");
        var walks = overlay.Ops.OfType<AddOp>().Where(a => a.Id.Value.StartsWith("design:north-dangui-walk-", StringComparison.Ordinal)).ToArray();
        Require(walks.Select(a => a.Id.Value).SequenceEqual(ids), "the three 丹桂路 walks, in order");

        var world = World();
        var dangui = world.Network.Roads.Single(r => r.Id.Value == road).Line.Points;
        var walls = world.Features.Values.SelectMany(f => f.Parts.Count > 0
                ? f.Parts.Where(p => (Text(p.Props, "kind") ?? f.Kind.Value) is "building" or "water").SelectMany(p => Framework.Plan.Polygons(p.Geometry))
                : f.Kind.Value is "building" or "water" ? Framework.Plan.Polygons(f.Geometry) : [])
            .Select(r => r.Outer).ToArray();
        var terrain = Terrain().Ops;
        var corridors = terrain.OfType<TerrainCorridorOp>().ToDictionary(c => c.Road);
        var roads = world.Network.Roads.ToDictionary(r => r.Id);
        foreach (var add in walks)
        {
            var props = add.Props;
            Require(add.Kind.Value == "road" && Text(props, "roadType") == "footway" && props!.Value.GetProperty("width").GetSingle() == 2.5f, $"{add.Id.Value}: a 2.5 m footway WP-15 paves");
            Require(!string.IsNullOrWhiteSpace(add.Reason), $"{add.Id.Value}: reason given");
            var line = ((PolylineGeometry)add.Geometry).Line.Points;
            Require(world.Features[add.Id].Geometry is PolylineGeometry loaded && loaded.Line.Points.SequenceEqual(line), $"{add.Id.Value}: loaded into the world model");
            foreach (var p in line)
            {
                var d = PolylineDistance(p, dangui);
                Require(MathF.Abs(d - offset) <= 0.2f, $"{add.Id.Value}: {p} is {d:0.00} m off the road centre, not {offset}");
                Require(p.X is > -260 and < 760 && p.Y is > -1000 and < -150, $"{add.Id.Value}: {p} is in the north zone");
            }
            foreach (var p in Samples(line))
            {
                var wall = walls.Min(r => Framework.Plan.Contains(r, p) ? 0f : RingDistance(p, r)) - half;
                Require(wall >= wallClear, $"{add.Id.Value}: surface at {p} is {wall:0.00} m from a wall or water edge");
                foreach (var water in world.Network.Waterways)
                    Require(PolylineDistance(p, water.Line.Points) - water.Width * 0.5f - half >= wallClear, $"{add.Id.Value}: {p} keeps off {water.Id.Value}");
                foreach (var op in terrain)
                    switch (op)
                    {
                        // The side slope reaches y·n beyond the crest, plus half the fillet (WLD-TERRAIN §3.5).
                        case TerrainPlatformOp t:
                            var n = double.Parse(t.Side.Fill.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture);
                            var foot = Math.Abs(t.Y) * n + t.Fillet * 0.5;
                            Require(!Framework.Plan.Contains(t.Rings.Outer, p) && RingDistance(p, t.Rings.Outer) - half >= foot, $"{add.Id.Value}: {p} is on {t.Id} or its side slope");
                            break;
                        case TerrainMoundOp or TerrainBasinOp:
                            var e = (TerrainEllipse)(op is TerrainMoundOp m ? m.Shape : ((TerrainBasinOp)op).Shape);
                            double yaw = e.YawDeg * Math.PI / 180, dx = p.X - e.Center.X, dz = p.Y - e.Center.Y;
                            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
                            Require(Math.Pow(along / (e.Radii.X + half), 2) + Math.Pow(across / (e.Radii.Y + half), 2) >= 1, $"{add.Id.Value}: {p} is on {op.Id}");
                            break;
                    }
                // Where a road crosses or runs beside the walk, its authored profile is 0 m there.
                foreach (var (id, c) in corridors)
                {
                    var hit = Project(p, roads[id].Line.Points);
                    if (hit.Distance <= roads[id].Width * 0.5f + c.Shoulder + half)
                        Require(Math.Abs(Elevation(corridors, roads[id], hit.S)) < 0.01, $"{add.Id.Value}: {p} meets {c.Id} at {Elevation(corridors, roads[id], hit.S):0.00} m");
                }
            }
            var joins = props!.Value.GetProperty("joins").EnumerateArray().Select(j => j.GetString()!).ToArray();
            Require(joins.Length == 2, $"{add.Id.Value}: names what each end joins");
            foreach (var (end, to) in new[] { (line[0], joins[0]), (line[^1], joins[1]) })
            {
                if (roads.TryGetValue(new(to), out var target))
                {
                    var type = Text(target.Props, "highway");
                    var reach = type is "footway" or "path" or "pedestrian" or "steps" ? MathF.Max(target.Width, 2f) * 0.5f : target.Width * 0.5f;
                    Require(PolylineDistance(end, target.Line.Points) <= reach + 0.05f, $"{add.Id.Value}: end {end} reaches {to}");
                    Require(type is "footway" or "path" or "pedestrian" or "steps" || Meets(target.Line.Points, dangui), $"{add.Id.Value}: {to} is a walk or a road that meets 丹桂路");
                }
                else
                {
                    var area = world.Features[new(to)];
                    Require(area.Kind.Value == "road" && Text(area.Props, "roadType") == "pedestrian", $"{add.Id.Value}: {to} is a pedestrian area");
                    var rings = Framework.Plan.Polygons(area.Geometry).Select(r => r.Outer).ToArray();
                    Require(rings.Any(r => Framework.Plan.Contains(r, end) || RingDistance(end, r) <= 0.05f), $"{add.Id.Value}: end {end} reaches {to}");
                }
            }
        }
    }

    private static bool Meets(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b) =>
        a.Any(p => b.Contains(p)) || Enumerable.Range(0, a.Count - 1).Any(i => Enumerable.Range(0, b.Count - 1).Any(k => Framework.Plan.Intersect(a[i], a[i + 1], b[k], b[k + 1]) is not null));

    private static (float Distance, double S) Project(Vector2 p, IReadOnlyList<Vector2> line)
    {
        (float Distance, double S) best = (float.MaxValue, 0);
        var run = 0.0;
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var closest = Framework.Plan.Closest(p, line[i], line[i + 1]);
            var d = Vector2.Distance(p, closest);
            if (d < best.Distance) best = (d, run + Vector2.Distance(line[i], closest));
            run += Vector2.Distance(line[i], line[i + 1]);
        }
        return best;
    }

    private static float RingDistance(Vector2 p, IReadOnlyList<Vector2> ring) =>
        Enumerable.Range(0, ring.Count).Min(i => DistanceToSegment(p, ring[i], ring[(i + 1) % ring.Count]));

    private static float PolylineDistance(Vector2 p, IReadOnlyList<Vector2> line) =>
        Enumerable.Range(0, line.Count - 1).Min(i => DistanceToSegment(p, line[i], line[i + 1]));

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double Grade(IReadOnlyList<TerrainAnchor> a, int i) => (a[i + 1].Y - a[i].Y) / (a[i + 1].S - a[i].S);

    /// <summary>Authored profile at a station (anchors interpolate linearly; nodes sit on anchors or inside straight pieces); 0 without a corridor.</summary>
    private static double Elevation(Dictionary<FeatureId, TerrainCorridorOp> corridors, NetworkLine road, double s)
    {
        if (!corridors.TryGetValue(road.Id, out var c)) return 0;
        var a = c.Anchors;
        for (var i = 0; i + 1 < a.Count; i++)
            if (s <= a[i + 1].S) return a[i].Y + (a[i + 1].Y - a[i].Y) * Math.Clamp((s - a[i].S) / (a[i + 1].S - a[i].S), 0, 1);
        return a[^1].Y;
    }

    private static IEnumerable<Vector2> Samples(IReadOnlyList<Vector2> line)
    {
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(line[i], line[i + 1])));
            for (var k = 0; k < steps; k++) yield return Vector2.Lerp(line[i], line[i + 1], (float)k / steps);
        }
        yield return line[^1];
    }

    /// <summary>
    /// 20a-3 (Opus 13:32): the seat wall stands at the courtyard walk facing the hill and the lawn, a paved pad joins it to
    /// the walk, and its back keeps 8 m off every dormitory window.
    /// </summary>
    [TestCase]
    public void CourtyardSeatWallFacesTheHillFromTheWalk()
    {
        var world = World();
        var registered = (PolylineGeometry)world.Features[new("design:north-xz1-seatwall")].Geometry;
        Require(registered.Line.Points.SequenceEqual([NorthTune.Court.SeatStart, NorthTune.Court.SeatMid, NorthTune.Court.SeatEnd]), "overlay and tune agree on the seat wall");
        var hill = (TerrainEllipse)Terrain().Ops.OfType<TerrainMoundOp>().Single(o => o.Id == "design:terrain:north-xz1-court-hill").Shape;
        var yaw = hill.YawDeg * Math.PI / 180;
        var path = CourtyardSite.Path(NorthTune.Court.SeatSegments);
        var front = CourtyardSite.Front();
        var mid = (NorthTune.Court.SeatStart + NorthTune.Court.SeatEnd) * 0.5f;
        Require(Vector2.Dot(front, Vector2.Normalize(hill.Center - mid)) > 0.9f, "the seat faces the hill");
        var walk = world.Network.Roads.Single(r => r.Id.Value == NorthTune.Court.Walk);
        float ToWalk(Vector2 p) => Enumerable.Range(0, walk.Line.Points.Count - 1).Min(i => DistanceToSegment(p, walk.Line.Points[i], walk.Line.Points[i + 1]));
        Require(ToWalk(mid + front) < ToWalk(mid), "the walk lies in front of the seat");
        // The pad's straight edge runs along the walk's paving edge, 2 cm short of it, over the whole length of the wall.
        var pad = CourtyardSite.PadOutline();
        var edge = pad.Where(p => MathF.Abs(Vector2.Dot(p - NorthTune.Court.SeatStart, front) - NorthTune.Court.PadReach) < 0.01f).ToArray();
        Require(edge.Length == 2, "the pad has one straight edge toward the walk");
        foreach (var p in edge)
            Require(MathF.Abs(ToWalk(p) - (walk.Width * 0.5f + 0.02f)) < 0.05f, $"pad corner {p} meets the walk's edge");
        var buildings = world.Features.Values.SelectMany(Framework.Plan.BuildingRings)
            .Where(ring => ring.Any(q => Vector2.Distance(q, mid) < 60f)).ToArray();
        Require(buildings.Length > 0, "dormitories around the courtyard found");
        foreach (var p in path)
        {
            double dx = p.X - hill.Center.X, dz = p.Y - hill.Center.Y;
            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
            Require(along * along / (hill.Radii.X * hill.Radii.X) + across * across / (hill.Radii.Y * hill.Radii.Y) > 1.2, $"{p} is on level ground off the hill");
            foreach (var road in world.Network.Roads)
                for (var i = 0; i + 1 < road.Line.Points.Count; i++)
                    Require(DistanceToSegment(p, road.Line.Points[i], road.Line.Points[i + 1]) > road.Width * 0.5f + NorthTune.Court.BodyWidth, $"{p} stands off the paving of {road.Id.Value}");
            foreach (var ring in buildings)
                for (var i = 0; i < ring.Count; i++)
                    Require(DistanceToSegment(p, ring[i], ring[(i + 1) % ring.Count]) - NorthTune.Court.BodyWidth * 0.5f >= 8f + NorthTune.Hall.WallFace,
                        $"{p} keeps 8 m off the windows");
        }
    }

    private static bool Covers(Collider c, Vector2 p, float slack) => c switch
    {
        BoxCollider box => InBox(box, p, slack),
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + slack,
        _ => false,
    };

    private static bool InBox(BoxCollider box, Vector2 p, float slack)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + slack && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + slack;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    private static byte[] Bytes(BuildContext ctx)
    {
        var module = new NorthModule(NorthSites.All.ToArray());
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
            foreach (var collider in module.Collide(ctx, tile)) writer.Write(collider.ToString());
        }
        return stream.ToArray();
    }

    private static BuildContext Context() => new() { World = World(), Map = null!, WorldSeed = 1 };

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    private static TerrainOverlayFile Terrain() =>
        TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "terrain", "north.terrain.json")));

    private static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json", "overlay/north.overlay.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

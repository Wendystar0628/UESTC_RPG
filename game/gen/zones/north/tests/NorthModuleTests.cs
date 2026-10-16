using System.Numerics;
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
                    Require(height <= 1.5 && steepest <= 1.0 / 3 + 1e-6, $"{op.Id}: R6 relief ≤ 1.5 m, no steeper than 1:3"); break;
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

    [TestCase]
    public void CourtyardSeatWallStandsOnTheLawnBesideTheHill()
    {
        var world = World();
        var registered = (PolylineGeometry)world.Features[new("design:north-xz1-seatwall")].Geometry;
        Require(registered.Line.Points.SequenceEqual([NorthTune.Court.SeatStart, NorthTune.Court.SeatMid, NorthTune.Court.SeatEnd]), "overlay and tune agree on the seat wall");
        var hill = (TerrainEllipse)Terrain().Ops.OfType<TerrainMoundOp>().Single(o => o.Id == "design:terrain:north-xz1-court-hill").Shape;
        var yaw = hill.YawDeg * Math.PI / 180;
        var path = CourtyardSite.Path(NorthTune.Court.SeatSegments);
        var hall = Framework.Plan.BuildingRings(world.Features[new(NorthTune.Hall.Feature)]).First();
        foreach (var p in path)
        {
            double dx = p.X - hill.Center.X, dz = p.Y - hill.Center.Y;
            double along = dx * Math.Sin(yaw) - dz * Math.Cos(yaw), across = dx * Math.Cos(yaw) + dz * Math.Sin(yaw);
            Require(along * along / (hill.Radii.X * hill.Radii.X) + across * across / (hill.Radii.Y * hill.Radii.Y) > 1.2, $"{p} is on level lawn off the hill");
            foreach (var road in world.Network.Roads)
                for (var i = 0; i + 1 < road.Line.Points.Count; i++)
                    Require(DistanceToSegment(p, road.Line.Points[i], road.Line.Points[i + 1]) > road.Width * 0.5f + 0.75f + 1.5f, $"{p} keeps clear of {road.Id.Value}");
            for (var i = 0; i < hall.Count; i++)
                Require(DistanceToSegment(p, hall[i], hall[(i + 1) % hall.Count]) > 6f, $"{p} keeps the hall's grading band clear");
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

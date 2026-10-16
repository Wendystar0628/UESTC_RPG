using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Zones.CenterEast.Pinxue;
using GdUnit4;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;

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
    public void ClaimsNameExistingFeaturesAndCoverTheFrameAndBridges()
    {
        var world = World();
        var module = new CenterEastModule();
        Require(module.Claims.Count == 1, "only the 品学楼 entity (frame bar and link bridges) is claimed");
        foreach (var claim in module.Claims.OfType<FeatureClaim>())
        {
            Require(world.Features.TryGetValue(claim.Feature, out var feature), $"claimed feature {claim.Feature.Value} exists");
            Require((claim.Aspects & Aspects.Massing) != 0, "claims cover massing so WP-14 skips the extrusion");
            var parts = feature!.Parts.Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Require(parts.SequenceEqual([P.FrameBar, P.BridgeBC, P.BridgeAC]), "the claim holds exactly the frame bar and the two bridges");
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
        var columns = colliders.OfType<BoxCollider>().Count(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y < 0.01f && b.Half.Y * 2 >= P.BeamBottom - 0.01f);
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
        var tiers = props.GetProperty("tiers").EnumerateArray().ToArray();
        float[] rims = [P.BasinRim, P.MiddleRim, P.TopRim];
        float[] water = [P.BasinWater, P.MiddleWater, P.TopWater];
        for (var i = 0; i < 3; i++)
        {
            Require(MathF.Abs(tiers[i].GetProperty("rimAbove").GetSingle() - rims[i]) < 1e-3f, $"tier {i} rim");
            Require(MathF.Abs(tiers[i].GetProperty("waterAbove").GetSingle() - water[i]) < 1e-3f, $"tier {i} water");
            Require(water[i] < rims[i], $"tier {i} water stays below its rim");
        }
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
    public void SitesStayInsideTheCenterEastZone()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            Require(item.Anchor.X is > 60 and < 1000 && item.Anchor.Y is > -800 and < 600, $"item at {item.Anchor} is in the center-east zone");
        foreach (var probe in module.Probes)
            Require(world(probe.Feature), $"probe {probe.Id} names an existing feature");

        bool world(FeatureId id) => ctx.World.Features.ContainsKey(id);
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

    private static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json", "overlay/center-east.overlay.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

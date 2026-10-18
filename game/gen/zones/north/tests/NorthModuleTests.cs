using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Zones.North.Clusters;
using Campus.Gen.Zones.North.Framework;
using GdUnit4;

namespace Campus.Gen.Zones.North.Tests;

using static Campus.Gen.Zones.North.Tests.NorthTestSupport;

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
            // The registered gate point is the table's MainGate: moving a main gate moves both.
            var at = ((PointGeometry)world.Features[new("design:north-gate-" + cluster.Key)].Geometry).Position;
            Require(Vector2.Distance(new(at.X, at.Z), cluster.MainGate) < 0.05f, $"{cluster.Name} registered gate point is its MainGate");
            foreach (var building in cluster.Buildings)
                Require(world.Features[new(building)].Facade?.Value == cluster.Facade, $"{building} carries the {cluster.Name} facade preset");
        }
    }

    /// <summary>
    /// 20b1 第一项 (main session 16:07): every dorm preset key the overlay writes has its expected values in DormPresets.json,
    /// which WP-14 maps into its FacadePresets; every colour named there is a registered palette family and every window a
    /// catalogued part.
    /// </summary>
    [TestCase]
    public void DormPresetTableCoversEveryOverlayKey()
    {
        var game = Path.GetFullPath(Path.Combine(DataDirectory(), "..", "..", ".."));
        using var table = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(game, "gen", "zones", "north", "DormPresets.json")));
        using var palette = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(game, "style", "palette", "core.json")));
        var tokens = palette.RootElement.GetProperty("entries").EnumerateArray().Select(e => e.GetProperty("token").GetString()).ToHashSet();
        var parts = typeof(Campus.Contracts.Parts.PartCatalog).GetFields().Where(f => f.IsLiteral).Select(f => f.GetValue(null) as string).ToHashSet();
        var presets = table.RootElement.GetProperty("presets").EnumerateArray().ToDictionary(p => p.GetProperty("key").GetString()!);
        foreach (var feature in World().Features.Values)
            if (feature.Facade is { Value: { Length: > 0 } key })
                Require(presets.ContainsKey(key), $"{feature.Id.Value}: preset {key} has no entry in DormPresets.json");
        foreach (var (key, p) in presets)
        {
            foreach (var field in new[] { "glass", "frame", "trim", "accent" })
                Require(tokens.Contains(p.GetProperty(field).GetString() + ".lit"), $"{key}.{field} is a registered colour family");
            Require(parts.Contains(p.GetProperty("window").GetString()), $"{key}.window is a catalogued part");
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
                Require(tops.All(b => b.At.M42 + b.Half.Y >= WalkRules.BarrierHeight - 0.01f), $"{cluster.Name} cabinets reach 1.5 m");
            }
        }
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

    [TestCase, TestCategory("Slow")]
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
        // East edge: 硕丰苑十组团 runs out to the 磨底河 bank near x 790 (MAP-QSH-NORTH); batched boxes anchor at their
        // 64 m cell's centre, x 800 for that cell. South edge: 博翰一组团's fence reaches z −73 by 4499 (cell centre −96).
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            Require(item.Anchor.X is > -260 and <= 800 && item.Anchor.Y is > -1000 and < -64, $"item at {item.Anchor} is in the north zone");
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
}

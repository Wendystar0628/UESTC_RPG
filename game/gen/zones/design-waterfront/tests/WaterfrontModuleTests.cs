using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using Campus.Gen.Zones.DesignWaterfront.Bridges;
using GdUnit4;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront.Tests;

[TestSuite]
public sealed class WaterfrontModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];
    private const string Yinxing = "design:waterfront-bridge-yinxing";
    private static readonly string[] YinxingLines = ["qsh:source:way:683646721", "qsh:source:way:988857960", "qsh:source:way:988857962"];

    [TestCase]
    public void OverlayRegistersTheBridgesAndKeepsTheNetwork()
    {
        var world = World();
        var site = WaterfrontModule.SiteOf(world);
        Require(site.Bridges.Select(b => b.Id.Value).SequenceEqual(["design:waterfront-bridge-library-north", "design:waterfront-bridge-ximen", Yinxing]), "three bridges in ID order");
        Require(site.Stones.Count == 2, "two bridge-head stones");
        // 路网拓扑照旧: every merged line is still its own network bridge.
        foreach (var id in YinxingLines) Require(world.Network.Bridges.Any(l => l.Id.Value == id), $"{id} stays in the network");
        var yinxing = site.Bridges.Single(b => b.Id.Value == Yinxing);
        Require(yinxing.Lines.Select(l => l.Value).SequenceEqual(YinxingLines), "the merged deck carries all three lines");
        Require(yinxing.Bands.Count == 3 && yinxing.Bands[1].Kind == BandKind.Carriage, "one carriageway between two walking strips");
    }

    [TestCase]
    public void DeckSpansTheWaterOnTwoAbutments()
    {
        var world = World();
        foreach (var plan in WaterfrontModule.SiteOf(world).Bridges)
        {
            Require(plan.Abutments.Count == 2, $"{plan.Id.Value} crosses water and stands on two abutments");
            foreach (var id in plan.Lines)
            {
                var line = world.Network.Bridges.Single(l => l.Id == id);
                var ends = new[] { line.Line.Points[0], line.Line.Points[^1] }.Select(p => Local(plan, p)).OrderBy(p => p.S).ToArray();
                Require(ends.All(e => e.U >= plan.Left - 0.01f && e.U <= plan.Right + 0.01f), $"{id} lies within the walking width");
                // The deck is fitted to the water, not to the mapped line ends; it stays of the line's own order of size.
                var length = plan.EndS(ends[0].U) - plan.StartS(ends[0].U);
                Require(length > 3f && length < ends[1].S - ends[0].S + 6f, $"{plan.Id.Value} deck length {length} along {id}");
            }
            // Both abutments stand under the deck, between its ends.
            foreach (var a in plan.Abutments)
                foreach (var c in new[] { a.A, a.B, a.C, a.D })
                {
                    var (s, u) = Local(plan, c);
                    Require(s >= plan.StartS(u) - 0.01f && s <= plan.EndS(u) + 0.01f, $"{plan.Id.Value} abutment corner {c} lies under the deck");
                }
        }
    }

    private static (float S, float U) Local(BridgePlan plan, Vector2 p)
    {
        var d = p - plan.Origin;
        return (Vector2.Dot(d, plan.Axis), Vector2.Dot(d, plan.Normal));
    }

    [TestCase]
    public void BuildsEveryLevelWithinBudget()
    {
        var ctx = Context();
        var module = new WaterfrontModule();
        var tiles = module.Tiles(ctx).ToArray();
        Require(tiles.Length > 0, "the module occupies cells");
        foreach (var lod in Levels)
        {
            var keys = lod == LodLevel.L4 ? [new TileKey(0, 0)] : tiles.Select(t => new TileKey(t.X >> (int)lod, t.Z >> (int)lod)).Distinct().ToArray();
            foreach (var tile in keys)
            {
                var output = module.Build(ctx, tile, lod);
                foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
                var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
                if (lod == LodLevel.L0) Require(triangles <= T.BudgetTrianglesL0 && output.Meshes.Count <= T.BudgetDrawCalls, $"L0 cell {tile} has {triangles} triangles in {output.Meshes.Count} surfaces");
                if (lod == LodLevel.L1) Require(triangles <= T.BudgetTrianglesL1, $"L1 cell {tile} has {triangles} triangles");
                if (lod == LodLevel.L4) Require(output.Meshes.Count == 0, "nothing at L4");
            }
        }
        var first = module.Build(ctx, tiles[0], LodLevel.L0);
        var again = module.Build(Context(), tiles[0], LodLevel.L0);
        Require(first.Meshes.Count == again.Meshes.Count && first.Meshes.Zip(again.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position)), "deterministic");
    }

    [TestCase]
    public void DeckWalksAndParapetsBlock()
    {
        var ctx = Context();
        var module = new WaterfrontModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var site = WaterfrontModule.SiteOf(ctx.World);
        Require(colliders.Length == site.Colliders.Count, "every collider is filed in exactly one cell");
        foreach (var plan in site.Bridges)
        {
            var ramps = colliders.OfType<RampCollider>().Where(r => Inside(plan, new(r.P0.X, r.P0.Z))).ToArray();
            Require(ramps.Length > 0 && ramps.All(r => r.Tag == SurfaceTag.Ground && r.Foot != FootMat.Auto), $"{plan.Id.Value}: walkable deck ramps with a foot material");
            Require(ramps.All(r => Vector3.Distance(r.P0, r.P3) <= T.RampStep + 0.01f), "ramps are cut every 2 m");
            Require(ramps.All(r => MathF.Abs(r.P0.Y - plan.RoadY) < 1e-4f || MathF.Abs(r.P0.Y - plan.WalkY) < 1e-4f), "ramp tops at the carriageway or walking level");
            var walls = colliders.OfType<WallCollider>().Where(w => Inside(plan, (w.A + w.B) * 0.5f)).ToArray();
            Require(walls.Length == 2 && walls.All(w => w.Tag == SurfaceTag.Barrier && w.Y1 >= plan.WalkY + 1.5f - 1e-4f && w.HalfT <= 0.3f), $"{plan.Id.Value}: two parapets, top at least 1.5 m above the walk");
        }
        Require(colliders.OfType<BoxCollider>().Count() == 2, "each stone is one box");
    }

    private static bool Inside(BridgePlan plan, Vector2 p)
    {
        var d = p - plan.Origin;
        float s = Vector2.Dot(d, plan.Axis), u = Vector2.Dot(d, plan.Normal);
        return u >= plan.OuterLeft - 0.5f && u <= plan.OuterRight + 0.5f && s >= plan.StartS(u) - 0.5f && s <= plan.EndS(u) + 0.5f;
    }

    private static BuildContext Context() => new() { World = World(), Map = null!, WorldSeed = 1u };

    private static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json", "overlay/waterfront.overlay.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "waterfront.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        return directory is null ? throw new DirectoryNotFoundException("game/data/maps/qingshuihe") : Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

using static Campus.Gen.Buildings.Tests.BuildingsTestSupport;

[TestSuite]
public sealed class BuildingMeshTests
{
    [TestCase]
    public void DormitoryHasStoreysBaysAndNumberedWindows()
    {
        var world = World(Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"));
        var set = BuildingSet.For(world, 1u);
        Require(set.Plans.Count == 1, "one plan");
        var plan = set.Plans[0];
        Require(plan.Storeys == 6, "storeys from data");
        Require(plan.TotalColumns >= 20, "bays on all four facades");
        Require(plan.Entrances.Count == 1 && plan.Entrances[0].Anchor is null, "the generic entrance on the longest facade");
        Require(set.WindowCount == plan.TotalColumns * plan.Storeys, "window block covers the building grid");
        var bundles = new BuildingsModule().BuildOne(plan, set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var glass = bundles.Where(b => b.Material.Spec.Kind == ToonKind.Glass).ToArray();
        Require(glass.Length > 0, "glass bundle");
        var block = set.Blocks[0];
        foreach (var g in glass)
            for (var i = 0; i < g.Position.Length / 3; i++)
            {
                Require(g.Uv2![2 * i] == block.Base, "UV2.x is the building window base");
                var packed = (int)MathF.Round(g.Uv2[2 * i + 1]);
                Require(packed / 32 == plan.TotalColumns, "UV2.y carries the grid column count");
                var col = (int)MathF.Floor(g.Uv![2 * i]);
                var row = (int)MathF.Floor(g.Uv[2 * i + 1]);
                var id = block.Base + row * plan.TotalColumns + col;
                Require(id >= block.Base && id < block.Base + block.Count, "window number inside the block");
                Require((g.Reserved![i] & WindowCardEncoding.TypeMask) is >= 1 and <= 15, "window card type in the spare byte");
            }
    }

    [TestCase]
    public void EveryLevelIsValidWoundAndFinite()
    {
        var world = World(
            Feature("t:a", Rect(0, 0, 48, 16), 5, 19.5f, "teaching"),
            Feature("t:b", LShape(100, 0), 4, 15.6f, "research"),
            Feature("t:c", Courtyard(0, 100), 3, 12f, "residence"),
            Feature("t:d", Rect(200, 0, 5, 6), 1, 3f, "service"));
        var set = BuildingSet.For(world, 7u);
        Require(set.Plans.Count == 4, "four plans");
        var module = new BuildingsModule();
        for (var i = 0; i < set.Plans.Count; i++)
            foreach (var lod in Levels)
            {
                var bundles = module.BuildOne(set.Plans[i], set.Blocks[i], lod, Campus.Contracts.Tests.PaletteFixture.Catalog);
                Require(bundles.Count > 0, $"{set.Plans[i].Key} {lod} has geometry");
                foreach (var b in bundles)
                {
                    b.ValidateVertexData();
                    Require(b.Position.Length / 3 <= 65536, "surface vertex limit");
                    Require(b.Ao is not null && b.Flags is not null && b.Reserved is not null, "COLOR bytes always supplied");
                    for (var v = 0; v < b.Normal.Length / 3; v++)
                    {
                        var n = new Vector3(b.Normal[3 * v], b.Normal[3 * v + 1], b.Normal[3 * v + 2]);
                        Require(MathF.Abs(n.Length() - 1f) < 0.01f, "unit normals");
                    }
                    for (var t = 0; t < b.Index.Length; t += 3)
                    {
                        var a = P(b, b.Index[t]);
                        var g = Vector3.Cross(P(b, b.Index[t + 1]) - a, P(b, b.Index[t + 2]) - a);
                        var n = N(b, b.Index[t]) + N(b, b.Index[t + 1]) + N(b, b.Index[t + 2]);
                        // Godot front faces are clockwise: the geometric normal opposes the shading normal.
                        Require(Vector3.Dot(g, n) <= 1e-6f, "front-face winding");
                    }
                }
            }
    }

    [TestCase]
    public void GenerationIsDeterministic()
    {
        var world1 = World(Feature("t:a", LShape(0, 0), 6, 21f, "residence"));
        var world2 = World(Feature("t:a", LShape(0, 0), 6, 21f, "residence"));
        var s1 = BuildingSet.For(world1, 3u);
        var s2 = BuildingSet.For(world2, 3u);
        var b1 = new BuildingsModule().BuildOne(s1.Plans[0], s1.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var b2 = new BuildingsModule().BuildOne(s2.Plans[0], s2.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        Require(b1.Count == b2.Count, "same bundle count");
        for (var i = 0; i < b1.Count; i++)
        {
            Require(b1[i].Position.SequenceEqual(b2[i].Position), "same positions");
            Require(b1[i].Normal.SequenceEqual(b2[i].Normal), "same normals");
            Require(b1[i].Index.SequenceEqual(b2[i].Index), "same indices");
            Require(b1[i].Material == b2[i].Material, "same materials");
        }
    }

    [TestCase]
    public void PartGlassKeepsTheWindowNumbersAndCards()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"),
            Feature("t:hall", Rect(100, 0, 40, 30), 1, 9f, "sport"),
            Feature("t:plant", Rect(200, 0, 20, 12), 2, 8f, "utility"));
        var set = BuildingSet.For(world, 5u);
        var module = new BuildingsModule();
        for (var b = 0; b < set.Plans.Count; b++)
        {
            var plan = set.Plans[b];
            var block = set.Blocks[b];
            // Expected cells straight from the plan, with the encoding the mainline glass used.
            var expected = new Dictionary<int, byte>();
            for (var fi = 0; fi < plan.Facades.Count; fi++)
                foreach (var o in plan.Openings[fi])
                {
                    var id = block.Base + o.Row * plan.TotalColumns + plan.Facades[fi].ColumnStart + o.Bay;
                    var hash = StableHash.Hash32(plan.Seed, "card", id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    expected.Add(id, (byte)plan.CardOf(o.Kind, hash));
                }
            foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
            {
                var seen = new HashSet<int>();
                foreach (var g in module.BuildOne(plan, block, lod, Campus.Contracts.Tests.PaletteFixture.Catalog).Where(x => x.Material.Spec.Kind == ToonKind.Glass))
                    for (var i = 0; i < g.Position.Length / 3; i++)
                    {
                        Require(g.Uv2![2 * i] == block.Base && (int)MathF.Round(g.Uv2[2 * i + 1]) == plan.Profile.Channel + 32 * plan.TotalColumns,
                            $"{plan.Key} {lod}: UV2 is (BaseId, slot + 32 × columns)");
                        var id = block.Base + (int)MathF.Floor(g.Uv![2 * i + 1]) * plan.TotalColumns + (int)MathF.Floor(g.Uv[2 * i]);
                        Require(expected.TryGetValue(id, out var card) && (g.Reserved![i] & WindowCardEncoding.TypeMask) == card, $"{plan.Key} {lod}: window {id} keeps its cell and card");
                        seen.Add(id);
                    }
                Require(seen.SetEquals(expected.Keys), $"{plan.Key} {lod}: every opening has glass");
            }
        }
    }
}

using Campus.Gen.Geometry;
using Campus.Gen.Buildings.Mesh;
using Campus.Tests;
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

using static GdUnit4.Assertions;

[TestSuite]
public sealed class BuildingMeshTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void DormitoryHasStoreysBaysAndNumberedWindows()
    {
        var world = World(WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6));
        var set = BuildingSet.For(Ctx(world, 1u));
        AssertThat(set.Plans.Count == 1).OverrideFailureMessage("Expected: " + ("one plan")).IsTrue();
        var plan = set.Plans[0];
        AssertThat(plan.Storeys == 6).OverrideFailureMessage("Expected: " + ("storeys from data")).IsTrue();
        AssertThat(plan.TotalColumns > 0).OverrideFailureMessage("Expected: " + ("bays on all four facades")).IsTrue();
        AssertThat(plan.Entrances.Count == 1 && plan.Entrances[0].Anchor is null).OverrideFailureMessage("Expected: " + ("the generic entrance on the longest facade")).IsTrue();
        AssertThat(set.WindowCount == plan.TotalColumns * plan.Storeys).OverrideFailureMessage("Expected: " + ("window block covers the building grid")).IsTrue();
        var bundles = new BuildingsModule().BuildOne(plan, set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var glass = bundles.Where(b => b.Material.Spec.Kind == ToonKind.Glass).ToArray();
        AssertThat(glass.Length > 0).OverrideFailureMessage("Expected: " + ("glass bundle")).IsTrue();
        var block = set.Blocks[0];
        foreach (var g in glass)
            for (var i = 0; i < g.Position.Length / 3; i++)
            {
                AssertThat(g.Uv2![2 * i] == block.Base).OverrideFailureMessage("Expected: " + ("UV2.x is the building window base")).IsTrue();
                var packed = (int)MathF.Round(g.Uv2[2 * i + 1]);
                AssertThat(packed / 32 == plan.TotalColumns).OverrideFailureMessage("Expected: " + ("UV2.y carries the grid column count")).IsTrue();
                var col = (int)MathF.Floor(g.Uv![2 * i]);
                var row = (int)MathF.Floor(g.Uv[2 * i + 1]);
                var id = block.Base + row * plan.TotalColumns + col;
                AssertThat(id >= block.Base && id < block.Base + block.Count).OverrideFailureMessage("Expected: " + ("window number inside the block")).IsTrue();
                AssertThat((g.Reserved![i] & WindowCardEncoding.TypeMask) is >= 1 and <= 15).OverrideFailureMessage("Expected: " + ("window card type in the spare byte")).IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EveryLevelIsValidWoundAndFinite()
    {
        var world = World(
            WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 48, 16))), props: "{\"category\":\"teaching\"}", height: 19.5f, storeys: 5),
            WorldFeatureFixture.Create("t:b", "building", new PolygonGeometry(new PolygonRings(LShape(100, 0))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4),
            WorldFeatureFixture.Create("t:c", "building", new PolygonGeometry(Courtyard(0, 100)), props: "{\"category\":\"residence\"}", height: 12f, storeys: 3),
            WorldFeatureFixture.Create("t:d", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 5, 6))), props: "{\"category\":\"service\"}", height: 3f, storeys: 1));
        var set = BuildingSet.For(Ctx(world, 7u));
        AssertThat(set.Plans.Count == 4).OverrideFailureMessage("Expected: " + ("four plans")).IsTrue();
        var module = new BuildingsModule();
        for (var i = 0; i < set.Plans.Count; i++)
            foreach (var lod in Levels)
            {
                var bundles = module.BuildOne(set.Plans[i], set.Blocks[i], lod, Campus.Contracts.Tests.PaletteFixture.Catalog);
                AssertThat(bundles.Count > 0).OverrideFailureMessage("Expected: " + ($"{set.Plans[i].Key} {lod} has geometry")).IsTrue();
                foreach (var b in bundles)
                {
                    b.ValidateVertexData();
                    AssertThat(b.Position.Length / 3 <= GeometryVertexLimits.Batch).OverrideFailureMessage("Expected: " + ("surface vertex limit")).IsTrue();
                    for (var v = 0; v < b.Normal.Length / 3; v++)
                    {
                        var n = new Vector3(b.Normal[3 * v], b.Normal[3 * v + 1], b.Normal[3 * v + 2]);
                        AssertThat(MathF.Abs(n.Length() - 1f) < TestTolerance.Position).OverrideFailureMessage("Expected: " + ("unit normals")).IsTrue();
                    }
                    for (var t = 0; t < b.Index.Length; t += 3)
                    {
                        var a = P(b, b.Index[t]);
                        var g = Vector3.Cross(P(b, b.Index[t + 1]) - a, P(b, b.Index[t + 2]) - a);
                        var n = N(b, b.Index[t]) + N(b, b.Index[t + 1]) + N(b, b.Index[t + 2]);
                        // Godot front faces are clockwise: the geometric normal opposes the shading normal.
                        AssertThat(Vector3.Dot(g, n) <= 1e-6f).OverrideFailureMessage("Expected: " + ("front-face winding")).IsTrue();
                    }
                }
            }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void GenerationIsDeterministic()
    {
        var world1 = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(LShape(0, 0))), props: "{\"category\":\"residence\"}", height: 21f, storeys: 6));
        var world2 = World(WorldFeatureFixture.Create("t:a", "building", new PolygonGeometry(new PolygonRings(LShape(0, 0))), props: "{\"category\":\"residence\"}", height: 21f, storeys: 6));
        var s1 = BuildingSet.For(Ctx(world1, 3u));
        var s2 = BuildingSet.For(Ctx(world2, 3u));
        var b1 = new BuildingsModule().BuildOne(s1.Plans[0], s1.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var b2 = new BuildingsModule().BuildOne(s2.Plans[0], s2.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        AssertThat(b1.Count == b2.Count).OverrideFailureMessage("Expected: " + ("same bundle count")).IsTrue();
        for (var i = 0; i < b1.Count; i++)
        {
            AssertThat(b1[i].Position.SequenceEqual(b2[i].Position)).OverrideFailureMessage("Expected: " + ("same positions")).IsTrue();
            AssertThat(b1[i].Normal.SequenceEqual(b2[i].Normal)).OverrideFailureMessage("Expected: " + ("same normals")).IsTrue();
            AssertThat(b1[i].Index.SequenceEqual(b2[i].Index)).OverrideFailureMessage("Expected: " + ("same indices")).IsTrue();
            AssertThat(b1[i].Material == b2[i].Material).OverrideFailureMessage("Expected: " + ("same materials")).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PartGlassKeepsTheWindowNumbersAndCards()
    {
        var world = World(
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6),
            WorldFeatureFixture.Create("t:hall", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 40, 30))), props: "{\"category\":\"sport\"}", height: 9f, storeys: 1),
            WorldFeatureFixture.Create("t:plant", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 20, 12))), props: "{\"category\":\"utility\"}", height: 8f, storeys: 2));
        var set = BuildingSet.For(Ctx(world, 5u));
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
                        AssertThat(g.Uv2![2 * i] == block.Base && (int)MathF.Round(g.Uv2[2 * i + 1]) == plan.Profile.Channel + 32 * plan.TotalColumns).OverrideFailureMessage("Expected: " + ($"{plan.Key} {lod}: UV2 is (BaseId, slot + 32 × columns)")).IsTrue();
                        var id = block.Base + (int)MathF.Floor(g.Uv![2 * i + 1]) * plan.TotalColumns + (int)MathF.Floor(g.Uv[2 * i]);
                        AssertThat(expected.TryGetValue(id, out var card) && (g.Reserved![i] & WindowCardEncoding.TypeMask) == card).OverrideFailureMessage("Expected: " + ($"{plan.Key} {lod}: window {id} keeps its cell and card")).IsTrue();
                        seen.Add(id);
                    }
                AssertThat(seen.SetEquals(expected.Keys)).OverrideFailureMessage("Expected: " + ($"{plan.Key} {lod}: every opening has glass")).IsTrue();
            }
        }
    }
}

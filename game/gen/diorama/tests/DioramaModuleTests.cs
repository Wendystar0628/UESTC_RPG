using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Diorama.Tests;

using static GdUnit4.Assertions;

/// <summary>Builds the backdrop from every map's real backdrop file and checks budget, layout, encodings and determinism.</summary>
[TestSuite]
public sealed class DioramaModuleTests
{
    private static MapDescriptor Map(string backdrop) => new(
        new MapId("test"), new LocalizedName("", ""), MapKind.Outdoor, 1, 1,
        new GeoFrame(0, 0, 0, Vector2.Zero, Vector2.Zero), Vector2.Zero, new Box3(),
        new MapRect(new(-1280, -1280), new(1280, 1280)), new RoamLimits(0.9f, 30, 3000, -28, 2000, 30, 40, 150),
        new TileKey(-20, -20), new TileKey(20, 20), "P00", [],
        new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("", ""), false, "test"),
        new MapEnvironment("sky.json", backdrop, []), []);

    private static BuildContext Context(byte[]? backdrop, uint seed = 1) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = new FakeWorld(),
        Map = Map("backdrop.json"),
        WorldSeed = seed,
        SourceInputs = backdrop is null
            ? BuildInputSnapshot.Empty.Entries
            : BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["backdrop.json"] = backdrop }, "fixture").Entries,
    };

    private static IEnumerable<string> BackdropFiles()
    {
        var maps = RepositoryFixture.FilePath("game", "data", "maps");
        return Directory.EnumerateFiles(maps, "backdrop.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    public void EveryMapBackdropStaysInsideTheDioramaBudget()
    {
        var count = 0;
        foreach (var file in BackdropFiles())
        {
            var ctx = Context(File.ReadAllBytes(file));
            var module = new DioramaModule();
            var tiles = module.Tiles(ctx).ToArray();
            AssertThat(tiles.Length != 1).OverrideFailureMessage($"{file}: the backdrop is one resident product, got {tiles.Length} cells").IsFalse();
            var output = module.Build(ctx, new TileKey(0, 0), LodLevel.L4);
            var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
            Console.WriteLine($"{file}: {output.Meshes.Count} surfaces, {output.Meshes.Sum(m => m.Position.Length / 3)} vertices, {triangles} triangles");
            AssertThat(triangles > TestTolerance.BudgetFactor * module.Budget.TrianglesL0).OverrideFailureMessage($"backdrop uses {triangles} triangles, an order of magnitude over its budget {module.Budget.TrianglesL0}").IsFalse();
            AssertThat(output.Meshes.Count > TestTolerance.BudgetFactor * module.Budget.DrawCalls).OverrideFailureMessage("draw calls an order of magnitude over budget").IsFalse();
            foreach (var mesh in output.Meshes)
            {
                AssertThat(mesh.Shader != ShaderId.DioramaFar).OverrideFailureMessage("every backdrop surface uses diorama_far").IsFalse();
                AssertThat(mesh.Flags is not { } flags || flags.Any(f => (f & 8) == 0)).OverrideFailureMessage("the backdrop never casts shadows").IsFalse();
                AssertThat(mesh.Position.Length / 3 > 65536).OverrideFailureMessage("surface over 65 536 vertices").IsFalse();
                AssertThat(mesh.Position.Any(v => !float.IsFinite(v)) || mesh.Normal.Any(v => !float.IsFinite(v))).OverrideFailureMessage("non-finite vertex data").IsFalse();
            }
            AssertThat(module.Build(ctx, new TileKey(0, 0), LodLevel.L0).Meshes.Count != 0 || module.Build(ctx, new TileKey(1, 0), LodLevel.L4).Meshes.Count != 0).OverrideFailureMessage("only L4 tile (0, 0) carries the backdrop").IsFalse();
            count++;
        }
        AssertThat(count == 0).OverrideFailureMessage("no backdrop.json found").IsFalse();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void LayersSitWhereTheFileSaysAndSinkBelowTheFogSea()
    {
        foreach (var file in BackdropFiles())
        {
            var data = BackdropData.Parse(File.ReadAllBytes(file));
            var output = new DioramaModule().Build(Context(File.ReadAllBytes(file)), new TileKey(0, 0), LodLevel.L4);
            var byKind = output.Meshes.GroupBy(m => m.Reserved![0]).ToDictionary(g => g.Key, g => g.ToArray());
            AssertThat(byKind[DioramaVertex.KindFogSea].Length != 1 || byKind[DioramaVertex.KindFar].Length != 1 ||
                byKind[DioramaVertex.KindCity].Length != data.CityRings.Count).OverrideFailureMessage("one fog sea, one far ring and one surface per city ring").IsFalse();
            var fog = byKind[DioramaVertex.KindFogSea][0];
            var outer = Radii(fog).Max();
            AssertThat(MathF.Abs(outer - data.FogSea.OuterRadius) > 20 * TestTolerance.Layout).OverrideFailureMessage($"fog sea reaches {outer}").IsFalse();
            AssertThat(Heights(fog).Min() < data.FogSea.TopY - TestTolerance.Position).OverrideFailureMessage("fog sea dips below its top").IsFalse();
            AssertThat(data.FogSea.EdgeBank.Height > 0f && Heights(fog).Max() <= 0f).OverrideFailureMessage("the edge bank must rise above the ground").IsFalse();
            for (var i = 0; i < data.CityRings.Count; i++)
            {
                var ring = data.CityRings[i];
                var mesh = byKind[DioramaVertex.KindCity][i];
                var radii = Radii(mesh).ToArray();
                AssertThat(radii.Min() < ring.Radius - ring.Depth || radii.Max() > ring.Radius + ring.Depth).OverrideFailureMessage($"city ring {ring.Id} spans {radii.Min()}–{radii.Max()}").IsFalse();
                AssertThat(Heights(mesh).Min() > data.FogSea.TopY).OverrideFailureMessage($"city ring {ring.Id} must sink below the fog sea").IsFalse();
                AssertThat(mesh.Uv2!.Where((_, k) => k % 2 == 0).Any(k => MathF.Abs(k - ring.Parallax) > TestTolerance.Position)).OverrideFailureMessage("parallax in UV2.x").IsFalse();
            }
            var far = byKind[DioramaVertex.KindFar][0];
            AssertThat(Heights(far).Min() > data.FarRing.SkirtY + TestTolerance.Position).OverrideFailureMessage("far ring skirt").IsFalse();
            // Every bearing has a silhouette above the horizon (WLD-DIORAMA: no empty direction).
            var crest = new float[360];
            for (var v = 0; v < far.Position.Length / 3; v++)
            {
                if (far.Uv![2 * v + 1] < 0.5f) continue;
                var p = new Vector2(far.Position[3 * v], far.Position[3 * v + 2]);
                var bearing = (int)MathF.Floor(Angles.Bearing(p)) % 360;
                crest[bearing] = MathF.Max(crest[bearing], far.Position[3 * v + 1]);
            }
            AssertThat(crest.Any(h => h <= 0f)).OverrideFailureMessage("a bearing of the far ring has no silhouette").IsFalse();
            // The snow mountains stand clearly above the hills at their bearings, near their scaled elevation.
            foreach (var m in data.FarRing.Mountains)
            {
                var expected = FarRing.MountainHeight(data.FarRing, m);
                var top = crest[(int)Angles.WrapDegrees(MathF.Round(m.Azimuth)) % 360];
                AssertThat(top < expected * 0.85f || top > expected * 1.15f).OverrideFailureMessage($"{m.Name}: crest {top} m, expected about {expected} m").IsFalse();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SeasonWordsRoundTrip()
    {
        float[] values = [0.75f, 0.95f, 0.8f, 0.5f];
        var word = DioramaVertex.PackSeasons(values);
        AssertThat(word >= 1 << 24 || word != MathF.Floor(word)).OverrideFailureMessage("word must be an exact fp32 integer").IsFalse();
        for (var s = 0; s < 4; s++)
            AssertThat(MathF.Abs(DioramaVertex.UnpackSeason(word, s) - values[s]) > 0.6f / DioramaVertex.SeasonQuantizationMax).OverrideFailureMessage($"season {s}").IsFalse();
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void NoBackdropFileMeansNoProductAndTheBuildIsDeterministic()
    {
        var empty = new DioramaModule();
        AssertThat(empty.Tiles(Context(null)).Any() || empty.Build(Context(null), new TileKey(0, 0), LodLevel.L4).Meshes.Count != 0).OverrideFailureMessage("a map without backdrop file has no backdrop").IsFalse();
        var file = BackdropFiles().First();
        var a = new DioramaModule().Build(Context(File.ReadAllBytes(file)), new TileKey(0, 0), LodLevel.L4);
        var b = new DioramaModule().Build(Context(File.ReadAllBytes(file)), new TileKey(0, 0), LodLevel.L4);
        for (var i = 0; i < a.Meshes.Count; i++)
            AssertThat(!a.Meshes[i].Position.SequenceEqual(b.Meshes[i].Position) || !a.Meshes[i].Index.SequenceEqual(b.Meshes[i].Index) ||
                !a.Meshes[i].Uv!.SequenceEqual(b.Meshes[i].Uv!)).OverrideFailureMessage("the backdrop must be byte-identical across builds").IsFalse();
        var other = new DioramaModule().Build(Context(File.ReadAllBytes(file), 7), new TileKey(0, 0), LodLevel.L4);
        AssertThat(other.Meshes[1].Position.SequenceEqual(a.Meshes[1].Position)).OverrideFailureMessage("the world seed varies the city").IsFalse();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FogSeaInnerEdgeFollowsTheCutLineWhenGiven()
    {
        var rect = new MapRectLike(new(-1280, -1280), new(1280, 1280));
        var d = FogSea.EdgeDistance(Vector2.Zero, new(1, 0), null, rect);
        AssertThat(MathF.Abs(d - 1280f) > TestTolerance.Position).OverrideFailureMessage($"rectangle edge {d}").IsFalse();
        var circle = Enumerable.Range(0, 720).Select(i => new Vector2(1500f * MathF.Cos(i * MathF.PI / 360f), 1500f * MathF.Sin(i * MathF.PI / 360f))).ToArray();
        var c = FogSea.EdgeDistance(Vector2.Zero, Vector2.Normalize(new(1, 1)), circle, rect);
        AssertThat(MathF.Abs(c - 1500f) > 20 * TestTolerance.Layout).OverrideFailureMessage($"cut line edge {c}").IsFalse();
    }

    private static IEnumerable<float> Radii(GeometryBundle m)
    {
        for (var v = 0; v < m.Position.Length / 3; v++) yield return MathF.Sqrt(m.Position[3 * v] * m.Position[3 * v] + m.Position[3 * v + 2] * m.Position[3 * v + 2]);
    }

    private static IEnumerable<float> Heights(GeometryBundle m)
    {
        for (var v = 0; v < m.Position.Length / 3; v++) yield return m.Position[3 * v + 1];
    }
}

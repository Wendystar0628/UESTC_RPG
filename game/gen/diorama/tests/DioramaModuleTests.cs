using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Gen.Diorama.Tests;

/// <summary>Builds the backdrop from every map's real backdrop file and checks budget, layout, encodings and determinism.</summary>
[TestSuite]
public sealed class DioramaModuleTests
{
    private sealed class World : IWorldModel, ISpatialIndex
    {
        public PolygonRings Boundary { get; } = new([new(-1000, -1000), new(1000, -1000), new(1000, 1000), new(-1000, 1000)]);
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = new Dictionary<FeatureId, WorldFeature>();
        public WorldNetwork Network => WorldNetwork.Empty;
        public ISpatialIndex Spatial => this;
        public IInteriorIndex Interiors => null!;
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity => null;
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => 0f;
        public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
        public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
    }

    private static MapDescriptor Map(string backdrop) => new(
        new MapId("test"), new LocalizedName("", ""), MapKind.Outdoor, 1, 1,
        new GeoFrame(0, 0, 0, Vector2.Zero, Vector2.Zero), Vector2.Zero, new Box3(),
        new MapRect(new(-1280, -1280), new(1280, 1280)), new RoamLimits(0.9f, 30, 3000, -28, 2000, 30, 40, 150),
        new TileKey(-20, -20), new TileKey(20, 20), "P00", [],
        new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("", ""), false, "test"),
        new MapEnvironment("sky.json", backdrop, []), new Dictionary<string, string>(), []);

    private static BuildContext Context(byte[]? backdrop, uint seed = 1) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = new World(),
        Map = Map("backdrop.json"),
        WorldSeed = seed,
        SourceInputs = backdrop is null
            ? BuildInputSnapshot.Empty.Entries
            : BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["backdrop.json"] = backdrop }).Entries,
    };

    private static IEnumerable<string> BackdropFiles()
    {
        var maps = Path.Combine(FindRepository(), "game", "data", "maps");
        return Directory.EnumerateFiles(maps, "backdrop.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
    }

    [TestCase]
    public void EveryMapBackdropStaysInsideTheDioramaBudget()
    {
        var count = 0;
        foreach (var file in BackdropFiles())
        {
            var ctx = Context(File.ReadAllBytes(file));
            var module = new DioramaModule();
            var tiles = module.Tiles(ctx).ToArray();
            if (tiles.Length != 1) throw new InvalidOperationException($"{file}: the backdrop is one resident product, got {tiles.Length} cells");
            var output = module.Build(ctx, new TileKey(0, 0), LodLevel.L4);
            var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
            Console.WriteLine($"{file}: {output.Meshes.Count} surfaces, {output.Meshes.Sum(m => m.Position.Length / 3)} vertices, {triangles} triangles");
            if (triangles > module.Budget.TrianglesL0) throw new InvalidOperationException($"backdrop uses {triangles} triangles (budget {module.Budget.TrianglesL0})");
            if (output.Meshes.Count > module.Budget.DrawCalls) throw new InvalidOperationException("too many draw calls");
            foreach (var mesh in output.Meshes)
            {
                if (mesh.Shader != ShaderId.DioramaFar) throw new InvalidOperationException("every backdrop surface uses diorama_far");
                if (mesh.Flags is not { } flags || flags.Any(f => (f & 8) == 0)) throw new InvalidOperationException("the backdrop never casts shadows");
                if (mesh.Position.Length / 3 > 65536) throw new InvalidOperationException("surface over 65 536 vertices");
                if (mesh.Position.Any(v => !float.IsFinite(v)) || mesh.Normal.Any(v => !float.IsFinite(v))) throw new InvalidOperationException("non-finite vertex data");
            }
            if (module.Build(ctx, new TileKey(0, 0), LodLevel.L0).Meshes.Count != 0 || module.Build(ctx, new TileKey(1, 0), LodLevel.L4).Meshes.Count != 0)
                throw new InvalidOperationException("only L4 tile (0, 0) carries the backdrop");
            count++;
        }
        if (count == 0) throw new InvalidOperationException("no backdrop.json found");
    }

    [TestCase]
    public void LayersSitWhereTheFileSaysAndSinkBelowTheFogSea()
    {
        foreach (var file in BackdropFiles())
        {
            var data = BackdropData.Parse(File.ReadAllBytes(file));
            var output = new DioramaModule().Build(Context(File.ReadAllBytes(file)), new TileKey(0, 0), LodLevel.L4);
            var byKind = output.Meshes.GroupBy(m => m.Reserved![0]).ToDictionary(g => g.Key, g => g.ToArray());
            if (byKind[DioramaVertex.KindFogSea].Length != 1 || byKind[DioramaVertex.KindFar].Length != 1 ||
                byKind[DioramaVertex.KindCity].Length != data.CityRings.Count)
                throw new InvalidOperationException("one fog sea, one far ring and one surface per city ring");
            var fog = byKind[DioramaVertex.KindFogSea][0];
            var outer = Radii(fog).Max();
            if (MathF.Abs(outer - data.FogSea.OuterRadius) > 1f) throw new InvalidOperationException($"fog sea reaches {outer}");
            if (Heights(fog).Min() < data.FogSea.TopY - 0.01f) throw new InvalidOperationException("fog sea dips below its top");
            if (data.FogSea.EdgeBank.Height > 0f && Heights(fog).Max() <= 0f) throw new InvalidOperationException("the edge bank must rise above the ground");
            for (var i = 0; i < data.CityRings.Count; i++)
            {
                var ring = data.CityRings[i];
                var mesh = byKind[DioramaVertex.KindCity][i];
                var radii = Radii(mesh).ToArray();
                if (radii.Min() < ring.Radius - ring.Depth || radii.Max() > ring.Radius + ring.Depth)
                    throw new InvalidOperationException($"city ring {ring.Id} spans {radii.Min()}–{radii.Max()}");
                if (Heights(mesh).Min() > data.FogSea.TopY) throw new InvalidOperationException($"city ring {ring.Id} must sink below the fog sea");
                if (mesh.Uv2!.Where((_, k) => k % 2 == 0).Any(k => MathF.Abs(k - ring.Parallax) > 1e-6f)) throw new InvalidOperationException("parallax in UV2.x");
            }
            var far = byKind[DioramaVertex.KindFar][0];
            if (Heights(far).Min() > data.FarRing.SkirtY + 0.01f) throw new InvalidOperationException("far ring skirt");
            // Every bearing has a silhouette above the horizon (WLD-DIORAMA: no empty direction).
            var crest = new float[360];
            for (var v = 0; v < far.Position.Length / 3; v++)
            {
                if (far.Uv![2 * v + 1] < 0.5f) continue;
                var p = new Vector2(far.Position[3 * v], far.Position[3 * v + 2]);
                var bearing = (int)MathF.Floor(CityRing.Wrap(MathF.Atan2(p.X, -p.Y) * 180f / MathF.PI)) % 360;
                crest[bearing] = MathF.Max(crest[bearing], far.Position[3 * v + 1]);
            }
            if (crest.Any(h => h <= 0f)) throw new InvalidOperationException("a bearing of the far ring has no silhouette");
            // The snow mountains stand clearly above the hills at their bearings, near their scaled elevation.
            foreach (var m in data.FarRing.Mountains)
            {
                var expected = FarRing.MountainHeight(data.FarRing, m);
                var top = crest[(int)CityRing.Wrap(MathF.Round(m.Azimuth)) % 360];
                if (top < expected * 0.85f || top > expected * 1.15f) throw new InvalidOperationException($"{m.Name}: crest {top} m, expected about {expected} m");
            }
        }
    }

    [TestCase]
    public void SeasonWordsRoundTrip()
    {
        float[] values = [0.75f, 0.95f, 0.8f, 0.5f];
        var word = DioramaVertex.PackSeasons(values);
        if (word >= 1 << 24 || word != MathF.Floor(word)) throw new InvalidOperationException("word must be an exact fp32 integer");
        for (var s = 0; s < 4; s++)
            if (MathF.Abs(DioramaVertex.UnpackSeason(word, s) - values[s]) > 0.6f / 63f) throw new InvalidOperationException($"season {s}");
    }

    [TestCase]
    public void NoBackdropFileMeansNoProductAndTheBuildIsDeterministic()
    {
        var empty = new DioramaModule();
        if (empty.Tiles(Context(null)).Any() || empty.Build(Context(null), new TileKey(0, 0), LodLevel.L4).Meshes.Count != 0)
            throw new InvalidOperationException("a map without backdrop file has no backdrop");
        var file = BackdropFiles().First();
        var a = new DioramaModule().Build(Context(File.ReadAllBytes(file)), new TileKey(0, 0), LodLevel.L4);
        var b = new DioramaModule().Build(Context(File.ReadAllBytes(file)), new TileKey(0, 0), LodLevel.L4);
        for (var i = 0; i < a.Meshes.Count; i++)
            if (!a.Meshes[i].Position.SequenceEqual(b.Meshes[i].Position) || !a.Meshes[i].Index.SequenceEqual(b.Meshes[i].Index) ||
                !a.Meshes[i].Uv!.SequenceEqual(b.Meshes[i].Uv!))
                throw new InvalidOperationException("the backdrop must be byte-identical across builds");
        var other = new DioramaModule().Build(Context(File.ReadAllBytes(file), 7), new TileKey(0, 0), LodLevel.L4);
        if (other.Meshes[1].Position.SequenceEqual(a.Meshes[1].Position)) throw new InvalidOperationException("the world seed varies the city");
    }

    [TestCase]
    public void FogSeaInnerEdgeFollowsTheCutLineWhenGiven()
    {
        var rect = new MapRectLike(new(-1280, -1280), new(1280, 1280));
        var d = FogSea.EdgeDistance(Vector2.Zero, new(1, 0), null, rect);
        if (MathF.Abs(d - 1280f) > 0.01f) throw new InvalidOperationException($"rectangle edge {d}");
        var circle = Enumerable.Range(0, 720).Select(i => new Vector2(1500f * MathF.Cos(i * MathF.PI / 360f), 1500f * MathF.Sin(i * MathF.PI / 360f))).ToArray();
        var c = FogSea.EdgeDistance(Vector2.Zero, Vector2.Normalize(new(1, 1)), circle, rect);
        if (MathF.Abs(c - 1500f) > 1f) throw new InvalidOperationException($"cut line edge {c}");
    }

    private static IEnumerable<float> Radii(GeometryBundle m)
    {
        for (var v = 0; v < m.Position.Length / 3; v++) yield return MathF.Sqrt(m.Position[3 * v] * m.Position[3 * v] + m.Position[3 * v + 2] * m.Position[3 * v + 2]);
    }

    private static IEnumerable<float> Heights(GeometryBundle m)
    {
        for (var v = 0; v < m.Position.Length / 3; v++) yield return m.Position[3 * v + 1];
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root (ownership.json) not found.");
    }
}

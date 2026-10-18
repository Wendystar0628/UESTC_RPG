using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Fauna;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Fauna.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class FaunaTests
{
    private static readonly Lazy<IWorldModel> World = new(Load);
    private static readonly Lazy<IReadOnlyList<FeatureId>> Lakes = new(() =>
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe", "map.json")));
        return doc.RootElement.GetProperty("env").GetProperty("reflectLakes").EnumerateArray().Select(e => new FeatureId(e.GetString()!)).ToArray();
    });

    private static IWorldModel Load()
    {
        var data = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var overlay in Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Order(StringComparer.Ordinal))
            files[Path.GetRelativePath(data, overlay).Replace('\\', '/')] = File.ReadAllText(overlay);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static string FindRepository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "game", "project.godot"))) return dir.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    [TestCase]
    public void LayoutIsDeterministicAndKeepsTheMainlineCounts()
    {
        var a = FaunaPlanner.Build(World.Value, 1, Lakes.Value);
        var b = FaunaPlanner.Build(World.Value, 1, Lakes.Value);
        foreach (var line in a.Log) Console.WriteLine("[fauna] " + line);
        AssertThat(a.Sites.Select(s => s.Key).SequenceEqual(b.Sites.Select(s => s.Key))).OverrideFailureMessage("Same world, same sites.").IsTrue();
        AssertThat(a.Sites.SelectMany(s => s.Day).Select(x => x.Position).SequenceEqual(b.Sites.SelectMany(s => s.Day).Select(x => x.Position))).OverrideFailureMessage("Same positions.").IsTrue();
        var cats = a.Sites.Where(s => s.Kind == FaunaKind.Cat).ToArray();
        AssertThat(cats.Length is >= 2 and <= 3).OverrideFailureMessage($"Two or three cat sites, got {cats.Length}: {string.Join("; ", a.Log)}").IsTrue();
        var dayCats = cats.Sum(s => s.Day.Count);
        var nightCats = cats.Sum(s => s.Night.Count);
        AssertThat(dayCats <= 4 && nightCats < dayCats).OverrideFailureMessage($"At most four cats by day and fewer at night ({dayCats}/{nightCats}).").IsTrue();
        AssertThat(cats.All(s => s.Rain.Count == s.Day.Count)).OverrideFailureMessage("Every cat has an under-the-eaves place in rain.").IsTrue();
        var pigeons = a.Sites.Single(s => s.Kind == FaunaKind.Pigeon);
        AssertThat(pigeons.Day.Count is >= 6 and <= 12).OverrideFailureMessage($"6-12 pigeons, got {pigeons.Day.Count}.").IsTrue();
        AssertThat(pigeons.Night.Count == 0 && pigeons.Rain.Count == 0).OverrideFailureMessage("No pigeons on the ground at night or in rain.").IsTrue();
        AssertThat(pigeons.Day.Count(p => p.Model.White) == 1).OverrideFailureMessage("One white pigeon among the grey.").IsTrue();
        var sparrowSites = a.Sites.Count(s => s.Kind == FaunaKind.Sparrow);
        AssertThat(sparrowSites is >= 2 and <= 6).OverrideFailureMessage($"3-6 sparrow places (fewer rather than more), got {sparrowSites}.").IsTrue();
        foreach (var site in a.Sites)
            foreach (var x in site.Day.Concat(site.Night).Concat(site.Rain))
                AssertThat(float.IsFinite(x.Position.X) && float.IsFinite(x.Position.Y) && float.IsFinite(x.Yaw)).OverrideFailureMessage($"{site.Key}: finite pose.").IsTrue();
        var swans = a.Sites.Where(s => s.Kind == FaunaKind.Swan).ToArray();
        AssertThat(swans.Length == 2 && swans[0].Day.Count == 4 && swans[1].Day.Count == 2 && swans.All(s => s.Night.Count == s.Day.Count)).OverrideFailureMessage($"Four black swans on the first lake, two on the second: {string.Join("; ", a.Log)}").IsTrue();
        var crows = a.Sites.Where(s => s.Kind == FaunaKind.Crow).ToArray();
        AssertThat(crows.Length is >= 2 and <= 3 && crows.All(c => c.Day.Count is >= 2 and <= 3)).OverrideFailureMessage($"2-3 crow groups of 2-3: {string.Join("; ", a.Log)}").IsTrue();
        for (var i = 0; i < crows.Length; i++)
            for (var j = i + 1; j < crows.Length; j++)
                AssertThat(Vector3.Distance(crows[i].Day[0].Position, crows[j].Day[0].Position) >= 140f).OverrideFailureMessage("Crow groups at least 150 m apart.").IsTrue();
        var spots = cats.Select(s => s.Day[0].Position).ToArray();
        for (var i = 0; i < spots.Length; i++)
            for (var j = i + 1; j < spots.Length; j++)
                AssertThat(Vector3.Distance(spots[i], spots[j]) >= FaunaStyle.CatSiteSpacing - 1f).OverrideFailureMessage("Cat sites keep apart.").IsTrue();
    }

    private static FakeWorld NoNetworkWorld(IWorldModel inner) => new(inner)
    {
        Features = inner.Features.Where(p => p.Value.Source != FeatureSource.Design).ToDictionary(p => p.Key, p => p.Value),
        Network = WorldNetwork.Empty,
    };

    [TestCase]
    public void AStubWorldWithoutNetworkOrOverlaysDegradesQuietly()
    {
        var layout = FaunaPlanner.Build(NoNetworkWorld(World.Value), 1, Lakes.Value);
        AssertThat(layout.Sites.Any(s => s.Kind == FaunaKind.Cat)).OverrideFailureMessage("Cats still find a wall without roads.").IsTrue();
        AssertThat(!layout.Sites.Any(s => s.Kind == FaunaKind.Pigeon)).OverrideFailureMessage("No stepped plaza, no pigeons, and no exception.").IsTrue();
    }

    [TestCase]
    public void ModelsAreClosedValidAndWithinBudget()
    {
        var scene = new FaunaScene(Campus.Contracts.Tests.PaletteFixture.Catalog);
        var keys = new List<FaunaModelKey>();
        foreach (var coat in Enum.GetValues<CatCoat>())
            foreach (var pose in Enum.GetValues<CatPose>()) keys.Add(FaunaModelKey.CatOf(coat, pose));
        foreach (var pose in Enum.GetValues<BirdPose>()) { keys.Add(FaunaModelKey.PigeonOf(pose, false)); keys.Add(FaunaModelKey.SparrowOf(pose)); }
        keys.Add(FaunaModelKey.SwanOf(SwanPose.Float));
        keys.Add(FaunaModelKey.SwanOf(SwanPose.Sleep));
        keys.Add(FaunaModelKey.CrowOf());
        foreach (var key in keys)
        {
            var mesh = scene.Model(key);
            var box = mesh.Bounds();
            AssertThat(mesh.Triangles is > 100 and < 7000).OverrideFailureMessage($"{key}: {mesh.Triangles} triangles.").IsTrue();
            if (key.Kind == FaunaKind.Swan) AssertThat(box.Min.Y < -0.05f && box.Min.Y > -0.15f).OverrideFailureMessage($"{key}: the hull sits in the water (min y {box.Min.Y:F3}).").IsTrue();
            else AssertThat(box.Min.Y > -0.01f && box.Min.Y < 0.03f).OverrideFailureMessage($"{key}: feet on the ground (min y {box.Min.Y:F3}).").IsTrue();
            AssertThat(mesh.Normals.All(n => MathF.Abs(n.Length() - 1) < 0.01f)).OverrideFailureMessage($"{key}: unit normals.").IsTrue();
            AssertThat(SignedVolume(mesh) < 0).OverrideFailureMessage($"{key}: clockwise front faces seen from outside (Godot), so the closed mesh has negative signed volume.").IsTrue();
            var height = box.Max.Y - MathF.Max(0f, box.Min.Y);
            var (lo, hi) = key.Kind switch
            {
                FaunaKind.Cat => (0.09f, 0.4f),
                FaunaKind.Pigeon => (0.12f, 0.28f),
                FaunaKind.Sparrow => (0.06f, 0.14f),
                FaunaKind.Swan => (0.25f, 0.9f),
                FaunaKind.Crow => (0.25f, 0.45f),
                _ => (0f, 0f),
            };
            AssertThat(height >= lo && height <= hi).OverrideFailureMessage($"{key}: height {height:F3} out of the species' range.").IsTrue();
        }
        var sites = FaunaPlanner.Build(World.Value, 1, Lakes.Value).Sites;
        var day = sites.SelectMany(s => s.Day).ToArray();
        var output = scene.Build(day);
        AssertThat(output.Meshes.Count == 2 && output.Meshes.All(m => m.ColorIndices is { } c && c.Length * 3 == m.Position.Length)).OverrideFailureMessage("One cat and one bird bundle with colour IDs.").IsTrue();
        AssertThat(scene.Triangles(day) < 90000).OverrideFailureMessage("The whole day set stays small.").IsTrue();
    }

    private static double SignedVolume(AnimalMesh m)
    {
        double v = 0;
        for (var i = 0; i < m.Index.Length; i += 3)
            v += Vector3.Dot(m.Positions[m.Index[i]], Vector3.Cross(m.Positions[m.Index[i + 1]], m.Positions[m.Index[i + 2]])) / 6.0;
        return v;
    }

    [TestCase]
    public void ColoursAreRegisteredNames()
    {
        var root = FindRepository();
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var files = new[] { Path.Combine(root, "game", "style", "palette", "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(root, "game", "style", "palette", "ext"), "WP-*.json"));
        foreach (var file in files)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            foreach (var e in doc.RootElement.GetProperty("entries").EnumerateArray()) tokens.Add(e.GetProperty("token").GetString()!);
        }
        foreach (var field in typeof(FaunaStyle).GetFields().Where(f => f.FieldType == typeof(PaletteToken)))
        {
            var token = (PaletteToken)field.GetValue(null)!;
            AssertThat(tokens.Contains(token.Value) && tokens.Contains(FaunaStyle.ShadeOf(token).Value)).OverrideFailureMessage($"{field.Name}: '{token.Value}' and its shade are registered.").IsTrue();
        }
    }
}

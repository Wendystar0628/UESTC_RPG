using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Fauna.Tests;

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

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [TestCase]
    public void LayoutIsDeterministicAndKeepsTheMainlineCounts()
    {
        var a = FaunaPlanner.Build(World.Value, 1, Lakes.Value);
        var b = FaunaPlanner.Build(World.Value, 1, Lakes.Value);
        foreach (var line in a.Log) Console.WriteLine("[fauna] " + line);
        Check(a.Sites.Select(s => s.Key).SequenceEqual(b.Sites.Select(s => s.Key)), "Same world, same sites.");
        Check(a.Sites.SelectMany(s => s.Day).Select(x => x.Position).SequenceEqual(b.Sites.SelectMany(s => s.Day).Select(x => x.Position)), "Same positions.");
        var cats = a.Sites.Where(s => s.Kind == FaunaKind.Cat).ToArray();
        Check(cats.Length is >= 2 and <= 3, $"Two or three cat sites, got {cats.Length}: {string.Join("; ", a.Log)}");
        var dayCats = cats.Sum(s => s.Day.Count);
        var nightCats = cats.Sum(s => s.Night.Count);
        Check(dayCats <= 4 && nightCats < dayCats, $"At most four cats by day and fewer at night ({dayCats}/{nightCats}).");
        Check(cats.All(s => s.Rain.Count == s.Day.Count), "Every cat has an under-the-eaves place in rain.");
        var pigeons = a.Sites.Single(s => s.Kind == FaunaKind.Pigeon);
        Check(pigeons.Day.Count is >= 6 and <= 12, $"6-12 pigeons, got {pigeons.Day.Count}.");
        Check(pigeons.Night.Count == 0 && pigeons.Rain.Count == 0, "No pigeons on the ground at night or in rain.");
        Check(pigeons.Day.Count(p => p.Model.White) == 1, "One white pigeon among the grey.");
        var sparrowSites = a.Sites.Count(s => s.Kind == FaunaKind.Sparrow);
        Check(sparrowSites is >= 2 and <= 6, $"3-6 sparrow places (fewer rather than more), got {sparrowSites}.");
        foreach (var site in a.Sites)
            foreach (var x in site.Day.Concat(site.Night).Concat(site.Rain))
                Check(float.IsFinite(x.Position.X) && float.IsFinite(x.Position.Y) && float.IsFinite(x.Yaw), $"{site.Key}: finite pose.");
        var swans = a.Sites.Where(s => s.Kind == FaunaKind.Swan).ToArray();
        Check(swans.Length == 2 && swans[0].Day.Count == 4 && swans[1].Day.Count == 2 && swans.All(s => s.Night.Count == s.Day.Count),
            $"Four black swans on the first lake, two on the second: {string.Join("; ", a.Log)}");
        var crows = a.Sites.Where(s => s.Kind == FaunaKind.Crow).ToArray();
        Check(crows.Length is >= 2 and <= 3 && crows.All(c => c.Day.Count is >= 2 and <= 3), $"2-3 crow groups of 2-3: {string.Join("; ", a.Log)}");
        for (var i = 0; i < crows.Length; i++)
            for (var j = i + 1; j < crows.Length; j++)
                Check(Vector3.Distance(crows[i].Day[0].Position, crows[j].Day[0].Position) >= 140f, "Crow groups at least 150 m apart.");
        var spots = cats.Select(s => s.Day[0].Position).ToArray();
        for (var i = 0; i < spots.Length; i++)
            for (var j = i + 1; j < spots.Length; j++)
                Check(Vector3.Distance(spots[i], spots[j]) >= FaunaStyle.CatSiteSpacing - 1f, "Cat sites keep apart.");
    }

    private sealed class NoNetworkWorld(IWorldModel inner) : IWorldModel
    {
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } =
            inner.Features.Where(p => p.Value.Source != FeatureSource.Design).ToDictionary(p => p.Key, p => p.Value);
        public WorldNetwork Network => WorldNetwork.Empty;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    [TestCase]
    public void AStubWorldWithoutNetworkOrOverlaysDegradesQuietly()
    {
        var layout = FaunaPlanner.Build(new NoNetworkWorld(World.Value), 1, Lakes.Value);
        Check(layout.Sites.Any(s => s.Kind == FaunaKind.Cat), "Cats still find a wall without roads.");
        Check(!layout.Sites.Any(s => s.Kind == FaunaKind.Pigeon), "No stepped plaza, no pigeons, and no exception.");
    }

    [TestCase]
    public void ScheduleFollowsTheSpeciesTable()
    {
        var clear = new WeatherVec(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
        var rain = clear with { Precip = 0.6f, Wet = 0.8f };
        var after = clear with { Wet = 0.6f };
        Check(FaunaSchedule.Classify(rain) == WeatherDim.Rain && FaunaSchedule.Classify(after) == WeatherDim.AfterRain && FaunaSchedule.Classify(clear) == WeatherDim.Clear, "Weather dimension.");
        Check(FaunaSchedule.Target(FaunaKind.Cat, 20, 9.0, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Cat, -10, 9.0, clear) == FaunaSet.Night, "Cats by day and night.");
        Check(FaunaSchedule.Target(FaunaKind.Cat, 20, 9.0, rain) == FaunaSet.Rain && FaunaSchedule.Target(FaunaKind.Cat, -10, 9.0, rain) == FaunaSet.Rain, "Cats under the eaves in rain.");
        Check(FaunaSchedule.Target(FaunaKind.Pigeon, 20, 9.0, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Pigeon, 20, 9.0, rain) == FaunaSet.Night, "Pigeons roost in rain.");
        Check(FaunaSchedule.Target(FaunaKind.Sparrow, 20, 9.0, rain) is null && FaunaSchedule.Target(FaunaKind.Sparrow, -10, 9.0, clear) is null, "No sparrows at night or in rain.");
        Check(FaunaSchedule.Target(FaunaKind.Sparrow, 20, 9.0, after) == FaunaSet.Day, "Birds come back after rain.");
        Check(FaunaSchedule.Target(FaunaKind.Cat, -5.9f, 9.0, clear) == FaunaSet.Day, "Night starts below -6 degrees.");
        Check(FaunaSchedule.Target(FaunaKind.Swan, 20, 9.0, clear with { Precip = 0.3f }) == FaunaSet.Day
            && FaunaSchedule.Target(FaunaKind.Swan, 20, 9.0, rain with { Precip = 0.8f }) == FaunaSet.Night, "Swans stay out in light rain, tuck in in heavy rain.");
        Check(FaunaSchedule.Target(FaunaKind.Crow, 4f, 17.5, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Crow, 20f, 15.0, clear) is null
            && FaunaSchedule.Target(FaunaKind.Crow, 4f, 6.5, clear) is null && FaunaSchedule.Target(FaunaKind.Crow, -8f, 18.5, clear) is null
            && FaunaSchedule.Target(FaunaKind.Crow, 4f, 17.5, rain) is null, "Crows only at dusk, never at night or in rain.");
    }

    [TestCase]
    public void ModelsAreClosedValidAndWithinBudget()
    {
        var ids = new Dictionary<string, byte>(StringComparer.Ordinal);
        byte Id(PaletteToken t)
        {
            if (!ids.TryGetValue(t.Value, out var id)) ids[t.Value] = id = (byte)(64 + 2 * ids.Count);
            return id;
        }
        var scene = new FaunaScene(Id);
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
            Check(mesh.Triangles is > 100 and < 7000, $"{key}: {mesh.Triangles} triangles.");
            if (key.Kind == FaunaKind.Swan) Check(box.Min.Y < -0.05f && box.Min.Y > -0.15f, $"{key}: the hull sits in the water (min y {box.Min.Y:F3}).");
            else Check(box.Min.Y > -0.01f && box.Min.Y < 0.03f, $"{key}: feet on the ground (min y {box.Min.Y:F3}).");
            Check(mesh.Normals.All(n => MathF.Abs(n.Length() - 1) < 1e-3f), $"{key}: unit normals.");
            Check(SignedVolume(mesh) < 0, $"{key}: clockwise front faces seen from outside (Godot), so the closed mesh has negative signed volume.");
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
            Check(height >= lo && height <= hi, $"{key}: height {height:F3} out of the species' range.");
        }
        var sites = FaunaPlanner.Build(World.Value, 1, Lakes.Value).Sites;
        var day = sites.SelectMany(s => s.Day).ToArray();
        var output = scene.Build(day);
        Check(output.Meshes.Count == 2 && output.Meshes.All(m => m.ColorIds is { } c && c.Length * 3 == m.Position.Length), "One cat and one bird bundle with colour IDs.");
        Check(scene.Triangles(day) < 90000, "The whole day set stays small.");
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
            Check(tokens.Contains(token.Value) && tokens.Contains(FaunaStyle.ShadeOf(token).Value), $"{field.Name}: '{token.Value}' and its shade are registered.");
        }
    }
}

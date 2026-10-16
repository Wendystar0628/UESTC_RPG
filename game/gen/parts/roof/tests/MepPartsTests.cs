using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Roof.Mep;
using GdUnit4;

namespace Campus.Gen.Parts.Roof.Tests;

[TestSuite]
public sealed class MepPartsTests
{
    private const byte InteriorNoShadow = 1 | 8;

    /// <summary>Floor-standing variations that must stop the walker; everything else is mounted and visual only.</summary>
    private static bool FloorStanding(string id, int variation) => id switch
    {
        PartCatalog.Boiler => variation != 2,
        PartCatalog.WaterDispenser or PartCatalog.Extinguisher => true,
        PartCatalog.IndoorAirConditioner => variation == 2,
        _ => false,
    };

    private static IEnumerable<Type> MepParts() =>
        typeof(MepGallery).Assembly.GetTypes().Where(t => t.Namespace == typeof(MepGallery).Namespace && t.GetCustomAttribute<PartAttribute>() is not null);

    [TestCase]
    public void RegisteredPartsMatchTheirCatalogueEntries()
    {
        var parts = MepParts().ToArray();
        Check(parts.Length == 10, $"Expected 10 interior equipment parts; found {parts.Length}.");
        foreach (var type in parts)
        {
            var attribute = type.GetCustomAttribute<PartAttribute>()!;
            var entry = PartCatalog.Find(new(attribute.Id)) ?? throw new InvalidOperationException($"{attribute.Id} is not catalogued.");
            Check(attribute.Id.StartsWith("interior.mep.", StringComparison.Ordinal), $"{attribute.Id} is not a group-D id.");
            Check(attribute.Owner == "WP-12" && entry.Owner == new WpId("WP-12"), $"{attribute.Id} has the wrong owner.");
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            Check(contract.GetGenericArguments()[0] == entry.ParameterType, $"{attribute.Id} takes the wrong parameter record.");
        }
        Check(parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id).Distinct().Count() == parts.Length, "A part ID is registered twice.");
    }

    [TestCase]
    public void CatalogueDefaultsBuildInEveryVariationWithinBudget()
    {
        foreach (var type in MepParts())
        {
            var entry = PartCatalog.Find(new(type.GetCustomAttribute<PartAttribute>()!.Id))!;
            var part = Activator.CreateInstance(type)!;
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            var lods = (System.Collections.IDictionary)contract.GetProperty("Lods")!.GetValue(part)!;
            var budget = (PartBudget)contract.GetProperty("Budget")!.GetValue(part)!;
            Check(lods.Count == 2 && lods.Contains(LodLevel.L0) && lods.Contains(LodLevel.L1), $"{entry.Id.Value} needs exactly the near and mid levels.");
            for (var variation = 0; variation <= 2; variation++)
            {
                var p = WithVariation((BoxPartParams)entry.Defaults, variation);
                var what = $"{entry.Id.Value} v{variation}";
                var bounds = (Box3)contract.GetMethod("Bounds")!.Invoke(part, [p])!;
                foreach (System.Collections.DictionaryEntry level in lods)
                {
                    var geometry = (PartGeometry)((Delegate)level.Value!).DynamicInvoke(p)!;
                    ClosedClockwise(geometry, $"{what} {level.Key}");
                    Check(geometry.Bounds == bounds, $"{what} {level.Key} moved the bounds.");
                    var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                    Check(triangles <= ((LodLevel)level.Key == LodLevel.L0 ? budget.L0 : budget.L1), $"{what} {level.Key} has {triangles} triangles, over budget.");
                }
                var colliders = (IReadOnlyList<Collider>)contract.GetMethod("Collision")!.Invoke(part, [p])!;
                if (FloorStanding(entry.Id.Value, variation))
                {
                    var box = colliders.Count == 1 ? colliders[0] as BoxCollider : null;
                    Check(box is { Tag: SurfaceTag.Prop } && box.Foot != FootMat.Auto, $"{what} should be one prop box with a foot material.");
                }
                else Check(colliders.Count == 0, $"{what} is mounted and should be visual only.");
            }
        }
    }

    [TestCase]
    public void ReviewSamplesAreClosedBoundedAndWithinBudget()
    {
        var budgets = MepParts().ToDictionary(t => t.GetCustomAttribute<PartAttribute>()!.Id,
            t => (PartBudget)t.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>))
                .GetProperty("Budget")!.GetValue(Activator.CreateInstance(t))!);
        var samples = MepGallery.CreateSamples();
        Check(samples.Count == 30, "Every part shows all three variations in the review set.");
        foreach (var group in samples.GroupBy(s => s.Part))
            Check(group.Select(s => s.Params.Variation).Distinct().Count() == 3, $"{group.Key.Value} lacks a variation in the review set.");
        foreach (var sample in samples)
            foreach (var level in new[] { LodLevel.L0, LodLevel.L1 })
            {
                var geometry = sample.Build(level) ?? throw new InvalidOperationException($"{sample.Id} lacks {level}.");
                ClosedClockwise(geometry, $"{sample.Id} {level}");
                Check(geometry.Bounds == sample.Bounds, $"{sample.Id} {level} moved the bounds.");
                var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                var budget = budgets[sample.Part.Value];
                Check(triangles <= (level == LodLevel.L0 ? budget.L0 : budget.L1), $"{sample.Id} {level} has {triangles} triangles, over budget.");
            }
    }

    [TestCase]
    public void EveryVertexIsInteriorAndCastsNoShadow()
    {
        foreach (var sample in MepGallery.CreateSamples())
            foreach (var level in new[] { LodLevel.L0, LodLevel.L1 })
                foreach (var mesh in sample.Build(level)!.Meshes)
                    Check(mesh.Flags is { } flags && flags.All(b => b == InteriorNoShadow), $"{sample.Id} {level} has a vertex without the interior and no-shadow flags.");
    }

    [TestCase]
    public void EveryColourIsARegisteredPair()
    {
        var palette = Path.Combine(RepositoryRoot(), "game", "style", "palette");
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { Path.Combine(palette, "core.json") }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(file));
            if (!document.RootElement.TryGetProperty("entries", out var entries)) continue;
            foreach (var entry in entries.EnumerateArray())
                if (entry.TryGetProperty("token", out var token)) tokens.Add(token.GetString()!);
        }
        foreach (var example in MepGallery.CreateExamples())
            foreach (var mesh in example.Geometry.Meshes)
            {
                var lit = mesh.Material.Spec.Lit.Value; var shade = mesh.Material.Spec.Shade.Value;
                Check(lit.EndsWith(".lit", StringComparison.Ordinal) && shade == lit[..^4] + ".shade", $"{example.Id} uses an unpaired colour {lit}.");
                Check(tokens.Contains(lit) && tokens.Contains(shade), $"{example.Id} uses unregistered colour {lit}.");
            }
    }

    [TestCase]
    public void FireEquipmentUsesTheRegisteredFireRed()
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(Path.Combine(RepositoryRoot(), "game", "style", "palette", "ext", "WP-12.json")));
        var ids = document.RootElement.GetProperty("entries").EnumerateArray().ToDictionary(e => e.GetProperty("token").GetString()!, e => e.GetProperty("id").GetInt32());
        Check(ids.GetValueOrDefault("equip.fireRed.lit") == 254 && ids.GetValueOrDefault("equip.fireRed.shade") == 255, "equip.fireRed must sit at ids 254/255.");
        foreach (var sample in MepGallery.CreateSamples().Where(s => s.Part.Value is PartCatalog.Hydrant or PartCatalog.Extinguisher))
            Check(sample.Build(LodLevel.L0)!.Meshes.Any(m => m.Material.Spec.Lit.Value == "equip.fireRed.lit"), $"{sample.Id} should be in fire red.");
    }

    [TestCase]
    public void ExitSignIsSelfLitOnTheAlwaysOnSignalChannel()
    {
        foreach (var sample in MepGallery.CreateSamples().Where(s => s.Part.Value == PartCatalog.ExitSign))
        {
            var lit = sample.Build(LodLevel.L0)!.Meshes.Where(m => m.Material.Spec.Emissive is not null).ToArray();
            Check(lit.Length >= 2, $"{sample.Id}: face and lettering should both be self-lit.");
            Check(lit.All(m => m.Material.Spec.Emissive == new ChannelId("sign.signal")), $"{sample.Id} is lit by the wrong channel.");
        }
        var p = new SignParams(0.36f, 0.145f, 0.03f);
        var glow = ExitSignPart.Glow(p, Matrix4x4.CreateTranslation(2, 2.3f, -5));
        Check(LightChannelCatalog.Slot(glow.Channel) == 22 && glow.Color == new PaletteToken("sign.green"), "The exit glow must use sign.signal and sign.green.");
        Check(Vector3.Distance(glow.Position, new(2, 2.3f + p.Height * 0.5f, -5 - p.Depth * 0.5f - 0.02f)) < 1e-5f, "The glow sits just in front of the face centre.");
        Check(glow.Range > 0 && glow.Energy > 0, "The glow needs a radius and a level.");
        // Only the exit sign is self-lit.
        foreach (var example in MepGallery.CreateExamples().Where(e => !e.Id.StartsWith("exit-", StringComparison.Ordinal)))
            Check(example.Geometry.Meshes.All(m => m.Material.Spec.Emissive is null), $"{example.Id} should not glow.");
    }

    [TestCase]
    public void DoorPlatesCarryTheirNumbersOnlyNear()
    {
        var p = new SignParams(0.3f, 0.12f, 0.02f);
        int Triangles(PartGeometry g) => g.Meshes.Sum(m => m.Index.Length / 3);
        var blank = new RoomSignPart().Lods[LodLevel.L0](p);
        var numbered = RoomSignPart.Build(p, "0123456789-", LodLevel.L0);
        ClosedClockwise(numbered, "every numeral");
        Check(Triangles(numbered) > Triangles(blank), "Numerals add strokes near.");
        Check(Triangles(RoomSignPart.Build(p, "1-201", LodLevel.L1)) == Triangles(new RoomSignPart().Lods[LodLevel.L1](p)), "Mid level drops the numerals.");
        var refused = false;
        try { RoomSignPart.Build(p, "A101", LodLevel.L0); }
        catch (ArgumentException) { refused = true; }
        Check(refused, "Characters without a glyph must be refused, not dropped.");
    }

    [TestCase]
    public void GenerationIsBitwiseRepeatable()
    {
        var first = MepGallery.CreateExamples(); var second = MepGallery.CreateExamples();
        Check(first.Count == second.Count && first.Count == 30, "Missing gallery examples.");
        for (var i = 0; i < first.Count; i++)
        {
            Check(first[i].Geometry.Meshes.Count == second[i].Geometry.Meshes.Count, "Surface count changed.");
            for (var m = 0; m < first[i].Geometry.Meshes.Count; m++)
            {
                var a = first[i].Geometry.Meshes[m]; var b = second[i].Geometry.Meshes[m];
                Check(a.Material == b.Material, "Material order changed.");
                Check(a.Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Position.Select(BitConverter.SingleToInt32Bits)), "Vertex bytes changed.");
                Check(a.Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Normal.Select(BitConverter.SingleToInt32Bits)), "Normal bytes changed.");
                Check(a.Index.SequenceEqual(b.Index), "Index bytes changed.");
            }
        }
    }

    private static BoxPartParams WithVariation(BoxPartParams defaults, int variation)
    {
        var copy = (BoxPartParams)defaults.GetType().GetMethod("<Clone>$")!.Invoke(defaults, null)!;
        typeof(BoxPartParams).GetProperty(nameof(BoxPartParams.Variation))!.SetValue(copy, variation);
        return copy;
    }

    private static string RepositoryRoot()
    {
        if (Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is { Length: > 0 } root) return root;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "game", "style", "palette", "core.json"))) return directory.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }

    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);
    private static void ClosedClockwise(PartGeometry geometry, string what)
    {
        Check(geometry.Meshes.Count > 0, $"{what} is empty.");
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                var a = m.Index[i]; var b = m.Index[i + 1]; var c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                Check(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0, $"{what}: face winding disagrees with outward normals.");
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            foreach (var (edge, count) in edges)
                Check(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1, $"{what}: a solid has an open or non-manifold edge.");
            for (var i = 0; i < m.Position.Length / 3; i++)
            {
                Check(geometry.Bounds.Contains(P(m, i)), $"{what}: bounds miss vertex {P(m, i)} ({geometry.Bounds.Min} – {geometry.Bounds.Max}).");
                Check(Math.Abs(1 - N(m, i).Length()) <= 1e-5, $"{what}: normal {i} is not unit length.");
            }
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

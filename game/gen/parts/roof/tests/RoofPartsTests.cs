using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;

namespace Campus.Gen.Parts.Roof.Tests;

[TestSuite]
public sealed class RoofPartsTests
{
    private static readonly Assembly Roof = typeof(RoofGallery).Assembly;

    /// <summary>Roof and facade parts only; the interior equipment in the Mep namespace has its own suite.</summary>
    private static IEnumerable<Type> RoofParts() => Roof.GetTypes().Where(t => t.Namespace == typeof(RoofGallery).Namespace && t.GetCustomAttribute<PartAttribute>() is not null);

    [TestCase]
    public void RegisteredPartsMatchTheirCatalogueEntries()
    {
        var parts = RoofParts().ToArray();
        Check(parts.Length == 11, $"Expected 11 roof parts; found {parts.Length}.");
        foreach (var type in parts)
        {
            var attribute = type.GetCustomAttribute<PartAttribute>()!;
            var entry = PartCatalog.Find(new(attribute.Id)) ?? throw new InvalidOperationException($"{attribute.Id} is not catalogued.");
            Check(attribute.Owner == "WP-12" && entry.Owner == new WpId("WP-12"), $"{attribute.Id} has the wrong owner.");
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            Check(contract.GetGenericArguments()[0] == entry.ParameterType, $"{attribute.Id} takes the wrong parameter record.");
        }
        Check(parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id).Distinct().Count() == parts.Length, "A part ID is registered twice.");
    }

    [TestCase]
    public void CatalogueDefaultsBuildClosedAtEveryLevelWithinBudget()
    {
        foreach (var type in RoofParts())
        {
            var entry = PartCatalog.Find(new(type.GetCustomAttribute<PartAttribute>()!.Id))!;
            var part = Activator.CreateInstance(type)!;
            var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(part)!;
            var bounds = (Box3)type.GetMethod("Bounds")!.Invoke(part, [entry.Defaults])!;
            var budget = (PartBudget)type.GetProperty("Budget")!.GetValue(part)!;
            Check(lods.Contains(LodLevel.L0) && lods.Contains(LodLevel.L1), $"{entry.Id.Value} lacks near levels.");
            foreach (System.Collections.DictionaryEntry level in lods)
            {
                var geometry = (PartGeometry)((Delegate)level.Value!).DynamicInvoke(entry.Defaults)!;
                ClosedClockwise(geometry, $"{entry.Id.Value} {level.Key}");
                Check(geometry.Bounds == bounds, $"{entry.Id.Value} {level.Key} moved the bounds.");
                var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                Check(triangles <= ((LodLevel)level.Key == LodLevel.L0 ? budget.L0 : budget.L1), $"{entry.Id.Value} {level.Key} has {triangles} triangles, over budget.");
            }
            // Interface dispatch, so default (empty) implementations count too. Roof and wall parts are visual only
            // (MAP-QSH-ADDITIONS §7); the equipment fence alone is a barrier.
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            var colliders = (IReadOnlyList<Collider>)contract.GetMethod("Collision")!.Invoke(part, [entry.Defaults])!;
            var barrier = entry.Id.Value == PartCatalog.EquipmentFence;
            Check(barrier ? colliders.Count == 1 && colliders[0].Tag == SurfaceTag.Barrier : colliders.Count == 0, $"{entry.Id.Value} has the wrong collision class.");
        }
    }

    [TestCase]
    public void ReviewSamplesAreClosedBoundedAndWithinBudget()
    {
        var budgets = RoofParts()
            .ToDictionary(t => t.GetCustomAttribute<PartAttribute>()!.Id, t => (PartBudget)t.GetProperty("Budget")!.GetValue(Activator.CreateInstance(t))!);
        foreach (var sample in RoofGallery.CreateSamples())
            foreach (var level in Enum.GetValues<LodLevel>())
            {
                if (sample.Build(level) is not { } geometry) continue;
                ClosedClockwise(geometry, $"{sample.Id} {level}");
                Check(geometry.Bounds == sample.Bounds, $"{sample.Id} {level} moved the bounds.");
                var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                var budget = budgets[sample.Part.Value];
                Check(triangles <= (level == LodLevel.L0 ? budget.L0 : budget.L1), $"{sample.Id} {level} has {triangles} triangles, over budget.");
            }
    }

    [TestCase]
    public void GenerationIsBitwiseRepeatable()
    {
        var first = RoofGallery.CreateExamples(); var second = RoofGallery.CreateExamples();
        Check(first.Count == second.Count && first.Count >= 11, "Missing gallery examples.");
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

    [TestCase]
    public void RingsCloseWithMitredCorners()
    {
        Vector2[] ring = [new(-4, -3), new(4, -3), new(4, 3), new(-1, 3), new(-1, 1), new(-4, 1)];
        ClosedClockwise(ParapetCapPart.Along(ring, true, new TrimParams(1, 0.3f, 0.5f), LodLevel.L0), "coping ring");
        ClosedClockwise(LightningStripPart.Along(ring, true, new TrimParams(1, 0.15f, 0.08f) { Variation = 1 }, LodLevel.L0), "strip ring");
        ClosedClockwise(EquipmentFencePart.Along(ring, true, new RailingParams(1, 1.8f, 0.06f, 1.5f), LodLevel.L0), "fence ring");
        ClosedClockwise(EquipmentFencePart.Along([new(0, 0), new(5, 0), new(5, 3)], false, new RailingParams(1, 1.8f, 0.06f, 1.5f) { Variation = 1 }, LodLevel.L0), "fence run");
    }

    [TestCase]
    public void SizesFollowTheStyleRules()
    {
        // The catalogue's bare-rod default is raised to the prop minimum section and the documented standoff.
        var strip = new LightningStripPart().Bounds(new TrimParams(3.6f, 0.02f, 0.02f));
        Check(strip.Max.Z - strip.Min.Z >= RoofStyle.StripMinRod && strip.Max.Y >= RoofStyle.StripDefaultHeight, "Lightning strip thinner than the style minimum.");
        // The real-size unit fits inside its grille cage, behind the front louvres and under the lid.
        var unit = RoofGallery.DormUnit; var cage = RoofGallery.DormCage;
        Check(unit.Width <= cage.Width - 2 * 0.07f && unit.Height <= cage.Height - RoofStyle.GrilleTopThickness && unit.Depth <= cage.Depth - 0.07f, "The unit does not fit its cage.");
        // Every surface colour is a registered Lit/Shade pair name, never a literal.
        foreach (var example in RoofGallery.CreateExamples())
            foreach (var mesh in example.Geometry.Meshes)
            {
                var lit = mesh.Material.Spec.Lit.Value; var shade = mesh.Material.Spec.Shade.Value;
                Check(lit.EndsWith(".lit", StringComparison.Ordinal) && shade == lit[..^4] + ".shade", $"{example.Id} uses an unpaired colour {lit}.");
            }
        IPartDef<RailingParams> fence = new EquipmentFencePart();
        var collider = (WallCollider)fence.Collision(new RailingParams() { Foot = FootMat.Metal }).Single();
        Check(collider.Foot == FootMat.Metal && collider.Y1 >= 1.5f, "Fence collision lost the foot material or the 1.5 m barrier top.");
    }

    [TestCase]
    public void EveryColourIsRegisteredAndOurSegmentStaysInsideItsIds()
    {
        var palette = Path.Combine(RepositoryRoot(), "game", "style", "palette");
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { Path.Combine(palette, "core.json") }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(file));
            if (!document.RootElement.TryGetProperty("entries", out var entries)) continue;
            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("token", out var token)) continue;
                tokens.Add(token.GetString()!);
                if (Path.GetFileName(file) != "WP-12.json") continue;
                Check(entry.GetProperty("id").GetInt32() is >= 248 and <= 251 or >= 254 and <= 255, $"{token.GetString()} lies outside WP-12's ids 248–251 and 254–255.");
                // Equipment surfaces are large neutral areas: intrinsic OKLCH chroma stays within 0.05 (ART-MODELING §5).
                // Fire red is the small saturated identity colour (WLD-INTERIORS §8.4) and is exempt.
                if (token.GetString()!.StartsWith("equip.fireRed.", StringComparison.Ordinal)) continue;
                var albedo = entry.GetProperty("albedo").EnumerateArray().Select(v => v.GetDouble()).ToArray();
                Check(OklabChroma(albedo[0], albedo[1], albedo[2]) <= 0.05, $"{token.GetString()} has intrinsic chroma above 0.05.");
            }
        }
        foreach (var example in RoofGallery.CreateExamples())
            foreach (var mesh in example.Geometry.Meshes)
                Check(tokens.Contains(mesh.Material.Spec.Lit.Value) && tokens.Contains(mesh.Material.Spec.Shade.Value),
                    $"{example.Id} uses unregistered colour {mesh.Material.Spec.Lit.Value}.");
    }

    [TestCase]
    public void KernelVisibilityPassesThroughOnlyOnOpeningReveals()
    {
        foreach (var example in RoofGallery.CreateExamples())
        {
            var reduced = example.Geometry.Meshes.Where(m => m.Ao is not null).SelectMany(m => m.Ao!).Where(v => v < 1f).ToArray();
            // Only the louvre frames are opening panels; their reveals keep the kernel's visibility, nothing goes dark.
            var grille = example.Id.StartsWith("grille-", StringComparison.Ordinal);
            Check(grille ? reduced.Length > 0 : reduced.Length == 0, $"{example.Id} has the wrong occlusion layout.");
            Check(reduced.All(v => v is >= 0.45f and <= 0.88f), $"{example.Id} has a reveal visibility outside 0.45–0.88.");
        }
    }

    /// <summary>OKLCH chroma of an sRGB colour (Ottosson's matrices, as WP-37's Oklab).</summary>
    private static double OklabChroma(double r, double g, double b)
    {
        static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        double lr = Linear(r), lg = Linear(g), lb = Linear(b);
        var l = Math.Cbrt(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb);
        var m = Math.Cbrt(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
        var s = Math.Cbrt(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
        var a = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var bb = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        return Math.Sqrt(a * a + bb * bb);
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
                Near(1, N(m, i).Length(), 1e-5);
            }
        }
    }
    private static void Near(double expected, double actual, double tolerance) => Check(Math.Abs(expected - actual) <= tolerance, $"Expected {expected}; got {actual}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Roof.Mep;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Parts.Roof.Tests;

[TestSuite]
public sealed class MepPartsTests
{
    private const byte InteriorNoShadow = 1 | 8;

    /// <summary>Floor-standing variations that must stop the walker; everything else is mounted and visual only.</summary>
    private static bool FloorStanding(string id, int variation) => id switch
    {
        PartCatalog.Boiler => variation != 2,
        PartCatalog.WaterDispenser or PartCatalog.Extinguisher or PartCatalog.Turnstile or PartCatalog.Switchgear => true,
        PartCatalog.IndoorAirConditioner => variation == 2,
        _ => false,
    };

    private static IEnumerable<Type> MepParts() =>
        typeof(BoilerPart).Assembly.GetTypes().Where(t => t.Namespace == typeof(BoilerPart).Namespace && t.GetCustomAttribute<PartAttribute>() is not null);

    [TestCase, TestCategory(TestReason.Contract)]
    public void RegisteredPartsMatchTheirCatalogueEntries()
    {
        var parts = MepParts().ToArray();
        AssertThat(parts.Length > 0).OverrideFailureMessage("No interior equipment parts are registered.").IsTrue();
        foreach (var type in parts)
        {
            var attribute = type.GetCustomAttribute<PartAttribute>()!;
            var entry = PartCatalog.Find(new(attribute.Id)) ?? throw new InvalidOperationException($"{attribute.Id} is not catalogued.");
            AssertThat(attribute.Id.StartsWith("interior.mep.", StringComparison.Ordinal)).OverrideFailureMessage($"{attribute.Id} is not a group-D id.").IsTrue();
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            AssertThat(contract.GetGenericArguments()[0] == entry.ParameterType).OverrideFailureMessage($"{attribute.Id} takes the wrong parameter record.").IsTrue();
        }
        AssertThat(parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id).Distinct().Count() == parts.Length).OverrideFailureMessage("A part ID is registered twice.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CatalogueDefaultsBuildInEveryVariation()
    {
        foreach (var type in MepParts())
        {
            var entry = PartCatalog.Find(new(type.GetCustomAttribute<PartAttribute>()!.Id))!;
            var part = Activator.CreateInstance(type)!;
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            var lods = (System.Collections.IDictionary)contract.GetProperty("Lods")!.GetValue(part)!;
            AssertThat(lods.Count == 2 && lods.Contains(LodLevel.L0) && lods.Contains(LodLevel.L1)).OverrideFailureMessage($"{entry.Id.Value} needs exactly the near and mid levels.").IsTrue();
            for (var variation = 0; variation <= 2; variation++)
            {
                var p = WithVariation((BoxPartParams)entry.Defaults, variation);
                var what = $"{entry.Id.Value} v{variation}";
                var bounds = (Box3)contract.GetMethod("Bounds")!.Invoke(part, [p])!;
                foreach (System.Collections.DictionaryEntry level in lods)
                {
                    var geometry = (PartGeometry)((Delegate)level.Value!).DynamicInvoke(p)!;
                    ClosedClockwise(geometry, $"{what} {level.Key}");
                    AssertThat(geometry.Bounds == bounds).OverrideFailureMessage($"{what} {level.Key} moved the bounds.").IsTrue();
                }
                var colliders = (IReadOnlyList<Collider>)contract.GetMethod("Collision")!.Invoke(part, [p])!;
                if (FloorStanding(entry.Id.Value, variation))
                {
                    var box = colliders.Count == 1 ? colliders[0] as BoxCollider : null;
                    AssertThat(box is { Tag: SurfaceTag.Prop } && box.Foot != FootMat.Auto).OverrideFailureMessage($"{what} should be one prop box with a foot material.").IsTrue();
                }
                else AssertThat(colliders.Count == 0).OverrideFailureMessage($"{what} is mounted and should be visual only.").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ExitSignIsSelfLitOnTheAlwaysOnSignalChannel()
    {
        var p = new SignParams(0.36f, 0.145f, 0.03f);
        var glow = ExitSignPart.Glow(p, Matrix4x4.CreateTranslation(2, 2.3f, -5));
        AssertThat(glow.Color == new PaletteToken("sign.green")).OverrideFailureMessage("The exit glow must use sign.green.").IsTrue();
        AssertThat(Vector3.Distance(glow.Position, new(2, 2.3f + p.Height * 0.5f, -5 - p.Depth * 0.5f - 0.02f)) < TestTolerance.Position).OverrideFailureMessage("The glow sits just in front of the face centre.").IsTrue();
        AssertThat(glow.Range > 0 && glow.Energy > 0).OverrideFailureMessage("The glow needs a radius and a level.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void DoorPlatesCarryTheirNumbersOnlyNear()
    {
        var p = new SignParams(0.3f, 0.12f, 0.02f);
        int Triangles(PartGeometry g) => g.Meshes.Sum(m => m.Index.Length / 3);
        var blank = new RoomSignPart().Lods[LodLevel.L0](p);
        var numbered = RoomSignPart.Build(p, "0123456789-", LodLevel.L0);
        ClosedClockwise(numbered, "every numeral");
        AssertThat(Triangles(numbered) > Triangles(blank)).OverrideFailureMessage("Numerals add strokes near.").IsTrue();
        AssertThat(Triangles(RoomSignPart.Build(p, "1-201", LodLevel.L1)) == Triangles(new RoomSignPart().Lods[LodLevel.L1](p))).OverrideFailureMessage("Mid level drops the numerals.").IsTrue();
        var refused = false;
        try { RoomSignPart.Build(p, "A101", LodLevel.L0); }
        catch (ArgumentException) { refused = true; }
        AssertThat(refused).OverrideFailureMessage("Characters without a glyph must be refused, not dropped.").IsTrue();
    }

    private static BoxPartParams WithVariation(BoxPartParams defaults, int variation)
    {
        var copy = (BoxPartParams)defaults.GetType().GetMethod("<Clone>$")!.Invoke(defaults, null)!;
        typeof(BoxPartParams).GetProperty(nameof(BoxPartParams.Variation))!.SetValue(copy, variation);
        return copy;
    }

    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);
    private static void ClosedClockwise(PartGeometry geometry, string what)
    {
        AssertThat(geometry.Meshes.Count > 0).OverrideFailureMessage($"{what} is empty.").IsTrue();
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                var a = m.Index[i]; var b = m.Index[i + 1]; var c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                AssertThat(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0).OverrideFailureMessage($"{what}: face winding disagrees with outward normals.").IsTrue();
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            foreach (var (edge, count) in edges)
                AssertThat(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1).OverrideFailureMessage($"{what}: a solid has an open or non-manifold edge.").IsTrue();
            for (var i = 0; i < m.Position.Length / 3; i++)
            {
                AssertThat(geometry.Bounds.Contains(P(m, i))).OverrideFailureMessage($"{what}: bounds miss vertex {P(m, i)} ({geometry.Bounds.Min} – {geometry.Bounds.Max}).").IsTrue();
                AssertThat(Math.Abs(1 - N(m, i).Length()) <= TestTolerance.Position).OverrideFailureMessage($"{what}: normal {i} is not unit length.").IsTrue();
            }
        }
    }

}

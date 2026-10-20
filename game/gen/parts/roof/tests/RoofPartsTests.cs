using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Parts.Roof.Tests;

[TestSuite]
public sealed class RoofPartsTests
{
    private static readonly Assembly Roof = typeof(RoofEquipment).Assembly;

    /// <summary>Roof and facade parts only; the interior equipment in the Mep namespace has its own suite.</summary>
    private static IEnumerable<Type> RoofParts() => Roof.GetTypes().Where(t => t.Namespace == typeof(RoofEquipment).Namespace && t.GetCustomAttribute<PartAttribute>() is not null);

    [TestCase, TestCategory(TestReason.Contract)]
    public void RegisteredPartsMatchTheirCatalogueEntries()
    {
        var parts = RoofParts().ToArray();
        AssertThat(parts.Length > 0).OverrideFailureMessage("No roof parts are registered.").IsTrue();
        foreach (var type in parts)
        {
            var attribute = type.GetCustomAttribute<PartAttribute>()!;
            var entry = PartCatalog.Find(new(attribute.Id)) ?? throw new InvalidOperationException($"{attribute.Id} is not catalogued.");
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            AssertThat(contract.GetGenericArguments()[0] == entry.ParameterType).OverrideFailureMessage($"{attribute.Id} takes the wrong parameter record.").IsTrue();
        }
        AssertThat(parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id).Distinct().Count() == parts.Length).OverrideFailureMessage("A part ID is registered twice.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CatalogueDefaultsBuildClosedAtEveryLevel()
    {
        foreach (var type in RoofParts())
        {
            var entry = PartCatalog.Find(new(type.GetCustomAttribute<PartAttribute>()!.Id))!;
            var part = Activator.CreateInstance(type)!;
            var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(part)!;
            var bounds = (Box3)type.GetMethod("Bounds")!.Invoke(part, [entry.Defaults])!;
            AssertThat(lods.Contains(LodLevel.L0) && lods.Contains(LodLevel.L1)).OverrideFailureMessage($"{entry.Id.Value} lacks near levels.").IsTrue();
            foreach (System.Collections.DictionaryEntry level in lods)
            {
                var geometry = (PartGeometry)((Delegate)level.Value!).DynamicInvoke(entry.Defaults)!;
                ClosedClockwise(geometry, $"{entry.Id.Value} {level.Key}");
                AssertThat(geometry.Bounds == bounds).OverrideFailureMessage($"{entry.Id.Value} {level.Key} moved the bounds.").IsTrue();
            }
            // Interface dispatch, so default (empty) implementations count too. Roof and wall parts are visual only
            // (MAP-QSH-ADDITIONS); the equipment fence alone is a barrier.
            var contract = type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            var colliders = (IReadOnlyList<Collider>)contract.GetMethod("Collision")!.Invoke(part, [entry.Defaults])!;
            var barrier = entry.Id.Value == PartCatalog.EquipmentFence;
            AssertThat(barrier ? colliders.Count == 1 && colliders[0].Tag == SurfaceTag.Barrier : colliders.Count == 0).OverrideFailureMessage($"{entry.Id.Value} has the wrong collision class.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RingsCloseWithMitredCorners()
    {
        Vector2[] ring = [new(-4, -3), new(4, -3), new(4, 3), new(-1, 3), new(-1, 1), new(-4, 1)];
        ClosedClockwise(ParapetCapPart.Along(ring, true, new TrimParams(1, 0.3f, 0.5f), LodLevel.L0), "coping ring");
        ClosedClockwise(LightningStripPart.Along(ring, true, new TrimParams(1, 0.15f, 0.08f) { Variation = 1 }, LodLevel.L0), "strip ring");
        ClosedClockwise(EquipmentFencePart.Along(ring, true, new RailingParams(1, 1.8f, 0.06f, 1.5f), LodLevel.L0), "fence ring");
        ClosedClockwise(EquipmentFencePart.Along([new(0, 0), new(5, 0), new(5, 3)], false, new RailingParams(1, 1.8f, 0.06f, 1.5f) { Variation = 1 }, LodLevel.L0), "fence run");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SizesFollowTheStyleRules()
    {
        // The catalogue's bare-rod default is raised to the prop minimum section and the documented standoff.
        var strip = new LightningStripPart().Bounds(new TrimParams(3.6f, 0.02f, 0.02f));
        AssertThat(strip.Max.Z - strip.Min.Z >= RoofStyle.StripMinRod && strip.Max.Y >= RoofStyle.StripDefaultHeight).OverrideFailureMessage("Lightning strip thinner than the style minimum.").IsTrue();
        // The real-size unit fits inside its grille cage, behind the front louvres and under the lid.
        var unit = RoofEquipment.DormUnit; var cage = RoofEquipment.DormCage;
        AssertThat(unit.Width <= cage.Width - 2 * 0.07f && unit.Height <= cage.Height - RoofStyle.GrilleTopThickness && unit.Depth <= cage.Depth - 0.07f).OverrideFailureMessage("The unit does not fit its cage.").IsTrue();
        IPartDef<RailingParams> fence = new EquipmentFencePart();
        var collider = (WallCollider)fence.Collision(new RailingParams() { Foot = FootMat.Metal }).Single();
        AssertThat(collider.Foot == FootMat.Metal && collider.Y1 >= WalkRules.BarrierHeight).OverrideFailureMessage("Fence collision lost the foot material or the 1.5 m barrier top.").IsTrue();
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
                AssertThat(Math.Abs((double)(1) - N(m, i).Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {N(m, i).Length()}.").IsTrue();
            }
        }
    }
}

using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Parts.Openings.Tests;

[TestSuite]
public sealed class OpeningPartsTests
{
    private static IEnumerable<Type> OpeningTypes => typeof(OpeningPartFactory).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(OpeningPartFactory).Namespace ||
            type.Namespace?.StartsWith(typeof(OpeningPartFactory).Namespace + ".", StringComparison.Ordinal) == true);

    private static IEnumerable<PartCatalogEntry> OpeningEntries
    {
        get
        {
            var ids = OpeningTypes.Select(type => type.GetCustomAttribute<PartAttribute>())
                .OfType<PartAttribute>().Select(attribute => attribute.Id).ToHashSet(StringComparer.Ordinal);
            return PartCatalog.Entries.Where(entry => ids.Contains(entry.Id.Value));
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EveryOpeningCatalogEntryHasARealPartWithTheCatalogParameters()
    {
        var parts = OpeningTypes.Select(t => (Type: t, Attribute: t.GetCustomAttribute<PartAttribute>())).Where(x => x.Attribute is not null).ToArray();
        foreach (var entry in OpeningEntries)
        {
            var match = parts.Where(x => x.Attribute!.Id == entry.Id.Value).ToArray();
            AssertThat(match.Length == 1).OverrideFailureMessage($"No single opening part for {entry.Id.Value}.").IsTrue();
            AssertThat(typeof(IPartDef<>).MakeGenericType(entry.ParameterType).IsAssignableFrom(match[0].Type)).OverrideFailureMessage($"{entry.Id.Value} has the wrong parameter type.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CatalogDefaultsBuildClosedGeometryAtEveryLevel()
    {
        foreach (var entry in OpeningEntries)
            foreach (var level in Enum.GetValues<LodLevel>())
                Closed(OpeningPartFactory.Build(entry.Id, entry.Defaults, level), entry.Id.Value + " " + level);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FramesKeepNoShadowAndLeaveFarLevelsToTheBuilding()
    {
        var p = new WindowParams(1.8f, 1.6f, 0.07f, 2, 2);
        var part = new CasementWindowPart();
        var near = part.Lods[LodLevel.L0](p);
        AssertThat(near.Meshes.All(m => m.Flags!.All(f => f == 8))).OverrideFailureMessage("Window frames must not cast shadow.").IsTrue();
        AssertThat(near.Meshes.All(m => m.Ao!.All(a => a == 1f))).OverrideFailureMessage("Open faces must write AO visibility 1.").IsTrue();
        AssertThat(near.Meshes.Any(m => m.Material.Spec == OpeningStyle.Gasket)).OverrideFailureMessage("L0 lacks the gasket line.").IsTrue();
        AssertThat(part.Lods[LodLevel.L1](p).Meshes.Sum(m => m.Index.Length) < near.Meshes.Sum(m => m.Index.Length)).OverrideFailureMessage("L1 is not simpler than L0.").IsTrue();
        AssertThat(part.Lods[LodLevel.L2](p).Meshes.Count == 0).OverrideFailureMessage("Frames stay out of L2.").IsTrue();
        var own = new CasementWindowPart().Lods[LodLevel.L0](p with { Material = OpeningStyle.Spec("wall.greyWhite") });
        AssertThat(own.Meshes.Any(m => m.Material.Spec.Lit.Value == "wall.greyWhite.lit")).OverrideFailureMessage("An explicit material was ignored.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void KernelVisibilityPassesThroughUnchanged()
    {
        // Wood door at L0: the vision hole is cut by the kernel's slab, whose reveal faces carry visibility 0.8.
        var door = new WoodDoorPart().Lods[LodLevel.L0](new DoorParams(0.9f, 2.1f, 0.2f));
        var leaf = door.Meshes.Single(m => m.Material.Spec.Lit.Value == "door.wood.lit");
        var values = leaf.Ao!.Distinct().OrderBy(v => v).ToArray();
        AssertThat(values.SequenceEqual([0.8f, 1f])).OverrideFailureMessage("Kernel visibility must pass through: reveals 0.8, faces 1; got " + string.Join(", ", values)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void DoorGlassUsesTheWindowCardEncoding()
    {
        var door = new EntranceDoorPart().Lods[LodLevel.L0](new DoorParams(1.8f, 2.7f, 0.12f, 0f, 2));
        var glass = door.Meshes.Single(m => m.Material.Spec.Kind == ToonKind.Glass);
        AssertThat(glass.Uv2 is not null && glass.Reserved is not null).OverrideFailureMessage("Door glass lacks UV2 or the card byte.").IsTrue();
        for (var i = 0; i < glass.Reserved!.Length; i++)
        {
            AssertThat(glass.Reserved[i] == OpeningStyle.DoorCard && glass.Uv2![2 * i] == 0 && glass.Uv2[2 * i + 1] == 32).OverrideFailureMessage("Door glass encoding changed.").IsTrue();
            AssertThat(glass.Uv![2 * i] >= 0 && glass.Uv[2 * i] < 1 && glass.Uv[2 * i + 1] >= 0 && glass.Uv[2 * i + 1] < 1).OverrideFailureMessage("Door glass leaves its pane cell.").IsTrue();
        }
        var pane = OpeningGlass.Opening(1.8f, 1.6f, 1000, 7, 3, 24, LightChannelCatalog.Id(2), 8);
        AssertThat(pane.Index.Length == 6 && pane.Reserved!.All(b => b == 8)).OverrideFailureMessage("Opening glass is one card quad.").IsTrue();
        for (var i = 0; i < pane.Position.Length / 3; i++)
        {
            AssertThat((int)Math.Floor(pane.Uv![2 * i]) == 7 && (int)Math.Floor(pane.Uv[2 * i + 1]) == 3).OverrideFailureMessage("Window cell differs from game/gen/buildings/'s.").IsTrue();
            AssertThat(pane.Uv2![2 * i] == 1000 && pane.Uv2[2 * i + 1] == 2 + 32 * 24).OverrideFailureMessage("UV2 differs from game/gen/buildings/'s.").IsTrue();
            AssertThat(pane.Normal[3 * i + 2] == -1).OverrideFailureMessage("Glass must face the exterior (−Z).").IsTrue();
        }
        var grid = OpeningGlass.Grid(3.6f, 3.2f, 3, 2, 0, 4, 1, 24, LightChannelCatalog.Id(0), 3);
        AssertThat(grid.Index.Length == 36).OverrideFailureMessage("A 3 × 2 grid has six lights.").IsTrue();
        AssertThrown(() => OpeningGlass.Opening(1, 1, 0, 0, 0, 1, new ChannelId("window.unknown"), 1)).IsInstanceOf<ArgumentException>();
        var frame = OpeningPlacement.Frame(new(10, 3, 0), new(0, 0, -1));
        var decl = OpeningGlass.Declare(frame, 1.8f, 1.6f, 1000, 7, 3, 24, LightChannelCatalog.Id(2));
        AssertThat(decl.Id == 1000 + 3 * 24 + 7 && decl.Center == new Vector3(10, 3.8f, 0)).OverrideFailureMessage("The declaration must match the glass numbering.").IsTrue();
        AssertThat(Vector3.Cross(decl.Right, decl.Up) == new Vector3(0, 0, -1)).OverrideFailureMessage("Right × Up must point outdoors.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ClosedDoorsAndLoweredShuttersBlockAsDoorClosed()
    {
        var shut = (BoxCollider)new WoodDoorPart().Collision(new DoorParams(0.9f, 2.1f, 0.2f)).Single();
        AssertThat(shut.Tag == SurfaceTag.DoorClosed && shut.Foot == FootMat.WoodDoor && shut.At.M42 == 1.05f).OverrideFailureMessage("A shut door is a DoorClosed closure.").IsTrue();
        AssertThat(new EntranceDoorPart().Collision(new DoorParams(1.8f, 2.4f, 0.12f, 90f, 2)).Count == 0).OverrideFailureMessage("Open leaves carry no collision.").IsTrue();
        var half = (BoxCollider)new RollerShutterPart().Collision(new DoorParams(3f, 2.8f, 0.15f, 45f)).Single();
        AssertThat(half.Tag == SurfaceTag.DoorClosed && half.Foot == FootMat.Shutter).OverrideFailureMessage("A half-lowered shutter blocks the way.").IsTrue();
        AssertThat(new RollerShutterPart().Collision(new DoorParams(3f, 2.8f, 0.15f, 90f)).Count == 0).OverrideFailureMessage("A raised shutter leaves the way free.").IsTrue();
        AssertThat(new CasementWindowPart().Collision(new WindowParams()).Count == 0).OverrideFailureMessage("Windows collide through their wall.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OpenLeavesSwingOutwardsWithinBounds()
    {
        var p = new DoorParams(1.8f, 2.4f, 0.12f, 90f, 2);
        var part = new EntranceDoorPart();
        var geometry = part.Lods[LodLevel.L0](p);
        var minZ = geometry.Meshes.SelectMany(m => Enumerable.Range(0, m.Position.Length / 3).Select(i => m.Position[3 * i + 2])).Min();
        AssertThat(minZ < -0.7f).OverrideFailureMessage("Open leaves must swing out towards −Z.").IsTrue();
        Closed(geometry, "open door");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CurtainChoiceCoversEveryStateAndIsStable()
    {
        // The ratios are design values, so only their presence is checked.
        const int samples = TestSamples.PerProperty;
        int none = 0, open = 0, half = 0, drawn = 0, cream = 0;
        for (var i = 0; i < samples; i++)
        {
            var c = OpeningPlacement.Curtain(7u, i, 1.6f, 1.4f);
            if (c is null) { none++; continue; }
            if (c.Material.Lit.Value == "curtain.cream.lit") cream++;
            AssertThat(OpeningStyle.CurtainColours.Any(k => k.Family + ".lit" == c.Material.Lit.Value)).OverrideFailureMessage("Curtain colour outside the list.").IsTrue();
            switch (c.Variation) { case 0: open++; break; case 1: half++; break; default: drawn++; break; }
            AssertThat(c.Material.Steps == ShadeSteps.TwoHalf).OverrideFailureMessage("Curtains keep soft-goods shading.").IsTrue();
        }
        AssertThat(none > 0 && none < samples).OverrideFailureMessage("Some windows hang curtains and some do not.").IsTrue();
        AssertThat(open > 0 && half > 0 && drawn > 0 && cream > 0).OverrideFailureMessage("Every curtain state and the cream colour occur.").IsTrue();
        AssertThat(OpeningPlacement.Curtain(7u, 42, 1.6f, 1.4f) == OpeningPlacement.Curtain(7u, 42, 1.6f, 1.4f)).OverrideFailureMessage("Curtain choice must be stable.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PlacementFrameIsAProperRotation()
    {
        var m = OpeningPlacement.Frame(new(5, 1, 2), new(-3, 0, 0));
        var x = new Vector3(m.M11, m.M12, m.M13); var y = new Vector3(m.M21, m.M22, m.M23); var z = new Vector3(m.M31, m.M32, m.M33);
        AssertThat(Math.Abs((double)(1) - Vector3.Dot(Vector3.Cross(x, y), z)) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {Vector3.Dot(Vector3.Cross(x, y), z)}.").IsTrue();
        AssertThat(Math.Abs((double)(1) - y.Y) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {y.Y}.").IsTrue();
        AssertThat(z == new Vector3(1, 0, 0) && m.M41 == 5).OverrideFailureMessage("Exterior must map to the part's −Z.").IsTrue();
        AssertThat(OpeningPlacement.Frame(Vector3.Zero, new(0, 0, -1)) == Matrix4x4.Identity).OverrideFailureMessage("A facade facing −Z is the part frame itself.").IsTrue();
        AssertThrown(() => OpeningPlacement.Frame(Vector3.Zero, new(0, 1, 0))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => OpeningPlacement.Frame(Vector3.Zero, Vector3.Zero)).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void InvalidParametersFailClosed()
    {
        AssertThrown(() => new CasementWindowPart().Lods[LodLevel.L0](new WindowParams(0.4f, 0.4f, 0.07f, 6, 1))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new EntranceDoorPart().Bounds(new DoorParams(1.8f, 2.4f, 0.12f, 200f))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new EntranceDoorPart().Bounds(new DoorParams(0.6f, 2.4f, 0.12f, 0f, 4))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new CurtainPart().Bounds(new PanelParams(1.8f, 2.2f, 0.06f) { Variation = 3 })).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new RollerShutterPart().Bounds(new DoorParams(-1f, 2.8f, 0.15f))).IsInstanceOf<ArgumentException>();
    }

    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);

    /// <summary>Every surface is a set of closed solids, wound clockwise against its normals, inside the part bounds.</summary>
    private static void Closed(PartGeometry geometry, string what)
    {
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                int a = m.Index[i], b = m.Index[i + 1], c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                AssertThat(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0).OverrideFailureMessage($"{what}: winding disagrees with the normals.").IsTrue();
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            foreach (var (edge, count) in edges)
                AssertThat(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1).OverrideFailureMessage($"{what}: open or non-manifold edge {edge}.").IsTrue();
            for (var i = 0; i < m.Position.Length / 3; i++)
            {
                AssertThat(geometry.Bounds.Contains(P(m, i))).OverrideFailureMessage($"{what}: vertex {P(m, i)} outside {geometry.Bounds}.").IsTrue();
                AssertThat(Math.Abs((double)(1) - N(m, i).Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {N(m, i).Length()}.").IsTrue();
            }
        }
    }

}

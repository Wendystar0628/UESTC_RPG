using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;

namespace Campus.Gen.Parts.Openings.Tests;

[TestSuite]
public sealed class OpeningPartsTests
{
    private static IEnumerable<Type> OpeningTypes => typeof(OpeningGallery).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(OpeningGallery).Namespace ||
            type.Namespace?.StartsWith(typeof(OpeningGallery).Namespace + ".", StringComparison.Ordinal) == true);

    [TestCase]
    public void EveryWp10CatalogEntryHasARealPartWithTheCatalogParameters()
    {
        var parts = OpeningTypes.Select(t => (Type: t, Attribute: t.GetCustomAttribute<PartAttribute>())).Where(x => x.Attribute is not null).ToArray();
        foreach (var entry in PartCatalog.Entries.Where(e => e.Owner.Value == "WP-10"))
        {
            var match = parts.Where(x => x.Attribute!.Id == entry.Id.Value).ToArray();
            Check(match.Length == 1, $"No single WP-10 part for {entry.Id.Value}.");
            Check(match[0].Attribute!.Owner == "WP-10", $"{entry.Id.Value} is not owned by WP-10.");
            Check(typeof(IPartDef<>).MakeGenericType(entry.ParameterType).IsAssignableFrom(match[0].Type), $"{entry.Id.Value} has the wrong parameter type.");
        }
    }

    [TestCase]
    public void CatalogDefaultsBuildClosedGeometryAtEveryLevel()
    {
        foreach (var entry in PartCatalog.Entries.Where(e => e.Owner.Value == "WP-10"))
            foreach (var level in Enum.GetValues<LodLevel>())
                Closed(OpeningGallery.Build(entry.Id, entry.Defaults, level), entry.Id.Value + " " + level);
    }

    [TestCase]
    public void GallerySamplesAreClosedInsideBoundsAndWithinBudget()
    {
        var budgets = OpeningTypes.Where(t => t.GetCustomAttribute<PartAttribute>() is not null)
            .ToDictionary(t => t.GetCustomAttribute<PartAttribute>()!.Id, t => (PartBudget)t.GetProperty("Budget")!.GetValue(Activator.CreateInstance(t))!);
        var over = new List<string>();
        void Measure(string id, PartId part, object parameters)
        {
            foreach (var level in Enum.GetValues<LodLevel>())
            {
                var geometry = OpeningGallery.Build(part, parameters, level);
                Closed(geometry, id + " " + level);
                var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                var budget = budgets[part.Value];
                if (level == LodLevel.L0 && (triangles == 0 || triangles > budget.L0)) over.Add($"{id} L0 {triangles}/{budget.L0}");
                if (level == LodLevel.L1 && triangles > budget.L1) over.Add($"{id} L1 {triangles}/{budget.L1}");
            }
        }
        foreach (var sample in OpeningGallery.Samples())
        {
            Measure(sample.Id, sample.Part, sample.Params);
            if (sample.Covering is { } covering) Measure(sample.Id + " covering", covering, sample.CoveringParams!);
        }
        Check(over.Count == 0, "Over budget: " + string.Join("; ", over));
    }

    [TestCase]
    public void GenerationIsBitwiseRepeatable()
    {
        foreach (var sample in OpeningGallery.Samples())
        {
            var a = OpeningGallery.Build(sample.Part, sample.Params, LodLevel.L0);
            var b = OpeningGallery.Build(sample.Part, sample.Params, LodLevel.L0);
            Check(a.Meshes.Count == b.Meshes.Count, "Surface count changed.");
            for (var i = 0; i < a.Meshes.Count; i++)
            {
                Check(a.Meshes[i].Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Meshes[i].Position.Select(BitConverter.SingleToInt32Bits)), "Vertex bytes changed.");
                Check(a.Meshes[i].Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Meshes[i].Normal.Select(BitConverter.SingleToInt32Bits)), "Normal bytes changed.");
                Check(a.Meshes[i].Index.SequenceEqual(b.Meshes[i].Index), "Index order changed.");
            }
        }
    }

    [TestCase]
    public void FramesKeepNoShadowAndLeaveFarLevelsToTheBuilding()
    {
        var p = new WindowParams(1.8f, 1.6f, 0.07f, 2, 2);
        var part = new CasementWindowPart();
        var near = part.Lods[LodLevel.L0](p);
        Check(near.Meshes.All(m => m.Flags!.All(f => f == 8)), "Window frames must not cast shadow.");
        Check(near.Meshes.All(m => m.Ao!.All(a => a == 1f)), "Open faces must write AO visibility 1.");
        Check(near.Meshes.Any(m => m.Material.Spec == OpeningStyle.Gasket), "L0 lacks the gasket line.");
        Check(part.Lods[LodLevel.L1](p).Meshes.Sum(m => m.Index.Length) < near.Meshes.Sum(m => m.Index.Length), "L1 is not simpler than L0.");
        Check(part.Lods[LodLevel.L2](p).Meshes.Count == 0, "Frames stay out of L2.");
        var own = new CasementWindowPart().Lods[LodLevel.L0](p with { Material = OpeningStyle.Spec("wall.greyWhite") });
        Check(own.Meshes.Any(m => m.Material.Spec.Lit.Value == "wall.greyWhite.lit"), "An explicit material was ignored.");
    }

    [TestCase]
    public void KernelVisibilityPassesThroughUnchanged()
    {
        // Wood door at L0: the vision hole is cut by the kernel's slab, whose reveal faces carry visibility 0.8.
        var door = new WoodDoorPart().Lods[LodLevel.L0](new DoorParams(0.9f, 2.1f, 0.2f));
        var leaf = door.Meshes.Single(m => m.Material.Spec.Lit.Value == "door.wood.lit");
        var values = leaf.Ao!.Distinct().OrderBy(v => v).ToArray();
        Check(values.SequenceEqual([0.8f, 1f]), "Kernel visibility must pass through: reveals 0.8, faces 1; got " + string.Join(", ", values));
        foreach (var sample in OpeningGallery.Samples())
            foreach (var m in OpeningGallery.Build(sample.Part, sample.Params, LodLevel.L0).Meshes)
                Check(m.Ao!.All(v => v >= 0.45f), $"{sample.Id}: visibility below the reveal floor.");
    }

    [TestCase]
    public void DoorGlassUsesTheWindowCardEncoding()
    {
        var door = new EntranceDoorPart().Lods[LodLevel.L0](new DoorParams(1.8f, 2.7f, 0.12f, 0f, 2));
        var glass = door.Meshes.Single(m => m.Material.Spec.Kind == ToonKind.Glass);
        Check(glass.Uv2 is not null && glass.Reserved is not null, "Door glass lacks UV2 or the card byte.");
        for (var i = 0; i < glass.Reserved!.Length; i++)
        {
            Check(glass.Reserved[i] == OpeningStyle.DoorCard && glass.Uv2![2 * i] == 0 && glass.Uv2[2 * i + 1] == 32, "Door glass encoding changed.");
            Check(glass.Uv![2 * i] >= 0 && glass.Uv[2 * i] < 1 && glass.Uv[2 * i + 1] >= 0 && glass.Uv[2 * i + 1] < 1, "Door glass leaves its pane cell.");
        }
        var pane = OpeningGlass.Opening(1.8f, 1.6f, 1000, 7, 3, 24, LightChannelCatalog.Id(2), 8);
        Check(pane.Index.Length == 6 && pane.Reserved!.All(b => b == 8), "Opening glass is one card quad.");
        for (var i = 0; i < pane.Position.Length / 3; i++)
        {
            Check((int)Math.Floor(pane.Uv![2 * i]) == 7 && (int)Math.Floor(pane.Uv[2 * i + 1]) == 3, "Window cell differs from WP-14's.");
            Check(pane.Uv2![2 * i] == 1000 && pane.Uv2[2 * i + 1] == 2 + 32 * 24, "UV2 differs from WP-14's.");
            Check(pane.Normal[3 * i + 2] == -1, "Glass must face the exterior (−Z).");
        }
        var grid = OpeningGlass.Grid(3.6f, 3.2f, 3, 2, 0, 4, 1, 24, LightChannelCatalog.Id(0), 3);
        Check(grid.Index.Length == 36, "A 3 × 2 grid has six lights.");
        Throws(() => OpeningGlass.Opening(1, 1, 0, 0, 0, 1, new ChannelId("window.unknown"), 1));
        var frame = OpeningPlacement.Frame(new(10, 3, 0), new(0, 0, -1));
        var decl = OpeningGlass.Declare(frame, 1.8f, 1.6f, 1000, 7, 3, 24, LightChannelCatalog.Id(2));
        Check(decl.Id == 1000 + 3 * 24 + 7 && decl.Center == new Vector3(10, 3.8f, 0), "The declaration must match the glass numbering.");
        Check(Vector3.Cross(decl.Right, decl.Up) == new Vector3(0, 0, -1), "Right × Up must point outdoors.");
    }

    [TestCase]
    public void ClosedDoorsAndLoweredShuttersBlockAsDoorClosed()
    {
        var shut = (BoxCollider)new WoodDoorPart().Collision(new DoorParams(0.9f, 2.1f, 0.2f)).Single();
        Check(shut.Tag == SurfaceTag.DoorClosed && shut.Foot == FootMat.WoodDoor && shut.At.M42 == 1.05f, "A shut door is a DoorClosed closure.");
        Check(new EntranceDoorPart().Collision(new DoorParams(1.8f, 2.4f, 0.12f, 90f, 2)).Count == 0, "Open leaves carry no collision.");
        var half = (BoxCollider)new RollerShutterPart().Collision(new DoorParams(3f, 2.8f, 0.15f, 45f)).Single();
        Check(half.Tag == SurfaceTag.DoorClosed && half.Foot == FootMat.Shutter, "A half-lowered shutter blocks the way.");
        Check(new RollerShutterPart().Collision(new DoorParams(3f, 2.8f, 0.15f, 90f)).Count == 0, "A raised shutter leaves the way free.");
        Check(new CasementWindowPart().Collision(new WindowParams()).Count == 0, "Windows collide through their wall.");
    }

    [TestCase]
    public void OpenLeavesSwingOutwardsWithinBounds()
    {
        var p = new DoorParams(1.8f, 2.4f, 0.12f, 90f, 2);
        var part = new EntranceDoorPart();
        var geometry = part.Lods[LodLevel.L0](p);
        var minZ = geometry.Meshes.SelectMany(m => Enumerable.Range(0, m.Position.Length / 3).Select(i => m.Position[3 * i + 2])).Min();
        Check(minZ < -0.7f, "Open leaves must swing out towards −Z.");
        Closed(geometry, "open door");
    }

    [TestCase]
    public void CurtainChoiceFollowsTheDormitoryRatio()
    {
        int none = 0, open = 0, half = 0, drawn = 0, cream = 0;
        for (var i = 0; i < 20000; i++)
        {
            var c = OpeningPlacement.Curtain(7u, i, 1.6f, 1.4f);
            if (c is null) { none++; continue; }
            if (c.Material.Lit.Value == "curtain.cream.lit") cream++;
            Check(OpeningStyle.CurtainColours.Any(k => k.Family + ".lit" == c.Material.Lit.Value), "Curtain colour outside the list.");
            switch (c.Variation) { case 0: open++; break; case 1: half++; break; default: drawn++; break; }
            Check(c.Material.Steps == ShadeSteps.TwoHalf, "Curtains keep soft-goods shading.");
        }
        Near(0.4, none / 20000.0, 0.02);
        var hung = 20000.0 - none;
        Near(0.3, open / hung, 0.03); Near(0.4, half / hung, 0.03); Near(0.3, drawn / hung, 0.03);
        Near(0.6, cream / hung, 0.03);
        Check(OpeningPlacement.Curtain(7u, 42, 1.6f, 1.4f) == OpeningPlacement.Curtain(7u, 42, 1.6f, 1.4f), "Curtain choice must be stable.");
    }

    [TestCase]
    public void PlacementFrameIsAProperRotation()
    {
        var m = OpeningPlacement.Frame(new(5, 1, 2), new(-3, 0, 0));
        var x = new Vector3(m.M11, m.M12, m.M13); var y = new Vector3(m.M21, m.M22, m.M23); var z = new Vector3(m.M31, m.M32, m.M33);
        Near(1, Vector3.Dot(Vector3.Cross(x, y), z), 0.01);
        Near(1, y.Y, 0.01);
        Check(z == new Vector3(1, 0, 0) && m.M41 == 5, "Exterior must map to the part's −Z.");
        Check(OpeningPlacement.Frame(Vector3.Zero, new(0, 0, -1)) == Matrix4x4.Identity, "A facade facing −Z is the part frame itself.");
        Throws(() => OpeningPlacement.Frame(Vector3.Zero, new(0, 1, 0)));
        Throws(() => OpeningPlacement.Frame(Vector3.Zero, Vector3.Zero));
    }

    [TestCase]
    public void InvalidParametersFailClosed()
    {
        Throws(() => new CasementWindowPart().Lods[LodLevel.L0](new WindowParams(0.4f, 0.4f, 0.07f, 6, 1)));
        Throws(() => new EntranceDoorPart().Bounds(new DoorParams(1.8f, 2.4f, 0.12f, 200f)));
        Throws(() => new EntranceDoorPart().Bounds(new DoorParams(0.6f, 2.4f, 0.12f, 0f, 4)));
        Throws(() => new CurtainPart().Bounds(new PanelParams(1.8f, 2.2f, 0.06f) { Variation = 3 }));
        Throws(() => new RollerShutterPart().Bounds(new DoorParams(-1f, 2.8f, 0.15f)));
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
                Check(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0, $"{what}: winding disagrees with the normals.");
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            foreach (var (edge, count) in edges)
                Check(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1, $"{what}: open or non-manifold edge {edge}.");
            for (var i = 0; i < m.Position.Length / 3; i++)
            {
                Check(geometry.Bounds.Contains(P(m, i)), $"{what}: vertex {P(m, i)} outside {geometry.Bounds}.");
                Near(1, N(m, i).Length(), 0.01);
            }
        }
    }

    private static void Near(double expected, double actual, double tolerance) => Check(Math.Abs(expected - actual) <= tolerance, $"Expected {expected}; got {actual}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Throws(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid input was accepted.");
    }
}

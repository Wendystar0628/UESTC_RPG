using System.Numerics;
using System.Security.Cryptography;
using Campus.Contracts;
using Campus.Contracts.Parts;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Parts.Core.Tests;

[TestSuite]
public sealed class AoContractTests
{
    private static readonly ToonSpec Material = new(new("wall.lit"), new("wall.shade"));

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void SameInputBuildsTheSameGeometryBytes()
    {
        // Counts, positions, normals, indices, UVs and bounds of every example are bitwise the same on a second build.
        var examples = Examples();
        var again = Examples();
        for (var i = 0; i < examples.Count; i++)
            AssertThat(Fingerprint(examples[i].Geometry) != Fingerprint(again[i].Geometry))
                .OverrideFailureMessage($"{examples[i].Id}: a second build gave different geometry bytes.").IsFalse();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void UnoccludedVerticesUseVisibilityOneAcrossProductionPaths()
    {
        foreach (var example in Examples())
        {
            ValidateVisibility(example.Geometry);
            if (example.Id is not ("window-opening" or "courtyard-extrusion")) AssertUnoccluded(example.Geometry);
        }
        AssertPartUnoccluded(new BoxPart(), new BoxParams());
        AssertPartUnoccluded(new ColumnPart(), new ColumnParams());
        AssertPartUnoccluded(new WallPanelPart(), new PanelParams());
        AssertPartUnoccluded(new SlabPart(), new PanelParams(3.6f, 0.2f, 3.6f));
        AssertPartUnoccluded(new TrimPart(), new TrimParams());
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OnlyOpeningRevealsReduceVisibility()
    {
        Vector2[] outer = [new(-2, 0), new(2, 0), new(2, 3), new(-2, 3)];
        Vector2[] hole = [new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, 2), new(-0.5f, 2)];
        var wall = new PolygonRings(outer, [hole]);
        (PartGeometry Geometry, Vector3 CapNormal)[] examples =
        [
            (PartGeometryKernel.WallPanel(4, 3, 0.4f, [hole], Material), Vector3.UnitZ),
            (PartGeometryKernel.WallPanel(wall, 0.4f, Material), Vector3.UnitZ),
            (PartGeometryKernel.Extrude(wall, 2, Material), Vector3.UnitY),
        ];
        foreach (var (geometry, capNormal) in examples)
        {
            ValidateVisibility(geometry);
            var mesh = geometry.Meshes.Single();
            var ao = mesh.Ao!;
            AssertThat(ao.Count(v => v == 0.8f) != 16 || ao.Count(v => v == 1) != ao.Length - 16)
                .OverrideFailureMessage("Only the four reveal quads should have 0.8 visibility; every other vertex must have 1.").IsFalse();
            for (var i = 0; i < ao.Length; i++)
            {
                var normal = new Vector3(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]);
                AssertThat((normal == capNormal || normal == -capNormal) && ao[i] != 1)
                    .OverrideFailureMessage("An unoccluded principal face has reduced visibility.").IsFalse();
            }
        }
    }

    private static void AssertPartUnoccluded<T>(IPartDef<T> part, T parameters) where T : notnull
    {
        foreach (var level in Enum.GetValues<LodLevel>()) AssertUnoccluded(part.Lods[level](parameters));
    }
    private static void AssertUnoccluded(PartGeometry geometry)
    {
        ValidateVisibility(geometry);
        AssertThat(geometry.Meshes.Any(mesh => mesh.Ao!.Any(value => value != 1)))
            .OverrideFailureMessage("Unoccluded geometry must write visibility 1 directly.").IsFalse();
    }
    private static void ValidateVisibility(PartGeometry geometry)
    {
        foreach (var mesh in geometry.Meshes)
        {
            mesh.ValidateVertexData();
            AssertThat(mesh.Ao is null || mesh.Ao.Any(value => !float.IsFinite(value) || value < 0 || value > 1))
                .OverrideFailureMessage("Core geometry needs explicit finite visibility in [0, 1].").IsFalse();
        }
    }

    private static IReadOnlyList<GalleryExample> Examples()
    {
        var examples = PartGallery.CreateExamples().ToList();
        Vector2[] outer = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)];
        Vector2[] hole = [new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)];
        examples.Add(new("box-without-bevel", "", PartGeometryKernel.BeveledBox(2, 3, 4, 0, Material)));
        examples.Add(new("wall-without-opening", "", PartGeometryKernel.WallPanel(4, 3, 0.4f, [], Material)));
        examples.Add(new("courtyard-extrusion", "", PartGeometryKernel.Extrude(new(outer, [hole]), 4, Material)));
        examples.Add(new("closed-sweep", "", PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.4f, 0.3f, 0.04f), outer, Material, closed: true)));
        examples.Add(new("sweep-without-bevel", "", PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.4f, 0.3f, 0), [new(0, 0), new(3, 0)], Material)));
        return examples;
    }

    private static string Fingerprint(PartGeometry geometry)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(geometry.Meshes.Count);
        foreach (var mesh in geometry.Meshes)
        {
            WriteFloats(writer, mesh.Position); WriteFloats(writer, mesh.Normal);
            writer.Write(mesh.Index.Length); foreach (var index in mesh.Index) writer.Write(index);
            WriteFloats(writer, mesh.Uv); WriteFloats(writer, mesh.Uv2);
        }
        WriteFloats(writer, [geometry.Bounds.Min.X, geometry.Bounds.Min.Y, geometry.Bounds.Min.Z, geometry.Bounds.Max.X, geometry.Bounds.Max.Y, geometry.Bounds.Max.Z]);
        writer.Flush();
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    private static void WriteFloats(BinaryWriter writer, float[]? values)
    {
        writer.Write(values?.Length ?? -1);
        if (values is not null) foreach (var value in values) writer.Write(BitConverter.SingleToInt32Bits(value));
    }
}

using System.Numerics;
using System.Security.Cryptography;
using Campus.Contracts;
using Campus.Contracts.Parts;
using GdUnit4;

namespace Campus.Gen.Parts.Core.Tests;

[TestSuite]
public sealed class AoContractTests
{
    private static readonly ToonSpec Material = new(new("wall.lit"), new("wall.shade"));

    [TestCase]
    public void GeometryMatchesThePreContract20AoFix()
    {
        // Captured from the unchanged production core at main 153ebf0 (which includes 3af35c3).
        // Excludes AO only: counts, positions, normals, indices, UVs and bounds must remain bitwise unchanged.
        string[] expected =
        [
            "679E55CDCA115EB54966B2AF5E33696748D911BAFF96C7569CD26F7D414FB7EA",
            "403EEACBCD39FDC0132DE74A6E0739471C6B7F364A662634B4E90D6E81397780",
            "F82125715B8593B34D5DFF13EDD3F7B64E4A5BDE915CCA0BCE8F2AE9F6BB2E2F",
            "D041C26F156CC2A9399D167D7E1BD5FC424361C67F679A38A6C6723D6F6B5963",
            "0A902FC0A83E668197536A3519744158120934411237CC1343BBA2E5E2382403",
            "A19E8DEC69289174F726D082C49D8CF9ADCA468943DFA972BB8D2C5865D867B7",
            "619C86874706797F3AAAC9527B6344A1DCC58A503CE8A08F62BD4C53CE5C7FFC",
            "FAF2A164EDA67A05223333BE7F433AE93D34AEED132CD59F21C4E50C30C343C2",
            "0B8E4FF5239128E91C8FB93BD5F256ADA5FE927E24CF48E4C89AEA36FDCAF4FC",
            "AE081F8507647CB755254A3D6781171942DC2F23C0AB94210C446121993BB716",
        ];
        var examples = Examples();
        var actual = examples.Select(example => Fingerprint(example.Geometry)).ToArray();
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException("Geometry baseline mismatch:\n" + string.Join("\n", examples.Select((example, i) => example.Id + " " + actual[i])));
    }

    [TestCase]
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

    [TestCase]
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
            if (ao.Count(v => v == 0.8f) != 16 || ao.Count(v => v == 1) != ao.Length - 16)
                throw new InvalidOperationException("Only the four reveal quads should have 0.8 visibility; every other vertex must have 1.");
            for (var i = 0; i < ao.Length; i++)
            {
                var normal = new Vector3(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]);
                if ((normal == capNormal || normal == -capNormal) && ao[i] != 1)
                    throw new InvalidOperationException("An unoccluded principal face has reduced visibility.");
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
        if (geometry.Meshes.Any(mesh => mesh.Ao!.Any(value => value != 1)))
            throw new InvalidOperationException("Unoccluded geometry must write visibility 1 directly.");
    }
    private static void ValidateVisibility(PartGeometry geometry)
    {
        foreach (var mesh in geometry.Meshes)
        {
            mesh.ValidateVertexData();
            if (mesh.Ao is null || mesh.Ao.Any(value => !float.IsFinite(value) || value < 0 || value > 1))
                throw new InvalidOperationException("Core geometry needs explicit finite visibility in [0, 1].");
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

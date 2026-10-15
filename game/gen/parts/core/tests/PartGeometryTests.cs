using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;

namespace Campus.Gen.Parts.Core.Tests;

[TestSuite]
public sealed class PartGeometryTests
{
    private static readonly ToonSpec Material = new(new("wall.lit"), new("wall.shade"));

    [TestCase]
    public void EarcutPreservesConcaveOutlineAndTwoHoles()
    {
        Vector2[] outer = [new(0, 0), new(8, 0), new(8, 6), new(5, 6), new(5, 4), new(0, 4)];
        Vector2[] a = [new(1, 1), new(2, 1), new(2, 2), new(1, 2)];
        Vector2[] b = [new(6, 1), new(7, 1), new(7, 3), new(6, 3)];
        var polygon = new PolygonRings(outer, [a, b]);
        var result = PolygonTriangulator.Triangulate(polygon);
        Near(35, Area(result), 1e-6);
        foreach (var hole in new[] { a, b })
            for (var i = 0; i < result.Indices.Length; i += 3)
            {
                var centre = (result.Vertices[result.Indices[i]] + result.Vertices[result.Indices[i + 1]] + result.Vertices[result.Indices[i + 2]]) / 3;
                Check(!(centre.X > hole[0].X && centre.X < hole[2].X && centre.Y > hole[0].Y && centre.Y < hole[2].Y), "Triangle fills an opening.");
            }
        Check(result.Indices.SequenceEqual(PolygonTriangulator.Triangulate(polygon).Indices), "Triangulation order changed.");
        Array.Reverse(outer); Array.Reverse(a);
        Near(35, Area(PolygonTriangulator.Triangulate(polygon)), 1e-6);
    }

    [TestCase]
    public void EarcutNormalizesClosedAndCollinearRings()
    {
        var p = PolygonTriangulator.Triangulate(new([new(0, 0), new(1, 0), new(2, 0), new(2, 2), new(0, 2), new(0, 0)]));
        Check(p.Vertices.Length == 4 && p.Indices.Length == 6, "Ring cleanup failed.");
        Near(4, Area(p), 1e-6);
    }

    [TestCase]
    public void InvalidTopologyNeverReturnsPartialGeometry()
    {
        Throws(() => PolygonTriangulator.Triangulate(new([new(0, 0), new(2, 2), new(0, 2), new(2, 0)])));
        Vector2[] shell = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        Throws(() => PolygonTriangulator.Triangulate(new(shell, [[new(5, 1), new(6, 1), new(6, 2), new(5, 2)]])));
        Throws(() => PolygonTriangulator.Triangulate(new(shell, [[new(0, 1), new(1, 1), new(1, 2), new(0, 2)]])));
        Throws(() => PolygonTriangulator.Triangulate(new(shell, [[new(1, 1), new(3, 1), new(3, 3), new(1, 3)], [new(2, 2), new(3.5f, 2), new(3.5f, 3.5f), new(2, 3.5f)]])));
        Throws(() => PolygonTriangulator.Triangulate(new([new(float.NaN, 0), new(1, 0), new(0, 1)])));
    }

    [TestCase]
    public void BeveledBoxHasDocumentedTopologyAndFlatPrincipalFaces()
    {
        var geometry = PartGeometryKernel.BeveledBox(2, 3, 4, 0.1f, Material);
        var mesh = geometry.Meshes.Single();
        Check(mesh.Position.Length / 3 == 24 && mesh.Index.Length / 3 == 44, "Beveled box topology changed.");
        Check(geometry.Bounds == new Box3(new(-1, 0, -2), new(1, 3, 2)), "Box bounds changed.");
        for (var face = 0; face < 6; face++)
            for (var vertex = 1; vertex < 4; vertex++)
                Check(N(mesh, face * 4) == N(mesh, face * 4 + vertex), "Principal face is not exactly flat.");
        ClosedClockwise(geometry);
    }

    [TestCase]
    public void OpeningsHaveRevealsAndRemainHolesOnBothSides()
    {
        Vector2[] opening = [new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, 2), new(-0.5f, 2)];
        var geometry = PartGeometryKernel.WallPanel(4, 3, 0.4f, [opening], Material);
        var mesh = geometry.Meshes.Single(); double frontArea = 0;
        for (var i = 0; i < mesh.Index.Length; i += 3)
        {
            var a = mesh.Index[i]; var b = mesh.Index[i + 1]; var c = mesh.Index[i + 2];
            if (N(mesh, a) != -Vector3.UnitZ) continue;
            frontArea += Vector3.Cross(P(mesh, b) - P(mesh, a), P(mesh, c) - P(mesh, a)).Length() / 2;
        }
        Near(10.5, frontArea, 1e-6);
        Check(mesh.Ao!.Any(v => v > 0), "Reveals have no local AO.");
        ClosedClockwise(geometry);
        // A door is represented by a notch that connects to the bottom boundary.
        ClosedClockwise(PartGeometryKernel.WallPanel(new([new(-2, 0), new(-0.6f, 0), new(-0.6f, 2.1f), new(0.6f, 2.1f), new(0.6f, 0), new(2, 0), new(2, 3), new(-2, 3)]), 0.3f, Material));
    }

    [TestCase]
    public void ConcaveAndCourtyardExtrusionsAreClosed()
    {
        ClosedClockwise(PartGeometryKernel.Extrude(new([new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)], [[new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)]]), 4, Material));
        ClosedClockwise(PartGeometryKernel.Extrude(new([new(0, 0), new(3, 0), new(3, 1), new(1, 1), new(1, 3), new(0, 3)]), 2, Material));
        ClosedClockwise(PartGeometryKernel.BeveledExtrude([new(-2, -1), new(2, -1), new(2, 1), new(-2, 1)], 1, 0.1f, Material));
    }

    [TestCase]
    public void MiterSweepsCloseBothOpenAndLoopPaths()
    {
        var profile = PartGeometryKernel.BeveledRectangleProfile(0.4f, 0.3f, 0.04f);
        ClosedClockwise(PartGeometryKernel.Sweep(profile, [new(0, 0), new(3, 0), new(3, 3)], Material));
        ClosedClockwise(PartGeometryKernel.Sweep(profile, [new(0, 0), new(3, 0), new(3, 3), new(0, 3)], Material, closed: true));
        Throws(() => PartGeometryKernel.Sweep(profile, [new(0, 0), new(1, 0), new(0, 0)], Material));
        Throws(() => PartGeometryKernel.Sweep(profile, [new(0, 0), new(0, 0)], Material));
        Throws(() => PartGeometryKernel.BeveledExtrude([new(-0.1f, -1), new(0.1f, -1), new(0.1f, 1), new(-0.1f, 1)], 2, 0.2f, Material));
    }

    [TestCase]
    public void GalleryGenerationIsBitwiseRepeatable()
    {
        var first = PartGallery.CreateExamples(); var second = PartGallery.CreateExamples();
        Check(first.Count == 5 && second.Count == first.Count, "Missing gallery examples.");
        for (var i = 0; i < first.Count; i++)
        {
            ClosedClockwise(first[i].Geometry);
            var a = first[i].Geometry.Meshes.Single(); var b = second[i].Geometry.Meshes.Single();
            Check(a.Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Position.Select(BitConverter.SingleToInt32Bits)), "Vertex bytes changed.");
            Check(a.Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Normal.Select(BitConverter.SingleToInt32Bits)), "Normal bytes changed.");
            Check(a.Index.SequenceEqual(b.Index), "Index bytes changed.");
        }
    }

    [TestCase]
    public void ArraysPreserveSourceMetadataAndSocketsAreOrthonormal()
    {
        var source = new PlacedPart(new(PartCatalog.Column), new ColumnParams(), Matrix4x4.CreateTranslation(2, 0, 0), new("wall.lit")) { IgnoreOcclusionCulling = true };
        var line = PartPlacement.Linear(source, 4, new(1, 0, 0));
        Check(line[3].At.M41 == 5 && line[3].Tint == source.Tint && line[3].Params == source.Params && line[3].IgnoreOcclusionCulling, "Array lost placement metadata.");
        Check(source.At.M41 == 2, "Array mutated its source.");
        var radial = PartPlacement.Radial(source, 4, Vector3.Zero, (float)(Math.PI / 2));
        Near(-2, radial[1].At.M43, 1e-5); Near(-2, radial[2].At.M41, 1e-5);
        foreach (var socket in PartPlacement.BoxSockets(2, 3, 4).Values)
        {
            var x = new Vector3(socket.M11, socket.M12, socket.M13); var y = new Vector3(socket.M21, socket.M22, socket.M23); var z = new Vector3(socket.M31, socket.M32, socket.M33);
            Near(1, x.Length(), 1e-6); Near(1, y.Length(), 1e-6); Near(1, z.Length(), 1e-6); Near(0, Vector3.Dot(x, y), 1e-6);
        }
        Throws(() => PartPlacement.Socket(Vector3.Zero, Vector3.UnitY, Vector3.UnitY));
    }

    [TestCase]
    public void RegisteredPartsRespectLodBudgetBoundsAndCollisionFoot()
    {
        var part = new BoxPart(); var p = new BoxParams(2, 3, 4) { Foot = FootMat.Metal };
        foreach (var level in Enum.GetValues<LodLevel>())
        {
            var geometry = part.Lods[level](p); ClosedClockwise(geometry);
            Check(geometry.Bounds == part.Bounds(p), "LOD moved the bounds.");
            Check(geometry.Meshes.Sum(m => m.Index.Length / 3) <= (level == LodLevel.L0 ? part.Budget.L0 : part.Budget.L1), "Part exceeds its declared budget.");
        }
        var collider = (BoxCollider)part.Collision(p).Single();
        Check(collider.Foot == p.Foot && collider.At.M42 == 1.5f && collider.Half == new Vector3(1, 1.5f, 2), "Collision differs from the part parameters.");
        ClosedClockwise(new TrimPart().Lods[LodLevel.L0](new TrimParams()));
    }

    private static double Area(TriangulatedPolygon p)
    {
        double area = 0;
        for (var i = 0; i < p.Indices.Length; i += 3)
        {
            var a = p.Vertices[p.Indices[i]]; var b = p.Vertices[p.Indices[i + 1]]; var c = p.Vertices[p.Indices[i + 2]];
            area += ((double)b.X - a.X) * (c.Y - a.Y) / 2 - ((double)b.Y - a.Y) * (c.X - a.X) / 2;
        }
        return area;
    }
    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);
    private static void ClosedClockwise(PartGeometry geometry)
    {
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                var a = m.Index[i]; var b = m.Index[i + 1]; var c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                Check(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0, "Face winding disagrees with outward normals.");
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            foreach (var (edge, count) in edges)
                Check(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1, "A solid has an open or non-manifold edge.");
            for (var i = 0; i < m.Position.Length / 3; i++)
            { Check(geometry.Bounds.Contains(P(m, i)), "Bounds miss a vertex."); Near(1, N(m, i).Length(), 1e-5); }
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

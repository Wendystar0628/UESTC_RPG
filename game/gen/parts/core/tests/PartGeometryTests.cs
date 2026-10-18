using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Parts.Core.Tests;

[TestSuite]
public sealed class PartGeometryTests
{
    private static readonly ToonSpec Material = new(new("wall.lit"), new("wall.shade"));

    [TestCase, TestCategory(TestReason.Contract)]
    public void EarcutPreservesConcaveOutlineAndTwoHoles()
    {
        Vector2[] outer = [new(0, 0), new(8, 0), new(8, 6), new(5, 6), new(5, 4), new(0, 4)];
        Vector2[] a = [new(1, 1), new(2, 1), new(2, 2), new(1, 2)];
        Vector2[] b = [new(6, 1), new(7, 1), new(7, 3), new(6, 3)];
        var polygon = new PolygonRings(outer, [a, b]);
        var result = PolygonTriangulator.Triangulate(polygon);
        AssertThat(Math.Abs((double)(35) - Area(result)) <= TestTolerance.Position).OverrideFailureMessage($"Expected {35}; got {Area(result)}.").IsTrue();
        foreach (var hole in new[] { a, b })
            for (var i = 0; i < result.Indices.Length; i += 3)
            {
                var centre = (result.Vertices[result.Indices[i]] + result.Vertices[result.Indices[i + 1]] + result.Vertices[result.Indices[i + 2]]) / 3;
                AssertThat(!(centre.X > hole[0].X && centre.X < hole[2].X && centre.Y > hole[0].Y && centre.Y < hole[2].Y)).OverrideFailureMessage("Triangle fills an opening.").IsTrue();
            }
        AssertThat(result.Indices.SequenceEqual(PolygonTriangulator.Triangulate(polygon).Indices)).OverrideFailureMessage("Triangulation order changed.").IsTrue();
        Array.Reverse(outer); Array.Reverse(a);
        AssertThat(Math.Abs((double)(35) - Area(PolygonTriangulator.Triangulate(polygon))) <= TestTolerance.Position).OverrideFailureMessage($"Expected {35}; got {Area(PolygonTriangulator.Triangulate(polygon))}.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EarcutNormalizesClosedAndCollinearRings()
    {
        var p = PolygonTriangulator.Triangulate(new([new(0, 0), new(1, 0), new(2, 0), new(2, 2), new(0, 2), new(0, 0)]));
        AssertThat(p.Vertices.Length == 4 && p.Indices.Length == 6).OverrideFailureMessage("Ring cleanup failed.").IsTrue();
        AssertThat(Math.Abs((double)(4) - Area(p)) <= TestTolerance.Position).OverrideFailureMessage($"Expected {4}; got {Area(p)}.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void InvalidTopologyNeverReturnsPartialGeometry()
    {
        AssertThrown(() => PolygonTriangulator.Triangulate(new([new(0, 0), new(2, 2), new(0, 2), new(2, 0)]))).IsInstanceOf<ArgumentException>();
        Vector2[] shell = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        AssertThrown(() => PolygonTriangulator.Triangulate(new(shell, [[new(5, 1), new(6, 1), new(6, 2), new(5, 2)]]))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PolygonTriangulator.Triangulate(new(shell, [[new(0, 1), new(1, 1), new(1, 2), new(0, 2)]]))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PolygonTriangulator.Triangulate(new(shell, [[new(1, 1), new(3, 1), new(3, 3), new(1, 3)], [new(2, 2), new(3.5f, 2), new(3.5f, 3.5f), new(2, 3.5f)]]))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PolygonTriangulator.Triangulate(new([new(float.NaN, 0), new(1, 0), new(0, 1)]))).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void BeveledBoxHasDocumentedTopologyAndFlatPrincipalFaces()
    {
        var geometry = PartGeometryKernel.BeveledBox(2, 3, 4, 0.1f, Material);
        var mesh = geometry.Meshes.Single();
        AssertThat(mesh.Position.Length / 3 == 24 && mesh.Index.Length / 3 == 44).OverrideFailureMessage("Beveled box topology changed.").IsTrue();
        AssertThat(geometry.Bounds == new Box3(new(-1, 0, -2), new(1, 3, 2))).OverrideFailureMessage("Box bounds changed.").IsTrue();
        for (var face = 0; face < 6; face++)
            for (var vertex = 1; vertex < 4; vertex++)
                AssertThat(N(mesh, face * 4) == N(mesh, face * 4 + vertex)).OverrideFailureMessage("Principal face is not exactly flat.").IsTrue();
        ClosedClockwise(geometry);
    }

    [TestCase, TestCategory(TestReason.Contract)]
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
        AssertThat(Math.Abs((double)(10.5) - frontArea) <= TestTolerance.Position).OverrideFailureMessage($"Expected {10.5}; got {frontArea}.").IsTrue();
        AssertThat(mesh.Ao!.Any(v => v < 1)).OverrideFailureMessage("Reveals have no local occlusion.").IsTrue();
        ClosedClockwise(geometry);
        // A door is represented by a notch that connects to the bottom boundary.
        ClosedClockwise(PartGeometryKernel.WallPanel(new([new(-2, 0), new(-0.6f, 0), new(-0.6f, 2.1f), new(0.6f, 2.1f), new(0.6f, 0), new(2, 0), new(2, 3), new(-2, 3)]), 0.3f, Material));
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ConcaveAndCourtyardExtrusionsAreClosed()
    {
        ClosedClockwise(PartGeometryKernel.Extrude(new([new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)], [[new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)]]), 4, Material));
        ClosedClockwise(PartGeometryKernel.Extrude(new([new(0, 0), new(3, 0), new(3, 1), new(1, 1), new(1, 3), new(0, 3)]), 2, Material));
        ClosedClockwise(PartGeometryKernel.BeveledExtrude([new(-2, -1), new(2, -1), new(2, 1), new(-2, 1)], 1, 0.1f, Material));
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MiterSweepsCloseBothOpenAndLoopPaths()
    {
        var profile = PartGeometryKernel.BeveledRectangleProfile(0.4f, 0.3f, 0.04f);
        ClosedClockwise(PartGeometryKernel.Sweep(profile, [new(0, 0), new(3, 0), new(3, 3)], Material));
        ClosedClockwise(PartGeometryKernel.Sweep(profile, [new(0, 0), new(3, 0), new(3, 3), new(0, 3)], Material, closed: true));
        AssertThrown(() => PartGeometryKernel.Sweep(profile, [new(0, 0), new(1, 0), new(0, 0)], Material)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PartGeometryKernel.Sweep(profile, [new(0, 0), new(0, 0)], Material)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PartGeometryKernel.BeveledExtrude([new(-0.1f, -1), new(0.1f, -1), new(0.1f, 1), new(-0.1f, 1)], 2, 0.2f, Material)).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void GalleryGenerationIsBitwiseRepeatable()
    {
        var first = PartGallery.CreateExamples(); var second = PartGallery.CreateExamples();
        AssertThat(first.Count > 0 && second.Count == first.Count).OverrideFailureMessage("Missing gallery examples.").IsTrue();
        for (var i = 0; i < first.Count; i++)
        {
            ClosedClockwise(first[i].Geometry);
            var a = first[i].Geometry.Meshes.Single(); var b = second[i].Geometry.Meshes.Single();
            AssertThat(a.Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Position.Select(BitConverter.SingleToInt32Bits))).OverrideFailureMessage("Vertex bytes changed.").IsTrue();
            AssertThat(a.Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Normal.Select(BitConverter.SingleToInt32Bits))).OverrideFailureMessage("Normal bytes changed.").IsTrue();
            AssertThat(a.Index.SequenceEqual(b.Index)).OverrideFailureMessage("Index bytes changed.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ArraysPreserveSourceMetadataAndSocketsAreOrthonormal()
    {
        var source = new PlacedPart(new(PartCatalog.Column), new ColumnParams(), Matrix4x4.CreateTranslation(2, 0, 0), new("wall.lit")) { IgnoreOcclusionCulling = true };
        var line = PartPlacement.Linear(source, 4, new(1, 0, 0));
        AssertThat(line[3].At.M41 == 5 && line[3].Tint == source.Tint && line[3].Params == source.Params && line[3].IgnoreOcclusionCulling).OverrideFailureMessage("Array lost placement metadata.").IsTrue();
        AssertThat(source.At.M41 == 2).OverrideFailureMessage("Array mutated its source.").IsTrue();
        var radial = PartPlacement.Radial(source, 4, Vector3.Zero, (float)(Math.PI / 2));
        AssertThat(Math.Abs((double)(-2) - radial[1].At.M43) <= TestTolerance.Position).OverrideFailureMessage($"Expected {-2}; got {radial[1].At.M43}.").IsTrue(); AssertThat(Math.Abs((double)(-2) - radial[2].At.M41) <= TestTolerance.Position).OverrideFailureMessage($"Expected {-2}; got {radial[2].At.M41}.").IsTrue();
        foreach (var socket in PartPlacement.BoxSockets(2, 3, 4).Values)
        {
            var x = new Vector3(socket.M11, socket.M12, socket.M13); var y = new Vector3(socket.M21, socket.M22, socket.M23); var z = new Vector3(socket.M31, socket.M32, socket.M33);
            AssertThat(Math.Abs((double)(1) - x.Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {x.Length()}.").IsTrue(); AssertThat(Math.Abs((double)(1) - y.Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {y.Length()}.").IsTrue(); AssertThat(Math.Abs((double)(1) - z.Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {z.Length()}.").IsTrue(); AssertThat(Math.Abs((double)(0) - Vector3.Dot(x, y)) <= TestTolerance.Position).OverrideFailureMessage($"Expected {0}; got {Vector3.Dot(x, y)}.").IsTrue();
        }
        AssertThrown(() => PartPlacement.Socket(Vector3.Zero, Vector3.UnitY, Vector3.UnitY)).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RegisteredPartsRespectBoundsAndCollisionFoot()
    {
        var part = new BoxPart(); var p = new BoxParams(2, 3, 4) { Foot = FootMat.Metal };
        foreach (var level in Enum.GetValues<LodLevel>())
        {
            var geometry = part.Lods[level](p); ClosedClockwise(geometry);
            AssertThat(geometry.Bounds == part.Bounds(p)).OverrideFailureMessage("LOD moved the bounds.").IsTrue();
        }
        var collider = (BoxCollider)part.Collision(p).Single();
        AssertThat(collider.Foot == p.Foot && collider.At.M42 == 1.5f && collider.Half == new Vector3(1, 1.5f, 2)).OverrideFailureMessage("Collision differs from the part parameters.").IsTrue();
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
                AssertThat(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0).OverrideFailureMessage("Face winding disagrees with outward normals.").IsTrue();
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            foreach (var (edge, count) in edges)
                AssertThat(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1).OverrideFailureMessage("A solid has an open or non-manifold edge.").IsTrue();
            for (var i = 0; i < m.Position.Length / 3; i++)
            { AssertThat(geometry.Bounds.Contains(P(m, i))).OverrideFailureMessage("Bounds miss a vertex.").IsTrue(); AssertThat(Math.Abs((double)(1) - N(m, i).Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {N(m, i).Length()}.").IsTrue(); }
        }
    }
}

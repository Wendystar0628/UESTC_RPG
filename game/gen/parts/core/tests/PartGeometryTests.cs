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
    public void BeveledBoxHasExpectedBoundsAndFlatPrincipalFaces()
    {
        var geometry = PartSolids.BeveledBox(2, 3, 4, 0.1f, Material);
        var mesh = geometry.Meshes.Single();
        AssertThat(mesh.Position.Length / 3 == 24 && mesh.Index.Length / 3 == 44).OverrideFailureMessage("Beveled box topology changed.").IsTrue();
        AssertThat(geometry.Bounds == new Box3(new(-1, 0, -2), new(1, 3, 2))).OverrideFailureMessage("Box bounds changed.").IsTrue();
        for (var face = 0; face < 6; face++)
            for (var vertex = 1; vertex < 4; vertex++)
                AssertThat(N(mesh, face * 4) == N(mesh, face * 4 + vertex)).OverrideFailureMessage("Principal face is not exactly flat.").IsTrue();
        ValidateVisibleGeometry(geometry);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OpeningsHaveRevealsAndRemainHolesOnBothSides()
    {
        Vector2[] opening = [new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, 2), new(-0.5f, 2)];
        var geometry = PartSolids.WallPanel(4, 3, 0.4f, [opening], Material);
        var mesh = geometry.Meshes.Single(); double frontArea = 0;
        for (var i = 0; i < mesh.Index.Length; i += 3)
        {
            var a = mesh.Index[i]; var b = mesh.Index[i + 1]; var c = mesh.Index[i + 2];
            if (N(mesh, a) != -Vector3.UnitZ) continue;
            frontArea += Vector3.Cross(P(mesh, b) - P(mesh, a), P(mesh, c) - P(mesh, a)).Length() / 2;
        }
        AssertThat(Math.Abs((double)(10.5) - frontArea) <= TestTolerance.Position).OverrideFailureMessage($"Expected {10.5}; got {frontArea}.").IsTrue();
        AssertThat(mesh.Ao!.Any(v => v < 1)).OverrideFailureMessage("Reveals have no local occlusion.").IsTrue();
        ValidateVisibleGeometry(geometry);
        // A door is represented by a notch that connects to the bottom boundary.
        ValidateVisibleGeometry(PartSolids.WallPanel(new([new(-2, 0), new(-0.6f, 0), new(-0.6f, 2.1f), new(0.6f, 2.1f), new(0.6f, 0), new(2, 0), new(2, 3), new(-2, 3)]), 0.3f, Material));
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void InvalidSweepInputsAreRejected()
    {
        var profile = PartSweeps.BeveledRectangleProfile(0.4f, 0.3f, 0.04f);
        ValidateVisibleGeometry(PartSweeps.Sweep(profile, [new(0, 0), new(3, 0)], Material));
        ValidateVisibleGeometry(PartSweeps.Sweep(profile, [new(0, 0), new(3, 0), new(3, 3)], Material));
        ValidateVisibleGeometry(PartSweeps.Sweep(profile, [new(0, 0), new(3, 0), new(3, 3), new(0, 3)], Material, closed: true));
        AssertThrown(() => PartSweeps.Sweep(profile, [new(0, 0), new(1, 0), new(0, 0)], Material)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PartSweeps.Sweep(profile, [new(0, 0), new(0, 0)], Material)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PartSweeps.Sweep(profile, [new(0, 0), new(float.NaN, 1)], Material)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PartSweeps.Sweep(profile, [new(0, 0), new(1, 0)], Material, miterLimit: 0)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PartSolids.BeveledExtrude([new(-0.1f, -1), new(0.1f, -1), new(0.1f, 1), new(-0.1f, 1)], 2, 0.2f, Material)).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void RightAngleSweepKeepsItsOffsetIntersectionAndShortInnerJoinsStayLocal()
    {
        var profile = PartSweeps.BeveledRectangleProfile(0.4f, 0.3f, 0);
        Vector2[] squareTurn = [new(0, 0), new(3, 0), new(3, 3)];
        var ordinary = PartSweeps.Sweep(profile, squareTurn, Material);
        ValidateVisibleGeometry(ordinary);
        var vertices = ordinary.Meshes.SelectMany(mesh => Enumerable.Range(0, mesh.Position.Length / 3).Select(i => P(mesh, i))).ToArray();
        foreach (var p in profile.Points)
        {
            var expected = new Vector3(squareTurn[1].X + p.X, p.Y, squareTurn[1].Y - p.X);
            AssertThat(vertices.Contains(expected)).OverrideFailureMessage("An ordinary right-angle offset intersection moved.").IsTrue();
        }
        Vector2[][] shortPaths = [
            [new(-0.1f, 0), new(0, 0), new(-0.1f, 0.02f)],
            [new(-3, 0), new(0, 0), new(0.1f, 0.1f), new(-3, 0.1f)]];
        var halfWidth = profile.Points.Max(p => Math.Abs(p.X));
        foreach (var path in shortPaths)
            foreach (var reverse in new[] { false, true })
            {
                var line = reverse ? path.Reverse().ToArray() : path;
                var geometry = PartSweeps.Sweep(profile, line, Material);
                // D-290: short/near-reversing joins may overlap; guard their visible extent, not manifold closure.
                foreach (var mesh in geometry.Meshes) mesh.ValidateVertexData();
                var points = geometry.Meshes.SelectMany(mesh => Enumerable.Range(0, mesh.Position.Length / 3).Select(i => P(mesh, i))).ToArray();
                AssertThat(points.Length > 0 && points.All(p => geometry.Bounds.Contains(p)))
                    .OverrideFailureMessage("A complex join has no geometry or leaves its bounds.").IsTrue();
                var reach = halfWidth * PartSweeps.DefaultMiterLimit;
                AssertThat(points.All(p => p.X >= line.Min(q => q.X) - reach - TestTolerance.Position && p.X <= line.Max(q => q.X) + reach + TestTolerance.Position &&
                    p.Z >= line.Min(q => q.Y) - reach - TestTolerance.Position && p.Z <= line.Max(q => q.Y) + reach + TestTolerance.Position))
                    .OverrideFailureMessage("A short inner intersection escaped the neighbouring segments.").IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void SplitRoundedHedgeFootUsesItsFlatEdgeNormal()
    {
        // Motto's rounded clipped section: opposite side normals meet across a flat bottom edge.
        Vector2[] points = [new(-0.5f, 0), new(0.5f, 0), new(0.5f, 0.55f), new(0.32f, 0.92f),
            new(0, 1), new(-0.32f, 0.92f), new(-0.5f, 0.55f)];
        Vector2[] normals = [new(-1, 0), new(1, 0), new(0.94f, 0.34f), new(0.6f, 0.8f),
            new(0, 1), new(-0.6f, 0.8f), new(-0.94f, 0.34f)];
        var profile = new SweepProfile(points, normals.Select(n => n / MathF.Sqrt(n.X * n.X + n.Y * n.Y)).ToArray());
        Vector2[] path = [new(-0.1f, 0), new(0, 0), new(-0.1f, 0.02f)];
        var geometry = PartSweeps.Sweep(profile, path, Material);
        foreach (var mesh in geometry.Meshes) mesh.ValidateVertexData();
        foreach (var p in path)
            AssertThat(geometry.Meshes.Any(mesh => Enumerable.Range(0, mesh.Position.Length / 3)
                .Any(i => P(mesh, i) == new Vector3(p.X, points[0].Y, p.Y) && N(mesh, i) == -Vector3.UnitY)))
                .OverrideFailureMessage("The inserted hedge-foot vertex must face down along its flat edge.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void OverLimitSweepsCutOuterTipsAndKeepSlopingSectionsBounded()
    {
        SweepProfile[] profiles = [PartSweeps.BeveledRectangleProfile(0.4f, 0.3f, 0.04f),
            new([new(-0.3f, 0), new(0.2f, 0.04f), new(0.25f, 0.3f), new(-0.15f, 0.4f)])];
        var rotation = Matrix3x2.CreateRotation(0.37f); var inverse = Matrix3x2.CreateRotation(-0.37f);
        Vector2[] open = [new(-20, 0), new(0, 0), new(-20, 4)];
        Vector2[] loop = [new(0, 0), new(30, 0), new(2, 5)];
        foreach (var profile in profiles)
            foreach (var reverse in new[] { false, true })
            {
                var path = (reverse ? open.Reverse() : open.AsEnumerable()).Select(p => Vector2.Transform(p, rotation)).ToArray();
                var geometry = PartSweeps.Sweep(profile, path, Material);
                // D-290: complex joins may overlap; visible shape, bounds and normals remain checked.
                foreach (var partMesh in geometry.Meshes) partMesh.ValidateVertexData();
                var reach = profile.Points.Max(p => Math.Abs(p.X));
                var mesh = geometry.Meshes.Single();
                for (var i = 0; i < mesh.Position.Length / 3; i++)
                {
                    var p = P(mesh, i); var local = Vector2.Transform(new(p.X, p.Z), inverse);
                    AssertThat(geometry.Bounds.Contains(p)).OverrideFailureMessage("A cut sweep leaves its bounds.").IsTrue();
                    // The outer tip must be cut back to the incident offsets, not a clamped miter spike.
                    AssertThat(local.X <= reach + TestTolerance.Position).OverrideFailureMessage("An over-limit outer tip protrudes beyond the bevel.").IsTrue();
                }
                var ring = (reverse ? loop.Reverse() : loop.AsEnumerable()).Select(p => Vector2.Transform(p, rotation)).ToArray();
                var closed = PartSweeps.Sweep(profile, ring, Material, closed: true);
                var expansion = reach * PartSweeps.DefaultMiterLimit;
                foreach (var ringMesh in closed.Meshes)
                {
                    ringMesh.ValidateVertexData();
                    for (var i = 0; i < ringMesh.Position.Length / 3; i++)
                    {
                        var p = P(ringMesh, i);
                        AssertThat(closed.Bounds.Contains(p) && p.X >= ring.Min(q => q.X) - expansion - TestTolerance.Position &&
                            p.X <= ring.Max(q => q.X) + expansion + TestTolerance.Position && p.Z >= ring.Min(q => q.Y) - expansion - TestTolerance.Position &&
                            p.Z <= ring.Max(q => q.Y) + expansion + TestTolerance.Position).OverrideFailureMessage("A complex closed sweep escapes its path envelope.").IsTrue();
                    }
                }
            }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void GalleryGenerationIsBitwiseRepeatable()
    {
        var first = PartGallery.CreateExamples(); var second = PartGallery.CreateExamples();
        AssertThat(first.Count > 0 && second.Count == first.Count).OverrideFailureMessage("Missing gallery examples.").IsTrue();
        for (var i = 0; i < first.Count; i++)
        {
            ValidateVisibleGeometry(first[i].Geometry);
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
            var geometry = part.Lods[level](p); ValidateVisibleGeometry(geometry);
            AssertThat(geometry.Bounds == part.Bounds(p)).OverrideFailureMessage("LOD moved the bounds.").IsTrue();
        }
        var collider = (BoxCollider)part.Collision(p).Single();
        AssertThat(collider.Foot == p.Foot && collider.At.M42 == 1.5f && collider.Half == new Vector3(1, 1.5f, 2)).OverrideFailureMessage("Collision differs from the part parameters.").IsTrue();
        ValidateVisibleGeometry(new TrimPart().Lods[LodLevel.L0](new TrimParams()));
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
    private static void ValidateVisibleGeometry(PartGeometry geometry)
    {
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                var a = m.Index[i]; var b = m.Index[i + 1]; var c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                AssertThat(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0).OverrideFailureMessage("Face winding disagrees with outward normals.").IsTrue();
            }
            for (var i = 0; i < m.Position.Length / 3; i++)
            { AssertThat(geometry.Bounds.Contains(P(m, i))).OverrideFailureMessage("Bounds miss a vertex.").IsTrue(); AssertThat(Math.Abs((double)(1) - N(m, i).Length()) <= TestTolerance.Position).OverrideFailureMessage($"Expected {1}; got {N(m, i).Length()}.").IsTrue(); }
        }
    }
}

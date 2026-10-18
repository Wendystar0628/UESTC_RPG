using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.Water.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class WaterMesherTests
{
    private static WaterBody Body(string id, WaterKind kind, IReadOnlyList<Vector2> outer, params IReadOnlyList<Vector2>[] holes)
    {
        var rings = new PolygonRings(outer, holes);
        var min = outer.Aggregate(Vector2.Min);
        var max = outer.Aggregate(Vector2.Max);
        return new(new FeatureId(id), kind, rings, 0f, -1, min, max, WaterCatalog.Area(rings));
    }

    private static Vector2[] Rect(float x0, float z0, float x1, float z1) => [new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)];

    private static float MeshArea(WaterMesh m)
    {
        var area = 0f;
        for (var t = 0; t < m.TriangleCount; t++) area += MathF.Abs(Cross(m, t)) * 0.5f;
        return area;
    }

    private static float Cross(WaterMesh m, int t)
    {
        int a = m.Index[3 * t], b = m.Index[3 * t + 1], c = m.Index[3 * t + 2];
        float ax = m.Position[3 * a], az = m.Position[3 * a + 2];
        float bx = m.Position[3 * b], bz = m.Position[3 * b + 2];
        float cx = m.Position[3 * c], cz = m.Position[3 * c + 2];
        return (bz - az) * (cx - ax) - (bx - ax) * (cz - az);
    }

    private static readonly IReadOnlyList<WaterBody> Shapes =
    [
        Body("lake", WaterKind.Lake, Rect(-21.3f, -17.9f, 30.7f, 25.1f), Rect(-3.1f, -2.2f, 6.4f, 5.9f)),
        // A concave lake with slanted shores and a vertex exactly on a grid line.
        Body("bay", WaterKind.Lake, [new(100f, 0f), new(160f, 8f), new(150.5f, 60f), new(128f, 24f), new(104f, 52f), new(96f, 20f)]),
    ];

    [TestCase]
    public void MeshCoversThePolygonExactly()
    {
        foreach (var body in Shapes)
        {
            var mesh = WaterMesher.Build(body, Shapes);
            var area = MeshArea(mesh);
            AssertThat(MathF.Abs(area - body.Area) > body.Area * 0.01f).OverrideFailureMessage($"{body.Id.Value}: mesh area {area} != polygon area {body.Area}").IsFalse();
        }
    }

    [TestCase]
    public void TrianglesFaceUpAndStayInside()
    {
        foreach (var body in Shapes)
        {
            var mesh = WaterMesher.Build(body, Shapes);
            for (var t = 0; t < mesh.TriangleCount; t++)
            {
                AssertThat(Cross(mesh, t) >= 0f).OverrideFailureMessage($"{body.Id.Value}: triangle {t} faces down").IsFalse();
                var c = Vector2.Zero;
                for (var k = 0; k < 3; k++)
                {
                    var v = mesh.Index[3 * t + k];
                    c += new Vector2(mesh.Position[3 * v], mesh.Position[3 * v + 2]) / 3f;
                }
                AssertThat(!body.Contains(c)).OverrideFailureMessage($"{body.Id.Value}: triangle {t} outside the water").IsFalse();
            }
        }
    }

    [TestCase]
    public void InnerEdgesAreSharedSoTheSurfaceIsWatertight()
    {
        foreach (var body in Shapes)
        {
            var mesh = WaterMesher.Build(body, Shapes);
            var edges = new Dictionary<(long, long, long, long), int>();
            for (var t = 0; t < mesh.TriangleCount; t++)
                for (var k = 0; k < 3; k++)
                {
                    var a = Key(mesh, mesh.Index[3 * t + k]);
                    var b = Key(mesh, mesh.Index[3 * t + (k + 1) % 3]);
                    var e = a.CompareTo(b) < 0 ? (a.Item1, a.Item2, b.Item1, b.Item2) : (b.Item1, b.Item2, a.Item1, a.Item2);
                    edges[e] = edges.GetValueOrDefault(e) + 1;
                }
            foreach (var (e, count) in edges)
            {
                if (count == 2) continue;
                var mid = new Vector2((e.Item1 + e.Item3) * 0.5e-4f, (e.Item2 + e.Item4) * 0.5e-4f);
                // A single-use edge must lie on the shore: water on one side only.
                var n = Vector2.Normalize(new Vector2(-(e.Item4 - e.Item2), e.Item3 - e.Item1)) * 0.05f;
                AssertThat(count != 1 || body.Contains(mid + n) == body.Contains(mid - n)).OverrideFailureMessage($"{body.Id.Value}: open inner edge at {mid} used {count} times").IsFalse();
            }
        }

        static (long, long) Key(WaterMesh m, int v) =>
            ((long)MathF.Round(m.Position[3 * v] * 1e4f), (long)MathF.Round(m.Position[3 * v + 2] * 1e4f));
    }

    [TestCase]
    public void ShoreDistanceIsZeroOnTheBankAndExactInside()
    {
        var lake = Shapes[0];
        var mesh = WaterMesher.Build(lake, Shapes);
        for (var v = 0; v < mesh.VertexCount; v++)
        {
            var p = new Vector2(mesh.Position[3 * v], mesh.Position[3 * v + 2]);
            var expected = MathF.Min(Exact(p), WaterParams.Default.SdfClamp);
            AssertThat(MathF.Abs(mesh.Shore[v] - expected) > 0.01f).OverrideFailureMessage($"shore distance {mesh.Shore[v]} at {p}, expected {expected}").IsFalse();
        }

        static float Exact(Vector2 p)
        {
            var outer = MathF.Min(MathF.Min(p.X + 21.3f, 30.7f - p.X), MathF.Min(p.Y + 17.9f, 25.1f - p.Y));
            var dx = MathF.Max(MathF.Max(-3.1f - p.X, p.X - 6.4f), 0f);
            var dz = MathF.Max(MathF.Max(-2.2f - p.Y, p.Y - 5.9f), 0f);
            return MathF.Min(outer, MathF.Sqrt(dx * dx + dz * dz));
        }
    }

    [TestCase]
    public void JointsBetweenRiverPiecesAreOpenWater()
    {
        var a = Body("a", WaterKind.Channel, Rect(0f, 0f, 8f, 40f));
        var b = Body("b", WaterKind.Channel, Rect(0f, 40f, 8f, 80f));
        IReadOnlyList<WaterBody> both = [a, b];
        var mesh = WaterMesher.Build(a, both);
        for (var v = 0; v < mesh.VertexCount; v++)
        {
            var p = new Vector2(mesh.Position[3 * v], mesh.Position[3 * v + 2]);
            AssertThat(p.Y == 40f && p.X > 0.5f && p.X < 7.5f && mesh.Shore[v] < 0.49f).OverrideFailureMessage($"joint vertex {p} treated as shore ({mesh.Shore[v]})").IsFalse();
            AssertThat(mesh.Flow[2 * v + 1] <= 0f).OverrideFailureMessage("channel must flow toward +z").IsFalse();
        }
    }

    [TestCase]
    public void SameInputGivesTheSameBytes()
    {
        var first = WaterMesher.Build(Shapes[1], Shapes);
        var second = WaterMesher.Build(Shapes[1], Shapes);
        AssertThat(!first.Position.SequenceEqual(second.Position) || !first.Index.SequenceEqual(second.Index) ||
            !first.Shore.SequenceEqual(second.Shore)).OverrideFailureMessage("water mesh is not deterministic").IsFalse();
    }
}

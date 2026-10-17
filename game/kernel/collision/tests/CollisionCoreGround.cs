using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;
using Campus.Gen.Ground;
using GdUnit4;

namespace Campus.Kernel.Collision.Tests;

public sealed partial class CollisionCoreTests
{
    private const float GroundLimit = .02f;

    private void CheckGround(CollisionTile collision, TileBundle visible)
    {
        var fields = collision.Colliders.OfType<HeightFieldCollider>().Where(field => field.Tile == collision.Tile).ToArray();
        if (fields.Length != 1) { Fail($"tile={collision.Tile} ground height fields={fields.Length}, expected one."); return; }
        var tops = new GroundTopIndex(collision.Tile, collision.Colliders);
        var checkedPoints = 0;
        var maximum = 0f;
        Vector3 worst = default;
        foreach (var surface in visible.Surfaces)
            foreach (var triangle in Triangles(surface))
            {
                if (!SupportingTriangle(triangle)) continue;
                foreach (var point in Samples(triangle))
                {
                    var error = tops.Error(point, triangle);
                    checkedPoints++;
                    if (error > maximum) { maximum = error; worst = point; }
                }
            }
        if (checkedPoints == 0) Fail($"tile={collision.Tile} has no measured L0 supporting triangles.");
        if (maximum > GroundLimit)
            Fail($"tile={collision.Tile} L0/C actual same-layer top error={maximum:R}m at {worst}, limit={GroundLimit:R}m (Infinity means missing compatible support).");
    }

    [TestCase]
    public void GroundSupportRequiresTheActualRoadAndPitLayerAtQuantizedEdges()
    {
        var tile = new TileKey(2, -1);
        var road = new SlabCollider([new(0, 0), new(2, 0), new(2, 2), new(0, 2)], .1f, .5f, SurfaceTag.Ground);
        var pit = new SlabCollider([new(3, 0), new(5, 0), new(5, 2), new(3, 2)], -.2f, .5f, SurfaceTag.Ground);
        var frame = pit with { Y = .2f };
        var roadFace = new Triangle(new(127.999f, .1f, -64), new(129, .1f, -64), new(129, .1f, -63));
        var pitFace = new Triangle(new(131, -.2f, -64), new(132, -.2f, -64), new(132, -.2f, -63));
        var actual = new GroundTopIndex(tile, [road, pit, frame]);
        Require(actual.Error(roadFace.A, roadFace) < GroundLimit && actual.Error(pitFace.A, pitFace) < GroundLimit,
            "Tile-local C tops must match transformed L0, including the finite edge displaced by XZ quantization.");
        Require(new GroundTopIndex(tile, [road with { Y = 0 }, frame]).Error(roadFace.A, roadFace) > GroundLimit
            && new GroundTopIndex(tile, [road, frame]).Error(pitFace.A, pitFace) > GroundLimit,
            "A lower base cannot replace a missing road, and a tree-pit frame cannot replace its soil layer.");
        Require(actual.Error(roadFace.A - Vector3.UnitX * .03f, roadFace) > GroundLimit,
            "Nearest finite-face distance must retain the original 2cm limit at a genuine support gap.");
    }

    private static bool SupportingTriangle(Triangle triangle)
    {
        var normal = Vector3.Cross(triangle.B - triangle.A, triangle.C - triangle.A);
        return float.IsFinite(normal.LengthSquared()) && normal.LengthSquared() > 1e-16f
            && MathF.Abs(normal.Y) >= normal.Length() * .7f;
    }

    // Only formal Ground tops participate. Wall caps and unrelated object tops cannot fill ground holes.
    private static IEnumerable<Triangle> GroundTops(TileKey tile, IEnumerable<Collider> colliders)
    {
        var at = Matrix4x4.CreateTranslation(CollisionGeometry.Origin(tile));
        foreach (var source in colliders.Where(collider => collider.Tag == SurfaceTag.Ground))
        {
            if (source is HeightFieldCollider field)
            {
                var origin = CollisionGeometry.Origin(field.Tile);
                Vector3 Point(int x, int z) => origin + new Vector3(x,
                    TerrainSampling.DecodeHeight(field.HeightsCm[z * GroundCollision.Samples + x]), z);
                for (var z = 0; z < GroundCollision.Samples - 1; z++)
                    for (var x = 0; x < GroundCollision.Samples - 1; x++)
                    {
                        var a = Point(x, z); var b = Point(x + 1, z); var c = Point(x, z + 1); var d = Point(x + 1, z + 1);
                        // Jolt and the production L0 both use the anti-diagonal b-c.
                        if (float.IsFinite(a.Y) && float.IsFinite(b.Y) && float.IsFinite(c.Y)) yield return new(a, b, c);
                        if (float.IsFinite(b.Y) && float.IsFinite(c.Y) && float.IsFinite(d.Y)) yield return new(b, d, c);
                    }
                continue;
            }
            switch (CollisionGeometry.Transform(source, at))
            {
                case SlabCollider slab:
                    foreach (var part in CollisionGeometry.PrepareSlab(slab))
                        for (var i = 1; i + 1 < part.Ring.Count; i++)
                            yield return new(new(part.Ring[0].X, part.Y, part.Ring[0].Y),
                                new(part.Ring[i].X, part.Y, part.Ring[i].Y), new(part.Ring[i + 1].X, part.Y, part.Ring[i + 1].Y));
                    break;
                case RampCollider ramp:
                    yield return new(ramp.P0, ramp.P1, ramp.P2);
                    yield return new(ramp.P0, ramp.P2, ramp.P3);
                    break;
                case BoxCollider box:
                    Vector3 P(float x, float z) => Vector3.Transform(new(x * box.Half.X, box.Half.Y, z * box.Half.Z), box.At);
                    yield return new(P(-1, -1), P(1, -1), P(1, 1));
                    yield return new(P(-1, -1), P(1, 1), P(-1, 1));
                    break;
            }
        }
    }

    private sealed class GroundTopIndex
    {
        private readonly Dictionary<(int X, int Z), List<Top>> _bins = [];
        private readonly record struct Top(Triangle Face, Vector3 Normal);

        public GroundTopIndex(TileKey tile, IEnumerable<Collider> colliders)
        {
            foreach (var face in GroundTops(tile, colliders).Where(SupportingTriangle))
            {
                var normal = Vector3.Normalize(Vector3.Cross(face.B - face.A, face.C - face.A));
                var top = new Top(face, normal);
                var min = Vector3.Min(face.A, Vector3.Min(face.B, face.C)) - new Vector3(GroundLimit);
                var max = Vector3.Max(face.A, Vector3.Max(face.B, face.C)) + new Vector3(GroundLimit);
                for (var z = (int)MathF.Floor(min.Z); z <= (int)MathF.Floor(max.Z); z++)
                    for (var x = (int)MathF.Floor(min.X); x <= (int)MathF.Floor(max.X); x++)
                    {
                        if (!_bins.TryGetValue((x, z), out var bin)) _bins.Add((x, z), bin = []);
                        bin.Add(top);
                    }
            }
        }

        public float Error(Vector3 point, Triangle visible)
        {
            if (!_bins.TryGetValue(((int)MathF.Floor(point.X), (int)MathF.Floor(point.Z)), out var bin)) return float.PositiveInfinity;
            var minimum = float.PositiveInfinity;
            foreach (var top in bin)
            {
                // The L0 facet must fit this C supporting plane across all three vertices. This
                // retains its layer and slope instead of selecting an accidentally nearby Y.
                if (MathF.Abs(Vector3.Dot(visible.A - top.Face.A, top.Normal)) > GroundLimit
                    || MathF.Abs(Vector3.Dot(visible.B - top.Face.A, top.Normal)) > GroundLimit
                    || MathF.Abs(Vector3.Dot(visible.C - top.Face.A, top.Normal)) > GroundLimit) continue;
                minimum = MathF.Min(minimum, PointTriangleDistanceSquared(point, top.Face));
                if (minimum == 0) break;
            }
            return MathF.Sqrt(minimum);
        }
    }

    private static float PointTriangleDistanceSquared(Vector3 p, Triangle triangle)
    {
        var a = triangle.A; var b = triangle.B; var c = triangle.C;
        var ab = b - a; var ac = c - a; var ap = p - a;
        var d1 = Vector3.Dot(ab, ap); var d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return ap.LengthSquared();
        var bp = p - b; var d3 = Vector3.Dot(ab, bp); var d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return bp.LengthSquared();
        var vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0) return Vector3.DistanceSquared(p, a + ab * (d1 / (d1 - d3)));
        var cp = p - c; var d5 = Vector3.Dot(ab, cp); var d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6) return cp.LengthSquared();
        var vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0) return Vector3.DistanceSquared(p, a + ac * (d2 / (d2 - d6)));
        var va = d3 * d6 - d5 * d4;
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            return Vector3.DistanceSquared(p, b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6))));
        var inverse = 1f / (va + vb + vc);
        return Vector3.DistanceSquared(p, a + ab * (vb * inverse) + ac * (vc * inverse));
    }
}

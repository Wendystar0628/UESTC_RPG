using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using Campus.Gen.World;

namespace Campus.Gen.Maps.Qingshuihe.Core.Museum;

/// <summary>
/// Plan helpers for convex rings in (x, z) with positive shoelace area, where edge i runs from point i to point i + 1
/// and its outward side is (dz, −dx): the orientation <see cref="Face"/> and the kernel sweep both use (+X outward).
/// Arithmetic only.
/// </summary>
public static class MuseumGeometry
{

    /// <summary>Outward unit normal of edge <paramref name="i"/>.</summary>
    public static Vector2 Normal(IReadOnlyList<Vector2> ring, int i) =>
        PlanOffsets.RightUnitNormal(ring[i], ring[(i + 1) % ring.Count]);

    /// <summary>The wall face of edge <paramref name="i"/>: u from point i, outward normal, length.</summary>
    public static Face FaceOf(IReadOnlyList<Vector2> ring, int i)
    {
        Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
        var length = Vector2.Distance(a, b);
        var along = (b - a) / length;
        return new(a, along, new(along.Y, -along.X), length);
    }

    /// <summary>Each edge moved outward by its own distance (negative moves it in); corners at the moved lines' intersections.</summary>
    public static IReadOnlyList<Vector2> Offset(IReadOnlyList<Vector2> ring, IReadOnlyList<float> distance)
    {
        var n = ring.Count;
        if (distance.Count != n) throw new ArgumentException("One distance per edge.", nameof(distance));
        var result = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            var prev = (i + n - 1) % n;
            Vector2 n0 = Normal(ring, prev), n1 = Normal(ring, i);
            float c0 = Vector2.Dot(ring[i], n0) + distance[prev], c1 = Vector2.Dot(ring[i], n1) + distance[i];
            var det = n0.X * n1.Y - n0.Y * n1.X;
            result[i] = MathF.Abs(det) < 1e-6f
                ? ring[i] + n1 * distance[i]
                : new((c0 * n1.Y - c1 * n0.Y) / det, (n0.X * c1 - n1.X * c0) / det);
        }
        return result;
    }

    /// <summary>
    /// Every corner cut back by <paramref name="cut"/> along both edges: point i becomes (in, out), so face 2i is corner i's
    /// chamfer and face 2i + 1 the shortened edge i.
    /// </summary>
    public static IReadOnlyList<Vector2> Chamfer(IReadOnlyList<Vector2> ring, float cut)
    {
        var n = ring.Count;
        var result = new Vector2[2 * n];
        for (var i = 0; i < n; i++)
        {
            Vector2 p = ring[(i + n - 1) % n], c = ring[i], q = ring[(i + 1) % n];
            result[2 * i] = c + Vector2.Normalize(p - c) * cut;
            result[2 * i + 1] = c + Vector2.Normalize(q - c) * cut;
        }
        return result;
    }

    /// <summary>
    /// The ring scaled about its box centre in the registered axis frame to the given length
    /// and width: an inner octagon parallel to the outer one, as the central hall is to the gallery.
    /// </summary>
    public static IReadOnlyList<Vector2> Scale(IReadOnlyList<Vector2> ring, Vector2 along, float length, float width)
    {
        var u = along;
        var v = new Vector2(-u.Y, u.X);
        float u0 = ring.Min(p => Vector2.Dot(p, u)), u1 = ring.Max(p => Vector2.Dot(p, u));
        float v0 = ring.Min(p => Vector2.Dot(p, v)), v1 = ring.Max(p => Vector2.Dot(p, v));
        var centre = u * ((u0 + u1) * 0.5f) + v * ((v0 + v1) * 0.5f);
        float su = length / (u1 - u0), sv = width / (v1 - v0);
        return ring.Select(p => centre + u * (Vector2.Dot(p - centre, u) * su) + v * (Vector2.Dot(p - centre, v) * sv)).ToArray();
    }

}

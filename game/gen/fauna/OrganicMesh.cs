using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Fauna;

/// <summary>Colour of a surface patch, from its model-space centre and normal.</summary>
public delegate PaletteToken Paint(Vector3 position, Vector3 normal);

/// <summary>One cross-section of a loft: centre, half width (side axis), half height (up axis) and a roll about the path.</summary>
public readonly record struct Ring(Vector3 Centre, float HalfWidth, float HalfHeight);

/// <summary>An animal model in its own frame (metres; +Z forward, +Y up, feet on y = 0), each triangle one colour.</summary>
public sealed record AnimalMesh(Vector3[] Positions, Vector3[] Normals, PaletteToken[] Colors, int[] Index)
{
    public int Triangles => Index.Length / 3;
    public Box3 Bounds()
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in Positions) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        return new(min, max);
    }
}

/// <summary>
/// Smooth organic shapes for the animals: every part is a closed loft (a path of elliptical rings with rounded pole caps),
/// ellipsoids included. Normals are smooth within a part (area-weighted), so each part reads as one soft volume under the
/// two-and-a-half-step toon; parts overlap so the outline draws their seams like a drawn figure. Colours are per triangle
/// (the toon reads the palette ID as a flat varying), split where a part's paint changes. Pure and deterministic.
/// </summary>
public sealed class OrganicMesh
{
    /// <summary>Tessellation factor over the nominal segment counts: round silhouettes and clean toon bands at close range (ART-MODELING, 1 px silhouette error).</summary>
    public const float Detail = 1.6f;

    private readonly List<Vector3> _p = [];
    private readonly List<Vector3> _n = [];
    private readonly List<int> _index = [];
    private readonly List<PaletteToken> _triColor = [];

    /// <summary>A closed loft through the rings, capped by pole vertices a little beyond each end ring (rounded ends).</summary>
    public OrganicMesh Loft(IReadOnlyList<Ring> rings, Paint paint, int segments = 14, float startCap = 0.7f, float endCap = 0.7f, Vector3? up = null)
    {
        ArgumentNullException.ThrowIfNull(rings);
        ArgumentNullException.ThrowIfNull(paint);
        if (rings.Count < 2) throw new ArgumentException("A loft needs at least two rings.", nameof(rings));
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        segments = (int)MathF.Round(segments * Detail);
        var count = rings.Count;
        var tangents = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            var a = rings[Math.Max(0, i - 1)].Centre;
            var b = rings[Math.Min(count - 1, i + 1)].Centre;
            tangents[i] = Unit(b - a, Vector3.UnitZ);
        }
        // Parallel-transported frame: v is the "height" axis, s the "width" axis.
        var hint = up ?? (MathF.Abs(tangents[0].Y) > 0.9f ? Vector3.UnitZ : Vector3.UnitY);
        var v = Unit(hint - tangents[0] * Vector3.Dot(hint, tangents[0]), Vector3.UnitY);
        var local = new List<Vector3>(count * segments + 2);
        for (var i = 0; i < count; i++)
        {
            var t = tangents[i];
            v = Unit(v - t * Vector3.Dot(v, t), v);
            var s = Vector3.Cross(v, t);
            var ring = rings[i];
            for (var k = 0; k < segments; k++)
            {
                var angle = 2f * float.Pi * k / segments;
                var c = DetMath.Cos(angle);
                var sn = DetMath.Sin(angle);
                local.Add(ring.Centre + s * (c * ring.HalfWidth) + v * (sn * ring.HalfHeight));
            }
        }
        var first = rings[0];
        var last = rings[count - 1];
        local.Add(first.Centre - tangents[0] * (MathF.Min(first.HalfWidth, first.HalfHeight) * startCap));
        local.Add(last.Centre + tangents[count - 1] * (MathF.Min(last.HalfWidth, last.HalfHeight) * endCap));
        var startPole = count * segments;
        var endPole = startPole + 1;
        var tris = new List<(int A, int B, int C)>(count * segments * 2);
        for (var i = 0; i + 1 < count; i++)
            for (var k = 0; k < segments; k++)
            {
                var a = i * segments + k;
                var b = i * segments + (k + 1) % segments;
                var c = (i + 1) * segments + (k + 1) % segments;
                var d = (i + 1) * segments + k;
                tris.Add((a, b, c));
                tris.Add((a, c, d));
            }
        for (var k = 0; k < segments; k++)
        {
            tris.Add((startPole, (k + 1) % segments, k));
            tris.Add((endPole, (count - 1) * segments + k, (count - 1) * segments + (k + 1) % segments));
        }
        Emit(local, tris, paint, rings.Select(r => r.Centre).ToArray());
        return this;
    }

    /// <summary>An ellipsoid along <paramref name="axis"/> (its long radius), with <paramref name="up"/> fixing the height radius.</summary>
    public OrganicMesh Ellipsoid(Vector3 centre, Vector3 axis, Vector3 up, float alongRadius, float widthRadius, float heightRadius,
        Paint paint, int segments = 14, int rings = 8)
    {
        rings = (int)MathF.Round(rings * 1.4f);
        var a = Unit(axis, Vector3.UnitZ);
        var u = Unit(up - a * Vector3.Dot(up, a), Vector3.UnitY);
        var list = new List<Ring>(rings - 1);
        for (var i = 1; i < rings; i++)
        {
            var theta = float.Pi * i / rings;
            var z = -DetMath.Cos(theta) * alongRadius;
            var r = DetMath.Sin(theta);
            list.Add(new(centre + a * z, widthRadius * r, heightRadius * r));
        }
        // Pole caps land on the true poles: cap distance = remaining radius / min ring radius.
        var endDist = alongRadius * (1f - DetMath.Cos(float.Pi / rings));
        var minRing = MathF.Min(list[0].HalfWidth, list[0].HalfHeight);
        var cap = endDist / MathF.Max(minRing, 1e-4f);
        return Loft(list, paint, segments, cap, cap, u);
    }

    /// <summary>Ellipsoid aligned to the model axes: radii along X (width), Y (height), Z (length).</summary>
    public OrganicMesh Blob(Vector3 centre, Vector3 radii, Paint paint, int segments = 14, int rings = 8) =>
        Ellipsoid(centre, Vector3.UnitZ, Vector3.UnitY, radii.Z, radii.X, radii.Y, paint, segments, rings);

    /// <summary>A tapered tube through points, radius interpolated from start to end, rounded ends.</summary>
    public OrganicMesh Tube(IReadOnlyList<Vector3> path, float startRadius, float endRadius, Paint paint, int segments = 10, float flatten = 1f)
    {
        path = Smooth(path, 3);
        var rings = new List<Ring>(path.Count);
        var length = 0f;
        var cumulative = new float[path.Count];
        for (var i = 1; i < path.Count; i++) cumulative[i] = length += Vector3.Distance(path[i - 1], path[i]);
        for (var i = 0; i < path.Count; i++)
        {
            var t = length <= 0 ? 0 : cumulative[i] / length;
            var r = startRadius + (endRadius - startRadius) * t;
            rings.Add(new(path[i], r, r * flatten));
        }
        return Loft(rings, paint, segments, 0.8f, 0.8f);
    }

    /// <summary>Catmull-Rom subdivision of a path, so tails and necks bend without kinks.</summary>
    private static IReadOnlyList<Vector3> Smooth(IReadOnlyList<Vector3> path, int steps)
    {
        if (path.Count < 3) return path;
        var result = new List<Vector3>((path.Count - 1) * steps + 1);
        for (var i = 0; i + 1 < path.Count; i++)
        {
            var p0 = path[Math.Max(0, i - 1)];
            var p1 = path[i];
            var p2 = path[i + 1];
            var p3 = path[Math.Min(path.Count - 1, i + 2)];
            for (var k = 0; k < steps; k++)
            {
                var t = (float)k / steps;
                var t2 = t * t;
                var t3 = t2 * t;
                result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
            }
        }
        result.Add(path[^1]);
        return result;
    }

    /// <summary>A cone-like spike (ears, beaks): base ring to a near-point tip.</summary>
    public OrganicMesh Spike(Vector3 baseCentre, Vector3 tip, float baseHalfWidth, float baseHalfHeight, Paint paint, int segments = 8)
    {
        var mid = Vector3.Lerp(baseCentre, tip, 0.55f);
        var nearTip = Vector3.Lerp(baseCentre, tip, 0.92f);
        return Loft([new(baseCentre, baseHalfWidth, baseHalfHeight), new(mid, baseHalfWidth * 0.55f, baseHalfHeight * 0.55f),
            new(nearTip, baseHalfWidth * 0.12f, baseHalfHeight * 0.12f)], paint, segments, 0.4f, 1.2f);
    }

    private void Emit(List<Vector3> local, List<(int A, int B, int C)> tris, Paint paint, Vector3[] spine)
    {
        // Orient every triangle outward (away from the nearest spine point), then accumulate smooth normals.
        var normals = new Vector3[local.Count];
        var oriented = new List<(int A, int B, int C)>(tris.Count);
        foreach (var (a0, b0, c0) in tris)
        {
            var a = a0; var b = b0; var c = c0;
            var pa = local[a]; var pb = local[b]; var pc = local[c];
            var face = Vector3.Cross(pb - pa, pc - pa);
            if (face.LengthSquared() < 1e-14f) continue;
            var centroid = (pa + pb + pc) / 3f;
            var outward = centroid - Nearest(spine, centroid);
            if (Vector3.Dot(face, outward) < 0) { (b, c) = (c, b); face = -face; }
            normals[a] += face; normals[b] += face; normals[c] += face;
            oriented.Add((a, b, c));
        }
        for (var i = 0; i < normals.Length; i++) normals[i] = Unit(normals[i], Vector3.UnitY);
        // Split vertices where the paint changes: one output vertex per (source vertex, colour).
        var map = new Dictionary<(int, string), int>();
        foreach (var (a, b, c) in oriented)
        {
            var centre = (local[a] + local[b] + local[c]) / 3f;
            var n = Unit(normals[a] + normals[b] + normals[c], Vector3.UnitY);
            var color = paint(centre, n);
            // Godot's front face is clockwise seen from outside: emit the outward counter-clockwise triangle reversed.
            _index.Add(Vertex(a)); _index.Add(Vertex(c)); _index.Add(Vertex(b));
            _triColor.Add(color);

            int Vertex(int source)
            {
                if (map.TryGetValue((source, color.Value), out var id)) return id;
                id = _p.Count;
                _p.Add(local[source]);
                _n.Add(normals[source]);
                map[(source, color.Value)] = id;
                return id;
            }
        }
    }

    private static Vector3 Nearest(Vector3[] spine, Vector3 p)
    {
        // Distance to the spine polyline (segments), so long tubes orient correctly along their length.
        if (spine.Length == 1) return spine[0];
        var best = spine[0];
        var bestD = float.MaxValue;
        for (var i = 0; i + 1 < spine.Length; i++)
        {
            var a = spine[i];
            var ab = spine[i + 1] - a;
            var len = ab.LengthSquared();
            var t = len <= 1e-12f ? 0 : Math.Clamp(Vector3.Dot(p - a, ab) / len, 0, 1);
            var q = a + ab * t;
            var d = Vector3.DistanceSquared(p, q);
            if (d < bestD) { bestD = d; best = q; }
        }
        return best;
    }

    /// <summary>Per-vertex colours: each output vertex belongs to triangles of a single colour by construction.</summary>
    public AnimalMesh Finish()
    {
        var colors = new PaletteToken[_p.Count];
        for (var t = 0; t < _triColor.Count; t++)
            for (var k = 0; k < 3; k++) colors[_index[t * 3 + k]] = _triColor[t];
        return new([.. _p], [.. _n], colors, [.. _index]);
    }

    internal static Vector3 Unit(Vector3 v, Vector3 fallback)
    {
        var l = v.Length();
        return l > 1e-8f ? v / l : fallback;
    }
}

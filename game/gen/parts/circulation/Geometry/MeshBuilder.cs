using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Circulation.Geometry;

/// <summary>
/// Collects closed solids from the WP-09 kernel and from profile extrusions into one surface per material and vertex
/// flag, in first-use order. Everything is local to the part; nothing here allocates engine objects.
/// </summary>
internal sealed class MeshBuilder
{
    /// <summary>Vertex flag byte (contract VF-A): 8 = no shadow, for balusters, bars and other thin repeated items.</summary>
    internal const byte NoShadow = 8;
    private const float BoundsTolerance = 1e-4f;

    private sealed class Layer(ToonSpec material, byte flags)
    {
        public ToonSpec Material { get; } = material;
        public byte Flags { get; } = flags;
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<float> Uv { get; } = [];
        public List<float> Ao { get; } = [];
        public List<int> Index { get; } = [];
        public int Count => Position.Count / 3;
    }

    private readonly List<Layer> _layers = [];

    private Layer For(ToonSpec material, byte flags)
    {
        foreach (var layer in _layers)
            if (layer.Flags == flags && layer.Material == material) return layer;
        var created = new Layer(material, flags);
        _layers.Add(created);
        return created;
    }

    /// <summary>Appends kernel geometry; its Ao is already visibility (1 = unoccluded, contract VF-A) and passes through.</summary>
    internal void Add(PartGeometry geometry, Frame frame, byte flags = 0)
    {
        foreach (var mesh in geometry.Meshes)
        {
            var layer = For(mesh.Material.Spec, flags);
            var offset = layer.Count;
            var count = mesh.Position.Length / 3;
            if (offset + count > 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices.");
            for (var i = 0; i < count; i++)
            {
                var p = frame.Point(new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]));
                var n = frame.Direction(new(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]));
                layer.Position.Add(p.X); layer.Position.Add(p.Y); layer.Position.Add(p.Z);
                layer.Normal.Add(n.X); layer.Normal.Add(n.Y); layer.Normal.Add(n.Z);
                layer.Uv.Add(mesh.Uv is null ? 0 : mesh.Uv[2 * i]); layer.Uv.Add(mesh.Uv is null ? 0 : mesh.Uv[2 * i + 1]);
                layer.Ao.Add(mesh.Ao is null ? 1 : mesh.Ao[i]);
            }
            foreach (var index in mesh.Index) layer.Index.Add(offset + index);
        }
    }

    /// <summary>Axis-aligned box in frame space; a positive bevel uses the kernel's single-band chamfer box.</summary>
    internal void Box(ToonSpec material, Vector3 min, Vector3 max, float bevel, Frame frame, byte flags = 0)
    {
        var size = V3.Sub(max, min);
        var smallest = Math.Min(size.X, Math.Min(size.Y, size.Z));
        var b = bevel > 0 && bevel < smallest * 0.45f ? bevel : 0;
        var box = PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, b, material);
        Add(box, Frame.At((min.X + max.X) * 0.5f, min.Y, (min.Z + max.Z) * 0.5f).Then(frame), flags);
    }

    /// <summary>
    /// Closed ellipsoid with smooth analytic normals (ART-MODELING "曲面用软边"), for soft masses and rail end knobs.
    /// Rings run pole to pole; both poles are single vertices with triangle fans.
    /// </summary>
    internal void Ellipsoid(ToonSpec material, Vector3 centre, Vector3 radii, int rings, int segments, Frame frame, byte flags = 0)
    {
        if (rings < 3 || segments < 6 || !(radii.X > 0 && radii.Y > 0 && radii.Z > 0)) throw new ArgumentException("An ellipsoid needs 3 rings, 6 segments and positive radii.");
        var layer = For(material, flags);
        if (layer.Count + (rings - 1) * segments + 2 > 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices.");
        Vector3 Normal(Vector3 unit) => V3.Unit(new(unit.X / radii.X, unit.Y / radii.Y, unit.Z / radii.Z));
        Vector3 At(Vector3 unit) => new(centre.X + radii.X * unit.X, centre.Y + radii.Y * unit.Y, centre.Z + radii.Z * unit.Z);
        var top = Vertex(layer, frame, At(Vector3.UnitY), Vector3.UnitY, Vector2.Zero);
        var ring = new int[rings - 1, segments];
        for (var i = 1; i < rings; i++)
        {
            var theta = Math.PI * i / rings;
            var y = (float)Math.Cos(theta); var r = (float)Math.Sin(theta);
            for (var j = 0; j < segments; j++)
            {
                var phi = 2 * Math.PI * j / segments;
                var unit = new Vector3(r * (float)Math.Cos(phi), y, r * (float)Math.Sin(phi));
                ring[i - 1, j] = Vertex(layer, frame, At(unit), Normal(unit), new(j / (float)segments, i / (float)rings));
            }
        }
        var bottom = Vertex(layer, frame, At(-Vector3.UnitY), -Vector3.UnitY, Vector2.One);
        Vector3 Out(int a, int b, int c) => V3.Sub(V3.Scale(V3.Add(V3.Add(P(layer, a), P(layer, b)), P(layer, c)), 1f / 3), frame.Point(centre));
        for (var j = 0; j < segments; j++)
        {
            var k = (j + 1) % segments;
            Triangle(layer, top, ring[0, j], ring[0, k], Out(top, ring[0, j], ring[0, k]));
            for (var i = 0; i + 1 < rings - 1; i++)
            {
                int a = ring[i, j], b = ring[i, k], c = ring[i + 1, k], d = ring[i + 1, j];
                Triangle(layer, a, b, c, Out(a, b, c)); Triangle(layer, a, c, d, Out(a, c, d));
            }
            Triangle(layer, bottom, ring[rings - 2, k], ring[rings - 2, j], Out(bottom, ring[rings - 2, k], ring[rings - 2, j]));
        }
    }

    /// <summary>
    /// Sweeps a closed (lateral, up) section along a polyline whose points carry their own height. Like the WP-09 sweep
    /// the joins are mitred in plan and positive lateral is (dz, −dx) of the travel direction; unlike it the section's up
    /// axis stays vertical on a sloping path (curbs and risers stay plumb), faces take their true normals, chamfer edges
    /// interpolate between neighbouring faces, smooth sections (tubes) take bisector-radial normals, and open ends are
    /// capped with caller-supplied triangles (section index triples) or the kernel's Earcut.
    /// </summary>
    internal void SweepPath(ToonSpec material, IReadOnlyList<Vector2> section, IReadOnlyList<bool>? chamfer, IReadOnlyList<int>? capTriangles,
        IReadOnlyList<Vector3> path, bool closed, Frame frame, byte flags = 0, bool smooth = false, float miterLimit = 4)
    {
        ArgumentNullException.ThrowIfNull(section); ArgumentNullException.ThrowIfNull(path);
        int n = section.Count, m = path.Count;
        if (n < 3 || chamfer is not null && chamfer.Count != n) throw new ArgumentException("A section needs three points and one flag per edge.");
        if (m < (closed ? 3 : 2)) throw new ArgumentException("A sweep needs two points, or three for a closed path.");
        double area = 0;
        for (var i = 0; i < n; i++)
        {
            var a = section[i]; var b = section[(i + 1) % n];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        if (!(Math.Abs(area) > 1e-12)) throw new ArgumentException("A section must enclose area.");
        var sign = area > 0 ? 1f : -1f;
        // Outward section-edge normals in (lateral, up); counterclockwise means outward = (du, −dl).
        var edge = new Vector2[n];
        for (var k = 0; k < n; k++)
        {
            var d = section[(k + 1) % n] - section[k];
            var length = V3.Length(d);
            if (!(length > 1e-7f)) throw new ArgumentException("A section has a repeated point.");
            edge[k] = new(sign * d.Y / length, -sign * d.X / length);
        }
        var segments = closed ? m : m - 1;
        var tangent = new Vector3[segments]; var right = new Vector3[segments];
        for (var s = 0; s < segments; s++)
        {
            var a = path[s]; var b = path[(s + 1) % m];
            tangent[s] = V3.Unit(new(b.X - a.X, 0, b.Z - a.Z));
            right[s] = new(tangent[s].Z, 0, -tangent[s].X);
        }
        var miter = new Vector3[m];
        for (var i = 0; i < m; i++)
        {
            if (!closed && i == 0) { miter[i] = right[0]; continue; }
            if (!closed && i == m - 1) { miter[i] = right[^1]; continue; }
            var ra = right[(i + segments - 1) % segments]; var rb = right[i % segments];
            var denominator = 1 + V3.Dot(ra, rb);
            if (denominator < 1e-8) throw new ArgumentException("A sweep cannot reverse direction at a join.");
            miter[i] = V3.Scale(V3.Add(ra, rb), (float)(1 / denominator));
            if (V3.Dot(miter[i], miter[i]) > (double)miterLimit * miterLimit + 1e-6) throw new ArgumentException("A sweep join exceeds the miter limit.");
        }
        Vector3 Pos(int i, Vector2 p) => new(path[i].X + miter[i].X * p.X, path[i].Y + p.Y, path[i].Z + miter[i].Z * p.X);
        Vector3 Lift(Vector3 lateral, Vector2 n2) => new(lateral.X * n2.X, n2.Y, lateral.Z * n2.X);
        var layer = For(material, flags);
        if (layer.Count + segments * n * 4 + 2 * n > 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices.");
        float along = 0;
        for (var s = 0; s < segments; s++)
        {
            int i = s, j = (s + 1) % m;
            for (var k = 0; k < n; k++)
                if (V3.Dot(V3.Sub(Pos(j, section[k]), Pos(i, section[k])), tangent[s]) <= 0)
                    throw new ArgumentException("A sweep segment is too short for the section and adjacent miters.");
            // True face normals of this segment, oriented by the section's outward normal.
            var face = new Vector3[n];
            for (var k = 0; k < n; k++)
            {
                var a = Pos(i, section[k]); var b = Pos(i, section[(k + 1) % n]); var d = Pos(j, section[k]);
                var guess = Lift(right[s], edge[k]);
                var cross = V3.Cross(V3.Sub(b, a), V3.Sub(d, a));
                face[k] = V3.Dot(cross, cross) > 1e-20 ? V3.Unit(cross) : guess;
                if (V3.Dot(face[k], guess) < 0) face[k] = V3.Scale(face[k], -1);
            }
            Vector3 Smooth(int vertex, int k)
            {
                var axis = V3.Unit(miter[vertex]);
                return V3.Unit(Lift(axis, edge[(k + n - 1) % n] + edge[k]));
            }
            var length = V3.Length(new Vector2(path[j].X - path[i].X, path[j].Z - path[i].Z));
            for (var k = 0; k < n; k++)
            {
                var l = (k + 1) % n;
                var bevel = chamfer is not null && chamfer[k];
                Vector3 n0, n1, n2, n3;
                if (smooth) { n0 = Smooth(i, k); n1 = Smooth(i, l); n2 = Smooth(j, l); n3 = Smooth(j, k); }
                else
                {
                    n0 = n3 = bevel ? face[(k + n - 1) % n] : face[k];
                    n1 = n2 = bevel ? face[l] : face[k];
                }
                var v = V3.Length(section[l] - section[k]);
                var a = Vertex(layer, frame, Pos(i, section[k]), n0, new(along, 0));
                var b = Vertex(layer, frame, Pos(i, section[l]), n1, new(along, v));
                var c = Vertex(layer, frame, Pos(j, section[l]), n2, new(along + length, v));
                var d = Vertex(layer, frame, Pos(j, section[k]), n3, new(along + length, 0));
                Quad(layer, a, b, c, d, frame.Direction(face[k]));
            }
            along += length;
        }
        if (closed) return;
        var (capPoints, capIndices) = capTriangles is null ? Earcut(section) : Explicit(section, capTriangles, Math.Abs(area) * 0.5);
        for (var end = 0; end < 2; end++)
        {
            var i = end == 0 ? 0 : m - 1;
            var outward = end == 0 ? V3.Scale(tangent[0], -1) : tangent[^1];
            var ids = new int[capPoints.Count];
            for (var k = 0; k < ids.Length; k++) ids[k] = Vertex(layer, frame, Pos(i, capPoints[k]), outward, capPoints[k]);
            for (var t = 0; t < capIndices.Count; t += 3)
                Triangle(layer, ids[capIndices[t]], ids[capIndices[t + 1]], ids[capIndices[t + 2]], frame.Direction(outward));
        }
    }

    /// <summary>Kernel sweep laid into the vertical plane through x (see <see cref="Frame.VerticalPlane"/>).</summary>
    internal void VerticalSweep(ToonSpec material, SweepProfile profile, IReadOnlyList<Vector2> zyPath, float x, Frame frame, byte flags = 0) =>
        Add(PartGeometryKernel.Sweep(profile, zyPath, material), Frame.VerticalPlane(x).Then(frame), flags);

    /// <summary>
    /// Extrudes a simple (z, y) ring across x0..x1. Edges flagged in <paramref name="chamfer"/> are single-band bevels whose
    /// normals run from the previous face's normal to the next face's; every other face is exactly flat (ART-MODELING).
    /// Caps come from <paramref name="capTriangles"/> (ring index triples, any winding) when given, which lets a ring carry
    /// collinear split points; otherwise from the kernel's Earcut, and then the ring must not have collinear neighbours.
    /// </summary>
    internal void Extrude(ToonSpec material, IReadOnlyList<Vector2> ring, IReadOnlyList<bool>? chamfer, float x0, float x1, Frame frame,
        IReadOnlyList<int>? capTriangles = null, byte flags = 0)
    {
        ArgumentNullException.ThrowIfNull(ring);
        if (!(x1 > x0)) throw new ArgumentException("Extrusion needs x1 > x0.");
        var n = ring.Count;
        if (n < 3 || chamfer is not null && chamfer.Count != n) throw new ArgumentException("A ring needs three points and one flag per edge.");
        double area = 0;
        for (var i = 0; i < n; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % n];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        if (!(Math.Abs(area) > 1e-12)) throw new ArgumentException("A ring must enclose area.");
        var (capPoints, capIndices) = capTriangles is null ? Earcut(ring) : Explicit(ring, capTriangles, Math.Abs(area) * 0.5);
        var sign = area > 0 ? 1f : -1f;
        var normals = new Vector3[n];
        for (var i = 0; i < n; i++)
        {
            var d = ring[(i + 1) % n] - ring[i];
            var length = V3.Length(d);
            if (!(length > 1e-7f)) throw new ArgumentException("A ring has a repeated point.");
            // Ring (z, y) is counterclockwise when area > 0; its outward normal is (dy, −dz).
            normals[i] = new(0, -sign * d.X / length, sign * d.Y / length);
        }
        var layer = For(material, flags);
        var start = layer.Count;
        if (start + 4 * n + 2 * n > 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices.");
        float along = 0;
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var bevel = chamfer is not null && chamfer[i];
            var ni = bevel ? normals[(i + n - 1) % n] : normals[i];
            var nj = bevel ? normals[j] : normals[i];
            var length = V3.Length(ring[j] - ring[i]);
            var a = Vertex(layer, frame, new(x0, ring[i].Y, ring[i].X), ni, new(x0, along));
            var b = Vertex(layer, frame, new(x1, ring[i].Y, ring[i].X), ni, new(x1, along));
            var c = Vertex(layer, frame, new(x1, ring[j].Y, ring[j].X), nj, new(x1, along + length));
            var d = Vertex(layer, frame, new(x0, ring[j].Y, ring[j].X), nj, new(x0, along + length));
            Quad(layer, a, b, c, d, frame.Direction(normals[i]));
            along += length;
        }
        for (var side = 0; side < 2; side++)
        {
            var x = side == 0 ? x0 : x1;
            var normal = new Vector3(side == 0 ? -1 : 1, 0, 0);
            var ids = new int[capPoints.Count];
            for (var i = 0; i < ids.Length; i++) ids[i] = Vertex(layer, frame, new(x, capPoints[i].Y, capPoints[i].X), normal, capPoints[i]);
            // Cap triangles arrive counterclockwise in (z, y), whose right-hand normal is −x. Godot's front face is
            // clockwise from outside (right-hand normal pointing inward), so the +x cap keeps that order and the −x cap
            // reverses it. No float cross product decides the winding, so near-collinear split points cannot flip it.
            for (var i = 0; i < capIndices.Count; i += 3)
            {
                int a = ids[capIndices[i]], b = ids[capIndices[i + 1]], c = ids[capIndices[i + 2]];
                layer.Index.Add(a); layer.Index.Add(side == 1 ? b : c); layer.Index.Add(side == 1 ? c : b);
            }
        }
    }

    private static (IReadOnlyList<Vector2> Points, IReadOnlyList<int> Indices) Earcut(IReadOnlyList<Vector2> ring)
    {
        var cap = PolygonTriangulator.Triangulate(new(ring));
        if (cap.Vertices.Length != ring.Count) throw new ArgumentException("A ring has collinear points; caps would not close.");
        return (cap.Vertices, cap.Indices);
    }

    /// <summary>Validates caller-supplied cap triangles: non-degenerate, together covering the ring's area; returns them counterclockwise.</summary>
    private static (IReadOnlyList<Vector2> Points, IReadOnlyList<int> Indices) Explicit(IReadOnlyList<Vector2> ring, IReadOnlyList<int> triangles, double area)
    {
        if (triangles.Count == 0 || triangles.Count % 3 != 0) throw new ArgumentException("Cap triangles must be index triples.");
        var ordered = new int[triangles.Count];
        double covered = 0;
        for (var i = 0; i < triangles.Count; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            if (a < 0 || b < 0 || c < 0 || a >= ring.Count || b >= ring.Count || c >= ring.Count) throw new ArgumentException("Cap triangle index out of range.");
            var pa = ring[a]; var pb = ring[b]; var pc = ring[c];
            var twice = ((double)pb.X - pa.X) * ((double)pc.Y - pa.Y) - ((double)pb.Y - pa.Y) * ((double)pc.X - pa.X);
            if (!(Math.Abs(twice) > 1e-9)) throw new ArgumentException($"Cap triangle {pa} {pb} {pc} is degenerate.");
            covered += Math.Abs(twice) * 0.5;
            ordered[i] = a; ordered[i + 1] = twice > 0 ? b : c; ordered[i + 2] = twice > 0 ? c : b;
        }
        if (Math.Abs(covered - area) > Math.Max(1e-7, area * 1e-5)) throw new ArgumentException($"Cap triangles cover {covered} of {area}.");
        return (ring, ordered);
    }

    private static int Vertex(Layer layer, Frame frame, Vector3 local, Vector3 normal, Vector2 uv)
    {
        var p = frame.Point(local); var n = frame.Direction(normal);
        layer.Position.Add(p.X); layer.Position.Add(p.Y); layer.Position.Add(p.Z);
        layer.Normal.Add(n.X); layer.Normal.Add(n.Y); layer.Normal.Add(n.Z);
        layer.Uv.Add(uv.X); layer.Uv.Add(uv.Y); layer.Ao.Add(1);
        return layer.Count - 1;
    }

    private static Vector3 P(Layer layer, int i) => new(layer.Position[3 * i], layer.Position[3 * i + 1], layer.Position[3 * i + 2]);

    private static void Triangle(Layer layer, int a, int b, int c, Vector3 outward)
    {
        var pa = P(layer, a);
        var direction = V3.Dot(V3.Cross(V3.Sub(P(layer, b), pa), V3.Sub(P(layer, c), pa)), outward);
        if (!double.IsFinite(direction) || direction == 0)
            throw new ArgumentException($"A face is degenerate: {pa} {P(layer, b)} {P(layer, c)} toward {outward}.");
        // Godot's front face is clockwise seen from outside, i.e. the triangle's right-hand normal points inward.
        layer.Index.Add(a); layer.Index.Add(direction < 0 ? b : c); layer.Index.Add(direction < 0 ? c : b);
    }

    private static void Quad(Layer layer, int a, int b, int c, int d, Vector3 outward)
    {
        Triangle(layer, a, b, c, outward); Triangle(layer, a, c, d, outward);
    }

    internal bool IsEmpty => _layers.Count == 0;

    /// <summary>
    /// One bundle per layer. <paramref name="bounds"/> is the part's declared Bounds; vertices outside it by more than float
    /// noise are a construction error, and noise-level excursions are clamped in place (shared positions stay shared).
    /// </summary>
    internal PartGeometry Finish(Box3 bounds)
    {
        if (_layers.Count == 0) throw new InvalidOperationException("A part must contain geometry.");
        var meshes = new List<GeometryBundle>(_layers.Count);
        foreach (var layer in _layers)
        {
            var position = layer.Position.ToArray();
            for (var i = 0; i < position.Length; i++)
            {
                var axis = i % 3;
                float lo = axis == 0 ? bounds.Min.X : axis == 1 ? bounds.Min.Y : bounds.Min.Z;
                float hi = axis == 0 ? bounds.Max.X : axis == 1 ? bounds.Max.Y : bounds.Max.Z;
                if (position[i] < lo - BoundsTolerance || position[i] > hi + BoundsTolerance)
                    throw new InvalidOperationException($"Part geometry leaves its bounds on axis {axis}: {position[i]} not in [{lo}, {hi}].");
                position[i] = Math.Clamp(position[i], lo, hi);
            }
            var flags = layer.Flags == 0 ? null : Enumerable.Repeat(layer.Flags, layer.Count).ToArray();
            var mesh = new GeometryBundle(new(layer.Material), position, layer.Normal.ToArray(), layer.Index.ToArray(), layer.Ao.ToArray(), layer.Uv.ToArray())
            { Flags = flags };
            mesh.ValidateVertexData();
            meshes.Add(mesh);
        }
        return new(meshes, bounds);
    }
}

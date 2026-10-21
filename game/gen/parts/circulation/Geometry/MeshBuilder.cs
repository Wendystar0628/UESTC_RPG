using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Circulation.Geometry;

/// <summary>
/// Collects closed solids from the game/gen/parts/core/ kernel and from profile extrusions into one surface per material and vertex
/// flag, in first-use order. Everything is local to the part; nothing here allocates engine objects.
/// </summary>
internal sealed class MeshBuilder
{
    private const float BoundsTolerance = 0.01f;

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
        var size = Vector3.Subtract(max, min);
        var smallest = Math.Min(size.X, Math.Min(size.Y, size.Z));
        var b = bevel > 0 && bevel < smallest * 0.45f ? bevel : 0;
        var box = PartSolids.BeveledBox(size.X, size.Y, size.Z, b, material);
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

        Vector3 Normal(Vector3 unit) => VectorMath.Unit(new Vector3(unit.X / radii.X, unit.Y / radii.Y, unit.Z / radii.Z));
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
        Vector3 Out(int a, int b, int c) => Vector3.Subtract(Vector3.Multiply(Vector3.Add(Vector3.Add(P(layer, a), P(layer, b)), P(layer, c)), 1f / 3), frame.Point(centre));
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
    /// Sweeps a closed (lateral, up) section along a polyline whose points carry their own height. The kernel's shared
    /// bounded plan joins keep positive lateral at (dz, −dx) of the travel direction; the section's up
    /// axis stays vertical on a sloping path (curbs and risers stay plumb), faces take their true normals, chamfer edges
    /// interpolate between neighbouring faces, and smooth sections (tubes) take normals from the actual section and run.
    /// Open ends are capped with caller-supplied triangles (section index triples) or the kernel's Earcut.
    /// </summary>
    internal void SweepPath(ToonSpec material, IReadOnlyList<Vector2> section, IReadOnlyList<bool>? chamfer, IReadOnlyList<int>? capTriangles,
        IReadOnlyList<Vector3> path, bool closed, Frame frame, byte flags = 0, bool smooth = false, float miterLimit = PartSweeps.DefaultMiterLimit)
    {
        ArgumentNullException.ThrowIfNull(section); ArgumentNullException.ThrowIfNull(path);
        int n = section.Count, m = path.Count;
        if (n < 3 || chamfer is not null && chamfer.Count != n) throw new ArgumentException("A section needs three points and one flag per edge.");
        if (m < (closed ? 3 : 2)) throw new ArgumentException("A sweep needs two points, or three for a closed path.");
        double area = 2d * PlanRings.SignedArea(section);
        if (!(Math.Abs(area) > 1e-12)) throw new ArgumentException("A section must enclose area.");
        var sign = area > 0 ? 1f : -1f;
        var edge = SectionEdgeNormals(section, sign);
        var joins = new PathJoins(path, closed, section.Min(p => p.X), section.Max(p => p.X), miterLimit);
        var tangent = joins.Tangents; var right = joins.Rights; var miter = joins.Miters;
        var strips = PathJoins.SectionEdges(new(section), joins.SplitCentre, out var sectionSplits);
        Vector3 Pos(int i, Vector2 p, bool outgoing) => joins.Position(i, p, outgoing);
        var layer = For(material, flags);
        float along = 0;
        for (var s = 0; s < tangent.Length; s++)
        {
            var j = (s + 1) % m;
            // Preserve the true grade normals and section chamfer interpolation of this writer.
            var faces = new Vector3[n];
            for (var k = 0; k < n; k++)
                faces[k] = Face(Pos(s, section[k], true), Pos(s, section[(k + 1) % n], true), Pos(j, section[k], false), Lift(right[s], edge[k]));
            var delta = new Vector2(path[j].X - path[s].X, path[j].Z - path[s].Z);
            var length = VectorMath.Length(delta);
            Vector3 Run(Vector2 p) => Pos(j, p, false) - Pos(s, p, true);
            var startNormals = smooth ? SmoothNormals(section, edge, joins, sign, s, true, Run) : null;
            var endNormals = smooth ? SmoothNormals(section, edge, joins, sign, j, false, Run) : null;
            foreach (var strip in strips)
            {
                var k = strip.Source;
                var (n0, n1) = StripNormals(section, strip, EndNormals(k, faces, startNormals, chamfer));
                var (n3, n2) = StripNormals(section, strip, EndNormals(k, faces, endNormals, chamfer));
                var outward = Face(Pos(s, strip.A, true), Pos(s, strip.B, true), Pos(j, strip.A, false), Lift(right[s], edge[k]));
                if (!smooth && (chamfer is null || !chamfer[k])) n0 = n1 = n2 = n3 = outward;
                var a = Vertex(layer, frame, Pos(s, strip.A, true), n0, new(along, strip.V0));
                var b = Vertex(layer, frame, Pos(s, strip.B, true), n1, new(along, strip.V1));
                var c = Vertex(layer, frame, Pos(j, strip.B, false), n2, new(along + length, strip.V1));
                var d = Vertex(layer, frame, Pos(j, strip.A, false), n3, new(along + length, strip.V0));
                StripQuad(layer, frame, a, b, c, d, outward);
            }
            along += length;
            if (joins.BevelTurns[j] == 0) continue;
            var advance = VectorMath.Length(new Vector2(right[j % right.Length].X - right[s].X, right[j % right.Length].Z - right[s].Z)) * section.Max(p => Math.Abs(p.X));
            var joinFaces = edge.Select(normal => VectorMath.Unit(Lift(miter[j], normal))).ToArray();
            Vector3 JoinRun(Vector2 p) => Pos(j, p, true) - Pos(j, p, false);
            var incomingNormals = smooth ? SmoothNormals(section, edge, joins, sign, j, false, JoinRun) : null;
            var outgoingNormals = smooth ? SmoothNormals(section, edge, joins, sign, j, true, JoinRun) : null;
            foreach (var strip in strips)
            {
                if (joins.BevelTurns[j] * strip.A.X <= 0 && joins.BevelTurns[j] * strip.B.X <= 0) continue;
                var (n0, n1) = StripNormals(section, strip, EndNormals(strip.Source, joinFaces, incomingNormals, chamfer));
                var (n3, n2) = StripNormals(section, strip, EndNormals(strip.Source, joinFaces, outgoingNormals, chamfer));
                var ai = Pos(j, strip.A, false); var bi = Pos(j, strip.B, false);
                var ao = Pos(j, strip.A, true); var bo = Pos(j, strip.B, true);
                var outward = Face(ai, bi, strip.B.X == 0 ? ao : bo, joinFaces[strip.Source]);
                if (!smooth && (chamfer is null || !chamfer[strip.Source])) n0 = n1 = n2 = n3 = outward;
                int V(Vector3 p, Vector3 normal, bool outgoing, float v) => Vertex(layer, frame, p, normal, new(along + (outgoing ? advance : 0), v));
                var a = V(ai, n0, false, strip.V0); var b = V(bi, n1, false, strip.V1);
                if (strip.A.X == 0) Triangle(layer, a, b, V(bo, n2, true, strip.V1), frame.Direction(outward));
                else if (strip.B.X == 0) Triangle(layer, a, b, V(ao, n3, true, strip.V0), frame.Direction(outward));
                else StripQuad(layer, frame, a, b, V(bo, n2, true, strip.V1), V(ao, n3, true, strip.V0), outward);
            }
            along += advance;
        }
        if (!closed) CapPath(layer, section, capTriangles, sectionSplits, Math.Abs(area) * 0.5, tangent, m, frame, (i, p) => Pos(i, p, true));
    }

    private static Vector3 Lift(Vector3 lateral, Vector2 normal) => new(lateral.X * normal.X, normal.Y, lateral.Z * normal.X);

    private static Vector3 Face(Vector3 a, Vector3 b, Vector3 d, Vector3 guess)
    {
        var cross = VectorMath.Cross(Vector3.Subtract(b, a), Vector3.Subtract(d, a));
        var normal = VectorMath.Dot(cross, cross) > 1e-20 ? VectorMath.Unit(cross) : guess;
        return VectorMath.Dot(normal, guess) < 0 ? Vector3.Multiply(normal, -1) : normal;
    }

    private static Vector3[] SmoothNormals(IReadOnlyList<Vector2> section, Vector2[] edge, PathJoins joins, float sign,
        int ring, bool outgoing, Func<Vector2, Vector3> run)
    {
        var n = section.Count;
        var normals = new Vector3[n];
        for (var k = 0; k < n; k++)
        {
            var previous = (k + n - 1) % n; var next = (k + 1) % n;
            var across = joins.Position(ring, section[next], outgoing) - joins.Position(ring, section[previous], outgoing);
            var guess = VectorMath.Unit(Lift(VectorMath.Unit(joins.Miters[ring]), edge[previous] + edge[k]));
            // The bounded ring changes the actual run independently at each lateral coordinate.
            // Its grade, including at a trimmed open end, must enter the radial normal as well.
            // Section order fixes orientation: the old planar radial guess can oppose the real
            // normal after a long inner cutback, so it must not independently flip ring normals.
            var cross = VectorMath.Cross(across, run(section[k]));
            normals[k] = VectorMath.Dot(cross, cross) > 1e-20
                ? Vector3.Multiply(VectorMath.Unit(cross), sign) : guess;
        }
        return normals;
    }

    private static (Vector3, Vector3) EndNormals(int k, Vector3[] faces, Vector3[]? smoothNormals, IReadOnlyList<bool>? chamfer)
    {
        var n = faces.Length;
        if (smoothNormals is not null) return (smoothNormals[k], smoothNormals[(k + 1) % n]);
        return chamfer is not null && chamfer[k] ? (faces[(k + n - 1) % n], faces[(k + 1) % n]) : (faces[k], faces[k]);
    }

    private static (Vector3, Vector3) StripNormals(IReadOnlyList<Vector2> section, PathJoins.SectionEdge strip, (Vector3 A, Vector3 B) normals)
    {
        var n = section.Count;
        var full = VectorMath.Length(section[(strip.Source + 1) % n] - section[strip.Source]);
        return (VectorMath.Unit(Vector3.Lerp(normals.A, normals.B, strip.V0 / full)),
            VectorMath.Unit(Vector3.Lerp(normals.A, normals.B, strip.V1 / full)));
    }

    private static void StripQuad(Layer layer, Frame frame, int a, int b, int c, int d, Vector3 outward)
    {
        var normal = frame.Direction(outward);
        if (PathJoins.OtherDiagonal(P(layer, a), P(layer, b), P(layer, c), P(layer, d), normal))
        { Triangle(layer, a, b, d, normal); Triangle(layer, b, c, d, normal); }
        else Quad(layer, a, b, c, d, normal);
    }

    private static void CapPath(Layer layer, IReadOnlyList<Vector2> section, IReadOnlyList<int>? capTriangles,
        IReadOnlyDictionary<(Vector2, Vector2), Vector2> splits, double area, Vector3[] tangent, int m, Frame frame, Func<int, Vector2, Vector3> Pos)
    {
        var (points, indices) = capTriangles is null ? Earcut(section) : Explicit(section, capTriangles, area);
        var (capPoints, capIndices) = PathJoins.SplitCap(points, indices, splits);
        for (var end = 0; end < 2; end++)
        {
            var i = end == 0 ? 0 : m - 1;
            var outward = end == 0 ? Vector3.Multiply(tangent[0], -1) : tangent[^1];
            var ids = new int[capPoints.Count];
            for (var k = 0; k < ids.Length; k++) ids[k] = Vertex(layer, frame, Pos(i, capPoints[k]), outward, capPoints[k]);
            for (var t = 0; t < capIndices.Count; t += 3)
                Triangle(layer, ids[capIndices[t]], ids[capIndices[t + 1]], ids[capIndices[t + 2]], frame.Direction(outward));
        }
    }

    private static Vector2[] SectionEdgeNormals(IReadOnlyList<Vector2> section, float sign)
    {
        var n = section.Count;
        // Outward section-edge normals in (lateral, up); counterclockwise means outward = (du, −dl).
        var edge = new Vector2[n];
        for (var k = 0; k < n; k++)
        {
            var d = section[(k + 1) % n] - section[k];
            var length = (float)Math.Sqrt(VectorMath.Dot(d, d));
            if (!(length > 1e-7f)) throw new ArgumentException("A section has a repeated point.");
            edge[k] = new(sign * d.Y / length, -sign * d.X / length);
        }
        return edge;
    }

    /// <summary>Kernel sweep laid into the vertical plane through x (see <see cref="Frame.VerticalPlane"/>).</summary>
    internal void VerticalSweep(ToonSpec material, SweepProfile profile, IReadOnlyList<Vector2> zyPath, float x, Frame frame, byte flags = 0) =>
        Add(PartSweeps.Sweep(profile, zyPath, material), Frame.VerticalPlane(x).Then(frame), flags);

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
        double area = 2d * PlanRings.SignedArea(ring);
        if (!(Math.Abs(area) > 1e-12)) throw new ArgumentException("A ring must enclose area.");
        var (capPoints, capIndices) = capTriangles is null ? Earcut(ring) : Explicit(ring, capTriangles, Math.Abs(area) * 0.5);
        var sign = area > 0 ? 1f : -1f;
        var normals = new Vector3[n];
        for (var i = 0; i < n; i++)
        {
            var d = ring[(i + 1) % n] - ring[i];
            var length = (float)Math.Sqrt(VectorMath.Dot(d, d));
            if (!(length > 1e-7f)) throw new ArgumentException("A ring has a repeated point.");
            // Ring (z, y) is counterclockwise when area > 0; its outward normal is (dy, −dz).
            normals[i] = new(0, -sign * d.X / length, sign * d.Y / length);
        }
        var layer = For(material, flags);
        var start = layer.Count;

        float along = 0;
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var bevel = chamfer is not null && chamfer[i];
            var ni = bevel ? normals[(i + n - 1) % n] : normals[i];
            var nj = bevel ? normals[j] : normals[i];
            var length = (float)Math.Sqrt(VectorMath.Dot(ring[j] - ring[i], ring[j] - ring[i]));
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
            var twice = PlanRings.Cross(pa, pb, pc);
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
        var flip = GeometryWinding.Flip(P(layer, a), P(layer, b), P(layer, c), outward);
        layer.Index.Add(a); layer.Index.Add(flip ? c : b); layer.Index.Add(flip ? b : c);
    }

    private static void Quad(Layer layer, int a, int b, int c, int d, Vector3 outward)
    {
        Triangle(layer, a, b, c, outward); Triangle(layer, a, c, d, outward);
    }

    internal bool IsEmpty => _layers.Count == 0;

    /// <summary>Path-built geometry takes its bounds from the actual final vertices, including cut joins.</summary>
    internal PartGeometry Finish()
    {
        if (_layers.Count == 0) throw new InvalidOperationException("A part must contain geometry.");
        var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
        foreach (var layer in _layers)
            for (var i = 0; i < layer.Count; i++)
            { var p = P(layer, i); min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        return Finish(new(min, max));
    }

    /// <summary>
    /// One bundle per layer. Vertices outside the declared bounds by more than float noise are a construction error;
    /// noise-level excursions are clamped in place (shared positions stay shared).
    /// </summary>
    internal PartGeometry Finish(Box3 bounds)
    {
        if (_layers.Count == 0) throw new InvalidOperationException("A part must contain geometry.");
        var sink = new GeometrySink();
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
            var vertices = new GeometryVertex[layer.Count];
            for (var i = 0; i < vertices.Length; i++)
                vertices[i] = new(new(position[3 * i], position[3 * i + 1], position[3 * i + 2]),
                    new(layer.Normal[3 * i], layer.Normal[3 * i + 1], layer.Normal[3 * i + 2]),
                    new Vector2(layer.Uv[2 * i], layer.Uv[2 * i + 1]), Ao: layer.Ao[i], Flags: layer.Flags == 0 ? null : layer.Flags);
            sink.Polygon(layer.Material, vertices, layer.Index);
        }
        return new(sink.Finish(), bounds);
    }
}

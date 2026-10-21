using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// Proper rigid placement inside one part: the images of the unit axes plus an origin.
/// the output is reproducible for the same input on the same machine (D-232).
/// </summary>
internal readonly record struct Place(Vector3 X, Vector3 Y, Vector3 Z, Vector3 Origin)
{
    public static Place Identity { get; } = new(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, Vector3.Zero);
    public static Place At(float x, float y, float z) => Identity with { Origin = new(x, y, z) };

    /// <summary>Kernel sweeps run in X/Z with the section's up along Y: map path Z to facade Y and section up to −Z from <paramref name="zBack"/>.</summary>
    public static Place Facade(float zBack) => new(Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY, new(0, 0, zBack));

    /// <summary>Kernel extrusions run along Y over an X/Z footprint: map extrusion to X, footprint (z, y) to the facade section.</summary>
    public static Place AlongX(float x0) => new(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, new(x0, 0, 0));

    /// <summary>Rotation about the vertical axis through <paramref name="pivot"/>; positive turns +X towards −Z.</summary>
    public static Place TurnY(float cos, float sin, Vector3 pivot)
    {
        var x = new Vector3(cos, 0, -sin);
        var z = new Vector3(sin, 0, cos);
        return new(x, Vector3.UnitY, z, new(pivot.X - (pivot.X * x.X + pivot.Z * z.X), 0, pivot.Z - (pivot.X * x.Z + pivot.Z * z.Z)));
    }

    public Vector3 Point(Vector3 p) => new(
        Origin.X + p.X * X.X + p.Y * Y.X + p.Z * Z.X,
        Origin.Y + p.X * X.Y + p.Y * Y.Y + p.Z * Z.Y,
        Origin.Z + p.X * X.Z + p.Y * Y.Z + p.Z * Z.Z);

    public Vector3 Direction(Vector3 n) => new(
        n.X * X.X + n.Y * Y.X + n.Z * Z.X,
        n.X * X.Y + n.Y * Y.Y + n.Z * Z.Y,
        n.X * X.Z + n.Y * Y.Z + n.Z * Z.Z);

    /// <summary>This placement followed by <paramref name="outer"/>.</summary>
    public Place Then(Place outer) => new(outer.Direction(X), outer.Direction(Y), outer.Direction(Z), outer.Point(Origin));
}

/// <summary>
/// Collects the solids of one part, one surface per material (and shadow flag), in a stable order.
/// Kernel solids keep their topology; this class only places them and rewrites AO, flags and glass data.
/// </summary>
internal sealed class PartMesh
{
    private readonly List<Layer> _layers = [];

    public Layer Get(ToonSpec spec, byte flags = 0)
    {
        foreach (var layer in _layers)
            if (layer.Flags == flags && layer.Spec == spec) return layer;
        var created = new Layer(spec, flags);
        _layers.Add(created);
        return created;
    }

    /// <summary>Adds kernel geometry, passing its AO through: the kernel already writes visibility (1 = open face).</summary>
    public void Solid(PartGeometry geometry, Place at, ToonSpec spec, byte flags)
    {
        var layer = Get(spec, flags);
        foreach (var mesh in geometry.Meshes)
        {
            var count = mesh.Position.Length / 3;
            var first = layer.Count;
            for (var i = 0; i < count; i++)
            {
                var p = at.Point(new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]));
                var n = at.Direction(new(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]));
                var uv = mesh.Uv is null ? Vector2.Zero : new Vector2(mesh.Uv[2 * i], mesh.Uv[2 * i + 1]);
                layer.Vertex(p, n, uv, mesh.Ao?[i] ?? 1f);
            }
            foreach (var index in mesh.Index) layer.RawIndex(first + index);
        }
    }

    public PartGeometry Finish(Box3 bounds)
    {
        var sink = new GeometrySink();
        foreach (var layer in _layers) layer.Write(sink);
        return new(sink.Finish(), bounds);
    }

    internal sealed class Layer(ToonSpec spec, byte flags)
    {
        private readonly List<GeometryVertex> _vertices = [];
        private readonly List<int> _index = [];
        public ToonSpec Spec { get; } = spec;
        public byte Flags { get; } = flags;
        public int Count => _vertices.Count;

        public int Vertex(Vector3 p, Vector3 n, Vector2 uv, float ao = 1f, Vector2 uv2 = default, byte reserved = 0)
        {
            var length = (float)Math.Sqrt((double)n.X * n.X + (double)n.Y * n.Y + (double)n.Z * n.Z);
            if (!(length > 1e-12f)) throw new ArgumentException("A vertex normal must be nonzero.", nameof(n));
            var glass = Spec.Kind == ToonKind.Glass;
            _vertices.Add(new(p, n / length, uv, glass ? uv2 : null, ao, Flags: Flags, Reserved: glass ? reserved : null));
            return Count - 1;
        }

        public void RawIndex(int index) => _index.Add(index);

        public void Triangle(int a, int b, int c, Vector3 outward)
        {
            var flip = GeometryWinding.Flip(_vertices[a].Position, _vertices[b].Position, _vertices[c].Position, outward);
            _index.Add(a); _index.Add(flip ? c : b); _index.Add(flip ? b : c);
        }

        public void Quad(int a, int b, int c, int d, Vector3 outward)
        { Triangle(a, b, c, outward); Triangle(a, c, d, outward); }

        public void Write(GeometrySink sink)
        {
            if (_index.Count > 0) sink.Polygon(Spec, _vertices, _index);
        }
    }
}

/// <summary>Placed kernel solids (game/gen/parts/core/) in the opening frame: boxes, mitred rings, slabs with holes and X-extruded prisms.</summary>
internal static class Solid
{
    // The kernel needs a material; the openings re-layer every solid, so this one is never emitted.
    private static readonly ToonSpec Carrier = OpeningStyle.Spec("wall");

    public static void Box(PartMesh mesh, ToonSpec spec, byte flags, float x0, float y0, float z0, float x1, float y1, float z1,
        float bevel = 0f)
    {
        float w = x1 - x0, h = y1 - y0, d = z1 - z0;
        var geometry = PartSolids.BeveledBox(w, h, d, Bevel(bevel, Math.Min(w, Math.Min(h, d))), Carrier);
        mesh.Solid(geometry, Place.At((x0 + x1) * 0.5f, y0, (z0 + z1) * 0.5f), spec, flags);
    }

    /// <summary>Box turned about a vertical pivot (door leaves and their fittings).</summary>
    public static void Box(PartMesh mesh, ToonSpec spec, byte flags, Place turn, float x0, float y0, float z0, float x1, float y1, float z1,
        float bevel = 0f)
    {
        float w = x1 - x0, h = y1 - y0, d = z1 - z0;
        var geometry = PartSolids.BeveledBox(w, h, d, Bevel(bevel, Math.Min(w, Math.Min(h, d))), Carrier);
        mesh.Solid(geometry, Place.At((x0 + x1) * 0.5f, y0, (z0 + z1) * 0.5f).Then(turn), spec, flags);
    }

    /// <summary>Closed rectangular ring of members <paramref name="face"/> wide whose outer edge is the given rectangle.</summary>
    public static void Ring(PartMesh mesh, ToonSpec spec, byte flags, float x0, float y0, float x1, float y1, float face,
        float zFront, float zBack, float bevel = 0f, Place? turn = null)
    {
        var h = face * 0.5f;
        Vector2[] path = [new(x0 + h, y0 + h), new(x1 - h, y0 + h), new(x1 - h, y1 - h), new(x0 + h, y1 - h)];
        var profile = PartSweeps.BeveledRectangleProfile(face, zBack - zFront, Bevel(bevel, Math.Min(face, zBack - zFront)));
        var geometry = PartSweeps.Sweep(profile, path, Carrier, closed: true);
        var at = Place.Facade(zBack);
        mesh.Solid(geometry, turn is { } t ? at.Then(t) : at, spec, flags);
    }

    /// <summary>Prism along X from <paramref name="x0"/> to <paramref name="x1"/> over a convex section given as (z, y) points.</summary>
    public static void Prism(PartMesh mesh, ToonSpec spec, byte flags, IReadOnlyList<Vector2> sectionZY, float x0, float x1)
    {
        var geometry = PartSolids.Extrude(new PolygonRings(sectionZY), x1 - x0, Carrier);
        mesh.Solid(geometry, Place.AlongX(x0), spec, flags);
    }

    /// <summary>
    /// Closed glass slab for leaves and panels that move or stand alone (toon_glass). Both large faces carry pane
    /// coordinates (UV in [0, 1)), UV2 = (window base, channel + 32 × columns) and the card type in the spare byte,
    /// as game/gen/buildings/ writes them (WLD-INTERIORS).
    /// </summary>
    public static void Glass(PartMesh mesh, byte flags, Place turn, float x0, float y0, float z0, float x1, float y1, float z1,
        byte card, Vector2 uv2)
    {
        var layer = mesh.Get(OpeningStyle.Glass, flags);
        const float m = 0.002f;
        Vector2 Pane(float x, float y) => new(m + (1 - 2 * m) * (x - x0) / (x1 - x0), m + (1 - 2 * m) * (y - y0) / (y1 - y0));
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, bool pane)
        {
            var nn = turn.Direction(n);
            int V(Vector3 p) => layer.Vertex(turn.Point(p), nn, pane ? Pane(p.X, p.Y) : new(0.5f, 0.5f), 1f, uv2, card);
            layer.Quad(V(a), V(b), V(c), V(d), nn);
        }
        Face(new(x0, y0, z0), new(x1, y0, z0), new(x1, y1, z0), new(x0, y1, z0), -Vector3.UnitZ, true);
        Face(new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1), Vector3.UnitZ, true);
        Face(new(x0, y0, z0), new(x0, y1, z0), new(x0, y1, z1), new(x0, y0, z1), -Vector3.UnitX, false);
        Face(new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1), new(x1, y0, z1), Vector3.UnitX, false);
        Face(new(x0, y0, z0), new(x1, y0, z0), new(x1, y0, z1), new(x0, y0, z1), -Vector3.UnitY, false);
        Face(new(x0, y1, z0), new(x1, y1, z0), new(x1, y1, z1), new(x0, y1, z1), Vector3.UnitY, false);
    }

    /// <summary>
    /// Flush rectilinear slab between <paramref name="zFront"/> and <paramref name="zBack"/> on the grid of breakpoints
    /// <paramref name="xs"/> × <paramref name="ys"/>; cell (i, j) is solid when <paramref name="solid"/> says so. Every face
    /// is cut at the grid lines, so caps and sides share their vertices: watertight, without the T-junctions that crack and
    /// stipple the outline (ART-MODELING). Used for frames with mullions and transoms, and for leaf stiles and rails.
    /// </summary>
    public static void Grid(PartMesh mesh, ToonSpec spec, byte flags, Place turn, IReadOnlyList<float> xs, IReadOnlyList<float> ys,
        Func<int, int, bool> solid, float zFront, float zBack)
    {
        var layer = mesh.Get(spec, flags);
        bool Solid(int i, int j) => i >= 0 && j >= 0 && i < xs.Count - 1 && j < ys.Count - 1 && solid(i, j);
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Vector2 uv0, Vector2 uv1)
        {
            var nn = turn.Direction(n);
            layer.Quad(layer.Vertex(turn.Point(a), nn, uv0), layer.Vertex(turn.Point(b), nn, new(uv1.X, uv0.Y)),
                layer.Vertex(turn.Point(c), nn, uv1), layer.Vertex(turn.Point(d), nn, new(uv0.X, uv1.Y)), nn);
        }
        for (var j = 0; j < ys.Count - 1; j++)
            for (var i = 0; i < xs.Count - 1; i++)
            {
                if (!Solid(i, j)) continue;
                float x0 = xs[i], x1 = xs[i + 1], y0 = ys[j], y1 = ys[j + 1];
                Face(new(x0, y0, zFront), new(x1, y0, zFront), new(x1, y1, zFront), new(x0, y1, zFront), -Vector3.UnitZ, new(x0, y0), new(x1, y1));
                Face(new(x0, y0, zBack), new(x1, y0, zBack), new(x1, y1, zBack), new(x0, y1, zBack), Vector3.UnitZ, new(x0, y0), new(x1, y1));
                var depth = new Vector2(0, zBack - zFront);
                if (!Solid(i - 1, j)) Face(new(x0, y0, zFront), new(x0, y1, zFront), new(x0, y1, zBack), new(x0, y0, zBack), -Vector3.UnitX, new(y0, 0), new(y1, depth.Y));
                if (!Solid(i + 1, j)) Face(new(x1, y0, zFront), new(x1, y1, zFront), new(x1, y1, zBack), new(x1, y0, zBack), Vector3.UnitX, new(y0, 0), new(y1, depth.Y));
                if (!Solid(i, j - 1)) Face(new(x0, y0, zFront), new(x1, y0, zFront), new(x1, y0, zBack), new(x0, y0, zBack), -Vector3.UnitY, new(x0, 0), new(x1, depth.Y));
                if (!Solid(i, j + 1)) Face(new(x0, y1, zFront), new(x1, y1, zFront), new(x1, y1, zBack), new(x0, y1, zBack), Vector3.UnitY, new(x0, 0), new(x1, depth.Y));
            }
    }

    /// <summary>Sorted, de-duplicated breakpoints for <see cref="Grid"/>.</summary>
    public static float[] Breaks(IEnumerable<float> values) => values.Distinct().OrderBy(v => v).ToArray();

    /// <summary>Kernel slab with rectangular holes (vision panels), turned like a leaf.</summary>
    public static void Panel(PartMesh mesh, ToonSpec spec, byte flags, Place turn, float x0, float y0, float x1, float y1, float zMid,
        float thickness, IReadOnlyList<IReadOnlyList<Vector2>> holes)
    {
        var geometry = PartSolids.WallPanel(x1 - x0, y1 - y0, thickness, holes, Carrier);
        mesh.Solid(geometry, Place.At((x0 + x1) * 0.5f, y0, zMid).Then(turn), spec, flags);
    }

    /// <summary>
    /// Closed corrugated sheet of constant thickness: z = <paramref name="zCentre"/> + amp·cos(2π(u − u0)/pitch) across u,
    /// straight along v. <paramref name="uAlongX"/> puts u on X (curtain pleats) or on Y (shutter slats).
    /// Both faces have smooth analytic normals; the four edges are flat (ART-MODELING "curved surfaces use soft edges").
    /// <paramref name="shadeAmp"/> is the amplitude the normals follow: larger than <paramref name="amp"/>, a shallow sheet
    /// still shades like deep folds ("segments follow the silhouette, shading follows the normals").
    /// </summary>
    public static void Wave(PartMesh mesh, ToonSpec spec, byte flags, bool uAlongX, float u0, float u1, float v0, float v1,
        float zCentre, float thickness, float amp, float pitch, int segmentsPerPitch, float? shadeAmp = null)
    {
        var normalAmp = shadeAmp ?? amp;
        var layer = mesh.Get(spec, flags);
        var count = Math.Max(1, (int)Math.Round((u1 - u0) / pitch * segmentsPerPitch));
        var uAxis = uAlongX ? Vector3.UnitX : Vector3.UnitY;
        var vAxis = uAlongX ? Vector3.UnitY : Vector3.UnitX;
        Vector3 P(float u, float v, float z) => uAlongX ? new(u, v, z) : new(v, u, z);
        var us = new float[count + 1];
        var zs = new float[count + 1];
        var slopes = new float[count + 1];
        for (var k = 0; k <= count; k++)
        {
            var u = k == count ? u1 : u0 + (u1 - u0) * k / count;
            var phase = 2 * Math.PI * (u - u0) / pitch;
            us[k] = u;
            zs[k] = zCentre + amp * (float)Math.Cos(phase);
            slopes[k] = -normalAmp * (float)(2 * Math.PI / pitch * Math.Sin(phase));
        }
        var t = thickness * 0.5f;
        foreach (var side in new[] { -1f, 1f })
        {
            var outward = side * Vector3.UnitZ;
            int prev0 = -1, prev1 = -1;
            for (var k = 0; k <= count; k++)
            {
                // Front normal (side −1) is slope·U − Z; the back is its mirror.
                var n = -side * slopes[k] * uAxis + side * Vector3.UnitZ;
                var a = layer.Vertex(P(us[k], v0, zs[k] + side * t), n, new(us[k], v0));
                var b = layer.Vertex(P(us[k], v1, zs[k] + side * t), n, new(us[k], v1));
                if (k > 0) layer.Quad(prev0, a, b, prev1, outward);
                prev0 = a; prev1 = b;
            }
        }
        foreach (var (k, sign) in new[] { (0, -1f), (count, 1f) })
        {
            var n = sign * uAxis;
            var q0 = layer.Vertex(P(us[k], v0, zs[k] - t), n, Vector2.Zero);
            var q1 = layer.Vertex(P(us[k], v0, zs[k] + t), n, Vector2.Zero);
            var q2 = layer.Vertex(P(us[k], v1, zs[k] + t), n, Vector2.Zero);
            var q3 = layer.Vertex(P(us[k], v1, zs[k] - t), n, Vector2.Zero);
            layer.Quad(q0, q1, q2, q3, n);
        }
        foreach (var (v, sign) in new[] { (v0, -1f), (v1, 1f) })
        {
            var n = sign * vAxis;
            int prevFront = -1, prevBack = -1;
            for (var k = 0; k <= count; k++)
            {
                var front = layer.Vertex(P(us[k], v, zs[k] - t), n, new(us[k], 0));
                var back = layer.Vertex(P(us[k], v, zs[k] + t), n, new(us[k], thickness));
                if (k > 0) layer.Quad(prevFront, front, back, prevBack, n);
                prevFront = front; prevBack = back;
            }
        }
    }

    private static float Bevel(float bevel, float smallest) => bevel <= 0f ? 0f : Math.Min(bevel, smallest * 0.3f);
}

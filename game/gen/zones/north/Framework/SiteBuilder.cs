using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Zones.North.Framework;

/// <summary>How much of a site's geometry one level carries: L0 everything, L1 without fine parts, L2 and coarser masses only.</summary>
public enum SiteDetail { Full, Plain, Mass }

/// <summary>One output of a site, filed under the cell of its plan anchor (world x, z).</summary>
internal sealed record SiteItem(Vector2 Anchor, GeometryBundle? Mesh, PlacedPart? Part);

/// <summary>
/// Collects a site's world-space geometry and placed catalogue parts for one level. Sites build in world space through
/// <see cref="PlanFrame"/> matrices; the module files every item under the cell of its anchor, so a site may span cells.
/// Long runs of small boxes (fence bars, posts, rails) go through <see cref="Box"/>, which batches them per cell and
/// material instead of one bundle each. Transforms are written out in scalar arithmetic so the bytes do not depend on SIMD.
/// </summary>
public sealed class SiteBuilder
{
    private readonly List<SiteItem> _items = [];
    private readonly List<(int X, int Z, ToonSpec Material, bool NoShadow)> _batchKeys = [];
    private readonly Dictionary<(int X, int Z, ToonSpec Material, bool NoShadow), BoxBatch> _batches = [];

    internal SiteBuilder(BuildContext context, LodLevel lod)
    {
        Context = context;
        Lod = lod;
    }

    public BuildContext Context { get; }
    public LodLevel Lod { get; }

    public SiteDetail Detail => Lod switch
    {
        LodLevel.L0 => SiteDetail.Full,
        LodLevel.L1 => SiteDetail.Plain,
        LodLevel.L2 or LodLevel.L3 or LodLevel.L4 => SiteDetail.Mass,
        _ => throw new ArgumentOutOfRangeException(nameof(Lod), Lod, "Unknown LOD level."),
    };

    /// <summary>Ground height from the map terrain (flat stub until the WP-08 evaluator lands).</summary>
    public float Ground(float x, float z) => Context.World.Terrain.HeightAt(x, z);

    public float Ground(Vector2 p) => Ground(p.X, p.Y);

    /// <summary>Items in build order, batched boxes last in first-use order.</summary>
    internal IReadOnlyList<SiteItem> Items
    {
        get
        {
            var all = new List<SiteItem>(_items);
            foreach (var key in _batchKeys) all.Add(_batches[key].Finish());
            return all;
        }
    }

    /// <summary>Adds part geometry placed by a row-vector matrix (rigid or uniformly scaled).</summary>
    public void Add(PartGeometry geometry, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        foreach (var mesh in geometry.Meshes) Add(mesh, at);
    }

    public void Add(GeometryBundle mesh, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var count = mesh.Position.Length / 3;
        var position = new float[mesh.Position.Length];
        var normal = new float[mesh.Normal.Length];
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        for (var i = 0; i < count; i++)
        {
            float x = mesh.Position[3 * i], y = mesh.Position[3 * i + 1], z = mesh.Position[3 * i + 2];
            var wx = x * at.M11 + y * at.M21 + z * at.M31 + at.M41;
            var wy = x * at.M12 + y * at.M22 + z * at.M32 + at.M42;
            var wz = x * at.M13 + y * at.M23 + z * at.M33 + at.M43;
            position[3 * i] = wx; position[3 * i + 1] = wy; position[3 * i + 2] = wz;
            minX = Math.Min(minX, wx); maxX = Math.Max(maxX, wx); minZ = Math.Min(minZ, wz); maxZ = Math.Max(maxZ, wz);
            float nx = mesh.Normal[3 * i], ny = mesh.Normal[3 * i + 1], nz = mesh.Normal[3 * i + 2];
            var tx = nx * at.M11 + ny * at.M21 + nz * at.M31;
            var ty = nx * at.M12 + ny * at.M22 + nz * at.M32;
            var tz = nx * at.M13 + ny * at.M23 + nz * at.M33;
            var length = MathF.Sqrt(tx * tx + ty * ty + tz * tz);
            if (length > 1e-12f) { tx /= length; ty /= length; tz /= length; }
            normal[3 * i] = tx; normal[3 * i + 1] = ty; normal[3 * i + 2] = tz;
        }
        var placed = mesh with { Position = position, Normal = normal };
        _items.Add(new(new((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f), placed, null));
    }

    /// <summary>Places a catalogue part (PartCatalog); it shows as a box until its owner's geometry lands (ENG-ARCH §5).</summary>
    public void Place(string part, BoxPartParams parameters, Matrix4x4 at, PaletteToken? tint = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _items.Add(new(new(at.M41, at.M43), null, new PlacedPart(new(part), parameters, at, tint)));
    }

    /// <summary>
    /// A plain box in world space, batched with others of the same material in its cell: centre <paramref name="c"/> on the
    /// plan, length along unit <paramref name="u"/>, depth across it, from y0 to y1. Caps off leaves the top and bottom
    /// open (bars hidden by rails); noShadow sets the no-shadow vertex flag (thin bars, ART-MODELING §7.4).
    /// </summary>
    public void Box(ToonSpec material, Vector2 c, Vector2 u, float length, float depth, float y0, float y1, bool caps = true, bool noShadow = false)
    {
        ArgumentNullException.ThrowIfNull(material);
        var size = 64f * (1 << (int)Lod);
        var key = ((int)MathF.Floor(c.X / size), (int)MathF.Floor(c.Y / size), material, noShadow);
        if (!_batches.TryGetValue(key, out var batch) || batch.Vertices > BoxBatch.MaxVertices)
        {
            // A full batch is sealed into the item list and a fresh one takes its key.
            if (batch is not null) _items.Add(batch.Finish());
            else _batchKeys.Add(key);
            batch = new BoxBatch(material, noShadow, new((key.Item1 + 0.5f) * size, (key.Item2 + 0.5f) * size));
            _batches[key] = batch;
        }
        batch.Box(c, u, length * 0.5f, depth * 0.5f, y0, y1, caps);
    }

    /// <summary>A box between two plan points (a rail or bar run), depth across the run.</summary>
    public void Run(ToonSpec material, Vector2 a, Vector2 b, float depth, float y0, float y1, bool caps = true, bool noShadow = false)
    {
        var d = b - a;
        var length = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
        if (length < 1e-4f) return;
        Box(material, (a + b) * 0.5f, d / length, length, depth, y0, y1, caps, noShadow);
    }

    /// <summary>World-space box faces of one material and cell, written with the kernel's winding (clockwise from outside).</summary>
    private sealed class BoxBatch(ToonSpec material, bool noShadow, Vector2 anchor)
    {
        /// <summary>Sealed past this many vertices so a bundle stays under the 65,536-vertex limit.</summary>
        public const int MaxVertices = 60000;

        private readonly List<float> _p = [], _n = [], _uv = [];
        private readonly List<int> _i = [];

        public int Vertices => _p.Count / 3;

        public void Box(Vector2 c, Vector2 u, float hx, float hz, float y0, float y1, bool caps)
        {
            var w = new Vector2(-u.Y, u.X);
            Vector2 P(float sx, float sz) => new(c.X + u.X * hx * sx + w.X * hz * sz, c.Y + u.Y * hx * sx + w.Y * hz * sz);
            var a = P(-1, -1); var b = P(1, -1); var d = P(1, 1); var e = P(-1, 1);
            // Sides: −Z (a→b), +X (b→d), +Z (d→e), −X (e→a).
            Side(a, b, y0, y1, new(-w.X, 0, -w.Y));
            Side(b, d, y0, y1, new(u.X, 0, u.Y));
            Side(d, e, y0, y1, new(w.X, 0, w.Y));
            Side(e, a, y0, y1, new(-u.X, 0, -u.Y));
            if (!caps) return;
            Cap(a, b, d, e, y1, 1);
            Cap(a, b, d, e, y0, -1);
        }

        private void Side(Vector2 p, Vector2 q, float y0, float y1, Vector3 n)
        {
            var length = Vector2.Distance(p, q);
            var v0 = Vertex(new(p.X, y0, p.Y), n, new(0, y0));
            var v1 = Vertex(new(q.X, y0, q.Y), n, new(length, y0));
            var v2 = Vertex(new(q.X, y1, q.Y), n, new(length, y1));
            var v3 = Vertex(new(p.X, y1, p.Y), n, new(0, y1));
            Quad(v0, v1, v2, v3, n);
        }

        private void Cap(Vector2 a, Vector2 b, Vector2 d, Vector2 e, float y, float sign)
        {
            var n = new Vector3(0, sign, 0);
            var v0 = Vertex(new(a.X, y, a.Y), n, a);
            var v1 = Vertex(new(b.X, y, b.Y), n, b);
            var v2 = Vertex(new(d.X, y, d.Y), n, d);
            var v3 = Vertex(new(e.X, y, e.Y), n, e);
            Quad(v0, v1, v2, v3, n);
        }

        private int Vertex(Vector3 p, Vector3 n, Vector2 uv)
        {
            _p.Add(p.X); _p.Add(p.Y); _p.Add(p.Z);
            _n.Add(n.X); _n.Add(n.Y); _n.Add(n.Z);
            _uv.Add(uv.X); _uv.Add(uv.Y);
            return _p.Count / 3 - 1;
        }

        private void Quad(int a, int b, int c, int d, Vector3 outward)
        {
            Triangle(a, b, c, outward);
            Triangle(a, c, d, outward);
        }

        private void Triangle(int a, int b, int c, Vector3 outward)
        {
            var pa = Pos(a);
            var e1 = Pos(b) - pa;
            var e2 = Pos(c) - pa;
            var cross = new Vector3(e1.Y * e2.Z - e1.Z * e2.Y, e1.Z * e2.X - e1.X * e2.Z, e1.X * e2.Y - e1.Y * e2.X);
            var dot = (double)cross.X * outward.X + (double)cross.Y * outward.Y + (double)cross.Z * outward.Z;
            if (dot == 0) return;
            _i.Add(a);
            _i.Add(dot < 0 ? b : c);
            _i.Add(dot < 0 ? c : b);
        }

        private Vector3 Pos(int i) => new(_p[3 * i], _p[3 * i + 1], _p[3 * i + 2]);

        public SiteItem Finish()
        {
            var count = _p.Count / 3;
            var ao = new float[count];
            Array.Fill(ao, 1f);
            var mesh = new GeometryBundle(new(material), _p.ToArray(), _n.ToArray(), _i.ToArray(), ao, _uv.ToArray());
            if (noShadow)
            {
                var flags = new byte[count];
                Array.Fill(flags, (byte)8);
                mesh = mesh with { Flags = flags };
            }
            mesh.ValidateVertexData();
            return new(anchor, mesh, null);
        }
    }
}

/// <summary>Collects a site's colliders, each filed under the 64 m cell of its anchor.</summary>
public sealed class SiteColliders
{
    private readonly List<(Vector2 Anchor, Collider Collider)> _items = [];

    internal SiteColliders(BuildContext context) => Context = context;

    public BuildContext Context { get; }

    public float Ground(float x, float z) => Context.World.Terrain.HeightAt(x, z);

    public float Ground(Vector2 p) => Ground(p.X, p.Y);

    internal IReadOnlyList<(Vector2 Anchor, Collider Collider)> Items => _items;

    public void Add(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        _items.Add((AnchorOf(collider), collider));
    }

    /// <summary>A box collider from a placement matrix of its centre and its half extents in that frame.</summary>
    public void Box(Matrix4x4 centre, Vector3 half, SurfaceTag tag, Contracts.Audio.FootMat foot) =>
        Add(new BoxCollider(centre, half, tag) { Foot = foot });

    /// <summary>A thin wall collider between two plan points.</summary>
    public void Wall(Vector2 a, Vector2 b, float halfThickness, float y0, float y1, SurfaceTag tag, Contracts.Audio.FootMat foot) =>
        Add(new WallCollider(a, b, halfThickness, y0, y1, tag) { Foot = foot });

    /// <summary>Adds part-local colliders (a part's Collision or a circulation helper's Collide) placed by a rigid matrix.</summary>
    public void Add(IReadOnlyList<Collider> local, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(local);
        foreach (var collider in local) Add(Transform(collider, at));
    }

    private static Collider Transform(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = Affine.Mul(box.At, at) },
        RampCollider ramp => ramp with { P0 = Point(ramp.P0, at), P1 = Point(ramp.P1, at), P2 = Point(ramp.P2, at), P3 = Point(ramp.P3, at) },
        WallCollider wall => wall with { A = Plan(wall.A, at), B = Plan(wall.B, at), Y0 = wall.Y0 + at.M42, Y1 = wall.Y1 + at.M42 },
        PostCollider post => post with { C = Plan(post.C, at), Y0 = post.Y0 + at.M42, Y1 = post.Y1 + at.M42 },
        _ => throw new ArgumentException("Unsupported collider kind for a placed part.", nameof(collider)),
    };

    private static Vector3 Point(Vector3 p, Matrix4x4 at) => new(
        p.X * at.M11 + p.Y * at.M21 + p.Z * at.M31 + at.M41,
        p.X * at.M12 + p.Y * at.M22 + p.Z * at.M32 + at.M42,
        p.X * at.M13 + p.Y * at.M23 + p.Z * at.M33 + at.M43);

    private static Vector2 Plan(Vector2 p, Matrix4x4 at) => new(
        p.X * at.M11 + p.Y * at.M31 + at.M41,
        p.X * at.M13 + p.Y * at.M33 + at.M43);

    private static Vector2 AnchorOf(Collider collider) => collider switch
    {
        BoxCollider box => new(box.At.M41, box.At.M43),
        WallCollider wall => (wall.A + wall.B) * 0.5f,
        PostCollider post => post.C,
        RampCollider ramp => new((ramp.P0.X + ramp.P2.X) * 0.5f, (ramp.P0.Z + ramp.P2.Z) * 0.5f),
        SlabCollider slab => slab.Ring[0],
        SlopedWallCollider sloped => (sloped.A + sloped.B) * 0.5f,
        _ => throw new ArgumentException("Unsupported collider kind for a zone site.", nameof(collider)),
    };
}

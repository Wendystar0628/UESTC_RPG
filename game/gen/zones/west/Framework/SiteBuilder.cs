using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Zones.West.Framework;

/// <summary>How much of a site's geometry one level carries: L0 everything, L1 without fine parts, L2 and coarser masses only.</summary>
public enum SiteDetail { Full, Plain, Mass }

/// <summary>One output of a site, filed under the cell of its plan anchor (world x, z).</summary>
internal sealed record SiteItem(Vector2 Anchor, GeometryBundle? Mesh, PlacedPart? Part);

/// <summary>
/// Collects a site's world-space geometry and placed catalogue parts for one level. Sites build in world space through
/// <see cref="PlanFrame"/> matrices; the module files every item under the cell of its anchor, so a site may span cells.
/// Transforms are written out in scalar arithmetic so the bytes do not depend on SIMD paths.
/// </summary>
public sealed class SiteBuilder
{
    private readonly List<SiteItem> _items = [];

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

    internal IReadOnlyList<SiteItem> Items => _items;

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
}

/// <summary>Collects a site's colliders, each filed under the 64 m cell of its anchor.</summary>
public sealed class SiteColliders
{
    private readonly List<(Vector2 Anchor, Collider Collider)> _items = [];

    internal SiteColliders(BuildContext context) => Context = context;

    public BuildContext Context { get; }

    public float Ground(float x, float z) => Context.World.Terrain.HeightAt(x, z);

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

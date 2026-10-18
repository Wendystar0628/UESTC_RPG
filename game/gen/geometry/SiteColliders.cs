using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Collects a site's colliders, each filed under the 64 m cell of its anchor.</summary>
public sealed class SiteColliders
{
    private readonly List<(Vector2 Anchor, Collider Collider)> _items = [];

    public SiteColliders(BuildContext context) => Context = context;

    public BuildContext Context { get; }

    public float Ground(float x, float z) => Context.World.Terrain.HeightAt(x, z);

    public float Ground(Vector2 p) => Ground(p.X, p.Y);

    public IReadOnlyList<(Vector2 Anchor, Collider Collider)> Items => _items;

    public void Add(Collider collider, Vector2? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(collider);
        _items.Add((anchor ?? GeometryPlacement.AnchorOf(collider), collider));
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
        foreach (var collider in local) Add(GeometryPlacement.Transform(collider, at));
    }

}

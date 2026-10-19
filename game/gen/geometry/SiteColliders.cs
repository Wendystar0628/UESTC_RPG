using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.Geometry;

/// <summary>Collects a site's colliders, each filed under the 64 m cell of its anchor.</summary>
public sealed class SiteColliders
{
    private readonly List<(Vector2 Anchor, Collider Collider)> _items = [];

    private readonly TileKey? _requestedTile;

    public SiteColliders(BuildContext context, TileKey? requestedTile = null)
    {
        Context = context;
        _requestedTile = requestedTile;
    }

    public BuildContext Context { get; }

    /// <summary>Whether a source range meets the requested collision cell; an unrestricted trace includes all sources.</summary>
    public bool Includes(MapRect range) => SiteTileRequest.Includes(_requestedTile, LodLevel.L0, range);

    /// <summary>For a source filed wholly under one explicit anchor; spanning sources must use their range instead.</summary>
    public bool Includes(Vector2 anchor) => _requestedTile is not { } tile || TileGrid.At(anchor) == tile;

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

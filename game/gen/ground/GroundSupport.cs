using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Contracts.Maps;

namespace Campus.Gen.Ground;

/// <summary>Prepared ground tops and optional elevated supports. Sampling never rebuilds a tile or mesh.</summary>
public sealed class GroundSupport : IWalkSupportSource
{
    private readonly IReadOnlyDictionary<TileKey, GroundSupportTile> _tiles;
    private readonly IWalkSupportSource? _elevated;
    private readonly WalkSupportData _data;

    private GroundSupport(IReadOnlyDictionary<TileKey, GroundSupportTile> tiles, IReadOnlyList<Collider> other)
    {
        _tiles = tiles;
        _data = new(Array.AsReadOnly(tiles.OrderBy(pair => pair.Key.Z).ThenBy(pair => pair.Key.X)
            .Select(pair => pair.Value.Data).ToArray()), Array.AsReadOnly(other.ToArray()));
        _elevated = other.Count == 0 ? null : new WalkSupportSurfaces(_data.Other);
    }

    public static GroundSupport Prepare(IWorldModel world, MapDescriptor map)
    {
        var tiles = new Dictionary<TileKey, GroundSupportTile>();
        foreach (var tile in GroundModule.TileKeys(map))
            tiles.Add(tile, GroundSupportTile.Build(world, map, tile, GroundSection.Default));
        return new(tiles, []);
    }

    public GroundSupport WithOtherSupports(IReadOnlyList<Collider> other) => new(_tiles, other);

    public WalkSupportData Export() => _data;

    /// <summary>Restores only CPU query indices; the product's original colliders remain the ground collision source.</summary>
    public static GroundSupport Restore(WalkSupportData data)
    {
        data.Validate();
        return new(data.Ground.ToDictionary(tile => tile.Base.Tile, tile => new GroundSupportTile(tile)), data.Other);
    }

    public float? SupportY(float x, float z, float yHint)
    {
        var key = new TileKey((int)MathF.Floor(x / 64f), (int)MathF.Floor(z / 64f));
        var ground = _tiles.TryGetValue(key, out var tile) ? tile.SupportY(x, z) : null;
        var elevated = _elevated?.SupportY(x, z, yHint);
        if (elevated is null) return ground;
        if (ground is null) return elevated;
        return MathF.Abs(elevated.Value - yHint) <= MathF.Abs(ground.Value - yHint) ? elevated : ground;
    }

    internal static GroundSupportTile TileFor(BuildContext context, TileKey tile, GroundSection section)
    {
        if (section == GroundSection.Default && context.World is WorldModelSnapshot { WalkSupport: GroundSupport prepared })
            return prepared._tiles.TryGetValue(tile, out var result) ? result :
                throw new InvalidOperationException($"Resident ground support has no tile {tile}.");
        return GroundSupportTile.Build(context.World, context.Map, tile, section);
    }
}

/// <summary>The visible mesher's top facets, merged across material colour boundaries into convex physical tops.</summary>
internal sealed class GroundSupportTile
{
    private readonly WalkSupportSurfaces _tops;
    private readonly HeightFieldCollider _basis;
    internal GroundSupportTileData Data { get; }
    public IReadOnlyList<Collider> Colliders { get; }

    internal GroundSupportTile(GroundSupportTileData data)
    {
        Data = data;
        _basis = data.Base with { HeightsCm = data.Base.HeightsCm.ToArray() };
        _tops = new(data.Tops, 4f);
        Colliders = Array.AsReadOnly(new Collider[] { data.Base }.Concat(data.Tops).ToArray());
    }

    internal static GroundSupportTile Build(IWorldModel world, MapDescriptor map, TileKey tile, GroundSection section)
    {
        var layout = GroundLayout.Of(world);
        var extent = GroundLod.Tile(tile, LodLevel.L0, map.DataRect);
        var terrain = TerrainGrid.For(world.Terrain, tile, LodLevel.L0, extent);
        // Lawn tones share their height and contact material; colour contours do not partition physical support.
        var field = TileField.Build(layout, extent, section, terrain);
        var support = new GroundSupportBuilder();
        var mesher = new GroundMesher(field, section, LodLevel.L0, layout, terrain, new MeshSink(false), support);
        mesher.Run();
        var tops = new List<Collider>();
        foreach (var face in support.Finish()) face.AddColliders(tops);
        return new(new(GroundCollision.BuildBase(tile, world.Terrain, mesher, field.N), tops.AsReadOnly()));
    }

    internal float? SupportY(float x, float z)
    {
        var top = _tops.SupportY(x, z, float.PositiveInfinity);
        if (top is not null) return top;
        var basis = WalkSupportSurfaces.SampleHeight(_basis, x, z);
        return float.IsFinite(basis) ? basis : null;
    }
}

internal sealed class GroundSupportFace(Vector3[] ring, Contracts.Audio.FootMat foot)
{
    internal Vector3[] Ring { get; } = ring;
    internal Contracts.Audio.FootMat Foot { get; } = foot;
    private readonly Vector3 _normal = Normal(ring);

    private static Vector3 Normal(Vector3[] points)
    {
        for (var i = 1; i + 1 < points.Length; i++)
        {
            var normal = Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]);
            if (MathF.Abs(normal.Y) > 1e-10f) return normal;
        }
        throw new InvalidOperationException("A ground support face must have a nonzero horizontal footprint.");
    }

    internal float Height(float x, float z) => Ring[0].Y - (_normal.X * (x - Ring[0].X) + _normal.Z * (z - Ring[0].Z)) / _normal.Y;

    internal bool Coplanar(GroundSupportFace other)
    {
        if (Foot != other.Foot) return false;
        foreach (var point in other.Ring)
            if (MathF.Abs(Height(point.X, point.Z) - point.Y) > .00005f) return false;
        return true;
    }

    internal void AddColliders(List<Collider> result)
    {
        var outline = Simplify(Ring);
        // Tops extend down into their base, retaining real kerb, soil and frame heights without filling the pit.
        var thickness = MathF.Max(.1f, outline.Max(p => p.Y) - outline.Min(p => p.Y) + .5f);
        if (outline.All(point => MathF.Abs(point.Y - outline[0].Y) < .00001f))
        {
            result.Add(new SlabCollider(outline.Select(point => new Vector2(point.X, point.Z)).ToArray(), outline[0].Y,
                thickness, SurfaceTag.Ground)
            { Foot = Foot });
            return;
        }
        // RampCollider is a convex extrusion of its top. Pair adjacent fan triangles; the final triangle repeats its last corner.
        for (var i = 1; i + 1 < outline.Length; i += 2)
            result.Add(new RampCollider(outline[0], outline[i], outline[i + 1], outline[Math.Min(i + 2, outline.Length - 1)],
                thickness, SurfaceTag.Ground)
            { Foot = Foot });
    }

    private static Vector3[] Simplify(Vector3[] ring)
    {
        var result = new List<Vector3>();
        for (var i = 0; i < ring.Length; i++)
        {
            var a = ring[(i + ring.Length - 1) % ring.Length]; var b = ring[i]; var c = ring[(i + 1) % ring.Length];
            var ab = new Vector2(b.X - a.X, b.Z - a.Z); var bc = new Vector2(c.X - b.X, c.Z - b.Z);
            if (MathF.Abs(ab.X * bc.Y - ab.Y * bc.X) <= .000001f && Vector2.Dot(ab, bc) >= 0f) continue;
            result.Add(b);
        }
        return result.Count >= 3 ? result.ToArray() : ring;
    }
}

internal sealed class GroundSupportBuilder
{
    private readonly List<GroundSupportFace?> _faces = [];
    private readonly Dictionary<(PointKey, PointKey), int> _edges = [];
    private readonly Queue<int> _pending = [];
    private readonly record struct PointKey(float X, float Y, float Z)
    {
        internal static PointKey Of(Vector3 p) => new(p.X, p.Y, p.Z);
    }

    internal void Add(GroundSurface surface, Vector3 a, Vector3 b, Vector3 c)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() < 1e-14f) return;
        var area = (double)(b.X - a.X) * (c.Z - a.Z) - (double)(b.Z - a.Z) * (c.X - a.X);
        if (Math.Abs(area) < 1e-10) return;
        if (area < 0) (b, c) = (c, b);
        var index = _faces.Count;
        _faces.Add(new([a, b, c], GroundPalette.FootOf(surface)));
        Register(index);
    }

    internal GroundSupportFace[] Finish()
    {
        while (_pending.TryDequeue(out var index))
        {
            if (_faces[index] is not { } face) continue;
            for (var edge = 0; edge < face.Ring.Length; edge++)
            {
                var a = face.Ring[edge]; var b = face.Ring[(edge + 1) % face.Ring.Length];
                if (!_edges.TryGetValue((PointKey.Of(b), PointKey.Of(a)), out var neighbour) || neighbour == index
                    || _faces[neighbour] is not { } other || !face.Coplanar(other)) continue;
                var opposite = Array.FindIndex(other.Ring, point => PointKey.Of(point) == PointKey.Of(b));
                if (opposite < 0 || PointKey.Of(other.Ring[(opposite + 1) % other.Ring.Length]) != PointKey.Of(a)) continue;
                var joined = new List<Vector3>(face.Ring.Length + other.Ring.Length - 2);
                for (var i = 1; i < face.Ring.Length; i++) joined.Add(face.Ring[(edge + i) % face.Ring.Length]);
                for (var i = 1; i < other.Ring.Length; i++) joined.Add(other.Ring[(opposite + i) % other.Ring.Length]);
                if (joined.Count > 256 || joined.Distinct().Count() != joined.Count || !Convex(joined)) continue;
                Unregister(index); Unregister(neighbour);
                _faces[neighbour] = null;
                _faces[index] = new(joined.ToArray(), face.Foot);
                Register(index);
                break;
            }
        }
        return _faces.OfType<GroundSupportFace>().ToArray();
    }

    private void Register(int index)
    {
        var ring = _faces[index]!.Ring;
        for (var i = 0; i < ring.Length; i++) _edges[(PointKey.Of(ring[i]), PointKey.Of(ring[(i + 1) % ring.Length]))] = index;
        _pending.Enqueue(index);
    }

    private void Unregister(int index)
    {
        var ring = _faces[index]!.Ring;
        for (var i = 0; i < ring.Length; i++)
        {
            var key = (PointKey.Of(ring[i]), PointKey.Of(ring[(i + 1) % ring.Length]));
            if (_edges.TryGetValue(key, out var value) && value == index) _edges.Remove(key);
        }
    }

    private static bool Convex(List<Vector3> ring)
    {
        double area = 0;
        for (var i = 1; i + 1 < ring.Count; i++)
            area += (double)(ring[i].X - ring[0].X) * (ring[i + 1].Z - ring[0].Z)
                - (double)(ring[i].Z - ring[0].Z) * (ring[i + 1].X - ring[0].X);
        if (area <= 1e-10) return false;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count]; var c = ring[(i + 2) % ring.Count];
            if ((double)(b.X - a.X) * (c.Z - b.Z) - (double)(b.Z - a.Z) * (c.X - b.X) < -.0000001) return false;
        }
        return true;
    }
}

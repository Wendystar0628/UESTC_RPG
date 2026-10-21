using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;

namespace Campus.Gen.Geometry;

/// <summary>How much of a site's geometry one level carries: L0 everything, L1 without fine parts, L2 and coarser masses only.</summary>
public enum SiteDetail { Full, Plain, Mass }

/// <summary>One output of a site, filed under the cell of its plan anchor (world x, z).</summary>
public sealed record SiteItem(Vector2 Anchor, GeometryBuffer? Mesh, PlacedPart? Part, LightEmitter? Light = null, bool HasLodChannel = false);

/// <summary>
/// Collects a site's world-space geometry and placed catalogue parts for one level. Sites build in world space through
/// row-vector matrices; the module files every item under the cell of its anchor, so a site may span cells.
/// Long runs of small boxes (fence bars, posts, rails) go through <see cref="Box"/>, which batches them per cell and
/// material instead of one bundle each. Transforms are written out in scalar arithmetic so the bytes do not depend on SIMD.
/// </summary>
public sealed class SiteBuilder
{
    private readonly List<SiteItem> _items = [];
    private readonly List<(int X, int Z, ToonSpec Material, bool NoShadow)> _batchKeys = [];
    private readonly Dictionary<(int X, int Z, ToonSpec Material, bool NoShadow), BoxBatch> _batches = [];
    private readonly TileKey? _requestedTile;

    public SiteBuilder(BuildContext context, LodLevel lod, SiteDetail detail, TileKey? requestedTile = null)
    {
        Context = context;
        Lod = lod;
        Detail = detail;
        _requestedTile = requestedTile;
    }

    public BuildContext Context { get; }
    public LodLevel Lod { get; }

    public SiteDetail Detail { get; }

    /// <summary>Whether a source range meets the requested grid cell; an unrestricted build and L4 include all sources.</summary>
    public bool Includes(MapRect range) => SiteTileRequest.Includes(_requestedTile, Lod, range);

    /// <summary>For a source filed wholly under one explicit anchor; spanning sources must use their range instead.</summary>
    public bool Includes(Vector2 anchor) => _requestedTile is not { } tile || Lod == LodLevel.L4 || TileGrid.At(anchor, Lod) == tile;

    /// <summary>Ground height from the map terrain (flat stub until the game/gen/world/ evaluator lands).</summary>
    public float Ground(float x, float z) => Context.World.Terrain.HeightAt(x, z);

    public float Ground(Vector2 p) => Ground(p.X, p.Y);

    /// <summary>Items in build order, batched boxes last in first-use order.</summary>
    public IReadOnlyList<SiteItem> Items
    {
        get
        {
            var all = new List<SiteItem>(_items);
            foreach (var key in _batchKeys) all.AddRange(_batches[key].Finish());
            return all.AsReadOnly();
        }
    }

    /// <summary>Adds placed geometry; an explicit anchor changes cell filing only.</summary>
    public void Add(PartGeometry geometry, Matrix4x4 at, Vector2? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        foreach (var mesh in geometry.Meshes) Add(mesh, at, anchor);
    }

    public void Add(GeometryBundle mesh, Matrix4x4 at, Vector2? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var (centre, placed) = GeometryPlacement.Place(mesh, at);
        _items.Add(new(anchor ?? centre, GeometryBuffer.Freeze(placed), null, HasLodChannel: mesh.Lods is not null));
    }

    /// <summary>Places a catalogue part (PartCatalog); it shows as a box until its owner's geometry lands (ENG-ARCH).</summary>
    public void Place(string part, BoxPartParams parameters, Matrix4x4 at, PaletteToken? tint = null, Vector2? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Place(new PlacedPart(new(part), parameters, at, tint), anchor);
    }

    /// <summary>Retains the complete catalogue declaration, including emitter, variant and culling policy.</summary>
    public void Place(PlacedPart part, Vector2? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(part);
        _items.Add(new(anchor ?? new(part.At.M41, part.At.M43), null, part));
    }

    /// <summary>Files a light declaration by its plan position, retaining site insertion order.</summary>
    public void Light(LightEmitter emitter, Vector2? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        _items.Add(new(anchor ?? new(emitter.Position.X, emitter.Position.Z), null, null, emitter));
    }

    /// <summary>
    /// A plain box in world space, batched with others of the same material in its cell: centre <paramref name="c"/> on the
    /// plan, length along unit <paramref name="u"/>, depth across it, from y0 to y1. Caps off leaves the top and bottom
    /// open (bars hidden by rails); noShadow sets the no-shadow vertex flag (thin bars, ART-MODELING).
    /// </summary>
    public void Box(ToonSpec material, Vector2 c, Vector2 u, float length, float depth, float y0, float y1, bool caps = true, bool noShadow = false)
    {
        ArgumentNullException.ThrowIfNull(material);
        var size = 64f * (1 << (int)Lod);
        var key = ((int)MathF.Floor(c.X / size), (int)MathF.Floor(c.Y / size), material, noShadow);
        if (!_batches.TryGetValue(key, out var batch))
        {
            _batchKeys.Add(key);
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
        private readonly GeometrySink _sink = new();
        private readonly List<GeometryVertex> _face = [];

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
            _face.Add(new(p, n, uv, Ao: 1f, Flags: noShadow ? VertexFlag.NoShadow : null));
            return _face.Count - 1;
        }

        private void Quad(int a, int b, int c, int d, Vector3 outward)
        {
            _sink.Quad(material, _face[a], _face[b], _face[c], _face[d]);
            _face.Clear();
        }

        public IReadOnlyList<SiteItem> Finish() => _sink.Finish()
            .Select(mesh => new SiteItem(anchor, GeometryBuffer.Freeze(mesh), null)).ToArray();
    }
}

public static class SiteTileRequest
{
    public static bool Includes(TileKey? requestedTile, LodLevel lod, MapRect range)
    {
        if (requestedTile is not { } tile || lod == LodLevel.L4) return true;
        var min = TileGrid.At(range.Min, lod);
        var max = TileGrid.At(range.Max, lod);
        return tile.X >= min.X && tile.X <= max.X && tile.Z >= min.Z && tile.Z <= max.Z;
    }
}

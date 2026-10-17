using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision;

public sealed record CollisionDiagnostics(int Tiles, int ReadyTiles, int Bodies, int Shapes, int MaxBodyShapes,
    int MainThreadViolations, int IncompleteMounts)
{
    public int RegisteredBodies { get; init; }
    public int UnclassifiedHits { get; init; }
}

/// <summary>The collision service owns every body it mounts. All entry points are main-thread only.</summary>
// Registration activates with the formal Boot composition.
[KernelService(typeof(ICollisionWorld), Owner = "WP-43")]
public sealed class CollisionWorld : ICollisionWorld
{
    public const uint WalkMask = 479;
    public const uint SupportMask = (1u << (int)SurfaceTag.Ground) | (1u << (int)SurfaceTag.Stair) | (1u << (int)SurfaceTag.Prop);
    private readonly Dictionary<TileKey, TileState> _tiles = [];
    private readonly Dictionary<Rid, SurfaceTag> _bodySurfaces = [];
    private Node3D? _root;
    private IMapContext? _context;
    private int _threadViolations;
    private int _incompleteMounts;
    private int _unclassifiedHits;
    public MapId? Map => _context?.Descriptor.Id;
    public long Revision { get; private set; }
    public string? Error { get; private set; }

    public CollisionDiagnostics Diagnostics
    {
        get
        {
            MainThread();
            return new(_tiles.Count, _tiles.Values.Count(t => t.Ready),
                _tiles.Values.Sum(t => t.Bodies.Count), _tiles.Values.Sum(t => t.Shapes),
                _tiles.Values.SelectMany(t => t.Bodies).Select(b => ShapeCount(b.Body)).DefaultIfEmpty().Max(), _threadViolations, _incompleteMounts)
            {
                RegisteredBodies = _bodySurfaces.Count,
                UnclassifiedHits = _unclassifiedHits
            };
        }
    }

    public void Attach(Node3D root, IMapContext context)
    {
        MainThread();
        Unload();
        _root = root;
        _context = context;
        Error = null;
    }

    public bool HasTile(TileKey tile) => _tiles.ContainsKey(tile);

    public bool Ready(N.Vector3 p, float r)
    {
        MainThread();
        if (_context is null || _root is null || !_root.IsInsideTree() || !Finite(p) || !float.IsFinite(r) || r < 0) return false;
        var center = CollisionGeometry.TileAt(p);
        var min = CollisionGeometry.TileAt(p - new N.Vector3(r, 0, r));
        var max = CollisionGeometry.TileAt(p + new N.Vector3(r, 0, r));
        for (var x = Math.Min(min.X, center.X - 1); x <= Math.Max(max.X, center.X + 1); x++)
            for (var z = Math.Min(min.Z, center.Z - 1); z <= Math.Max(max.Z, center.Z + 1); z++)
            {
                var tile = new TileKey(x, z);
                if (Math.Abs(x - center.X) > 1 || Math.Abs(z - center.Z) > 1)
                {
                    var origin = CollisionGeometry.Origin(tile);
                    var dx = MathF.Max(MathF.Max(origin.X - p.X, p.X - origin.X - 64), 0);
                    var dz = MathF.Max(MathF.Max(origin.Z - p.Z, p.Z - origin.Z - 64), 0);
                    if (dx * dx + dz * dz > r * r) continue;
                }
                // Outside the declared tile rectangle is never silently treated as physical ground.
                if (!_tiles.TryGetValue(tile, out var state) || !state.Ready) return false;
            }
        return true;
    }

    public void BeginTile(TileKey tile)
    {
        MainThread();
        if (_root is null || _context is null) throw new InvalidOperationException("Collision map is not attached.");
        if (!_tiles.TryAdd(tile, new())) throw new InvalidOperationException($"Duplicate collision tile: {tile}.");
    }

    /// <summary>One atomic body: no shape additions are allowed after AddChild.</summary>
    public void MountBody(TileKey tile, IReadOnlyList<Collider> colliders)
    {
        MainThread();
        if (colliders.Count is < 1 or > CollisionGeometry.MaxShapesPerBody) throw new ArgumentException("A body needs 1..250 shapes.");
        if (_root is null || !_tiles.TryGetValue(tile, out var state) || state.Ready) throw new InvalidOperationException("Collision tile is not pending.");
        var bodyIndex = state.Bodies.Count;
        var first = colliders[0];
        StaticBody3D? body = null;
        var rid = default(Rid);
        try
        {
            body = new StaticBody3D
            {
                Name = $"Collision_{tile.X}_{tile.Z}_{bodyIndex}",
                Position = CollisionGeometry.Origin(tile).ToGodot(),
                CollisionLayer = 1u << (int)first.Tag,
                CollisionMask = 0,
            };
            rid = body.GetRid();
            body.SetMeta("surface", (int)first.Tag);
            body.SetMeta("foot", (int)first.Foot);
            body.SetMeta("tile_x", tile.X);
            body.SetMeta("tile_z", tile.Z);
            foreach (var source in colliders)
            {
                if (source.Tag != first.Tag || source.Foot != first.Foot || !Enum.IsDefined(source.Tag) || !Enum.IsDefined(source.Foot) || source.Tag == SurfaceTag.Water)
                    throw new ArgumentException("A collision body must have a single valid surface and material.");
                var (shape, transform) = CollisionShapeFactory.MakeShape(source);
                using var ownedShape = shape;
                shape.SetMeta("surface", (int)source.Tag);
                shape.SetMeta("foot", (int)source.Foot);
                var owner = body.CreateShapeOwner(shape);
                body.ShapeOwnerSetTransform(owner, transform);
                body.ShapeOwnerAddShape(owner, shape);
            }
            if (body.IsInsideTree() || ShapeCount(body) != colliders.Count)
            { _incompleteMounts++; throw new InvalidOperationException("Collision body entered the tree before all shapes were ready."); }
            _root.AddChild(body);
            _bodySurfaces.Add(rid, first.Tag);
            state.Bodies.Add(new(body, rid));
            state.Shapes += colliders.Count;
        }
        catch
        {
            _bodySurfaces.Remove(rid);
            if (body is not null && GodotObject.IsInstanceValid(body)) body.Free();
            throw;
        }
    }

    public void CompleteTile(TileKey tile)
    {
        MainThread();
        if (!_tiles.TryGetValue(tile, out var state)) throw new InvalidOperationException("Unknown collision tile.");
        state.Ready = true;
    }

    public void FailTile(TileKey tile, string error) { MainThread(); RemoveTile(tile); Error = error; }

    public void RemoveTile(TileKey tile)
    {
        MainThread();
        if (!_tiles.Remove(tile, out var state)) return;
        foreach (var mounted in state.Bodies)
        {
            // Remove by the saved RID even when the managed wrapper was already freed.
            _bodySurfaces.Remove(mounted.Rid);
            if (GodotObject.IsInstanceValid(mounted.Body)) mounted.Body.Free();
        }
    }

    public void Unload()
    {
        MainThread();
        foreach (var tile in _tiles.Keys.OrderBy(t => t.X).ThenBy(t => t.Z).ToArray()) RemoveTile(tile);
        _bodySurfaces.Clear();
        _context = null; _root = null; Error = null; Revision++;
    }

    public SupportHit? SupportY(float x, float z, float yHint)
    {
        MainThread();
        if (_root is null || !_root.IsInsideTree() || !float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(yHint)) return null;
        using var query = PhysicsRayQueryParameters3D.Create(new(x, yHint + 0.4f, z), new(x, MathF.Min(yHint - 1, (_context?.Descriptor.Bounds.Min.Y ?? -300) - 1), z), SupportMask);
        query.CollideWithAreas = false;
        query.HitBackFaces = false;
        var hit = _root.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0 || hit["normal"].AsVector3().Y <= 0) return null;
        var position = hit["position"].AsVector3();
        var rid = hit["rid"].AsRid();
        var tag = SurfaceForHit(rid, hit["collider"].AsGodotObject());
        // An unclassified external body cannot establish a safe walking support.
        return tag is { } known ? new(position.Y, known) : null;
    }

    public SweepHit? Sweep(N.Vector3 from, N.Vector3 to, float r, float h)
    {
        MainThread();
        if (_root is null || !_root.IsInsideTree()) return null;
        if (!Finite(from) || !Finite(to) || !float.IsFinite(r) || !float.IsFinite(h) || r <= 0 || h <= 0) throw new ArgumentException("Invalid sweep cylinder.");
        using var cylinder = new CylinderShape3D { Radius = r, Height = h };
        using var query = new PhysicsShapeQueryParameters3D
        {
            Shape = cylinder,
            Transform = new(Basis.Identity, (from + N.Vector3.UnitY * (h / 2)).ToGodot()),
            Motion = (to - from).ToGodot(),
            CollisionMask = WalkMask,
            CollideWithAreas = false,
            Margin = 0.001f,
        };
        var space = _root.GetWorld3D().DirectSpaceState;
        var fractions = space.CastMotion(query);
        if (fractions.Length != 2) throw new InvalidOperationException("Physics CastMotion returned invalid fractions.");
        if (fractions[0] >= 1f && from != to) return null;
        var t = from == to ? 0f : fractions[0];
        var contact = from + (to - from) * (from == to ? 0f : fractions[1]);
        query.Transform = new(Basis.Identity, (contact + N.Vector3.UnitY * (h / 2)).ToGodot());
        query.Motion = Vector3.Zero;
        query.Margin = 0.005f;
        var rest = space.GetRestInfo(query);
        if (rest.Count == 0) return null;
        var rid = rest["rid"].AsRid();
        // GetRestInfo provides a physics RID independently of collider_id. Server-created
        // bodies and stale object IDs may have no managed GodotObject wrapper at all.
        var instanceId = rest["collider_id"].AsUInt64();
        var collider = instanceId == 0 ? null : GodotObject.InstanceFromId(instanceId);
        var tag = SurfaceForHit(rid, collider);
        // Preserve the real blocking hit when no classification is available. Barrier
        // disallows automatic stepping; no floor tag or foot material is invented.
        return new(rest["point"].AsVector3().ToNumerics(), rest["normal"].AsVector3().ToNumerics(), t, tag ?? SurfaceTag.Barrier);
    }

    private void MainThread()
    {
        if (OS.GetThreadCallerId() == OS.GetMainThreadId()) return;
        _threadViolations++;
        throw new InvalidOperationException("Collision bodies and queries are main-thread only.");
    }
    private static bool Finite(N.Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
    private SurfaceTag? SurfaceForHit(Rid rid, GodotObject? body)
    {
        if (_bodySurfaces.TryGetValue(rid, out var registered)) return registered;
        // Metadata from another kernel owner is usable only if its live collision
        // object is the exact physics body reported by the query.
        if (body is CollisionObject3D collision && GodotObject.IsInstanceValid(collision) &&
            collision.GetRid() == rid && collision.HasMeta("surface"))
        {
            var tag = (SurfaceTag)collision.GetMeta("surface").AsInt32();
            if (Enum.IsDefined(tag)) return tag;
        }
        _unclassifiedHits++;
        return null;
    }
    private static int ShapeCount(StaticBody3D body) => body.GetShapeOwners().Sum(owner => body.ShapeOwnerGetShapeCount((uint)owner));
    private sealed record MountedBody(StaticBody3D Body, Rid Rid);
    private sealed class TileState { public bool Ready; public int Shapes; public List<MountedBody> Bodies { get; } = []; }
}

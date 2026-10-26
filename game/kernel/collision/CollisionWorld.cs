using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision;

public interface ICollisionMountRuntime : ICollisionWorld
{
    MapId? Map { get; }
    long Revision { get; }
    void Attach(Node3D root, IMapContext context);
    object BeginTile(TileKey tile);
    bool MountBodyStep(object owner, IReadOnlyList<Collider> colliders, double budgetMilliseconds);
    void CompleteTile(object owner);
    void RetireTile(object owner);
    bool IsTileReady(TileKey tile);
    void FailTile(object owner, Exception error);
    bool RemoveTileStep(object owner, double budgetMilliseconds);
    void Unload();
}

public sealed record CollisionDiagnostics(int Tiles, int ReadyTiles, int Bodies, int Shapes, int MaxBodyShapes,
    int MainThreadViolations, int IncompleteMounts)
{
    public int RegisteredBodies { get; init; }
    public int UnclassifiedHits { get; init; }
}

/// <summary>The collision service owns every body it mounts. All entry points are main-thread only.</summary>
// Registration activates with the formal Boot composition.
[KernelService(typeof(ICollisionMountRuntime))]
[KernelService(typeof(ICollisionWorld))]
public sealed class CollisionWorld : ICollisionMountRuntime
{
    public const uint WalkMask = 479;
    public const uint SupportMask = (1u << (int)SurfaceTag.Ground) | (1u << (int)SurfaceTag.Stair) | (1u << (int)SurfaceTag.Prop);
    private readonly Dictionary<TileKey, TileState> _tiles = [];
    private readonly Dictionary<Rid, SurfaceTag> _bodySurfaces = [];
    private Node3D? _root;
    private IMapContext? _context;
    private CollisionBoundaryProbe? _boundaryProbe;
    private int _threadViolations;
    private int _incompleteMounts;
    private int _unclassifiedHits;
    public MapId? Map => _context?.Descriptor.Id;
    public long Revision { get; private set; }
    public Exception? Failure { get; private set; }

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
        if (_tiles.Count != 0) throw new InvalidOperationException("Collision owners must drain before attaching a map.");
        Revision++;
        _root = root;
        _context = context;
        Failure = null;
    }

    public bool IsTileReady(TileKey tile) => _tiles.TryGetValue(tile, out var state) && state.Ready && !state.Retiring;

    public bool Ready(N.Vector3 p, float r)
    {
        MainThread();
        if (_context is null || _root is null || !_root.IsInsideTree() || !VectorMath.Finite(p) || !float.IsFinite(r) || r < 0) return false;
        var center = TileGrid.At(p);
        var min = TileGrid.At(p - new N.Vector3(r, 0, r));
        var max = TileGrid.At(p + new N.Vector3(r, 0, r));
        for (var x = Math.Min(min.X, center.X - 1); x <= Math.Max(max.X, center.X + 1); x++)
            for (var z = Math.Min(min.Z, center.Z - 1); z <= Math.Max(max.Z, center.Z + 1); z++)
            {
                var tile = new TileKey(x, z);
                if (Math.Abs(x - center.X) > 1 || Math.Abs(z - center.Z) > 1)
                {
                    var origin = TileGrid.Origin(tile);
                    var dx = MathF.Max(MathF.Max(origin.X - p.X, p.X - origin.X - TileGrid.BaseEdge), 0);
                    var dz = MathF.Max(MathF.Max(origin.Y - p.Z, p.Z - origin.Y - TileGrid.BaseEdge), 0);
                    if (dx * dx + dz * dz > r * r) continue;
                }
                // Outside the declared tile rectangle is never silently treated as physical ground.
                if (!_tiles.TryGetValue(tile, out var state) || !state.Ready || state.Retiring) return false;
            }
        return true;
    }

    public object BeginTile(TileKey tile)
    {
        MainThread();
        if (_root is null || _context is null) throw new InvalidOperationException("Collision map is not attached.");
        if (_tiles.TryGetValue(tile, out var current) && !current.Retiring)
            throw new InvalidOperationException($"Duplicate collision tile: {tile}.");
        var state = new TileState(tile, _root);
        _tiles[tile] = state;
        return state;
    }

    /// <summary>Checks the shared remainder between shapes; AddChild occurs only after the whole body is ready.</summary>
    public bool MountBodyStep(object owner, IReadOnlyList<Collider> colliders, double budgetMilliseconds)
    {
        MainThread();
        var state = (TileState)owner;
        if (state.Ready || state.Retiring) throw new InvalidOperationException("Collision tile is not pending.");
        if (colliders.Count is < 1 or > CollisionGeometry.MaxShapesPerBody) throw new ArgumentException("A body needs 1..250 shapes.");
        if (budgetMilliseconds <= 0) return false;
        var start = FrameClock.MonotonicMilliseconds;
        var first = colliders[0];
        if (state.Pending is null)
        {
            var body = new StaticBody3D();
            var mounted = new MountedBody(body);
            state.Bodies.Add(mounted); state.Pending = mounted;
            var origin = TileGrid.Origin(state.Tile);
            body.Name = $"Collision_{state.Tile.X}_{state.Tile.Z}_{state.Bodies.Count - 1}";
            body.Position = new Vector3(origin.X, 0, origin.Y);
            body.CollisionLayer = 1u << (int)first.Tag; body.CollisionMask = 0;
            mounted.Rid = body.GetRid();
            body.SetMeta("surface", (int)first.Tag); body.SetMeta("foot", (int)first.Foot);
            body.SetMeta("tile_x", state.Tile.X); body.SetMeta("tile_z", state.Tile.Z);
            state.Batch = colliders;
        }
        if (!ReferenceEquals(state.Batch, colliders)) throw new InvalidOperationException("A pending collision body changed its source batch.");
        var pending = state.Pending.Body;
        while (state.Shape < colliders.Count)
        {
            if (FrameClock.MonotonicMilliseconds - start >= budgetMilliseconds) return false;
            var source = colliders[state.Shape];
            if (source.Tag != first.Tag || source.Foot != first.Foot || !Enum.IsDefined(source.Tag) || !Enum.IsDefined(source.Foot) || source.Tag == SurfaceTag.Water)
                throw new ArgumentException("A collision body must have a single valid surface and material.");
            var (shape, transform) = CollisionShapeFactory.MakeShape(source);
            state.PendingShape = shape;
            shape.SetMeta("surface", (int)source.Tag); shape.SetMeta("foot", (int)source.Foot);
            var shapeOwner = pending.CreateShapeOwner(shape);
            pending.ShapeOwnerSetTransform(shapeOwner, transform); pending.ShapeOwnerAddShape(shapeOwner, shape);
            state.Shape++; state.Shapes++;
            shape.Dispose(); state.PendingShape = null;
        }
        if (FrameClock.MonotonicMilliseconds - start >= budgetMilliseconds) return false;
        if (pending.IsInsideTree() || ShapeCount(pending) != colliders.Count)
        { _incompleteMounts++; throw new InvalidOperationException("Collision body entered the tree before all shapes were ready."); }
        state.Root.AddChild(pending);
        _bodySurfaces.Add(state.Pending.Rid, first.Tag);
        state.Pending.Shapes = colliders.Count;
        state.Pending = null; state.Batch = null; state.Shape = 0;
        return true;
    }

    public void CompleteTile(object owner)
    {
        MainThread(); var state = (TileState)owner;
        if (state.Retiring || state.Pending is not null) throw new InvalidOperationException("Collision tile is not complete.");
        state.Ready = true;
    }

    public void RetireTile(object owner)
    {
        MainThread(); var state = (TileState)owner;
        state.Ready = false; state.Retiring = true;
    }

    public void FailTile(object owner, Exception error)
    {
        MainThread(); Failure = RetainedFailure.Append(Failure, error); RetireTile(owner);
    }

    public bool RemoveTileStep(object owner, double budgetMilliseconds)
    {
        MainThread(); var state = (TileState)owner; RetireTile(state);
        var start = FrameClock.MonotonicMilliseconds;
        if (state.PendingShape is { } pendingShape)
        {
            pendingShape.Dispose(); state.PendingShape = null;
            return false;
        }
        if (state.Bodies.Count > 0)
            do
            {
                var mounted = state.Bodies[^1];
                if (GodotObject.IsInstanceValid(mounted.Body)) mounted.Body.Free();
                _bodySurfaces.Remove(mounted.Rid);
                state.Bodies.RemoveAt(state.Bodies.Count - 1);
                state.Shapes -= ReferenceEquals(state.Pending, mounted) ? state.Shape : mounted.Shapes;
                if (ReferenceEquals(state.Pending, mounted)) { state.Pending = null; state.Batch = null; state.Shape = 0; }
            } while (state.Bodies.Count > 0 && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
        if (state.Bodies.Count != 0) return false;
        if (_tiles.TryGetValue(state.Tile, out var current) && ReferenceEquals(current, state)) _tiles.Remove(state.Tile);
        if (_context is null && _tiles.Count == 0) { _boundaryProbe?.Dispose(); _boundaryProbe = null; }
        return true;
    }

    public void Unload()
    {
        MainThread();
        foreach (var state in _tiles.Values) RetireTile(state);
        _context = null; _root = null; Revision++;
        if (_tiles.Count == 0) { _boundaryProbe?.Dispose(); _boundaryProbe = null; }
    }

    public SupportHit? SupportY(float x, float z, float yHint)
    {
        MainThread();
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(yHint)) return null;
        var hit = SupportRay(new(x, yHint + 0.4f, z), new(x, MathF.Min(yHint - 1, (_context?.Descriptor.Bounds.Min.Y ?? -300) - 1), z));
        return hit is { } support ? new(support.Point.Y, support.Tag) : null;
    }

    public SweepHit? SupportRay(N.Vector3 from, N.Vector3 to)
    {
        MainThread();
        if (_root is null || !_root.IsInsideTree() || !VectorMath.Finite(from) || !VectorMath.Finite(to)) return null;
        using var query = PhysicsRayQueryParameters3D.Create(from.ToGodot(), to.ToGodot(), SupportMask);
        query.CollideWithAreas = false;
        query.HitBackFaces = false;
        var hit = _root.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0 || hit["normal"].AsVector3().Y <= 0) return null;
        var position = hit["position"].AsVector3();
        var rid = hit["rid"].AsRid();
        var tag = SurfaceForHit(rid, hit["collider"].AsGodotObject());
        // An unclassified external body cannot establish a safe walking support.
        float length = N.Vector3.Distance(from, to);
        return tag is { } known ? new(position.ToNumerics(), hit["normal"].AsVector3().ToNumerics(),
            length > 0 ? N.Vector3.Distance(from, position.ToNumerics()) / length : 0, known) : null;
    }

    public int NearbyBoundaries(N.Vector3 feet, float r, float h, Span<BoundaryHit> hits)
    {
        MainThread();
        if (_root is null || !_root.IsInsideTree()) return 0;
        if (!VectorMath.Finite(feet) || !float.IsFinite(r) || !float.IsFinite(h) || r <= 0 || h <= 0)
            throw new ArgumentException("Invalid boundary query cylinder.");
        if (hits.IsEmpty) return 0;
        _boundaryProbe ??= new(_root.GetWorld3D().Space);
        return _boundaryProbe.Nearby(feet, r, h, hits);
    }

    public SweepHit? Sweep(N.Vector3 from, N.Vector3 to, float r, float h)
    {
        MainThread();
        if (_root is null || !_root.IsInsideTree()) return null;
        if (!VectorMath.Finite(from) || !VectorMath.Finite(to) || !float.IsFinite(r) || !float.IsFinite(h) || r <= 0 || h <= 0) throw new ArgumentException("Invalid sweep cylinder.");
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
    private sealed class MountedBody(StaticBody3D body)
    { public StaticBody3D Body { get; } = body; public Rid Rid; public int Shapes; }
    private sealed class TileState(TileKey tile, Node3D root)
    {
        public TileKey Tile { get; } = tile;
        public Node3D Root { get; } = root;
        public bool Ready, Retiring;
        public int Shapes, Shape;
        public MountedBody? Pending;
        public Shape3D? PendingShape;
        public IReadOnlyList<Collider>? Batch;
        public List<MountedBody> Bodies { get; } = [];
    }
}

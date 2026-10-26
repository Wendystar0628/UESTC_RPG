using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Renderer;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Instances;

/// <summary>Kernel-owned dynamic instances, sharing immutable prototype meshes within each map load.</summary>
[KernelNode("kernel.instance-pools")]
[KernelService(typeof(IInstancePoolHost))]
public sealed partial class InstancePoolHost : Node3D, IInstancePoolHost, IMapAware
{
    internal const int FloatsPerInstance = InstanceStreamLayout.FloatsPerInstance;
    private const uint ValidLayers = (1u << 20) - 1;
    private readonly Dictionary<string, InstancePool> _pools = new(StringComparer.Ordinal);
    private readonly List<InstancePool> _orderedPools = [];
    private readonly Dictionary<string, SharedPrototypeMesh> _prototypeMeshes = new(StringComparer.Ordinal);
    private readonly Dictionary<ShaderId, ShaderMaterial> _shaderMaterials = [];
    private IToonMaterialProvider? _materials;
    private PaletteCatalog? _catalog;
    private IShaderMaterialSource? _shaderSource;
    private MapId? _activeMap;
    private bool _mounted;
    private bool _accepting;
    private bool _exiting;
    private long _nextNodeId;

    public InstancePoolHost() { }
    public InstancePoolHost(IToonMaterialProvider materials, IShaderMaterialSource shaderSource, IPaletteCatalogSource palettes)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(shaderSource);
        ArgumentNullException.ThrowIfNull(palettes);
        _materials = materials;
        _catalog = palettes.Catalog;
        _shaderSource = shaderSource;
    }

    public override void _EnterTree()
    {
        RequireMainThread();
        ProcessPriority = FramePriority.StreamMount;
        _mounted = _accepting = true;
        _exiting = false;
    }

    public override void _ExitTree()
    {
        RequireMainThread();
        _mounted = false;
        _exiting = true;
        _activeMap = null;
        if (_orderedPools.Count != 0) throw new InvalidOperationException("Instance-pool owners must drain before host exit.");
        // Created materials remain borrowed until the last pool owner has returned its mesh.
        foreach (var pair in _shaderMaterials.OrderBy(pair => pair.Key)) pair.Value.Dispose();
        _shaderMaterials.Clear();
    }

    public void OnMapUnloading(MapId from)
    {
        RequireMainThread();
        ValidateMapId(from);
        var unloadingActiveMap = _activeMap == from;
        if (unloadingActiveMap) { _activeMap = null; _accepting = false; }
        // Consumers own their pools through the shared stream retirement queue.
        // Their final Free returns the shared mesh; clearing this table here would lose those borrows.
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        RequireMainThread();
        if (!_mounted || _exiting || !IsInsideTree()) throw new InvalidOperationException("The instance-pool host is not mounted.");
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(ctx.Descriptor);
        var map = ctx.Descriptor.Id;
        ValidateMapId(map);
        if (_activeMap is { } active && active != map)
            throw new InvalidOperationException("Unload the previous pool map before loading another map.");
        _activeMap = map; _accepting = true;
    }

    /// <summary>Stable CPU allocation/submission snapshot.</summary>
    public IReadOnlyList<InstancePoolStatistics> GetStatistics()
    {
        RequireMainThread();
        return _orderedPools.Select(pool => pool.Statistics).ToArray();
    }

    public IInstancePool Mount(InstancePoolSpec spec)
    {
        RequireMounted();
        ArgumentNullException.ThrowIfNull(spec);
        ValidateIdentityAndCapacity(spec.Id, spec.Capacity, spec.Layers, spec.CustomAabb);
        ValidateRaw(spec);
        RequireUnique(spec.Id);
        var vertices = new Vector3[spec.Position.Length / 3];
        var uv = new Vector2[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new(spec.Position[i * 3], spec.Position[i * 3 + 1], spec.Position[i * 3 + 2]);
            uv[i] = new(spec.Uv[i * 2], spec.Uv[i * 2 + 1]);
        }
        using var arrays = new Godot.Collections.Array();
        if (arrays.Resize((int)Mesh.ArrayType.Max) != Error.Ok)
            throw new InvalidOperationException("Cannot allocate instance-pool surface arrays.");
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uv;
        arrays[(int)Mesh.ArrayType.Index] = spec.Index;
        var mesh = new ArrayMesh();
        try
        {
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            if (mesh.GetSurfaceCount() != 1) throw new InvalidOperationException("Godot rejected the instance-pool surface.");
            mesh.SurfaceSetMaterial(0, spec.Material);
        }
        catch { mesh.Dispose(); throw; }
        return Publish(spec.Id, spec.Capacity, spec.CustomAabb, spec.Layers,
            spec.CastShadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
            null, false, mesh, spec.Index.Length / 3);
    }

    public IInstancePool Mount(GeometryInstancePoolSpec spec)
    {
        RequireMounted();
        ArgumentNullException.ThrowIfNull(spec);
        ValidateIdentityAndCapacity(spec.Id, spec.Capacity, spec.Layers, spec.CustomAabb);
        ArgumentNullException.ThrowIfNull(spec.Prototype);
        ValidateShadows(spec.Shadows);
        RequireUnique(spec.Id);
        if (spec.Map is { } map)
        {
            ValidateMapId(map);
            if (_activeMap != map)
                throw new InvalidOperationException("The pool map is not active; stale or pre-load mounts are rejected.");
        }
        if (spec.Prototype.Surfaces.Any(surface => surface.Material.Spec.SurfacePattern is not null))
            throw new NotSupportedException("Surface patterns currently require direct world-coordinate geometry, not instance pools.");
        _materials ??= KernelServices.Get<IToonMaterialProvider>();
        var catalog = _catalog ??= KernelServices.Get<IPaletteCatalogSource>().Catalog;
        var triangles = 0;
        foreach (var surface in spec.Prototype.Surfaces)
        {
            catalog.Validate(surface.Material.Spec);
            if (surface.Reflect || surface.Variant is not null)
                throw new NotSupportedException("Geometry-pool reflection proxies and variant masks are not implemented.");
            if (surface.Position.Length < 9 || surface.Index.Length == 0)
                throw new ArgumentException("Every prototype surface must contain drawable triangles.", nameof(spec));
            ArgumentNullException.ThrowIfNull(surface.Material.Spec);
            if (surface.Shader is { } shader && !Enum.IsDefined(shader))
                throw new ArgumentOutOfRangeException(nameof(spec), "Unknown surface shader.");
            if (surface.Lods.Select(lod => lod.Error).Distinct().Count() != surface.Lods.Count)
                throw new ArgumentException("Duplicate LOD errors cannot preserve every index buffer.", nameof(spec));
            triangles = checked(triangles + surface.Index.Length / 3);
        }

        if (spec.Map is not null && _prototypeMeshes.TryGetValue(spec.Prototype.Id, out var shared))
        {
            shared.References = checked(shared.References + 1);
            return Publish(spec.Id, spec.Capacity, spec.CustomAabb, spec.Layers, spec.Shadows,
                spec.Map, shared.IgnoreOcclusion, shared.Mesh, shared.Triangles, shared);
        }

        var provider = _materials ??= KernelServices.Get<IToonMaterialProvider>();
        var mesh = new ArrayMesh();
        SharedPrototypeMesh? sharedMesh = null;
        var ignoreOcclusion = spec.Prototype.Surfaces.Any(surface => surface.IgnoreOcclusionCulling);
        try
        {
            for (var index = 0; index < spec.Prototype.Surfaces.Count; index++)
            {
                var surface = spec.Prototype.Surfaces[index];
                var material = surface.Shader is { } shader
                    ? ResolveShader(shader) : provider.Get(surface.Material.Spec);
                RequireSpatialMaterial(material);
                var slot = GeometrySurfaceUpload.Append(mesh, surface, catalog: catalog);
                if (slot != index) throw new InvalidOperationException("Prototype material slots changed during upload.");
                mesh.SurfaceSetMaterial(slot, material);
            }
            if (spec.Map is not null)
            {
                sharedMesh = new(spec.Prototype.Id, mesh, triangles, ignoreOcclusion);
                _prototypeMeshes.Add(sharedMesh.Id, sharedMesh);
            }
        }
        catch { mesh.Dispose(); throw; }

        return Publish(spec.Id, spec.Capacity, spec.CustomAabb, spec.Layers, spec.Shadows,
            spec.Map, ignoreOcclusion, mesh, triangles, sharedMesh);
    }

    private IInstancePool Publish(string id, int capacity, Aabb bounds, uint layers,
        GeometryInstance3D.ShadowCastingSetting shadows, MapId? map, bool ignoreOcclusion,
        ArrayMesh mesh, int triangles, SharedPrototypeMesh? sharedMesh = null)
    {
        MultiMesh? multiMesh = null;
        MultiMeshInstance3D? instance = null;
        InstancePool? pool = null;
        try
        {
            multiMesh = new MultiMesh();
            multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            multiMesh.UseColors = false;
            multiMesh.UseCustomData = true;
            multiMesh.Mesh = mesh;
            multiMesh.CustomAabb = bounds;
            multiMesh.InstanceCount = capacity;
            multiMesh.VisibleInstanceCount = 0;
            if (multiMesh.InstanceCount != capacity) throw new InvalidOperationException("Godot rejected pool capacity.");
            instance = new MultiMeshInstance3D();
            instance.Name = $"InstancePool_{checked(++_nextNodeId)}";
            instance.Multimesh = multiMesh;
            instance.Layers = layers;
            instance.CustomAabb = bounds;
            instance.CastShadow = shadows;
            instance.IgnoreOcclusionCulling = ignoreOcclusion;
            instance.Visible = false;
            instance.SetMeta("instance_pool_id", id);
            if (map is { } mapId) instance.SetMeta("map_id", mapId.Value);
            pool = new InstancePool(this, id, capacity, triangles, shadows, map,
                mesh.GetSurfaceCount(), instance, multiMesh, mesh, sharedMesh);
            AddChild(instance);
            if (instance.GetParent() != this) throw new InvalidOperationException("Godot rejected the pool node.");
            RequireMounted();
            _pools.Add(id, pool);
            _orderedPools.Add(pool);
            _orderedPools.Sort(static (a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            return pool;
        }
        catch
        {
            if (pool is not null) pool.Free();
            else
            {
                try
                {
                    if (GodotObject.IsInstanceValid(instance)) instance!.Free();
                }
                finally
                {
                    try { multiMesh?.Dispose(); }
                    finally { ReleaseMesh(mesh, sharedMesh); }
                }
            }
            throw;
        }
    }

    private ShaderMaterial ResolveShader(ShaderId shader)
    {
        if (_shaderMaterials.TryGetValue(shader, out var cached)) return cached;
        var material = (_shaderSource ??= KernelServices.Get<IShaderMaterialSource>()).Create(shader);
        try { RequireSpatialMaterial(material); }
        catch { material?.Dispose(); throw; }
        _shaderMaterials.Add(shader, material);
        return material;
    }

    internal void Release(InstancePool pool)
    {
        if (_pools.TryGetValue(pool.Id, out var mounted) && ReferenceEquals(mounted, pool)) _pools.Remove(pool.Id);
        _orderedPools.Remove(pool);
    }

    internal void ReleaseMesh(ArrayMesh mesh, SharedPrototypeMesh? sharedMesh)
    {
        if (sharedMesh is not null)
        {
            if (sharedMesh.References > 1) { sharedMesh.References--; return; }
            if (_prototypeMeshes.TryGetValue(sharedMesh.Id, out var current) && ReferenceEquals(current, sharedMesh))
                { mesh.Dispose(); _prototypeMeshes.Remove(sharedMesh.Id); return; }
        }
        mesh.Dispose();
    }

    /// <summary>Outstanding borrows include publication in progress; no entry survives its map load.</summary>
    internal sealed class SharedPrototypeMesh(string id, ArrayMesh mesh, int triangles, bool ignoreOcclusion)
    {
        internal string Id { get; } = id;
        internal ArrayMesh Mesh { get; } = mesh;
        internal int Triangles { get; } = triangles;
        internal bool IgnoreOcclusion { get; } = ignoreOcclusion;
        internal int References { get; set; } = 1;
    }

    internal static void RequireMainThread()
    {
        if (!GodotThread.IsMainThread())
            throw new InvalidOperationException("Instance-pool operations must run on the Godot main thread.");
    }
    private void RequireMounted()
    {
        RequireMainThread();
        if (!_mounted || !_accepting || _exiting || !IsInsideTree() || IsQueuedForDeletion())
            throw new InvalidOperationException("The instance-pool host must be mounted before its consumers.");
    }
    private void RequireUnique(string id)
    {
        if (_pools.ContainsKey(id)) throw new InvalidOperationException($"Instance pool already mounted: {id}.");
    }
    internal static void ValidateAabb(Aabb box)
    {
        if (!box.IsFinite() || !box.End.IsFinite() || box.Size.X < 0 || box.Size.Y < 0 || box.Size.Z < 0)
            throw new ArgumentException("Pool bounds must be finite with nonnegative size and finite endpoints.", nameof(box));
    }
    internal static void ValidateFinite(ReadOnlySpan<float> data, string parameter)
    {
        foreach (var value in data)
            if (!float.IsFinite(value)) throw new ArgumentException("Pool data must be finite.", parameter);
    }
    private static void ValidateMapId(MapId map)
    {
        if (string.IsNullOrWhiteSpace(map.Value) || map.Value != map.Value.Trim())
            throw new ArgumentException("Map ownership must identify a nonempty map.", nameof(map));
    }
    private static void ValidateShadows(GeometryInstance3D.ShadowCastingSetting shadows)
    {
        switch (shadows)
        {
            case GeometryInstance3D.ShadowCastingSetting.Off:
            case GeometryInstance3D.ShadowCastingSetting.On:
            case GeometryInstance3D.ShadowCastingSetting.DoubleSided:
            case GeometryInstance3D.ShadowCastingSetting.ShadowsOnly: break;
            default: throw new ArgumentOutOfRangeException(nameof(shadows));
        }
    }
    private static void ValidateIdentityAndCapacity(string id, int capacity, uint layers, Aabb bounds)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 256 || id != id.Trim() || id.IndexOf('.') is <= 0 || id.EndsWith('.'))
            throw new ArgumentException("Pool Id must have a domain prefix and local name separated by a dot, be trimmed, and be at most 256 characters.", nameof(id));
        if (capacity <= 0 || capacity > System.Array.MaxLength / FloatsPerInstance)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must fit a complete 16-float buffer.");
        if ((layers & ~ValidLayers) != 0) throw new ArgumentOutOfRangeException(nameof(layers));
        ValidateAabb(bounds);
    }
    private static void RequireSpatialMaterial(Material material)
    {
        if (material is not ShaderMaterial shaderMaterial || !GodotObject.IsInstanceValid(material) ||
            !GodotObject.IsInstanceValid(shaderMaterial.Shader) || shaderMaterial.Shader.GetMode() != Shader.Mode.Spatial)
            throw new ArgumentException("Pools require a live spatial shader material.", nameof(material));
    }
    private static void ValidateRaw(InstancePoolSpec spec)
    {
        if (!Enum.IsDefined(spec.Shader)) throw new ArgumentOutOfRangeException(nameof(spec), "Unknown pool shader.");
        ArgumentNullException.ThrowIfNull(spec.Position);
        ArgumentNullException.ThrowIfNull(spec.Uv);
        ArgumentNullException.ThrowIfNull(spec.Index);
        if (spec.Position.Length < 9 || spec.Position.Length % 3 != 0)
            throw new ArgumentException("Position must contain at least three xyz vertices.", nameof(spec));
        var count = spec.Position.Length / 3;
        if (spec.Uv.Length != count * 2) throw new ArgumentException("Uv must contain one pair per vertex.", nameof(spec));
        if (spec.Index.Length == 0 || spec.Index.Length % 3 != 0)
            throw new ArgumentException("Index must contain complete triangles.", nameof(spec));
        ValidateFinite(spec.Position, nameof(spec.Position));
        ValidateFinite(spec.Uv, nameof(spec.Uv));
        foreach (var index in spec.Index)
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(spec), "Index is outside Position.");
        RequireSpatialMaterial(spec.Material);
    }
}

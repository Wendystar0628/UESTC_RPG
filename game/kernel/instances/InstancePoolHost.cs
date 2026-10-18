using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Instances;

/// <summary>Kernel-owned dynamic instances, with map cleanup and the existing material/prewarm bridges.</summary>
[KernelNode("kernel.instance-pools", Owner = "WP-00")]
[KernelService(typeof(IInstancePoolHost), Owner = "WP-00")]
public sealed partial class InstancePoolHost : Node3D, IInstancePoolHost, IMapAware
{
    internal const int FloatsPerInstance = InstanceStreamLayout.FloatsPerInstance;
    private const uint ValidLayers = (1u << 20) - 1;
    private readonly Dictionary<(string OwnerWp, string Id), InstancePool> _pools = [];
    private readonly List<InstancePool> _orderedPools = [];
    private readonly Dictionary<ShaderId, ShaderMaterial> _shaderMaterials = [];
    private IToonMaterialProvider? _materials;
    private PaletteCatalog? _catalog;
    private IShaderMaterialSource? _shaderSource;
    private MapId? _activeMap;
    private bool _mounted;
    private bool _exiting;
    private long _nextNodeId;

    public InstancePoolHost() { }
    public InstancePoolHost(IToonMaterialProvider materials, IShaderMaterialSource shaderSource, PaletteCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(shaderSource);
        ArgumentNullException.ThrowIfNull(catalog);
        _materials = materials;
        _catalog = catalog;
        _shaderSource = shaderSource;
    }

    public override void _EnterTree()
    {
        RequireMainThread();
        ProcessPriority = FramePriority.StreamMount;
        _mounted = true;
        _exiting = false;
    }

    public override void _Process(double delta)
    {
        foreach (var pool in _orderedPools) pool.RefreshPreparation();
    }

    public override void _ExitTree()
    {
        RequireMainThread();
        _mounted = false;
        _exiting = true;
        _activeMap = null;
        try
        {
            foreach (var pool in _orderedPools.ToArray()) pool.Free();
        }
        finally
        {
            // Created shader materials are cached for this host lifetime; borrowed Toon materials are never disposed.
            foreach (var pair in _shaderMaterials.OrderBy(pair => pair.Key)) pair.Value.Dispose();
            _shaderMaterials.Clear();
        }
    }

    public void OnMapUnloading(MapId from)
    {
        RequireMainThread();
        ValidateMapId(from);
        if (_activeMap == from) _activeMap = null;
        foreach (var pool in _orderedPools.Where(pool => pool.Map == from).ToArray()) pool.Free();
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        RequireMounted();
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(ctx.Descriptor);
        var map = ctx.Descriptor.Id;
        ValidateMapId(map);
        if (_activeMap is { } active && active != map)
            throw new InvalidOperationException("Unload the previous pool map before loading another map.");
        _activeMap = map;
    }

    /// <summary>Stable CPU allocation/submission snapshot; warm readiness is separate from requested visibility.</summary>
    public IReadOnlyList<InstancePoolStatistics> GetStatistics()
    {
        RequireMainThread();
        return _orderedPools.Select(pool => pool.Statistics).ToArray();
    }

    public IInstancePool Mount(InstancePoolSpec spec)
    {
        RequireMounted();
        ArgumentNullException.ThrowIfNull(spec);
        ValidateIdentityAndCapacity(spec.Id, spec.OwnerWp, spec.Capacity, spec.Layers, spec.CustomAabb);
        ValidateLegacy(spec);
        RequireUnique(spec.OwnerWp, spec.Id);

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
        // The legacy caller-supplied material stays borrowed; its established readiness contract is unchanged.
        return Publish(spec.Id, spec.OwnerWp, spec.Capacity, spec.CustomAabb, spec.Layers,
            spec.CastShadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
            null, false, mesh, spec.Index.Length / 3, null);
    }

    public IInstancePool Mount(GeometryInstancePoolSpec spec)
    {
        RequireMounted();
        ArgumentNullException.ThrowIfNull(spec);
        ValidateIdentityAndCapacity(spec.Id, spec.OwnerWp, spec.Capacity, spec.Layers, spec.CustomAabb);
        ArgumentNullException.ThrowIfNull(spec.Prototype);
        if (spec.Prototype.Surfaces.Any(surface => surface.Material.Spec.SurfacePattern is not null))
            throw new NotSupportedException("Surface patterns currently require direct world-coordinate geometry, not instance pools.");
        ValidateShadows(spec.Shadows);
        RequireUnique(spec.OwnerWp, spec.Id);
        if (spec.Map is { } map)
        {
            ValidateMapId(map);
            if (_activeMap != map)
                throw new InvalidOperationException("The pool map is not active; stale or pre-load mounts are rejected.");
        }
        _materials ??= Services.Get<IToonMaterialProvider>();
        var catalog = _catalog ??= Services.Get<IPaletteCatalogSource>().Catalog;
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

        var provider = _materials ??= Services.Get<IToonMaterialProvider>();
        var bindings = new PoolMaterialBinding[spec.Prototype.Surfaces.Count];
        var mesh = new ArrayMesh();
        try
        {
            for (var index = 0; index < spec.Prototype.Surfaces.Count; index++)
            {
                var surface = spec.Prototype.Surfaces[index];
                var material = surface.Shader is { } shader
                    ? ResolveShader(shader, provider) : provider.Get(surface.Material.Spec);
                RequireSpatialMaterial(material);
                var slot = GeometrySurfaceUpload.Append(mesh, surface, catalog: catalog);
                if (slot != index) throw new InvalidOperationException("Prototype material slots changed during upload.");
                mesh.SurfaceSetMaterial(slot, material);
                provider.TrackLayout(material, new WarmDrawLayout(MeshSurfaceFormat.Read(mesh, slot), Instanced: true));
                bindings[index] = new(material, surface.Shader, surface.Material.Spec);
            }
        }
        catch { mesh.Dispose(); throw; }

        return Publish(spec.Id, spec.OwnerWp, spec.Capacity, spec.CustomAabb, spec.Layers, spec.Shadows,
            spec.Map, spec.Prototype.Surfaces.Any(surface => surface.IgnoreOcclusionCulling), mesh, triangles,
            () => AreMaterialsReady(provider, bindings));
    }

    private IInstancePool Publish(string id, string owner, int capacity, Aabb bounds, uint layers,
        GeometryInstance3D.ShadowCastingSetting shadows, MapId? map, bool ignoreOcclusion,
        ArrayMesh mesh, int triangles, Func<bool>? ready)
    {
        MultiMesh? multiMesh = null;
        MultiMeshInstance3D? instance = null;
        try
        {
            multiMesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = false,
                UseCustomData = true,
                Mesh = mesh,
                CustomAabb = bounds,
                InstanceCount = capacity,
                VisibleInstanceCount = 0
            };
            if (multiMesh.InstanceCount != capacity) throw new InvalidOperationException("Godot rejected pool capacity.");
            instance = new MultiMeshInstance3D
            {
                Name = $"InstancePool_{checked(++_nextNodeId)}",
                Multimesh = multiMesh,
                Layers = layers,
                CustomAabb = bounds,
                CastShadow = shadows,
                IgnoreOcclusionCulling = ignoreOcclusion,
                Visible = ready is null
            };
            instance.SetMeta("owner_wp", owner);
            instance.SetMeta("instance_pool_id", id);
            if (map is { } mapId) instance.SetMeta("map_id", mapId.Value);
            var pool = new InstancePool(this, id, owner, capacity, triangles, shadows, map,
                mesh.GetSurfaceCount(), instance, multiMesh, mesh, ready);
            pool.RefreshPreparation();
            AddChild(instance);
            if (instance.GetParent() != this) throw new InvalidOperationException("Godot rejected the pool node.");
            _pools.Add((owner, id), pool);
            _orderedPools.Add(pool);
            _orderedPools.Sort(static (a, b) =>
            {
                var ownerOrder = StringComparer.Ordinal.Compare(a.OwnerWp, b.OwnerWp);
                return ownerOrder != 0 ? ownerOrder : StringComparer.Ordinal.Compare(a.Id, b.Id);
            });
            return pool;
        }
        catch
        {
            if (GodotObject.IsInstanceValid(instance)) instance!.Free();
            multiMesh?.Dispose();
            mesh.Dispose();
            throw;
        }
    }

    private ShaderMaterial ResolveShader(ShaderId shader, IToonMaterialProvider provider)
    {
        if (_shaderMaterials.TryGetValue(shader, out var cached)) return cached;
        var material = (_shaderSource ??= Services.Get<IShaderMaterialSource>()).Create(shader);
        try { RequireSpatialMaterial(material); provider.Track(shader, material); }
        catch { material?.Dispose(); throw; }
        _shaderMaterials.Add(shader, material);
        return material;
    }

    private static bool AreMaterialsReady(IToonMaterialProvider provider, PoolMaterialBinding[] bindings)
    {
        foreach (var binding in bindings)
            if (!GodotObject.IsInstanceValid(binding.Material) ||
                !(binding.Shader is { } shader ? provider.IsReady(shader, binding.Material) : provider.IsReady(binding.Spec)))
                return false;
        return true;
    }

    private readonly record struct PoolMaterialBinding(Material Material, ShaderId? Shader, ToonSpec Spec);
    internal bool IsExiting => _exiting;
    internal void Release(InstancePool pool)
    {
        _pools.Remove((pool.OwnerWp, pool.Id));
        _orderedPools.Remove(pool);
    }

    internal static void RequireMainThread()
    {
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Instance-pool operations must run on the Godot main thread.");
    }
    private void RequireMounted()
    {
        RequireMainThread();
        if (!_mounted || _exiting || !IsInsideTree() || IsQueuedForDeletion())
            throw new InvalidOperationException("The instance-pool host must be mounted before its consumers.");
    }
    private void RequireUnique(string owner, string id)
    {
        if (_pools.ContainsKey((owner, id))) throw new InvalidOperationException($"Instance pool already mounted: {owner}/{id}.");
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
    private static void ValidateIdentityAndCapacity(string id, string owner, int capacity, uint layers, Aabb bounds)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 256 || id != id.Trim())
            throw new ArgumentException("Pool Id must be nonempty, trimmed, and at most 256 characters.", nameof(id));
        if (owner is null || owner.Length is < 5 or > 6 || !owner.StartsWith("WP-", StringComparison.Ordinal) ||
            !char.IsAsciiDigit(owner[3]) || !char.IsAsciiDigit(owner[4]) ||
            (owner.Length == 6 && owner[5] is not (>= 'a' and <= 'z')))
            throw new ArgumentException("Owner must be a work-package ID such as WP-00 or WP-45b.", nameof(owner));
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
    private static void ValidateLegacy(InstancePoolSpec spec)
    {
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

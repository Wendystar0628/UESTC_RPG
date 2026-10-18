using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Instances;

/// <summary>Owns one node, MultiMesh and multi-surface mesh. Materials belong to the provider or host cache.</summary>
internal sealed class InstancePool(
    InstancePoolHost host, string id, string ownerWp, int capacity, int meshTriangles,
    GeometryInstance3D.ShadowCastingSetting shadows, MapId? map, int surfaceCount,
    MultiMeshInstance3D instance, MultiMesh multiMesh, ArrayMesh mesh, Func<bool>? preparation) : IInstancePool
{
    private MultiMeshInstance3D? _instance = instance;
    private MultiMesh? _multiMesh = multiMesh;
    private ArrayMesh? _mesh = mesh;
    private Func<bool>? _preparation = preparation;
    private bool _materialReady = preparation is null;
    private bool _freed;
    private bool _hasBuffer;
    private int _visibleCount;

    public string Id { get; } = id;
    public string OwnerWp { get; } = ownerWp;
    public MapId? Map { get; } = map;
    public int Capacity { get; } = capacity;
    internal InstancePoolStatistics Statistics => new(Id, OwnerWp, Capacity, _visibleCount, meshTriangles,
        shadows != GeometryInstance3D.ShadowCastingSetting.Off)
    {
        Map = Map,
        SurfaceCount = surfaceCount,
        Shadows = shadows,
        MaterialReady = _materialReady
    };

    public void WriteBuffer(ReadOnlySpan<float> data)
    {
        RequireLive();
        if (data.Length != Capacity * InstancePoolHost.FloatsPerInstance)
            throw new ArgumentException("Buffer must contain exactly Capacity * 16 floats.", nameof(data));
        InstancePoolHost.ValidateFinite(data, nameof(data));
        _multiMesh!.SetBuffer(data);
        _hasBuffer = true;
    }

    public int VisibleCount
    {
        get { RequireLive(); return _visibleCount; }
        set
        {
            RequireLive();
            if (value < 0 || value > Capacity) throw new ArgumentOutOfRangeException(nameof(value));
            if (value > 0 && !_hasBuffer)
                throw new InvalidOperationException("Write the complete instance buffer before showing the pool.");
            RefreshPreparation();
            _multiMesh!.VisibleInstanceCount = value;
            _visibleCount = value;
        }
    }

    internal void RefreshPreparation()
    {
        if (_freed || _preparation is null) return;
        bool ready;
        try { ready = _preparation(); }
        catch
        {
            // A provider failure must not leave a previously warmed draw visible.
            _instance!.Visible = false;
            _materialReady = false;
            throw;
        }
        if (ready == _materialReady) return;
        _materialReady = ready;
        _instance!.Visible = ready;
    }

    public void SetParameter(StringName name, Vector4 value)
    {
        RequireLive();
        ArgumentNullException.ThrowIfNull(name);
        if (name != InstanceShaderNames.PrecipD && name != InstanceShaderNames.PrecipV && name != InstanceShaderNames.PrecipK)
            throw new ArgumentException("The name is not a registered vec4 instance parameter.", nameof(name));
        if (!value.IsFinite()) throw new ArgumentException("Instance parameters must be finite.", nameof(value));
        _instance!.SetInstanceShaderParameter(name, value);
    }

    public void SetParameter(StringName name, float value)
    {
        RequireLive();
        ArgumentNullException.ThrowIfNull(name);
        if (name != InstanceShaderNames.ShadowProxy)
            throw new ArgumentException("Only the registered shadow_proxy scalar is supported by instance pools.", nameof(name));
        if (!float.IsFinite(value)) throw new ArgumentException("Instance parameters must be finite.", nameof(value));
        _instance!.SetInstanceShaderParameter(name, value);
    }

    public void SetAabb(Aabb box)
    {
        RequireLive();
        InstancePoolHost.ValidateAabb(box);
        _multiMesh!.CustomAabb = box;
        _instance!.CustomAabb = box;
    }

    public void Free()
    {
        InstancePoolHost.RequireMainThread();
        if (_freed) return;
        _freed = true;
        _visibleCount = 0;
        _materialReady = false;
        _preparation = null;
        host.Release(this);
        var ownedInstance = _instance;
        var ownedMultiMesh = _multiMesh;
        var ownedMesh = _mesh;
        _instance = null;
        _multiMesh = null;
        _mesh = null;
        try
        {
            if (GodotObject.IsInstanceValid(ownedInstance))
            {
                ownedInstance!.Multimesh = null;
                if (host.IsExiting) ownedInstance.QueueFree();
                else ownedInstance.Free();
            }
        }
        finally
        {
            try { ownedMultiMesh?.Dispose(); }
            finally { ownedMesh?.Dispose(); }
        }
    }

    private void RequireLive()
    {
        InstancePoolHost.RequireMainThread();
        ObjectDisposedException.ThrowIf(_freed, this);
    }
}

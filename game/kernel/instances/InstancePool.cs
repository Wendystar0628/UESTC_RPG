using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Instances;

/// <summary>Owns one node and MultiMesh, and returns its mesh ownership or shared borrow to the host.</summary>
internal sealed class InstancePool(
    InstancePoolHost host, string id, int capacity, int meshTriangles,
    GeometryInstance3D.ShadowCastingSetting shadows, MapId? map, int surfaceCount,
    MultiMeshInstance3D instance, MultiMesh multiMesh, ArrayMesh mesh,
    InstancePoolHost.SharedPrototypeMesh? sharedMesh) : IInstancePool
{
    private static readonly StringName _shaderInstancePrecipD = InstanceShaderParameters.PrecipD;
    private static readonly StringName _shaderInstancePrecipK = InstanceShaderParameters.PrecipK;
    private static readonly StringName _shaderInstancePrecipV = InstanceShaderParameters.PrecipV;
    private static readonly StringName _shaderInstanceShadowProxy = InstanceShaderParameters.ShadowProxy;

    private MultiMeshInstance3D? _instance = instance;
    private MultiMesh? _multiMesh = multiMesh;
    private ArrayMesh? _mesh = mesh;
    private InstancePoolHost.SharedPrototypeMesh? _sharedMesh = sharedMesh;
    private bool _freed;
    private bool _hasBuffer;
    private int _visibleCount;

    public string Id { get; } = id;
    public MapId? Map { get; } = map;
    public int Capacity { get; } = capacity;
    public bool IsReady
    {
        get
        {
            RequireLive();
            return _hasBuffer;
        }
    }
    internal InstancePoolStatistics Statistics => new(Id, Capacity, _visibleCount, meshTriangles,
        shadows != GeometryInstance3D.ShadowCastingSetting.Off)
    {
        Map = Map,
        SurfaceCount = surfaceCount,
        Shadows = shadows
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
            _multiMesh!.VisibleInstanceCount = value;
            _visibleCount = value;
            _instance!.Visible = value > 0;
        }
    }

    public void SetParameter(StringName name, Vector4 value)
    {
        RequireLive();
        ArgumentNullException.ThrowIfNull(name);
        if (name != _shaderInstancePrecipD && name != _shaderInstancePrecipV && name != _shaderInstancePrecipK)
            throw new ArgumentException("The name is not a registered vec4 instance parameter.", nameof(name));
        if (!value.IsFinite()) throw new ArgumentException("Instance parameters must be finite.", nameof(value));
        _instance!.SetInstanceShaderParameter(name, value);
    }

    public void SetParameter(StringName name, float value)
    {
        RequireLive();
        ArgumentNullException.ThrowIfNull(name);
        if (name != _shaderInstanceShadowProxy)
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
        host.Release(this);
        var ownedInstance = _instance;
        var ownedMultiMesh = _multiMesh;
        var ownedMesh = _mesh;
        var borrowedMesh = _sharedMesh;
        _instance = null;
        _multiMesh = null;
        _mesh = null;
        _sharedMesh = null;
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
            finally { if (ownedMesh is not null) host.ReleaseMesh(ownedMesh, borrowedMesh); }
        }
    }

    private void RequireLive()
    {
        InstancePoolHost.RequireMainThread();
        ObjectDisposedException.ThrowIf(_freed, this);
    }
}

using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Instances;

/// <summary>Owns exactly one node, one MultiMesh, and one ArrayMesh; never owns the material.</summary>
internal sealed class InstancePool(
    InstancePoolHost host,
    string id,
    string ownerWp,
    int capacity,
    int meshTriangles,
    bool castShadows,
    MultiMeshInstance3D instance,
    MultiMesh multiMesh,
    ArrayMesh mesh) : IInstancePool
{
    private MultiMeshInstance3D? _instance = instance;
    private MultiMesh? _multiMesh = multiMesh;
    private ArrayMesh? _mesh = mesh;
    private bool _freed;
    private bool _hasBuffer;
    private int _visibleCount;

    public string Id { get; } = id;
    public string OwnerWp { get; } = ownerWp;
    public int Capacity { get; } = capacity;
    internal InstancePoolStatistics Statistics => new(Id, OwnerWp, Capacity, _visibleCount, meshTriangles, castShadows);

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
        get
        {
            RequireLive();
            return _visibleCount;
        }
        set
        {
            RequireLive();
            if (value < 0 || value > Capacity)
                throw new ArgumentOutOfRangeException(nameof(value), "VisibleCount must be between zero and Capacity.");
            if (value > 0 && !_hasBuffer)
                throw new InvalidOperationException("Write the complete instance buffer before showing the pool.");
            _multiMesh!.VisibleInstanceCount = value;
            _visibleCount = value;
        }
    }

    public void SetParameter(StringName name, Vector4 value)
    {
        RequireLive();
        ArgumentNullException.ThrowIfNull(name);
        // The other registered instance uniforms are scalars; this API deliberately accepts only vec4.
        if (name != InstanceShaderNames.PrecipD && name != InstanceShaderNames.PrecipV && name != InstanceShaderNames.PrecipK)
            throw new ArgumentException("The name is not a registered vec4 instance shader parameter.", nameof(name));
        if (!value.IsFinite())
            throw new ArgumentException("Instance shader parameters must be finite.", nameof(value));
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
        _instance = null;
        _multiMesh = null;
        _mesh = null;
        try
        {
            if (GodotObject.IsInstanceValid(ownedInstance))
            {
                // Release native references before freeing resources. Material remains caller-owned.
                ownedInstance!.Multimesh = null;
                ownedInstance.MaterialOverride = null;
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

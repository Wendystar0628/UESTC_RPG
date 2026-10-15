using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Instances;

/// <summary>Kernel-owned dynamic instances, shared by all systems and mounted by the stage scanner.</summary>
[KernelNode("kernel.instance-pools", Owner = "WP-00")]
[KernelService(typeof(IInstancePoolHost), Owner = "WP-00")]
public sealed partial class InstancePoolHost : Node3D, IInstancePoolHost
{
    internal const int FloatsPerInstance = 16;
    private const uint ValidLayers = (1u << 20) - 1;
    private readonly Dictionary<(string OwnerWp, string Id), InstancePool> _pools = [];
    private bool _mounted;
    private bool _exiting;
    private long _nextNodeId;

    public override void _EnterTree()
    {
        RequireMainThread();
        _mounted = true;
        _exiting = false;
    }

    public override void _ExitTree()
    {
        RequireMainThread();
        _mounted = false;
        _exiting = true;
        // Snapshot before releasing: each release removes itself from the ownership ledger.
        foreach (var pool in OrderedPools().ToArray()) pool.Free();
    }

    /// <summary>Stable per-owner/per-pool diagnostic snapshot; no mutable engine objects escape.</summary>
    public IReadOnlyList<InstancePoolStatistics> GetStatistics()
    {
        RequireMainThread();
        return OrderedPools().Select(pool => pool.Statistics).ToArray();
    }

    public IInstancePool Mount(InstancePoolSpec spec)
    {
        RequireMainThread();
        if (!_mounted || _exiting || !IsInsideTree() || IsQueuedForDeletion())
            throw new InvalidOperationException("The instance-pool host must be mounted before its consumers.");
        ArgumentNullException.ThrowIfNull(spec);
        ValidateSpec(spec);
        var key = (spec.OwnerWp, spec.Id);
        if (_pools.ContainsKey(key))
            throw new InvalidOperationException($"Instance pool already mounted: {spec.OwnerWp}/{spec.Id}.");

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

        ArrayMesh? mesh = null;
        MultiMesh? multiMesh = null;
        MultiMeshInstance3D? instance = null;
        try
        {
            mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            if (mesh.GetSurfaceCount() != 1)
                throw new InvalidOperationException("Godot rejected the instance-pool mesh surface.");
            // These three format properties must precede InstanceCount, which allocates the buffer.
            multiMesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = false,
                UseCustomData = true,
                Mesh = mesh,
                CustomAabb = spec.CustomAabb,
                InstanceCount = spec.Capacity,
                VisibleInstanceCount = 0
            };
            if (multiMesh.InstanceCount != spec.Capacity)
                throw new InvalidOperationException("Godot rejected the instance-pool capacity.");
            instance = new MultiMeshInstance3D
            {
                Name = $"InstancePool_{checked(++_nextNodeId)}",
                Multimesh = multiMesh,
                MaterialOverride = spec.Material,
                Layers = spec.Layers,
                CustomAabb = spec.CustomAabb,
                CastShadow = spec.CastShadows
                    ? GeometryInstance3D.ShadowCastingSetting.On
                    : GeometryInstance3D.ShadowCastingSetting.Off
            };
            instance.SetMeta("owner_wp", spec.OwnerWp);
            instance.SetMeta("instance_pool_id", spec.Id);
            AddChild(instance);
            if (instance.GetParent() != this)
                throw new InvalidOperationException("Godot rejected the instance-pool child node.");
            var pool = new InstancePool(this, spec.Id, spec.OwnerWp, spec.Capacity,
                spec.Index.Length / 3, spec.CastShadows, instance, multiMesh, mesh);
            _pools.Add(key, pool);
            return pool;
        }
        catch
        {
            if (GodotObject.IsInstanceValid(instance)) instance!.Free();
            multiMesh?.Dispose();
            mesh?.Dispose();
            throw;
        }
    }

    internal void Release(InstancePool pool)
    {
        _pools.Remove((pool.OwnerWp, pool.Id));
    }

    internal bool IsExiting => _exiting;

    internal static void RequireMainThread()
    {
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Instance-pool operations must run on the Godot main thread.");
    }

    internal static void ValidateAabb(Aabb box)
    {
        if (!box.IsFinite() || !box.End.IsFinite() || box.Size.X < 0 || box.Size.Y < 0 || box.Size.Z < 0)
            throw new ArgumentException("Instance-pool bounds must be finite with nonnegative size and finite endpoints.", nameof(box));
    }

    internal static void ValidateFinite(ReadOnlySpan<float> data, string parameter)
    {
        foreach (var value in data)
            if (!float.IsFinite(value))
                throw new ArgumentException("Instance-pool data must contain only finite values.", parameter);
    }

    private IEnumerable<InstancePool> OrderedPools() => _pools.Values
        .OrderBy(pool => pool.OwnerWp, StringComparer.Ordinal)
        .ThenBy(pool => pool.Id, StringComparer.Ordinal);

    private static void ValidateSpec(InstancePoolSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Id) || spec.Id.Length > 256 || spec.Id != spec.Id.Trim())
            throw new ArgumentException("Pool Id must be nonempty, trimmed, and at most 256 characters.", nameof(spec));
        var owner = spec.OwnerWp;
        if (owner is null || owner.Length is < 5 or > 6 || !owner.StartsWith("WP-", StringComparison.Ordinal) ||
            !char.IsAsciiDigit(owner[3]) || !char.IsAsciiDigit(owner[4]) ||
            (owner.Length == 6 && owner[5] is not (>= 'a' and <= 'z')))
            throw new ArgumentException("OwnerWp must be a work-package ID such as WP-00 or WP-45b.", nameof(spec));
        if (spec.Capacity <= 0 || spec.Capacity > System.Array.MaxLength / FloatsPerInstance)
            throw new ArgumentOutOfRangeException(nameof(spec), "Capacity must fit one complete 16-float-per-instance buffer.");
        if ((spec.Layers & ~ValidLayers) != 0)
            throw new ArgumentOutOfRangeException(nameof(spec), "Only the 20 Godot render layers are valid.");
        ArgumentNullException.ThrowIfNull(spec.Position);
        ArgumentNullException.ThrowIfNull(spec.Uv);
        ArgumentNullException.ThrowIfNull(spec.Index);
        if (spec.Position.Length < 9 || spec.Position.Length % 3 != 0)
            throw new ArgumentException("Position must contain at least three xyz vertices.", nameof(spec));
        var vertexCount = spec.Position.Length / 3;
        if (spec.Uv.Length != vertexCount * 2)
            throw new ArgumentException("Uv must contain exactly one uv pair per vertex.", nameof(spec));
        if (spec.Index.Length == 0 || spec.Index.Length % 3 != 0)
            throw new ArgumentException("Index must contain complete triangles.", nameof(spec));
        ValidateFinite(spec.Position, nameof(spec.Position));
        ValidateFinite(spec.Uv, nameof(spec.Uv));
        foreach (var index in spec.Index)
            if (index < 0 || index >= vertexCount)
                throw new ArgumentOutOfRangeException(nameof(spec), "Triangle index lies outside Position.");
        ValidateAabb(spec.CustomAabb);
        if (!GodotObject.IsInstanceValid(spec.Material) || !GodotObject.IsInstanceValid(spec.Material.Shader))
            throw new ArgumentException("Material must be a live ShaderMaterial with a live shader.", nameof(spec));
        if (spec.Material.Shader.GetMode() != Shader.Mode.Spatial)
            throw new ArgumentException("Instance pools require a spatial shader material.", nameof(spec));
    }
}

using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Instances;

/// <summary>
/// One indexed triangle mesh and a fixed-capacity dynamic instance pool.
/// Position contains xyz triples, Uv contains uv pairs, and Index contains triangle triples.
/// Input arrays are copied into engine resources during Mount and are not retained.
/// Material is borrowed: its caller must keep it alive until the pool is freed.
/// Shader identifies the material in the build-time pipeline catalog.
/// CustomAabb is in pool-local coordinates and must include every shader-driven displacement.
/// </summary>
public sealed record InstancePoolSpec(
    string Id,
    string OwnerWp,
    float[] Position,
    float[] Uv,
    int[] Index,
    ShaderMaterial Material,
    ShaderId Shader,
    int Capacity,
    Aabb CustomAabb,
    uint Layers,
    bool CastShadows = false);

/// <summary>
/// Main-thread-only handle owned by its mounting host. The host also frees it when leaving the tree.
/// Capacity remains readable after Free; all other operations except repeated Free reject a freed handle.
/// </summary>
public interface IInstancePool
{
    int Capacity { get; }

    /// <summary>The live pool has its complete instance buffer.</summary>
    bool IsReady { get; }

    /// <summary>
    /// Uploads exactly Capacity * 16 finite floats without retaining the source span.
    /// Each instance is the Godot row-major Transform3D (12 floats), then CustomData (4 floats).
    /// There is no instance color. A complete first upload is required before making instances visible.
    /// </summary>
    void WriteBuffer(ReadOnlySpan<float> data);

    /// <summary>Visible prefix length in [0, Capacity], after a complete buffer upload.</summary>
    int VisibleCount { get; set; }

    /// <summary>Sets a registered vec4 instance uniform; scalar and unregistered names are rejected.</summary>
    void SetParameter(StringName name, Vector4 value);

    /// <summary>Sets an approved scalar instance uniform. Initial vegetation support permits ShadowProxy only; unsupported hosts fail explicitly.</summary>
    void SetParameter(StringName name, float value) =>
        throw new NotSupportedException("This instance-pool implementation does not support scalar instance parameters.");

    void SetAabb(Aabb box);

    /// <summary>Idempotently releases the instance, MultiMesh and mesh, but never the borrowed material.</summary>
    void Free();
}

public interface IInstancePoolHost
{
    /// <summary>
    /// Mounts under the host on the main thread after the host has entered the tree.
    /// The (OwnerWp, Id) pair must be unique among live pools.
    /// </summary>
    IInstancePool Mount(InstancePoolSpec spec);

    /// <summary>Mounts a complete frozen multi-surface prototype using existing material providers. Requires an implemented native upload path.</summary>
    IInstancePool Mount(GeometryInstancePoolSpec spec) =>
        throw new NotSupportedException("This instance-pool implementation does not support complete geometry prototypes.");
}

/// <summary>Allocation and submitted geometry counts, not measured GPU memory or draw calls.</summary>
public sealed record InstancePoolStatistics(
    string Id,
    string OwnerWp,
    int Capacity,
    int VisibleCount,
    int MeshTriangles,
    bool CastShadows)
{
    /// <summary>Null denotes an explicitly global or legacy pool.</summary>
    public MapId? Map { get; init; }
    public int SurfaceCount { get; init; } = 1;
    public GeometryInstance3D.ShadowCastingSetting Shadows { get; init; }
    public long InstanceBufferBytes => (long)Capacity * 16 * sizeof(float);
    public long VisibleTriangles => (long)VisibleCount * MeshTriangles;
}

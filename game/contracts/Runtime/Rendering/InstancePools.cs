using Campus.Contracts;
using Godot;

namespace Campus.Contracts;

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

    /// <summary>Sets an approved scalar instance uniform (ShadowProxy, VegLevel or VegTransitionBase); unsupported hosts fail explicitly.</summary>
    void SetParameter(StringName name, float value) =>
        throw new NotSupportedException("This instance-pool implementation does not support scalar instance parameters.");

    void SetAabb(Aabb box);

    /// <summary>Idempotently releases the instance and MultiMesh, and returns its mesh ownership or shared borrow.
    /// A shared mesh is released after its last pool; borrowed materials are never released.</summary>
    void Free();
}

public interface IInstancePoolHost
{
    /// <summary>
    /// Mounts under the host on the main thread after the host has entered the tree.
    /// The domain-prefixed Id must be unique among live pools.
    /// </summary>
    IInstancePool Mount(InstancePoolSpec spec);

    /// <summary>Mounts a complete frozen multi-surface prototype using existing material providers. Requires an implemented native upload path.</summary>
    IInstancePool Mount(GeometryInstancePoolSpec spec) =>
        throw new NotSupportedException("This instance-pool implementation does not support complete geometry prototypes.");
}

/// <summary>Allocation and submitted geometry counts, not measured GPU memory or draw calls.</summary>
public sealed record InstancePoolStatistics(
    string Id,
    int Capacity,
    int VisibleCount,
    int MeshTriangles,
    bool CastShadows)
{
    /// <summary>Null denotes a pool without map scope.</summary>
    public MapId? Map { get; init; }
    public int SurfaceCount { get; init; } = 1;
    public GeometryInstance3D.ShadowCastingSetting Shadows { get; init; }
    public long InstanceBufferBytes => (long)Capacity * 16 * sizeof(float);
    public long VisibleTriangles => (long)VisibleCount * MeshTriangles;
}

/// <summary>Complete immutable prototype for a dynamic native pool.</summary>
/// <remarks>Prototype vertices are local and immutable. Within one map load, the same complete Prototype.Id denotes
/// identical complete geometry, materials and ordered surface slots. The host uploads that mesh once while pools
/// borrow it; each pool retains its own MultiMesh, buffer, bounds, visibility and instance parameters.
/// A pool without Map owns its uploaded mesh exclusively. WriteBuffer never uploads prototype geometry.
/// Material resolution uses the existing ToonRef/ShaderId provider; there is no caller-supplied material override.</remarks>
public sealed record GeometryInstancePoolSpec(
    string Id, GeometryPrototype Prototype, int Capacity,
    Aabb CustomAabb, uint Layers,
    GeometryInstance3D.ShadowCastingSetting Shadows = GeometryInstance3D.ShadowCastingSetting.Off)
{
    /// <summary>Optional map ownership for unload cleanup and mesh sharing within that map load.
    /// Null is reserved for explicitly global pools with exclusive mesh ownership.</summary>
    public MapId? Map { get; init; }
}

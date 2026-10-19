using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Instances;

/// <summary>Complete immutable prototype for a dynamic native pool.</summary>
/// <remarks>Prototype vertices are local and immutable. The pool copies/uploads them at mount, not on each WriteBuffer.
/// Material resolution uses the existing ToonRef/ShaderId provider; there is no caller-supplied material override.</remarks>
public sealed record GeometryInstancePoolSpec(
    string Id, string OwnerWp, GeometryPrototype Prototype, int Capacity,
    Aabb CustomAabb, uint Layers,
    GeometryInstance3D.ShadowCastingSetting Shadows = GeometryInstance3D.ShadowCastingSetting.Off)
{
    /// <summary>Optional map ownership for unload cleanup; null is reserved for explicitly global pools.</summary>
    public MapId? Map { get; init; }
}

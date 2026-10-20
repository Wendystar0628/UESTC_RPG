using System.Numerics;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

public sealed record PartBudget(int L0, int L1);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PartAttribute(string id) : RegistrationAttribute(RegistrationKind.Part, id, RegistrationLifetime.Global)
{ }
/// <summary>Parameter-independent part capabilities. The composition boundary freezes this list before workers run.</summary>
public interface IPartDef
{
    IReadOnlyList<LodLevel> SupportedLods { get; }
}

public interface IPartDef<TParams> : IPartDef where TParams : notnull
{
    IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }
    /// <summary>Existing generic definitions need no new member; each read returns a validated numeric-order copy of this object's Lods keys.</summary>
    IReadOnlyList<LodLevel> IPartDef.SupportedLods => Parts.PartLodCatalog.FreezeLods(
        Lods?.Keys ?? throw new InvalidOperationException("A part definition must supply its LOD builders."));
    Box3 Bounds(TParams p);
    IReadOnlyDictionary<string, Matrix4x4> Sockets => new Dictionary<string, Matrix4x4>();
    PartBudget Budget { get; }
    IReadOnlyList<Collider> Collision(TParams p) => [];
    IReadOnlyList<SoundSocketDecl> Sounds(TParams p) => [];
    /// <summary>Part-local seat declarations sharing the collision source. The baker applies placement exactly once.</summary>
    IReadOnlyList<SeatDecl> Seats(TParams p) => [];
}
public sealed record PlacedPart(
    PartId Part, object Params, Matrix4x4 At,
    PaletteToken? Tint = null, ChannelId? Emitter = null, VariantTag? Variant = null)
{
    public bool IgnoreOcclusionCulling { get; init; }
}

using System.Numerics;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PartAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
}
[AttributeUsage(AttributeTargets.Property)]
public sealed class ParamAttribute : Attribute
{
    public double Min { get; set; }
    public double Max { get; set; }
}
public interface IPartDef<TParams> where TParams : notnull
{
    IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }
    Box3 Bounds(TParams p);
    IReadOnlyDictionary<string, Matrix4x4> Sockets => new Dictionary<string, Matrix4x4>();
    PartBudget Budget { get; }
    IReadOnlyList<Collider> Collision(TParams p) => [];
    IReadOnlyList<SoundSocketDecl> Sounds(TParams p) => [];
}
public sealed record PlacedPart(
    PartId Part, object Params, Matrix4x4 At,
    PaletteToken? Tint = null, ChannelId? Emitter = null, VariantTag? Variant = null)
{
    public bool IgnoreOcclusionCulling { get; init; }
}

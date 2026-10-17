using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;

namespace Campus.Gen.Parts.Circulation;

/// <summary>What a level keeps (ART-MODELING): bevels only at L0, balusters through L1, coarse silhouettes after.</summary>
internal enum Detail { Full, Plain, Coarse, Mass }

/// <summary>
/// Shared plumbing for WP-11 parts: one build per semantic level through the WP-09 kernel, the declared bounds as the
/// geometry bounds, and a budget that holds for the PartCatalog default instance (parameters scale the real count).
/// </summary>
public abstract class CirculationPart<TParams> : IPartDef<TParams> where TParams : BoxPartParams
{
    private protected CirculationPart(PartBudget budget)
    {
        Budget = budget;
        Lods = new ReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>>(new SortedDictionary<LodLevel, Func<TParams, PartGeometry>>
        {
            [LodLevel.L0] = p => Build(p, Detail.Full),
            [LodLevel.L1] = p => Build(p, Detail.Plain),
            [LodLevel.L2] = p => Build(p, Detail.Coarse),
            [LodLevel.L3] = p => Build(p, Detail.Mass),
            [LodLevel.L4] = p => Build(p, Detail.Mass),
        });
    }

    public IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; }
    // IPartDef.Sockets takes no parameters, so only the parameter-independent origin is published.
    public IReadOnlyDictionary<string, Matrix4x4> Sockets { get; } =
        new ReadOnlyDictionary<string, Matrix4x4>(new SortedDictionary<string, Matrix4x4>(StringComparer.Ordinal) { ["origin"] = Matrix4x4.Identity });

    public Box3 Bounds(TParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Positive(p.Width, nameof(p.Width)); Positive(p.Height, nameof(p.Height)); Positive(p.Depth, nameof(p.Depth));
        return BoundsOf(p);
    }

    public IReadOnlyList<Collider> Collision(TParams p)
    {
        _ = Bounds(p);
        return Colliders(p);
    }

    private protected abstract Box3 BoundsOf(TParams p);
    private protected abstract IReadOnlyList<Collider> Colliders(TParams p);
    private protected abstract void Compose(MeshBuilder mesh, TParams p, Detail detail);

    private PartGeometry Build(TParams p, Detail detail)
    {
        var bounds = Bounds(p);
        var mesh = new MeshBuilder();
        Compose(mesh, p, detail);
        return mesh.Finish(bounds);
    }

    private protected static Box3 Centered(float width, float y0, float y1, float depth) =>
        new(new(-width * 0.5f, y0, -depth * 0.5f), new(width * 0.5f, y1, depth * 0.5f));

    /// <summary>The caller's material wins unless it is the untouched BoxPartParams default (plain wall).</summary>
    private protected static ToonSpec Pick(ToonSpec given, ToonSpec fallback) => given == DefaultMaterial ? fallback : given;

    private protected static FootMat Foot(FootMat given, FootMat fallback) => given == FootMat.Auto ? fallback : given;

    private protected static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }

    private protected static void Range(float value, float min, float max, string name)
    {
        if (!float.IsFinite(value) || value < min || value > max)
            throw new ArgumentOutOfRangeException(name, $"{name} must lie in [{min}, {max}]; got {value}.");
    }

    private static readonly ToonSpec DefaultMaterial = new FurnitureParams().Material;
}

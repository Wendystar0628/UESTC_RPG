using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// Plumbing shared by every WP-10 part: semantic LODs, bounds, sockets and parameter checks.
/// A level that adds nothing returns an empty geometry: the level keeps no copy of the part (ART-MODELING).
/// </summary>
public abstract class OpeningPart<TParams> : IPartDef<TParams> where TParams : BoxPartParams
{
    protected OpeningPart(PartBudget budget)
    {
        Budget = budget;
        var levels = new SortedDictionary<LodLevel, Func<TParams, PartGeometry>>();
        foreach (var level in Enum.GetValues<LodLevel>()) levels.Add(level, p => Make(p, level));
        Lods = new ReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>>(levels);
    }

    public IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }

    /// <summary>Triangles at L0 and L1 for the reference parameters used in the tests; grids scale with columns × rows.</summary>
    public PartBudget Budget { get; }

    /// <summary>"origin" and "glass" coincide: the bottom centre of the clear opening on the glass plane.</summary>
    public IReadOnlyDictionary<string, Matrix4x4> Sockets { get; } = new ReadOnlyDictionary<string, Matrix4x4>(
        new SortedDictionary<string, Matrix4x4>(StringComparer.Ordinal) { ["glass"] = Matrix4x4.Identity, ["origin"] = Matrix4x4.Identity });

    public Box3 Bounds(TParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Positive(p.Width, nameof(p.Width)); Positive(p.Height, nameof(p.Height)); Positive(p.Depth, nameof(p.Depth));
        if (!float.IsFinite(p.Bevel) || p.Bevel < 0) throw new ArgumentOutOfRangeException(nameof(p), "Bevel must be finite and nonnegative.");
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), "Variation must be 0, 1 or 2.");
        Validate(p);
        // One millimetre of slack covers float rounding in mitred joints.
        var box = Extent(p);
        return new(box.Min - new Vector3(Slack), box.Max + new Vector3(Slack));
    }

    private const float Slack = 0.001f;

    public virtual IReadOnlyList<Collider> Collision(TParams p)
    {
        _ = Bounds(p);
        return [];
    }

    protected abstract Box3 Extent(TParams p);
    protected virtual void Validate(TParams p) { }
    /// <summary>Adds the solids of one level; adding nothing yields an empty level.</summary>
    private protected abstract void Build(PartMesh mesh, TParams p, LodLevel lod);

    private PartGeometry Make(TParams p, LodLevel lod)
    {
        var bounds = Bounds(p);
        var mesh = new PartMesh();
        Build(mesh, p, lod);
        return mesh.Finish(bounds);
    }

    protected static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }

    protected static void Range(int value, int min, int max, string name)
    {
        if (value < min || value > max) throw new ArgumentOutOfRangeException(name, $"Must be within {min}–{max}.");
    }

    /// <summary>
    /// The contract default material (wall) means "the part's own material". p.Bevel = 0 switches the large-member
    /// chamfers off; their size comes from <see cref="OpeningStyle.LargeBevel"/>.
    /// </summary>
    internal static ToonSpec Pick(TParams p, ToonSpec own) => p.Material == ContractDefault ? own : p.Material;
    internal static bool Bevelled(TParams p, LodLevel lod) => lod == LodLevel.L0 && p.Bevel > 0;
    /// <summary>Bevel lift for chamfered members, set at every level so the material ID survives level changes (ART-MODELING).</summary>
    internal static ToonSpec Hard(ToonSpec spec) => spec with { Fx = spec.Fx | ToonFx.BevelLift };

    private static readonly ToonSpec ContractDefault = new BoxParams().Material;
}

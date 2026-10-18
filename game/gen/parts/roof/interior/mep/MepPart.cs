using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Common shape of the interior equipment parts: a near level (L0, within about 14 m) and a mid level (L1, 14–50 m, the room
/// MultiMesh split of WLD-INTERIORS); the footprint box is the bounds. Front is −Z; wall-mounted parts put their back
/// on +Z. Variation 0–2 picks the look (WLD-INTERIORS: at least three, chosen by the room hash).
/// </summary>
public abstract class MepPart<TParams> : IPartDef<TParams> where TParams : BoxPartParams
{
    protected MepPart(PartBudget budget)
    {
        Budget = budget;
        Lods = Levels.Of<TParams>(Build, LodLevel.L0, LodLevel.L1);
    }

    public IReadOnlyDictionary<LodLevel, Func<TParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; }

    /// <summary>Smallest width, height and depth the shape stays valid at.</summary>
    protected abstract Vector3 Minimum { get; }

    public Box3 Bounds(TParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    /// <summary>Wall- and ceiling-mounted parts are visual only; floor-standing ones override with <see cref="PropBox"/>.</summary>
    public virtual IReadOnlyList<Collider> Collision(TParams p) => [];

    private protected abstract void Compose(PartBuilder builder, TParams p, bool near);

    protected static int Variant(TParams p) => Math.Clamp(p.Variation, 0, 2);

    /// <summary>One prop box over the footprint, so the walker cannot pass through a floor-standing unit.</summary>
    protected IReadOnlyList<Collider> PropBox(TParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop)
        {
            Foot = p.Foot == FootMat.Auto ? FootMat.SteelPlate : p.Foot,
        }];
    }

    private PartGeometry Build(TParams p, LodLevel lod)
    {
        Validate(p);
        var builder = new PartBuilder();
        Compose(builder, p, lod == LodLevel.L0);
        return builder.Finish(Bounds(p));
    }

    private void Validate(TParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        var min = Minimum;
        if (p.Width < min.X || p.Height < min.Y || p.Depth < min.Z)
            throw new ArgumentOutOfRangeException(nameof(p), $"{GetType().Name} needs at least {min.X} × {min.Y} × {min.Z} m.");
    }
}

using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>Near-pool levels (ENG-PERF): L0+ 0–15 m, L0 15–40 m, L1 40–150 m on a 12 m tree.</summary>
public enum VegLevel : byte { L0Plus = 0, L0 = 1, L1 = 2 }

/// <summary>
/// One mother model at one level: the wood surface (trunk and branches, veg_trunk) and the crown surface (leaf cards
/// first, then the crown masses, all two-sided, veg_card; max-011 review). The crown is one material, so the outline
/// pass only draws its outer contour; every crown vertex carries the normal of one smooth shell (<see cref="CrownShell"/>),
/// so the crown shades as one lit side and one shadow band. Lower levels' cards are a prefix of higher levels' (CUSTOM2.w
/// is the importance rank). TrunkRadius at breast height and BranchHeight feed the trunk collider (WLD-VEGETATION).
/// </summary>
public sealed record MotherModel(
    string Species,
    int Variant,
    VegLevel Level,
    GeometryBundle Wood,
    GeometryBundle Crown,
    float Height,
    float TrunkRadius,
    float BranchHeight,
    float CrownRadius,
    Box3 Bounds)
{
    public int Triangles => (Wood.Index.Length + Crown.Index.Length) / 3;

    /// <summary>Leaf cards at the front of <see cref="Crown"/> (4 vertices and 2 triangles each); the masses follow.</summary>
    public int Cards { get; init; }

    /// <summary>The last of <see cref="Cards"/>: the crown-underside layer (<see cref="MotherModels.UnderClearance"/>).</summary>
    public int UnderCards { get; init; }

    /// <summary>Horizontal diameter of the crown shell, the yardstick of the 5% card rule.</summary>
    public float CrownDiameter { get; init; }

    /// <summary>Branch ends the winter twig strokes hang from (WLD-VEGETATION); also the song-post candidates (§12).</summary>
    public IReadOnlyList<Vector3> BranchTips { get; init; } = [];
}


namespace Campus.Gen.Vegetation;

/// <summary>
/// Tree shape of a mother model. Lengths are fractions of the tree height H unless named in metres.
/// All shape tuning for the mothers lives in <see cref="TreeForms"/>; species.json only names the form.
/// </summary>
public sealed record TreeForm(
    string Id,
    float ClearTrunk,      // first scaffold branch height / H
    float CrownBottom,     // crown envelope bottom / H
    float CrownRadius,     // crown envelope horizontal radius / H
    float Taper,           // 0 ellipsoid … 1 cone (top narrows)
    bool Leader,           // trunk runs on to the crown top (ginkgo, metasequoia, palm)
    int MainMin,
    int MainMax,
    float MainPitchDeg,    // main-branch rise above horizontal where it leaves the trunk
    int SecondaryMin,
    int SecondaryMax,
    float TrunkRadius,     // breast-height trunk radius / H
    float CardSize,        // leaf-card edge in metres (WLD-VEGETATION: 0.4–0.7 m on a 12 m ginkgo)
    float Droop,           // 0 none … 1 secondaries and cards hang (willow)
    float Lumps,           // blob bump amplitude (cloud balls use 0.13, ART-MODELING)
    int Stems = 1);        // multi-stem shrubs and small trees

/// <summary>The mother-model shape table: one place for every tunable tree proportion (WLD-VEGETATION).</summary>
public static class TreeForms
{
    private static readonly Dictionary<string, TreeForm> Table = new(StringComparer.Ordinal)
    {
        // Ginkgo: upright ovoid-cone, leader to the top, branches rising steeply (WLD-VEGETATION).
        ["ginkgo"] = new("ginkgo", 0.26f, 0.30f, 0.27f, 0.45f, true, 5, 7, 42f, 2, 3, 0.022f, 0.55f, 0f, 0.10f),
        // Metasequoia: narrow cone, straight leader, whorled near-horizontal tiers.
        ["metaseq"] = new("metaseq", 0.10f, 0.12f, 0.17f, 1.00f, true, 8, 10, 8f, 2, 2, 0.020f, 0.50f, 0f, 0.08f),
        // Cherry, plum, crabapple, apricot: low, broad and spreading.
        ["spreading"] = new("spreading", 0.24f, 0.32f, 0.62f, 0.05f, false, 4, 5, 30f, 2, 3, 0.026f, 0.38f, 0f, 0.13f),
        // Maple, koelreuteria: rounded crown.
        ["round"] = new("round", 0.28f, 0.30f, 0.44f, 0.15f, false, 4, 6, 38f, 2, 3, 0.024f, 0.45f, 0f, 0.12f),
        // Willow: arching scaffold, hanging strands.
        ["willow"] = new("willow", 0.30f, 0.22f, 0.46f, 0.0f, false, 4, 6, 40f, 3, 4, 0.026f, 0.60f, 1f, 0.08f),
        // Jacaranda, magnolia, nanmu: upright oval.
        ["upright"] = new("upright", 0.28f, 0.30f, 0.34f, 0.25f, false, 4, 6, 50f, 2, 3, 0.022f, 0.50f, 0f, 0.11f),
        // Camphor, osmanthus: broad dome, the evergreen campus base.
        ["dome"] = new("dome", 0.25f, 0.26f, 0.52f, 0.10f, false, 5, 6, 34f, 2, 3, 0.028f, 0.55f, 0f, 0.12f),
        // Crape myrtle, hibiscus, wintersweet, red plum: several stems from the ground, vase-shaped.
        ["vase"] = new("vase", 0.32f, 0.34f, 0.40f, 0.0f, false, 3, 4, 58f, 2, 3, 0.030f, 0.32f, 0f, 0.13f, 3),
        // Fan palm: tall bare stem, a head of fronds.
        ["palm"] = new("palm", 0.86f, 0.80f, 0.32f, 0.0f, true, 0, 0, 0f, 0, 0, 0.030f, 1.30f, 0.4f, 0.0f),
        // Clipped shrub ball: no visible stem.
        ["shrub"] = new("shrub", 0.05f, 0.0f, 0.62f, 0.0f, false, 3, 3, 30f, 0, 0, 0.040f, 0.22f, 0f, 0.13f),
        // Grass tussock (max-011 23c2b): a low mound built like a shrub ball with fewer satellites, no stem; Understorey widens it.
        ["tuft"] = new("tuft", 0.05f, 0.0f, 0.62f, 0.2f, false, 3, 3, 20f, 0, 0, 0.040f, 0.16f, 0f, 0.13f),
    };

    /// <summary>Stemless forms built as clipped balls (TreePlan.BuildShrub): shrubs and grass tussocks; they cast no trunk collider.</summary>
    public static bool IsBall(string id) => id is "shrub" or "tuft";

    public static bool Exists(string id) => Table.ContainsKey(id);

    public static TreeForm Get(string id) => Table.TryGetValue(id, out var form) ? form
        : throw new KeyNotFoundException($"Unknown tree form '{id}'.");

    /// <summary>Mother models per species (ART-MODELING: young, mature, old, and one asymmetric crown).</summary>
    public const int Variants = 4;

    /// <summary>Height of one mother model within the species range: young 85% of the minimum, mature mid, old maximum, asymmetric mid.</summary>
    public static float HeightOf(SpeciesInfo species, int variant) => variant switch
    {
        0 => (species.HeightMin + species.HeightMax) * 0.5f,
        1 => species.HeightMin * 0.85f,
        2 => species.HeightMax,
        3 => (species.HeightMin + species.HeightMax) * 0.5f,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Mother-model variants are 0–3.")
    };

    // Crown-width and trunk factors per variant: young trees are slim, old trees broad and stout.
    internal static float CrownScale(int variant) => variant switch { 1 => 0.82f, 2 => 1.12f, _ => 1f };
    internal static float TrunkScale(int variant) => variant switch { 1 => 0.7f, 2 => 1.3f, _ => 1f };
    internal static float Asymmetry(int variant) => variant == 3 ? 0.22f : 0f;
}

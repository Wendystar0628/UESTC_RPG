using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Roof;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// How the facade fittings take the WP-12 parts (WF-FLOW: tunables in one place): the unit grille (机位格栅) with its
/// outdoor unit, and the rain downpipe and condensate drain (落水管). Part shapes and their section details are the
/// PartCatalog defaults and RoofStyle; these values only decide how a WP-14 facade maps onto them (MAP-QSH-ADDITIONS-VOCAB
/// and WLD-RENOVATION). Initial values; the main session sets the final ones at review.
/// </summary>
public static class FittingRules
{
    // ---- Unit grille: one niche beside each window of a storey, stacked into a vertical strip ----
    /// <summary>
    /// Niche envelope (a cage, 0.36 m deep): the stand-in's footprint it replaces, close to the reference 1.0 × 0.9 m.
    /// The wall is at the back (+Z of the part), the louvred front faces out.
    /// </summary>
    public static readonly PanelParams Grille = new(0.95f, 0.8f, 0.36f);
    /// <summary>The outdoor unit inside the niche at its real size (the WP-12 gallery's dormitory unit).</summary>
    public static readonly EquipmentParams Unit = RoofEquipment.DormUnit;
    /// <summary>The unit stands this far from the wall at its back.</summary>
    public const float UnitBackGap = 0.02f;
    /// <summary>Niches in this zone or a finer one (K1, K2) hold their unit at the building's L0; farther out the cage alone, at its far level.</summary>
    public const FidelityZone UnitUpTo = FidelityZone.K2;
    /// <summary>Gap between the window's right edge and the niche; the sill's ear reaches 70 mm.</summary>
    public const float GrilleGap = 0.14f;
    /// <summary>Niche bottom above the storey floor.</summary>
    public const float GrilleLift = 0.2f;
    /// <summary>Free facade kept at the edge end, and between the niche and the next opening of its storey.</summary>
    public const float GrilleEndMargin = 0.2f, GrilleClear = 0.1f;
    /// <summary>Shelf under the niche: thickness, overhang past the cage front, and past each side; its front top edge is chamfered at L0.</summary>
    public const float ShelfThickness = 0.08f, ShelfOverhang = 0.04f, ShelfSide = 0.03f, ShelfChamfer = 0.015f, ShelfAo = 0.3f;

    // ---- Downpipes ----
    /// <summary>Half width of a pipe with its clamps; the sill ears (70 mm) and the pipe's neighbours keep PipeClear beyond it.</summary>
    public const float PipeHalf = 0.075f, PipeClear = 0.1f;
    /// <summary>A rain pipe on a bay boundary of a facade with grilles shifts this far along, clear of the niche strips and their drains.</summary>
    public const float RainBesideGrille = 0.55f;
    /// <summary>The condensate drain runs beside a strip of at least this many niches, its axis this far off the niche's right edge.</summary>
    public const int DrainFromNiches = 2;
    public const float DrainBesideGrille = 0.105f;
    /// <summary>Niches whose left edges are within this are one strip.</summary>
    public const float StripTolerance = 0.1f;
    /// <summary>Dark steel-grey of the condensate drains (WLD-RENOVATION: 统一深灰竖管); rain pipes take the facade's trim colour.</summary>
    public const string DrainFamily = RoofStyle.FenceSteel;
}

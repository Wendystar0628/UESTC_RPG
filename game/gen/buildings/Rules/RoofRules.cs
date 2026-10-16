using Campus.Contracts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// How the generic roofs take the WP-12 parts (ENG-TEAM §5: tunables in one place). Every parapet gets the coping and a
/// lightning strip; the roof plant stands inside a perforated equipment fence and only its top shows (MAP-QSH-ADDITIONS
/// §2.9, ART-FIDELITY §0.1 K4 (a)). Density follows the roof's precision zone (D-176): K1 and K2 roofs carry the full set
/// at the building's level, K3 and beyond fewer units, one part level coarser near. Part sizes are the PartCatalog
/// defaults. Initial values; the main session sets the final ones at review.
/// </summary>
public static class RoofRules
{
    /// <summary>Roofs in this zone or a finer one (K1, K2) carry the full equipment set.</summary>
    public const FidelityZone FullEquipmentUpTo = FidelityZone.K2;
    /// <summary>Lightning-strip supports every metre (WP-12 Variation 1) only on K1 roofs; elsewhere one continuous rod.</summary>
    public const FidelityZone StripSupportsUpTo = FidelityZone.K1;
    /// <summary>
    /// Coarsest building level with the lightning strip: full roofs keep it to L1, simple roofs to L0 only (beyond 45 m
    /// the 80 mm rod drops under two pixels and the outline would break it into dashes).
    /// </summary>
    public const LodLevel StripUpToFull = LodLevel.L1;
    public const LodLevel StripUpToSimple = LodLevel.L0;
    /// <summary>Coping overhang beyond each face of the parapet; the WP-12 drip hangs under the outer one.</summary>
    public const float CopingLip = 0.06f;

    // ---- Plant enclosure: a row of units along the building axis inside a closed equipment-fence ring ----
    /// <summary>Gap between neighbouring units, and between the units and the fence.</summary>
    public const float UnitGap = 0.5f;
    public const float FenceClearance = 0.6f;
    /// <summary>The enclosure is never narrower than this across the building axis.</summary>
    public const float MinEnclosureDepth = 2.4f;
    /// <summary>Free roof kept between the enclosure and the parapet, a courtyard or a stair-core penthouse.</summary>
    public const float RoofMargin = 0.8f;
    /// <summary>Only roofs at least this large (m²) carry a plant enclosure.</summary>
    public const float PlantFromArea = 400f;
    /// <summary>
    /// Colour family of the fence boards in place of WP-12's dark blue-grey fence steel (equip.fence), which read as a hole
    /// in the roof from mid distance: the light grey aluminium of the frames (core frame.alu), one value step above the roof
    /// slab (roof.grey) and one below the plant casings (equip.grey), so the louvred fence reads as light metal and the
    /// plant still shows above it (main-session review, 2026-10-16 12:35).
    /// </summary>
    public const string FenceBoard = "frame.alu";

    /// <summary>
    /// What the enclosure holds: dormitories run air-source heat pumps (no solar heaters, MAP-QSH-ADDITIONS §2.9),
    /// canteens their kitchen units, teaching and service buildings a panel water tank beside the heat pumps.
    /// Full roofs one heat pump more than simple ones.
    /// </summary>
    public static (bool Tank, int HeatPumps) Plant(BuildingStyle style, bool full) => style switch
    {
        BuildingStyle.Dormitory or BuildingStyle.Residential or BuildingStyle.Canteen => (false, full ? 3 : 2),
        BuildingStyle.Academic or BuildingStyle.Research or BuildingStyle.Library or BuildingStyle.Medical or BuildingStyle.Modern
            or BuildingStyle.Service or BuildingStyle.Utility => (true, full ? 2 : 1),
        BuildingStyle.Sports => (false, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown building style."),
    };

    // ---- Communication mast (ART-FIDELITY §6: "高楼屋角偶尔有通信天线") ----
    /// <summary>Buildings at least this many storeys high may carry one mast at a roof corner (every other one, by hash).</summary>
    public const int MastFromStoreys = 9;
    /// <summary>Distance of the mast from its roof corner, along the inward bisector.</summary>
    public const float MastInset = 1.6f;
    /// <summary>Mast height and base plate width (the WP-12 gallery mast).</summary>
    public const float MastHeight = 3.5f, MastBase = 0.8f;
}

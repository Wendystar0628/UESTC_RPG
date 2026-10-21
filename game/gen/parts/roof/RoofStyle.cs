using Campus.Contracts;

namespace Campus.Gen.Parts.Roof;

/// <summary>
/// Every tunable value of the roof and equipment parts in one place (WF-FLOW): colours, material classes and sizes.
/// Initial values follow ART-MODELING, ART-FIDELITY and MAP-QSH-ADDITIONS; Opus review sets the finals.
/// Collision: roof and wall-mounted parts are visual only (MAP-QSH-ADDITIONS: roofs cannot be reached, and small parts on
/// a wall sit inside the building's wall collider), so they return no colliders and cannot form jump chains. Only the
/// equipment fence returns a barrier, for ground-level enclosures; roof placers leave it out.
/// </summary>
public static class RoofStyle
{
    // ---- Colour families (ART-STYLE). game/gen/parts/roof/ owns ids 248–251 in style/palette/ext/roof_equipment.json; the rest are core names. ----
    /// <summary>Off-white casings: air-conditioner body, dish and panel antennas.</summary>
    public const string CasingWhite = "wall.greyWhite";
    /// <summary>Galvanised and stainless steel: lightning strip, tank shell, grille frame, masts.</summary>
    public const string Steel = "roof.silverMetal";
    /// <summary>Grey aluminium: louvre blades, base rails, fence posts and caps, skylight frames, ladders.</summary>
    public const string Aluminium = "frame.alu";
    /// <summary>
    /// Blue-grey steel (ids 248/249; intrinsic #50647F capped at chroma 0.049, lit #6E737B at autumn 17:30): the perforated fence boards that read as one dark band, and the dark
    /// inserts that must stay dark without turning olive in low sun (fan discs, heat-pump coil panels).
    /// </summary>
    public const string FenceSteel = "equip.fence";
    /// <summary>Light equipment grey (ids 250/251; lit #CFCAC0 at autumn 17:30, intrinsic #B0BDC9): roof plant bodies (heat pump casing and shrouds) and the tank stand.</summary>
    public const string EquipmentGrey = "equip.grey";
    /// <summary>Canopy and skylight glazing.</summary>
    public const string Glass = "glass";

    // ---- Shared ----
    /// <summary>Round sections: 12 segments with smooth normals (ART-MODELING, 1 px silhouette error near).</summary>
    public const int RoundSegments = 12;
    public const int RoundSegmentsFine = 16;
    public const int RoundSegmentsFar = 6;
    /// <summary>Bevels on equipment casings, near level only (ART-MODELING: 10–20% of the smallest section).</summary>
    public const float CasingBevel = 0.03f;
    /// <summary>Bounds pad that absorbs float rounding of rotated pieces.</summary>
    public const float BoundsPad = 0.002f;

    // ---- Parapet coping (MAP-QSH-ADDITIONS, ART-MODELING) ----
    public const float CopingBevel = 0.02f;
    public const float CopingDripDepth = 0.06f;
    public const float CopingDripWidth = 0.04f;

    // ---- Lightning strip: one continuous rod, never thinner than the prop minimum section (ART-FIDELITY) ----
    public const float StripMinRod = 0.08f;
    public const float StripDefaultHeight = 0.15f;
    /// <summary>Supports every metre only on Variation ≥ 1 (precision zone K1); otherwise one near each end.</summary>
    public const float StripSupportSpacing = 1f;
    public const float StripSupportWidth = 0.04f;
    public const float StripSupportInset = 0.15f;

    // ---- Air-conditioner outdoor unit (real size; about 20% smaller and one value step lighter than game/gen/buildings/'s stand-in) ----
    public const float AcFanRadiusRatio = 0.36f;
    public const float AcFanProud = 0.012f;
    public const float AcFanCentreRatio = 0.36f;
    public const float AcValveBoxWidth = 0.07f;

    // ---- Unit grille (dorm AC niche) ----
    public const float GrilleFrame = 0.045f;
    public const float GrilleBladePitch = 0.13f;
    public const float GrilleBladeDepth = 0.06f;
    public const float GrilleBladeThickness = 0.018f;
    public const float GrilleBladeTiltDeg = 30f;
    /// <summary>Depth at or above which the grille is a three-sided cage instead of a flat front panel.</summary>
    public const float GrilleCageDepth = 0.16f;
    public const float GrilleTopThickness = 0.04f;

    // ---- Water tank (panel tank; Variation 1 stands on a steel frame, MAP-QSH-ADDITIONS) ----
    public const float TankPanel = 1f;
    public const float TankFlange = 0.04f;
    public const float TankFlangeHeight = 0.06f;
    public const float TankSkid = 0.18f;
    public const float TankStandRatio = 0.4f;
    public const float TankLeg = 0.12f;

    // ---- Heat pump (top-discharge air-source unit) ----
    public const float HeatPumpSkid = 0.1f;
    public const float HeatPumpShroud = 0.14f;
    public const float HeatPumpFanRatio = 0.38f;
    public const float HeatPumpLouvrePitch = 0.12f;

    // ---- Equipment fence (dark perforated aluminium, 1.8 m, MAP-QSH-ADDITIONS) ----
    public const float FencePost = 0.07f;
    public const float FenceCap = 0.05f;
    public const float FenceBoardInset = 0.012f;
    public const float FenceDoorWidth = 0.9f;

    // ---- Canopy ----
    public const float CanopySlab = 0.15f;
    /// <summary>The edge band rises this far above the slab: a clear step, not a near-coplanar seam that breaks into dashes.</summary>
    public const float CanopyFasciaProud = 0.06f;
    public const float CanopyRodRise = 1.2f;
    public const float CanopyRod = 0.05f;

    // ---- Downpipe (ART-FIDELITY: one clamp per storey, an elbow at the foot) ----
    public const float DownpipeClampPitch = 3f;
    public const float DownpipeClampHeight = 0.05f;
    public const float DownpipeShoeRise = 0.3f;
    public const float DownpipeShoeKick = 0.16f;
    public const float DownpipeHopper = 0.22f;

    // ---- Skylight ----
    public const float SkylightCurb = 0.18f;
    public const float SkylightFrame = 0.05f;
    public const float SkylightBarPitch = 1.2f;
    public const float SkylightDefaultSlopeDeg = 18f;
    /// <summary>Glazing slopes stay below the 40–50° crease band of the outline pass (ART-MODELING).</summary>
    public const float SkylightMaxSlopeDeg = 35f;

    // ---- Antenna ----
    public const float DishTiltDeg = 30f;
    public const float DishDepthRatio = 0.16f;
    public const float MastDiameter = 0.1f;
    public const float PanelAntennaWidth = 0.26f;
    public const float PanelAntennaDepth = 0.09f;

    // ---- Flags and banners (ART-STYLE: the electronic blue is for flags, school bus and shared bicycles; WLD-LIFE-STREET: low-saturation red and blue pennants) ----
    public const string FlagBlue = "uestc.blue";
    public const string FlagBlueLight = "uestc.techblue";
    /// <summary>The red of the anniversary pennant and the red banner: the terracotta band, the closest registered low-saturation red.</summary>
    public const string FlagRed = "band.terracotta";
    public const float FlagBarRadius = 0.015f;
    public const float FlagHem = 0.03f;
    public const float FlagHemRadius = 0.012f;
    public const float FlagOverlap = 0.003f;

    // ---- Lattice test tower (MAP-QSH-ADDITIONS-VOCAB: a white lattice mast on the research roofs) ----
    public const float TowerTopRatio = 0.55f;
    public const float TowerLeg = 0.045f;
    public const float TowerRing = 0.03f;
    public const float TowerBrace = 0.025f;
    public const float TowerMast = 1.2f;
    public const float TowerMastRadius = 0.04f;
    public const float TowerPanelHeight = 1f;
    public const float TowerDeck = 0.05f;

    // ---- Photovoltaic module (a 1.1 × 1.8 m module in an aluminium frame, dark teal cells) ----
    public const float SolarFrame = 0.035f;
    public const int SolarColumns = 6;
    public const int SolarRows = 10;
    public const float SolarLine = 0.004f;
}

using Campus.Contracts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// Every tunable of the game/gen/parts/openings/ openings in one place (WF-FLOW): stylised sections in metres, after
/// ART-FIDELITY, ART-MODELING. Initial values; the main session sets the final ones at review.
/// Local frame of all glazed parts: origin at the bottom centre of the clear opening on the glass plane,
/// X along the facade, Y up, exterior towards −Z.
/// </summary>
public static class OpeningStyle
{
    // ---- Shared frame sections -------------------------------------------------------------------------------
    /// <summary>Frame edges are pressed into the reveal so no light leaks (ART-MODELING).</summary>
    public const float PressIn = 0.005f;
    /// <summary>Inserted overlap between touching solids instead of coplanar contact (ART-MODELING).</summary>
    public const float Insert = 0.003f;
    /// <summary>Outer frame face width, seen from outside (stylised ≈ 1.4 × real).</summary>
    public const float FrameFace = 0.075f;
    /// <summary>Mullion and transom face width between lights.</summary>
    public const float MullionFace = 0.065f;
    /// <summary>Opening sash face width; the sash sits one step behind the frame face (two-level section).</summary>
    public const float SashFace = 0.055f;
    /// <summary>Step from the frame face back to the sash face.</summary>
    public const float SashStep = 0.012f;
    /// <summary>Sash depth behind its face (it straddles the glass plane).</summary>
    public const float SashDepth = 0.042f;
    /// <summary>Glazing bead of a fixed light: face width and depth, in front of the glass.</summary>
    public const float BeadFace = 0.02f;
    public const float BeadDepth = 0.018f;
    /// <summary>Dark gasket where glass meets sash or bead (ART-FIDELITY: 5–8 mm), L0 only.</summary>
    public const float GasketWidth = 0.007f;
    public const float GasketDepth = 0.008f;
    /// <summary>
    /// Single-band chamfer of the R ≥ 10 mm tier (ART-FIDELITY: copings, sills) on heavy members: shutter housing,
    /// counter shelf. Frames, sashes, leaves and handles (the 2–5 mm tier) get no geometric chamfer: it belongs to L0+,
    /// and a 45° band a pixel or two wide stipples the outline (ART-MODELING item 6); the line pass's convex-edge
    /// light carries those edges instead.
    /// </summary>
    public const float LargeBevel = 0.012f;
    /// <summary>Gap between the glass plane and anything placed in front of it, against depth fighting.</summary>
    public const float GlassClear = 0.0015f;

    // ---- Casement and slit windows ----------------------------------------------------------------------------
    /// <summary>Transom light height as a share of the window height, clamped (ART-MODELING proportions).</summary>
    public const float TransomShare = 0.28f;
    public const float TransomMin = 0.32f;
    public const float TransomMax = 0.6f;
    /// <summary>Lever handle on the opening sash: rose (w, h, d), lever (w, h, d) and stand-off from the sash face.</summary>
    public const float RoseWidth = 0.03f, RoseHeight = 0.075f, RoseDepth = 0.008f;
    public const float LeverWidth = 0.017f, LeverLength = 0.11f, LeverDepth = 0.017f, LeverStandOff = 0.018f;
    /// <summary>Handles go on the exterior face so they read from outside (stylisation; real casements hide them indoors).</summary>
    public static bool HandleOnExterior { get; } = true;
    /// <summary>Slit windows read as deep punched slots: wider frame face.</summary>
    public const float SlitFrameFace = 0.085f;

    // ---- Ribbon windows: sliding sashes and fin mullions ------------------------------------------------------
    public const float SlideOverlap = 0.035f;
    public const float SlideSashDepth = 0.022f;
    public const float FinFace = 0.04f;
    public const float FinProjection = 0.03f;
    public const float LatchWidth = 0.022f, LatchHeight = 0.045f, LatchDepth = 0.012f;

    // ---- Curtain wall (stick system) --------------------------------------------------------------------------
    public const float WallMullionBody = 0.06f;
    public const float WallCapFace = 0.065f;
    public const float WallCapDepth = 0.048f;
    public const float WallTransomCapFace = 0.055f;
    public const float WallTransomCapDepth = 0.03f;

    // ---- Doors ------------------------------------------------------------------------------------------------
    public const float DoorFrameFace = 0.09f;
    public const float DoorLeafThickness = 0.05f;
    public const float DoorStile = 0.09f;
    public const float DoorKickRail = 0.2f;
    public const float DoorFloorGap = 0.008f;
    public const float DoorHeadGap = 0.004f;
    /// <summary>Dark astragal strip over the meeting line of two leaves (a seam without a sub-pixel gap).</summary>
    public const float SeamWidth = 0.01f, SeamProud = 0.006f;
    /// <summary>Leaves taller than this get a transom light above them.</summary>
    public const float DoorLeafMaxHeight = 2.25f;
    public const float DoorTransomMinLight = 0.18f;
    public const float ThresholdHeight = 0.015f;
    public const float PullLength = 0.62f, PullBar = 0.028f, PullStandOff = 0.055f, PullCentreY = 1.05f;
    public const float LeverCentreY = 1.0f;
    public const float PanicBarY = 1.0f, PanicBarSize = 0.04f, PanicStandOff = 0.06f;
    public const float VisionWidth = 0.16f, VisionHeight = 0.5f, VisionTopGap = 0.35f;
    public const float CasingFace = 0.07f, CasingProud = 0.015f;
    public const float GlassDoorRail = 0.085f;
    public const float StallLift = 0.15f;
    public const float ScreenFrameFace = 0.12f, ScreenPanelInset = 0.08f, ScreenPanelProud = 0.015f;
    /// <summary>Window type written into door glass: W-COR = 3 (VF-B glass byte), as game/gen/buildings/ does for door bays; the lobby card of WLD-INTERIORS has no type yet.</summary>
    public const byte DoorCard = 3;

    // ---- Roller shutter and louvres ---------------------------------------------------------------------------
    public const float RollerHousing = 0.34f;
    public const float RollerHousingProud = 0.05f;
    public const float RollerRail = 0.07f;
    public const float RollerSlatPitch = 0.1f;
    /// <summary>V-groove between slats: width on the face and depth into the curtain (faces about 60° off the slat face).</summary>
    public const float RollerGroove = 0.012f, RollerGrooveDepth = 0.01f;
    public const float RollerSkin = 0.016f;
    public const float RollerBottomBar = 0.07f;
    /// <summary>A shutter whose bottom edge is below this blocks walking (DoorClosed).</summary>
    public const float RollerPassHeight = 2.0f;
    public const float LouvreFrame = 0.05f;
    public const float LouvrePitch = 0.09f;
    /// <summary>Blade chord as (rise, run): ≈ 35°, clear of the 40–50° line-flicker band (ART-MODELING).</summary>
    public const float LouvreRise = 0.05f, LouvreRun = 0.07f, LouvreBlade = 0.01f;
    public const float BlindPitch = 0.04f, BlindChord = 0.018f, BlindTilt = 0.006f, BlindSlat = 0.005f, BlindRail = 0.02f;

    // ---- Curtains ---------------------------------------------------------------------------------------------
    public const float CurtainRail = 0.05f;
    public const float CurtainFabric = 0.008f;
    public const float CurtainHem = 0.02f;
    public const float PleatRelaxed = 0.14f, PleatStacked = 0.08f;
    public const int PleatSegments = 6;
    /// <summary>Steepest fold slope the shading follows, so shallow curtains between glass and sash still read as soft folds.</summary>
    public const float PleatSlopeStacked = 0.75f, PleatSlopeRelaxed = 0.45f;
    /// <summary>Share of the width taken by one bunched stack, and the drawn panel width when half open.</summary>
    public const float StackShare = 0.16f, HalfDrawnShare = 0.58f;
    /// <summary>Dormitory curtain states fully open : half : drawn = 3 : 4 : 3 (ART-FIDELITY); ≥ 40 % of windows have one.</summary>
    public const int RatioOpen = 3, RatioHalf = 4, RatioDrawn = 3;
    public const float CurtainShare = 0.6f;

    // ---- Materials ---------------------------------------------------------------------------------------------
    // Registered tokens only: the toon library validates every token. curtain.cream and door.wood are game/gen/parts/openings/'s own
    // pairs (game/style/palette/ext/openings.json, IDs 244–247, albedo solved from the default-cell lit); the gasket uses
    // game/gen/parts/roof/'s equip.fence; the rest are core.
    public static ToonSpec Metal { get; } = Spec("frame.alu") with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f };
    /// <summary>Dark gaskets, seams and plenums: game/gen/parts/roof/'s blue-grey equipment steel (equip.fence, 248/249).</summary>
    public static ToonSpec Gasket { get; } = Spec("equip.fence") with { Line = LineClass.None, Wet = 0.3f };
    public static ToonSpec DarkMetal { get; } = Spec("roof.grey") with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f };
    public static ToonSpec Counter { get; } = Spec("stone.beige") with { Wet = 0.55f };
    public static ToonSpec Silver { get; } = Spec("roof.silverMetal") with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.5f };
    public static ToonSpec Glass { get; } = Spec("glass") with { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0f };
    public static ToonSpec Wood { get; } = Spec("door.wood") with { Wet = 0.5f };
    public static ToonSpec PaintedSteel { get; } = Spec("stone.beige") with { Fx = ToonFx.Rim, Wet = 0.45f };
    public static ToonSpec Casing { get; } = Spec("wall.greyWhite") with { Wet = 0.55f };
    public static ToonSpec Laminate { get; } = Spec("wall.greyWhite") with { Wet = 0.55f };
    public static ToonSpec Blind { get; } = Spec("wall.greyWhite") with { Line = LineClass.Fine, Wet = 0.3f };
    /// <summary>Soft goods: two and a half steps with smooth normals (ART-STYLE, ART-MODELING).</summary>
    public static ToonSpec Fabric { get; } = Spec("curtain.cream") with { Steps = ShadeSteps.TwoHalf, Normal = ToonNormal.Smooth, Line = LineClass.Fine, Wet = 0f };
    /// <summary>
    /// Curtain colours chosen per window with weights (ART-FIDELITY: colours differ): cream about 60 %, the rest
    /// light and low in chroma (17:30 lit: silver L 0.89 C 0.03, grey-white L 0.93 C 0.05).
    /// </summary>
    public static IReadOnlyList<(string Family, int Weight)> CurtainColours { get; } =
        [("curtain.cream", 6), ("roof.silverMetal", 2), ("wall.greyWhite", 2)];

    public static ToonSpec Spec(string family) => new(new(family + ".lit"), new(family + ".shade"));
}

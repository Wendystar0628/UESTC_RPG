using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// How the facade fills its openings (WF-FLOW: tunables in one place). Detail follows the precision zone
/// (D-176): openings in K1 and K2 take the full game/gen/parts/openings/ parts, K3 and beyond the simple window below. Section sizes,
/// frame depth and leaf counts of the parts start from the PartCatalog defaults; these values only decide how a
/// game/gen/buildings/ opening maps onto them. Initial values; the main session sets the final ones at review.
/// </summary>
public static class OpeningRules
{
    /// <summary>Openings in this zone or a finer one (K1, K2) take the full game/gen/parts/openings/ window parts and curtains.</summary>
    public const FidelityZone FullPartsUpTo = FidelityZone.K2;
    /// <summary>An opening belongs to the fidelity facade segment its centre lies within this distance of (m).</summary>
    public const float ZoneMatchDistance = 1.0f;

    // ---- Simple window (K3 and beyond): a chamfered frame ring with mullions and a transom, ≈ 24 + 10 per bar
    // triangles; no sashes, handles, gaskets or curtains. Sections match the game/gen/parts/openings/ frame so the zones read alike.
    /// <summary>Frame face width; the game/gen/parts/openings/ frame face.</summary>
    public const float SimpleFrameFace = 0.075f;
    /// <summary>Mullion and transom face width; the game/gen/parts/openings/ mullion face.</summary>
    public const float SimpleBarFace = 0.065f;
    /// <summary>Frame face in front of the glass plane (half the game/gen/parts/openings/ frame depth of 70 mm).</summary>
    public const float SimpleFrameProud = 0.035f;
    /// <summary>Transoms sit this much behind the frame face so the mullions lead and the crossings do not fight.</summary>
    public const float SimpleTransomStep = 0.004f;
    /// <summary>Coarsest level with mullions and transoms; farther out the simple window is its frame ring alone (16 triangles).</summary>
    public const LodLevel SimpleBarsUpTo = LodLevel.L0;
    /// <summary>Single 45° chamfer on the inner front edges of frame and bars, at L0 only (ART-FIDELITY R ≥ 10 mm tier).</summary>
    public const float SimpleChamfer = 0.012f;
    /// <summary>Frame edges pressed into the reveal, as the game/gen/parts/openings/ frames (ART-MODELING).</summary>
    public const float SimplePressIn = 0.005f;
    /// <summary>Top light of a two-row window: share of the height, clamped (the game/gen/parts/openings/ casement rule).</summary>
    public const float TransomShare = 0.28f, TransomMin = 0.32f, TransomMax = 0.6f;
    /// <summary>Target width of one light: a window gets round(width / LightWidth) columns once wider than MullionFrom.</summary>
    public const float LightWidth = 1.0f;
    public const int MaxColumns = 4;
    /// <summary>Openings taller than this always get a transom row; profiles with Transom get one above 1.3 m.</summary>
    public const float TwoRowsFrom = 2.2f;
    public const float TransomFrom = 1.3f;
    /// <summary>Entrance doors at least this wide get two leaves.</summary>
    public const float TwoLeavesFrom = 1.5f;
    /// <summary>
    /// The door's leaf mid-plane stands this far in front of the glass plane, so its frame's back face sits on the
    /// glass plane (as in the game/gen/parts/openings/ gallery) and the door reads one step proud of the windows.
    /// </summary>
    public const float DoorForward = 0.06f;
    /// <summary>Curtain size against the clear opening: inset at each side and at the head (the rail hides behind the frame).</summary>
    public const float CurtainSideInset = 0.03f;
    public const float CurtainHeadInset = 0.02f;
    /// <summary>Gap between the curtain's back face and the glass plane.</summary>
    public const float CurtainGlassGap = 0.002f;
    /// <summary>
    /// Coarsest building level that still hangs solid curtains; farther out toon_glass draws the curtains of lit
    /// windows. game/gen/parts/openings/ curtains exist at L0 and L1.
    /// </summary>
    public const LodLevel CurtainsUpTo = LodLevel.L0;
    /// <summary>
    /// game/gen/parts/openings/ curtain level used on the building's L0: L1 folds each pleat in two segments (≈ 190 triangles a curtain
    /// instead of ≈ 560); the pleats still shade softly through their smooth normals.
    /// </summary>
    public const LodLevel CurtainDetail = LodLevel.L1;
    /// <summary>
    /// The 5–8 mm dark gaskets of the game/gen/parts/openings/ windows (K1, K2 only, ART-FIDELITY): off on generic buildings; a zone
    /// pack's landmark may turn them on.
    /// </summary>
    public const bool Gaskets = false;
    /// <summary>
    /// Window-part triangles wholly behind the glass plane (part z ≥ BehindGlass) are left out: toon_glass is opaque and
    /// closed buildings are only seen from outside, so frame backs and the inner halves of sashes never show.
    /// </summary>
    public const bool CullBehindGlass = true;
    public const float BehindGlass = 0.001f;

    /// <summary>Window cards whose rooms hang curtains: never stairs, toilets, corridors, plant rooms or halls.</summary>
    public static bool Curtained(WindowCard card) => card switch
    {
        WindowCard.Classroom or WindowCard.Lecture or WindowCard.Office or WindowCard.Lab or WindowCard.Dorm or WindowCard.Medical => true,
        WindowCard.None or WindowCard.Corridor or WindowCard.Stair or WindowCard.Clean or WindowCard.Toilet or WindowCard.Canteen
            or WindowCard.Shop or WindowCard.Gym or WindowCard.Pool or WindowCard.Mechanical => false,
        _ => throw new ArgumentOutOfRangeException(nameof(card), card, "Unknown window card."),
    };

    /// <summary>Light columns and rows of a window part for a clear opening of the given size.</summary>
    public static (int Columns, int Rows) Grid(FacadeProfile profile, string part, float width, float height)
    {
        var columns = width > profile.MullionFrom ? Math.Clamp((int)MathF.Round(width / LightWidth), 2, MaxColumns) : 1;
        var rows = part switch
        {
            PartCatalog.WindowHigh => 1,
            PartCatalog.CurtainWall => height > TwoRowsFrom ? 2 : 1,
            PartCatalog.WindowCasement or PartCatalog.WindowRibbon => height > TwoRowsFrom || (profile.Transom && height > TransomFrom) ? 2 : 1,
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, "No game/gen/buildings/ rule for this window part."),
        };
        return (columns, rows);
    }
}

using Campus.Contracts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// Where a generic building opens its doors and how they meet the ground (WF-FLOW: tunables in one place). A building
/// with open entrances in the interior index (EntranceDecl) opens a door at each anchor it can take, so that the door and
/// the entrance data agree (WLD-INTERIORS, G18 ≤ 0.5 m); one without keeps the generic door in the middle of its
/// longest facade. A door at the building's own floor gets the WP-11 landing and the connection down to the outside
/// landing by WLD-TERRAIN and §5.1; a door whose outside landing is a deck or porch gets the door alone. Detail
/// follows the door's precision zone (D-176). Initial values; the main session sets the final ones at review.
/// </summary>
public static class EntranceRules
{
    // ---- Anchors (EntranceDecl from the interior index) ----
    /// <summary>An anchor takes the facade edge within this distance of its door centre (m) …</summary>
    public const float AnchorReach = 1.0f;
    /// <summary>… whose outward normal agrees with the anchor's facing at least this much (cosine).</summary>
    public const float FacingMin = 0.7f;
    /// <summary>A door stays centred in its bay when the anchor lies this close to the bay centre; otherwise it centres on the anchor.</summary>
    public const float BaySnap = 0.3f;
    /// <summary>A door that cannot stand within this of its anchor (facade too short) is not opened there (G18).</summary>
    public const float AnchorTolerance = 0.5f;
    /// <summary>An anchor floor within this of a storey floor takes the building's floor; farther, the door stands at the anchor floor.</summary>
    public const float FloorSnap = 0.2f;
    /// <summary>A raised door needs at least this height under the next floor's slab (SlabDepth); lower anchors are left to their zone pack.</summary>
    public const float MinDoorHeight = 2.0f, SlabDepth = 0.15f;
    /// <summary>Wall kept between an anchor door and a window beside it in the same storey; nearer windows give way to the door.</summary>
    public const float Pier = 0.3f;
    /// <summary>An anchor door is at least its clear width plus this (the frame on both sides).</summary>
    public const float DoorFrame = 0.2f;
    /// <summary>The ground this far out in front of an anchor door must be open (not inside another building footprint).</summary>
    public const float ExteriorProbe = 1.0f;
    /// <summary>An outside landing this far above the terrain is a deck or porch that another package builds (as WP-13's DeckAbove).</summary>
    public const float DeckAbove = 0.5f;

    // ---- Landing and connection: WP-11 platform, ramp and steps by PartCatalog (WLD-TERRAIN) ----
    /// <summary>Doors in this zone or a finer one (K1, K2) take the full set: a deeper, wider landing and cheek walls on every flight.</summary>
    public const FidelityZone FullUpTo = FidelityZone.K2;
    /// <summary>Landing depth out from the wall plane and its reach beyond each door jamb: full set, simple set.</summary>
    public const float LandingDepth = 1.5f, LandingSide = 1.0f;
    public const float SimpleLandingDepth = 1.2f, SimpleLandingSide = 0.6f;
    /// <summary>The landing keeps this far from the facade's ends.</summary>
    public const float LandingEnd = 0.2f;
    /// <summary>The outside landing level is the terrain this far beyond the landing's front edge.</summary>
    public const float GroundProbe = 1.0f;
    /// <summary>Steps connect at most one flight; a larger drop in front of a door is a terrace edge (WLD-TERRAIN), not a door step.</summary>
    public const float MaxRise = 1.5f;
    /// <summary>
    /// A landing standing more than this above the ground gets WP-11 guard railings (balusters) on its open sides, this
    /// high and this far in from its edges (WLD-TERRAIN: 临空 > 1.0 m, 1.05 m railing).
    /// </summary>
    public const float GuardFrom = 1.0f, GuardHeight = 1.05f, GuardInset = 0.1f;
    /// <summary>Below this landing height nothing is built in front of the door (the stone threshold alone).</summary>
    public const float MinLanding = 0.02f;
}

using Campus.Contracts;
using static Campus.Gen.Zones.Common.ZoneLooks;

namespace Campus.Gen.Zones.DesignPublic;

/// <summary>
/// Every tunable value of WP-29's public spaces in one place (WF-FLOW). Sizes follow MAP-QSH-ADDITIONS
/// (转角口袋绿地: about 6 × 6 m of paving, a 4 m curved bench, one tree) and §7 (walk: the pad walkable, the bench
/// jumpable). Where the pocket greens are, how big their pads are and which way their benches face is data, in
/// overlay/public.d/; this class holds the section of the bench and the pit, and the material families.
/// </summary>
public static class PublicTune
{
    /// <summary>Overlay kind of the curved bench: point geometry at the arc centre (the pad centre), props radius/from/to.</summary>
    public const string ArcBenchKind = "arc-bench";

    /// <summary>Overlay kind of a tree pit (WP-23 plants it, WP-15 skips pits whose props say stone = site).</summary>
    public const string TreePitKind = "tree-pit";

    public static class Bench
    {
        /// <summary>Seat top above the paving (SYS-WALK 第 5 条: jumpable, 0.35–1.25 m; a comfortable 0.44 m).</summary>
        public const float SeatTop = 0.44f;
        /// <summary>Granite plinths: height to the steel carrier, tangential length and radial depth.</summary>
        public const float PlinthHeight = 0.36f, PlinthLength = 0.30f, PlinthDepth = 0.40f, PlinthBevel = 0.025f;
        /// <summary>Plinths along the arc, as fractions of its length (both ends inset so the slats overhang a little).</summary>
        public static readonly float[] PlinthAt = [0.1f, 0.5f, 0.9f];
        /// <summary>Flat steel carrier between plinth and slats.</summary>
        public const float CarrierHeight = 0.04f, CarrierLength = 0.07f, CarrierDepth = 0.46f;
        /// <summary>Seat slats: radial offsets from the arc radius (negative is toward the sitter's side), width, thickness.</summary>
        public static readonly float[] SlatOffsets = [-0.165f, -0.055f, 0.055f, 0.165f];
        public const float SlatWidth = 0.095f, SlatThickness = 0.04f, SlatBevel = 0.008f;
        /// <summary>Back posts (dark steel) at each plinth, behind the seat.</summary>
        public const float PostOffset = 0.235f, PostSize = 0.045f, BackTop = 0.86f;
        /// <summary>Two back slats on the posts' inner face: radial offset, thickness, height and their bottoms above the paving.</summary>
        public const float BackSlatOffset = 0.2f, BackSlatThickness = 0.03f, BackSlatHeight = 0.09f;
        public static readonly float[] BackSlatBottoms = [0.56f, 0.72f];
        /// <summary>Half the radial depth of the seat (for the collider and the seat declaration).</summary>
        public const float HalfDepth = 0.23f;
        /// <summary>Arc segments of the slat sweeps per level, and collider/seat boxes along the arc.</summary>
        public const int SegmentsL0 = 18, SegmentsL1 = 9, SegmentsL2 = 5, ColliderBoxes = 4;
    }

    public static class Pit
    {
        /// <summary>Granite kerb of the round tree pit: width and height above the paving; the soil sits 5 cm below its top.</summary>
        public const float KerbWidth = 0.16f, KerbHeight = 0.2f, KerbBevel = 0.02f, SoilBelow = 0.05f;
        public const int SegmentsL0 = 28, SegmentsL1 = 16, SegmentsL2 = 10;
    }

    /// <summary>Material families (ART-STYLE): only registered pairs, no new colours, no 砖红 or 电子蓝.</summary>
    public static class Look
    {
        /// <summary>Warm wood slats, the same family WP-13's bench uses (door.wood).</summary>
        public static readonly ToonSpec Wood = S("door.wood", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Light granite: plinths and the pit kerb (the forecourt and tree-pit stone of the campus).</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Dark grey steel: carriers, back posts (WP-13's DarkSteel).</summary>
        public static readonly ToonSpec Steel = S("equip.fence", ToonFx.Rim | ToonFx.AnisoBand, LineClass.Fine, 0.4f);
        /// <summary>Pit soil: the horizontal face that catches leaves, petals and snow (MAP-QSH-ADDITIONS R3).</summary>
        public static readonly ToonSpec Soil = S("base.warmBrown", line: LineClass.None);
    }
}

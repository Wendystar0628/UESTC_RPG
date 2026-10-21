using Campus.Contracts;

namespace Campus.Gen.Fauna;

/// <summary>
/// Every look and placement constant of the game/systems/fauna/ animals in one place (WF-FLOW): colour names, shading specs, the
/// overall scale and the site rules. Colours are palette names only (ART-STYLE); the three fauna pairs live in
/// game/style/palette/ext/fauna.json, the rest are borrowed from the core table.
/// </summary>
public static class FaunaStyle
{
    // ---- Colours (lit tokens; shade = the registered pair) ----
    public static readonly PaletteToken Ginger = new("fauna.ginger.lit");       // 橘猫、三花的橘斑
    public static readonly PaletteToken Ink = new("fauna.ink.lit");             // 黑猫、眼睛、鸽子翼斑与尾端、麻雀的黑喉
    public static readonly PaletteToken Dove = new("fauna.dove.lit");           // 广场鸽的浅灰
    public static readonly PaletteToken White = new("wall.greyWhite.lit");      // 白猫、猫的白腹与白爪、白鸽（借核心表）
    public static readonly PaletteToken Slate = new("roof.grey.lit");           // 鸽子的头颈与喙（借核心表）
    public static readonly PaletteToken Rosy = new("panel.pinkSand.lit");       // 猫鼻（借核心表）
    public static readonly PaletteToken Coral = new("brick.lit");               // 鸽脚（借核心表）
    public static readonly PaletteToken Amber = new("uestc.gold.lit");          // 黑猫的眼睛（借核心表）
    public static readonly PaletteToken Wren = new("door.wood.lit");            // 麻雀背羽（借 game/gen/parts/openings/）
    public static readonly PaletteToken Crown = new("base.warmBrown.lit");      // 麻雀头顶（借核心表）
    public static readonly PaletteToken Cream = new("wall.lit");                // 麻雀的脸颊与腹（借核心表）

    /// <summary>Shade token of a lit token: the pair is registered as "x.lit" / "x.shade" (Shade ID = Lit ID + 1).</summary>
    public static PaletteToken ShadeOf(PaletteToken lit) =>
        lit.Value.EndsWith(".lit", StringComparison.Ordinal) ? new(lit.Value[..^4] + ".shade") : throw new ArgumentException($"'{lit.Value}' is not a lit token.", nameof(lit));

    /// <summary>Cats are soft goods: two and a half steps with the 2.2× band (ART-STYLE, WLD-LIFE).</summary>
    public static ToonSpec Fur { get; } = new(White, ShadeOf(White))
    { Steps = ShadeSteps.Soft, Normal = ToonNormal.Smooth, Line = LineClass.Fine, Wet = 0.15f };

    /// <summary>Birds are curved props: two and a half steps (ART-STYLE, WLD-LIFE).</summary>
    public static ToonSpec Feather { get; } = new(Dove, ShadeOf(Dove))
    { Steps = ShadeSteps.TwoHalf, Normal = ToonNormal.Smooth, Line = LineClass.Fine, Wet = 0.15f };

    /// <summary>Small things read 1.1–1.25× larger (ART-MODELING); animals take the low end so they stay true to size.</summary>
    public const float Scale = 1.1f;

    // ---- Sites (WLD-LIFE; mainline keeps 3–6 sites per kind, fewer rather than more) ----
    /// <summary>Distance from a building wall to a cat resting at its door side, and to one sheltering under the eaves.</summary>
    public const float CatDoorOffset = 1.3f;
    public const float CatEaveOffset = 0.45f;
    /// <summary>The night pair of the dorm cats moves this much further out, toward the road and its lamp pool.</summary>
    public const float CatNightOut = 2.4f;
    /// <summary>A cat's building must have a road within this distance (the door side faces it).</summary>
    public const float CatRoadReach = 30f;
    /// <summary>Cat sites keep this far apart, so each one reads as its own place.</summary>
    public const float CatSiteSpacing = 150f;

    /// <summary>Pigeon flock on the largest stepped plaza: count, ring fraction of the steps' foot radius, scatter.</summary>
    public const int PigeonCount = 9;
    public const float PigeonRingFraction = 0.55f;
    public const float PigeonScatter = 4.5f;

    /// <summary>Sparrows peck in small groups near trees by dining and dorm buildings; up to this many groups, this far apart.</summary>
    public const int SparrowGroups = 3;
    public const int SparrowsPerGroup = 4;
    public const float SparrowTreeReach = 30f;
    public const float SparrowGroupSpacing = 120f;
    /// <summary>Perched sparrows stand on the top of seat walls (overlay kind "seat-wall"), two per wall, up to this many walls.</summary>
    public const int SparrowWalls = 2;

    /// <summary>Black swans: four on the first lake, two on the second (WLD-LIFE); kept off the banks; pairs a little apart.</summary>
    public const int SwansFirstLake = 4;
    public const int SwansSecondLake = 2;
    public const float SwanShoreMargin = 14f;
    public const float SwanPairGap = 2.6f;
    /// <summary>The night huddle sits this far out from the bank.</summary>
    public const float SwanHuddleOut = 5f;

    /// <summary>Dusk crows: 2-3 groups of 2-3 on dorm parapets, groups at least 150 m apart (WLD-LIFE).</summary>
    public const int CrowGroups = 3;
    public const float CrowGroupSpacing = 150f;
    /// <summary>Coping top above pad + height on game/gen/buildings/'s generic buildings (parapet rise 0.8 m + coping 0.15 m, plus the coping lip the birds stand on; checked in the 25a review shots).</summary>
    public const float CrowCopingRise = 1.12f;

    /// <summary>Categories read from the entity's "category" property (world files), never feature IDs.</summary>
    public const string Residence = "residence";
    public const string Research = "research";
    public const string Dining = "dining";
    public static readonly DesignKind Building = new("building");
    public static readonly DesignKind Water = new("water");
    public static readonly DesignKind Steps = new("steps");
    public static readonly DesignKind SeatWall = new("seat-wall");
}

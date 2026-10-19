using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using System.Text.Json;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// A facade preset: the facade parameters a building takes over its family profile when its zone pack names the preset
/// in the world record's facade field (read as WorldFeature.Facade). Null keeps the family's value. A preset never changes
/// the bay grid's columns or the storeys, so every window keeps its number (contract-29).
/// </summary>
public sealed record FacadeLook(string Key)
{
    /// <summary>窗墙比 across: window width as a share of the bay (FacadeProfile.Glazing; the bay grid stays).</summary>
    public float? Glazing { get; init; }
    /// <summary>窗墙比 up: window height, and the sills of the upper storeys and of the ground storey (m).</summary>
    public float? WindowHeight { get; init; }
    public float? Sill { get; init; }
    public float? GroundSill { get; init; }
    /// <summary>玻璃色: colour family of the window and door glass.</summary>
    public string? Glass { get; init; }
    /// <summary>框架材质: colour family of the wall frame (piers and spandrels), and of its trims (string courses, cornice).</summary>
    public string? Frame { get; init; }
    public string? Trim { get; init; }
    /// <summary>
    /// 窗间板: colour family of the spandrels and lintels of the window columns (FacadeProfile.AccentSpandrels). Set, it is
    /// fixed, whatever the dormitory cluster hash picks; the same family as <see cref="Frame"/> reads as no vertical band.
    /// </summary>
    public string? Accent { get; init; }
    /// <summary>
    /// 窗下裙板: the accent takes only the apron under each window (floor to sill, as wide as the window); the lintel stays the
    /// frame colour (FacadeProfile.AccentApron). Window cells, bays and storeys do not move (contract-29).
    /// </summary>
    public bool? AccentApron { get; init; }
    /// <summary>窗型: the WP-10 part filling the windows (PartCatalog; curtain walls hang no curtains).</summary>
    public string? Window { get; init; }
}

/// <summary>
/// The facade presets WP-14 knows, by key (WF-FLOW: tunables in one place). Initial values; the main session sets the
/// final ones at review. A key no preset has leaves the family profile as it is (logged, BuildingsModule.UnknownPreset).
/// </summary>
public static class FacadePresets
{
    /// <summary>
    /// 科研楼家族，浅暖灰石材框架加大面积深色玻璃，比六号科研楼更通透（MAP-QSH-SOUTH，成电国际创新中心）：
    /// windows take 0.84 of the bay and 2.8 m of the storey (about 0.6 of the wall against the family's 0.3), filled
    /// with curtain-wall lights in registered deep glass between stone piers and spandrels.
    /// </summary>
    public static readonly FacadeLook ResearchStoneFrame = new("research.stoneFrame")
    {
        Glazing = 0.84f,
        WindowHeight = 2.8f,
        Sill = 0.6f,
        GroundSill = 0.75f,
        Glass = Families.GlassDeep,
        Frame = Families.StoneBeige,
        Trim = Families.WallGreyWhite,
        Window = PartCatalog.CurtainWall,
    };

    // 北区宿舍预设 (WP-20, MAP-QSH-NORTH). Every one names its frame, trims and accent outright:
    // the dormitory family is one of two by cluster hash (DormA, DormB) and a preset has to cover both. Window cells, bays and
    // storeys stay the family's, so every window keeps its number (contract-29).

    /// <summary>
    /// 本科 A 型: the bay's 1.8 m window (half of it) in a 1.6 m opening 1.0 m off the floor, a solid wall around it, and the
    /// 陶红 only as the apron under each window, as wide as the window (MAP-QSH-NORTH): about a sixth of the wall against a
    /// quarter when the lintels took it too (WLD-RENOVATION, the 陶红 at most a quarter of the facade).
    /// </summary>
    private static FacadeLook TypeA(string key, string frame) => new(key)
    {
        Glazing = 0.50f,
        WindowHeight = 1.60f,
        Sill = 1.00f,
        GroundSill = 1.00f,
        Glass = Families.Glass,
        Frame = frame,
        Trim = Families.WallWarmWhite,
        Accent = Families.BandTerracotta,
        AccentApron = true,
        Window = PartCatalog.WindowCasement,
    };

    /// <summary>学知二、三、五、七组团: warm yellow wall (宿舍暖米黄墙) with the 陶红 aprons.</summary>
    public static readonly FacadeLook DormAYellow = TypeA("dorm.a.dormYellow", Families.WallDormYellow);

    /// <summary>学知一、四、八组团: the same type A in 浅奶咖, the cluster told apart by the wall colour (MAP-QSH-NORTH).</summary>
    public static readonly FacadeLook DormACreamCoffee = TypeA("dorm.a.creamCoffee", Families.WallCreamCoffee);

    /// <summary>
    /// 学知六组团 B 型 (宿舍金样): wider and taller windows (0.62 of the bay, 1.75 m) between narrow warm-white piers, 灰粉砂
    /// spandrels and registered 深灰蓝玻璃 (glass.deep).
    /// </summary>
    public static readonly FacadeLook DormB = new("dorm.b")
    {
        Glazing = 0.62f,
        WindowHeight = 1.75f,
        Sill = 0.85f,
        GroundSill = 1.00f,
        Glass = Families.GlassDeep,
        Frame = Families.WallWarmWhite,
        Trim = Families.WallGreyWhite,
        Accent = Families.PanelPinkSand,
        Window = PartCatalog.WindowCasement,
    };

    /// <summary>
    /// 硕士楼 (硕丰苑 1–20 栋): larger openings than the undergraduate blocks (0.55 of the bay, 1.8 m, sills at 0.8 m), the
    /// wall one grey-beige stone: no band, the accent is the frame's colour (MAP-QSH-NORTH).
    /// </summary>
    private static FacadeLook Master(string key, string frame, string trim, string accent) => new(key)
    {
        Glazing = 0.55f,
        WindowHeight = 1.80f,
        Sill = 0.80f,
        GroundSill = 0.90f,
        Glass = Families.Glass,
        Frame = frame,
        Trim = trim,
        Accent = accent,
        Window = PartCatalog.WindowCasement,
    };

    public static readonly FacadeLook DormMasterStoneBeige = Master("dorm.master.stoneBeige", Families.StoneBeige, Families.WallGreyWhite, Families.StoneBeige);

    /// <summary>硕丰苑 21–33 栋, the newer blocks: the master language with the colours turned round, a 灰白 wall with 灰米石材 bands.</summary>
    public static readonly FacadeLook DormMasterGreyWhite = Master("dorm.master.greyWhite", Families.WallGreyWhite, Families.StoneBeige, Families.StoneBeige);

    /// <summary>
    /// 博翰苑 1–6 栋 (博翰一组团, old blocks renovated): the master language on a 浅奶咖 wall with 暖白 trims, no vertical band (the
    /// accent is the frame's own colour). Also the wall language of the unnamed 5232 and 5561 beside it (MAP-QSH-RENOVATION Q-05, Q-06).
    /// </summary>
    public static readonly FacadeLook DormMasterCreamCoffee = Master("dorm.master.creamCoffee", Families.WallCreamCoffee, Families.WallWarmWhite, Families.WallCreamCoffee);

    /// <summary>
    /// 博翰苑 7–9 栋 and the two 留学生 blocks (the 2015 buildings, wall updated, cleaner): the same 浅奶咖 wall, the spandrels and
    /// lintels of the window columns 暖白, so the facade reads as cream piers between warm-white window bands. The unnamed 9693 in
    /// the 博翰二 cluster takes the 7–9 language (Q-10).
    /// </summary>
    public static readonly FacadeLook DormMasterCreamCoffeeWhite = Master("dorm.master.creamCoffeeWhite", Families.WallCreamCoffee, Families.WallWarmWhite, Families.WallWarmWhite);

    // 博翰一组团附属 and 6735 (WP-20, WLD-INTERIORS). Not dormitories: the family (service, utility) stays the
    // zone pack's and keeps its own curtains, string courses and window cards. The accent is written out to keep the table whole,
    // though neither family paints it (AccentSpandrels is off there).

    /// <summary>无名 4499 (社区活动室, 2 层, 门关着): the 博翰一 wall and trims, openings a step larger than a dormitory's (0.6 of the bay, 2.0 m).</summary>
    public static readonly FacadeLook AnnexServiceCreamCoffee = new("annex.service.creamCoffee")
    {
        Glazing = 0.60f,
        WindowHeight = 2.00f,
        Sill = 0.90f,
        GroundSill = 0.90f,
        Glass = Families.Glass,
        Frame = Families.WallCreamCoffee,
        Trim = Families.WallWarmWhite,
        Accent = Families.WallCreamCoffee,
        Window = PartCatalog.WindowCasement,
    };

    /// <summary>无名 5247 (配电房、水泵房, 后勤小楼, 门关着): the utility family's own high windows, only the wall turned 浅奶咖 to join 4499.</summary>
    public static readonly FacadeLook AnnexUtilityCreamCoffee = new("annex.utility.creamCoffee")
    {
        Glazing = 0.38f,
        WindowHeight = 1.20f,
        Sill = 1.60f,
        GroundSill = 1.60f,
        Glass = Families.Glass,
        Frame = Families.WallCreamCoffee,
        Trim = Families.WallWarmWhite,
        Accent = Families.WallCreamCoffee,
        Window = PartCatalog.WindowHigh,
    };

    /// <summary>无名 6735 (校医院西侧条楼, 门关着): 灰白 single-colour wall with warm-white coping, wider high windows so the 39 m strip does not close up.</summary>
    public static readonly FacadeLook AnnexUtilityGreyWhite = new("annex.utility.greyWhite")
    {
        Glazing = 0.45f,
        WindowHeight = 1.40f,
        Sill = 1.20f,
        GroundSill = 1.40f,
        Glass = Families.Glass,
        Frame = Families.WallGreyWhite,
        Trim = Families.WallWarmWhite,
        Accent = Families.WallGreyWhite,
        Window = PartCatalog.WindowHigh,
    };

    public static IReadOnlyList<FacadeLook> All { get; } =
    [
        ResearchStoneFrame, DormAYellow, DormACreamCoffee, DormB, DormMasterStoneBeige, DormMasterGreyWhite,
        DormMasterCreamCoffee, DormMasterCreamCoffeeWhite, AnnexServiceCreamCoffee, AnnexUtilityCreamCoffee, AnnexUtilityGreyWhite,
    ];

    /// <summary>The preset of a key in <paramref name="presets"/> (default: <see cref="All"/>); null for no key or an unknown one.</summary>
    public static FacadeLook? Find(FacadePreset? key, IReadOnlyList<FacadeLook>? presets = null) =>
        key is { Value: { Length: > 0 } name } ? (presets ?? All).FirstOrDefault(p => p.Key == name) : null;

    /// <summary>Authored three-colour overrides affect colours only, preserving the family's complete shape and window plan.</summary>
    public static FacadeProfile ApplyColors(FacadeProfile profile, WorldFeature feature)
    {
        if (feature.Props is not { ValueKind: JsonValueKind.Object } props || !props.TryGetProperty("facadeColors", out var colors))
            return profile;
        return profile with { Wall = colors.Text("wall"), Trim = colors.Text("trim"), Accent = colors.Text("accent") };
    }

    /// <summary>The family profile with the preset's values over it.</summary>
    public static FacadeProfile Apply(FacadeProfile profile, FacadeLook? look) => look is null ? profile : profile with
    {
        Glazing = look.Glazing ?? profile.Glazing,
        WindowHeight = look.WindowHeight ?? profile.WindowHeight,
        Sill = look.Sill ?? profile.Sill,
        GroundSill = look.GroundSill ?? profile.GroundSill,
        Glass = look.Glass ?? profile.Glass,
        Wall = look.Frame ?? profile.Wall,
        Trim = look.Trim ?? profile.Trim,
        Accent = look.Accent ?? profile.Accent,
        AccentApron = look.AccentApron ?? profile.AccentApron,
        Window = look.Window ?? profile.Window,
        Preset = look.Key,
    };
}

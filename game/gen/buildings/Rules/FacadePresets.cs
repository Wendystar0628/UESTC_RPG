using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>
/// A facade preset: the facade parameters a building takes over its family profile when its zone pack names the preset
/// in the overlay (StyleOp.Facade, read as WorldFeature.Facade). Null keeps the family's value. A preset never changes
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
    /// <summary>窗型: the WP-10 part filling the windows (PartCatalog; curtain walls hang no curtains).</summary>
    public string? Window { get; init; }
}

/// <summary>
/// The facade presets WP-14 knows, by key (ENG-TEAM §5: tunables in one place). Initial values; the main session sets the
/// final ones at review. A key no preset has leaves the family profile as it is (logged, BuildingsModule.UnknownPreset).
/// </summary>
public static class FacadePresets
{
    /// <summary>
    /// 科研楼家族，浅暖灰石材框架加大面积深色玻璃，比六号科研楼更通透（MAP-QSH-SOUTH §8.2，成电国际创新中心）：
    /// windows take 0.84 of the bay and 2.8 m of the storey (about 0.6 of the wall against the family's 0.3), filled
    /// with curtain-wall lights between stone piers and spandrels. 玻璃色借 glass（全校唯一的玻璃色名），深色玻璃色名进调色板后换。
    /// </summary>
    public static readonly FacadeLook ResearchStoneFrame = new("research.stoneFrame")
    {
        Glazing = 0.84f,
        WindowHeight = 2.8f,
        Sill = 0.6f,
        GroundSill = 0.75f,
        Glass = Families.Glass,
        Frame = Families.StoneBeige,
        Trim = Families.WallGreyWhite,
        Window = PartCatalog.CurtainWall,
    };

    public static IReadOnlyList<FacadeLook> All { get; } = [ResearchStoneFrame];

    /// <summary>The preset of a key in <paramref name="presets"/> (default: <see cref="All"/>); null for no key or an unknown one.</summary>
    public static FacadeLook? Find(FacadePreset? key, IReadOnlyList<FacadeLook>? presets = null) =>
        key is { Value: { Length: > 0 } name } ? (presets ?? All).FirstOrDefault(p => p.Key == name) : null;

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
        Window = look.Window ?? profile.Window,
        Preset = look.Key,
    };
}

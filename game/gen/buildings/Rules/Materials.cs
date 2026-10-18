using Campus.Contracts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>Material classes used by generated buildings. Colours vary per vertex; classes stay few (ENG-PERF).</summary>
public enum MaterialKind : byte
{
    /// <summary>Main wall surfaces, reveals, pilasters and penthouse walls.</summary>
    Wall,
    /// <summary>Plinth, string courses, cornice, coping, sills, door surrounds and canopies: own class so trims draw material lines.</summary>
    Trim,
    /// <summary>Roof slabs and caps.</summary>
    Roof,
    /// <summary>Window and door glass (toon_glass): window number in UV/UV2, window card type in the spare byte.</summary>
    Glass,
    /// <summary>Window frames, mullions, penthouse doors: metal, fine line class, no shadow.</summary>
    Metal,
}

/// <summary>Palette token pair helpers and the ToonSpec of each material class.</summary>
public static class Materials
{
    public static PaletteToken Lit(string family) => new(family + ".lit");
    public static PaletteToken Shade(string family) => new(family + ".shade");

    /// <summary>The family used as the bundle default colour (and as the colour when no registry is available).</summary>
    public static string DefaultFamily(MaterialKind kind) => kind switch
    {
        MaterialKind.Wall => Families.WallWarmWhite,
        MaterialKind.Trim => Families.WallGreyWhite,
        MaterialKind.Roof => Families.RoofGrey,
        MaterialKind.Glass => Families.Glass,
        MaterialKind.Metal => Families.FrameAlu,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown material kind.")
    };

    /// <summary>
    /// ToonSpec for a material class with the given colour family as Lit/Shade.
    /// The non-colour fields define the material class (WP-04 ToonLibrary), so each kind maps to one material ID;
    /// the wall's construction pattern (<see cref="WallPatterns"/>) is part of that class.
    /// </summary>
    public static ToonSpec Spec(MaterialKind kind, string family, WallPattern pattern = WallPattern.None) => kind switch
    {
        MaterialKind.Wall => new(Lit(family), Shade(family)) { Fx = ToonFx.BevelLift | ToonFx.Mottle, Wet = 0.6f, SurfacePattern = WallPatterns.Spec(pattern) },
        MaterialKind.Trim => new(Lit(family), Shade(family)) { Fx = ToonFx.BevelLift, Wet = 0.55f },
        MaterialKind.Roof => new(Lit(family), Shade(family)) { Fx = ToonFx.Mottle, Wet = 0.85f },
        MaterialKind.Glass => new(Lit(family), Shade(family)) { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0f },
        MaterialKind.Metal => new(Lit(family), Shade(family)) { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown material kind.")
    };

    public static IReadOnlyList<MaterialKind> All { get; } = Enum.GetValues<MaterialKind>();
}

/// <summary>Colour families from the WP-04 core registry (game/style/palette/core.json). Names only; values live in the registry.</summary>
public static class Families
{
    public const string WallWarmWhite = "wall";
    public const string StoneBeige = "stone.beige";
    public const string WallGreyWhite = "wall.greyWhite";
    public const string WallDormYellow = "wall.dormYellow";
    public const string WallCreamCoffee = "wall.creamCoffee";
    public const string BandTerracotta = "band.terracotta";
    public const string PanelPinkSand = "panel.pinkSand";
    public const string PanelBrown = "panel.brown";
    public const string TileCanteen = "tile.canteen";
    public const string BaseWarmBrown = "base.warmBrown";
    public const string RoofGrey = "roof.grey";
    public const string RoofSilverMetal = "roof.silverMetal";
    public const string FrameAlu = "frame.alu";
    public const string Glass = "glass";
}

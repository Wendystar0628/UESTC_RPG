using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Ground;

/// <summary>Final surface of a piece of ground after priority resolution (water first, lawn last).</summary>
public enum GroundSurface : byte
{
    Water, Asphalt, Parking, Bevel, Curb, Edging, Track, Turf, Court, Granite, PlazaBorder, Paver, Grass, GrassLight, GrassDark
}

/// <summary>Colour slots used by ground geometry. Each slot is one Lit/Shade pair in the palette.</summary>
public enum GroundColor : byte
{
    Asphalt, Concrete, Curb, Paver, Court, Turf, Track, Grass, GrassLight, GrassDark, Marking, MarkingYellow, Soil
}

/// <summary>
/// Shading materials of the ground. Colours within one material differ only by the per-vertex colour byte, so they
/// share a material ID and the outline pass draws no line between them (lawn tones, paving kinds); the kerb has its
/// own material so the kerb line is drawn (ART-MODELING 7.8).
/// </summary>
public enum GroundMaterial : byte { Asphalt, Curb, Paving, Lawn, Marking }

/// <summary>
/// Palette tokens and material specs for the ground. Colours live only in the palette data
/// (game/style/palette/ext/WP-15.json, columns 178-197; grass, brick and base.warmBrown from the core table);
/// this file only names them. Plazas share the granite of the kerbs; step faces under lawns use the core warm brown.
/// </summary>
public static class GroundPalette
{
    public static PaletteToken Lit(GroundColor color) => new(Family(color) + ".lit");
    public static PaletteToken Shade(GroundColor color) => new(Family(color) + ".shade");

    /// <summary>
    /// Registered palette column of the Lit entry (Shade = Lit + 1). Columns never change once registered
    /// (ART-STYLE 8.5); the unit tests check this table against the registry files.
    /// </summary>
    public static byte Id(GroundColor color) => color switch
    {
        GroundColor.Asphalt => 178,
        GroundColor.Concrete => 180,
        GroundColor.Curb => 182,
        GroundColor.Paver => 184,
        GroundColor.Court => 186,
        GroundColor.Turf => 188,
        GroundColor.GrassLight => 190,
        GroundColor.GrassDark => 192,
        GroundColor.Marking => 194,
        GroundColor.MarkingYellow => 196,
        GroundColor.Grass => 150,
        GroundColor.Track => 98,
        GroundColor.Soil => 88,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown ground colour.")
    };

    public static GroundMaterial MaterialOf(GroundColor color) => color switch
    {
        GroundColor.Asphalt or GroundColor.Concrete => GroundMaterial.Asphalt,
        GroundColor.Curb => GroundMaterial.Curb,
        GroundColor.Paver or GroundColor.Court or GroundColor.Turf or GroundColor.Track => GroundMaterial.Paving,
        GroundColor.Grass or GroundColor.GrassLight or GroundColor.GrassDark or GroundColor.Soil => GroundMaterial.Lawn,
        GroundColor.Marking or GroundColor.MarkingYellow => GroundMaterial.Marking,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown ground colour.")
    };

    public static string Family(GroundColor color) => color switch
    {
        GroundColor.Asphalt => "ground.asphalt",
        GroundColor.Concrete => "ground.concrete",
        GroundColor.Curb => "ground.curb",
        GroundColor.Paver => "ground.paver",
        GroundColor.Court => "ground.court",
        GroundColor.Turf => "ground.turf",
        GroundColor.Track => "brick",
        GroundColor.Grass => "grass",
        GroundColor.GrassLight => "ground.grassLight",
        GroundColor.GrassDark => "ground.grassDark",
        GroundColor.Marking => "ground.marking",
        GroundColor.MarkingYellow => "ground.markingYellow",
        GroundColor.Soil => "base.warmBrown",
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown ground colour.")
    };

    public static GroundColor ColorOf(GroundSurface surface) => surface switch
    {
        GroundSurface.Asphalt => GroundColor.Asphalt,
        GroundSurface.Parking => GroundColor.Concrete,
        GroundSurface.Bevel or GroundSurface.Curb or GroundSurface.Edging or GroundSurface.Granite => GroundColor.Curb,
        GroundSurface.Track => GroundColor.Track,
        GroundSurface.Turf => GroundColor.Turf,
        GroundSurface.Court => GroundColor.Court,
        GroundSurface.Paver or GroundSurface.PlazaBorder => GroundColor.Paver,
        GroundSurface.Grass => GroundColor.Grass,
        GroundSurface.GrassLight => GroundColor.GrassLight,
        GroundSurface.GrassDark => GroundColor.GrassDark,
        GroundSurface.Water => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Water has no ground colour."),
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
    };

    public static bool IsLawn(GroundSurface surface) => surface switch
    {
        GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark => true,
        GroundSurface.Water or GroundSurface.Asphalt or GroundSurface.Parking or GroundSurface.Bevel or GroundSurface.Curb or
            GroundSurface.Edging or GroundSurface.Track or GroundSurface.Turf or GroundSurface.Court or GroundSurface.Granite or
            GroundSurface.PlazaBorder or GroundSurface.Paver => false,
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
    };

    /// <summary>Colour of the vertical step face below the upper surface (kerb faces, lawn steps, banks).</summary>
    public static GroundColor FaceColorOf(GroundSurface upper) => upper switch
    {
        GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark or GroundSurface.Turf => GroundColor.Soil,
        GroundSurface.Asphalt or GroundSurface.Parking or GroundSurface.Bevel or GroundSurface.Curb or GroundSurface.Edging or
            GroundSurface.Track or GroundSurface.Court or GroundSurface.Granite or GroundSurface.PlazaBorder or GroundSurface.Paver => GroundColor.Curb,
        GroundSurface.Water => throw new ArgumentOutOfRangeException(nameof(upper), upper, "Water has no face."),
        _ => throw new ArgumentOutOfRangeException(nameof(upper), upper, "Unknown ground surface.")
    };

    /// <summary>Footstep material of each surface (SYS-AUDIO 9.3); the ground never leaves Auto in the grid.</summary>
    public static FootMat FootOf(GroundSurface surface) => surface switch
    {
        GroundSurface.Asphalt => FootMat.Asphalt,
        GroundSurface.Parking => FootMat.Concrete,
        GroundSurface.Bevel or GroundSurface.Curb or GroundSurface.Edging or GroundSurface.Granite or GroundSurface.PlazaBorder => FootMat.StonePaving,
        GroundSurface.Paver => FootMat.BrickPaver,
        GroundSurface.Track or GroundSurface.Court => FootMat.RubberTrack,
        GroundSurface.Turf or GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark => FootMat.Grass,
        GroundSurface.Water => FootMat.Soil,
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
    };

    /// <summary>Material spec; its Lit/Shade pair is the material's main colour, vertices carry their own colour byte.</summary>
    public static ToonSpec Spec(GroundMaterial material) => material switch
    {
        GroundMaterial.Asphalt => Pair(GroundColor.Asphalt) with { Wet = 1f, Fx = ToonFx.Mottle },
        GroundMaterial.Curb => Pair(GroundColor.Curb) with { Wet = 0.6f, Fx = ToonFx.BevelLift | ToonFx.DetailTiling },
        GroundMaterial.Paving => Pair(GroundColor.Paver) with { Wet = 0.6f, Fx = ToonFx.DetailTiling | ToonFx.Mottle },
        GroundMaterial.Lawn => Pair(GroundColor.Grass) with { Wet = 0.4f },
        GroundMaterial.Marking => Pair(GroundColor.Marking) with { Wet = 0.8f, Line = LineClass.None },
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown ground material.")
    };

    private static ToonSpec Pair(GroundColor color) => new(Lit(color), Shade(color));

    /// <summary>VF-A flag bits (WP-04 registry): 4 decal, 8 no shadow. The ground never casts shadows.</summary>
    public const byte FlagNoShadow = 8;
    public const byte FlagDecal = 4;
}

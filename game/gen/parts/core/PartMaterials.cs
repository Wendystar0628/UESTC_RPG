using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Shared material classes for parts; palette families remain the callers' colour choices.</summary>
public static class PartMaterials
{
    public static PaletteToken Lit(string family) => new(family + ".lit");
    public static PaletteToken Shade(string family) => new(family + ".shade");

    // ---- Material classes: the non-colour ToonSpec fields. Trim, Metal, Grille and Glass match WP-14's classes so they share material IDs. ----
    // Each class takes a colour family, or a host ToonSpec whose Lit/Shade pair it keeps (parts coloured by their host).
    /// <summary>Bulky equipment bodies: chamfer highlight, building line.</summary>
    public static ToonSpec Casing(string family) => Casing(Pair(family));
    public static ToonSpec Casing(ToonSpec colours) => new(colours.Lit, colours.Shade) { Fx = ToonFx.BevelLift, Wet = 0.5f };
    /// <summary>Curved casings (dish): two-and-a-half steps with smooth normals (ART-STYLE).</summary>
    public static ToonSpec Curved(string family) => new(Lit(family), Shade(family)) { Steps = ShadeSteps.TwoHalf, Normal = ToonNormal.Smooth, Wet = 0.5f };
    /// <summary>Host-coloured trims (coping, canopy, skylight curb): same class as building trims.</summary>
    public static ToonSpec Trim(string family) => Trim(Pair(family));
    public static ToonSpec Trim(ToonSpec colours) => new(colours.Lit, colours.Shade) { Fx = ToonFx.BevelLift, Wet = 0.55f };
    /// <summary>Thin metal: rods, frames, rails.</summary>
    public static ToonSpec Metal(string family) => Metal(Pair(family));
    public static ToonSpec Metal(ToonSpec colours) => new(colours.Lit, colours.Shade) { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f };
    /// <summary>Round pipes: the metal class with two-and-a-half steps so the cylinder terminator stays soft.</summary>
    public static ToonSpec Pipe(string family) => Pipe(Casing(family));
    public static ToonSpec Pipe(ToonSpec colours) => Metal(colours) with { Steps = ShadeSteps.TwoHalf };
    /// <summary>Louvre blades, fan discs, small dark inserts.</summary>
    public static ToonSpec Grille(string family) => new(Lit(family), Shade(family)) { Line = LineClass.Fine, Wet = 0.5f };
    /// <summary>Large flat boards (perforated fence): no chamfer lift, building line.</summary>
    public static ToonSpec Board(string family) => new(Lit(family), Shade(family)) { Wet = 0.5f };
    public static ToonSpec Glazing() => new(Lit("glass"), Shade("glass")) { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0f };
    private static ToonSpec Pair(string family) => new(Lit(family), Shade(family));
}

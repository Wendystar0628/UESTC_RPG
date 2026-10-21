using Campus.Contracts;

namespace Campus.Gen.Diorama;

/// <summary>
/// Colour names the backdrop reads (all registered in game/style/palette/core.json; game/gen/diorama/ registers none of its own yet).
/// City silhouettes take the grey-white wall pair, the far ring the snow pair; rock, haze, glow and rim colours are the
/// frame colours the shader reads directly (fog, sky, sun light, cloud rim, window). The fog sea's spec is a placeholder:
/// its colour comes only from the shared fog function (WLD-OUTSKIRTS-DIORAMA).
/// </summary>
public static class DioramaPalette
{
    public static ToonSpec City { get; } = Spec("wall.greyWhite");
    public static ToonSpec Far { get; } = Spec("snow");
    public static ToonSpec FogSea { get; } = Spec("snow");

    private static ToonSpec Spec(string name) => new(new PaletteToken(name + ".lit"), new PaletteToken(name + ".shade"))
    {
        Kind = ToonKind.Static,
        Line = LineClass.None,
        Wet = 0f,
    };
}

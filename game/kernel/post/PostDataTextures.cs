using System.Text.Json;
using Godot;

namespace Campus.Kernel.Post;

/// <summary>
/// Stand-ins for the two global data textures the compute passes bind, used only while
/// IGlobalDataTextureRegistry has no producer for a name: the palette (registered by WP-37) and mat_table
/// (registered by WP-04; row 0 is the line table defined by WP-05). The stand-in palette takes its fog and line
/// columns from the D0 palette data; the stand-in mat_table gives every ID the default line class and no bevel lift.
/// Lines are a style floor, so the outline keeps running on these rather than switching off.
/// </summary>
internal static class PostDataTextures
{
    private const int GenericNoLineId = 126;       // ART-STYLE §4.5: 112–127 reserved for WP-05, 126 = no line
    private const byte NoLineFlag = 8;
    // Columns mirrored from fog.glslinc, used only by the stand-in palette when no colour script is running.
    private const int LineWarmColumn = 4;
    private const int LineCoolColumn = 5;
    private const int FogSunColumn = 13;
    private const int FogAntiColumn = 14;

    private static ImageTexture? _fallbackPalette;
    private static ImageTexture? _fallbackMatTable;

    public static Rid FallbackPalette => (_fallbackPalette ??= BuildFallbackPalette()).GetRid();

    public static Rid FallbackMatTable => (_fallbackMatTable ??= BuildFallbackMatTable()).GetRid();

    private static ImageTexture BuildFallbackPalette()
    {
        var fog = Colors.Gainsboro;
        var line = Colors.DimGray;
        var bytes = Godot.FileAccess.GetFileAsBytes("res://data/d0/palette.json");
        if (bytes.Length > 0)
        {
            using var document = JsonDocument.Parse(bytes);
            fog = Read(document.RootElement, "d0.fog", fog);
            line = Read(document.RootElement, "d0.text", line);
        }
        var image = Image.CreateEmpty(256, 64, false, Image.Format.Rgba8);
        image.Fill(fog);
        image.SetPixel(LineWarmColumn, 0, line);
        image.SetPixel(LineCoolColumn, 0, line);
        image.SetPixel(FogSunColumn, 0, fog);
        image.SetPixel(FogAntiColumn, 0, fog);
        return ImageTexture.CreateFromImage(image);
    }

    private static Color Read(JsonElement root, string token, Color fallback)
    {
        if (!root.TryGetProperty(token, out var value) || value.GetArrayLength() < 3) return fallback;
        return new Color(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle());
    }

    private static ImageTexture BuildFallbackMatTable()
    {
        // Row 0 per ID: R width ×2/255 (1.0 → 128), G tint 0.22 (56), B flags, A small-item fade end /2 m (0 = none).
        var image = Image.CreateEmpty(128, 4, false, Image.Format.Rgba8);
        var line = new Color(128f / 255f, 56f / 255f, 0f, 0f);
        for (var id = 0; id < 128; id++) image.SetPixel(id, 0, line);
        image.SetPixel(GenericNoLineId, 0, new Color(128f / 255f, 56f / 255f, NoLineFlag / 255f, 0f));
        return ImageTexture.CreateFromImage(image);
    }
}

using Campus.Contracts.Quality;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Shares native viewport clarity mapping and writes between production and isolated preparation.</summary>
internal static class ViewportClarity
{
    internal readonly record struct ClaritySettings(Viewport.Scaling3DModeEnum Mode, float Scale,
        Viewport.ScreenSpaceAAEnum AntiAliasing, float Sharpness);

    // Player sharpness increases towards the sharp end of Godot's reversed range.
    internal static ClaritySettings MapClarity(float scale, float antiAliasing, float sharpness) => new(
        scale < 1 ? Viewport.Scaling3DModeEnum.Fsr : Viewport.Scaling3DModeEnum.Bilinear, scale,
        antiAliasing switch
        {
            0 => Viewport.ScreenSpaceAAEnum.Smaa,
            1 => Viewport.ScreenSpaceAAEnum.Fxaa,
            _ => throw new ArgumentOutOfRangeException(nameof(antiAliasing)),
        }, scale < 1 ? QualityRules.FsrSharpnessMax - sharpness : 0);

    internal static void ApplyClarity(Viewport viewport, in ClaritySettings settings)
    {
        // Godot has separate setters; submit them together without a frame boundary and skip unchanged values.
        if (viewport.Scaling3DMode != settings.Mode) viewport.Scaling3DMode = settings.Mode;
        if (viewport.Scaling3DScale != settings.Scale) viewport.Scaling3DScale = settings.Scale;
        if (viewport.ScreenSpaceAA != settings.AntiAliasing) viewport.ScreenSpaceAA = settings.AntiAliasing;
        if (viewport.FsrSharpness != settings.Sharpness) viewport.FsrSharpness = settings.Sharpness;
    }
}

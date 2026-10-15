using System.Numerics;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>Existing approved D0 values only; no runtime palette evaluation or GPU resources.</summary>
internal static class D0RenderColors
{
    // Exact normalized sRGB values from style/palette/keys/registry.fallback.json.
    internal static readonly Vector3 WindowWarmSrgb = new(1f, 0.878431f, 0.658824f);
    internal static readonly Vector3 FogSunSrgb = new(0.952941f, 0.823529f, 0.65098f);
    internal static readonly Vector3 FogAntiSrgb = new(0.721569f, 0.713725f, 0.839216f);

    // project.godot's medium_color is a source_color, so its CPU shader twin is linear RGB.
    internal static readonly Vector3 MediumLinear = ToLinear(new(0.5f, 0.6f, 0.7f));

    internal static Vector3 ToLinear(Vector3 srgb) => new(Linear(srgb.X), Linear(srgb.Y), Linear(srgb.Z));

    private static float Linear(float value) => value <= 0.04045f
        ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
}

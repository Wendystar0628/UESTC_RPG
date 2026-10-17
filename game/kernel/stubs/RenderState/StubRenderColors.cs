using System.Numerics;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>Default medium colour for the not-yet-implemented medium publisher.</summary>
internal static class StubRenderColors
{
    // project.godot's medium_color is a source_color, so its CPU shader twin is linear RGB.
    internal static readonly Vector3 MediumLinear = ToLinear(new(0.5f, 0.6f, 0.7f));

    internal static Vector3 ToLinear(Vector3 srgb) => new(Linear(srgb.X), Linear(srgb.Y), Linear(srgb.Z));

    private static float Linear(float value) => value <= 0.04045f
        ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
}

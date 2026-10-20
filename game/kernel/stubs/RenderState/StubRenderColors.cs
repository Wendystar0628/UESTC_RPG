using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>Default medium colour for the not-yet-implemented medium publisher.</summary>
internal static class StubRenderColors
{
    // project.godot's medium_color is a source_color, so its CPU shader twin is linear RGB.
    internal static readonly Vector3 MediumLinear = ToLinear(new(0.5f, 0.6f, 0.7f));

    internal static Vector3 ToLinear(Vector3 srgb) => new((float)Oklab.SrgbToLinear(srgb.X), (float)Oklab.SrgbToLinear(srgb.Y), (float)Oklab.SrgbToLinear(srgb.Z));

}

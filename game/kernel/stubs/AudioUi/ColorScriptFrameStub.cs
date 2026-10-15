using System.Numerics;
using Campus.Contracts;
using Campus.Kernel.Stubs.RenderState;

namespace Campus.Kernel.Stubs.AudioUi;

/// <summary>Static autumn golden-hour coefficients; does not activate the real post-processing chain.</summary>
[KernelService(typeof(IColorScriptFrame), Owner = "WP-00", IsStub = true)]
public sealed class ColorScriptFrameStub : IColorScriptFrame
{
    public ColorScriptSnapshot Current { get; } = new(
        Rev: 1,
        LineDarkness: 0.15f,
        Glow: 0.02f,
        Vignette: 0.10f,
        Exposure: 0f,
        Stars: 0f,
        CloudCover: 0f,
        CdlSlope: new Vector3(0.97f),
        CdlOffset: new Vector3(0.02f),
        CdlPower: Vector3.One,
        FogLow: Vector4.Zero,
        CdlSaturation: 1.03f,
        GlowHdrThreshold: 1f,
        GlowHdrScale: 0.3f,
        GlowIntensity: 0.6f,
        VignetteTint: new Vector3(0.92f, 0.90f, 0.93f),
        Fog: new Vector4(1f, 60f, 200f, 800f))
    {
        LampPoolGain = 1f,
        FogSun = D0RenderColors.ToLinear(D0RenderColors.FogSunSrgb),
        FogAnti = D0RenderColors.ToLinear(D0RenderColors.FogAntiSrgb)
    };
}

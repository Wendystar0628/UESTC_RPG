using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>Explicit D0 key-light defaults from project.godot; replaced by WP-03's publisher.</summary>
[KernelService(typeof(IKeyLightState), Owner = "WP-00", IsStub = true)]
public sealed class KeyLightStateStub : IKeyLightState
{
    public Vector3 KeyDir => new(-0.870594f, 0.095846f, 0.482578f);
    public float KeyStrength => 1f;
    public float ShadowStrength => 1f;
    public float RetreatY => -10f;
    public Vector3 OvercastDir => new(-0.50167f, 0.819152f, 0.278078f);
    public KeyBody Body => KeyBody.Sun;
}

/// <summary>Explicit clear-weather shader defaults; replaced by WP-45b's publisher.</summary>
[KernelService(typeof(IWeatherRenderState), Owner = "WP-00", IsStub = true)]
public sealed class WeatherRenderStateStub : IWeatherRenderState
{
    public Vector4 WeatherFog => new(1f, 0f, 0f, 0f);
}

/// <summary>Explicit air-medium shader defaults; replaced by WP-07's publisher.</summary>
[KernelService(typeof(IMediumRenderState), Owner = "WP-00", IsStub = true)]
public sealed class MediumRenderStateStub : IMediumRenderState
{
    public Vector4 Medium => new(0f, 0f, 0f, 1f);
    public Vector3 MediumColor => D0RenderColors.MediumLinear;
}

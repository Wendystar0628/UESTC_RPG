using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

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
    public Vector3 MediumColor => StubRenderColors.MediumLinear;
}

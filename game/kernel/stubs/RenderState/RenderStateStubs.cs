using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>Explicit air-medium shader defaults; replaced by WP-07's publisher.</summary>
[KernelService(typeof(IMediumRenderState), IsStub = true)]
public sealed class MediumRenderStateStub : IMediumRenderState
{
    public Vector4 Medium => new(0f, 0f, 0f, 1f);
    public Vector3 MediumColor => StubRenderColors.MediumLinear;
}

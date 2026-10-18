using Campus.Contracts;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IWindField), Owner = "WP-00", IsStub = true)]
public sealed class WindFieldStub : IWindField
{
    public System.Numerics.Vector2 Direction => System.Numerics.Vector2.Zero;
    public WindSample Sample(float x, float z, double animTime) => new(0, 0);
}

using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Lighting;

public interface IKeyLightPublisher
{
    void Publish(in KeyLightFrame frame);
}

[KernelService(typeof(IKeyLightPublisher))]
[KernelService(typeof(IKeyLightState))]
public sealed class KeyLightState : IKeyLightState, IKeyLightPublisher
{
    public Vector3 KeyDir { get; private set; } = KeyLightModel.WithElevation(new(-0.870594f, 0f, 0.482578f), 5.48f);
    public float KeyStrength { get; private set; } = 1f;

    public void Publish(in KeyLightFrame frame)
    {
        KeyDir = frame.LightDir;
        KeyStrength = frame.KeyStrength;
    }
}

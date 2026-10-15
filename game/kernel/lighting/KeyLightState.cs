using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Lighting;

[KernelService(typeof(IKeyLightState), Owner = "WP-03")]
public sealed class KeyLightState : IKeyLightState
{
    public Vector3 KeyDir { get; private set; } = KeyLightModel.WithElevation(new(-0.870594f, 0f, 0.482578f), 5.48f);
    public float KeyStrength { get; private set; } = 1f;
    public float ShadowStrength { get; private set; } = 1f;
    public float RetreatY { get; private set; } = KeyLightModel.RetreatLow;
    public Vector3 OvercastDir { get; private set; } = KeyLightModel.WithElevation(new(-0.870594f, 0f, 0.482578f), KeyLightModel.OvercastElevationDeg);
    public KeyBody Body { get; private set; }

    internal void Publish(in KeyLightFrame frame)
    {
        KeyDir = frame.LightDir;
        KeyStrength = frame.KeyStrength;
        ShadowStrength = frame.ShadowStrength;
        RetreatY = frame.RetreatY;
        OvercastDir = frame.OvercastDir;
        Body = frame.Body;
    }
}

using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Lighting;

/// <summary>
/// Read-only C# twin of the key-light globals (key_dir, key_strength, shadow_strength, retreat_y,
/// overcast_dir), for code that cannot read shader globals, e.g. compute passes (contract-7 item 2).
/// Values are those written in the current frame's key-light step; nothing reads the globals back.
/// </summary>
public interface IKeyLightState
{
    /// <summary>Unit vector towards the key body, altitude clamped to 4°; identical to the light's LIGHT.</summary>
    Vector3 KeyDir { get; }
    float KeyStrength { get; }
    float ShadowStrength { get; }
    float RetreatY { get; }
    Vector3 OvercastDir { get; }
    KeyBody Body { get; }
}

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

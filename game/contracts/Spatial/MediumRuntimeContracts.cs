namespace Campus.Contracts;

/// <summary>Pure .NET view of the runtime services available to medium presenters.</summary>
public interface IRuntimeContext
{
    IWorldState World { get; }
    ICameraRig Camera { get; }
    IRenderHost Render { get; }
}

/// <summary>Optional medium overrides. Null leaves the underlying presentation unchanged.</summary>
public sealed record PostOverride(
    float? Exposure = null,
    float? Saturation = null,
    float? Contrast = null,
    float? Vignette = null,
    PaletteToken? FogColor = null,
    PaletteToken? TintColor = null,
    float? VisibilityM = null,
    float? TintStrength = null,
    float? ShadowStrength = null,
    uint? VisibleLayers = null)
{
    public static PostOverride None { get; } = new();
}

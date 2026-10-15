using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>
/// Minimal D0 registry for UI and fog consumers, using the already approved fallback entries.
/// The production WP-04 source replaces this declaration and loads the complete core/extension registry.
/// </summary>
[KernelService(typeof(IPaletteRegistrySource), Owner = "WP-00", IsStub = true)]
public sealed class PaletteRegistrySourceStub : IPaletteRegistrySource
{
    public PaletteRegistry Registry { get; } = new([
        new PaletteEntry(new("window.warm"), 10, D0RenderColors.WindowWarmSrgb, "frame", false, PaletteRole.Frame),
        new PaletteEntry(new("fog.sun"), 13, D0RenderColors.FogSunSrgb, "frame", false, PaletteRole.Frame),
        new PaletteEntry(new("fog.anti"), 14, D0RenderColors.FogAntiSrgb, "frame", false, PaletteRole.Frame)
    ]);
}

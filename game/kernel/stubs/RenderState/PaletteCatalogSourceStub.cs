using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>D0 CPU catalog for the approved fallback entries; it does not claim texture readiness.</summary>
[KernelService(typeof(IPaletteCatalogSource), Owner = "WP-00", IsStub = true)]
[KernelService(typeof(IPaletteIndexResolver), Owner = "WP-00", IsStub = true)]
public sealed class PaletteCatalogSourceStub : IPaletteCatalogSource, IPaletteIndexResolver
{
    public PaletteCatalog Catalog { get; } = new(new PaletteRegistry([
        new PaletteEntry(new("window.warm"), 10, D0RenderColors.WindowWarmSrgb, "frame", false, PaletteRole.Frame),
        new PaletteEntry(new("fog.sun"), 13, D0RenderColors.FogSunSrgb, "frame", false, PaletteRole.Frame),
        new PaletteEntry(new("fog.anti"), 14, D0RenderColors.FogAntiSrgb, "frame", false, PaletteRole.Frame)
    ]), []);
    public PaletteIndex Resolve(PaletteToken token) => Catalog.Resolve(token);
}

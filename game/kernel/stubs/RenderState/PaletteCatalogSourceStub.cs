using Campus.Contracts;

namespace Campus.Kernel.Stubs.RenderState;

/// <summary>Wraps the currently selected legacy source only. Does not load schema-2 files or claim texture readiness.</summary>
[KernelService(typeof(IPaletteCatalogSource), Owner = "WP-00", IsStub = true)]
[KernelService(typeof(IPaletteIndexResolver), Owner = "WP-00", IsStub = true)]
public sealed class PaletteCatalogSourceStub(IPaletteRegistrySource legacy) : IPaletteCatalogSource, IPaletteIndexResolver
{
    public PaletteCatalog Catalog { get; } = PaletteCatalog.FromLegacy(legacy.Registry);
    public PaletteIndex Resolve(PaletteToken token) => Catalog.Resolve(token);
}

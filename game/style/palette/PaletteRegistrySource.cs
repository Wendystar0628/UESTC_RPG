using Campus.Contracts;

namespace Campus.Style.Palette;

/// <summary>核心表、schema 1 基础扩展与 ext-v2 在启动时合成同一份不可变 CPU 目录，不创建 GPU 资源。</summary>
[KernelService(typeof(IPaletteCatalogSource), Owner = "WP-04")]
[KernelService(typeof(IPaletteIndexResolver), Owner = "WP-04")]
public sealed class PaletteRegistrySource : IPaletteCatalogSource, IPaletteIndexResolver
{
    public PaletteCatalog Catalog { get; } = PaletteRegistryLoader.LoadCatalog();
    public PaletteIndex Resolve(PaletteToken token) => Catalog.Resolve(token);
}

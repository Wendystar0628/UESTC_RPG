using Campus.Contracts;

namespace Campus.Style.Palette;

/// <summary>
/// 色名登记表的正式来源（替换 WP-00 的 D0 桩）：用 <see cref="PaletteRegistryLoader"/> 读核心表与各包扩展表，
/// 只读 json，不建调色板节点、纹理、材质或任何 GPU 资源。Kernel 在 Discover 之后把 <see cref="Registry"/>
/// 注册成 PaletteRegistry 服务；取色号、校验色名的包依赖 PaletteRegistry 或本契约，不依赖本命名空间。
/// </summary>
/// <remarks>
/// 同一个实例也是完整色表的来源（contract-28，替换 WP-00 的 PaletteCatalogSourceStub）：<see cref="Catalog"/> 是启动时
/// 读一次的不可变快照，含 ext-v2 的 256–511；<see cref="Registry"/> 就是它的 <see cref="PaletteCatalog.LegacyRegistry"/>，
/// 旧的 byte 色号入口与新入口看到的是同一份旧表。
/// </remarks>
[KernelService(typeof(IPaletteRegistrySource), Owner = "WP-04")]
[KernelService(typeof(IPaletteCatalogSource), Owner = "WP-04")]
[KernelService(typeof(IPaletteIndexResolver), Owner = "WP-04")]
public sealed class PaletteRegistrySource : IPaletteRegistrySource, IPaletteCatalogSource, IPaletteIndexResolver
{
    public PaletteCatalog Catalog { get; } = PaletteRegistryLoader.LoadCatalog();
    public PaletteRegistry Registry => Catalog.LegacyRegistry;
    public PaletteIndex Resolve(PaletteToken token) => Catalog.Resolve(token);
}

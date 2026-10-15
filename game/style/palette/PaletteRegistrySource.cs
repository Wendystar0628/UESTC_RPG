using Campus.Contracts;

namespace Campus.Style.Palette;

/// <summary>
/// 色名登记表的正式来源（替换 WP-00 的 D0 桩）：用 <see cref="PaletteRegistryLoader"/> 读核心表与各包扩展表，
/// 只读 json，不建调色板节点、纹理、材质或任何 GPU 资源。Kernel 在 Discover 之后把 <see cref="Registry"/>
/// 注册成 PaletteRegistry 服务；取色号、校验色名的包依赖 PaletteRegistry 或本契约，不依赖本命名空间。
/// </summary>
[KernelService(typeof(IPaletteRegistrySource), Owner = "WP-04")]
public sealed class PaletteRegistrySource : IPaletteRegistrySource
{
    public PaletteRegistry Registry { get; } = PaletteRegistryLoader.Load();
}

namespace Campus.Contracts;

/// <summary>
/// Engine-independent source for the one shared palette registry. The composition root registers Registry
/// as the PaletteRegistry service before constructing palette, material or UI consumers.
/// </summary>
/// <remarks>
/// The production source loads core.json and package extensions through WP-04's PaletteRegistryLoader.Load().
/// Resolving this source must not construct a palette node, texture, material or any other GPU resource.
/// Consumers depend on this contract or PaletteRegistry, never the loader's implementation namespace.
/// </remarks>
public interface IPaletteRegistrySource
{
    PaletteRegistry Registry { get; }
}

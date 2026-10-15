using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>
/// WP-24 owns the independent reflection world and material; the kernel owns proxy instances.
/// All access and Changed notifications occur on the main thread. An invalid Scenario means unavailable:
/// consumers release their proxies and must not read ProxyMaterial until Scenario becomes valid.
/// Publish an invalid Scenario and raise Changed before freeing the old world, then raise Changed when the new world is ready.
/// </summary>
public interface IReflectionWorld
{
    Rid Scenario { get; }
    Material ProxyMaterial { get; }
    event Action? Changed;
}

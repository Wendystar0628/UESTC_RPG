using Godot;
using Campus.Contracts;

namespace Campus.Contracts;

/// <summary>
/// game/systems/water/ owns the independent reflection world and material; the kernel owns proxy instances.
/// All access and Changed notifications occur on the main thread. An invalid Scenario means unavailable:
/// consumers release their proxies and must not read ProxyMaterial until Scenario becomes valid.
/// Publish an invalid Scenario and raise Changed before freeing the old world, then raise Changed when the new world is ready.
/// </summary>
public interface IReflectionWorld
{
    Rid Scenario { get; }
    Material ProxyMaterial { get; }
    event Action? Changed;
    /// <summary>Queries the real water reflection for this source view without changing the applied lake or mirror camera.</summary>
    /// <remarks>Returned scratch memory must be consumed before the next query. False means no eligible lake or unavailable reflection resources.</remarks>
    bool TryGetDemand(in StreamDemandView source, out StreamDemandView reflected);
}

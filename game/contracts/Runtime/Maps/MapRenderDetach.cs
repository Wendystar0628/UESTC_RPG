namespace Campus.Contracts.Maps;

/// <summary>
/// Synchronously detaches map-owned rendering instances before any map-unload callback frees engine resources.
/// Engine resource release can flush unrelated dirty instances, so every detach participant runs first.
/// Detachment is idempotent and does not replace the subsequent IMapAware resource-release callback.
/// </summary>
public interface IMapRenderDetach
{
    void DetachMapInstances(MapId from);
}

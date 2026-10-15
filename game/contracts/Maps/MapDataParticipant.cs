namespace Campus.Contracts.Maps;

/// <summary>
/// Prepares map data after all registered map nodes enter the tree and before any IMapAware.OnMapLoaded callback.
/// Implement on an existing registered instance; the host deduplicates instances, rather than registering this interface as one service.
/// </summary>
public interface IMapDataParticipant
{
    Task Prepare(IMapContext context);
    /// <summary>Called after all IMapAware.OnMapUnloaded consumers have released the previous map's data.</summary>
    void Unload(MapId from);
}

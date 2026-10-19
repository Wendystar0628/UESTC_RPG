using Campus.Contracts.Maps;

namespace Campus.Contracts.Bake;

/// <summary>Prepared, immutable generation inputs shared by the baker and the streaming catalog.</summary>
public interface IBakeContextSource
{
    /// <summary>
    /// Called on the composition thread after map preparation, before starting a catalog worker.
    /// Returns the already prepared context for this exact map-context identity and registered module,
    /// including that producer's CodeHash, resolved claims, frozen inputs and the common window table.
    /// The query performs no IO, lazy preparation or mutation. Unknown modules, an unprepared map,
    /// a stale context or an unloaded map fail explicitly. Previously returned immutable inputs remain
    /// readable; the worker owner must still reject late results using its map generation/cancellation.
    /// </summary>
    BuildContext GetPreparedContext(IMapContext context, string moduleId);

    /// <summary>
    /// Returns the current prepared map's single immutable L0 tile index, keyed by module ID.
    /// Construction runs once on a worker and is shared by streaming and collision dependencies.
    /// Invalid or stale map contexts fail at entry; callers reject late results with their map generation.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>> GetPreparedModuleTilesAsync(IMapContext context);
}

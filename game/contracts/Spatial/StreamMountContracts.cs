using Campus.Contracts.Bake;

namespace Campus.Contracts;

public enum StreamMountKind : byte { Visual, Collision, Auxiliary }

/// <summary>Prepared main-thread work; it owns its engine objects without exposing engine types.</summary>
public interface IStreamMountWork
{
    BakeKey Key { get; }
    BakePriority Priority { get; }
    /// <summary>Queue revision captured when the source request starts, before background work begins.</summary>
    long Revision { get; }
    StreamMountKind Kind { get; }
    /// <summary>Advances one bounded unit; true means the whole work completed, false leaves it queued.</summary>
    /// <remarks>Only the sole StreamMount node calls this, on the main thread. Collision advances at most one body.
    /// A visual step may advance multiple instances within one tile and its budget; it never starts another tile.
    /// On exception the queue calls Cancel and reports the failure. No background generation is allowed here.</remarks>
    bool Step(double budgetMilliseconds);
    /// <summary>Idempotently cancels unfinished work and releases its partially mounted objects on the main thread.</summary>
    /// <remarks>After successful completion the producer owns the mounted objects and releases them on map unload.</remarks>
    void Cancel();
}

/// <summary>WP-02b owns the unique FramePriority.StreamMount executor and all shared mount time accounting.</summary>
public interface IStreamMountQueue
{
    /// <summary>Monotonically increases on map unload, including unloading and reloading the same map.</summary>
    long Revision { get; }
    /// <summary>Main-thread enqueue. Rejects stale revisions or maps and cancels their unfinished work.</summary>
    void Enqueue(IStreamMountWork work);
    /// <summary>Cancels this map's queued work and invalidates prior request revisions.</summary>
    /// <remarks>Only WP-02b calls this from its map-unload callback. Producers separately cancel their own work
    /// and release completed mounts; they do not advance the shared queue revision.</remarks>
    void RemoveMap(MapId map);
}
// Per-frame caps: total <= 1.0 ms; Collision <= one Step and <= 0.3 ms of that total;
// Visual <= one Step. Auxiliary work also consumes the same remaining total budget.

using Campus.Contracts.Bake;

namespace Campus.Contracts;

public enum StreamMountKind : byte { Visual, Collision, Auxiliary }

/// <summary>A real main-thread resource owner retained until its bounded release is complete.</summary>
public interface IStreamReleaseWork
{
    /// <summary>Releases bounded main-thread units. True means ownership is empty.</summary>
    /// <remarks>Even with zero remainder, advances at most one indivisible release unit. Native calls cannot be preempted.</remarks>
    bool ReleaseStep(double budgetMilliseconds);
}

/// <summary>Prepared main-thread work; it owns its engine objects without exposing engine types.</summary>
public interface IStreamMountWork : IStreamReleaseWork
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
    /// <summary>Stops further mounting and immediately revokes readiness; native ownership remains until ReleaseStep completes.</summary>
    void Cancel();
}

/// <summary>The sole stream-mount executor owns FramePriority.StreamMount and all shared mount time accounting.</summary>
public interface IStreamMountQueue
{
    /// <summary>Unfinished mounts only; never includes retirement owners.</summary>
    int Pending { get; }
    /// <summary>Actual retirement owners still retained by this queue, including failed releases and pending native preparation.</summary>
    int PendingRetirements { get; }
    /// <summary>Time spent in mount Step calls this frame, excluding preparation, visibility completion and release.</summary>
    double LastMilliseconds { get; }
    int WatchdogTrips { get; }
    /// <summary>Original mount, cancellation and release failures consumed by the map lifetime owner.</summary>
    Exception? Failure { get; }
    /// <summary>Monotonically increases when the map lifetime is unloaded.</summary>
    long Revision { get; }
    /// <summary>Main-thread enqueue. Rejects stale revisions or maps and retires their actual work.</summary>
    void Enqueue(IStreamMountWork work);
    /// <summary>Retains the actual release owner until completion, regardless of revision or failure.</summary>
    /// <remarks>A mount owner is cancelled immediately to stop mounting and revoke readiness. A release-only owner has no mount identity or cancellation.</remarks>
    void Retire(IStreamReleaseWork work);
    /// <summary>Stops this map's queued mounts, retires them and invalidates prior request revisions.</summary>
    /// <remarks>Only the map-unload coordinator calls this from its map-unload callback. Producers separately cancel their own work
    /// and retire completed mounts; they do not advance the shared queue revision.</remarks>
    void RemoveMap(MapId map);
}
// Mounts have first use of the existing frame slice; retirement uses its remainder, with one bounded release unit per frame.
// Visible-frame target: total <= 1.0 ms plus an indivisible native operation; Collision <= one Step and <= 0.3 ms of that total;
// Visual <= one Step. Auxiliary work shares the remainder. Actual opaque screen coverage
// permits multiple tiles/steps in a shared UI-responsive slice; readiness rules are unchanged.

/// <summary>One existing view and its near-layer world bounds. Frustum planes point outward: Normal dot p + D is at most zero inside.</summary>
/// <remarks>The caller owns and reuses the memory; participants consume it during Demand and never retain it.</remarks>
public readonly record struct StreamDemandView(Pose Eye,
    ReadOnlyMemory<System.Numerics.Plane> Frustum,
    ReadOnlyMemory<Box3> Near)
{
    /// <summary>The live observer's feet while walking; independent of render and reflection eyes.</summary>
    public System.Numerics.Vector3? WalkingCenter { get; init; }
    /// <summary>An outstanding walking arrival or retained static-resource destination.</summary>
    public System.Numerics.Vector3? WalkingDestination { get; init; }
    /// <summary>Static resources without a world instance; dynamic world-state producers wait for formal demand.</summary>
    public bool ResourcePreload { get; init; }
}

public enum StreamTileNeed : byte { None, Retain, Required }

/// <summary>A fixed tile payload owner; StreamDirector owns its request and mount lifecycle.</summary>
public interface IStreamTileParticipant
{
    /// <summary>Collision or Auxiliary; selects the existing queue budget and request priority.</summary>
    StreamMountKind Kind { get; }
    bool CanMount(BakeKey key);
    /// <summary>Pure demand from frozen influence bounds, this view and residency. Retain keeps an existing request or mount without creating a new one.</summary>
    StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident);
    /// <summary>Creates bounded work without engine side effects; the shared queue alone advances it.</summary>
    IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision);
    /// <summary>Mounted resources are ready. World/view display state belongs to IViewContentReadiness.</summary>
    bool IsReady(BakeKey key);
    Exception? Failure { get; }
}

/// <summary>Main-thread snapshots of existing stream owners; never a readiness or scheduling input.</summary>
public interface IStreamStatistics
{
    StreamStatistics Statistics { get; }
    event Action<StreamTileChange>? TileChanged;
}

public readonly record struct StreamStatistics
{
    public long Revision { get; init; }
    public int PendingRequests { get; init; }
    public int PendingBakes { get; init; }
    public int PendingPreparations { get; init; }
    public int PendingMounts { get; init; }
    public int PendingRetirements { get; init; }
    public double MountMilliseconds { get; init; }
    public int WatchdogTrips { get; init; }
    // Completed packed visual payload bytes, not driver upload bytes.
    public long MountedBytes { get; init; }
    // Successfully completed mounts this frame; unlike MountedTilesDelta, never subtracts retirements.
    public int MountedTiles { get; init; }
    public int MountedTilesDelta { get; init; }
}

public enum StreamTileChangeKind : byte { Mounted, Unmounted }
public readonly record struct StreamTileChange(BakeKey Key, StreamMountKind Kind,
    long Revision, StreamTileChangeKind Change, long? Bytes, double MonotonicMilliseconds);

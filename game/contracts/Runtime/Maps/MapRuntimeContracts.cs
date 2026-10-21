using Campus.Contracts.AppFlow;

namespace Campus.Contracts.Maps;

/// <summary>Main-thread destination preparation. The latest arrival is the only current request.</summary>
public interface IMapRuntime
{
    /// <summary>Original preparation and runtime failures for this map lifetime; cancellation is separate.</summary>
    Exception? Failure { get; }
    /// <summary>Receives a terminal map-lifetime failure into the same projection; retains original causes and startup codes.</summary>
    AppStartupException ReportFailure([FailureHandler("SYS-MAPS")] Exception error);
    /// <summary>Destination is the presented eye pose including FOV; walking collision uses collisionFeet when supplied.</summary>
    IMapArrival Arrive(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null);
    /// <summary>Cancels the formal arrival and clears its demand while retaining map resources and background pins.</summary>
    void ClearDestination();
    IMapArrival? Arrival { get; }
    event Action<IMapArrival?>? ArrivalChanged;
}

/// <summary>
/// One destination's readiness barrier, including resident content, destination geometry, current interior content and walking collision.
/// Done succeeds only when ready, faults on preparation failure, and is cancelled on cancellation/replacement/exit.
/// A completed handle remains a historical result; compare it with IMapRuntime.Arrival before reusing readiness.
/// Cancel never moves the camera or rolls back mounted content; cancelling an older handle cannot affect its replacement.
/// </summary>
public interface IMapArrival
{
    MapStreamDestination Destination { get; }
    Task Done { get; }
    int Completed { get; }
    int Total { get; }
    event Action? ProgressChanged;
    void Cancel();
}

/// <summary>
/// Startup map lifetime, separate from arrival. Mount runs once and completes after data preparation,
/// MapAware callbacks and required resident content. Shutdown belongs to the scene host.
/// </summary>
public interface IMapLifetime
{
    Task Mount(IMapContext context);
    /// <summary>Synchronously rejects/cancels new map work before the owner awaits background preparation.</summary>
    void StopNewWork();
    /// <summary>Drains and releases the map captured by this lifetime; never resolves a new map service during shutdown.</summary>
    Task ShutdownAsync();
    string DescribeReadiness();
}

/// <summary>
/// game/kernel/stream/ stream state read by the kernel map runtime. Startup and shutdown dispatch through IMapAware.
/// IsUnloaded is the shutdown drain condition: submitted preparation has returned and its runtime resources
/// have been released. No new requests start during shutdown.
/// </summary>
public sealed record MapStreamDestination(Pose Pose, ViewMode Mode, NavSpace? Space = null,
    System.Numerics.Vector3? CollisionFeet = null);

public interface IMapStreamState
{
    bool IsUnloaded { get; }
    bool ResidentReady { get; }
    bool DestinationReady { get; }
    Exception? Failure { get; }
    /// <summary>After OnMapUnloading stops producers, await every submitted task and release stream-owned resources.</summary>
    Task DrainAsync();
    MapStreamDestination? Destination { get; } // Prefetch sets this; shutdown clears it. Collision/interior consumers request the same destination.
    /// <summary>True while the latest prefetch still needs destination residency. After successful arrival this becomes false;
    /// Destination and DestinationReady keep the latest result until the next request or unload. Consumers only pin an active destination.
    /// </summary>
    bool HasPendingDestination { get; }
    /// <summary>Retained static-resource destinations in the active map; they never replace the formal arrival.</summary>
    IReadOnlyList<MapStreamDestination> PinnedDestinations { get; }
    /// <summary>Main-thread demand replacement; unchanged targets retain their work. An empty collection releases pins.</summary>
    void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations);
    /// <summary>Queries current-generation mounted and retained resources, independently of world/view/physics arrival.</summary>
    bool PinnedDestinationReady(MapStreamDestination destination);
    /// <summary>Clears formal/current demand, preserving pins and reusable resident resources.</summary>
    void ClearDestination();
    void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null);
}

// Stream readiness is consumed by the kernel arrival coordinator. Presentation callers use arrival handles.

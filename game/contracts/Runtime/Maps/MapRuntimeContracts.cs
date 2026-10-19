namespace Campus.Contracts.Maps;

/// <summary>Main-thread destination preparation. The latest arrival is the only current request.</summary>
public interface IMapRuntime
{
    IMapArrival Arrive(Pose destination, ViewMode mode, NavSpace? space = null);
    IMapArrival? Arrival { get; }
    event Action<IMapArrival?>? ArrivalChanged;
}

/// <summary>
/// One destination's readiness barrier, including resident content, destination geometry and pipeline warming.
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
    Task ShutdownAsync(MapId map);
    string DescribeReadiness();
}

/// <summary>
/// WP-02b stream state read by the kernel map runtime. Startup and shutdown dispatch through IMapAware.
/// IsUnloaded is the shutdown drain condition: submitted preparation has returned and its runtime resources
/// have been released. No new requests start during shutdown.
/// </summary>
public sealed record MapStreamDestination(Pose Pose, ViewMode Mode, NavSpace? Space = null);

public interface IMapStreamState
{
    bool IsUnloaded { get; }
    bool ResidentReady { get; }
    bool DestinationReady { get; }
    string? Error { get; }
    MapStreamDestination? Destination { get; } // Prefetch sets this; shutdown clears it. Collision/interior consumers request the same destination.
    /// <summary>True while the latest prefetch still needs destination residency. After successful arrival this becomes false;
    /// Destination and DestinationReady keep the latest result until the next request or unload. Consumers only pin an active destination.
    /// </summary>
    bool HasPendingDestination { get; }
    void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null);
}

// Stream readiness is consumed by the kernel arrival coordinator. Presentation callers use arrival handles.

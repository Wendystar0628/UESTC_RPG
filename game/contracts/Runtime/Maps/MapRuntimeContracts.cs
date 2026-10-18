namespace Campus.Contracts.Maps;

/// <summary>
/// Kernel-owned map lifecycle boundary. WP-01 aggregates node lifetime/MapAware dispatch,
/// shader map parameters and the WP-02b stream state. WP-46 owns startup and same-map entry ordering.
/// Methods run on the main thread; synchronous failures throw, asynchronous failures set Error.
/// Mount runs once at startup; shutdown belongs to the scene host. No engine handles cross this boundary.
/// </summary>
public interface IMapRuntime
{
    void Mount(IMapContext context);
    void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null);
    void BeginWarm();
    bool ResidentReady { get; }
    bool DestinationReady { get; }
    bool WarmReady { get; }
    string? Error { get; }
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
    /// Legacy implementations conservatively keep their destination pinned.</summary>
    bool HasPendingDestination => Destination is not null;
    void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null);
}

// Mount dispatches MapAware and begins resident loading. ResidentReady covers required resident content;
// Prefetch replaces the one destination request; DestinationReady includes required destination geometry
// and collision, including the explicit Space's interior floor content when supplied by a save.
// BeginWarm starts kernel pipeline/UI warming; WarmReady is independent of stream readiness.
// MapStage.Warm waits for all required ready flags; Error is terminal for that operation and never counts as ready.

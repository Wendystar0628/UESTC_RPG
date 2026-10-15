namespace Campus.Contracts.Maps;

/// <summary>
/// Kernel-owned map lifecycle boundary. WP-01 aggregates node lifetime/MapAware dispatch,
/// shader map parameters and the WP-02b stream state. WP-46 owns transition ordering and rollback.
/// Methods run on the main thread; synchronous failures throw, asynchronous failures set Error.
/// A new unload/mount operation clears the preceding operation's Error. No engine handles cross this boundary.
/// </summary>
public interface IMapRuntime
{
    void Unload(MapId from);
    void Mount(IMapContext context);
    void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null);
    void BeginWarm();
    bool IsUnloaded { get; }
    bool ResidentReady { get; }
    bool DestinationReady { get; }
    bool WarmReady { get; }
    string? Error { get; }
}

/// <summary>
/// WP-02b stream state read by the kernel map runtime. Loading/unloading starts through IMapAware,
/// not by fetching IMapService. IsUnloaded is true only after submitted preparation work has returned
/// and every resulting runtime resource has been released. No new requests start during unload.
/// </summary>
public sealed record MapStreamDestination(Pose Pose, ViewMode Mode, NavSpace? Space = null);

public interface IMapStreamState
{
    bool IsUnloaded { get; }
    bool ResidentReady { get; }
    bool DestinationReady { get; }
    string? Error { get; }
    MapStreamDestination? Destination { get; } // Prefetch sets this; Unload clears it. Collision/interior consumers request the same destination.
    void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null);
}

// Mount dispatches MapAware and begins resident loading. ResidentReady covers required resident content;
// Prefetch replaces the one destination request; DestinationReady includes required destination geometry
// and collision, including the explicit Space's interior floor content when supplied by a save.
// BeginWarm starts kernel pipeline/UI warming; WarmReady is independent of stream readiness.
// MapStage.Warm waits for all required ready flags; Error is terminal for that operation and never counts as ready.

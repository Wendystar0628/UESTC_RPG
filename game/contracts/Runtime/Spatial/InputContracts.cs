using Campus.Contracts.Save;

namespace Campus.Contracts;

public readonly record struct InputActionEvent(string Action, bool Pressed, bool Echo);

/// <summary>Shared keyboard action routing; physical key codes are numeric to keep Godot outside contracts.</summary>
public interface IInputRouter
{
    /// <summary>Lower priority values run first; equal priority layers run newest first.</summary>
    /// <remarks>A handler returning true consumes the event. Disposing a registration removes only that layer.</remarks>
    IDisposable RegisterLayer(string owner, int priority, Func<InputActionEvent, bool> handler);
    /// <summary>Shows the cursor while this reason's lease is held; does not lock movement or keyboard actions.</summary>
    IDisposable RequestCursor(InputLockReason reason);
    /// <summary>Derived from cursor leases, the held cursor action and window focus by the input owner.</summary>
    bool CursorVisible { get; }
    /// <summary>True while the rebound show_cursor action is held, independently of cursor leases.</summary>
    bool CursorHeld { get; }
    event Action<bool>? CursorVisibilityChanged;
    /// <summary>Tries one physical keyboard binding; after a successful rebind the input owner saves it. False reports the conflicting action or rejection reason.</summary>
    /// <param name="physicalLocation">Native key location: unspecified, left or right, passed as its numeric value.</param>
    bool TryRebind(string action, int physicalKey, int physicalLocation, out string? conflict);
    int PhysicalKey(string action);
}

/// <summary>game/camera/ transit execution. The camera resolves the driver after registration to avoid a constructor cycle.</summary>
public interface ITransitDriver
{
    /// <summary>True only when real transit execution and its readiness dependencies are available. Stub implementations remain false.</summary>
    bool IsReady { get; }
    ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null);
    /// <summary>Runs a camera-owned mode commit under the same loading, cancellation and arrival lifecycle as teleport.</summary>
    ITransitHandle SwitchMode(Func<CancellationToken, Task<ModeSwitchResult>> apply);
    /// <summary>Start returns a request handle. Until terrain, space and target data are prepared, retain the
    /// settled snapshot captured immediately before that request; then publish the accepted target snapshot.
    /// Null means no request is active. Preparation never claims arrival or collision readiness.</summary>
    ObserverSaveSnapshot? SaveSnapshot { get; }
    /// <summary>Cancels and drains all current and superseded executions, including loading cleanup; the driver remains reusable.</summary>
    Task CancelAsync();
}

public interface ICameraPoseValidator
{
    /// <param name="outsideBuffer">True also excludes the inner edge buffer, as required for --pose and authored waypoints.
    /// False accepts the full reachable volume including its buffer.</param>
    bool IsLegal(Pose pose, bool outsideBuffer);
    /// <summary>Prepares each actual terrain query before projecting into the reachable volume.</summary>
    Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default);
}

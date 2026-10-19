using Campus.Contracts.Save;

namespace Campus.Contracts.AppFlow;

public sealed record BootFlowRequest(
    bool ShowTitle = true,
    SlotId? Slot = null,
    string? LoadFile = null,
    MapId? Map = null,
    string? Spawn = null,
    Pose? Pose = null,
    ViewMode? Mode = null,
    uint? Seed = null);

/// <summary>Startup-only boundary implemented by the same instance as IAppFlow.</summary>
public interface IAppBootFlow
{
    /// <summary>The already-running world preparation; entering from an early title joins this task.</summary>
    Task WorldReady { get; }
    /// <summary>Opens the title after UI, audio and save metadata are ready, independently of world loading.</summary>
    Task OpenTitle();
    /// <summary>Propagates background startup failure to any entry already waiting for the world.</summary>
    void FailBoot(Exception error);
    /// <summary>Accurate after PrepareBoot: a selected slot or external file will restore the world instead of initializing a new one.</summary>
    bool WillRestoreWorld { get; }
    // Called after BeforeMap participants. Retains the typed request, selects the slot and initial map,
    // and holds boot input. Explicit Map wins; otherwise use selected slot metadata then default map.
    // An external LoadFile may initially use the default map; its Full load performs the authoritative travel.
    Task<MapId> PrepareBoot(BootFlowRequest request);

    // Called after Travel(Boot) and AfterMapWarm participants. This is the sole boot loader:
    // selected slots use Preview with a title, Full without it; external files retain LoadFile's Full semantics.
    // It sets preview T0/flow or the resumed endpoint. No-save preview does not create a new game or take entropy.
    // For a fresh world the kernel sets initial time/season/weather before the first Travel, then restores
    // its requested flow before this call. Only view overrides may follow it before BeforeReveal.
    Task PrepareWorld();

    // Completes WorldReady after final arrival readiness. Title/Entering may already be active;
    // no-title startup enters Playing. Does not load, travel or acquire presentation ownership again.
    Task CompleteBoot();
}

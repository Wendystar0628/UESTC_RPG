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
    CinematicMode? CinematicMode = null,
    string? Cinematic = null,
    uint? Seed = null);

/// <summary>Startup-only boundary implemented by the same instance as IAppFlow.</summary>
public interface IAppBootFlow
{
    /// <summary>Accurate after PrepareBoot: a selected slot or external file will restore the world instead of initializing a new one.</summary>
    bool WillRestoreWorld => false;
    // Called after BeforeMap participants. Retains the typed request, selects the slot and initial map,
    // and holds cinematic input. Explicit Map wins; otherwise use selected slot metadata then default map.
    // An external LoadFile may initially use the default map; its Full load performs the authoritative travel.
    Task<MapId> PrepareBoot(BootFlowRequest request);

    // Called after Travel(Boot), initial warmup and AfterMapWarm participants. This is the sole boot loader:
    // selected slots use Preview with a title, Full without it; external files retain LoadFile's Full semantics.
    // It sets preview T0/flow or the resumed endpoint. No-save preview does not create a new game or take entropy.
    // For a fresh world the kernel sets initial time/season/weather before the first Travel, then restores
    // its requested flow before this call. Only view overrides may follow it before BeforeReveal.
    Task PrepareWorld();

    // Called only after the kernel's final Reveal. Changes Boot to Title/Playing and releases the boot input
    // hold when entering Playing. Does not load, travel, warm or reveal again. Failures propagate to Boot.
    Task CompleteBoot();
}

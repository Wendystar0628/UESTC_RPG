using Campus.Contracts.Save;
using Campus.Contracts.Maps;

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

/// <summary>Startup orchestration owned by the same instance as IAppFlow; the static title is independent of preparation.</summary>
public interface IAppBootFlow
{
    /// <summary>Map foundation and resident content, before resource pins start.</summary>
    Task MapReady { get; }
    /// <summary>Map foundation plus global pipeline/JIT preparation; does not certify a world or arrival.</summary>
    Task BackgroundReady { get; }
    /// <summary>Derived current-generation resource readiness for the canonical new-game spawn.</summary>
    bool NewGamePreloadReady { get; }
    Task OpenTitle();
    void FailBoot(Exception error);
    /// <summary>Retains the typed intent and selects the initial map, without creating/restoring a world.</summary>
    Task<MapId> PrepareBoot(BootFlowRequest request);
    /// <summary>Owns the real preparation tasks. Map completion starts pins immediately; failures retain both causes.</summary>
    Task PrepareBackground(Task mapPreparation, Task globalPreparation);
    /// <summary>Explicit no-menu/capture entry through the normal complete entry transaction.</summary>
    Task<IMapArrival> EnterDirect(WorldPreset? start = null, NewGameBoot? boot = null);
}

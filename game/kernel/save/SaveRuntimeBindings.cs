using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Diagnostics;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save;

public sealed record SaveRuntimeOptions(string Directory, bool Enabled, SlotId? WriteSlot,
    int? SaveAtFrame, uint? WeatherSeed);

/// <summary>Adapters and diagnostics are supplied once at boot. Live-world adapters are not called on the disk worker.</summary>
public sealed record SaveRuntimeBindings(IWorldState World, IMapService Maps, ICameraRig Camera,
    IObserverState Observer, IRuleset Ruleset, IAtlasService Atlas, IAppLoadingPresentation Loading,
    IAppFlow AppFlow, IDiagnosticLog Diagnostics, Action<RulesetId> SelectRuleset, IWorldRestore WorldRestore,
    Func<Pose, NavSpace?, ViewMode, Task> WaitDestination, Func<Task<byte[]?>> CaptureThumbnail, Func<long> UnixTime,
    Func<Pose, Task<Pose>>? PreparePose = null)
{
    /// <summary>Invoked on the save frame; finishes terrain/history repairs before the immediate Pose restores.</summary>
    public Func<WalkSaveData, RestoreContext, CancellationToken, Task<WalkSaveData>>? PrepareWalk { get; init; }
}

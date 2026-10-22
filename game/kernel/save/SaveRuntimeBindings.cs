using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save;

public sealed record SaveRuntimeOptions(string Directory, bool Enabled, SlotId? WriteSlot,
    int? SaveAtFrame, uint? WeatherSeed);

/// <summary>Engine adapters are supplied once at boot. None are called on the disk worker.</summary>
public sealed record SaveRuntimeBindings(IWorldState World, IMapService Maps, ICameraRig Camera,
    IObserverState Observer, IRuleset Ruleset, IAtlasService Atlas, IAppLoadingPresentation Loading,
    IAppFlow AppFlow, Action<RulesetId> SelectRuleset, IWorldRestore WorldRestore,
    Func<Pose, NavSpace?, ViewMode, Task> WaitDestination, Func<Task<byte[]?>> CaptureThumbnail, Func<long> UnixTime,
    Func<Pose, Task<Pose>>? PreparePose = null);

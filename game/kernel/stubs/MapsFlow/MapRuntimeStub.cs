using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IMapRuntime), Owner = "WP-00", IsStub = true)]
public sealed class MapRuntimeStub : IMapRuntime
{
    public bool IsUnloaded { get; private set; } = true;
    public bool ResidentReady => !IsUnloaded;
    public bool DestinationReady => !IsUnloaded;
    public bool WarmReady { get; private set; }
    public string? Error => null;
    public void Unload(MapId from) { IsUnloaded = true; WarmReady = false; }
    public void Mount(IMapContext context) { IsUnloaded = false; WarmReady = false; }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) { }
    public void BeginWarm() => WarmReady = !IsUnloaded;
}

[KernelService(typeof(IMapStreamState), Owner = "WP-00", IsStub = true)]
public sealed class MapStreamStateStub : IMapStreamState, IMapAware
{
    public bool IsUnloaded { get; private set; } = true;
    public bool ResidentReady => !IsUnloaded;
    public bool DestinationReady => !IsUnloaded;
    public string? Error => null;
    public MapStreamDestination? Destination { get; private set; }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => Destination = new(destination, mode, space);
    public void OnMapUnloading(MapId from) { IsUnloaded = true; Destination = null; }
    public void OnMapLoaded(IMapContext context) { IsUnloaded = false; Destination = null; }
}

using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using KernelRegistry = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud.Map;

/// <summary>地图界面依赖的内核服务，宿主取一次，小地图与图集页控制共用（不直接调 ICameraRig.Transit，传送经 IAtlasService）。</summary>
internal sealed class MapHudServices
{
    public IAtlasService Atlas { get; } = KernelRegistry.Get<IAtlasService>();
    public IAtlasBaseSource AtlasBase { get; } = KernelRegistry.Get<IAtlasBaseSource>();
    public IObserverState Observer { get; } = KernelRegistry.Get<IObserverState>();
    public ICameraRig Rig { get; } = KernelRegistry.Get<ICameraRig>();
    public IWorldState World { get; } = KernelRegistry.Get<IWorldState>();
    public IAppFlow Flow { get; } = KernelRegistry.Get<IAppFlow>();
    public IVeilState Veil { get; } = KernelRegistry.Get<IVeilState>();
    public IMotionSettings Motion { get; } = KernelRegistry.Get<IMotionSettings>();
    public IMediumState Medium { get; } = KernelRegistry.Get<IMediumState>();
}

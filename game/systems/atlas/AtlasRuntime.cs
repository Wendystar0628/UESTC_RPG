using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Systems.Atlas;

[KernelNode("atlas")]
public partial class AtlasRuntime : Godot.Node, IMapAware, IMapDataParticipant
{
    private readonly AtlasService _atlas;
    private readonly IBakeService _bake;
    private readonly AtlasBaseSource _baseSource;
    private readonly ITerrainPreparation _terrain;
    private IBakeHandle? _mapBake;
    private bool _productPublished;

    public AtlasRuntime(AtlasService atlas, AtlasBaseSource baseSource, IMapService maps, IAppFlow flow,
        IAudioService audio, IBakeService bake, IViewControl view, ITerrainPreparation terrain)
    {
        _atlas = atlas;
        _bake = bake;
        _baseSource = baseSource;
        _terrain = terrain;
        _atlas.Connect(maps, flow, audio, view);
    }

    public Task Prepare(IMapContext context)
    {
        _atlas.OnMapLoaded(context);
        var points = _atlas.Markers.Where(m => m.Teleport is not null).SelectMany(m =>
        {
            var destination = m.Teleport!;
            var fly = new System.Numerics.Vector2(destination.Fly.Position.X, destination.Fly.Position.Z);
            return destination.Stand is { } stand
                ? new[] { fly, new System.Numerics.Vector2(stand.Feet.X, stand.Feet.Z) } : new[] { fly };
        }).Distinct().ToArray();
        return _terrain.PreparePoints(context, "atlas.travel-poses", points);
    }
    public void Unload(MapId from) { }

    public override void _Ready() => ProcessPriority = FramePriority.Systems;
    public override void _Process(double delta)
    {
        if (!_productPublished && _mapBake is { State: BakeState.Ready, Product: MapBakeProduct product })
        {
            _productPublished = true;
            _baseSource.Publish(_atlas.Map, _atlas.PageRect, _atlas.Regions, product);
            _atlas.SetRegionLookup(product.Regions.RegionAt);
        }
        _atlas.Tick();
    }
    public void OnMapLoaded(IMapContext context)
    {
        _atlas.OnMapLoaded(context);
        _baseSource.Reset(context.Descriptor.Id);
        _mapBake = _bake.Ensure(new(context.Descriptor.Id, "map", "base"), BakePriority.Normal);
        _productPublished = false;
    }
    public void OnMapUnloading(MapId from)
    {
        _mapBake = null; _productPublished = false;
        _baseSource.Reset(default);
        _atlas.OnMapUnloading(from);
    }
}

using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Pipeline.Map;

namespace Campus.Systems.Atlas;

[KernelNode("atlas", Owner = "WP-48c")]
public partial class AtlasRuntime : Godot.Node, IMapAware
{
    private readonly AtlasService _atlas;
    private readonly IBakeService _bake;
    private readonly AtlasBaseSource _baseSource;
    private IBakeHandle? _mapBake;
    private bool _productPublished;

    public AtlasRuntime(IAtlasService atlas, IAtlasBaseSource baseSource, IMapService maps, IAppFlow flow,
        IAudioService audio, ISaveService saves, IBakeService bake)
    {
        _atlas = (AtlasService)atlas;
        _bake = bake;
        _baseSource = (AtlasBaseSource)baseSource;
        _atlas.Connect(maps, flow, audio, saves);
    }

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

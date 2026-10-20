using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Maps;

namespace Campus.Systems.Fauna;

/// <summary>Owns streamed fauna tiles, mount generations, and their render resources.</summary>
internal sealed class FaunaTileMounts
{
    private readonly Dictionary<BakeKey, FaunaTile> _tiles = [];
    private IRenderHost? _host;
    private Action<FaunaTile>? _initialize;
    private Func<Box3, bool>? _visible;
    private Action<string>? _failed;
    private long _generation;

    public MapId? Map { get; private set; }
    public Dictionary<BakeKey, FaunaTile>.ValueCollection Tiles => _tiles.Values;
    internal IRenderHost Host => _host!;

    public void Initialize(IRenderHost host, Action<FaunaTile> initialize, Func<Box3, bool> visible, Action<string> failed)
    {
        _host = host;
        _initialize = initialize;
        _visible = visible;
        _failed = failed;
    }

    public void LoadMap(MapId map) => Map = map;
    public void ClearMap() => Map = null;
    public bool TryGet(BakeKey key, out FaunaTile tile) => _tiles.TryGetValue(key, out tile!);
    public bool IsCurrent(FaunaTile tile) => _generation == tile.Generation && Map == tile.Content.Key.Map;
    public bool IsVisible(Box3 bounds) => _visible!(bounds);
    public void Failed(string error) => _failed!(error);

    public IStreamMountWork CreateMount(FaunaTileContent content, BakePriority priority, long revision)
    {
        var tile = new FaunaTile(content, priority, revision, _generation);
        return tile.Work = new FaunaTileMountWork(this, tile, initial: true);
    }

    public FaunaTileMountWork CreateUpdate(FaunaTile tile) => new(this, tile, initial: false);

    public bool Begin(FaunaTile tile, bool initial)
    {
        if (initial)
        {
            if (!_tiles.TryAdd(tile.Content.Key, tile))
                throw new InvalidOperationException("Fauna owner tile is already mounted.");
            _initialize!(tile);
            return true;
        }
        return _tiles.TryGetValue(tile.Content.Key, out var current) && ReferenceEquals(current, tile);
    }

    public void Unmount(BakeKey key)
    {
        if (!_tiles.Remove(key, out var tile)) return;
        tile.Work?.Cancel();
        Free(tile);
    }

    public void Free(FaunaTile tile)
    {
        tile.Mounted = false;
        foreach (var latch in tile.Latches)
        {
            foreach (var handle in latch.Handles) _host!.Unmount(handle);
            latch.Handles.Clear();
            latch.Applied = false;
            latch.Requested = false;
        }
    }

    public void Release()
    {
        _generation++;
        foreach (var tile in _tiles.Values) { tile.Work?.Cancel(); Free(tile); }
        _tiles.Clear();
    }
}

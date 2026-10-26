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
    private Action<Exception>? _failed;
    private long _generation;

    public MapId? Map { get; private set; }
    public Dictionary<BakeKey, FaunaTile>.ValueCollection Tiles => _tiles.Values;
    internal IRenderHost Host => _host!;

    public void Initialize(IRenderHost host, Action<FaunaTile> initialize, Func<Box3, bool> visible, Action<Exception> failed)
    {
        _host = host;
        _initialize = initialize;
        _visible = visible;
        _failed = failed;
    }

    public void LoadMap(MapId map) => Map = map;
    public void ClearMap() => Map = null;
    public bool TryGet(BakeKey key, out FaunaTile tile) => _tiles.TryGetValue(key, out tile!);
    public bool IsCurrent(FaunaTile tile) => _generation == tile.Generation && Map == tile.Content.Key.Map && !tile.Retiring;
    public bool IsVisible(Box3 bounds) => _visible!(bounds);
    public void Failed(Exception error) => _failed!(error);

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

    public bool ReleaseStep(FaunaTile tile, double budgetMilliseconds)
    {
        tile.Mounted = false;
        var start = Campus.Kernel.Frame.FrameClock.MonotonicMilliseconds;
        do
        {
            if (tile.ReleaseLatch >= tile.Latches.Length)
            {
                if (_tiles.TryGetValue(tile.Content.Key, out var current) && ReferenceEquals(current, tile)) _tiles.Remove(tile.Content.Key);
                return true;
            }
            var latch = tile.Latches[tile.ReleaseLatch];
            var handles = latch.RetiringHandles.Count > 0 ? latch.RetiringHandles : latch.Handles;
            if (handles.Count > 0)
            {
                var handle = handles[^1];
                Host.Unmount(handle);
                if (!Host.IsReleased(handle)) return false;
                handles.RemoveAt(handles.Count - 1);
            }
            else
            { latch.Applied = latch.Requested = false; tile.ReleaseLatch++; }
        } while (Campus.Kernel.Frame.FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
        return false;
    }

    public void HarvestReleased(FaunaTile tile)
    {
        foreach (var latch in tile.Latches)
            for (var index = latch.RetiringHandles.Count - 1; index >= 0; index--)
                if (Host.IsReleased(latch.RetiringHandles[index])) latch.RetiringHandles.RemoveAt(index);
    }

    public void Release()
    {
        _generation++;
    }
}

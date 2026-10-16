using Campus.Contracts.Bake;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Stream;

public readonly record struct StreamRequest(BakeKey Key, StreamTile Tile, BakePriority Priority, float NeededInSeconds);

/// <summary>Only unissued requests are replaceable; issued bake handles are never cancelled.</summary>
public sealed class StreamRequestQueue
{
    private readonly PriorityQueue<StreamRequest, RequestOrder> _pending = new();
    public int Count => _pending.Count;
    public void Replace(IEnumerable<StreamRequest> requests)
    {
        _pending.Clear();
        foreach (var request in requests)
        {
            if (!float.IsFinite(request.NeededInSeconds)) throw new ArgumentException("Request time must be finite.");
            _pending.Enqueue(request, new(request));
        }
    }
    public bool TryDequeue(out StreamRequest request) => _pending.TryDequeue(out request, out _);
    public void Clear() => _pending.Clear();
    private readonly record struct RequestOrder(StreamRequest Request) : IComparable<RequestOrder>
    {
        public int CompareTo(RequestOrder other)
        {
            var order = Request.Priority.CompareTo(other.Request.Priority);
            if (order != 0) return order;
            order = other.Request.Tile.Level.CompareTo(Request.Tile.Level);
            if (order != 0) return order;
            order = Request.NeededInSeconds.CompareTo(other.Request.NeededInSeconds);
            if (order != 0) return order;
            order = Request.Tile.CompareTo(other.Request.Tile);
            if (order != 0) return order;
            order = StringComparer.Ordinal.Compare(Request.Key.Module, other.Request.Key.Module);
            return order != 0 ? order : StringComparer.Ordinal.Compare(Request.Key.Detail, other.Request.Key.Detail);
        }
    }
}

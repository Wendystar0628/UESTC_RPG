using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;

namespace Campus.Systems.Fauna;

public partial class FaunaNode
{
    /// <summary>One site's prepared output per shared auxiliary step; material readiness is local to its handles.</summary>
    private sealed class TileWork(FaunaNode node, TileState tile, bool initial) : IStreamMountWork
    {
        private bool _started;
        private bool _cancelled;
        private bool _complete;
        private int _site = -1;
        private FaunaSet? _target;
        private long _epoch;
        private long _version;
        public BakeKey Key => tile.Content.Key;
        public BakePriority Priority => tile.Priority;
        public long Revision => tile.Revision;
        public StreamMountKind Kind => StreamMountKind.Auxiliary;

        public bool Step(double budgetMilliseconds)
        {
            if (_cancelled || _complete) return true;
            if (budgetMilliseconds <= 0) return false;
            try
            {
                if (node._generation != tile.Generation || node._map != Key.Map) { Cancel(); return true; }
                if (!_started)
                {
                    if (initial)
                    {
                        if (!node._tiles.TryAdd(Key, tile)) throw new InvalidOperationException("Fauna owner tile is already mounted.");
                        for (var i = 0; i < tile.Latches.Length; i++)
                            node.Request(tile.Latches[i], node.Target(tile.Content.Content.Sites[i]), anywhere: true, visible: false);
                    }
                    else if (!node._tiles.TryGetValue(Key, out var current) || !ReferenceEquals(current, tile))
                    { Cancel(); return true; }
                    _started = true;
                }
                if (_site >= 0)
                {
                    var applied = tile.Latches[_site];
                    foreach (var handle in applied.Handles) if (!node._host!.IsReady(handle)) return false;
                    applied.Set = _target;
                    applied.Applied = true;
                    applied.AppliedEpoch = _epoch;
                    if (applied.Version == _version) applied.Requested = false;
                    _site = -1;
                    return false;
                }
                for (var i = 0; i < tile.Latches.Length; i++)
                {
                    var latch = tile.Latches[i];
                    if (!latch.Requested) continue;
                    var site = tile.Content.Content.Sites[i];
                    // Recheck at application time: camera motion between the 4 Hz check and Step must not pop a visible site.
                    if (!latch.AllowVisible && node.VisibleNow(site.Bounds))
                    {
                        latch.Requested = false;
                        continue;
                    }
                    Begin(i);
                    // Each original site contains one species and therefore one fur/feather surface.
                    // Replacing it in the same Step preserves the off-screen latch across queue frames.
                    if (!latch.Applied || latch.Set != _target)
                    {
                        foreach (var handle in latch.Handles) node._host!.Unmount(handle);
                        latch.Handles.Clear();
                        var output = _target is { } set ? site.Sets[(int)set] : ContentOutput.Empty;
                        if (output.Meshes.Count > 0 || output.Parts.Count > 0 || output.Instances.Count > 0)
                            latch.Handles.Add(node._host!.Mount(output, Package));
                    }
                    latch.Applied = false;
                    return false;
                }
                tile.Mounted = true;
                tile.Work = null;
                _complete = true;
                return true;
            }
            catch (Exception error)
            {
                node._error = error.GetBaseException().Message;
                Cancel();
                throw;
            }
        }

        private void Begin(int site)
        {
            _site = site;
            var latch = tile.Latches[site];
            _target = latch.Desired;
            _epoch = latch.Epoch;
            _version = latch.Version;
        }

        public void Cancel()
        {
            if (_cancelled || _complete) return;
            _cancelled = true;
            node.Free(tile);
            tile.Work = null;
        }
    }
}

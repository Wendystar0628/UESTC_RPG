using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Kernel.Stream;

namespace Campus.Systems.Fauna;

/// <summary>One site's prepared output per shared auxiliary step; material readiness is local to its handles.</summary>
internal sealed class FaunaTileMountWork(FaunaTileMounts mounts, FaunaTile tile, bool initial)
    : StreamMountWork(tile.Content.Key, tile.Priority, tile.Revision, StreamMountKind.Auxiliary)
{
    private bool _started;
    private int _site = -1;
    private FaunaSet? _target;
    private long _epoch;
    private long _version;
    protected override bool IsCurrent => mounts.IsCurrent(tile);

    protected override bool MountStep(double budgetMilliseconds)
    {
        if (!_started)
        {
            if (!mounts.Begin(tile, initial)) { Cancel(); return true; }
            _started = true;
        }
        if (_site >= 0) return FinishSite();
        for (var i = 0; i < tile.Latches.Length; i++)
        {
            var latch = tile.Latches[i];
            if (!latch.Requested) continue;
            var site = tile.Content.Content.Sites[i];
            // Recheck at application time: camera motion between the 4 Hz check and Step must not pop a visible site.
            if (!latch.AllowVisible && mounts.IsVisible(site.Bounds))
            {
                latch.Requested = false;
                continue;
            }
            Begin(i);
            // Each original site contains one species and therefore one fur/feather surface.
            // Replacing it in the same Step preserves the off-screen latch across queue frames.
            if (!latch.Applied || latch.Set != _target)
            {
                foreach (var handle in latch.Handles) mounts.Host.Unmount(handle);
                latch.Handles.Clear();
                var output = _target is { } set ? site.Sets[(int)set] : ContentOutput.Empty;
                if (output.Meshes.Count > 0 || output.Parts.Count > 0 || output.Instances.Count > 0)
                    latch.Handles.Add(mounts.Host.Mount(output));
            }
            latch.Applied = false;
            return false;
        }
        tile.Mounted = true;
        tile.Work = null;
        return true;
    }

    private bool FinishSite()
    {
        var applied = tile.Latches[_site];
        foreach (var handle in applied.Handles) if (!mounts.Host.IsReady(handle)) return false;
        applied.Set = _target;
        applied.Applied = true;
        applied.AppliedEpoch = _epoch;
        if (applied.Version == _version) applied.Requested = false;
        _site = -1;
        return false;
    }

    private void Begin(int site)
    {
        _site = site;
        var latch = tile.Latches[site];
        _target = latch.Desired;
        _epoch = latch.Epoch;
        _version = latch.Version;
    }

    protected override void Release() { mounts.Free(tile); tile.Work = null; }
    protected override void Failed(Exception error) => mounts.Failed(error.Message);
}

using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Systems.Nightlight.Glow;
using N = System.Numerics;

namespace Campus.Systems.Nightlight;

public partial class NightlightNode
{
    private sealed record MountedTile(NightlightTileContent Content, GlowMount Mount);
    private readonly Dictionary<BakeKey, MountedTile> _mounted = [];
    private readonly List<NightlightTileContent> _reflectionTiles = [];
    private long _generation;
    private Pose? _appliedPose;

    public bool CanMount(BakeKey key) => key.Module is BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows;

    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
    {
        if (!CanMount(entry.Key)) return StreamTileNeed.None;
        return GlowDemand.Required(entry, view) ? StreamTileNeed.Required : StreamTileNeed.None;
    }

    public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision)
    {
        if (!CanMount(key) || product is not NightlightTileContent tile || tile.Key != key)
            throw new ArgumentException("Nightlight mount needs its matching fixed tile payload.");
        var request = BakeRequestKeys.Parse(key);
        if (request.Tile != tile.Tile || request.Level != LodLevel.L0 || request.Collision ||
            tile.Glow.Kind != (key.Module == BakeModuleIds.NightlightLamps ? GlowKind.Lamp : GlowKind.Window))
            throw new ArgumentException("Nightlight owner tile or glow kind does not match its key.");
        return new MountWork(this, tile, priority, revision, _generation);
    }

    public bool IsReady(BakeKey key) => Error is null && _map?.Descriptor.Id == key.Map &&
        _mounted.TryGetValue(key, out var tile) && tile.Mount.IsReady;

    public void Unmount(BakeKey key)
    {
        if (!_mounted.Remove(key, out var tile)) return;
        tile.Mount.Free();
        _reflectionTiles.Remove(tile.Content);
        _appliedPose = null;
    }

    private void FreeTiles()
    {
        _generation++;
        foreach (var tile in _mounted.Values) tile.Mount.Free();
        _mounted.Clear();
        _reflectionTiles.Clear();
        _appliedPose = null;
    }

    private sealed class MountWork(NightlightNode owner, NightlightTileContent tile,
        BakePriority priority, long revision, long generation) : IStreamMountWork
    {
        private GlowMount? _mount;
        private bool _complete;
        private bool _cancelled;
        public BakeKey Key => tile.Key;
        public BakePriority Priority => priority;
        public long Revision => revision;
        public StreamMountKind Kind => StreamMountKind.Auxiliary;

        public bool Step(double budgetMilliseconds)
        {
            if (_cancelled || _complete) return true;
            if (budgetMilliseconds <= 0d) return false;
            if (owner._generation != generation || owner._map?.Descriptor.Id != Key.Map)
            {
                Cancel();
                return true;
            }
            try
            {
                var pools = owner._pools ?? throw new InvalidOperationException("Nightlight instance pools are not ready.");
                _mount = new(tile.Glow.Kind, $"{Key.Module}.{tile.Tile.X}.{tile.Tile.Z}");
                _mount.Upload(pools, () => owner.GlowMaterial(tile.Glow.Kind), tile.Glow);
                owner.Unmount(Key);
                owner._mounted.Add(Key, new(tile, _mount));
                owner._reflectionTiles.Add(tile);
                owner._appliedPose = null;
                _complete = true;
                return true;
            }
            catch (Exception error)
            {
                owner.Error = error.GetBaseException().Message;
                Cancel();
                throw;
            }
        }

        public void Cancel()
        {
            if (_cancelled || _complete) return;
            _cancelled = true;
            _mount?.Free();
            _mount = null;
        }
    }
}

/// <summary>Conservative bounds of the existing fx_glow shader, including ground-light reach and distant angular halos.</summary>
internal static class GlowDemand
{
    // These are the existing fx_glow shader constants, not independent art controls.
    private const float WindowFadeEnd = 250f;
    private const float LampMinimumAngle = 0.0022f;
    private const float LampPull = 0.35f;

    public static bool Required(in BakeTileEntry entry, in StreamDemandView view)
    {
        var bounds = entry.Bounds;
        var eye = view.Eye.Position;
        var pad = GlowMount.BoundsPad;
        if (entry.Key.Module == BakeModuleIds.NightlightWindows)
        {
            var closest = N.Vector3.Clamp(eye, bounds.Min - new N.Vector3(pad), bounds.Max + new N.Vector3(pad));
            if (N.Vector3.DistanceSquared(eye, closest) > WindowFadeEnd * WindowFadeEnd) return false;
        }
        else
        {
            var farthest = N.Vector3.Distance(eye, bounds.Center) + bounds.Size.Length() * 0.5f;
            pad += farthest * LampMinimumAngle + LampPull;
        }
        return Visible(new(bounds.Min - new N.Vector3(pad), bounds.Max + new N.Vector3(pad)), view.Frustum.Span);
    }

    private static bool Visible(Box3 bounds, ReadOnlySpan<N.Plane> planes)
    {
        foreach (var plane in planes)
        {
            var p = new N.Vector3(plane.Normal.X >= 0 ? bounds.Min.X : bounds.Max.X,
                plane.Normal.Y >= 0 ? bounds.Min.Y : bounds.Max.Y, plane.Normal.Z >= 0 ? bounds.Min.Z : bounds.Max.Z);
            if (N.Vector3.Dot(plane.Normal, p) + plane.D > 0f) return false;
        }
        return true;
    }
}

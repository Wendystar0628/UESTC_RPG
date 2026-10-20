using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Instances;
using Godot;
using N = System.Numerics;

namespace Campus.Systems.Nightlight.Glow;

internal sealed class GlowTileMounts(IInstancePoolHost pools, IShaderMaterialSource materials)
{
    private readonly IInstancePoolHost _pools = pools;
    private sealed record MountedTile(NightlightTileContent Content, GlowMount Mount);
    private readonly Dictionary<BakeKey, MountedTile> _mounted = [];
    private readonly List<NightlightTileContent> _reflectionTiles = [];
    private long _generation;
    private ShaderMaterial? _lampMaterial;
    private ShaderMaterial? _windowMaterial;
    private MapId? _map;
    public IReadOnlyList<NightlightTileContent> ReflectionTiles => _reflectionTiles;
    public long Revision { get; private set; }
    public string? Error { get; private set; }
    public bool Ready
    {
        get
        {
            if (Error is not null) return false;
            foreach (var tile in _mounted.Values) if (!tile.Mount.IsReady) return false;
            return true;
        }
    }

    public void Bind(MapId? map)
    {
        FreeTiles();
        _map = map;
        Error = null;
    }

    public void Dispose()
    {
        Bind(null);
        _lampMaterial?.Dispose();
        _windowMaterial?.Dispose();
        _lampMaterial = null;
        _windowMaterial = null;
    }

    private ShaderMaterial GlowMaterial(GlowKind kind) => kind switch
    {
        GlowKind.Lamp => _lampMaterial ??= materials.Create(ShaderId.FxGlow),
        GlowKind.Window => _windowMaterial ??= materials.Create(ShaderId.FxGlow),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

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

    public bool IsReady(BakeKey key) => Error is null && _map == key.Map &&
        _mounted.TryGetValue(key, out var tile) && tile.Mount.IsReady;

    public void Unmount(BakeKey key)
    {
        if (!_mounted.Remove(key, out var tile)) return;
        tile.Mount.Free();
        _reflectionTiles.Remove(tile.Content);
        Revision++;
    }

    private void FreeTiles()
    {
        _generation++;
        foreach (var tile in _mounted.Values) tile.Mount.Free();
        _mounted.Clear();
        _reflectionTiles.Clear();
        Revision++;
    }

    private sealed class MountWork(GlowTileMounts owner, NightlightTileContent tile,
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
            if (owner._generation != generation || owner._map != Key.Map)
            {
                Cancel();
                return true;
            }
            try
            {
                _mount = new(tile.Glow.Kind, $"{Key.Module}.{tile.Tile.X}.{tile.Tile.Z}");
                _mount.Upload(owner._pools, () => owner.GlowMaterial(tile.Glow.Kind), tile.Glow);
                owner.Unmount(Key);
                owner._mounted.Add(Key, new(tile, _mount));
                owner._reflectionTiles.Add(tile);
                owner.Revision++;
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
        return new Box3(bounds.Min - new N.Vector3(pad), bounds.Max + new N.Vector3(pad)).Intersects(view.Frustum.Span);
    }

}

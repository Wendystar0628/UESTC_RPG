using Campus.Kernel.Lod;

namespace Campus.Kernel.Stream;

/// <summary>Atomic target changes with complementary parent/child shader fades.</summary>
public sealed class TileVisibility(float fadeSeconds)
{
    private readonly HashSet<StreamTile> _target = [];
    private readonly HashSet<StreamTile> _drawn = [];
    private readonly Dictionary<StreamTile, float> _retiring = [];
    private readonly List<StreamTile> _scratch = [];
    public IReadOnlySet<StreamTile> Drawn => _drawn;

    public void Apply(IReadOnlySet<StreamTile> desired, float clock, bool fadesReady,
        Action<StreamTile, bool, float> change)
    {
        // Finish old fades before beginning new ones. The shader uses the same wrapped clock.
        _scratch.Clear();
        foreach (var pair in _retiring)
            if (Elapsed(clock, pair.Value) >= fadeSeconds) _scratch.Add(pair.Key);
        foreach (var tile in _scratch)
        {
            _retiring.Remove(tile); _drawn.Remove(tile); change(tile, false, 0);
        }
        if (_target.SetEquals(desired)) return;

        var count = _target.Count(t => !desired.Contains(t)) + desired.Count(t => !_target.Contains(t));
        // Retargeting an unfinished blend directly prevents duplicate coverage at three levels.
        var fade = fadesReady && count <= 50 && _retiring.Count == 0 && clock > .0001f;
        _scratch.Clear(); _scratch.AddRange(_drawn.Where(t => !desired.Contains(t)).Order());
        foreach (var tile in _scratch)
        {
            if (fade) { _retiring[tile] = clock; change(tile, true, -clock); }
            else { _retiring.Remove(tile); _drawn.Remove(tile); change(tile, false, 0); }
        }
        foreach (var tile in desired.Order())
        {
            var entering = !_target.Contains(tile);
            _retiring.Remove(tile); _drawn.Add(tile);
            if (entering) change(tile, true, fade ? clock : 0);
        }
        _target.Clear(); _target.UnionWith(desired);
    }
    public void Clear()
    {
        _target.Clear(); _drawn.Clear(); _retiring.Clear(); _scratch.Clear();
    }
    public static float Elapsed(float now, float then) => now >= then ? now - then : now + 480 - then;
}

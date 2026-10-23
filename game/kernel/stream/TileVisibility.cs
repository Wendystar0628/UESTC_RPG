using Campus.Contracts;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Stream;

/// <summary>Atomic target changes with complementary parent/child shader fades.</summary>
public sealed class TileVisibility(float fadeSeconds)
{
    private readonly HashSet<StreamTile> _target = [];
    private readonly HashSet<StreamTile> _drawn = [];
    private readonly Dictionary<StreamTile, float> _retiring = [];
    private readonly Dictionary<StreamTile, float> _entering = [];
    private readonly List<StreamTile> _scratch = [];
    public IReadOnlySet<StreamTile> Drawn => _drawn;
    public bool IsSettled(float clock)
    {
        if (_retiring.Count != 0) return false;
        foreach (var started in _entering.Values)
            if (Elapsed(clock, started) < fadeSeconds) return false;
        return true;
    }
    public void Apply(HashSet<StreamTile> desired, float clock, bool fadesReady,
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
        _scratch.Clear();
        foreach (var pair in _entering)
            if (Elapsed(clock, pair.Value) >= fadeSeconds) _scratch.Add(pair.Key);
        foreach (var tile in _scratch) _entering.Remove(tile);
        if (_target.SetEquals(desired)) return;

        // Keep the current complementary pair intact; the next frame supplies the latest target.
        if (_retiring.Count != 0 || _entering.Count != 0) return;
        // Zero is the shader's settled sentinel, so a fade cannot begin at the exact clock origin.
        var fade = fadesReady && clock > 0;
        _scratch.Clear();
        foreach (var tile in _drawn)
            if (!desired.Contains(tile)) _scratch.Add(tile);
        _scratch.Sort();
        foreach (var tile in _scratch)
        {
            _entering.Remove(tile);
            if (fade) { _retiring[tile] = clock; change(tile, true, -clock); }
            else { _retiring.Remove(tile); _drawn.Remove(tile); change(tile, false, 0); }
        }
        _scratch.Clear();
        foreach (var tile in desired) _scratch.Add(tile);
        _scratch.Sort();
        foreach (var tile in _scratch)
        {
            var entering = !_target.Contains(tile);
            _retiring.Remove(tile); _drawn.Add(tile);
            if (entering)
            {
                if (fade) _entering[tile] = clock;
                change(tile, true, fade ? clock : 0);
            }
        }
        _target.Clear();
        foreach (var tile in desired) _target.Add(tile);
    }
    public void Clear()
    {
        _target.Clear(); _drawn.Clear(); _retiring.Clear(); _entering.Clear(); _scratch.Clear();
    }
    public static float Elapsed(float now, float then) => now >= then ? now - then : now + WorldTime.AnimLoopSeconds - then;
}

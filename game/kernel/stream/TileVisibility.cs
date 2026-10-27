using Campus.Contracts;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Stream;

/// <summary>Complementary frontier changes are atomic within their real replacement ancestor.</summary>
public sealed class TileVisibility(float fadeSeconds, Func<double> clock)
{
    private readonly Func<double> _clock = clock;
    private readonly HashSet<StreamTile> _target = [], _desired = [], _drawn = [];
    private readonly Dictionary<StreamTile, StreamTile> _retiring = [];
    private readonly Dictionary<StreamTile, float> _active = [];
    private readonly HashSet<StreamTile> _groupRoots = [], _activeAncestors = [];
    private readonly List<StreamTile> _groups = [], _scratch = [];
    private readonly List<(StreamTile Group, StreamTile Tile)> _members = [];
    public IReadOnlySet<StreamTile> Drawn => _drawn;
    public bool IsSettled => _active.Count == 0 && _target.SetEquals(_desired);

    public void Apply(HashSet<StreamTile> desired, float clock, bool fadesReady,
        Action<StreamTile, bool, float> change, double budgetMilliseconds)
    {
        var start = _clock();
        _desired.Clear(); _desired.UnionWith(desired);
        if (!FinishGroups(clock, change, start, budgetMilliseconds) || _target.SetEquals(desired)) return;
        BuildGroups(desired);
        for (var cursor = 0; cursor < _members.Count;)
        {
            var group = _members[cursor].Group;
            var end = cursor + 1;
            while (end < _members.Count && _members[end].Group == group) end++;
            if (_clock() - start >= budgetMilliseconds) return;
            var fading = _activeAncestors.Contains(group);
            for (var ancestor = group; !fading; ancestor = ancestor.Parent)
            {
                if (_active.ContainsKey(ancestor)) { fading = true; break; }
                if (ancestor == StreamTile.Root) break;
            }
            if (!fading) ApplyGroup(cursor, end, clock, fadesReady && clock > 0, change);
            cursor = end;
        }
    }

    private bool FinishGroups(float clock, Action<StreamTile, bool, float> change, double start, double budget)
    {
        _groups.Clear(); _members.Clear();
        foreach (var group in _active.Keys) _groups.Add(group);
        foreach (var pair in _retiring) _members.Add((pair.Value, pair.Key));
        _groups.Sort(); _members.Sort(CompareMembers);
        var cursor = 0;
        foreach (var group in _groups)
        {
            var first = cursor;
            while (cursor < _members.Count && _members[cursor].Group == group) cursor++;
            if (Elapsed(clock, _active[group]) < fadeSeconds) continue;
            if (_clock() - start >= budget) return false;
            for (var i = first; i < cursor; i++)
            {
                var tile = _members[i].Tile;
                change(tile, false, 0); _retiring.Remove(tile); _drawn.Remove(tile);
            }
            _active.Remove(group);
        }
        return true;
    }

    private void BuildGroups(HashSet<StreamTile> desired)
    {
        // A changed parent owns all changed descendants, including the real whole-map L4 product.
        // Common frontier tiles receive no redundant native writes.
        _scratch.Clear(); _groupRoots.Clear(); _activeAncestors.Clear(); _members.Clear();
        foreach (var tile in _target)
            if (!desired.Contains(tile)) _scratch.Add(tile);
        foreach (var tile in desired)
            if (!_target.Contains(tile)) _scratch.Add(tile);
        _scratch.Sort();
        foreach (var active in _active.Keys)
            for (var ancestor = active; ; ancestor = ancestor.Parent)
            {
                _activeAncestors.Add(ancestor);
                if (ancestor == StreamTile.Root) break;
            }
        foreach (var tile in _scratch)
        {
            var group = tile;
            for (var ancestor = tile; ; ancestor = ancestor.Parent)
            {
                if (_groupRoots.Contains(ancestor)) { group = ancestor; break; }
                if (ancestor == StreamTile.Root) break;
            }
            _groupRoots.Add(group); _members.Add((group, tile));
        }
        _members.Sort(CompareMembers);
    }

    private void ApplyGroup(int first, int end, float clock, bool fade, Action<StreamTile, bool, float> change)
    {
        var group = _members[first].Group;
        for (var i = first; i < end; i++)
        {
            var tile = _members[i].Tile;
            if (_desired.Contains(tile))
            {
                change(tile, true, fade ? clock : 0); _target.Add(tile); _drawn.Add(tile);
            }
            else
            {
                if (fade) { _retiring.Add(tile, group); change(tile, true, -clock); }
                else { change(tile, false, 0); _drawn.Remove(tile); }
                _target.Remove(tile);
            }
        }
        // Started groups keep their complementary pairs until the shared shader fade has finished.
        if (fade) _active.Add(group, clock);
    }

    private static int CompareMembers((StreamTile Group, StreamTile Tile) a, (StreamTile Group, StreamTile Tile) b)
    {
        var group = a.Group.CompareTo(b.Group);
        return group != 0 ? group : a.Tile.CompareTo(b.Tile);
    }
    public void Clear()
    {
        _target.Clear(); _desired.Clear(); _drawn.Clear(); _retiring.Clear(); _active.Clear();
        _groupRoots.Clear(); _activeAncestors.Clear(); _groups.Clear(); _scratch.Clear(); _members.Clear();
    }
    public static float Elapsed(float now, float then) => now >= then ? now - then : now + WorldTime.AnimLoopSeconds - then;
}

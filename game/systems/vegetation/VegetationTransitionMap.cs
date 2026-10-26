using Campus.Contracts;
using Godot;

namespace Campus.Systems.Vegetation;

/// <summary>
/// Main-thread CPU image and reusable contiguous slots for VegetationRuntime's near-pool prefixes.
/// MultiMesh has no independent instance-colour shader input: its colours alter the vertex colour carrying palette data,
/// so this texture preserves the existing transform/custom and vertex-colour contracts.
/// </summary>
internal sealed class VegetationTransitionMap : IDisposable
{
    private readonly List<(int Start, int Count)> _free = [];
    private Color[] _values = new Color[VegetationTransitionEncoding.TextureWidth];
    private int _length;
    private bool _closed;
    public Image Image { get; private set; } = Godot.Image.CreateEmpty(VegetationTransitionEncoding.TextureWidth, 1, false, Godot.Image.Format.Rgbaf);
    public bool Dirty { get; private set; } = true;
    public bool LayoutChanged { get; private set; } = true;

    public int Allocate(int count)
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        for (var i = 0; i < _free.Count; i++)
        {
            var range = _free[i];
            if (range.Count < count) continue;
            if (range.Count == count) _free.RemoveAt(i);
            else _free[i] = (range.Start + count, range.Count - count);
            return range.Start;
        }
        var start = _length;
        _length = checked(_length + count);
        var rows = (_length + VegetationTransitionEncoding.TextureWidth - 1) / VegetationTransitionEncoding.TextureWidth;
        if (rows <= Image.GetHeight()) return start;
        var image = Godot.Image.CreateEmpty(VegetationTransitionEncoding.TextureWidth, rows, false, Godot.Image.Format.Rgbaf);
        try { image.BlitRect(Image, new Rect2I(Vector2I.Zero, Image.GetSize()), Vector2I.Zero); }
        catch { image.Dispose(); throw; }
        Image.Dispose();
        Image = image;
        Array.Resize(ref _values, rows * VegetationTransitionEncoding.TextureWidth);
        LayoutChanged = Dirty = true;
        return start;
    }

    public void Release(int start, int count)
    {
        // A cancelled mount can return its slots after the old map's CPU image was already closed.
        if (_closed || start < 0) return;
        var index = 0;
        while (index < _free.Count && _free[index].Start < start) index++;
        _free.Insert(index, (start, count));
        if (index > 0 && _free[index - 1].Start + _free[index - 1].Count == start)
        {
            var previous = _free[index - 1];
            _free[index - 1] = (previous.Start, previous.Count + count);
            _free.RemoveAt(index);
            index--;
        }
        if (index + 1 < _free.Count && _free[index].Start + _free[index].Count == _free[index + 1].Start)
        {
            var current = _free[index];
            _free[index] = (current.Start, current.Count + _free[index + 1].Count);
            _free.RemoveAt(index + 1);
        }
    }

    public void Write(int slot, byte from, byte to, float started)
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        if (slot < 0 || slot >= _length) throw new ArgumentOutOfRangeException(nameof(slot));
        var value = new Color(from, to, started, 0f);
        if (_values[slot] == value) return;
        _values[slot] = value;
        Image.SetPixel(slot % VegetationTransitionEncoding.TextureWidth, slot / VegetationTransitionEncoding.TextureWidth,
            value);
        Dirty = true;
    }

    public void Uploaded() => LayoutChanged = Dirty = false;

    public void Dispose()
    {
        if (_closed) return;
        Image.Dispose();
        _closed = true;
        _free.Clear();
        _values = [];
    }
}

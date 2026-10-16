using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Core.Library;

/// <summary>
/// World-space glass in the VF-B encoding (toon_glass, the same as OpeningGlass): floor(UV) = (building-grid column,
/// row), UV2 = (window base, channel slot + 32 × total columns), spare byte = window card. The base is the building's
/// allocation from the map's WindowNumberingSnapshot; without one (legacy and no-window fixtures, BuildContext) the
/// glass carries no UV or UV2 at all, never a made-up base. One sink is one bundle family; every quad is wound against
/// its outward normal (Godot front faces are clockwise from outside).
/// </summary>
public sealed class GlassSink(ToonSpec spec, PaletteRegistry? palette, int? windowBase, ChannelId channel)
{
    private const int MaxVertices = 65000;
    private const float Margin = 0.002f;

    private readonly List<float> _position = [], _normal = [], _uv = [], _uv2 = [];
    private readonly List<byte> _card = [];
    private readonly List<int> _index = [];
    private readonly List<GeometryBundle> _done = [];
    private readonly int _slot = LightChannelCatalog.Slot(channel) >= 0
        ? LightChannelCatalog.Slot(channel)
        : throw new ArgumentException($"Unregistered light channel '{channel.Value}'.", nameof(channel));

    /// <summary>Total columns of the building's window grid; set once the facades are laid out.</summary>
    public int Columns { get; set; } = 1;

    /// <summary>
    /// One window cell: a planar quad a (bottom left seen from outside), b (bottom right), c (top right), d (top left)
    /// facing <paramref name="outward"/>, lighting as window (column, row).
    /// </summary>
    public void Pane(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int column, int row, byte card)
    {
        if (_position.Count / 3 + 4 > MaxVertices) Flush();
        var i = _position.Count / 3;
        float u0 = column + Margin, u1 = column + 1 - Margin, v0 = row + Margin, v1 = row + 1 - Margin;
        Vertex(a, outward, u0, v0, card);
        Vertex(b, outward, u1, v0, card);
        Vertex(c, outward, u1, v1, card);
        Vertex(d, outward, u0, v1, card);
        Tri(i, i + 1, i + 2, outward);
        Tri(i, i + 2, i + 3, outward);
    }

    /// <summary>A triangle of one cell (skylight panes): all three corners take the cell's centre position.</summary>
    public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int column, int row, byte card)
    {
        if (_position.Count / 3 + 3 > MaxVertices) Flush();
        var i = _position.Count / 3;
        Vertex(a, outward, column + 0.2f, row + 0.2f, card);
        Vertex(b, outward, column + 0.8f, row + 0.2f, card);
        Vertex(c, outward, column + 0.5f, row + 0.8f, card);
        Tri(i, i + 1, i + 2, outward);
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        Flush();
        return _done;
    }

    private void Vertex(Vector3 p, Vector3 n, float u, float v, byte card)
    {
        _position.Add(p.X); _position.Add(p.Y); _position.Add(p.Z);
        _normal.Add(n.X); _normal.Add(n.Y); _normal.Add(n.Z);
        _uv.Add(u); _uv.Add(v);
        _uv2.Add(windowBase ?? 0); _uv2.Add(_slot + 32f * Columns);
        _card.Add(card);
    }

    private void Tri(int a, int b, int c, Vector3 outward)
    {
        Vector3 P(int k) => new(_position[3 * k], _position[3 * k + 1], _position[3 * k + 2]);
        var g = Vector3.Cross(P(b) - P(a), P(c) - P(a));
        if (Vector3.Dot(g, outward) > 0f) (b, c) = (c, b);
        _index.Add(a); _index.Add(b); _index.Add(c);
    }

    private void Flush()
    {
        if (_index.Count == 0) return;
        var count = _position.Count / 3;
        // UV2.y carries the total column count; rewrite it so panes written before Columns was final agree.
        for (var k = 0; k < count; k++) _uv2[2 * k + 1] = _slot + 32f * Columns;
        var colour = palette?.Get(spec.Lit).Id;
        var numbered = windowBase is not null;
        var bundle = new GeometryBundle(new ToonRef(spec), [.. _position], [.. _normal], [.. _index], Uv: numbered ? [.. _uv] : null, Uv2: numbered ? [.. _uv2] : null)
        {
            ColorIds = colour is { } id ? Enumerable.Repeat(id, count).ToArray() : null,
            Flags = new byte[count],
            Reserved = [.. _card],
        };
        bundle.ValidateVertexData();
        _done.Add(bundle);
        _position.Clear(); _normal.Clear(); _uv.Clear(); _uv2.Clear(); _card.Clear(); _index.Clear();
    }
}

using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Openings;

namespace Campus.Gen.Zones.Core.Library;

/// <summary>
/// World-space glass of one bundle family. Every cell is encoded by WP-10's <see cref="OpeningGlass"/>, the one VF-B
/// writer (floor(UV) = (building-grid column, row), UV2 = (window base, channel slot + 32 × total columns), spare byte =
/// window card), and placed with <see cref="OpeningPlacement.Frame"/>; the sink only merges the cells into bundles under
/// the zone's glass material. The base is the building's allocation from the map's WindowNumberingSnapshot; without one
/// (legacy and no-window fixtures, BuildContext) the glass carries no UV or UV2 at all, never a made-up base.
/// </summary>
public sealed class GlassSink(ToonSpec spec, PaletteRegistry? palette, int? windowBase, int columns, ChannelId channel)
{
    private const int MaxVertices = 65000;

    private readonly List<float> _position = [], _normal = [], _uv = [], _uv2 = [];
    private readonly List<byte> _card = [];
    private readonly List<int> _index = [];
    private readonly List<GeometryBundle> _done = [];

    /// <summary>Total columns of the building's window grid.</summary>
    public int Columns { get; } = columns;

    /// <summary>
    /// One window cell: a vertical rectangle a (bottom left seen from outside), b (bottom right), c (top right), d (top
    /// left) facing the horizontal <paramref name="outward"/>, lighting as window (column, row).
    /// </summary>
    public void Pane(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int column, int row, byte card)
    {
        var frame = OpeningPlacement.Frame((a + b) * 0.5f, outward);
        // The cell's columns run along the part's +X; a pane listed the other way round would mirror its card.
        if (Vector3.Dot(b - a, new Vector3(frame.M11, frame.M12, frame.M13)) <= 0f)
            throw new ArgumentException("Pane corners a → b must run along the opening frame's +X (to the viewer's left).");
        Append(Cell(Vector3.Distance(a, b), Vector3.Distance(a, d), column, row, card), frame);
    }

    /// <summary>
    /// A triangle of one cell (skylight facets): the cell's encoding from <see cref="OpeningGlass"/>, its corners at the
    /// cell positions (0.2, 0.2), (0.8, 0.2) and (0.5, 0.8).
    /// </summary>
    public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int column, int row, byte card)
    {
        var cell = Cell(1f, 1f, column, row, card);
        if (_position.Count / 3 + 3 > MaxVertices) Flush();
        var i = _position.Count / 3;
        Vertex(a, outward, Lerp(cell, 0.2f, 0.2f), Uv2(cell), card);
        Vertex(b, outward, Lerp(cell, 0.8f, 0.2f), Uv2(cell), card);
        Vertex(c, outward, Lerp(cell, 0.5f, 0.8f), Uv2(cell), card);
        Tri(i, i + 1, i + 2, outward);
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        Flush();
        return _done;
    }

    private GeometryBundle Cell(float width, float height, int column, int row, byte card) =>
        OpeningGlass.Opening(width, height, windowBase ?? 0, column, row, Columns, channel, card);

    /// <summary>A cell bundle placed by <paramref name="frame"/> (a rigid row-vector placement keeps the winding).</summary>
    private void Append(GeometryBundle cell, Matrix4x4 frame)
    {
        var count = cell.Position.Length / 3;
        if (_position.Count / 3 + count > MaxVertices) Flush();
        var first = _position.Count / 3;
        for (var k = 0; k < count; k++)
        {
            var p = Vector3.Transform(new(cell.Position[3 * k], cell.Position[3 * k + 1], cell.Position[3 * k + 2]), frame);
            var n = Vector3.TransformNormal(new(cell.Normal[3 * k], cell.Normal[3 * k + 1], cell.Normal[3 * k + 2]), frame);
            Vertex(p, n, new(cell.Uv![2 * k], cell.Uv[2 * k + 1]), new(cell.Uv2![2 * k], cell.Uv2[2 * k + 1]), cell.Reserved![k]);
        }
        foreach (var index in cell.Index) _index.Add(first + index);
    }

    /// <summary>UV at fractional cell position (s, t), from the encoded cell's bottom-left and top-right corners.</summary>
    private static Vector2 Lerp(GeometryBundle cell, float s, float t)
    {
        float u0 = float.MaxValue, v0 = float.MaxValue, u1 = float.MinValue, v1 = float.MinValue;
        for (var k = 0; k < cell.Uv!.Length; k += 2)
        {
            u0 = MathF.Min(u0, cell.Uv[k]); u1 = MathF.Max(u1, cell.Uv[k]);
            v0 = MathF.Min(v0, cell.Uv[k + 1]); v1 = MathF.Max(v1, cell.Uv[k + 1]);
        }
        return new(u0 + (u1 - u0) * s, v0 + (v1 - v0) * t);
    }

    private static Vector2 Uv2(GeometryBundle cell) => new(cell.Uv2![0], cell.Uv2[1]);

    private void Vertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 uv2, byte card)
    {
        _position.Add(p.X); _position.Add(p.Y); _position.Add(p.Z);
        _normal.Add(n.X); _normal.Add(n.Y); _normal.Add(n.Z);
        _uv.Add(uv.X); _uv.Add(uv.Y);
        _uv2.Add(uv2.X); _uv2.Add(uv2.Y);
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

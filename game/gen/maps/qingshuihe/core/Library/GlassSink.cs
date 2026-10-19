using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Openings;

namespace Campus.Gen.Maps.Qingshuihe.Core.Library;

/// <summary>
/// World-space glass of one tile and one glass material, shared by every building of the tile (16d-2): each building
/// writes its cells through its own <see cref="GlassSink"/>, and the batch merges them into as few bundles as the vertex
/// limit allows, so a second glazed building in a cell costs no extra draw call. Every cell keeps its own building's
/// VF-B encoding per vertex (UV, UV2, spare byte), so merging changes no window. Numbered and unnumbered glass never mix.
/// </summary>
public sealed class GlassBatch(ToonSpec spec, PaletteCatalog palette, bool numbered)
{
    private const int MaxVertices = 65000;

    private readonly List<float> _position = [], _normal = [], _uv = [], _uv2 = [];
    private readonly List<byte> _card = [];
    private readonly List<int> _index = [];
    private readonly List<GeometryBundle> _done = [];

    public ToonSpec Spec { get; } = spec;

    /// <summary>The batch's glass carries window numbering (UV, UV2); fixtures without a snapshot carry neither.</summary>
    public bool Numbered { get; } = numbered;

    public IReadOnlyList<GeometryBundle> Finish()
    {
        Flush();
        return _done;
    }

    /// <summary>A cell bundle placed by <paramref name="frame"/> (a rigid row-vector placement keeps the winding).</summary>
    internal void Append(GeometryBundle cell, Matrix4x4 frame)
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

    /// <summary>One triangle with its own UVs, wound to face <paramref name="outward"/>.</summary>
    internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Vector2 uva, Vector2 uvb, Vector2 uvc, Vector2 uv2, byte card)
    {
        if (_position.Count / 3 + 3 > MaxVertices) Flush();
        var i = _position.Count / 3;
        Vertex(a, outward, uva, uv2, card);
        Vertex(b, outward, uvb, uv2, card);
        Vertex(c, outward, uvc, uv2, card);
        int ib = i + 1, ic = i + 2;
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) > 0f) (ib, ic) = (ic, ib);
        _index.Add(i); _index.Add(ib); _index.Add(ic);
    }

    private void Vertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 uv2, byte card)
    {
        _position.Add(p.X); _position.Add(p.Y); _position.Add(p.Z);
        _normal.Add(n.X); _normal.Add(n.Y); _normal.Add(n.Z);
        _uv.Add(uv.X); _uv.Add(uv.Y);
        _uv2.Add(uv2.X); _uv2.Add(uv2.Y);
        _card.Add(card);
    }

    private void Flush()
    {
        if (_index.Count == 0) return;
        var count = _position.Count / 3;
        var colour = palette.Resolve(Spec.Lit).Value;
        var bundle = new GeometryBundle(new ToonRef(Spec), [.. _position], [.. _normal], [.. _index], Uv: Numbered ? [.. _uv] : null, Uv2: Numbered ? [.. _uv2] : null)
        {
            ColorIndices = Enumerable.Repeat(colour, count).ToArray(),
            Flags = new byte[count],
            Reserved = [.. _card],
        };
        bundle.ValidateVertexData();
        _done.Add(bundle);
        _position.Clear(); _normal.Clear(); _uv.Clear(); _uv2.Clear(); _card.Clear(); _index.Clear();
    }
}

/// <summary>
/// One building's glass writer into its tile's <see cref="GlassBatch"/>. Every cell is encoded by WP-10's
/// <see cref="OpeningGlass"/>, the one VF-B writer (floor(UV) = (building-grid column, row), UV2 = (window base, channel
/// slot + 32 × total columns), spare byte = window card), and placed with <see cref="OpeningPlacement.Frame"/>; the batch
/// only merges the cells into bundles under the glass material. The base is the building's allocation from the map's
/// WindowNumberingSnapshot; without one (legacy and no-window fixtures, BuildContext) the glass carries no UV or UV2 at
/// all, never a made-up base.
/// </summary>
public sealed class GlassSink
{
    private readonly int? _windowBase;
    private readonly ChannelId _channel;

    public GlassSink(GlassBatch batch, int? windowBase, int columns, ChannelId channel)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Numbered != windowBase is not null)
            throw new InvalidOperationException("Numbered and unnumbered glass cannot share a batch.");
        Batch = batch;
        _windowBase = windowBase;
        Columns = columns;
        _channel = channel;
    }

    /// <summary>The batch this sink writes into.</summary>
    public GlassBatch Batch { get; }

    /// <summary>Total columns of the building's window grid.</summary>
    public int Columns { get; }

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
        Batch.Append(Cell(Vector3.Distance(a, b), Vector3.Distance(a, d), column, row, card), frame);
    }

    /// <summary>
    /// A triangle of one cell (skylight facets): the cell's encoding from <see cref="OpeningGlass"/>, its corners at the
    /// cell positions (0.2, 0.2), (0.8, 0.2) and (0.5, 0.8).
    /// </summary>
    public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int column, int row, byte card)
    {
        var cell = Cell(1f, 1f, column, row, card);
        Batch.Triangle(a, b, c, outward, Lerp(cell, 0.2f, 0.2f), Lerp(cell, 0.8f, 0.2f), Lerp(cell, 0.5f, 0.8f), Uv2(cell), card);
    }

    private GeometryBundle Cell(float width, float height, int column, int row, byte card) =>
        OpeningGlass.Opening(width, height, _windowBase ?? 0, column, row, Columns, _channel, card);

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
}

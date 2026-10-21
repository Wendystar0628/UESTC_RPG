using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
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
    private readonly GeometrySink _sink = new(palette);
    public ToonSpec Spec { get; } = spec;

    /// <summary>Only numbered windows carry UV and UV2; fixtures omit both channels.</summary>
    public bool Numbered { get; } = numbered;

    public IReadOnlyList<GeometryBundle> Finish() => _sink.Finish()
        .Select(mesh => Numbered ? mesh : mesh with { Uv = null, Uv2 = null }).ToArray();

    internal void Append(GeometryBundle cell, Matrix4x4 frame) =>
        _sink.Append(cell with { Material = new ToonRef(Spec) }, frame);

    internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward,
        Vector2 uva, Vector2 uvb, Vector2 uvc, Vector2 uv2, byte card)
    {
        var color = _sink.ColorIndex(Spec.Lit);
        _sink.Tri(Spec, new(a, outward, uva, uv2, ColorIndex: color, Reserved: card),
            new(b, outward, uvb, uv2, ColorIndex: color, Reserved: card),
            new(c, outward, uvc, uv2, ColorIndex: color, Reserved: card));
    }
}

/// <summary>
/// One building's glass writer into its tile's <see cref="GlassBatch"/>. Every cell is encoded by game/gen/parts/openings/'s
/// <see cref="OpeningGlass"/>, the one VF-B writer (floor(UV) = (building-grid column, row), UV2 = (window base, channel
/// slot + 32 × total columns), spare byte = window card), and placed with <see cref="OpeningPlacement.Frame"/>; the batch
/// only merges the cells into bundles under the glass material. The base is the building's allocation from the map's
/// WindowNumberingSnapshot; explicitly unnumbered glass carries no UV or UV2 and no allocated base.
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

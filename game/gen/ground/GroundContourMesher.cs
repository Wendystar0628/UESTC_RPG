using Campus.Contracts;
using static Campus.Gen.Ground.GroundContourPolygon;

namespace Campus.Gen.Ground;

/// <summary>Clips selected leaf outlines against the active distance-field contours.</summary>
internal sealed class GroundContourMesher
{
    private readonly TileField _f;
    private readonly GroundSurfaceProfile _surfaces;
    private readonly GroundTilePartition _partition;
    private readonly GroundGeometryEmitter _emitter;

    public GroundContourMesher(TileField field, GroundSurfaceProfile surfaces,
        GroundTilePartition partition, GroundGeometryEmitter emitter)
    {
        _f = field;
        _surfaces = surfaces;
        _partition = partition;
        _emitter = emitter;
    }

    public void Run()
    {
        var leaves = _partition.Prepare();
        foreach (var (level, i, j, code) in leaves)
        {
            var size = 1 << level;
            if (code >= 0) _emitter.Block(i * size, j * size, size, code);
            else if (code == GroundTilePartition.MacroLeaf) Macro(i * size, j * size, size);
            else Cell(i * size, j * size);
        }
    }

    /// <summary>
    /// Clips an affine block as its two triangles, split along the anti-diagonal 10-01. Their outlines keep every lattice
    /// vertex used by neighbouring leaves, so crossings on shared edges are computed from the same two lattice values as
    /// the neighbour's. Both outlines run in the ring order 00, 10, 11, 01 (the winding of the cells and uniform blocks).
    /// </summary>
    private void Macro(int i0, int j0, int size)
    {
        var lower = new List<GroundContourVertex>();   // corners 00, 10, 01: bottom edge, the diagonal, left edge
        var upper = new List<GroundContourVertex>();   // corners 10, 11, 01: right edge, top edge, the diagonal
        void Walk(List<GroundContourVertex> list, int i, int j) { if (_partition.Used[j * _f.S + i]) list.Add(Lattice(i, j)); }
        for (var k = 0; k < size; k++) Walk(lower, i0 + k, j0);
        lower.Add(Lattice(i0 + size, j0));
        lower.Add(Lattice(i0, j0 + size));
        for (var k = 1; k < size; k++) Walk(lower, i0, j0 + size - k);
        upper.Add(Lattice(i0 + size, j0));
        for (var k = 1; k < size; k++) Walk(upper, i0 + size, j0 + k);
        upper.Add(Lattice(i0 + size, j0 + size));
        for (var k = 1; k < size; k++) Walk(upper, i0 + size - k, j0 + size);
        upper.Add(Lattice(i0, j0 + size));
        Polygon(lower, Varying(lower));
        Polygon(upper, Varying(upper));
    }

    private int Varying(List<GroundContourVertex> poly)
    {
        var first = _surfaces.CodeOf(poly[0].Vals);
        var varying = 0;
        foreach (var v in poly) varying |= _surfaces.CodeOf(v.Vals) ^ first;
        return varying;
    }

    // ---------------------------------------------------------------- boundary cells

    private void Cell(int i, int j)
    {
        var c00 = Lattice(i, j);
        var c10 = Lattice(i + 1, j);
        var c11 = Lattice(i + 1, j + 1);
        var c01 = Lattice(i, j + 1);
        // Anti-diagonal 10-01: triangles 00-10-01 and 10-11-01, each with the codes that differ from its first corner.
        Triangle(c00, c10, c01, _partition.Codes[j * _f.S + i] ^ _partition.Codes[j * _f.S + i + 1] | _partition.Codes[j * _f.S + i] ^ _partition.Codes[(j + 1) * _f.S + i]);
        Triangle(c10, c11, c01, _partition.Codes[j * _f.S + i + 1] ^ _partition.Codes[(j + 1) * _f.S + i + 1] | _partition.Codes[j * _f.S + i + 1] ^ _partition.Codes[(j + 1) * _f.S + i]);
    }

    private GroundContourVertex Lattice(int i, int j)
    {
        var vals = new float[TileField.Count];
        Array.Copy(_f.V, (j * _f.S + i) * TileField.Count, vals, 0, TileField.Count);
        return new GroundContourVertex { X = _f.X(i), Z = _f.Z(j), Vals = vals };
    }

    private void Triangle(GroundContourVertex a, GroundContourVertex b, GroundContourVertex c, int varying) => Polygon([a, b, c], varying);

    private void Polygon(List<GroundContourVertex> corners, int varying)
    {
        var start = new List<GroundContourVertex>(corners.Count);
        foreach (var v in corners) start.Add(Copy(v, -1));
        var pieces = new List<List<GroundContourVertex>> { start };
        for (var t = 0; t < _surfaces.Thresholds.Length; t++)
        {
            if ((varying & _surfaces.Enabled & (1 << t)) == 0) continue;
            var next = new List<List<GroundContourVertex>>(pieces.Count + 2);
            // Every piece is cut by every contour that crosses its triangle, even one that cannot change its surface:
            // sibling pieces must share every vertex on their common edges, or the seam opens a pinhole.
            foreach (var piece in pieces) Split(piece, t, next);
            pieces = next;
        }
        foreach (var piece in pieces) _emitter.EmitPiece(piece);
    }

    private void Split(List<GroundContourVertex> poly, int t, List<List<GroundContourVertex>> output)
    {
        var threshold = _surfaces.Thresholds[t];
        var (field, iso) = threshold;
        var inside = 0;
        foreach (var v in poly) if (threshold.Contains(v.Vals[field])) inside++;
        if (inside == 0 || inside == poly.Count)
        {
            output.Add(poly);
            return;
        }
        var a = new List<GroundContourVertex>();
        var b = new List<GroundContourVertex>();
        for (var k = 0; k < poly.Count; k++)
        {
            var p = poly[k];
            var q = poly[(k + 1) % poly.Count];
            var pin = threshold.Contains(p.Vals[field]);
            var qin = threshold.Contains(q.Vals[field]);
            var target = pin ? a : b;
            target.Add(Copy(p, p.Tag));
            if (pin != qin)
            {
                var x = Crossing(p, q, field, iso);
                target.Add(Copy(x, t));
                (qin ? a : b).Add(Copy(x, p.Tag));
            }
        }
        // A contour that coincides with an earlier one cuts off an exactly empty sliver; keep the piece whole so its
        // edges keep the tag of the contour that really bounds it. Thin but real slivers are kept on both sides.
        var aa = Clean(a);
        var ba = Clean(b);
        if (aa <= 1e-14 || ba <= 1e-14)
        {
            output.Add(poly);
            return;
        }
        output.Add(a);
        output.Add(b);
    }
}

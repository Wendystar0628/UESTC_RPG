using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

internal sealed partial class GroundMesher
{
    private void Block(int i0, int j0, int size, int code)
    {
        var surface = Classify(code);
        if (surface == GroundSurface.Water || CoveredLawn(code, surface)) return;
        var ring = new List<Vector3>();
        void Walk(int i, int j)
        {
            if (_used[j * _f.S + i]) ring.Add(new(_f.X(i), LatticeHeight(surface, i, j), _f.Z(j)));
        }
        for (var k = 0; k < size; k++) Walk(i0 + k, j0);
        for (var k = 0; k < size; k++) Walk(i0 + size, j0 + k);
        for (var k = 0; k < size; k++) Walk(i0 + size - k, j0 + size);
        for (var k = 0; k < size; k++) Walk(i0, j0 + size - k);
        for (var k = 0; k < ring.Count; k++) Skirt(surface, ring[k], ring[(k + 1) % ring.Count]);
        if (ring.Count == 4)
        {
            // Corners in ring order 00, 10, 11, 01: the anti-diagonal 10-01, as the fine cells and the collision height field.
            Top(surface, ring[0], ring[1], ring[3], Up, Up, Up);
            Top(surface, ring[1], ring[2], ring[3], Up, Up, Up);
            return;
        }
        var cx = _f.X(i0) + size * _f.Cell * 0.5f;
        var cz = _f.Z(j0) + size * _f.Cell * 0.5f;
        var cy = 0.25f * (LatticeHeight(surface, i0, j0) + LatticeHeight(surface, i0 + size, j0) +
                          LatticeHeight(surface, i0, j0 + size) + LatticeHeight(surface, i0 + size, j0 + size));
        var centre = new Vector3(cx, cy, cz);
        for (var k = 0; k < ring.Count; k++) Top(surface, centre, ring[k], ring[(k + 1) % ring.Count], Up, Up, Up);
    }

    /// <summary>
    /// Tiles of different levels meet with different vertices (and, on terrain, different heights). Every top edge on
    /// the tile border hangs a short skirt that shades like the surface (normal up, same colour), so a level seam can
    /// never open a gap; the neighbour's skirt covers the view from the other side.
    /// </summary>
    private void Skirt(GroundSurface surface, Vector3 a, Vector3 b)
    {
        if (surface is GroundSurface.Water or GroundSurface.Bevel or GroundSurface.PitBevel) return;
        var x1 = _f.X(_f.N);
        var z1 = _f.Z(_f.N);
        Vector3 outward;
        if (a.X == _f.X0 && b.X == _f.X0) outward = -Vector3.UnitX;
        else if (a.X == x1 && b.X == x1) outward = Vector3.UnitX;
        else if (a.Z == _f.Z0 && b.Z == _f.Z0) outward = -Vector3.UnitZ;
        else if (a.Z == z1 && b.Z == z1) outward = Vector3.UnitZ;
        else return;
        var drop = new Vector3(0f, SkirtDepth, 0f);
        var color = GroundPalette.ColorOf(surface);
        _sink.Triangle(color, VertexFlag.NoShadow, a, b, b - drop, Up, Up, Up, outward, new(surface, GroundFaceRole.Skirt, _sourceContext));
        _sink.Triangle(color, VertexFlag.NoShadow, a, b - drop, a - drop, Up, Up, Up, outward, new(surface, GroundFaceRole.Skirt, _sourceContext));
    }

    private const float SkirtDepth = 0.3f;

    /// <summary>
    /// Emits one top-surface triangle with the D-180 shading rule: level-ground normals except on designed earth
    /// slopes (1:6 and steeper), which get the triangle's own flat normal on their own vertices.
    /// </summary>
    private void Top(GroundSurface surface, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        if (!_grid.IsFlat && surface != GroundSurface.Bevel)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.Y < 0f) n = -n;
            if (n.LengthSquared() > 1e-14f)
            {
                n = Vector3.Normalize(n);
                var slope = MathF.Sqrt(n.X * n.X + n.Z * n.Z) / MathF.Max(n.Y, 1e-6f);
                if (GroundShadingRule.For(surface, slope) == GroundShading.Faceted) na = nb = nc = n;
            }
        }
        _sink.Triangle(GroundPalette.ColorOf(surface), VertexFlag.NoShadow, a, b, c, na, nb, nc, Up,
            new(surface, GroundFaceRole.Top, _sourceContext));
    }

    /// <summary>True when the base height inside the block stays within tolerance of its bilinear corner surface.</summary>
    private bool Planar(int i0, int j0, int size)
    {
        if (_grid.IsFlat) return true;
        var h00 = _f.Get(i0, j0, TileField.H);
        var h10 = _f.Get(i0 + size, j0, TileField.H);
        var h01 = _f.Get(i0, j0 + size, TileField.H);
        var h11 = _f.Get(i0 + size, j0 + size, TileField.H);
        if (MathF.Abs(h00 + h11 - h10 - h01) > PlanarTolerance) return false;
        for (var j = 0; j <= size; j++)
            for (var i = 0; i <= size; i++)
            {
                var u = (float)i / size;
                var w = (float)j / size;
                var plane = h00 + u * (h10 - h00) + w * (h01 - h00);
                if (MathF.Abs(_f.Get(i0 + i, j0 + j, TileField.H) - plane) > PlanarTolerance) return false;
            }
        return true;
    }


    // ---------------------------------------------------------------- affine blocks

    /// <summary>
    /// True when, on each of the block's two triangles, every field whose contour crosses the block is affine within
    /// tolerance (straight edges, gentle curves, lawn tone cells) and the base height is planar. Such a block is clipped
    /// whole instead of cell by cell: a straight kerb through an 8 m block costs a handful of triangles, not hundreds.
    /// </summary>
    private bool AffineLeaf(int i0, int j0, int size)
    {
        var first = _code[j0 * _f.S + i0];
        var varying = 0;
        for (var j = j0; j <= j0 + size; j++)
            for (var i = i0; i <= i0 + size; i++)
                varying |= _code[j * _f.S + i] ^ first;
        varying &= _enabled;
        for (var f = 0; f < TileField.Count; f++)
        {
            var isos = new List<float>(3);
            for (var t = 0; t < _t.Length; t++)
                if ((varying & (1 << t)) != 0 && _t[t].Field == f) isos.Add(_t[t].Iso);
            if (isos.Count > 0 && !Affine(i0, j0, size, f, _fieldTolerance, isos)) return false;
        }
        return _grid.IsFlat || Affine(i0, j0, size, TileField.H, PlanarTolerance, null);
    }


    /// <summary>
    /// The field matches the plane through the half's corners: every lattice point lies on the same side of each
    /// contour as the plane says (so each contour is one straight line per half), and points near a contour lie
    /// within tolerance of the plane. Far from the contours the field may bend (a road's centre-line ridge).
    /// With no contours given, the whole half must be within tolerance (height planarity).
    /// </summary>
    private bool Affine(int i0, int j0, int size, int field, float tolerance, List<float>? isos)
    {
        var near = 2f * _f.Cell;
        var f00 = _f.Get(i0, j0, field);
        var f10 = _f.Get(i0 + size, j0, field);
        var f01 = _f.Get(i0, j0 + size, field);
        var f11 = _f.Get(i0 + size, j0 + size, field);
        for (var j = 0; j <= size; j++)
            for (var i = 0; i <= size; i++)
            {
                var u = (float)i / size;
                var w = (float)j / size;
                var plane = CellSplit.Lerp(f00, f10, f01, f11, u, w);
                var value = _f.Get(i0 + i, j0 + j, field);
                if (isos is null)
                {
                    if (MathF.Abs(value - plane) > tolerance) return false;
                    continue;
                }
                var closest = float.MaxValue;
                foreach (var iso in isos)
                {
                    var threshold = new Threshold(field, iso);
                    if (threshold.Contains(value) != threshold.Contains(plane)) return false;
                    closest = MathF.Min(closest, MathF.Abs(plane - iso));
                }
                if (closest < near && MathF.Abs(value - plane) > tolerance) return false;
            }
        return true;
    }

    /// <summary>
    /// Clips an affine block as its two triangles, split along the anti-diagonal 10-01. Their outlines keep every lattice
    /// vertex used by neighbouring leaves, so crossings on shared edges are computed from the same two lattice values as
    /// the neighbour's. Both outlines run in the ring order 00, 10, 11, 01 (the winding of the cells and uniform blocks).
    /// </summary>
    private void Macro(int i0, int j0, int size)
    {
        var lower = new List<Vert>();   // corners 00, 10, 01: bottom edge, the diagonal, left edge
        var upper = new List<Vert>();   // corners 10, 11, 01: right edge, top edge, the diagonal
        void Walk(List<Vert> list, int i, int j) { if (_used[j * _f.S + i]) list.Add(Lattice(i, j)); }
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

    private int Varying(List<Vert> poly)
    {
        var first = CodeOf(poly[0].Vals);
        var varying = 0;
        foreach (var v in poly) varying |= CodeOf(v.Vals) ^ first;
        return varying;
    }

    // ---------------------------------------------------------------- boundary cells
}

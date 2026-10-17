using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Turns the tile's distance fields into ground geometry.
/// <list type="bullet">
/// <item>Each lattice cell is split along its anti-diagonal, (1,0)-(0,1) (<see cref="CellSplit"/>, the diagonal of the
/// collision height field); fields are linear on each triangle, so clipping a triangle by "field &lt; iso" is exact and
/// neighbouring regions share their boundary vertices exactly.</item>
/// <item>Cells whose four corners resolve to the same surface merge into quadtree blocks. A block's outline keeps
/// every lattice vertex used by its neighbours, so there are no T-junction cracks.</item>
/// <item>Where a contour separates two surfaces of different height, the higher side emits a vertical face
/// (kerb face, lawn step, bank). Face and bevel normals follow the exact distance gradient, so curves shade smoothly.</item>
/// </list>
/// </summary>
internal sealed class GroundMesher
{
    private readonly record struct Threshold(int Field, float Iso);

    private sealed class Vert
    {
        public required double X { get; init; }
        public required double Z { get; init; }
        public required float[] Vals { get; init; }
        public int Tag { get; set; } = -1;   // threshold that created the edge leaving this vertex; -1 = cell edge
    }

    private const int TWater = 0, TRoad = 1, TBevel = 2, TCurb = 3, TCar = 4, TPaved = 5, TEdge = 6,
        TTrack = 7, TTurf = 8, TCourt = 9, TPlaza = 10, TPlazaBorder = 11, TLawnEdge = 12, TLawnLow = 13, TLawnHigh = 14,
        TPit = 15, TPitTop = 16, TPitCoreBevel = 17, TPitCore = 18;

    private static readonly Vector3 Up = Vector3.UnitY;

    private readonly TileField _f;
    private readonly GroundSection _s;
    private readonly GroundLayout _layout;
    private readonly MeshSink _sink;
    private readonly GroundSupportBuilder? _support;
    private readonly GroundSourceContext _sourceContext;
    private readonly bool _bevel;
    private readonly bool _bands;
    private readonly bool _pits;
    private readonly Threshold[] _t;
    private readonly int _enabled;
    private readonly int _maxAffineLevel;
    private readonly float _grassY;
    private readonly float _pavingY;
    private readonly float _fieldTolerance;
    private const int MacroLeaf = -2;
    private readonly int[] _code;
    private readonly bool[] _used;
    private readonly Dictionary<(float, float, int), Vector2> _gradients = new();

    /// <summary>Uniform blocks larger than one cell merge only where the ground stays within this of a plane.</summary>
    private const float PlanarTolerance = 0.005f;

    private readonly TerrainGrid _grid;

    public GroundMesher(TileField field, GroundSection section, LodLevel lod, GroundLayout layout, TerrainGrid grid, MeshSink sink, GroundSupportBuilder? support = null)
    {
        _f = field;
        _s = section;
        _layout = layout;
        _grid = grid;
        _sink = sink;
        _support = support;
        _bevel = GroundLod.HasBevel(lod);
        // Steps per level: L0 full cross-section, L1 lawns flush with paving, L2 and coarser one flat surface.
        var steps = GroundLod.Steps(lod);
        _pavingY = steps == 0 ? section.RoadY : section.PavingY;
        _grassY = steps == 2 ? section.GrassY : _pavingY;
        _fieldTolerance = 0.03f * field.Cell;
        _sourceContext = new(field.Sources, section, _fieldTolerance, layout.Paving);
        _bands = GroundLod.HasBands(lod);
        _pits = _bands;   // the frame and the soil are resolved where kerbs are (L0, L1); beyond that a pit is under a pixel
        _t =
        [
            new(TileField.W, 0f),
            new(TileField.A, 0f),
            new(TileField.A, _bevel ? section.CurbBevel : 0f),
            new(TileField.A, _bands ? section.CurbWidth : 0f),
            new(TileField.Car, 0f),
            new(TileField.P, 0f),
            new(TileField.P, _bands ? -section.EdgingWidth : 0f),
            new(TileField.Track, 0f),
            new(TileField.Turf, 0f),
            new(TileField.Court, 0f),
            new(TileField.Plaza, 0f),
            new(TileField.Plaza, _bands ? -section.PlazaBorder : 0f),
            new(TileField.LawnEdge, section.Lawn.EdgeBand),
            new(TileField.Tone, section.Lawn.Low),
            new(TileField.Tone, section.Lawn.High),
            new(TileField.Pit, 0f),
            new(TileField.Pit, _bevel ? -section.CurbBevel : 0f),
            new(TileField.Core, _bevel ? section.CurbBevel : 0f),
            new(TileField.Core, 0f)
        ];
        // Disabled thresholds are never split on: a duplicate contour would steal the face tag of the real one.
        _enabled = (1 << _t.Length) - 1;
        if (!_bevel) _enabled &= ~(1 << TBevel);
        if (!_bands) _enabled &= ~((1 << TCurb) | (1 << TEdge) | (1 << TPlazaBorder));
        if (!_pits) _enabled &= ~((1 << TPit) | (1 << TPitTop) | (1 << TPitCoreBevel) | (1 << TPitCore));
        if (!_bevel) _enabled &= ~((1 << TPitTop) | (1 << TPitCoreBevel));
        if (!(section.Lawn.EdgeBand > 0f)) _enabled &= ~(1 << TLawnEdge);
        if (field.ToneMacro == 0) _enabled &= ~((1 << TLawnEdge) | (1 << TLawnLow) | (1 << TLawnHigh));
        if (lod != LodLevel.L0) _enabled &= ~(1 << TLawnEdge);
        // Affine blocks up to 16 m on a side.
        var levels = 0;
        while ((1 << levels) < field.N) levels++;
        _maxAffineLevel = 0;
        while (_maxAffineLevel < levels && (2 << _maxAffineLevel) * field.Cell <= (lod <= LodLevel.L1 ? 16f : 32f)) _maxAffineLevel++;
        _code = new int[field.S * field.S];
        _used = new bool[field.S * field.S];
        for (var j = 0; j < field.S; j++)
            for (var i = 0; i < field.S; i++)
            {
                var k = j * field.S + i;
                _code[k] = CodeAt(field.V, k * TileField.Count);
            }
    }

    public void Run()
    {
        var n = _f.N;
        var levels = 0;
        while ((1 << levels) < n) levels++;
        // Uniform code per block at each level, or -1.
        var blocks = new int[levels + 1][];
        blocks[0] = new int[n * n];
        for (var j = 0; j < n; j++)
            for (var i = 0; i < n; i++)
            {
                var c = _code[j * _f.S + i];
                var uniform = c == _code[j * _f.S + i + 1] && c == _code[(j + 1) * _f.S + i] && c == _code[(j + 1) * _f.S + i + 1];
                blocks[0][j * n + i] = uniform && Classify(c) is not (GroundSurface.Bevel or GroundSurface.PitBevel) ? c : -1;
            }
        for (var l = 1; l <= levels; l++)
        {
            var m = n >> l;
            var child = blocks[l - 1];
            var cm = m * 2;
            blocks[l] = new int[m * m];
            for (var j = 0; j < m; j++)
                for (var i = 0; i < m; i++)
                {
                    var a = child[(2 * j) * cm + 2 * i];
                    var ok = a >= 0 && a == child[(2 * j) * cm + 2 * i + 1] &&
                             a == child[(2 * j + 1) * cm + 2 * i] && a == child[(2 * j + 1) * cm + 2 * i + 1];
                    ok = ok && Planar(i << l, j << l, 1 << l);
                    blocks[l][j * m + i] = ok ? a : -1;
                }
        }
        var leaves = new List<(int Level, int I, int J, int Code)>();
        Collect(blocks, levels, 0, 0, leaves);
        // Wherever a contour crosses the tile border, both endpoints of that lattice segment are outline vertices in
        // both neighbouring tiles, so the crossing is computed from the same two values on both sides of the seam.
        for (var k = 0; k < n; k++)
        {
            MarkCrossing(k, 0, k + 1, 0);
            MarkCrossing(k, n, k + 1, n);
            MarkCrossing(0, k, 0, k + 1);
            MarkCrossing(n, k, n, k + 1);
        }
        foreach (var (level, i, j, _) in leaves)
        {
            var size = 1 << level;
            Mark(i * size, j * size);
            Mark(i * size + size, j * size);
            Mark(i * size, j * size + size);
            Mark(i * size + size, j * size + size);
        }
        foreach (var (level, i, j, code) in leaves)
        {
            var size = 1 << level;
            if (code >= 0) Block(i * size, j * size, size, code);
            else if (code == MacroLeaf) Macro(i * size, j * size, size);
            else Cell(i * size, j * size);
        }
    }

    private void Collect(int[][] blocks, int level, int i, int j, List<(int, int, int, int)> leaves)
    {
        var m = _f.N >> level;
        var code = blocks[level][j * m + i];
        if (code >= 0 || level == 0)
        {
            leaves.Add((level, i, j, code));
            return;
        }
        if (level <= _maxAffineLevel && AffineLeaf(i << level, j << level, 1 << level))
        {
            leaves.Add((level, i, j, MacroLeaf));
            return;
        }
        for (var dj = 0; dj < 2; dj++)
            for (var di = 0; di < 2; di++)
                Collect(blocks, level - 1, 2 * i + di, 2 * j + dj, leaves);
    }

    private void Mark(int i, int j) => _used[j * _f.S + i] = true;

    private void MarkCrossing(int i0, int j0, int i1, int j1)
    {
        if (((_code[j0 * _f.S + i0] ^ _code[j1 * _f.S + i1]) & _enabled) == 0) return;
        Mark(i0, j0);
        Mark(i1, j1);
    }

    // ---------------------------------------------------------------- uniform blocks

    private void Block(int i0, int j0, int size, int code)
    {
        var surface = Classify(code);
        if (surface == GroundSurface.Water) return;
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
        _sink.Triangle(color, GroundPalette.FlagNoShadow, a, b, b - drop, Up, Up, Up, outward, new(surface, GroundFaceRole.Skirt, _sourceContext));
        _sink.Triangle(color, GroundPalette.FlagNoShadow, a, b - drop, a - drop, Up, Up, Up, outward, new(surface, GroundFaceRole.Skirt, _sourceContext));
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
        _support?.Add(surface, a, b, c);
        _sink.Triangle(GroundPalette.ColorOf(surface), GroundPalette.FlagNoShadow, a, b, c, na, nb, nc, Up,
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
                    if ((value < iso) != (plane < iso)) return false;
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

    private void Cell(int i, int j)
    {
        var c00 = Lattice(i, j);
        var c10 = Lattice(i + 1, j);
        var c11 = Lattice(i + 1, j + 1);
        var c01 = Lattice(i, j + 1);
        // Anti-diagonal 10-01: triangles 00-10-01 and 10-11-01, each with the codes that differ from its first corner.
        Triangle(c00, c10, c01, _code[j * _f.S + i] ^ _code[j * _f.S + i + 1] | _code[j * _f.S + i] ^ _code[(j + 1) * _f.S + i]);
        Triangle(c10, c11, c01, _code[j * _f.S + i + 1] ^ _code[(j + 1) * _f.S + i + 1] | _code[j * _f.S + i + 1] ^ _code[(j + 1) * _f.S + i]);
    }

    private Vert Lattice(int i, int j)
    {
        var vals = new float[TileField.Count];
        Array.Copy(_f.V, (j * _f.S + i) * TileField.Count, vals, 0, TileField.Count);
        return new Vert { X = _f.X(i), Z = _f.Z(j), Vals = vals };
    }

    private void Triangle(Vert a, Vert b, Vert c, int varying) => Polygon([a, b, c], varying);

    private void Polygon(List<Vert> corners, int varying)
    {
        var start = new List<Vert>(corners.Count);
        foreach (var v in corners) start.Add(Copy(v, -1));
        var pieces = new List<List<Vert>> { start };
        for (var t = 0; t < _t.Length; t++)
        {
            if ((varying & _enabled & (1 << t)) == 0) continue;
            var next = new List<List<Vert>>(pieces.Count + 2);
            // Every piece is cut by every contour that crosses its triangle, even one that cannot change its surface:
            // sibling pieces must share every vertex on their common edges, or the seam opens a pinhole.
            foreach (var piece in pieces) Split(piece, t, next);
            pieces = next;
        }
        foreach (var piece in pieces) EmitPiece(piece);
    }

    private static Vert Copy(Vert v, int tag) => new() { X = v.X, Z = v.Z, Vals = v.Vals, Tag = tag };

    private void Split(List<Vert> poly, int t, List<List<Vert>> output)
    {
        var (field, iso) = _t[t];
        var inside = 0;
        foreach (var v in poly) if (v.Vals[field] < iso) inside++;
        if (inside == 0 || inside == poly.Count)
        {
            output.Add(poly);
            return;
        }
        var a = new List<Vert>();
        var b = new List<Vert>();
        for (var k = 0; k < poly.Count; k++)
        {
            var p = poly[k];
            var q = poly[(k + 1) % poly.Count];
            var pin = p.Vals[field] < iso;
            var qin = q.Vals[field] < iso;
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

    /// <summary>Removes zero-length edges (keeping the later tag); returns the remaining absolute (doubled) area.</summary>
    private static double Clean(List<Vert> poly)
    {
        for (var k = poly.Count - 1; k >= 0 && poly.Count > 1; k--)
        {
            var next = poly[(k + 1) % poly.Count];
            if (poly[k].X == next.X && poly[k].Z == next.Z)
            {
                next.Tag = next.Tag >= 0 ? next.Tag : poly[k].Tag;
                poly.RemoveAt(k);
            }
        }
        if (poly.Count < 3) return 0d;
        return Math.Abs(Area(poly));
    }

    /// <summary>True when three consecutive outline vertices are collinear (a lattice point or crossing mid-edge).</summary>
    private static bool HasCollinear(List<Vert> poly)
    {
        for (var k = 0; k < poly.Count; k++)
        {
            var a = poly[(k + poly.Count - 1) % poly.Count];
            var b = poly[k];
            var c = poly[(k + 1) % poly.Count];
            var cross = (b.X - a.X) * (c.Z - b.Z) - (b.Z - a.Z) * (c.X - b.X);
            if (Math.Abs(cross) < 1e-12) return true;
        }
        return false;
    }

    private static double Area(List<Vert> poly)
    {
        double area = 0;
        for (var k = 0; k < poly.Count; k++)
        {
            var p = poly[k];
            var q = poly[(k + 1) % poly.Count];
            area += p.X * q.Z - q.X * p.Z;
        }
        return area;
    }

    /// <summary>
    /// Crossing on edge pq, computed in a canonical order so both triangles sharing the edge agree exactly.
    /// Fields that coincide with the cut field on this edge (A = Car where no parking is near, P = Plaza where only a
    /// plaza is near) are set to the same exact value, so a later cut on them produces an exactly empty sliver.
    /// </summary>
    private static Vert Crossing(Vert p, Vert q, int field, float iso)
    {
        if (q.X < p.X || (q.X == p.X && q.Z < p.Z)) (p, q) = (q, p);
        var t = (iso - (double)p.Vals[field]) / ((double)q.Vals[field] - p.Vals[field]);
        if (t <= 0d) t = 0d;
        if (t >= 1d) t = 1d;
        var vals = new float[TileField.Count];
        for (var f = 0; f < vals.Length; f++)
        {
            if (p.Vals[f] == p.Vals[field] && q.Vals[f] == q.Vals[field]) vals[f] = iso;
            else vals[f] = t == 0d ? p.Vals[f] : t == 1d ? q.Vals[f] : (float)(p.Vals[f] + t * ((double)q.Vals[f] - p.Vals[f]));
        }
        vals[field] = iso;
        var x = t == 0d ? p.X : t == 1d ? q.X : p.X + t * (q.X - p.X);
        var z = t == 0d ? p.Z : t == 1d ? q.Z : p.Z + t * (q.Z - p.Z);
        return new Vert { X = x, Z = z, Vals = vals };
    }

    // ---------------------------------------------------------------- emission

    private static float[] Mean(List<Vert> poly)
    {
        var mean = new float[TileField.Count];
        foreach (var v in poly)
            for (var f = 0; f < mean.Length; f++) mean[f] += v.Vals[f];
        for (var f = 0; f < mean.Length; f++) mean[f] /= poly.Count;
        return mean;
    }

    private void EmitPiece(List<Vert> poly)
    {
        var code = CodeOf(Mean(poly));
        var surface = Classify(code);
        if (surface != GroundSurface.Water)
        {
            var points = new Vector3[poly.Count];
            var normals = new Vector3[poly.Count];
            for (var k = 0; k < poly.Count; k++)
            {
                points[k] = Point(poly[k], surface);
                normals[k] = SurfaceNormal(surface, poly[k]);
            }
            for (var k = 0; k < poly.Count; k++) Skirt(surface, points[k], points[(k + 1) % poly.Count]);
            if (poly.Count == 3 || (_grid.IsFlat && !HasCollinear(poly)))
            {
                for (var k = 1; k + 1 < poly.Count; k++)
                    Top(surface, points[0], points[k], points[k + 1], normals[0], normals[k], normals[k + 1]);
            }
            else
            {
                // Fan from the centroid so that every outline edge is a triangle edge. A fan from a vertex drops the
                // degenerate triangles of collinear outline runs, which leaves T-junctions (pinholes) against the
                // neighbour's vertices, and on sloping ground makes vertical slivers.
                var mean = Mean(poly);
                var c = new Vert { X = poly.Average(v => v.X), Z = poly.Average(v => v.Z), Vals = mean };
                var pc = Point(c, surface);
                var nc = SurfaceNormal(surface, c);
                for (var k = 0; k < poly.Count; k++)
                {
                    var next = (k + 1) % poly.Count;
                    Top(surface, pc, points[k], points[next], nc, normals[k], normals[next]);
                }
            }
        }
        var ccw = Area(poly) > 0;
        for (var k = 0; k < poly.Count; k++)
        {
            var u = poly[k];
            if (u.Tag < 0) continue;
            var w = poly[(k + 1) % poly.Count];
            // Outward edge normal in the xz plane (positive shoelace area: interior on the left of u -> w in x-z axes).
            var ex = (float)(w.X - u.X);
            var ez = (float)(w.Z - u.Z);
            var outward = ccw ? new Vector3(ez, 0f, -ex) : new Vector3(-ez, 0f, ex);
            if (outward.LengthSquared() < 1e-14f) continue;
            Face(u, w, u.Tag, code, surface, Vector3.Normalize(outward));
        }
    }

    /// <summary>Emits the vertical face along a contour edge when this side is higher than the other.</summary>
    private void Face(Vert u, Vert w, int t, int code, GroundSurface surface, Vector3 outward)
    {
        if (surface == GroundSurface.Water) return;
        var mid = new float[TileField.Count];
        for (var f = 0; f < mid.Length; f++) mid[f] = 0.5f * (u.Vals[f] + w.Vals[f]);
        var other = (CodeOf(mid) & ~(1 << t)) | (~code & (1 << t));
        var neighbour = Classify(other);
        var hu = HeightOf(surface, u.Vals);
        var hw = HeightOf(surface, w.Vals);
        var lu = HeightOf(neighbour, u.Vals);
        var lw = HeightOf(neighbour, w.Vals);
        if (hu - lu < 1e-4f || hw - lw < 1e-4f) return;
        var field = _t[t].Field;
        // Outward from this (higher) side: up the field gradient when this side is inside (field below iso).
        var sign = (code & (1 << t)) != 0 ? 1f : -1f;
        var nu = FaceNormal(u, field, sign, outward);
        var nw = FaceNormal(w, field, sign, outward);
        // Water banks are stone revetment whatever lies above them; other steps take the upper surface's face colour.
        var color = neighbour == GroundSurface.Water ? GroundColor.Curb : GroundPalette.FaceColorOf(surface);
        var pu0 = new Vector3((float)u.X, lu, (float)u.Z);
        var pu1 = new Vector3((float)u.X, hu, (float)u.Z);
        var pw0 = new Vector3((float)w.X, lw, (float)w.Z);
        var pw1 = new Vector3((float)w.X, hw, (float)w.Z);
        _sink.Triangle(color, GroundPalette.FlagNoShadow, pu0, pw0, pw1, nu, nw, nw, outward, new(surface, GroundFaceRole.Vertical, _sourceContext));
        _sink.Triangle(color, GroundPalette.FlagNoShadow, pu0, pw1, pu1, nu, nw, nu, outward, new(surface, GroundFaceRole.Vertical, _sourceContext));
    }

    private Vector3 Point(Vert v, GroundSurface surface) =>
        new((float)v.X, HeightOf(surface, v.Vals), (float)v.Z);

    /// <summary>Height of the surface at the lattice point (i, j).</summary>
    private float LatticeHeight(GroundSurface surface, int i, int j) => HeightOf(surface, _f.V, (j * _f.S + i) * TileField.Count);

    /// <summary>
    /// Base height plus the surface's offset in the street cross-section, from the field values that start at
    /// <paramref name="offset"/> in <paramref name="v"/> (a lattice row or one vertex). A pit's frame and soil take their
    /// heights from the pit's own values; the chamfers slope 45 degrees over the bevel width from the frame top down to
    /// its edge, outside and inside.
    /// </summary>
    private float HeightOf(GroundSurface surface, float[] v, int offset = 0)
    {
        var h = v[offset + TileField.H];
        var bevel = _s.CurbBevel;
        return h + surface switch
        {
            GroundSurface.Water => _s.BankBottomY,
            GroundSurface.Asphalt or GroundSurface.Parking => _s.RoadY,
            GroundSurface.Bevel => _s.PavingY - bevel + Math.Clamp(v[offset + TileField.A], 0f, bevel),
            GroundSurface.Curb or GroundSurface.Edging or GroundSurface.Track or GroundSurface.Turf or GroundSurface.Court or
                GroundSurface.Granite or GroundSurface.PlazaBorder or GroundSurface.Paver => _pavingY,
            GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark => _grassY,
            GroundSurface.PitRim => v[offset + TileField.PitRim],
            GroundSurface.PitSoil => v[offset + TileField.PitSoil],
            GroundSurface.PitBevel => v[offset + TileField.PitRim] - bevel +
                MathF.Min(Math.Clamp(-v[offset + TileField.Pit], 0f, bevel), Math.Clamp(v[offset + TileField.Core], 0f, bevel)),
            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
        };
    }

    private Vector3 SurfaceNormal(GroundSurface surface, Vert v) => surface switch
    {
        GroundSurface.Bevel => BevelNormal(v),
        GroundSurface.PitBevel => PitBevelNormal(v),
        _ => Up
    };

    /// <summary>Normal of a pit frame's chamfer: tilted outward on the outer top edge, toward the soil on the inner one.</summary>
    private Vector3 PitBevelNormal(Vert v)
    {
        var g = Gradient(v, TileField.Pit);
        if (g == Vector2.Zero) return Up;
        var inner = v.Vals[TileField.Core] < -v.Vals[TileField.Pit];
        var sign = inner ? -1f : 1f;
        return Vector3.Normalize(Up + new Vector3(sign * g.X, 0f, sign * g.Y));
    }

    private Vector3 BevelNormal(Vert v)
    {
        var g = Gradient(v, TileField.A);
        if (g == Vector2.Zero) return Up;
        return Vector3.Normalize(Up + new Vector3(-g.X, 0f, -g.Y));
    }

    /// <summary>Smooth face normal from the field gradient; falls back to the edge normal where the gradient disagrees.</summary>
    private Vector3 FaceNormal(Vert v, int field, float sign, Vector3 outward)
    {
        var g = Gradient(v, field) * sign;
        var n = new Vector3(g.X, 0f, g.Y);
        return Vector3.Dot(n, outward) > 0.5f ? n : outward;
    }

    /// <summary>Unit horizontal gradient of a field at the vertex, from exact point queries (seam consistent).</summary>
    private Vector2 Gradient(Vert v, int field)
    {
        var x = (float)v.X;
        var z = (float)v.Z;
        if (_gradients.TryGetValue((x, z, field), out var cached)) return cached;
        const float h = 0.02f;
        var limit = _f.Limit;
        var p = new Vector2(x, z);
        var shapes = _layout.Near(p - new Vector2(limit + h), p + new Vector2(limit + h));
        var gx = Value(field, shapes, new(x + h, z), limit) - Value(field, shapes, new(x - h, z), limit);
        var gz = Value(field, shapes, new(x, z + h), limit) - Value(field, shapes, new(x, z - h), limit);
        var g = new Vector2(gx, gz);
        g = g.LengthSquared() > 1e-12f ? Vector2.Normalize(g) : Vector2.Zero;
        _gradients.Add((x, z, field), g);
        return g;
    }

    private static float Value(int field, List<GroundShape> shapes, Vector2 p, float limit)
    {
        float Of(SurfaceKind kind)
        {
            var best = limit;
            foreach (var shape in shapes)
                if (shape.Kind == kind) best = MathF.Min(best, Sdf.Shape(shape, p, limit));
            return best;
        }
        return field switch
        {
            TileField.W => Of(SurfaceKind.Water),
            TileField.A => MathF.Min(Of(SurfaceKind.Carriageway), Of(SurfaceKind.Parking)),
            TileField.Car => Of(SurfaceKind.Carriageway),
            TileField.P => MathF.Min(MathF.Min(MathF.Min(Of(SurfaceKind.Track), Of(SurfaceKind.Turf)),
                MathF.Min(Of(SurfaceKind.Court), Of(SurfaceKind.Plaza))), Of(SurfaceKind.Walk)),
            TileField.Track => Of(SurfaceKind.Track),
            TileField.Turf => Of(SurfaceKind.Turf),
            TileField.Court => Of(SurfaceKind.Court),
            TileField.Plaza => Of(SurfaceKind.Plaza),
            // Core is Pit plus the frame thickness of the nearest pit: the same gradient.
            TileField.Pit or TileField.Core => Of(SurfaceKind.Pit),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field.")
        };
    }

    // ---------------------------------------------------------------- classification

    private int CodeAt(float[] values, int offset)
    {
        var code = 0;
        for (var t = 0; t < _t.Length; t++)
            if ((_enabled & (1 << t)) != 0 && values[offset + _t[t].Field] < _t[t].Iso) code |= 1 << t;
        return code;
    }

    private int CodeOf(float[] values) => CodeAt(values, 0);

    /// <summary>Priority: water, carriageway level, kerb bevel and kerb, paved surfaces (edging band first), lawn.</summary>
    public GroundSurface Classify(int code)
    {
        bool Has(int t) => (code & (1 << t)) != 0;
        if (Has(TWater)) return GroundSurface.Water;
        if (Has(TRoad)) return Has(TCar) ? GroundSurface.Asphalt : GroundSurface.Parking;
        if (_bevel && Has(TBevel)) return GroundSurface.Bevel;
        if (_bands && Has(TCurb)) return GroundSurface.Curb;
        if (_pits && Has(TPit))
        {
            // A tree pit or planting strip is carved out of whatever lies round it: nothing is paved or sown inside it.
            // Inside the outer face: a chamfer on the frame's outer top edge, the flat frame top, a chamfer on its inner
            // top edge, then the sunken soil.
            if (Has(TPitCore)) return GroundSurface.PitSoil;
            if (_bevel && (Has(TPitCoreBevel) || !Has(TPitTop))) return GroundSurface.PitBevel;
            return GroundSurface.PitRim;
        }
        if (!Has(TPaved))
        {
            if (Has(TLawnEdge) || Has(TLawnLow)) return GroundSurface.GrassDark;
            if ((_enabled & (1 << TLawnHigh)) == 0) return GroundSurface.Grass;
            return Has(TLawnHigh) ? GroundSurface.Grass : GroundSurface.GrassLight;
        }
        if (_bands && !Has(TEdge)) return GroundSurface.Edging;
        if (Has(TTrack)) return GroundSurface.Track;
        if (Has(TTurf)) return GroundSurface.Turf;
        if (Has(TCourt)) return GroundSurface.Court;
        if (!Has(TPlaza)) return GroundSurface.Paver;
        return _bands && !Has(TPlazaBorder) ? GroundSurface.PlazaBorder : GroundSurface.Granite;
    }

    /// <summary>Surface at a lattice point (used by the footstep and mask grids).</summary>
    public GroundSurface SurfaceAt(int i, int j) => Classify(_code[j * _f.S + i]);

    public float HeightAt(int i, int j) => LatticeHeight(SurfaceAt(i, j), i, j);

}

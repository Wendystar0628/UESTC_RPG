using Campus.Contracts;
using static Campus.Gen.Ground.GroundSurfaceProfile;

namespace Campus.Gen.Ground;

/// <summary>Selects uniform/affine leaves and records their shared lattice outline vertices.</summary>
internal sealed class GroundTilePartition
{
    private readonly TileField _f;

    private readonly TerrainGrid _grid;

    private readonly int _maxAffineLevel;

    private readonly float _fieldTolerance;

    internal const int MacroLeaf = -2;

    private readonly int[] _code;

    private readonly bool[] _used;

    /// <summary>Uniform blocks larger than one cell merge only where the ground stays within this of a plane.</summary>
    private const float PlanarTolerance = 0.005f;

    private readonly GroundSurfaceProfile _surfaces;

    public GroundTilePartition(TileField field, LodLevel lod, TerrainGrid grid, GroundSurfaceProfile surfaces)
    {
        _f = field;
        _grid = grid;
        _surfaces = surfaces;
        _fieldTolerance = 0.03f * field.Cell;
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
                _code[k] = _surfaces.CodeAt(field.V, k * TileField.Count);
            }
    }

    public int[] Codes => _code;
    public bool[] Used => _used;
    public float FieldTolerance => _fieldTolerance;

    public List<(int Level, int I, int J, int Code)> Prepare()
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
                blocks[0][j * n + i] = uniform && _surfaces.Classify(c) is not (GroundSurface.Bevel or GroundSurface.PitBevel) ? c : -1;
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
        return leaves;
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
        if (((_code[j0 * _f.S + i0] ^ _code[j1 * _f.S + i1]) & _surfaces.Enabled) == 0) return;
        Mark(i0, j0);
        Mark(i1, j1);
    }

    // ---------------------------------------------------------------- uniform blocks

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
        varying &= _surfaces.Enabled;
        for (var f = 0; f < TileField.Count; f++)
        {
            var isos = new List<float>(3);
            for (var t = 0; t < _surfaces.Thresholds.Length; t++)
                if ((varying & (1 << t)) != 0 && _surfaces.Thresholds[t].Field == f) isos.Add(_surfaces.Thresholds[t].Iso);
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
}

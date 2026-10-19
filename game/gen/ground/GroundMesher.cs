using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Turns the tile's distance fields into ground geometry.
/// <list type="bullet">
/// <item>Each lattice cell is split along its anti-diagonal, (1,0)-(0,1) (<see cref="CellSplit"/>, the diagonal of the
/// collision height field); fields are linear on each triangle, so clipping a triangle at a field threshold is exact and
/// neighbouring regions share their boundary vertices exactly.</item>
/// <item>Cells whose four corners resolve to the same surface merge into quadtree blocks. A block's outline keeps
/// every lattice vertex used by its neighbours, so there are no T-junction cracks.</item>
/// <item>Where a contour separates two surfaces of different height, the higher side emits a vertical face
/// (kerb face, lawn step, bank). Face and bevel normals follow the exact distance gradient, so curves shade smoothly.</item>
/// </list>
/// </summary>
internal sealed partial class GroundMesher
{
    private readonly record struct Threshold(int Field, float Iso)
    {
        // A building owns its boundary; its zero-valued corner samples must not classify an interior triangle as lawn.
        public bool Contains(float value) => Field == TileField.Building ? value <= Iso : value < Iso;
    }

    private sealed class Vert
    {
        public required double X { get; init; }
        public required double Z { get; init; }
        public required float[] Vals { get; init; }
        public int Tag { get; set; } = -1;   // threshold that created the edge leaving this vertex; -1 = cell edge
    }

    private const int TWater = 0, TRoad = 1, TBevel = 2, TCurb = 3, TCar = 4, TPaved = 5, TEdge = 6,
        TTrack = 7, TTurf = 8, TCourt = 9, TPlaza = 10, TPlazaBorder = 11, TLawnEdge = 12, TLawnLow = 13, TLawnHigh = 14,
        TPit = 15, TPitTop = 16, TPitCoreBevel = 17, TPitCore = 18, TBuilding = 19;

    private static readonly Vector3 Up = Vector3.UnitY;

    private readonly TileField _f;
    private readonly GroundSection _s;
    private readonly GroundLayout _layout;
    private readonly MeshSink _sink;
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

    public GroundMesher(TileField field, GroundSection section, LodLevel lod, GroundLayout layout, TerrainGrid grid, MeshSink sink)
    {
        _f = field;
        _s = section;
        _layout = layout;
        _grid = grid;
        _sink = sink;
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
            new(TileField.Core, 0f),
            new(TileField.Building, 0f)
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
}

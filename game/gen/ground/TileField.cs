using Campus.Contracts;
using System.Numerics;
using Campus.Gen.Geometry;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>
/// Signed distance fields sampled on the tile lattice (negative inside, clamped to ±<see cref="Limit"/>).
/// Lattice points on a tile edge are computed identically by both neighbouring tiles, so seams match bit for bit.
/// </summary>
internal sealed class TileField
{
    // Field slots per lattice point.
    // Tone: lawn tone noise in [0, 1]; LawnEdge: distance from the lawn to the nearest kerb, paving or building foot;
    // Building: footprint distance, with courtyard holes, retained for clipping lawn beneath buildings.
    // H: base height of the ground (designed terrain with flattened carriageways), not a threshold field.
    // Pit: distance to the outer face of the nearest tree pit's frame; Core: the same distance plus that pit's frame thickness, so
    // its zero contour is the frame's inner face; PitRim, PitSoil: that pit's frame top and soil top (heights, like H, not thresholds).
    public const int W = 0, A = 1, Car = 2, P = 3, Track = 4, Turf = 5, Court = 6, Plaza = 7, Tone = 8, LawnEdge = 9, H = 10,
        Pit = 11, Core = 12, PitRim = 13, PitSoil = 14, Building = 15, Count = 16;

    public int N { get; }
    public int S { get; }
    public float Cell { get; }
    public float X0 { get; }
    public float Z0 { get; }
    public float Limit { get; }
    public float[] V { get; }
    /// <summary>The same tile-local source set used by rasterisation; shared with the mesher without per-face copies.</summary>
    public IReadOnlyList<GroundShape> Sources { get; private set; } = [];
    /// <summary>Fine cells per tone cell (a power of two), or 0 when this level has no lawn tones.</summary>
    public int ToneMacro { get; private set; }

    private TileField(int n, float cell, float x0, float z0, float limit)
    {
        N = n;
        S = n + 1;
        Cell = cell;
        X0 = x0;
        Z0 = z0;
        Limit = limit;
        V = new float[S * S * Count];
    }

    public float X(int i) => X0 + i * Cell;
    public float Z(int j) => Z0 + j * Cell;
    public float Get(int i, int j, int field) => V[(j * S + i) * Count + field];

    /// <summary>
    /// Samples all fields of one tile. The lawn tone field is sampled every <paramref name="toneCell"/> metres and is
    /// linear on that coarser lattice's triangles (same anti-diagonal, <see cref="CellSplit"/>), so tone contours can be resolved per tone cell.
    /// </summary>
    public static TileField Build(GroundLayout layout, GroundTile tile, GroundSection section, TerrainGrid terrain,
        uint seed = 0u, float toneCell = 0f)
    {
        var n = tile.Cells;
        var cell = tile.Cell;
        if (n < 2 || (n & (n - 1)) != 0) throw new ArgumentOutOfRangeException(nameof(tile), tile, "Cell count must be a power of two.");
        // Exact up to the largest threshold plus three cells, so contours and their gradients are never clamped.
        var limit = section.MaxIso + 3f * cell + 0.05f;
        var field = new TileField(n, cell, tile.X0, tile.Z0, limit);
        var count = field.S * field.S;
        var kinds = new float[10][];
        for (var k = 0; k < kinds.Length; k++)
        {
            kinds[k] = new float[count];
            Array.Fill(kinds[k], limit);
        }
        var lo = new Vector2(field.X0 - limit, field.Z0 - limit);
        var hi = new Vector2(field.X0 + n * cell + limit, field.Z0 + n * cell + limit);
        // Tree pits: the nearest pit at each lattice point decides its frame thickness and heights, so a pit's values
        // are constant wherever its contours run and linear interpolation across a cell stays exact.
        var core = new float[count];
        var rim = new float[count];
        var soil = new float[count];
        var nearest = new double[count];
        Array.Fill(core, limit);
        Array.Fill(nearest, double.MaxValue);
        field.Sources = layout.Near(lo, hi);
        foreach (var shape in field.Sources)
        {
            if (shape.Kind != SurfaceKind.Pit)
            {
                field.Raster(kinds[(int)shape.Kind], shape, null);
                continue;
            }
            var (rimTop, soilTop, kerb) = shape.PitHeights(section);
            field.Raster(kinds[(int)SurfaceKind.Pit], shape, (k, d) =>
            {
                if (d >= nearest[k]) return;
                nearest[k] = d;
                core[k] = Math.Clamp(d + kerb, -limit, limit);
                rim[k] = rimTop;
                soil[k] = soilTop;
            });
        }

        var v = field.V;
        for (var k = 0; k < count; k++)
        {
            var o = k * Count;
            var car = kinds[(int)SurfaceKind.Carriageway][k];
            var park = kinds[(int)SurfaceKind.Parking][k];
            var track = kinds[(int)SurfaceKind.Track][k];
            var turf = kinds[(int)SurfaceKind.Turf][k];
            var court = kinds[(int)SurfaceKind.Court][k];
            var plaza = kinds[(int)SurfaceKind.Plaza][k];
            var walk = kinds[(int)SurfaceKind.Walk][k];
            v[o + W] = kinds[(int)SurfaceKind.Water][k];
            v[o + A] = MathF.Min(car, park);
            v[o + Car] = car;
            v[o + P] = MathF.Min(MathF.Min(MathF.Min(track, turf), MathF.Min(court, plaza)), walk);
            v[o + Track] = track;
            v[o + Turf] = turf;
            v[o + Court] = court;
            v[o + Plaza] = plaza;
            v[o + Pit] = kinds[(int)SurfaceKind.Pit][k];
            v[o + Core] = core[k];
            v[o + PitRim] = rim[k];
            v[o + PitSoil] = soil[k];
            v[o + Building] = kinds[(int)SurfaceKind.Building][k];
            v[o + LawnEdge] = MathF.Min(MathF.Min(v[o + P], v[o + A] - section.CurbWidth), v[o + Building]);
        }
        for (var j = 0; j < field.S; j++)
            for (var i = 0; i < field.S; i++)
                v[(j * field.S + i) * Count + H] = terrain.At(field.X(i), field.Z(j));
        field.ToneMacro = toneCell > 0f ? Math.Clamp((int)MathF.Round(toneCell / cell), 1, n) : 0;
        var m = field.ToneMacro;
        if (m == 0)
        {
            for (var k = 0; k < count; k++) v[k * Count + Tone] = 0.5f;
        }
        else
        {
            var lawn = section.Lawn;
            for (var j = 0; j < field.S; j += m)
                for (var i = 0; i < field.S; i += m)
                    v[(j * field.S + i) * Count + Tone] = LawnNoise.At(field.X(i), field.Z(j), seed, lawn);
            for (var j = 0; j < field.S; j++)
                for (var i = 0; i < field.S; i++)
                {
                    if (i % m == 0 && j % m == 0) continue;
                    var i0 = Math.Min(i / m * m, field.S - 1 - m);
                    var j0 = Math.Min(j / m * m, field.S - 1 - m);
                    var u = (float)(i - i0) / m;
                    var w = (float)(j - j0) / m;
                    float T(int a, int b) => v[((j0 + b * m) * field.S + i0 + a * m) * Count + Tone];
                    v[(j * field.S + i) * Count + Tone] = CellSplit.Lerp(T(0, 0), T(1, 0), T(0, 1), T(1, 1), u, w);
                }
        }
        // Building coverage keeps exact boundary zeros: treating its aligned corner as outside can leave a lawn triangle
        // entirely inside the footprint. It uses closed-set coverage, unlike the height-step contours below.
        // No height-step contour may pass (almost) exactly through a lattice point: a contour lying on a lattice line would never be
        // cut, its step face would be lost and its pieces would leave pinholes. Values within a micrometre of a
        // threshold move ten micrometres outside. Same rule on both sides of a seam, so tiles still agree bit for bit.
        float[] isos = [0f, section.CurbBevel, -section.CurbBevel, section.CurbWidth, -section.EdgingWidth, -section.PlazaBorder,
            section.Lawn.EdgeBand, section.Lawn.Low, section.Lawn.High];
        for (var k = 0; k < v.Length; k++)
        {
            if (k % Count is H or PitRim or PitSoil or Building) continue;
            foreach (var iso in isos)
                if (MathF.Abs(v[k] - iso) < 1e-6f) v[k] = iso + 1e-5f;
        }
        return field;
    }

    /// <summary>Lattice index range [lo, hi] covering coordinates in [a, b], clipped to the lattice.</summary>
    private (int Lo, int Hi) Range(float origin, float a, float b)
    {
        var lo = (int)MathF.Ceiling((a - origin) / Cell);
        var hi = (int)MathF.Floor((b - origin) / Cell);
        return (Math.Max(lo, 0), Math.Min(hi, S - 1));
    }

    private void Raster(float[] grid, GroundShape shape, Action<int, float>? visit)
    {
        var r = Limit + shape.HalfWidth;
        var (i0, i1) = Range(X0, shape.Min.X - Limit, shape.Max.X + Limit);
        var (j0, j1) = Range(Z0, shape.Min.Y - Limit, shape.Max.Y + Limit);
        if (i0 > i1 || j0 > j1) return;
        var w = i1 - i0 + 1;
        var dist = new double[w * (j1 - j0 + 1)];
        Array.Fill(dist, r);
        if (shape.Line is { } line)
        {
            for (var s = 0; s + 1 < line.Length; s++) Edge(dist, i0, i1, j0, j1, line[s], line[s + 1], r);
            for (var j = j0; j <= j1; j++)
                for (var i = i0; i <= i1; i++)
                {
                    var k = j * S + i;
                    var d = Math.Clamp((float)(dist[(j - j0) * w + (i - i0)] - shape.HalfWidth), -Limit, Limit);
                    visit?.Invoke(k, d);
                    if (d < grid[k]) grid[k] = d;
                }
            return;
        }
        var rings = shape.Rings!;
        foreach (var ring in rings)
            for (var e = 0; e < ring.Length; e++) Edge(dist, i0, i1, j0, j1, ring[e], ring[(e + 1) % ring.Length], r);
        var crossings = new List<float>();
        for (var j = j0; j <= j1; j++)
        {
            var z = Z(j);
            crossings.Clear();
            foreach (var ring in rings)
                for (var e = 0; e < ring.Length; e++)
                {
                    var a = ring[e];
                    var b = ring[(e + 1) % ring.Length];
                    if ((a.Y <= z) != (b.Y <= z)) crossings.Add(CrossX(a, b, z));
                }
            crossings.Sort();
            var c = 0;
            for (var i = i0; i <= i1; i++)
            {
                var x = X(i);
                while (c < crossings.Count && crossings[c] < x) c++;
                var inside = (c & 1) == 1;
                var raw = (float)dist[(j - j0) * w + (i - i0)];
                var d = Math.Clamp(inside ? -raw : raw, -Limit, Limit);
                var k = j * S + i;
                visit?.Invoke(k, d);
                if (d < grid[k]) grid[k] = d;
            }
        }
    }

    /// <summary>Same expression as <see cref="Sdf.Shape"/> so that the lattice agrees with point queries.</summary>
    public static float CrossX(Vector2 a, Vector2 b, float z) => a.X + (z - a.Y) * (b.X - a.X) / (b.Y - a.Y);

    /// <summary>Lowers the local distance grid inside the capsule of radius r around segment ab.</summary>
    private void Edge(double[] dist, int i0, int i1, int j0, int j1, Vector2 a, Vector2 b, double r)
    {
        var w = i1 - i0 + 1;
        var fr = (float)r;
        var (r0, r1) = Range(Z0, MathF.Min(a.Y, b.Y) - fr, MathF.Max(a.Y, b.Y) + fr);
        r0 = Math.Max(r0, j0);
        r1 = Math.Min(r1, j1);
        var dz = b.Y - a.Y;
        for (var j = r0; j <= r1; j++)
        {
            var z = Z(j);
            float xlo, xhi;
            if (MathF.Abs(dz) < 1e-6f)
            {
                xlo = MathF.Min(a.X, b.X);
                xhi = MathF.Max(a.X, b.X);
            }
            else
            {
                var t0 = (z - fr - a.Y) / dz;
                var t1 = (z + fr - a.Y) / dz;
                if (t0 > t1) (t0, t1) = (t1, t0);
                t0 = Math.Clamp(t0, 0f, 1f);
                t1 = Math.Clamp(t1, 0f, 1f);
                var xa = a.X + t0 * (b.X - a.X);
                var xb = a.X + t1 * (b.X - a.X);
                xlo = MathF.Min(xa, xb);
                xhi = MathF.Max(xa, xb);
            }
            var (c0, c1) = Range(X0, xlo - fr, xhi + fr);
            c0 = Math.Max(c0, i0);
            c1 = Math.Min(c1, i1);
            var row = (j - j0) * w - i0;
            for (var i = c0; i <= c1; i++)
            {
                var d = PlanLines.SegmentDistance(X(i), z, a, b);
                if (d < dist[row + i]) dist[row + i] = d;
            }
        }
    }
}

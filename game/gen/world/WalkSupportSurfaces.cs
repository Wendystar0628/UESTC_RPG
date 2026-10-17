using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Pure index of actual collider tops, independent of walk permission. No content generation or mesh sampling.</summary>
public sealed class WalkSupportSurfaces : IWalkSupportSource
{
    private readonly Dictionary<TileKey, Face[]> _cells;
    private readonly float _cellSize;

    public WalkSupportSurfaces(IEnumerable<Collider> supports, float cellSize = 64f)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
        _cellSize = cellSize;
        var cells = new Dictionary<TileKey, List<Face>>();
        foreach (var collider in supports)
        {
            if (collider.Tag is not (SurfaceTag.Ground or SurfaceTag.Stair)) continue;
            Face? face = collider switch
            {
                RampCollider ramp => new([ramp.P0, ramp.P1, ramp.P2, ramp.P3], null),
                SlabCollider slab => new(null, slab.Ring.ToArray(), slab.Y),
                BoxCollider box => BoxTop(box),
                HeightFieldCollider field => new(field),
                _ => null
            };
            if (face is null) continue;
            for (var z = Cell(face.Min.Y); z <= Cell(face.Max.Y); z++)
                for (var x = Cell(face.Min.X); x <= Cell(face.Max.X); x++)
                {
                    var key = new TileKey(x, z);
                    if (!cells.TryGetValue(key, out var list)) cells.Add(key, list = []);
                    list.Add(face);
                }
        }
        _cells = cells.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }

    public float? SupportY(float x, float z, float yHint) => TrySupport(new(x, z), yHint, out var y, out _) ? y : null;

    public bool TrySupport(Vector2 point, float yHint, out float y, out float slopeSquared)
    {
        y = 0f; slopeSquared = 0f;
        if (!_cells.TryGetValue(new(Cell(point.X), Cell(point.Y)), out var faces)) return false;
        var found = false;
        foreach (var face in faces)
        {
            if (!face.TrySample(point, out var height, out var slope)) continue;
            if (found && (float.IsFinite(yHint)
                ? MathF.Abs(height - yHint) > MathF.Abs(y - yHint) || (MathF.Abs(height - yHint) == MathF.Abs(y - yHint) && height <= y)
                : height <= y)) continue;
            found = true; y = height; slopeSquared = slope;
        }
        return found;
    }

    private static Face BoxTop(BoxCollider box)
    {
        Vector3 At(float x, float z) => Vector3.Transform(new(x, box.Half.Y, z), box.At);
        return new([At(-box.Half.X, -box.Half.Z), At(box.Half.X, -box.Half.Z),
            At(box.Half.X, box.Half.Z), At(-box.Half.X, box.Half.Z)], null);
    }

    private int Cell(float coordinate) => (int)MathF.Floor(coordinate / _cellSize);

    /// <summary>The same anti-diagonal interpolation as the actual 65 x 65 Jolt height field; selected holes remain absent.</summary>
    public static float SampleHeight(HeightFieldCollider collider, float x, float z)
    {
        var lx = x - collider.Tile.X * TerrainSampling.TileQuads;
        var lz = z - collider.Tile.Z * TerrainSampling.TileQuads;
        var i = Math.Clamp((int)MathF.Floor(lx), 0, TerrainSampling.TileQuads - 1);
        var j = Math.Clamp((int)MathF.Floor(lz), 0, TerrainSampling.TileQuads - 1);
        var u = Math.Clamp(lx - i, 0f, 1f);
        var v = Math.Clamp(lz - j, 0f, 1f);
        float H(int a, int b) => TerrainSampling.DecodeHeight(collider.HeightsCm[(j + b) * TerrainSampling.TileSamples + i + a]);
        return u + v <= 1f
            ? H(0, 0) + u * (H(1, 0) - H(0, 0)) + v * (H(0, 1) - H(0, 0))
            : H(1, 1) + (1f - v) * (H(1, 0) - H(1, 1)) + (1f - u) * (H(0, 1) - H(1, 1));
    }

    private sealed class Face
    {
        private readonly Vector3[]? _quad;
        private readonly Vector2[]? _flat;
        private readonly HeightFieldCollider? _field;
        private readonly float _y;
        internal Vector2 Min { get; }
        internal Vector2 Max { get; }

        internal Face(Vector3[]? quad, Vector2[]? flat, float y = 0f)
        {
            _quad = quad; _flat = flat; _y = y;
            var points = flat ?? quad!.Select(p => new Vector2(p.X, p.Z)).ToArray();
            Min = points.Aggregate(Vector2.Min); Max = points.Aggregate(Vector2.Max);
        }

        internal Face(HeightFieldCollider field)
        {
            _field = field with { HeightsCm = field.HeightsCm.ToArray() };
            Min = new(field.Tile.X * TerrainSampling.TileQuads, field.Tile.Z * TerrainSampling.TileQuads);
            Max = Min + new Vector2(TerrainSampling.TileQuads);
        }

        internal bool TrySample(Vector2 point, out float y, out float slope)
        {
            y = _y; slope = 0f;
            if (point.X < Min.X - .0001f || point.X > Max.X + .0001f || point.Y < Min.Y - .0001f || point.Y > Max.Y + .0001f) return false;
            if (_field is { } field)
            {
                y = SampleHeight(field, point.X, point.Y);
                if (!float.IsFinite(y)) return false;
                var i = Math.Clamp((int)MathF.Floor(point.X - Min.X), 0, TerrainSampling.TileQuads - 1);
                var j = Math.Clamp((int)MathF.Floor(point.Y - Min.Y), 0, TerrainSampling.TileQuads - 1);
                float H(int a, int b) => TerrainSampling.DecodeHeight(field.HeightsCm[(j + b) * TerrainSampling.TileSamples + i + a]);
                var lower = point.X - Min.X - i + point.Y - Min.Y - j <= 1f;
                var dx = lower ? H(1, 0) - H(0, 0) : H(1, 1) - H(0, 1);
                var dz = lower ? H(0, 1) - H(0, 0) : H(1, 1) - H(1, 0);
                slope = dx * dx + dz * dz;
                return true;
            }
            if (_flat is { } flat) return PlanGeometry.PointInRing(point, flat);
            var quad = _quad!;
            return Triangle(quad[0], quad[1], quad[2], point, out y, out slope)
                || Triangle(quad[0], quad[2], quad[3], point, out y, out slope);
        }

        private static bool Triangle(Vector3 a, Vector3 b, Vector3 c, Vector2 point, out float y, out float slope)
        {
            y = 0f; slope = 0f;
            var ab = new Vector2(b.X - a.X, b.Z - a.Z);
            var ac = new Vector2(c.X - a.X, c.Z - a.Z);
            var ap = point - new Vector2(a.X, a.Z);
            var determinant = (double)ab.X * ac.Y - (double)ab.Y * ac.X;
            if (Math.Abs(determinant) < 1e-10) return false;
            var u = ((double)ap.X * ac.Y - (double)ap.Y * ac.X) / determinant;
            var v = ((double)ab.X * ap.Y - (double)ab.Y * ap.X) / determinant;
            if (u < -.00001 || v < -.00001 || u + v > 1.00001) return false;
            y = (float)(a.Y + u * (b.Y - a.Y) + v * (c.Y - a.Y));
            if (!float.IsFinite(y)) throw new InvalidOperationException("Collision-top support must be finite.");
            var dx = ((b.Y - a.Y) * ac.Y - (c.Y - a.Y) * ab.Y) / determinant;
            var dz = (ab.X * (c.Y - a.Y) - ac.X * (b.Y - a.Y)) / determinant;
            slope = (float)(dx * dx + dz * dz);
            return true;
        }
    }
}

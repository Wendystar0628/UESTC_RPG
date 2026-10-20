using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>One tile-binned, ordered decal stream shared by roads, pitches and ground details.</summary>
internal sealed class GroundMarkings
{
    /// <summary><paramref name="Level"/> is the height of the surface the quad lies on above the carriageway level.</summary>
    public readonly record struct Quad(Vector2 A, Vector2 B, Vector2 C, Vector2 D, GroundColor Color, float Level = 0f);

    private readonly Dictionary<long, List<Quad>> _byTile = new();

    public int Count { get; private set; }

    internal List<Vector2> Crossings { get; } = [];

    /// <summary>Quads whose centre lies in the 64 m base cell (x, z).</summary>
    public IReadOnlyList<Quad> In(int x, int z) =>
        _byTile.TryGetValue(Key(x, z), out var list) ? list : [];

    /// <summary>
    /// Emits every quad whose 64 m base cell lies inside the tile (tiles are multiples of 64 m).
    /// Lines are draped in pieces of at most 1 m so they follow the terrain grid (D-180).
    /// </summary>
    public void Emit(GroundTile tile, GroundSurfaceSink sink, GroundSection section, TerrainGrid terrain)
    {
        const byte flags = VertexFlag.Decal | VertexFlag.NoShadow;
        var first = TileGrid.At(new Vector2(tile.X0, tile.Z0));
        var x0 = first.X;
        var z0 = first.Z;
        var span = Math.Max(1, (int)MathF.Round(tile.Size / TileGrid.BaseEdge));
        for (var z = z0; z < z0 + span; z++)
            for (var x = x0; x < x0 + span; x++)
                foreach (var q in In(x, z))
                {
                    var lift = section.RoadY + section.DecalLift + q.Level;
                    // Lie on the (possibly sloping) road: split long strips into pieces of at most 1 m.
                    var pieces = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(q.A, q.B)));
                    for (var k = 0; k < pieces; k++)
                    {
                        var t0 = (float)k / pieces;
                        var t1 = (float)(k + 1) / pieces;
                        Vector3 P(Vector2 from, Vector2 to, float t)
                        {
                            var p = from + (to - from) * t;
                            return new(p.X, terrain.At(p.X, p.Y) + lift, p.Y);
                        }
                        sink.Quad(q.Color, flags, P(q.A, q.B, t0), P(q.A, q.B, t1), P(q.D, q.C, t1), P(q.D, q.C, t0), Vector3.UnitY);
                    }
                }
    }

    internal void AddStrip(Vector2 p0, Vector2 p1, float width, GroundColor color, float level = 0f)
    {
        var dir = p1 - p0;
        if (dir.LengthSquared() < 1e-6f) return;
        dir = Vector2.Normalize(dir);
        var side = new Vector2(-dir.Y, dir.X) * (width * 0.5f);
        var quad = new Quad(p0 - side, p1 - side, p1 + side, p0 + side, color, level);
        var centre = (p0 + p1) * 0.5f;
        AddQuad(quad, centre);
    }

    internal void AddQuad(Quad quad, Vector2 centre)
    {
        var cell = TileGrid.At(centre);
        var key = Key(cell.X, cell.Z);
        if (!_byTile.TryGetValue(key, out var list)) _byTile.Add(key, list = []);
        list.Add(quad);
        Count++;
    }

    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

    /// <summary>
    /// Points of the arc of the given radius around local (cu, cv), from angle <paramref name="from"/> to
    /// <paramref name="to"/>; (<paramref name="ku"/>, <paramref name="kv"/>) mirror the cosine and sine directions.
    /// </summary>
    internal static List<Vector2> Arc(float cu, float cv, float radius, float from, float to, float ku, float kv, float chord)
    {
        var steps = Math.Max(4, (int)MathF.Ceiling(MathF.Abs(to - from) * radius / chord));
        var points = new List<Vector2>(steps + 1);
        for (var k = 0; k <= steps; k++)
        {
            var angle = from + (to - from) * k / steps;
            points.Add(new(cu + ku * radius * MathF.Cos(angle), cv + kv * radius * MathF.Sin(angle)));
        }
        return points;
    }
}

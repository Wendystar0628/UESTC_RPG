using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>
/// Dashed centre lines and zebra crossings derived from the road network (WLD-AMBIENCE 6). Markings are flat decal
/// quads lying on the asphalt; each quad belongs to the tile that contains its centre, so tiles never duplicate one.
/// Every quad is checked against the exact carriageway field, so mismatched centre lines never paint over a kerb.
/// Football pitches get their lines from the same quads (<see cref="AddPitches"/>), on the turf instead of the asphalt.
/// </summary>
internal sealed partial class RoadMarkings
{
    /// <summary><paramref name="Level"/> is the height of the surface the quad lies on above the carriageway level.</summary>
    public readonly record struct Quad(Vector2 A, Vector2 B, Vector2 C, Vector2 D, GroundColor Color, float Level = 0f);

    private readonly Dictionary<long, List<Quad>> _byTile = new();

    public int Count { get; private set; }
    public IReadOnlyList<Vector2> Crossings => _crossings;
    private readonly List<Vector2> _crossings = [];

    /// <summary>Quads whose centre lies in the 64 m base cell (x, z).</summary>
    public IReadOnlyList<Quad> In(int x, int z) =>
        _byTile.TryGetValue(Key(x, z), out var list) ? list : [];

    /// <summary>Emits every quad whose 64 m base cell lies inside the tile (tiles are multiples of 64 m).</summary>
    /// <summary>Lines are draped in pieces of at most 1 m so they follow the terrain grid (D-180).</summary>
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

    public static RoadMarkings Build(GroundLayout layout, GroundSection section)
    {
        var result = new RoadMarkings();
        var spec = section.Markings;
        var cars = layout.Roads.Where(r => r.Carriageway).ToList();
        var walks = layout.Roads.Where(r => !r.Carriageway && r.Type != "pedestrian" && r.Type != "steps").ToList();
        var junctions = RoadGraph.Junctions(cars);

        // Crossings at junction arms.
        var zebras = new List<(Vector2 Centre, Vector2 Along, float HalfWidth, float Length)>();
        foreach (var node in junctions)
        {
            foreach (var arm in node.Arms)
            {
                if (arm.Road.Width < spec.ZebraJunctionMinWidth) continue;
                var d = node.Radius + spec.ZebraSetback + spec.ZebraLength * 0.5f;
                var (centre, dir) = RoadGraph.Walk(arm.Road.Points, arm.Vertex, arm.Forward, d);
                if (centre is not { } c) continue;
                zebras.Add((c, dir, arm.Road.Width * 0.5f, spec.ZebraLength));
            }
        }

        // Crossings where a footpath crosses a carriageway away from junctions.
        foreach (var walk in walks)
            foreach (var road in cars)
                for (var i = 0; i + 1 < walk.Points.Length; i++)
                    for (var k = 0; k + 1 < road.Points.Length; k++)
                    {
                        if (PlanLines.SegmentIntersection(walk.Points[i], walk.Points[i + 1], road.Points[k], road.Points[k + 1], 1e-6f) is not { } x) continue;
                        if (NearJunction(junctions, x, spec.ZebraLength + spec.ZebraSetback)) continue;
                        var along = Vector2.Normalize(road.Points[k + 1] - road.Points[k]);
                        zebras.Add((x, along, road.Width * 0.5f, MathF.Max(spec.ZebraLength, walk.Width + 1f)));
                    }

        var placed = new List<Vector2>();
        foreach (var (centre, along, half, length) in zebras)
        {
            if (placed.Any(p => Vector2.DistanceSquared(p, centre) < 36f)) continue;
            if (!OnAsphalt(layout, centre, 1f)) continue;
            placed.Add(centre);
            result._crossings.Add(centre);
            result.AddZebra(layout, centre, along, half, length, spec);
        }
        // Centre lines, kept clear of junctions and crossings.
        foreach (var road in cars)
        {
            if (road.Width < spec.CentreLineMinWidth) continue;
            var period = spec.DashLength + spec.DashGap;
            var length = PlanLines.Length(road.Points);
            var start = spec.DashGap * 0.5f;
            for (var s = start; s + spec.DashLength <= length; s += period)
            {
                var p0 = PlanLines.Sample(road.Points, s).Point;
                var p1 = PlanLines.Sample(road.Points, s + spec.DashLength).Point;
                var mid = (p0 + p1) * 0.5f;
                if (NearJunction(junctions, mid, spec.ZebraSetback + spec.ZebraLength + 1.5f)) continue;
                if (placed.Any(p => Vector2.DistanceSquared(p, mid) < 25f)) continue;   // keep clear of mid-block crossings
                if (!OnAsphalt(layout, mid, 0.8f) || !OnAsphalt(layout, p0, 0.5f) || !OnAsphalt(layout, p1, 0.5f)) continue;
                result.AddStrip(p0, p1, spec.CentreLineWidth, GroundColor.Marking);
            }
        }
        result.AddPitches(layout, section);
        result.AddGroundDetails(layout, section, junctions);

        return result;
    }

    private void AddZebra(GroundLayout layout, Vector2 c, Vector2 along, float halfWidth, float length, MarkingSpec spec)
    {
        var across = new Vector2(-along.Y, along.X);
        var usable = halfWidth - spec.ZebraMargin;
        if (usable <= spec.ZebraStripe) return;
        var pitch = spec.ZebraStripe + spec.ZebraGap;
        var n = (int)MathF.Floor((2f * usable + spec.ZebraGap) / pitch);
        if (n < 1) return;
        var first = -0.5f * (n * pitch - spec.ZebraGap) + spec.ZebraStripe * 0.5f;
        for (var k = 0; k < n; k++)
        {
            var o = c + across * (first + k * pitch);
            var a = o - along * (length * 0.5f);
            var b = o + along * (length * 0.5f);
            if (!OnAsphalt(layout, o, spec.ZebraStripe) || !OnAsphalt(layout, a, 0.05f) || !OnAsphalt(layout, b, 0.05f)) continue;
            AddStrip(a, b, spec.ZebraStripe, GroundColor.Marking);
        }
    }

    private void AddStrip(Vector2 p0, Vector2 p1, float width, GroundColor color, float level = 0f)
    {
        var dir = p1 - p0;
        if (dir.LengthSquared() < 1e-6f) return;
        dir = Vector2.Normalize(dir);
        var side = new Vector2(-dir.Y, dir.X) * (width * 0.5f);
        var quad = new Quad(p0 - side, p1 - side, p1 + side, p0 + side, color, level);
        var centre = (p0 + p1) * 0.5f;
        AddQuad(quad, centre);
    }

    private void AddQuad(Quad quad, Vector2 centre)
    {
        var cell = TileGrid.At(centre);
        var key = Key(cell.X, cell.Z);
        if (!_byTile.TryGetValue(key, out var list)) _byTile.Add(key, list = []);
        list.Add(quad);
        Count++;
    }

    private static bool OnAsphalt(GroundLayout layout, Vector2 p, float inset) =>
        layout.SignedDistance(SurfaceKind.Carriageway, p, inset + 1f) < -inset &&
        layout.SignedDistance(SurfaceKind.Water, p, 1f) > 0f;

    // ---------------------------------------------------------------- network topology

    private static bool NearJunction(List<RoadGraph.Junction> junctions, Vector2 p, float extra)
    {
        foreach (var j in junctions)
        {
            var r = j.Radius + extra;
            if (Vector2.DistanceSquared(j.At, p) < r * r) return true;
        }
        return false;
    }


    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
}

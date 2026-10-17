using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Collision;

/// <summary>Pure CPU collision preparation. All coordinates supplied here are world metres.</summary>
public static class CollisionGeometry
{
    public const float TileSize = 64f;
    public const int MaxShapesPerBody = 250;

    public static TileKey TileAt(Vector3 position) => new((int)MathF.Floor(position.X / TileSize), (int)MathF.Floor(position.Z / TileSize));
    public static Vector3 Origin(TileKey tile) => new(tile.X * TileSize, 0, tile.Z * TileSize);

    public static IReadOnlyList<IReadOnlyList<Collider>> BodyBatches(IEnumerable<Collider> colliders) => colliders
        .GroupBy(c => (c.Tag, c.Foot)).OrderBy(g => (int)g.Key.Tag).ThenBy(g => (int)g.Key.Foot)
        .SelectMany(g => g.Chunk(MaxShapesPerBody)).Select(c => (IReadOnlyList<Collider>)c).ToArray();

    /// <summary>Applies a part placement or tile translation without consulting render geometry.</summary>
    public static Collider Transform(Collider source, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is BoxCollider box) return box with { At = box.At * at };
        RequireUpright(at);
        Vector3 P(Vector3 p) => Vector3.Transform(p, at);
        Vector2 Xz(Vector2 p) { var q = P(new(p.X, 0, p.Y)); return new(q.X, q.Z); }
        var sx = new Vector3(at.M11, at.M12, at.M13).Length();
        var sy = at.M22;
        var sz = new Vector3(at.M31, at.M32, at.M33).Length();
        var uniformPlan = MathF.Abs(sx - sz) <= 0.0001f;
        float Y(float y) => y * sy + at.M42;
        return source switch
        {
            WallCollider w when uniformPlan => w with { A = Xz(w.A), B = Xz(w.B), HalfT = w.HalfT * sx, Y0 = Y(w.Y0), Y1 = Y(w.Y1) },
            SlopedWallCollider w when uniformPlan => w with
            {
                A = Xz(w.A),
                B = Xz(w.B),
                HalfT = w.HalfT * sx,
                Y0a = Y(w.Y0a),
                Y1a = Y(w.Y1a),
                Y0b = Y(w.Y0b),
                Y1b = Y(w.Y1b)
            },
            PostCollider p when uniformPlan => p with { C = Xz(p.C), R = p.R * sx, Y0 = Y(p.Y0), Y1 = Y(p.Y1) },
            RampCollider r => r with { P0 = P(r.P0), P1 = P(r.P1), P2 = P(r.P2), P3 = P(r.P3), Thick = r.Thick * sy },
            SlabCollider s => s with { Ring = s.Ring.Select(Xz).ToArray(), Y = Y(s.Y), Thick = s.Thick * sy },
            // Height fields carry a tile address. Their body supplies the tile origin at runtime.
            HeightFieldCollider h when at.M11 == 1 && at.M33 == 1 && at.M13 == 0 && at.M31 == 0 && sy == 1 && at.M42 == 0 => h,
            _ => throw new ArgumentException($"Unsupported placement for {source.GetType().Name}; upright primitives need uniform X/Z scale."),
        };
    }

    public static IReadOnlyList<Collider> Localize(IEnumerable<Collider> worldColliders, TileKey tile)
    {
        var translation = Matrix4x4.CreateTranslation(-Origin(tile));
        var result = new List<Collider>();
        foreach (var source in worldColliders)
        {
            if (!Enum.IsDefined(source.Tag) || !Enum.IsDefined(source.Foot)) throw new ArgumentException("Unknown collision tag or material.");
            if (source.Tag == SurfaceTag.Water) continue;
            var local = Transform(source, translation);
            if (local is SlabCollider slab)
                result.AddRange(PrepareSlab(slab));
            else result.Add(local);
        }
        return result;
    }

    /// <summary>Keep convex floors continuous; only concave outlines need a convex decomposition.</summary>
    public static IReadOnlyList<SlabCollider> PrepareSlab(SlabCollider slab)
    {
        var ring = NormalizeSlabRing(slab);
        return IsConvexRing(ring) ? [slab with { Ring = ring.ToArray() }] : TriangulatePrepared(slab, ring);
    }

    /// <summary>Ear-clips a simple ring; never fills a concave recess with its convex hull.</summary>
    public static IReadOnlyList<SlabCollider> Triangulate(SlabCollider slab) => TriangulatePrepared(slab, NormalizeSlabRing(slab));

    /// <summary>Checks the turns of a previously validated simple ring, in either winding.</summary>
    public static bool IsConvexRing(IReadOnlyList<Vector2> ring)
    {
        if (ring.Count < 3) return false;
        var direction = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var turn = Cross(ring[i], ring[(i + 1) % ring.Count], ring[(i + 2) % ring.Count]);
            if (!double.IsFinite(turn)) return false;
            if (turn == 0) continue;
            var next = turn > 0 ? 1 : -1;
            if (direction != 0 && next != direction) return false;
            direction = next;
        }
        return direction != 0;
    }

    private static List<Vector2> NormalizeSlabRing(SlabCollider slab)
    {
        if (!float.IsFinite(slab.Y) || !float.IsFinite(slab.Thick) || slab.Thick <= 0) throw new ArgumentException("Invalid slab height or thickness.");
        var ring = slab.Ring.ToList();
        if (ring.Count > 1 && ring[0] == ring[^1]) ring.RemoveAt(ring.Count - 1);
        if (ring.Count < 3 || ring.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y))) throw new ArgumentException("Invalid slab outline.");
        bool removed;
        do
        {
            removed = false;
            for (var i = ring.Count - 1; i >= 0 && ring.Count >= 3; i--)
            {
                if (ring[i] != ring[(i + 1) % ring.Count] &&
                    Cross(ring[(i + ring.Count - 1) % ring.Count], ring[i], ring[(i + 1) % ring.Count]) != 0) continue;
                ring.RemoveAt(i);
                removed = true;
            }
        } while (removed && ring.Count >= 3);
        if (ring.Count < 3) throw new ArgumentException("Invalid slab outline.");
        for (var i = 0; i < ring.Count; i++)
            for (var j = i + 1; j < ring.Count; j++)
            {
                if (j == i + 1 || (i == 0 && j == ring.Count - 1)) continue;
                if (SegmentsCross(ring[i], ring[(i + 1) % ring.Count], ring[j], ring[(j + 1) % ring.Count]))
                    throw new ArgumentException("Slab outline must be simple.");
            }
        var area = 2 * PlanGeometry.SignedArea(ring);
        if (area == 0)
            throw new ArgumentException($"Slab outline has zero area: doubled area={area:R}, ring=[{string.Join("; ", ring)}].");
        if (area < 0) ring.Reverse();
        return ring;
    }

    private static IReadOnlyList<SlabCollider> TriangulatePrepared(SlabCollider slab, List<Vector2> ring)
    {
        var result = new List<SlabCollider>();
        while (ring.Count > 3)
        {
            var clipped = false;
            for (var i = 0; i < ring.Count; i++)
            {
                var previous = (i + ring.Count - 1) % ring.Count;
                var next = (i + 1) % ring.Count;
                var a = ring[previous]; var b = ring[i]; var c = ring[next];
                if (Cross(a, b, c) <= 0) continue;
                var occupied = false;
                for (var j = 0; j < ring.Count; j++)
                    if (j != previous && j != i && j != next && InTriangle(ring[j], a, b, c)) { occupied = true; break; }
                if (occupied) continue;
                result.Add(slab with { Ring = new[] { a, b, c } });
                ring.RemoveAt(i); clipped = true; break;
            }
            if (!clipped) throw new ArgumentException("Slab ring is self-intersecting or cannot be triangulated.");
        }
        result.Add(slab with { Ring = ring.ToArray() });
        return result;
    }

    public static bool IntersectsTile(Collider collider, TileKey tile)
    {
        var bounds = Bounds(collider);
        var origin = Origin(tile);
        return bounds.Max.X >= origin.X && bounds.Min.X <= origin.X + TileSize &&
            bounds.Max.Z >= origin.Z && bounds.Min.Z <= origin.Z + TileSize;
    }

    public static Box3 Bounds(Collider collider)
    {
        IEnumerable<Vector3> points = collider switch
        {
            WallCollider w => new[] { new Vector3(MathF.Min(w.A.X, w.B.X) - w.HalfT, w.Y0, MathF.Min(w.A.Y, w.B.Y) - w.HalfT), new Vector3(MathF.Max(w.A.X, w.B.X) + w.HalfT, w.Y1, MathF.Max(w.A.Y, w.B.Y) + w.HalfT) },
            SlopedWallCollider w => new[] { new Vector3(MathF.Min(w.A.X, w.B.X) - w.HalfT, MathF.Min(w.Y0a, w.Y0b), MathF.Min(w.A.Y, w.B.Y) - w.HalfT), new Vector3(MathF.Max(w.A.X, w.B.X) + w.HalfT, MathF.Max(w.Y1a, w.Y1b), MathF.Max(w.A.Y, w.B.Y) + w.HalfT) },
            PostCollider p => new[] { new Vector3(p.C.X - p.R, p.Y0, p.C.Y - p.R), new Vector3(p.C.X + p.R, p.Y1, p.C.Y + p.R) },
            RampCollider r => new[] { r.P0, r.P1, r.P2, r.P3, r.P0 - Vector3.UnitY * r.Thick, r.P1 - Vector3.UnitY * r.Thick, r.P2 - Vector3.UnitY * r.Thick, r.P3 - Vector3.UnitY * r.Thick },
            SlabCollider s => s.Ring.SelectMany(p => new[] { new Vector3(p.X, s.Y, p.Y), new Vector3(p.X, s.Y - s.Thick, p.Y) }),
            BoxCollider b => Enumerable.Range(0, 8).Select(i => Vector3.Transform(new Vector3((i & 1) == 0 ? -b.Half.X : b.Half.X, (i & 2) == 0 ? -b.Half.Y : b.Half.Y, (i & 4) == 0 ? -b.Half.Z : b.Half.Z), b.At)),
            HeightFieldCollider h => new[] { Origin(h.Tile), Origin(h.Tile) + new Vector3(TileSize, 0, TileSize) },
            _ => throw new NotSupportedException(collider.GetType().Name),
        };
        var min = new Vector3(float.PositiveInfinity); var max = new Vector3(float.NegativeInfinity);
        foreach (var p in points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        return new(min, max);
    }

    // These are topology tests: a fixed area cutoff would erase valid small support faces.
    private static double Cross(Vector2 a, Vector2 b, Vector2 c) => ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);
    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) => Cross(a, b, p) >= 0 && Cross(b, c, p) >= 0 && Cross(c, a, p) >= 0;
    private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d) =>
        MathF.Max(a.X, b.X) >= MathF.Min(c.X, d.X) && MathF.Max(c.X, d.X) >= MathF.Min(a.X, b.X) &&
        MathF.Max(a.Y, b.Y) >= MathF.Min(c.Y, d.Y) && MathF.Max(c.Y, d.Y) >= MathF.Min(a.Y, b.Y) &&
        Cross(a, b, c) * Cross(a, b, d) <= 0 && Cross(c, d, a) * Cross(c, d, b) <= 0;
    private static void RequireUpright(Matrix4x4 at)
    {
        if (at.M12 != 0 || at.M21 != 0 || at.M23 != 0 || at.M32 != 0 || at.M14 != 0 || at.M24 != 0 || at.M34 != 0 || at.M44 != 1 || at.M22 <= 0)
            throw new ArgumentException("Wall, post, ramp and slab placements must preserve upright Y.");
        if (MathF.Abs(at.M11 * at.M31 + at.M13 * at.M33) > 0.0001f) throw new ArgumentException("Upright primitive placement must not shear.");
    }
}

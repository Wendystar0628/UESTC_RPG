using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Collision;

/// <summary>Pure CPU collision preparation. All coordinates supplied here are world metres.</summary>
public static class CollisionGeometry
{
    public const int MaxShapesPerBody = 250;

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
        var origin = TileGrid.Origin(tile);
        var translation = Matrix4x4.CreateTranslation(-origin.X, 0, -origin.Y);
        var result = new List<Collider>();
        foreach (var source in worldColliders)
        {
            if (!Enum.IsDefined(source.Tag) || !Enum.IsDefined(source.Foot)) throw new ArgumentException("Unknown collision tag or material.");
            if (source.Tag == SurfaceTag.Water) continue;
            foreach (var collider in CollisionBoxes.Of(source))
                result.Add(Transform(collider, translation));
        }
        return result;
    }

    public static bool IntersectsTile(Collider collider, TileKey tile)
    {
        var bounds = Bounds(collider);
        var origin = TileGrid.Origin(tile);
        return bounds.Max.X >= origin.X && bounds.Min.X <= origin.X + TileGrid.BaseEdge &&
            bounds.Max.Z >= origin.Y && bounds.Min.Z <= origin.Y + TileGrid.BaseEdge;
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
            HeightFieldCollider h => new[]
            {
                new Vector3(TileGrid.Origin(h.Tile).X, 0, TileGrid.Origin(h.Tile).Y),
                new Vector3(TileGrid.Origin(h.Tile).X + TileGrid.BaseEdge, 0, TileGrid.Origin(h.Tile).Y + TileGrid.BaseEdge)
            },
            _ => throw new NotSupportedException(collider.GetType().Name),
        };
        var min = new Vector3(float.PositiveInfinity); var max = new Vector3(float.NegativeInfinity);
        foreach (var p in points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        return new(min, max);
    }

    private static void RequireUpright(Matrix4x4 at)
    {
        if (at.M12 != 0 || at.M21 != 0 || at.M23 != 0 || at.M32 != 0 || at.M14 != 0 || at.M24 != 0 || at.M34 != 0 || at.M44 != 1 || at.M22 <= 0)
            throw new ArgumentException("Wall, post, ramp and slab placements must preserve upright Y.");
        if (MathF.Abs(at.M11 * at.M31 + at.M13 * at.M33) > 0.0001f) throw new ArgumentException("Upright primitive placement must not shear.");
    }
}

using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>D0 numerical queries over the same declarative shapes mounted by the host.</summary>
[KernelService(typeof(ICollisionWorld), Owner = "WP-00", IsStub = true)]
public sealed class CollisionWorldStub : ICollisionWorld
{
    private Collider[] _colliders = [];
    public IReadOnlyList<Collider> Colliders => _colliders;
    public void Configure(IEnumerable<Collider> colliders) => _colliders = colliders.ToArray();
    public bool Ready(Vector3 p, float r) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z) && float.IsFinite(r) && r >= 0;

    public SupportHit? SupportY(float x, float z, float yHint)
    {
        SupportHit? best = null;
        foreach (Collider collider in _colliders)
        {
            float? candidate = collider switch
            {
                SlabCollider slab when Inside(slab.Ring, new(x, z)) => slab.Y,
                HeightFieldCollider field => Height(field, x, z),
                RampCollider ramp => RampHeight(ramp, x, z),
                _ => null
            };
            if (candidate is float y && y <= yHint + 0.35f && (best is null || y > best.Value.Y)) best = new(y, collider.Tag);
        }
        return best;
    }

    public SweepHit? Sweep(Vector3 from, Vector3 to, float r, float h)
    {
        if (!float.IsFinite(r) || r < 0 || !float.IsFinite(h) || h <= 0) throw new ArgumentOutOfRangeException(nameof(r));
        SweepHit? nearest = null;
        foreach (Collider collider in _colliders)
        {
            if (collider is WallCollider wall)
            {
                var hit = SweepWall(from, to, r, h, wall);
                if (hit is SweepHit value && (nearest is null || value.T < nearest.Value.T)) nearest = value;
            }
            else if (collider is SlabCollider slab && to.Y < from.Y && from.Y >= slab.Y && to.Y < slab.Y)
            {
                float t = (from.Y - slab.Y) / (from.Y - to.Y);
                Vector3 point = Vector3.Lerp(from, to, t);
                if (Inside(slab.Ring, new(point.X, point.Z)) && (nearest is null || t < nearest.Value.T))
                    nearest = new(point, Vector3.UnitY, t, slab.Tag);
            }
        }
        return nearest;
    }

    private static SweepHit? SweepWall(Vector3 from, Vector3 to, float radius, float height, WallCollider wall)
    {
        Vector2 direction = wall.B - wall.A;
        float length = direction.Length();
        if (length < 0.00001f) return null;
        Vector2 tangent = direction / length, normal = new(-tangent.Y, tangent.X);
        Vector2 start = new(from.X, from.Z), end = new(to.X, to.Z);
        float d0 = Vector2.Dot(start - wall.A, normal), d1 = Vector2.Dot(end - wall.A, normal);
        float half = wall.HalfT + radius;
        float sign = d0 >= 0 ? 1 : -1;
        if (MathF.Abs(d0) < half && MathF.Abs(d1) >= MathF.Abs(d0)) return null;
        float denominator = d0 - d1;
        if (MathF.Abs(denominator) < 0.000001f) return null;
        float t = Math.Clamp((d0 - sign * half) / denominator, 0, 1);
        if (MathF.Abs(d0) >= half && (d0 - sign * half) / denominator is < 0 or > 1) return null;
        Vector3 point = Vector3.Lerp(from, to, t);
        float along = Vector2.Dot(new Vector2(point.X, point.Z) - wall.A, tangent);
        if (along < -radius || along > length + radius || point.Y + height <= wall.Y0 || point.Y >= wall.Y1) return null;
        return new(point, new Vector3(normal.X * sign, 0, normal.Y * sign), t, wall.Tag);
    }

    private static float? Height(HeightFieldCollider field, float x, float z)
    {
        float localX = x - field.Tile.X * 64, localZ = z - field.Tile.Z * 64;
        if (localX < 0 || localX > 64 || localZ < 0 || localZ > 64 || field.HeightsCm.Length != 65 * 65) return null;
        int ix = Math.Min((int)localX, 63), iz = Math.Min((int)localZ, 63);
        float fx = localX - ix, fz = localZ - iz;
        float a = field.HeightsCm[iz * 65 + ix] * (1 - fx) + field.HeightsCm[iz * 65 + ix + 1] * fx;
        float b = field.HeightsCm[(iz + 1) * 65 + ix] * (1 - fx) + field.HeightsCm[(iz + 1) * 65 + ix + 1] * fx;
        return (a * (1 - fz) + b * fz) / 100;
    }

    private static float? RampHeight(RampCollider ramp, float x, float z)
    {
        Vector2 p = new(x, z), a = new(ramp.P0.X, ramp.P0.Z), b = new(ramp.P1.X, ramp.P1.Z), d = new(ramp.P3.X, ramp.P3.Z);
        Vector2 u = b - a, v = d - a, rel = p - a;
        float determinant = u.X * v.Y - u.Y * v.X;
        if (MathF.Abs(determinant) < 0.000001f) return null;
        float along = (rel.X * v.Y - rel.Y * v.X) / determinant;
        float up = (u.X * rel.Y - u.Y * rel.X) / determinant;
        if (along < 0 || along > 1 || up < 0 || up > 1) return null;
        return ramp.P0.Y + (ramp.P1.Y - ramp.P0.Y) * along + (ramp.P3.Y - ramp.P0.Y) * up;
    }

    private static bool Inside(IReadOnlyList<Vector2> ring, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[i], b = ring[j];
            if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }
}

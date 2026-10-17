using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Collision;

/// <summary>The C producer reduces source primitives to ordinary boxes; ground retains its height field.</summary>
internal static class CollisionBoxes
{
    public static IEnumerable<Collider> Of(Collider source) => source switch
    {
        HeightFieldCollider field => [field],
        BoxCollider box => [Normalize(box)],
        WallCollider wall => [Wall(wall.A, wall.B, wall.HalfT, wall.Y0, wall.Y1, wall)],
        SlopedWallCollider wall => [Wall(wall.A, wall.B, wall.HalfT,
            MathF.Min(wall.Y0a, wall.Y0b), MathF.Max(wall.Y1a, wall.Y1b), wall)],
        PostCollider post => [Box(new(post.C.X, (post.Y0 + post.Y1) / 2, post.C.Y),
            new(post.R, (post.Y1 - post.Y0) / 2, post.R), post)],
        RampCollider ramp => [Ramp(ramp)],
        SlabCollider slab => Slab(slab),
        _ => throw new NotSupportedException(source.GetType().Name)
    };

    private static BoxCollider Normalize(BoxCollider source)
    {
        var m = source.At;
        var x = Vector3.Normalize(new(m.M11, m.M12, m.M13));
        var y = Vector3.Normalize(new(m.M21, m.M22, m.M23));
        var z = Vector3.Normalize(new(m.M31, m.M32, m.M33));
        if (MathF.Abs(Vector3.Dot(x, y)) <= .0001f && MathF.Abs(Vector3.Dot(x, z)) <= .0001f &&
            MathF.Abs(Vector3.Dot(y, z)) <= .0001f) return source;
        var bounds = CollisionGeometry.Bounds(source);
        return Box((bounds.Min + bounds.Max) / 2, (bounds.Max - bounds.Min) / 2, source);
    }

    private static BoxCollider Wall(Vector2 a, Vector2 b, float halfT, float bottom, float top, Collider source)
    {
        var axis = Vector2.Normalize(b - a);
        return Oriented((a + b) / 2, axis, Vector2.Distance(a, b) / 2, halfT, bottom, top, source);
    }

    private static BoxCollider Ramp(RampCollider source)
    {
        var width = source.P1 - source.P0;
        var run = source.P3 - source.P0;
        var x = Vector3.Normalize(width);
        var z = Vector3.Normalize(run);
        // Standard rectangular slopes stay one inclined box, with its upper face on the declared ramp.
        if (MathF.Abs(Vector3.Dot(x, z)) <= .01f &&
            Vector3.Distance(source.P2, source.P1 + run) <= .05f)
        {
            var y = Vector3.Normalize(Vector3.Cross(z, x));
            if (y.Y < 0) y = -y;
            var halfThickness = MathF.Max(.01f, source.Thick * y.Y) / 2;
            var center = (source.P0 + source.P1 + source.P2 + source.P3) / 4 - y * halfThickness;
            return new(Frame(x, y, z, center), new(width.Length() / 2, halfThickness, run.Length() / 2), source.Tag)
                { Foot = source.Foot };
        }
        // A non-rectangular ramp still uses a coarse box in its own sloping frame.
        z = Vector3.Normalize(run - x * Vector3.Dot(run, x));
        var normal = Vector3.Normalize(Vector3.Cross(z, x));
        if (normal.Y < 0) normal = -normal;
        var points = new[] { source.P0, source.P1, source.P2, source.P3 };
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var point in points)
        {
            var local = new Vector3(Vector3.Dot(point, x), Vector3.Dot(point, normal), Vector3.Dot(point, z));
            min = Vector3.Min(min, local - new Vector3(0, source.Thick, 0));
            max = Vector3.Max(max, local);
        }
        var mid = (min + max) / 2;
        return new(Frame(x, normal, z, x * mid.X + normal * mid.Y + z * mid.Z), (max - min) / 2, source.Tag)
            { Foot = source.Foot };
    }

    private readonly record struct Strip(float X0, float X1, float Z0, float Z1);

    private static IEnumerable<Collider> Slab(SlabCollider source)
    {
        var ring = source.Ring.ToArray();
        if (ring.Length > 1 && ring[0] == ring[^1]) ring = ring[..^1];
        if (ring.Length < 3 || !float.IsFinite(source.Y) || !float.IsFinite(source.Thick) || source.Thick <= 0)
            throw new ArgumentException("Invalid platform outline or height.", nameof(source));
        var longest = Enumerable.Range(0, ring.Length)
            .MaxBy(i => Vector2.DistanceSquared(ring[i], ring[(i + 1) % ring.Length]));
        var axis = Vector2.Normalize(ring[(longest + 1) % ring.Length] - ring[longest]);
        var across = new Vector2(-axis.Y, axis.X);
        var origin = ring[0];
        var local = ring.Select(p => new Vector2(Vector2.Dot(p - origin, axis), Vector2.Dot(p - origin, across))).ToArray();
        var min = local.Aggregate(Vector2.Min);
        var max = local.Aggregate(Vector2.Max);
        var rectilinear = Enumerable.Range(0, local.Length).All(i =>
        {
            var edge = local[(i + 1) % local.Length] - local[i];
            return MathF.Abs(edge.X) <= .01f || MathF.Abs(edge.Y) <= .01f;
        });
        if (local.Length == 4 && rectilinear)
            return [Make(new(min.X, max.X, min.Y, max.Y))];
        // Right-angle platforms split only at authored corners. Curves and diagonal outlines use metre-wide strips.
        var cuts = new List<float> { min.Y };
        if (rectilinear)
        {
            foreach (var value in local.Select(p => p.Y).Order())
                if (value > cuts[^1] + .01f) cuts.Add(value);
        }
        else
            for (var z = min.Y + 1; z < max.Y; z += 1) cuts.Add(z);
        if (max.Y > cuts[^1] + .001f) cuts.Add(max.Y);
        var strips = new List<Strip>();
        for (var row = 0; row + 1 < cuts.Count; row++)
        {
            var z = (cuts[row] + cuts[row + 1]) / 2;
            var crossings = new List<float>();
            for (var i = 0; i < local.Length; i++)
            {
                var a = local[i]; var b = local[(i + 1) % local.Length];
                if ((a.Y > z) == (b.Y > z)) continue;
                crossings.Add(a.X + (z - a.Y) * (b.X - a.X) / (b.Y - a.Y));
            }
            crossings.Sort();
            for (var i = 0; i + 1 < crossings.Count; i += 2)
            {
                var strip = new Strip(crossings[i], crossings[i + 1], cuts[row], cuts[row + 1]);
                if (strip.X1 - strip.X0 < .01f) continue;
                var previous = strips.FindLastIndex(p => MathF.Abs(p.X0 - strip.X0) < .01f &&
                    MathF.Abs(p.X1 - strip.X1) < .01f && MathF.Abs(p.Z1 - strip.Z0) < .01f);
                if (previous >= 0) strips[previous] = strips[previous] with { Z1 = strip.Z1 };
                else strips.Add(strip);
            }
        }
        return strips.Select(Make).ToArray();

        BoxCollider Make(Strip strip)
        {
            var center = origin + axis * ((strip.X0 + strip.X1) / 2) + across * ((strip.Z0 + strip.Z1) / 2);
            return Oriented(center, axis, (strip.X1 - strip.X0) / 2, (strip.Z1 - strip.Z0) / 2,
                source.Y - source.Thick, source.Y, source);
        }
    }

    private static BoxCollider Oriented(Vector2 center, Vector2 axis, float halfX, float halfZ,
        float bottom, float top, Collider source) =>
        new(Frame(new(axis.X, 0, axis.Y), Vector3.UnitY, new(-axis.Y, 0, axis.X),
            new(center.X, (bottom + top) / 2, center.Y)), new(halfX, (top - bottom) / 2, halfZ), source.Tag)
            { Foot = source.Foot };

    private static Matrix4x4 Frame(Vector3 x, Vector3 y, Vector3 z, Vector3 center) => new(
        x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, center.X, center.Y, center.Z, 1);
    private static BoxCollider Box(Vector3 center, Vector3 half, Collider source) =>
        new(Matrix4x4.CreateTranslation(center), half, source.Tag) { Foot = source.Foot };
}

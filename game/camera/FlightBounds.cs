using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Camera;

public readonly record struct FlightBoundary(float Distance, Vector3 Outward, bool Horizontal,
    float ProjectionScale = 1, bool Buffered = true);

/// <summary>Map-backed reachable volume; negative distances mean inside the permitted space.</summary>
public sealed class FlightBounds(IMapContext context, PolygonRings boundary)
{
    private const float Epsilon = .002f;
    public RoamLimits Limits => context.Descriptor.Roam;

    /// <summary>Apply the existing radial cap before any terrain-dependent constraint is read.</summary>
    public Vector3 ProjectHorizontal(Vector3 p)
    {
        if (!VectorMath.Finite(p)) throw new ArgumentOutOfRangeException(nameof(p));
        double x = (double)p.X - context.Descriptor.Center.X;
        double z = (double)p.Z - context.Descriptor.Center.Y;
        double radius = Math.Sqrt(x * x + z * z);
        if (radius <= Limits.MaxRadius + Epsilon) return p;
        double scale = (Limits.MaxRadius - Epsilon) / radius;
        return new((float)(context.Descriptor.Center.X + x * scale), p.Y,
            (float)(context.Descriptor.Center.Y + z * scale));
    }

    public int Evaluate(Vector3 p, Span<FlightBoundary> result)
    {
        if (result.Length < 5) throw new ArgumentException("Five constraints are required.", nameof(result));
        var limits = Limits;
        Vector2 xz = new(p.X, p.Z);
        float campusDistance = RingDistance(xz, boundary, out Vector2 campusNormal);
        var outskirts = context.World.Outskirts;
        float front = outskirts?.FrontDistance(p.X, p.Z) ?? Math.Max(0, campusDistance - limits.CampusExpansionMargin);
        Vector2 frontNormal = campusNormal;
        float signedFront = campusDistance - limits.CampusExpansionMargin;
        if (outskirts is not null && outskirts.FrontLine.Count >= 3)
            signedFront = RingDistance(xz, outskirts.FrontLine, out frontNormal);
        else if (outskirts is not null && front > 0)
        {
            Vector2 gradient = new(outskirts.FrontDistance(p.X + 1, p.Z) - outskirts.FrontDistance(p.X - 1, p.Z),
                outskirts.FrontDistance(p.X, p.Z + 1) - outskirts.FrontDistance(p.X, p.Z - 1));
            if (gradient.LengthSquared() > .000001f) frontNormal = Vector2.Normalize(gradient);
        }
        float cone = (front > 0 ? front : signedFront) - limits.HeightFactor * Math.Max(0, p.Y - limits.HeightStart);
        Vector3 coneNormal = new(frontNormal.X, p.Y > limits.HeightStart ? -limits.HeightFactor : 0, frontNormal.Y);
        float coneScale = coneNormal.Length();
        result[0] = new(cone, coneNormal / coneScale, true, 1f / coneScale);
        Vector2 radial = xz - context.Descriptor.Center;
        float radius = radial.Length();
        result[1] = new(radius - limits.MaxRadius,
            radius > .001f ? new(radial.X / radius, 0, radial.Y / radius) : Vector3.UnitX, true);
        float ground = context.World.Spatial.GroundY(p.X, p.Z);
        // Underground movement meets a vertical campus wall; never lift the observer through street ground.
        bool underground = p.Y < ground;
        float floor = campusDistance <= 0 || underground ? limits.FloorY
            : front > Epsilon ? outskirts?.SoftFloor(p.X, p.Z) ?? ground + limits.SoftFloorClearance
            : ground + .3f;
        // The campus's geological bottom is a hard limit, not the exterior soft floor's 30 m band.
        result[2] = new(floor - p.Y, -Vector3.UnitY, false, Buffered: campusDistance > 0 && !underground);
        result[3] = new(p.Y - limits.CeilY, Vector3.UnitY, false);
        result[4] = new(underground ? campusDistance : -float.MaxValue,
            new(campusNormal.X, 0, campusNormal.Y), false);
        return 5;
    }

    public bool IsLegal(Vector3 p, bool outsideBuffer = false)
    {
        if (!VectorMath.Finite(p) || ProjectHorizontal(p) != p) return false;
        Span<FlightBoundary> constraints = stackalloc FlightBoundary[5];
        Evaluate(p, constraints);
        foreach (var c in constraints)
            if (c.Distance > Epsilon || (outsideBuffer && c.Buffered && c.Distance > -Limits.Buffer)) return false;
        return true;
    }

    public Vector3 Project(Vector3 p)
    {
        if (!VectorMath.Finite(p)) throw new ArgumentOutOfRangeException(nameof(p));
        for (int iteration = 0; iteration < 64; iteration++)
        {
            p = ProjectHorizontal(p);
            if (ProjectStep(ref p)) return p;
        }
        if (!IsLegal(p)) throw new InvalidDataException("Map reachable volume has no stable nearby projection.");
        return p;
    }

    public async Task<Vector3> ProjectPrepared(Vector3 p, ITerrainPreparation terrain,
        CancellationToken cancellation = default)
    {
        if (!VectorMath.Finite(p)) throw new ArgumentOutOfRangeException(nameof(p));
        for (int iteration = 0; iteration < 64; iteration++)
        {
            cancellation.ThrowIfCancellationRequested();
            p = ProjectHorizontal(p);
            var point = new Vector2(p.X, p.Z);
            if (!terrain.IsPrepared(context.World, point)) await terrain.PreparePoints(context, [point], cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (ProjectStep(ref p)) return p;
        }
        p = ProjectHorizontal(p);
        var final = new Vector2(p.X, p.Z);
        await terrain.PreparePoints(context, [final], cancellation);
        cancellation.ThrowIfCancellationRequested();
        if (!IsLegal(p)) throw new InvalidDataException("Map reachable volume has no stable nearby projection.");
        return p;
    }

    private bool ProjectStep(ref Vector3 p)
    {
        Span<FlightBoundary> constraints = stackalloc FlightBoundary[5];
        Evaluate(p, constraints);
        var worst = constraints[0];
        foreach (var constraint in constraints) if (constraint.Distance > worst.Distance) worst = constraint;
        if (worst.Distance <= Epsilon) return true;
        p -= worst.Outward * ((worst.Distance + Epsilon) * worst.ProjectionScale);
        return false;
    }

    public Vector3 FilterVelocity(Vector3 p, Vector3 velocity, out float edgeFog)
    {
        Span<FlightBoundary> constraints = stackalloc FlightBoundary[5];
        Evaluate(p, constraints);
        edgeFog = 0;
        foreach (var c in constraints)
        {
            float s = c.Buffered ? Math.Clamp(1f + c.Distance / Limits.Buffer, 0, 1) : 0;
            if (c.Horizontal) edgeFog = Math.Max(edgeFog, s);
            float outward = Vector3.Dot(velocity, c.Outward);
            if (outward > 0) velocity -= c.Outward * (outward * (1f - (1f - s) * (1f - s)));
        }
        return velocity;
    }

    public static float RingDistance(Vector2 p, PolygonRings rings, out Vector2 normal)
    {
        float distance = RingDistance(p, rings.Outer, out normal);
        foreach (var hole in rings.Holes)
        {
            float toHole = -RingDistance(p, hole, out var holeNormal);
            if (toHole > distance) { distance = toHole; normal = -holeNormal; }
        }
        return distance;
    }

    private static float RingDistance(Vector2 p, IReadOnlyList<Vector2> ring, out Vector2 normal)
    {
        if (ring.Count < 3) throw new InvalidDataException("The map must provide a closed campus boundary.");
        bool inside = false;
        float nearestSquared = float.MaxValue;
        Vector2 nearest = default;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[j], b = ring[i], edge = b - a;
            float lengthSquared = edge.LengthSquared();
            Vector2 q = a + edge * (lengthSquared > 0 ? Math.Clamp(Vector2.Dot(p - a, edge) / lengthSquared, 0, 1) : 0);
            float squared = Vector2.DistanceSquared(p, q);
            if (squared < nearestSquared) { nearestSquared = squared; nearest = q; }
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        float distance = MathF.Sqrt(nearestSquared);
        normal = distance > .00001f ? (inside ? nearest - p : p - nearest) / distance : Vector2.UnitX;
        return inside ? -distance : distance;
    }

}

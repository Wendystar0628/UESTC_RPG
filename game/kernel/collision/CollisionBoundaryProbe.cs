using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision;

/// <summary>A reusable native overlap probe reads actual Boundary surfaces without allocating per physics step.</summary>
internal sealed class CollisionBoundaryProbe : IDisposable
{
    private const float NormalEpsilonSquared = .000001f;
    private readonly CylinderShape3D _shape = new();
    private readonly PhysicsTestMotionParameters3D _query = new()
    {
        Motion = Vector3.Zero,
        Margin = .001f,
        RecoveryAsCollision = true,
        MaxCollisions = 32
    };
    private readonly PhysicsTestMotionResult3D _result = new();
    private readonly Rid _body;

    public CollisionBoundaryProbe(Rid space)
    {
        _body = PhysicsServer3D.BodyCreate();
        PhysicsServer3D.BodySetMode(_body, PhysicsServer3D.BodyMode.Kinematic);
        PhysicsServer3D.BodySetCollisionLayer(_body, 0);
        PhysicsServer3D.BodySetCollisionMask(_body, 1u << (int)SurfaceTag.Boundary);
        PhysicsServer3D.BodyAddShape(_body, _shape.GetRid(), new(Basis.Identity, Vector3.Up * (_shape.Height * .5f)));
        PhysicsServer3D.BodySetSpace(_body, space);
    }

    public int Nearby(N.Vector3 feet, float radius, float height, Span<BoundaryHit> hits)
    {
        float queryRadius = radius + WalkRules.BoundarySlowdownDistance;
        if (_shape.Radius != queryRadius) _shape.Radius = queryRadius;
        if (_shape.Height != height)
        {
            _shape.Height = height;
            PhysicsServer3D.BodySetShapeTransform(_body, 0, new(Basis.Identity, Vector3.Up * (height * .5f)));
        }
        _query.From = new(Basis.Identity, feet.ToGodot());
        if (!PhysicsServer3D.BodyTestMotion(_body, _query, _result)) return 0;
        int count = 0;
        for (int i = 0; i < _result.GetCollisionCount() && count < hits.Length; i++)
        {
            var normal = _result.GetCollisionNormal(i).ToNumerics() with { Y = 0 };
            if (normal.LengthSquared() <= NormalEpsilonSquared) continue; // The top of a gate is not a horizontal boundary.
            normal = N.Vector3.Normalize(normal);
            var point = _result.GetCollisionPoint(i).ToNumerics();
            float distance = MathF.Max(0, N.Vector3.Dot(feet - point, normal) - radius);
            if (distance >= WalkRules.BoundarySlowdownDistance) continue;
            bool repeated = false;
            for (int j = 0; j < count; j++)
            {
                if (N.Vector3.DistanceSquared(hits[j].Inward, normal) > NormalEpsilonSquared) continue;
                if (distance < hits[j].Distance) hits[j] = new(distance, normal);
                repeated = true;
                break;
            }
            if (repeated) continue; // Multiple contacts on one flat fence must not multiply the slowing repeatedly.
            hits[count++] = new(distance, normal);
        }
        return count;
    }

    public void Dispose()
    {
        PhysicsServer3D.FreeRid(_body);
        _result.Dispose();
        _query.Dispose();
        _shape.Dispose();
    }
}

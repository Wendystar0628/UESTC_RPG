using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

/// <summary>One placement rule for entry, warps and recovery, evaluated against mounted native collision.</summary>
public sealed class WalkPlacementAdmission(CharacterBody3D body) : IDisposable
{
    private readonly CylinderShape3D _shape = new() { Radius = WalkRules.Radius };
    private readonly PhysicsShapeQueryParameters3D _query = new()
    {
        CollisionMask = body.CollisionMask,
        CollideWithAreas = false,
        CollideWithBodies = true,
        Margin = 0,
        Exclude = [body.GetRid()]
    };

    public N.Vector3? Resolve(ISpatialIndex spatial, ICollisionWorld collision, N.Vector3 candidate, float height)
    {
        if (!VectorMath.Finite(candidate) || !collision.Ready(candidate, WalkRules.Radius)) return null;
        float hint = MathF.Max(candidate.Y, spatial.GroundY(candidate.X, candidate.Z) + WalkRules.StepHeight);
        if (collision.SupportY(candidate.X, candidate.Z, hint) is not { } hit || !float.IsFinite(hit.Y)) return null;
        var feet = new N.Vector3(candidate.X, hit.Y, candidate.Z);
        if (!WalkSafetyQueries.AllowsFeet(spatial, feet)
            || (WalkSafetyQueries.InWater(spatial, feet) && spatial.WalkClassAt(feet.X, feet.Z) != WalkClass.Wade)) return null;
        _shape.Radius = WalkRules.Radius;
        _shape.Height = height - WalkTuning.SafeMargin;
        _query.Shape = _shape;
        _query.CollisionMask = body.CollisionMask;
        // A flat cylinder touches an uphill floor above its centre ray. Lift within the existing step
        // height until its volume clears; ordinary gravity then settles it onto the native slope.
        for (int step = 0; step <= 4; step++)
        {
            var placed = feet + N.Vector3.UnitY * (WalkRules.StepHeight * step / 4);
            _query.Transform = new(Basis.Identity,
                (placed + N.Vector3.UnitY * (height * .5f + WalkTuning.SafeMargin)).ToGodot());
            if (body.GetWorld3D().DirectSpaceState.IntersectShape(_query, 1).Count == 0) return placed;
        }
        return null;
    }

    public bool ClearOfBoundary(N.Vector3 feet, float height)
    {
        _shape.Radius = WalkTuning.BoundaryClearance;
        _shape.Height = height - WalkTuning.SafeMargin;
        _query.Shape = _shape;
        _query.CollisionMask = 1u << (int)SurfaceTag.Boundary;
        _query.Transform = new(Basis.Identity, (feet + N.Vector3.UnitY * (height * .5f)).ToGodot());
        return body.GetWorld3D().DirectSpaceState.IntersectShape(_query, 1).Count == 0;
    }

    public void Dispose()
    {
        _query.Dispose();
        _shape.Dispose();
    }
}

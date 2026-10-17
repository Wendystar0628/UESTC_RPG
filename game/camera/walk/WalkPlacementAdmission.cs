using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

/// <summary>One placement rule for entry, warps and recovery, evaluated against mounted native collision.</summary>
public sealed class WalkPlacementAdmission(CharacterBody3D body) : IDisposable
{
    private readonly CylinderShape3D _shape = new() { Radius = WalkTuning.Radius };
    private readonly PhysicsShapeQueryParameters3D _query = new()
    {
        CollisionMask = body.CollisionMask,
        CollideWithAreas = false,
        CollideWithBodies = true,
        Margin = 0,
        Exclude = [body.GetRid()]
    };

    public static N.Vector3? ResolveSupport(ISpatialIndex spatial, N.Vector3 candidate)
    {
        if (!WalkMath.Finite(candidate) || !WalkMath.CanWalk(spatial.WalkClassAt(candidate.X, candidate.Z))) return null;
        var support = spatial.SupportY(candidate.X, candidate.Z, candidate.Y);
        return support is { } y && float.IsFinite(y) ? new(candidate.X, y, candidate.Z) : null;
    }

    public bool Accept(ISpatialIndex spatial, ICollisionWorld collision, N.Vector3 feet, float height, float lift)
    {
        if (!collision.Ready(feet, WalkTuning.Radius)) return false;
        var support = ResolveSupport(spatial, feet - N.Vector3.UnitY * lift);
        if (support is not { } floor || MathF.Abs(feet.Y - lift - floor.Y) > WalkTuning.SafeMargin) return false;
        // Resident support selects the layer; mounted C must actually support that same layer.
        if (collision.SupportY(floor.X, floor.Z, floor.Y) is not { } actual
            || !float.IsFinite(actual.Y) || MathF.Abs(actual.Y - floor.Y) > WalkTuning.SafeMargin) return false;
        _shape.Height = height - WalkTuning.SafeMargin;
        _query.Shape = _shape;
        _query.Transform = new(Basis.Identity,
            (feet + N.Vector3.UnitY * (height * .5f + WalkTuning.SafeMargin)).ToGodot());
        return body.GetWorld3D().DirectSpaceState.IntersectShape(_query, 1).Count == 0;
    }

    public static Pose? Recover(ISpatialIndex spatial, WalkSafetyRing safe, Pose from, bool manual,
        int skip, N.Vector3? rejected)
    {
        bool AcceptTarget(N.Vector3 candidate)
        {
            var support = ResolveSupport(spatial, candidate);
            return WalkSafetyQueries.SupportedDryFeet(spatial, candidate) && support is { } feet
                && (rejected is not { } failed || N.Vector3.DistanceSquared(feet, failed) >= .04f);
        }
        var target = safe.Nearest(from.Position, manual ? WalkTuning.ReturnMinimumDistance : 0, skip,
            pose => AcceptTarget(pose.Position));
        if (target is null && spatial.NearestSafe(from.Position, WalkTuning.SafetySearchRadius) is { } fallback
            && AcceptTarget(fallback)) target = new(fallback, from.YawDeg, from.PitchDeg);
        return target;
    }

    public void Dispose()
    {
        _query.Dispose();
        _shape.Dispose();
    }
}

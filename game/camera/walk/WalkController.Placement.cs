using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private bool ResolvePlacement()
    {
        var spatial = _map!.World.Spatial;
        var candidate = _placementTarget.Position;
        var floor = _admission!.Resolve(spatial, _collision!, candidate, _cylinder.Height);
        if (floor is null && !_placementStableTried && _lastStable is { } stable
            && N.Vector3.DistanceSquared(stable.Position, candidate) > WalkTuning.SaveSupportTolerance * WalkTuning.SaveSupportTolerance)
        {
            _placementStableTried = true;
            MovePlacementFocus(stable with { YawDeg = _yaw, PitchDeg = _pitch });
            return false;
        }
        floor ??= _admission.Nearby(spatial, _collision!, candidate, _cylinder.Height);
        if (floor is not { } feet)
        {
            if (!_placementFallbackTried)
            {
                _placementFallbackTried = true;
                var spawn = WalkSafetyQueries.PlacementHint(_map, DefaultFeet(_map.Descriptor.Id));
                if (N.Vector3.DistanceSquared(spawn.Position, candidate) > WalkTuning.SaveSupportTolerance * WalkTuning.SaveSupportTolerance)
                {
                    MovePlacementFocus(spawn with { YawDeg = _yaw, PitchDeg = _pitch });
                    return false;
                }
            }
            Leave();
            _safetyFailed = true;
            return false;
        }
        _placementTarget = new(feet, _yaw, _pitch);
        _feet = _previousFeet = feet + N.Vector3.UnitY * _placementLift;
        GlobalPosition = _feet.ToGodot();
        ResetPhysicsInterpolation();
        _placementNeedsCheck = false;
        CollisionReady = true;
        if (_entryFrom is { } from)
        {
            _descentFrom = from;
            float height = from.Position.Y - (feet.Y + EyeHeight);
            float distance = N.Vector3.Distance(from.Position, feet + N.Vector3.UnitY * EyeHeight);
            _descentDuration = distance > 3f ? WalkMath.DescentSeconds(MathF.Max(height, distance)) : 0;
            _descentElapsed = 0;
            _entryFrom = null;
        }
        Velocity = Vector3.Down * WalkTuning.LandingSpeed;
        return true;
    }

    private void MovePlacementFocus(Pose target)
    {
        _placementTarget = WalkSafetyQueries.PlacementHint(_map!, target);
        _feet = _previousFeet = _placementTarget.Position;
        GlobalPosition = _feet.ToGodot();
        ResetPhysicsInterpolation();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
    }

    private Vector3 LimitRegions(Vector3 motion)
    {
        if (RegionAllowsMotion(motion)) return motion;
        var alongZ = new Vector3(0, 0, motion.Z);
        if (RegionAllowsMotion(alongZ)) return alongZ;
        var alongX = new Vector3(motion.X, 0, 0);
        return RegionAllowsMotion(alongX) ? alongX : Vector3.Zero;
    }

    private bool RegionAllowsMotion(Vector3 motion)
    {
        int count = Math.Max(1, (int)MathF.Ceiling(motion.Length() / WalkTuning.Radius));
        for (int i = 1; i <= count; i++)
        {
            var center = _feet + (motion * ((float)i / count)).ToNumerics();
            if (!RegionAllowsPoint(center)
                || !RegionAllowsPoint(center + N.Vector3.UnitX * WalkTuning.Radius)
                || !RegionAllowsPoint(center - N.Vector3.UnitX * WalkTuning.Radius)
                || !RegionAllowsPoint(center + N.Vector3.UnitZ * WalkTuning.Radius)
                || !RegionAllowsPoint(center - N.Vector3.UnitZ * WalkTuning.Radius)) return false;
        }
        return true;
    }

    private bool RegionAllowsPoint(N.Vector3 point)
    {
        var spatial = _map!.World.Spatial;
        var classification = spatial.WalkClassAt(point.X, point.Z);
        if (classification != WalkClass.Water) return WalkMath.CanWalk(classification);
        if (!_collision!.Ready(point, WalkTuning.Radius)) return false;
        // A dry bridge is allowed only when the native floor, rather than airborne feet, is above water.
        var support = _collision.SupportY(point.X, point.Z, point.Y + WalkTuning.StepHeight);
        return support is { } hit && float.IsFinite(hit.Y)
            && !WalkSafetyQueries.InWater(spatial, point with { Y = hit.Y });
    }

    private bool IsSafeRecordPoint()
    {
        if (!Grounded || !CollisionReady || _map is null || _collision is null
            || WalkSafetyQueries.InWater(_map.World.Spatial, _feet)) return false;
        if (GetFloorNormal().Y < MathF.Cos(WalkTuning.SafetyRecordSlopeDegrees * (float.Pi / 180f))) return false;
        if (_map.World.Spatial.WalkClassAt(_feet.X, _feet.Z) is not (WalkClass.Walk or WalkClass.Interior)) return false;
        float boundary = FlightBounds.RingDistance(new(_feet.X, _feet.Z), _map.World.Boundary, out _);
        return boundary <= -WalkTuning.BoundaryClearance
            && _admission!.Resolve(_map.World.Spatial, _collision, _feet, _cylinder.Height) is { } floor
            && MathF.Abs(floor.Y - _feet.Y) <= WalkTuning.SaveSupportTolerance;
    }

    private bool NeedsRecovery() => WalkSafetyQueries.NeedsRecovery(_map!.World.Spatial, _feet);

    public bool ReturnToSafety() => Active && Recover();

    private bool Recover()
    {
        if (_map is null) return false;
        var target = _lastStable ?? BodyPose;
        Place(target with { YawDeg = _yaw, PitchDeg = _pitch }, 0);
        _placementStableTried = true;
        SafetyReturnCount++;
        return true;
    }

    private Pose ResolveSettledFeet()
    {
        if (Active && IsSettled) return BodyPose;
        if (_lastStable is { } stable) return stable with { YawDeg = _yaw, PitchDeg = _pitch };
        // Forced saves may precede collision mounting. Preserve the pending origin for the same admission on load.
        if (_hasPlacement && !_safetyFailed) return _placementTarget with { YawDeg = _yaw, PitchDeg = _pitch };
        if (_map is { } map) return DefaultFeet(map.Descriptor.Id);
        throw new InvalidOperationException("Cannot capture walk feet before a map is mounted.");
    }
}

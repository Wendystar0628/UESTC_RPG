using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private Pose EntryCandidate(Pose origin)
    {
        float ground = _collision!.SupportY(origin.Position.X, origin.Position.Z, origin.Position.Y)?.Y
            ?? _map!.World.Spatial.GroundY(origin.Position.X, origin.Position.Z);
        if (origin.Position.Y - ground <= WalkTuning.DirectLandingHeight) return origin;
        var direction = Angles.Forward(origin.YawDeg, origin.PitchDeg);
        var end = origin.Position + direction * WalkTuning.LandingSightDistance;
        var hit = _collision!.SupportRay(origin.Position, end);
        if (SightAllows(hit)) return origin with { Position = hit!.Value.Point };
        if (direction.Y >= 0) return origin;
        // Flight need not have native walking tiles. Terrain only hints where to prefetch;
        // the mounted support ray must confirm the intersection before this becomes a landing.
        float distance = (ground - origin.Position.Y) / direction.Y;
        for (int i = 0; i < WalkTuning.LandingSightHintIterations; i++)
        {
            if (!(distance > 0) || distance > WalkTuning.LandingSightDistance) return origin;
            var hint = origin.Position + direction * distance;
            ground = _map!.World.Spatial.GroundY(hint.X, hint.Z);
            distance = (ground - origin.Position.Y) / direction.Y;
        }
        if (!(distance > 0) || distance > WalkTuning.LandingSightDistance) return origin;
        _entrySightPending = true;
        return origin with { Position = origin.Position + direction * distance };
    }

    private bool SightAllows(SweepHit? hit) => hit is { } support && support.Normal.Y >= MathF.Cos(FloorMaxAngle)
        && WalkSafetyQueries.AllowsFeet(_map!.World.Spatial, support.Point)
        && !WalkSafetyQueries.InWater(_map.World.Spatial, support.Point);

    private bool ResolvePlacement()
    {
        if (!PlacementReady() || !_collision!.Ready(_placementTarget.Position, WalkRules.Radius)) return false;
        if (_entrySightPending && _entryFrom is { } eye)
        {
            _entrySightPending = false;
            var hit = _collision.SupportRay(eye.Position,
                eye.Position + Angles.Forward(eye.YawDeg, eye.PitchDeg) * WalkTuning.LandingSightDistance);
            MovePlacementFocus(SightAllows(hit) ? eye with { Position = hit!.Value.Point } : eye);
            return false;
        }
        var spatial = _map!.World.Spatial;
        var candidate = _placementTarget.Position;
        var floor = _admission!.Resolve(spatial, _collision!, candidate, _cylinder.Height);
        if (floor is { } possible && _placementRelocated && !SafeLanding(possible)) floor = null;
        if (floor is not { } feet)
        {
            if (_placementAlternative < 0) PreparePlacementAlternatives(candidate);
            if (_placementAlternative < _placementAlternatives.Count)
            {
                MovePlacementFocus(_placementAlternatives[_placementAlternative++] with { YawDeg = _yaw, PitchDeg = _pitch });
                _placementRelocated = true;
                return false;
            }
            RequestExit();
            return false;
        }
        bool descend = _entryFrom is not null && !_placementRelocated;
        if (!descend) _placementLift = 0;
        _placementTarget = new(feet, _yaw, _pitch);
        _feet = _previousFeet = feet + N.Vector3.UnitY * _placementLift;
        ResetGait();
        GlobalPosition = _feet.ToGodot();
        ResetPhysicsInterpolation();
        _placementNeedsCheck = false;
        CollisionReady = true;
        if (_placementRelocated && _entryFrom is null && float.IsFinite(_placementSearchRadius))
            GD.PushWarning("[WP-42] walk: invalid feet repaired to a nearby shore safety point.");
        if (descend && _entryFrom is { } from)
        {
            _descentFrom = from;
            float height = from.Position.Y - (feet.Y + EyeHeight);
            float distance = N.Vector3.Distance(from.Position, feet + N.Vector3.UnitY * EyeHeight);
            _descentDuration = distance <= WalkTuning.NearLandingDistance ? WalkTuning.NearLandingSeconds
                : WalkMath.DescentSeconds(MathF.Max(height, distance));
            _descentElapsed = 0;
        }
        else
        {
            _teleportPending |= _placementRelocated;
            _descentFrom = default;
            _descentDuration = _descentElapsed = 0;
            _fov = WalkFov;
            _input = default;
            _jumpQueued = false;
        }
        _entryFrom = null;
        Velocity = descend ? Vector3.Down * WalkTuning.LandingSpeed : Vector3.Zero;
        return true;
    }

    private void MovePlacementFocus(Pose target)
    {
        _placementTarget = target;
        _placementArrival = null;
        _feet = _previousFeet = _placementTarget.Position;
        ResetGait();
        GlobalPosition = _feet.ToGodot();
        ResetPhysicsInterpolation();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
    }

    private bool PlacementReady()
    {
        _mapRuntime ??= KernelServices.Get<IMapRuntime>();
        var current = _mapRuntime.Arrival;
        if (_placementArrival is not null && !ReferenceEquals(current, _placementArrival))
        {
            // An external request cannot silently become this placement's readiness result.
            if (!SamePlacement(current)) { RequestExit(); return false; }
            _placementArrival = current;
        }
        if (_placementArrival is null)
        {
            // Startup, saves and transits already own an arrival at these feet or their eye position.
            // Reuse that exact handle; a repair at another location requests its own destination.
            _placementArrival = SamePlacement(current) ? current : _mapRuntime.Arrive(_placementTarget, ViewMode.Walk, Space);
        }
        var arrival = _placementArrival ?? throw new InvalidOperationException("Walking placement has no arrival.");
        if (arrival.Done.IsFaulted || arrival.Done.IsCanceled) { RequestExit(); return false; }
        return ReferenceEquals(_mapRuntime.Arrival, arrival) && arrival.Done.IsCompletedSuccessfully;
    }

    private bool SamePlacement(IMapArrival? arrival) => arrival?.Destination is { Mode: ViewMode.Walk } at
        && N.Vector2.DistanceSquared(new(at.Pose.Position.X, at.Pose.Position.Z),
            new(_placementTarget.Position.X, _placementTarget.Position.Z)) <= WalkTuning.SaveSupportTolerance * WalkTuning.SaveSupportTolerance
        && MathF.Abs(at.Pose.Position.Y - _placementTarget.Position.Y) <= WalkRules.StandingHeight + WalkRules.StepHeight;

    private bool SafeLanding(N.Vector3 feet)
    {
        if (!_admission!.ClearOfBoundary(feet, _cylinder.Height)) return false;
        var support = _collision!.Sweep(feet + N.Vector3.UnitY * WalkRules.StepHeight,
            feet - N.Vector3.UnitY * WalkTuning.SnapLength, WalkRules.Radius, _cylinder.Height);
        return support is { } hit && hit.Normal.Y > MathF.Cos(WalkTuning.SafetyRecordSlopeDegrees * (float.Pi / 180f));
    }

    private void PreparePlacementAlternatives(N.Vector3 from, float minimumDistance = 0, int skipNewest = 0)
    {
        _placementAlternatives.Clear();
        var map = _map!;
        foreach (var point in _safePoints.Points.Take(Math.Max(0, _safePoints.Count - skipNewest))) Add(point);
        foreach (var spawn in map.Descriptor.Spawns)
            if (spawn.Stand is { } stand) Add(new(stand.Feet, stand.YawDeg, 0));
        var atlas = KernelServices.Get<IAtlasService>();
        if (atlas.Map == map.Descriptor.Id)
            foreach (var marker in atlas.Markers)
                if (marker.Teleport?.Stand is { } stand) Add(new(stand.Feet, stand.YawDeg, 0));
        _placementAlternatives.Sort((a, b) => N.Vector3.DistanceSquared(from, a.Position).CompareTo(N.Vector3.DistanceSquared(from, b.Position)));
        _placementAlternative = 0;

        void Add(Pose pose)
        {
            if (!WalkSafetyQueries.ValidPose(pose)) return;
            float distance = N.Vector3.DistanceSquared(from, pose.Position);
            if (distance <= WalkTuning.SaveSupportTolerance * WalkTuning.SaveSupportTolerance
                || distance < minimumDistance * minimumDistance
                || distance > _placementSearchRadius * _placementSearchRadius) return;
            if (WalkSafetyQueries.InWater(map.World.Spatial, pose.Position)
                || !WalkSafetyQueries.AllowsFeet(map.World.Spatial, pose.Position)) return;
            float boundary = FlightBounds.RingDistance(new(pose.Position.X, pose.Position.Z), map.World.Boundary, out _);
            if (boundary > -WalkTuning.BoundaryClearance || _placementAlternatives.Contains(pose)) return;
            _placementAlternatives.Add(pose);
        }
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
        float radius = _cylinder.Radius;
        int count = Math.Max(1, (int)MathF.Ceiling(motion.Length() / radius));
        for (int i = 1; i <= count; i++)
        {
            var center = _feet + (motion * ((float)i / count)).ToNumerics();
            if (!RegionAllowsPoint(center)
                || !RegionAllowsPoint(center + N.Vector3.UnitX * radius)
                || !RegionAllowsPoint(center - N.Vector3.UnitX * radius)
                || !RegionAllowsPoint(center + N.Vector3.UnitZ * radius)
                || !RegionAllowsPoint(center - N.Vector3.UnitZ * radius)) return false;
        }
        return true;
    }

    private bool RegionAllowsPoint(N.Vector3 point)
    {
        if (!ReadWater(point, out var water)) return false;
        var spatial = _map!.World.Spatial;
        var classification = spatial.WalkClassAt(point.X, point.Z);
        if (classification != WalkClass.Water) return WalkMath.CanWalk(classification);
        if (water.Sample is { CanSwim: true }) return true;
        if (!_collision!.Ready(point, _cylinder.Radius)) return false;
        // A dry bridge is allowed only when the native floor, rather than airborne feet, is above water.
        var support = _collision.SupportY(point.X, point.Z, point.Y + WalkRules.StepHeight);
        return support is { } hit && float.IsFinite(hit.Y)
            && !WalkSafetyQueries.InWater(spatial, point with { Y = hit.Y });
    }

    private bool IsSafeRecordPoint()
    {
        if (_swimming || !Grounded || !CollisionReady || _map is null || _collision is null
            || WalkSafetyQueries.InWater(_map.World.Spatial, _feet)) return false;
        if (GetFloorNormal().Y < MathF.Cos(WalkTuning.SafetyRecordSlopeDegrees * (float.Pi / 180f))) return false;
        if (_map.World.Spatial.WalkClassAt(_feet.X, _feet.Z) is not (WalkClass.Walk or WalkClass.Interior)) return false;
        float boundary = FlightBounds.RingDistance(new(_feet.X, _feet.Z), _map.World.Boundary, out _);
        return boundary <= -WalkTuning.BoundaryClearance
            && _admission!.Resolve(_map.World.Spatial, _collision, _feet, _cylinder.Height) is { } floor
            && MathF.Abs(floor.Y - _feet.Y) <= WalkTuning.SaveSupportTolerance
            && _admission.ClearOfBoundary(_feet, _cylinder.Height);
    }

    private bool NeedsRecovery(WaterQueryResult water) => WalkSafetyQueries.NeedsRecovery(_map!.World.Spatial, _feet,
        water.Sample is { CanSwim: true });

    public bool ReturnToSafety() => Active && _map is not null && _appFlow?.IsPlaying == true
        && (_appFlow.Activity & AppActivity.MapTransition) == 0 && Recover(manual: true);

    private bool Recover(bool manual = false)
    {
        if (_map is null) { RequestExit(); return false; }
        if (_sinceReturn > WalkTuning.SafetyReturnRepeatSeconds) _returnSkip = 0;
        float minimumDistance = manual ? WalkTuning.SafetyReturnMinimumDistance : 0;
        Pose? target = manual && _returnSkip > 0
            ? _safePoints.Nearest(_feet, minimumDistance, _returnSkip) : null;
        // A map with no history first tries real support directly below the body.
        if (target is null && _safePoints.Count == 0 && _collision!.Ready(_feet, WalkRules.Radius)
            && _collision.SupportY(_feet.X, _feet.Z, _feet.Y) is { } support)
        {
            var below = _feet with { Y = support.Y };
            if (_admission!.Resolve(_map.World.Spatial, _collision, below, _cylinder.Height) is { } floor
                && SafeLanding(floor)) target = new(floor, _yaw, _pitch);
        }
        if (target is null)
        {
            _placementSearchRadius = float.PositiveInfinity;
            PreparePlacementAlternatives(_feet, minimumDistance, manual ? _returnSkip : 0);
            if (_placementAlternatives.Count > 0) target = _placementAlternatives[0];
        }
        if (target is null) { RequestExit(); return false; }
        var history = _safePoints.Capture();
        int selected = Array.LastIndexOf(history, target.Value);
        _returnSkip = selected >= 0 ? history.Length - selected : 0;
        Place(target.Value with { YawDeg = _yaw, PitchDeg = _pitch }, 0);
        _placementRelocated = true;
        _sinceReturn = 0;
        SafetyReturnCount++;
        return true;
    }

    private Pose ResolveSettledFeet()
    {
        if (Active && IsSettled) return BodyPose;
        if (_safePoints.Nearest(_feet) is { } stable) return stable with { YawDeg = _yaw, PitchDeg = _pitch };
        if (_hasPlacement && !_placementNeedsCheck && !_safetyFailed
            && _map is { } placedMap && !WalkSafetyQueries.InWater(placedMap.World.Spatial, _placementTarget.Position))
            return _placementTarget with { YawDeg = _yaw, PitchDeg = _pitch };
        if (_map is { } map) return DefaultFeet(map.Descriptor.Id);
        throw new InvalidOperationException("Cannot capture walk feet before a map is mounted.");
    }
}

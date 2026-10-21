using Campus.Contracts.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Placement admission, arrival ownership, alternatives and chronological safety history.</summary>
internal sealed class WalkPlacement(CharacterBody3D body, Func<NavSpace?> space)
{
    private IMapContext? _map;
    private ICollisionWorld? _collision;
    private WalkPlacementAdmission? _admission;
    private IMapRuntime? _mapRuntime;
    private readonly List<Pose> _placementAlternatives = [];
    private int _placementAlternative;
    private IMapArrival? _placementArrival;
    private bool _placementRelocated;
    private float _placementSearchRadius;
    private bool _entrySightPending;
    private float _sinceReturn = float.PositiveInfinity;
    private int _returnSkip;
    private Pose _placementTarget;
    private bool _hasPlacement;
    private bool _placementNeedsCheck;
    private float _placementLift;
    private Pose? _entryFrom;
    private bool _safetyFailed;
    private float _sinceSafe;
    private bool _entryCandidatePending;
    private readonly WalkSafePoints _safePoints = new();

    public bool HasPlacement => _hasPlacement;
    public bool NeedsCheck => _placementNeedsCheck;
    public bool ExitRequested => _safetyFailed;
    public Pose? EntryFrom => _entryFrom;
    public bool EntryCandidatePending => _entryCandidatePending;
    public IMapArrival? Arrival => _placementArrival;
    public WalkSafePoints History => _safePoints;
    public int SafetyReturnCount { get; private set; }

    public void Bind(IMapContext? map, ICollisionWorld collision)
    {
        _map = map;
        _collision = collision;
    }

    public void Attach(CharacterBody3D body) => _admission = new(body);

    public void Detach()
    {
        _admission?.Dispose();
        _admission = null;
    }

    public void Begin(Pose feet, float lift, float searchRadius)
    {
        _placementTarget = feet;
        _hasPlacement = true;
        _sinceSafe = 0;
        _placementNeedsCheck = true;
        _placementLift = lift;
        _placementSearchRadius = searchRadius;
        _placementArrival = null;
        _placementRelocated = false;
        _entrySightPending = false;
        _placementAlternatives.Clear();
        _placementAlternative = -1;
        _entryFrom = null;
        _safetyFailed = false;
    }

    public void BeginEntry(Pose from)
    {
        _entryFrom = from;
        _entryCandidatePending = true;
    }

    public void CompleteEntryCandidate() => _entryCandidatePending = false;

    private void ClearEntry()
    {
        _entryFrom = null;
        _entryCandidatePending = false;
    }

    public void Leave()
    {
        _placementArrival = null;
        _entrySightPending = false;
        ClearEntry();
    }

    public void Fail()
    {
        _safetyFailed = true;
        ClearEntry();
    }

    public void WorldChanged()
    {
        _safePoints.Clear();
        _placementArrival = null;
    }

    public void AdvanceSafetyClocks(float dt)
    {
        _sinceSafe += dt;
        _sinceReturn += dt;
    }

    public void RecordSafety(Pose bodyPose, bool grounded, bool safe)
    {
        if (!grounded || _sinceSafe < WalkTuning.SafetyRecordSeconds || !safe) return;
        _safePoints.Record(bodyPose);
        _sinceSafe = 0;
    }

    public bool ShouldRecordSafety(bool grounded) => grounded && _sinceSafe >= WalkTuning.SafetyRecordSeconds;

    public void MoveFocus(Pose target)
    {
        _placementTarget = target;
        _placementArrival = null;
    }

    public Pose EntryCandidate(Pose origin)
    {
        float ground = _collision!.SupportY(origin.Position.X, origin.Position.Z, origin.Position.Y)?.Y
            ?? _map!.World.Spatial.GroundY(origin.Position.X, origin.Position.Z);
        if (origin.Position.Y - ground <= WalkTuning.DirectLandingHeight) return origin;
        var direction = Angles.Forward(origin.YawDeg, origin.PitchDeg);
        var end = origin.Position + direction * WalkRules.LandingSightDistance;
        var hit = _collision!.SupportRay(origin.Position, end);
        if (SightAllows(hit)) return origin with { Position = hit!.Value.Point };
        if (direction.Y >= 0) return origin;
        // Flight need not have native walking tiles. Terrain only hints where to prefetch;
        // the mounted support ray must confirm the intersection before this becomes a landing.
        float distance = (ground - origin.Position.Y) / direction.Y;
        for (int i = 0; i < WalkTuning.LandingSightHintIterations; i++)
        {
            if (!(distance > 0) || distance > WalkRules.LandingSightDistance) return origin;
            var hint = origin.Position + direction * distance;
            ground = _map!.World.Spatial.GroundY(hint.X, hint.Z);
            distance = (ground - origin.Position.Y) / direction.Y;
        }
        if (!(distance > 0) || distance > WalkRules.LandingSightDistance) return origin;
        _entrySightPending = true;
        return origin with { Position = origin.Position + direction * distance };
    }

    private bool SightAllows(SweepHit? hit) => hit is { } support && support.Normal.Y >= MathF.Cos(body.FloorMaxAngle)
        && WalkSafetyQueries.AllowsFeet(_map!.World.Spatial, support.Point)
        && !WalkSafetyQueries.InWater(_map.World.Spatial, support.Point);

    public WalkPlacementResult Resolve(float yaw, float pitch, float height)
    {
        if (!PlacementReady() || !_collision!.Ready(_placementTarget.Position, WalkRules.Radius)) return default;
        if (_entrySightPending && _entryFrom is { } eye)
        {
            _entrySightPending = false;
            var hit = _collision.SupportRay(eye.Position,
                eye.Position + Angles.Forward(eye.YawDeg, eye.PitchDeg) * WalkRules.LandingSightDistance);
            return new(WalkPlacementStatus.Refocused, SightAllows(hit) ? eye with { Position = hit!.Value.Point } : eye);
        }
        var candidate = _placementTarget.Position;
        var floor = _admission!.Resolve(_map!.World.Spatial, _collision!, candidate, height);
        if (floor is { } possible && _placementRelocated && !SafeLanding(possible, height)) floor = null;
        if (floor is not { } feet)
        {
            if (_placementAlternative < 0) PreparePlacementAlternatives(candidate);
            if (_placementAlternative < _placementAlternatives.Count)
            {
                var target = _placementAlternatives[_placementAlternative++] with { YawDeg = yaw, PitchDeg = pitch };
                return new(WalkPlacementStatus.Refocused, target, Relocated: true);
            }
            Fail();
            return default;
        }
        bool descend = _entryFrom is not null && !_placementRelocated;
        if (!descend) _placementLift = 0;
        _placementTarget = new(feet, yaw, pitch);
        return new(WalkPlacementStatus.Resolved, _placementTarget,
            feet + N.Vector3.UnitY * _placementLift, descend ? _entryFrom : null, _placementRelocated);
    }

    public void CommitResolution() => _placementNeedsCheck = false;

    public void WarnIfRepaired()
    {
        if (_placementRelocated && _entryFrom is null && float.IsFinite(_placementSearchRadius))
            KernelServices.Get<IDiagnosticLog>().Warning("", "walk: invalid feet repaired to a nearby shore safety point.");
    }

    public void FinishResolution() => _entryFrom = null;

    public void MarkRelocated() => _placementRelocated = true;

    private bool PlacementReady()
    {
        _mapRuntime ??= KernelServices.Get<IMapRuntime>();
        var current = _mapRuntime.Arrival;
        if (_placementArrival is not null && !ReferenceEquals(current, _placementArrival))
        {
            // An external request cannot silently become this placement's readiness result.
            if (!SamePlacement(current)) { Fail(); return false; }
            _placementArrival = current;
        }
        if (_placementArrival is null)
        {
            // Startup, saves and transits already own an arrival at these feet or their eye position.
            // Reuse that exact handle; a repair at another location requests its own destination.
            _placementArrival = SamePlacement(current) ? current : _mapRuntime.Arrive(_placementTarget, ViewMode.Walk, space());
        }
        var arrival = _placementArrival ?? throw new InvalidOperationException("Walking placement has no arrival.");
        if (arrival.Done.IsFaulted || arrival.Done.IsCanceled) { Fail(); return false; }
        return ReferenceEquals(_mapRuntime.Arrival, arrival) && arrival.Done.IsCompletedSuccessfully;
    }

    private bool SamePlacement(IMapArrival? arrival) => arrival?.Destination is { Mode: ViewMode.Walk } at
        && N.Vector2.DistanceSquared(new(at.Pose.Position.X, at.Pose.Position.Z),
            new(_placementTarget.Position.X, _placementTarget.Position.Z)) <= WalkTuning.SaveSupportTolerance * WalkTuning.SaveSupportTolerance
        && MathF.Abs(at.Pose.Position.Y - _placementTarget.Position.Y) <= WalkRules.StandingHeight + WalkRules.StepHeight;

    private bool SafeLanding(N.Vector3 feet, float height)
    {
        if (!_admission!.ClearOfBoundary(feet, height)) return false;
        var support = _collision!.Sweep(feet + N.Vector3.UnitY * WalkRules.StepHeight,
            feet - N.Vector3.UnitY * WalkTuning.SnapLength, WalkRules.Radius, height);
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

    public bool IsSafeRecordPoint(WalkMotionSample sample, bool swimming, bool collisionReady, float height)
    {
        if (swimming || !sample.Grounded || !collisionReady || _map is null || _collision is null
            || WalkSafetyQueries.InWater(_map.World.Spatial, sample.Feet)) return false;
        if (body.GetFloorNormal().Y < MathF.Cos(WalkTuning.SafetyRecordSlopeDegrees * (float.Pi / 180f))) return false;
        if (_map.World.Spatial.WalkClassAt(sample.Feet.X, sample.Feet.Z) is not (WalkClass.Walk or WalkClass.Interior)) return false;
        float boundary = FlightBounds.RingDistance(new(sample.Feet.X, sample.Feet.Z), _map.World.Boundary, out _);
        return boundary <= -WalkTuning.BoundaryClearance
            && _admission!.Resolve(_map.World.Spatial, _collision, sample.Feet, height) is { } floor
            && MathF.Abs(floor.Y - sample.Feet.Y) <= WalkTuning.SaveSupportTolerance
            && _admission.ClearOfBoundary(sample.Feet, height);
    }

    public bool NeedsRecovery(N.Vector3 feet, WaterQueryResult water) => WalkSafetyQueries.NeedsRecovery(_map!.World.Spatial, feet,
        water.Sample is { CanSwim: true });

    public Pose? RecoveryTarget(Pose bodyPose, float height, bool manual)
    {
        if (_map is null) { Fail(); return null; }
        var feet = bodyPose.Position;
        if (_sinceReturn > WalkTuning.SafetyReturnRepeatSeconds) _returnSkip = 0;
        float minimumDistance = manual ? WalkTuning.SafetyReturnMinimumDistance : 0;
        Pose? target = manual && _returnSkip > 0
            ? _safePoints.Nearest(feet, minimumDistance, _returnSkip) : null;
        // A map with no history first tries real support directly below the body.
        if (target is null && _safePoints.Count == 0 && _collision!.Ready(feet, WalkRules.Radius)
            && _collision.SupportY(feet.X, feet.Z, feet.Y) is { } support)
        {
            var below = feet with { Y = support.Y };
            if (_admission!.Resolve(_map.World.Spatial, _collision, below, height) is { } floor
                && SafeLanding(floor, height)) target = new(floor, bodyPose.YawDeg, bodyPose.PitchDeg);
        }
        if (target is null)
        {
            _placementSearchRadius = float.PositiveInfinity;
            PreparePlacementAlternatives(feet, minimumDistance, manual ? _returnSkip : 0);
            if (_placementAlternatives.Count > 0) target = _placementAlternatives[0];
        }
        if (target is null) { Fail(); return null; }
        var history = _safePoints.Capture();
        int selected = Array.LastIndexOf(history, target.Value);
        _returnSkip = selected >= 0 ? history.Length - selected : 0;
        return target.Value with { YawDeg = bodyPose.YawDeg, PitchDeg = bodyPose.PitchDeg };
    }

    public void CommitRecovery()
    {
        _placementRelocated = true;
        _sinceReturn = 0;
        SafetyReturnCount++;
    }

    public Pose ResolveSettledFeet(bool active, bool settled, Pose bodyPose)
    {
        if (active && settled) return bodyPose;
        if (_safePoints.Nearest(bodyPose.Position) is { } stable) return stable with { YawDeg = bodyPose.YawDeg, PitchDeg = bodyPose.PitchDeg };
        if (_hasPlacement && !_placementNeedsCheck && !_safetyFailed
            && _map is { } placedMap && !WalkSafetyQueries.InWater(placedMap.World.Spatial, _placementTarget.Position))
            return _placementTarget with { YawDeg = bodyPose.YawDeg, PitchDeg = bodyPose.PitchDeg };
        if (_map is { } map) return DefaultFeet(map.Descriptor.Id);
        throw new InvalidOperationException("Cannot capture walk feet before a map is mounted.");
    }

    private static Pose DefaultFeet(MapId targetMap) => WalkSafetyQueries.DefaultFeet(KernelServices.Get<IMapService>(), targetMap);

    public void Unload()
    {
        _map = null;
        _safePoints.Clear();
        _placementAlternatives.Clear();
        _placementArrival = null;
        _entryFrom = null;
        _placementNeedsCheck = false;
        _hasPlacement = false;
        _sinceSafe = 0;
    }
}

internal enum WalkPlacementStatus { Pending, Refocused, Resolved }
internal readonly record struct WalkPlacementResult(WalkPlacementStatus Status, Pose Target,
    N.Vector3 Feet = default, Pose? DescentFrom = null, bool Relocated = false);

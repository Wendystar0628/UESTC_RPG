using Campus.Contracts.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Placement admission, arrival ownership, alternatives and chronological safety history.</summary>
internal sealed class WalkPlacement(CharacterBody3D body, Func<NavSpace?> space, Func<Pose> eye)
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
    private float _sinceReturn = float.PositiveInfinity;
    private int _returnSkip;
    private Pose _placementTarget;
    private bool _hasPlacement;
    private bool _placementNeedsCheck;
    private bool _safetyFailed;
    private float _sinceSafe;
    private bool _entryPending;
    private readonly WalkSafePoints _safePoints = new();

    public bool HasPlacement => _hasPlacement;
    public bool NeedsCheck => _placementNeedsCheck;
    public bool ExitRequested => _safetyFailed;
    public bool EntryPending => _entryPending;
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

    public void Begin(Pose feet, float searchRadius)
    {
        _placementTarget = feet;
        _hasPlacement = true;
        _sinceSafe = 0;
        _placementNeedsCheck = true;
        _placementSearchRadius = searchRadius;
        _placementArrival = null;
        _placementRelocated = false;
        _placementAlternatives.Clear();
        _placementAlternative = -1;
        _entryPending = false;
        _safetyFailed = false;
    }

    public void BeginEntry() { _entryPending = true; _placementRelocated = true; }

    public void Leave()
    {
        _placementArrival = null;
        _entryPending = false;
    }

    public void Fail()
    {
        _safetyFailed = true;
        _entryPending = false;
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

    public WalkPlacementResult Resolve(float yaw, float pitch, float height)
    {
        if (!PlacementReady() || !_collision!.Ready(_placementTarget.Position, WalkRules.Radius)) return default;
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
        // Support admission can adjust the candidate floor. Select that actual eye/feet endpoint
        // before accepting readiness; the next pass adopts only its matching arrival.
        if (feet != _placementTarget.Position)
            return new(WalkPlacementStatus.Refocused, new(feet, yaw, pitch));
        _placementTarget = new(feet, yaw, pitch);
        return new(WalkPlacementStatus.Resolved, _placementTarget, feet, _placementRelocated);
    }

    public void CommitResolution() => _placementNeedsCheck = false;

    public void WarnIfRepaired()
    {
        if (_placementRelocated && !_entryPending && float.IsFinite(_placementSearchRadius))
            KernelServices.Get<IDiagnosticLog>().Warning("", "walk: invalid feet repaired to a nearby shore safety point.");
    }

    public void FinishResolution() => _entryPending = false;

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
            // Placement owns a single eye/FOV plus feet destination. Reuse only that exact endpoint;
            // a genuine safety relocation selects another destination.
            _placementArrival = SamePlacement(current) ? current : _mapRuntime.Arrive(eye(), ViewMode.Walk, space(), _placementTarget.Position);
        }
        var arrival = _placementArrival ?? throw new InvalidOperationException("Walking placement has no arrival.");
        if (arrival.Done.IsFaulted || arrival.Done.IsCanceled) { Fail(); return false; }
        return ReferenceEquals(_mapRuntime.Arrival, arrival) && arrival.Done.IsCompletedSuccessfully;
    }

    private bool SamePlacement(IMapArrival? arrival) => arrival?.Destination is { Mode: ViewMode.Walk } at
        && at.Pose == eye() && at.CollisionFeet == _placementTarget.Position;

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
        foreach (var feature in map.World.Features.Keys.OrderBy(id => id.Value, StringComparer.Ordinal))
            if (map.World.Interiors.Get(feature) is { } interior)
                foreach (var entrance in interior.Entrances) Add(new(entrance.Outside, 0, 0));
        var projected = new N.Vector2(from.X, from.Z);
        _placementAlternatives.Sort((a, b) =>
            N.Vector2.DistanceSquared(projected, new(a.Position.X, a.Position.Z))
                .CompareTo(N.Vector2.DistanceSquared(projected, new(b.Position.X, b.Position.Z))));
        _placementAlternative = 0;

        void Add(Pose pose)
        {
            if (!WalkSafetyQueries.ValidPose(pose)) return;
            float distance = N.Vector3.DistanceSquared(from, pose.Position);
            if (distance <= WalkTuning.SaveSupportTolerance * WalkTuning.SaveSupportTolerance
                || distance < minimumDistance * minimumDistance
                || distance > _placementSearchRadius * _placementSearchRadius) return;
            // Future candidates have no mounted terrain/collision yet. Common placement admission
            // checks water, region permission, support and body clearance after selecting one.
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

    public Pose? RecoveryTarget(Pose bodyPose, bool manual)
    {
        if (_map is null) { Fail(); return null; }
        var feet = bodyPose.Position;
        if (_sinceReturn > WalkTuning.SafetyReturnRepeatSeconds) _returnSkip = 0;
        float minimumDistance = manual ? WalkTuning.SafetyReturnMinimumDistance : 0;
        Pose? target = manual && _returnSkip > 0
            ? _safePoints.Nearest(feet, minimumDistance, _returnSkip) : null;
        // With no history, common placement admission resolves support below this origin
        // after its collision is mounted, then uses the same registered alternatives if needed.
        if (_safePoints.Count == 0) return bodyPose;
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
        _entryPending = false;
        _placementNeedsCheck = false;
        _hasPlacement = false;
        _sinceSafe = 0;
    }
}

internal enum WalkPlacementStatus { Pending, Refocused, Resolved }
internal readonly record struct WalkPlacementResult(WalkPlacementStatus Status, Pose Target,
    N.Vector3 Feet = default, bool Relocated = false);

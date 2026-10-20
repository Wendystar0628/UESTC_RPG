using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

/// <summary>Mounted flight movement, prepared terrain projection and edge fog.</summary>
internal sealed class ObserverFlight
{
    private readonly FlightTuning _tuning;
    private readonly FlightMotion _motion;
    private ITerrainPreparation? _terrain;
    private Task? _terrainPending;
    private Task<Pose>? _flowProjection;
    private Task<N.Vector3>? _projection;
    private N.Vector3 _projectedVelocity;
    private float _projectedFog;
    private FlightBounds? _bounds;
    private bool _automated;

    public ObserverFlight(FlightTuning tuning)
    {
        _tuning = tuning;
        _motion = new(tuning);
        SpeedStep = tuning.DefaultSpeedStep;
    }

    public IMapContext? Map { get; private set; }
    public N.Vector3 Velocity => _motion.Velocity;
    public int SpeedStep { get; private set; }
    public float EdgeFog { get; private set; }
    public bool IsSettled => _flowProjection is null && _projection is null && _terrainPending is null;

    public void Initialize(ITerrainPreparation terrain, bool automated)
    {
        _terrain = terrain;
        _automated = automated;
    }

    public void Mount(IMapContext context)
    {
        Map = context;
        _bounds = new(context, context.World.Boundary);
    }

    public void Unmount()
    {
        _bounds = null;
        Map = null;
        SetEdgeFog(0);
        ClearProjection();
    }

    public Task Prepare(IMapContext context, Pose pose)
    {
        var point = new N.Vector2(pose.Position.X, pose.Position.Z);
        return Terrain.PreparePoints(context, "camera.presentation", new[] { point });
    }

    public async Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default)
    {
        ObserverPresentation.ValidateFinite(pose);
        return _bounds is null ? pose : pose with
        {
            Position = await _bounds.ProjectPrepared(pose.Position, Terrain, cancellation)
        };
    }

    public bool IsLegal(Pose pose, bool outsideBuffer)
    {
        try { ObserverPresentation.ValidateFinite(pose); }
        catch (ArgumentOutOfRangeException) { return false; }
        return _bounds?.IsLegal(pose.Position, outsideBuffer) ?? false;
    }

    public void BeginFlowProjection(Pose pose) => _flowProjection = LegalizePrepared(pose);

    public bool AdvanceFlowProjection(ObserverPresentation presentation)
    {
        if (_flowProjection is not { } pending) return true;
        if (!pending.IsCompleted) return false;
        presentation.ApplyProjection(pending.GetAwaiter().GetResult());
        _flowProjection = null;
        return true;
    }

    public void Advance(Pose pose, ObserverInputState input, float seconds, ObserverPresentation presentation)
    {
        if (Map is null || _bounds is null) return;
        if (_projection is { } pending)
        {
            if (!pending.IsCompleted) return;
            ApplyPosition(pending.GetAwaiter().GetResult(), _projectedFog, seconds, presentation);
            _motion.ConstrainVelocity(_projectedVelocity);
            _projection = null;
            return;
        }
        if (!TerrainReady(pose.Position)) return;
        var move = input.Move;
        var displacement = _motion.Step(pose, new(move.X, input.Vertical, move.Y), input.Sprint,
            SpeedStep, Map.World.Spatial.GroundY(pose.Position.X, pose.Position.Z), seconds);
        var filtered = _bounds.FilterVelocity(pose.Position, displacement, out float fog);
        _motion.ConstrainVelocity(_bounds.FilterVelocity(pose.Position, _motion.Velocity, out _));
        var target = pose.Position + filtered;
        if (!TerrainReady(target, pose.Position))
        {
            _projectedVelocity = _motion.Velocity;
            _projectedFog = fog;
            _projection = ProjectAfterTerrain(target, _terrainPending!, _bounds);
            return;
        }
        // Projection can cross a terrain-block boundary even when the requested move does not.
        var position = target;
        if (!_bounds.IsLegal(target))
        {
            _projectedVelocity = _motion.Velocity;
            _projectedFog = fog;
            _projection = _bounds.ProjectPrepared(target, Terrain);
            if (!_projection.IsCompleted) return;
            position = _projection.GetAwaiter().GetResult();
            _projection = null;
        }
        ApplyPosition(position, fog, seconds, presentation);
    }

    public void Reset() => _motion.Reset();
    public void DiscardMotionProjection() => _projection = null;

    public void ClearProjection()
    {
        _projection = null;
        _terrainPending = null;
        _flowProjection = null;
    }

    public void ConstrainVelocity(N.Vector3 velocity) => _motion.ConstrainVelocity(velocity);
    public void SetSpeedStep(int step) => SpeedStep = Math.Clamp(step, 1, _tuning.SpeedSteps);
    public void ChangeSpeed(int steps) => SetSpeedStep(SpeedStep + steps);

    public void SetEdgeFog(float value)
    {
        EdgeFog = Math.Clamp(value, 0, 1);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.EdgeFog, EdgeFog);
    }

    private ITerrainPreparation Terrain => _terrain ??= KernelServices.Get<ITerrainPreparation>();

    private bool TerrainReady(N.Vector3 point, N.Vector3? from = null)
    {
        if (Map is null) return false;
        if (_terrainPending is { } pending)
        {
            if (!pending.IsCompleted) return false;
            pending.GetAwaiter().GetResult();
            _terrainPending = null;
        }
        var terrain = Terrain;
        var at = new N.Vector2(point.X, point.Z);
        if (terrain.IsPrepared(Map.World, at)) return true;
        var start = from is { } p ? new N.Vector2(p.X, p.Z) : at;
        _terrainPending = terrain.PrepareRegion(Map, new(N.Vector2.Min(start, at), N.Vector2.Max(start, at)));
        if (!_terrainPending.IsCompleted) return false;
        _terrainPending.GetAwaiter().GetResult();
        _terrainPending = null;
        return true;
    }

    private async Task<N.Vector3> ProjectAfterTerrain(N.Vector3 target, Task preparation, FlightBounds bounds)
    {
        await preparation;
        return await bounds.ProjectPrepared(target, _terrain!);
    }

    private void ApplyPosition(N.Vector3 position, float fog, float seconds, ObserverPresentation presentation)
    {
        presentation.MoveTo(position);
        var medium = Map!.World.Spatial.MediumAt(position).Kind;
        if (_automated || medium is MediumKind.Interior or MediumKind.Underground or MediumKind.Underwater) fog = 0;
        SetEdgeFog(fog + (EdgeFog - fog) * MathF.Exp(-seconds / _tuning.FogTimeConstant));
    }
}

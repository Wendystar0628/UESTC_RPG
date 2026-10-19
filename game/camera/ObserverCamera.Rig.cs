using Campus.Contracts;
using N = System.Numerics;

namespace Campus.Camera;

public partial class ObserverCamera
{
    public float RollDeg => IsPlaying && _mode == ViewMode.Walk ? _walk?.EyeRollDeg ?? 0 : 0;
    // Application flow owns a presentation pose while the restored walk body stays frozen.
    public Pose Pose => !IsPlaying || _mode is null ? _pose : Mode switch
    {
        ViewMode.Fly => _pose,
        ViewMode.Walk => _walk?.EyePose ?? throw new InvalidOperationException("Walk mode requires its physical eye pose."),
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public float Speed => _mode switch
    {
        null => 0,
        ViewMode.Fly => _flight.Velocity.Length(),
        ViewMode.Walk => _walk?.Velocity.Length() ?? 0,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    // Early UI reads can precede _Ready; the ruleset is loaded before any runtime node.
    // The nullable field still prevents input/physics from treating this as an applied mode.
    public ViewMode Mode => _mode ?? (_rules ?? Campus.Kernel.Registry.Kernel.Get<IRuleset>()).Current.DefaultViewMode;
    public int SpeedStep => _mode is null ? Mode switch
    {
        ViewMode.Fly => Tuning.DefaultSpeedStep,
        ViewMode.Walk => WalkRules.DefaultSpeedStep,
        _ => throw new InvalidOperationException("Unknown default view mode.")
    } : Mode switch
    {
        ViewMode.Fly => _flySpeedStep,
        ViewMode.Walk => _walk?.SpeedStep ?? throw new InvalidOperationException("Walk mode requires its speed setting."),
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public ITransitHandle? ActiveTransit { get; private set; }
    public float EdgeFog { get; private set; }
    public N.Vector3 Feet => _mode switch
    {
        null => _pose.Position,
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.BodyPose.Position ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public N.Vector3 Eye => _mode switch
    {
        null => _pose.Position,
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.ObserverEye ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public float YawDeg => _mode switch
    {
        null => _pose.YawDeg,
        ViewMode.Fly => Pose.YawDeg,
        ViewMode.Walk => _walk?.BodyPose.YawDeg ?? Pose.YawDeg,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public NavSpace? Space => _mode switch
    {
        null => null,
        ViewMode.Fly => _map?.World.Spatial.MediumAt(Pose.Position) is { Kind: MediumKind.Interior, Feature: { } feature } medium
            ? new NavSpace(feature, checked((short)medium.Storey)) : null,
        ViewMode.Walk => _walk?.Space,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public bool IsSettled => _mode is not null && ActiveTransit is null && _walkEntry is null
        && _flowProjection is null && _flightProjection is null && _terrainPending is null && (Mode switch
    {
        ViewMode.Fly => true,
        ViewMode.Walk => _walk?.IsSettled ?? false,
        _ => throw new InvalidOperationException("Unknown view mode.")
    });

    public void SetPose(Pose pose)
    {
        ValidateFinite(pose);
        _flightProjection = null; _terrainPending = null; _flowProjection = null;
        _pose = pose with { FovDeg = pose.FovDeg ?? (Mode switch
        {
            ViewMode.Fly => FlyFov,
            ViewMode.Walk => WalkFov,
            _ => throw new InvalidOperationException("Unknown view mode.")
        }) };
        _flyFovFrom = _pose.FovDeg.Value;
        _flyFovTarget = FlyFov;
        _flyFovElapsed = Tuning.ModeFovSeconds;
        _flight.Reset();
        _listener?.Sync(reset: true);
    }

    public ITransitHandle Transit(Pose to, TransitOptions? options = null)
    {
        if (_transit is null || !_transit.IsReady)
            throw new InvalidOperationException("The production transit driver is not ready.");
        ValidateFinite(to);
        ActiveTransit?.Cancel();
        _input.Clear(); _flight.Reset();
        ActiveTransit = _transit.Start(Pose, to, options);
        return ActiveTransit;
    }

    public Task<ModeSwitchResult> SetMode(ViewMode mode, ModeSwitchVia via)
    {
        _ = mode switch { ViewMode.Fly or ViewMode.Walk => 0, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        if (_walkEntry is not null)
        {
            CancelWalkEntry();
            return Task.FromResult(ModeSwitchResult.Refused);
        }
        if (_rules is null || !_rules.Current.ViewModes.Contains(mode)) return Task.FromResult(ModeSwitchResult.Refused);
        if (mode == _mode) return Task.FromResult(ModeSwitchResult.Ok);
        ActiveTransit?.Cancel(); ActiveTransit = null;
        switch (mode)
        {
            case ViewMode.Walk:
                if (_walk is null) return Task.FromResult(ModeSwitchResult.Refused);
                var result = _walk.Enter(Pose);
                if (result == ModeSwitchResult.Refused)
                { WalkEntryRefused?.Invoke(); return Task.FromResult(result); }
                _walkEntry = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _flight.Reset(); _input.Clear(); SetEdgeFog(0);
                return _walkEntry.Task;
            case ViewMode.Fly:
                return SwitchToFly();
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private async Task<ModeSwitchResult> SwitchToFly()
    {
        bool landingRefused = _walk?.ExitRequested == true && !PlacementReadFailed();
        var map = _map;
        var origin = _walk is { Active: true } ? _walk.EyePose : Pose;
        var pose = await LegalizePrepared(origin);
        if (!ReferenceEquals(map, _map)) return ModeSwitchResult.Refused;
        N.Vector3 inheritedVelocity = _walk is { Active: true } body ? new(body.Velocity.X, 0, body.Velocity.Z) : default;
        _walk?.Leave();
        _pose = pose;
        _flyFovFrom = _pose.FovDeg ?? FlyFov;
        _flyFovTarget = FlyFov; _flyFovElapsed = 0;
        _mode = ViewMode.Fly; _flight.Reset(); _input.Clear(); SetEdgeFog(0);
        _flight.ConstrainVelocity(inheritedVelocity);
        if (landingRefused) WalkEntryRefused?.Invoke();
        return ModeSwitchResult.Ok;
    }

    private bool AdvanceWalkEntry()
    {
        if (_walkEntry is not { } completion || _walk is null) return false;
        _walk.SetLook(_pose.YawDeg, _pose.PitchDeg);
        if (_walk.ExitRequested)
        {
            bool readFailed = PlacementReadFailed();
            _walkEntry = null;
            _walk.Leave();
            completion.TrySetResult(ModeSwitchResult.Refused);
            if (!readFailed) WalkEntryRefused?.Invoke();
            return false;
        }
        if (!_walk.CollisionReady) return true;
        _walkEntry = null;
        _mode = ViewMode.Walk;
        _flight.Reset(); SetEdgeFog(0);
        bool relocated = _walk.BodyPose.Position.X != _pose.Position.X || _walk.BodyPose.Position.Z != _pose.Position.Z;
        completion.TrySetResult(relocated ? ModeSwitchResult.Relocated : ModeSwitchResult.Ok);
        return false;
    }

    private void CancelWalkEntry()
    {
        if (_walkEntry is not { } completion) return;
        _walkEntry = null;
        _walk?.Leave();
        completion.TrySetResult(ModeSwitchResult.Refused);
    }

    private bool PlacementReadFailed() => _walk?.PlacementArrival?.Done is { IsFaulted: true } or { IsCanceled: true };
}

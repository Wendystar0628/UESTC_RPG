using Campus.Contracts;
using N = System.Numerics;

namespace Campus.Camera;

public partial class ObserverCamera
{
    public Pose Pose => _pose;
    public float Speed => _mode switch
    {
        ViewMode.Fly => _flight.Velocity.Length(),
        ViewMode.Walk => _walk?.Velocity.Length() ?? 0,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public ViewMode Mode => _mode;
    public int SpeedStep => _speedStep;
    public ITransitHandle? ActiveTransit { get; private set; }
    public float EdgeFog { get; private set; }
    public N.Vector3 Feet => Mode switch
    {
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.BodyPose.Position ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public N.Vector3 Eye => Mode switch
    {
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.ObserverEye ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public float YawDeg => Mode switch
    {
        ViewMode.Fly => Pose.YawDeg,
        ViewMode.Walk => _walk?.BodyPose.YawDeg ?? Pose.YawDeg,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public NavSpace? Space => Mode switch
    {
        ViewMode.Fly => _map?.World.Spatial.MediumAt(Pose.Position) is { Kind: MediumKind.Interior, Feature: { } feature } medium
            ? new NavSpace(feature, checked((short)medium.Storey)) : null,
        ViewMode.Walk => _walk?.Space,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public bool IsSettled => ActiveTransit is null && (Mode switch
    {
        ViewMode.Fly => true,
        ViewMode.Walk => _walk?.IsSettled ?? false,
        _ => throw new InvalidOperationException("Unknown view mode.")
    });

    public void SetPose(Pose pose)
    {
        ValidateFinite(pose);
        _pose = pose with { FovDeg = pose.FovDeg ?? Tuning.FlyFov };
        _flight.Reset();
        _listener?.Sync(reset: true);
    }

    public ITransitHandle Transit(Pose to, TransitOptions? options = null)
    {
        if (_transit is null || !_transit.IsReady)
            throw new InvalidOperationException("The production transit driver is not ready.");
        ValidateFinite(to);
        if (!IsLegal(to)) throw new ArgumentOutOfRangeException(nameof(to), "Transit destination is outside the reachable volume.");
        ActiveTransit?.Cancel();
        _input.Clear(); _flight.Reset();
        _transitDestination = to;
        ActiveTransit = _transit.Start(Pose, to, options);
        return ActiveTransit;
    }

    public Task<ModeSwitchResult> SetMode(ViewMode mode, ModeSwitchVia via)
    {
        _ = mode switch { ViewMode.Fly or ViewMode.Walk => 0, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        if (_rules is null || !_rules.Current.ViewModes.Contains(mode)) return Task.FromResult(ModeSwitchResult.Refused);
        if (mode == Mode) return Task.FromResult(ModeSwitchResult.Ok);
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        ModeSwitchResult result;
        N.Vector3 inheritedVelocity = default;
        switch (mode)
        {
            case ViewMode.Walk:
                if (_walk is null) return Task.FromResult(ModeSwitchResult.Refused);
                result = _walk.Enter(Pose);
                if (result == ModeSwitchResult.Refused) return Task.FromResult(result);
                _flySpeedStep = _speedStep; _speedStep = _walkSpeedStep;
                _walk.SetSpeedStep(_walkSpeedStep);
                _pose = _walk.EyePose;
                break;
            case ViewMode.Fly:
                _pose = (_walk is { Active: true } ? _walk.EyePose : Pose) with { FovDeg = Tuning.FlyFov };
                if (_walk is { Active: true }) inheritedVelocity = new(_walk.Velocity.X, 0, _walk.Velocity.Z);
                _walk?.Leave(); _speedStep = _flySpeedStep;
                _pose = Legalize(_pose);
                result = ModeSwitchResult.Ok;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
        _mode = mode; _flight.Reset(); _input.Clear(); SetEdgeFog(0);
        _flight.ConstrainVelocity(inheritedVelocity);
        return Task.FromResult(result);
    }
}

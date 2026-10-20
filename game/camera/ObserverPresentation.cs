using Campus.Contracts;
using Campus.Contracts.Settings;
using N = System.Numerics;

namespace Campus.Camera;

/// <summary>Presentation pose and preference-driven fly FOV interpolation.</summary>
internal sealed class ObserverPresentation(FlightTuning tuning)
{
    private ISettingsSection? _settings;
    private float _fovFrom;
    private float _fovTarget;
    private float _fovElapsed;

    public Pose Value { get; private set; }
    private float FlyFov => _settings?.Get(UiSettingDefinitions.FlyFov) ?? UiSettingDefinitions.FlyFov.Default;
    private float WalkFov => _settings?.Get(UiSettingDefinitions.WalkFov) ?? UiSettingDefinitions.WalkFov.Default;

    public void Bind(ISettingsSection? settings) => _settings = settings;

    public float DefaultFov(ViewMode mode) => mode switch
    {
        ViewMode.Fly => FlyFov,
        ViewMode.Walk => WalkFov,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };

    public void Set(Pose pose, float fov)
    {
        Value = pose with { FovDeg = fov };
        _fovFrom = Value.FovDeg.Value;
        _fovTarget = FlyFov;
        _fovElapsed = tuning.ModeFovSeconds;
    }

    public void BeginFly(Pose pose)
    {
        Value = pose;
        _fovFrom = Value.FovDeg ?? FlyFov;
        _fovTarget = FlyFov;
        _fovElapsed = 0;
    }

    public void ApplyProjection(Pose pose) => Value = pose;
    public void MoveTo(N.Vector3 position) => Value = Value with { Position = position };
    public void Look(Pose pose) => Value = pose;

    public void AdvanceFlyFov(float seconds)
    {
        float target = FlyFov;
        if (target != _fovTarget)
        {
            _fovFrom = Value.FovDeg ?? target;
            _fovTarget = target;
            _fovElapsed = 0;
        }
        // Keep an explicitly supplied review FOV until a mode or preference change.
        if (_fovElapsed >= tuning.ModeFovSeconds) return;
        _fovElapsed = MathF.Min(_fovElapsed + seconds, tuning.ModeFovSeconds);
        float t = ScalarMath.Smootherstep(_fovElapsed / tuning.ModeFovSeconds);
        Value = Value with { FovDeg = _fovFrom + (_fovTarget - _fovFrom) * t };
    }

    public static bool IsFinite(Pose pose) =>
        VectorMath.Finite(pose.Position) && float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg)
        && (pose.FovDeg is not { } fov || float.IsFinite(fov) && fov > 0 && fov < 180);

    public static void ValidateFinite(Pose pose)
    {
        if (!IsFinite(pose)) throw new ArgumentOutOfRangeException(nameof(pose));
    }
}

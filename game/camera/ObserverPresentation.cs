using Campus.Contracts;
using Campus.Contracts.Settings;
using N = System.Numerics;

namespace Campus.Camera;

/// <summary>Presentation pose and current mode FOV preferences.</summary>
internal sealed class ObserverPresentation
{
    private ISettingsSection? _settings;
    private float _appliedFlyFov;

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
        _appliedFlyFov = FlyFov;
    }

    public void ApplyProjection(Pose pose) => Value = pose;
    public void MoveTo(N.Vector3 position) => Value = Value with { Position = position };
    public void Look(Pose pose) => Value = pose;

    public void RefreshFlyFov()
    {
        float target = FlyFov;
        // Preserve an explicit review FOV until the preference itself changes.
        if (target == _appliedFlyFov) return;
        _appliedFlyFov = target;
        Value = Value with { FovDeg = target };
    }

    public static bool IsFinite(Pose pose) =>
        VectorMath.Finite(pose.Position) && float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg)
        && (pose.FovDeg is not { } fov || float.IsFinite(fov) && fov > 0 && fov < 180);

    public static void ValidateFinite(Pose pose)
    {
        if (!IsFinite(pose)) throw new ArgumentOutOfRangeException(nameof(pose));
    }
}

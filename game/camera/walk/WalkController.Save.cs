using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private static readonly WalkSaveJsonContext SaveJson = new(new JsonSerializerOptions(ContractJson.Options));
    public JsonTypeInfo<WalkSave> TypeInfo => SaveJson.WalkSave;

    public WalkSave Capture(in CaptureContext c)
    {
        RequireMap(c.Map);
        return new(ResolveSettledFeet(), _crouching, SpeedStep, _safe.Snapshot());
    }

    public void Restore(WalkSave dto, RestoreContext c)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequireMap(c.Map);
        _safe.Clear();
        var validSafe = WalkSafetyQueries.RestoreSafePoints(_map?.World.Spatial, dto.SafePoints, c);
        _safe.Restore(validSafe);
        _crouching = dto.Crouching;
        _restoredCrouch = dto.Crouching;
        ApplyHeight();
        SpeedStep = Math.Clamp(dto.SpeedStep, 0, WalkTuning.SpeedStepCount - 1);
        if (SpeedStep != dto.SpeedStep) c.Repair("walk: speed step clamped");
        var pose = dto.Feet;
        bool validAngles = float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg);
        float yaw = validAngles ? pose.YawDeg : 0;
        float pitch = validAngles ? Math.Clamp(pose.PitchDeg, -89f, 89f) : 0;
        if (!validAngles || pitch != pose.PitchDeg) c.Repair("walk: look angles repaired");
        if (!ValidSavedFeet(pose.Position))
        {
            N.Vector3 search = WalkMath.Finite(pose.Position) ? pose.Position : DefaultFeet(c.Map).Position;
            var safe = _map?.World.Spatial.NearestSafe(search, WalkTuning.SafetySearchRadius);
            if (safe is { } fallback && ValidSavedFeet(fallback))
            {
                pose = new(fallback, yaw, pitch);
                c.Repair("walk: invalid feet relocated to nearest safe point");
            }
            else
            {
                // Keep the invalid cylinder frozen. WP-06 can read Active=false and return to fly.
                Leave();
                _safetyFailed = true;
                c.Repair("walk: no safe landing within 50 m; walking disabled");
                return;
            }
        }
        else pose = new(pose.Position, yaw, pitch);
        Warp(pose);
        // view/Pose60 has just restored this mode; the world frame readout updates next frame.
        Active = Services.Get<IViewControl>().Mode switch
        {
            ViewMode.Fly => false,
            ViewMode.Walk => true,
            _ => throw new InvalidDataException("Unknown restored view mode.")
        };
        // Warp clears velocity, jump timers and interpolation. Physics stays frozen until real collision Ready.
    }

    public WalkSave CreateDefault(NewGameContext c) =>
        WalkSafetyQueries.CreateDefault(Services.Get<IMapService>(), c);

    private bool ValidSavedFeet(N.Vector3 feet) => _map is not null
        && WalkSafetyQueries.SupportedDryFeet(_map.World.Spatial, feet);

    private Pose DefaultFeet(MapId targetMap) => WalkSafetyQueries.DefaultFeet(Services.Get<IMapService>(), targetMap);

    private void RequireMap(MapId targetMap)
    {
        ResolveServices();
        var maps = Services.Get<IMapService>();
        if (maps.Current == targetMap && _map?.Descriptor.Id == targetMap) return;
        Leave();
        _safetyFailed = true;
        throw new InvalidOperationException($"Cannot restore or capture walk feet before map {targetMap.Value} is mounted.");
    }
}

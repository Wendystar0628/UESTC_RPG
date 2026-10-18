using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private static readonly WalkSaveJsonContext SaveJson = new(new JsonSerializerOptions(ContractJson.Options));
    public JsonTypeInfo<WalkSave> TypeInfo => SaveJson.WalkSave;

    public WalkSave Capture(in CaptureContext c)
    {
        RequireMap(c.Map);
        return new(ResolveSettledFeet(), _crouching, SpeedStep, _lastStable is { } stable ? [stable] : []);
    }

    public void Restore(WalkSave dto, RestoreContext c)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequireMap(c.Map);
        _lastStable = null;
        foreach (var saved in dto.SafePoints ?? [])
        {
            if (!WalkSafetyQueries.ValidPose(saved)) continue;
            _lastStable = saved;
            break;
        }
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
        if (!VectorMath.Finite(pose.Position))
        {
            pose = (_lastStable ?? DefaultFeet(c.Map)) with { YawDeg = yaw, PitchDeg = pitch };
            c.Repair("walk: invalid feet replaced by a pending landing origin");
        }
        else pose = new(pose.Position, yaw, pitch);
        Warp(pose);
        // view/Pose60 owns the mode; this section owns physical placement and walking speed.
        // Warp clears velocity, jump timers and interpolation. Physics stays frozen until real collision Ready.
    }

    public WalkSave CreateDefault(NewGameContext c) =>
        WalkSafetyQueries.CreateDefault(Services.Get<IMapService>(), c);

    private Pose DefaultFeet(MapId targetMap) => WalkSafetyQueries.DefaultFeet(Services.Get<IMapService>(), targetMap);

    private void RequireMap(MapId targetMap)
    {
        ResolveServices();
        var maps = Services.Get<IMapService>();
        if (maps.Current == targetMap && _map?.Descriptor.Id == targetMap) return;
        RequestExit();
        throw new InvalidOperationException($"Cannot restore or capture walk feet before map {targetMap.Value} is mounted.");
    }
}

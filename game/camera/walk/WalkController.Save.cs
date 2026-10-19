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
    private RestoreContext? _historyRestore;
    public JsonTypeInfo<WalkSave> TypeInfo => SaveJson.WalkSave;

    public WalkSave Capture(in CaptureContext c)
    {
        RequireMap(c.Map);
        return new(ResolveSettledFeet(), _crouching, SpeedStep, _safePoints.Capture());
    }

    public void Restore(WalkSave dto, RestoreContext c)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequireMap(c.Map);
        _safePoints.Clear();
        foreach (var saved in dto.SafePoints ?? [])
        {
            if (!WalkSafetyQueries.ValidPose(saved)) { c.Repair("walk: invalid history point removed"); continue; }
            float boundary = FlightBounds.RingDistance(new(saved.Position.X, saved.Position.Z), _map!.World.Boundary, out _);
            if (boundary > -WalkTuning.BoundaryClearance) { c.Repair("walk: boundary history point removed"); continue; }
            _safePoints.Record(saved);
        }
        _historyRestore = c;
        var points = _safePoints.Points.Select(p => new System.Numerics.Vector2(p.Position.X, p.Position.Z)).ToArray();
        var preparation = _terrain!.PreparePoints(_map!, "walk.saved-safe-points", points);
        _landingPointsPreparation = _landingPointsPreparation is { } authored ? Task.WhenAll(authored, preparation) : preparation;
        _crouching = dto.Crouching;
        _restoredCrouch = dto.Crouching;
        ApplyHeight();
        SpeedStep = Math.Clamp(dto.SpeedStep, 0, WalkRules.SpeedStepCount - 1);
        if (SpeedStep != dto.SpeedStep) c.Repair("walk: speed step clamped");
        var pose = dto.Feet;
        bool validAngles = float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg);
        float yaw = validAngles ? pose.YawDeg : 0;
        float pitch = validAngles ? Math.Clamp(pose.PitchDeg, -89f, 89f) : 0;
        if (!validAngles || pitch != pose.PitchDeg) c.Repair("walk: look angles repaired");
        if (!VectorMath.Finite(pose.Position))
        {
            pose = (_safePoints.Count > 0 ? _safePoints.Points.Last() : DefaultFeet(c.Map)) with { YawDeg = yaw, PitchDeg = pitch };
            c.Repair("walk: invalid feet replaced by a pending landing origin");
        }
        else pose = new(pose.Position, yaw, pitch);
        Place(pose, 0, WalkTuning.SaveRepairRadius);
        // view/Pose60 owns the mode; this section owns physical placement and walking speed.
        // Placement clears velocity, jump timers and interpolation; save repairs stay within their own radius.
        // Physics stays frozen until the actual arrival and collision are ready.
    }

    private void ValidateRestoredHistory()
    {
        if (_historyRestore is not { } restore) return;
        _historyRestore = null;
        var history = _safePoints.Capture();
        _safePoints.Clear();
        foreach (var point in history)
        {
            if (WalkSafetyQueries.InWater(_map!.World.Spatial, point.Position)
                || !WalkSafetyQueries.AllowsFeet(_map.World.Spatial, point.Position))
                restore.Repair("walk: unsafe history point removed");
            else _safePoints.Record(point);
        }
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

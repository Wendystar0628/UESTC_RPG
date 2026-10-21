using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Walk save serialization, restoration repairs and prepared-history validation.</summary>
internal sealed class WalkPersistence(WalkPlacement placement, WalkTerrainPreparation terrain)
{
    private static readonly WalkSaveJsonContext SaveJson = new(new JsonSerializerOptions(ContractJson.Options));
    private RestoreContext? _historyRestore;
    public bool HistoryPending => _historyRestore is not null;
    public JsonTypeInfo<WalkSave> TypeInfo => SaveJson.WalkSave;
    public WalkSave Capture(Pose settledFeet, bool crouching, int speedStep) =>
        new(settledFeet, crouching, speedStep, placement.History.Capture());

    public void RestoreHistory(WalkSave dto, RestoreContext context, IMapContext map)
    {
        var history = placement.History;
        history.Clear();
        foreach (var saved in dto.SafePoints ?? [])
        {
            if (!WalkSafetyQueries.ValidPose(saved)) { context.Repair("walk: invalid history point removed"); continue; }
            float boundary = FlightBounds.RingDistance(new(saved.Position.X, saved.Position.Z), map.World.Boundary, out _);
            if (boundary > -WalkTuning.BoundaryClearance) { context.Repair("walk: boundary history point removed"); continue; }
            history.Record(saved);
        }
        _historyRestore = context;
        terrain.PrepareSavedHistory(history);
    }

    public Pose RestoreFeet(WalkSave dto, RestoreContext context, IMapContext map)
    {
        var pose = dto.Feet;
        bool validAngles = float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg);
        float yaw = validAngles ? pose.YawDeg : 0;
        float pitch = validAngles ? Math.Clamp(pose.PitchDeg, -89f, 89f) : 0;
        if (!validAngles || pitch != pose.PitchDeg) context.Repair("walk: look angles repaired");
        if (!VectorMath.Finite(pose.Position)
            || new FlightBounds(map, map.World.Boundary).ProjectHorizontal(pose.Position) != pose.Position)
        {
            var history = placement.History;
            pose = (history.Count > 0 ? history.Points.Last() : WalkSafetyQueries.DefaultFeet(KernelServices.Get<IMapService>(), context.Map))
                with { YawDeg = yaw, PitchDeg = pitch };
            context.Repair("walk: invalid feet replaced by a pending landing origin");
        }
        else pose = new(pose.Position, yaw, pitch);
        return pose;
    }

    public void ValidateRestoredHistory(IMapContext map)
    {
        if (_historyRestore is not { } restore) return;
        _historyRestore = null;
        var history = placement.History.Capture();
        placement.History.Clear();
        foreach (var point in history)
        {
            if (WalkSafetyQueries.InWater(map.World.Spatial, point.Position)
                || !WalkSafetyQueries.AllowsFeet(map.World.Spatial, point.Position))
                restore.Repair("walk: unsafe history point removed");
            else placement.History.Record(point);
        }
    }

    public void Unload() => _historyRestore = null;
    public WalkSave CreateDefault(NewGameContext context) => WalkSafetyQueries.CreateDefault(KernelServices.Get<IMapService>(), context);
}

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Walk save data is prepared without mutating the body or its history, then committed synchronously.</summary>
internal sealed class WalkPersistence(WalkPlacement placement)
{
    private static readonly WalkSaveJsonContext SaveJson = new(new JsonSerializerOptions(ContractJson.Options));
    public JsonTypeInfo<WalkSaveData> TypeInfo => SaveJson.WalkSaveData;
    public WalkSaveData Capture(Pose settledFeet, bool crouching, int speedStep) =>
        new(settledFeet, crouching, speedStep, placement.History.Capture());

    public static async Task<WalkSaveData> PrepareRestore(WalkSaveData dto, RestoreContext context,
        IMapContext map, WalkTerrainPreparation terrain, Pose fallback, CancellationToken cancellation)
    {
        var history = new WalkSafePoints();
        foreach (var saved in dto.SafePoints ?? [])
        {
            if (!WalkSafetyQueries.ValidPose(saved)) { context.Repair("walk: invalid history point removed"); continue; }
            float boundary = FlightBounds.RingDistance(new(saved.Position.X, saved.Position.Z), map.World.Boundary, out _);
            if (boundary > -WalkTuning.BoundaryClearance) { context.Repair("walk: boundary history point removed"); continue; }
            history.Record(saved);
        }
        var pose = dto.Feet;
        bool validAngles = float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg);
        float yaw = validAngles ? pose.YawDeg : 0;
        float pitch = validAngles ? Math.Clamp(pose.PitchDeg, -89f, 89f) : 0;
        if (!validAngles || pitch != pose.PitchDeg) context.Repair("walk: look angles repaired");
        bool validFeet = VectorMath.Finite(pose.Position)
            && new FlightBounds(map, map.World.Boundary).ProjectHorizontal(pose.Position) == pose.Position;
        var points = history.Capture();
        await terrain.PrepareRestorePoints(map, points.Append(validFeet ? pose : fallback).Append(fallback).ToArray(), cancellation);
        cancellation.ThrowIfCancellationRequested();
        history.Clear();
        foreach (var point in points)
        {
            if (WalkSafetyQueries.InWater(map.World.Spatial, point.Position)
                || WalkSafetyQueries.NeedsRecovery(map.World.Spatial, point.Position, permittedWater: false))
                context.Repair("walk: unsafe history point removed");
            else history.Record(point);
        }
        if (validFeet && (WalkSafetyQueries.InWater(map.World.Spatial, pose.Position)
            || WalkSafetyQueries.NeedsRecovery(map.World.Spatial, pose.Position, permittedWater: false)))
        {
            validFeet = false;
            context.Repair("walk: unsafe feet replaced by a pending landing origin");
            pose = history.Points.LastOrDefault(fallback);
        }
        else if (!validFeet)
        {
            pose = history.Points.LastOrDefault(fallback);
            context.Repair("walk: invalid feet replaced by a pending landing origin");
        }
        pose = new(pose.Position, yaw, pitch);
        int speed = Math.Clamp(dto.SpeedStep, 0, WalkRules.SpeedStepCount - 1);
        if (speed != dto.SpeedStep) context.Repair("walk: speed step clamped");
        return new(pose, dto.Crouching, speed, history.Capture());
    }

    public void RestoreHistory(WalkSaveData dto)
    {
        placement.History.Clear();
        foreach (var point in dto.SafePoints) placement.History.Record(point);
    }

    public WalkSaveData CreateDefault(NewGameContext context) => WalkSafetyQueries.CreateDefault(KernelServices.Get<IMapService>(), context);
}

using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Boot;

public static class ReviewPoseResolver
{
    public static Pose Resolve(string id, IMapContext context, Func<string, byte[]> read)
    {
        var points = JsonSerializer.Deserialize<WaypointFile>(read(context.DataRoot + "waypoints.json"), ContractJson.Options)
            ?? throw new InvalidDataException("Waypoint file is empty.");
        if (Direct(id) is { } pose) return pose;
        using var review = JsonDocument.Parse(read(context.DataRoot + "review.json"));
        if (review.RootElement.GetProperty("version").GetInt32() != 1) throw new InvalidDataException("Unsupported review-pose version.");
        var candidates = review.RootElement.GetProperty("poses").EnumerateArray()
            .Where(item => item.GetProperty("id").GetString() == id).ToArray();
        if (candidates.Length != 1) throw new ArgumentException($"Unknown or duplicate review pose: {id}.");
        var item = candidates[0];
        var waypoint = item.TryGetProperty("waypoint", out var pointRef);
        var explicitPose = item.TryGetProperty("pose", out var poseData);
        if (waypoint == explicitPose) throw new InvalidDataException($"Review pose {id} requires exactly one waypoint or pose.");
        if (explicitPose) return poseData.Deserialize<Pose>(ContractJson.Options);
        return Direct(pointRef.GetString() ?? "") ?? throw new InvalidDataException($"Review pose {id} references an unknown waypoint.");

        Pose? Direct(string name) => context.Descriptor.Spawns.FirstOrDefault(spawn => spawn.Id == name)?.Fly
            ?? points.Waypoints.FirstOrDefault(point => point.Id == name)?.Fly;
    }
}

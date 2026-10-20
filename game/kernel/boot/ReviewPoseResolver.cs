using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;

namespace Campus.Kernel.Boot;

public static class ReviewPoseResolver
{
    public static Pose Resolve(string id, IMapContext context) =>
        MapCatalog.ReviewPoses(context.Descriptor, context.SourceInputs.Entries).TryGetValue(id, out var pose)
            ? pose : throw new ArgumentException($"Unknown review pose, spawn or waypoint: {id}.", nameof(id));
}

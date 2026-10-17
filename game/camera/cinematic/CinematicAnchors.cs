using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;

namespace Campus.Camera.Cinematic;

/// <summary>Immutable playback-start anchors, so $current never drifts with the camera.</summary>
public sealed class CinematicAnchorSet(Pose title, Pose current, Pose destination, float userFov,
    IMapService maps, MapId map)
{
    public float UserFov { get; } = userFov;
    public Pose Current { get; } = current;
    public Pose Destination { get; } = destination;
    public Pose Resolve(string anchor) => anchor switch
    {
        "$title" => title,
        "$current" => Current,
        "$dest" => Destination,
        "$destApproach" => CinematicMath.Approach(Destination),
        _ when anchor.StartsWith("$waypoint:", StringComparison.Ordinal) =>
            maps.ResolveSpawn(map, anchor[10..]).Fly,
        _ => throw new InvalidDataException($"Unknown cinematic anchor: {anchor}"),
    };
    public Pose Resolve(CinematicPose pose) => pose.Value ?? Resolve(pose.Anchor
        ?? throw new InvalidDataException("Cinematic endpoint has no pose."));
    public float Resolve(FovKey key) => key.Value ?? key.Anchor switch
    {
        "$userFov" => UserFov,
        "$dest" => Destination.FovDeg ?? UserFov,
        _ => throw new InvalidDataException($"Unknown cinematic FOV anchor: {key.Anchor}"),
    };
}

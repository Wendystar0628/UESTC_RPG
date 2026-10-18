using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;

namespace Campus.Contracts.Cinematic;

/// <summary>Captured playback-start poses; named waypoints remain lazily resolved through the map service.</summary>
public sealed record CinematicPlaybackStart(Pose Title, Pose Current, Pose Destination, float UserFov, MapId Map);

/// <summary>Flow owns world cues and handoff; the camera owns timeline advancement and presented-cover waits.</summary>
public interface ICinematicPlayback
{
    bool IsPlaying { get; }
    Pose ResolvePose(CinematicPose pose, CinematicPlaybackStart start, IMapService maps);
    Task Play(CinematicDef definition, CinematicPlaybackStart start, IMapService maps, IWorldModel world,
        AppState state, Func<WorldCue, bool, Task> worldCue);
    Task FinishWorld();
    Task WaitForPresentedCoverage();
    Task WaitUntil(Func<bool> ready, double timeout, string reason);
    void Pause();
    void Abort(Exception error);
    void Clear();
    void Tick(double delta);
    void FramePresented();
}

public static class CinematicPoseMath
{
    public static float AngleDelta(float from, float to) => ((to - from + 540) % 360 + 360) % 360 - 180;
}

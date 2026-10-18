namespace Campus.Contracts.Cinematic;

/// <summary>
/// WP-39 service used by the cinematic player. Only AppState.Entering may activate or advance the override;
/// calls in other states are ignored (Begin returns false). It changes lamp.cue/sys.cue timing only,
/// never persistent world state or the channels' final values.
/// </summary>
public interface ILampCueHook
{
    // cueRef is the lampCue reference in extras.json; altitude is the endpoint's effective solar altitude.
    bool Begin(string cueRef, float targetSunAltDeg);
    // Timeline seconds, called at FramePriority.Camera; no independent clock is started by the hook.
    void Seek(double timelineSeconds);
    // Revoke at completion, skip, or control handoff; all channels return to their non-overridden values.
    void End();
}

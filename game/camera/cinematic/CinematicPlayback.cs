using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;
using Campus.Flow;
using Campus.Kernel.Renderer;

namespace Campus.Camera.Cinematic;

/// <summary>Single playback owner, advanced only by the camera-priority node.</summary>
[KernelService(typeof(CinematicPlayback), Owner = "WP-49a")]
public sealed class CinematicPlayback(ICameraRig camera, IVeilService veil, IRuleset rules, IVeilPresentation presentation)
{
    private readonly List<FrameWait> _waits = [];
    private CinematicDef? _definition;
    private CinematicTrack? _track;
    private Func<WorldCue, bool, Task>? _worldCue;
    private TaskCompletionSource? _done;
    private Task? _pending;
    private Task? _veilTask;
    private Task? _finish;
    private int _worldIndex;
    private bool _paused;
    private bool _covered;
    private bool _stopping;
    private bool _clearInProgress;
    private double _carry;
    private long _drawFrame;
    private long _coveredDrawFrame = -1;
    public double Time { get; private set; }
    public bool IsPlaying => _done is { Task.IsCompleted: false };
    public CinematicDef? Definition => _definition;
    public void FramePresented()
    {
        _drawFrame++;
        if (presentation.IsCovered && veil.Opacity >= 1) _coveredDrawFrame = _drawFrame;
    }
    public Task WaitForPresentedCoverage()
    {
        var firstEligibleFrame = _drawFrame + 1;
        return WaitUntil(() => _coveredDrawFrame >= firstEligibleFrame && presentation.IsCovered && veil.Opacity >= 1,
            FlowTuning.PresentationReadyTimeoutSeconds, "Cinematic cover has not been presented by FramePostDraw.");
    }

    public Task Play(CinematicDef definition, CinematicAnchorSet anchors, IWorldModel world,
        AppState state, Func<WorldCue, bool, Task> worldCue)
    {
        if (IsPlaying) throw new InvalidOperationException("A cinematic is already playing.");
        if (!rules.Current.CinematicStates.Contains(state)) throw new InvalidOperationException("Ruleset forbids cinematic playback in this state.");
        _track?.Dispose(); _track = new(definition, anchors, world);
        _definition = definition; _worldCue = worldCue; _worldIndex = 0; Time = 0;
        _paused = false; _covered = false; _stopping = false; _pending = null; _finish = null; _veilTask = null; _carry = 0;
        _done = new();
        return _done.Task;
    }

    public void Tick(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        var waits = _waits.ToArray();
        for (var i = waits.Length - 1; i >= 0; i--)
        {
            var wait = waits[i];
            if (!_waits.Contains(wait)) continue;
            wait.Elapsed += delta;
            try
            {
                if (wait.Ready()) { if (_waits.Remove(wait)) wait.Done.TrySetResult(); }
                else if (wait.Elapsed >= wait.Timeout) { if (_waits.Remove(wait)) wait.Done.TrySetException(new TimeoutException(wait.Reason)); }
            }
            catch (Exception error) { if (_waits.Remove(wait)) wait.Done.TrySetException(error); }
        }
        if (!IsPlaying || _paused || _stopping || _definition is null || _track is null) return;
        try
        {
            if (_pending is { IsCompleted: false }) return;
            _pending?.GetAwaiter().GetResult(); _pending = null;
            if (_worldIndex < _definition.World.Count && _definition.World[_worldIndex].T <= Time && _veilTask is { IsCompleted: false }) return;
            _carry += delta;
            var next = Math.Min(_definition.Duration, Time + _carry);
            if (_worldIndex < _definition.World.Count) next = Math.Min(next, _definition.World[_worldIndex].T);
            for (var i = 0; i < _definition.Masks.Count; i++)
            {
                var mask = _definition.Masks[i];
                if (mask.T0 > Time) next = Math.Min(next, mask.T0);
                if (mask.T1 - mask.FadeOut > Time) next = Math.Min(next, mask.T1 - mask.FadeOut);
            }
            ApplyCover(next);
            var previous = camera.Pose;
            camera.SetPose(_track.Sample(next, previous, next - Time)); _carry -= next - Time; Time = next;
            if (_worldIndex < _definition.World.Count && _definition.World[_worldIndex].T <= Time)
            {
                if (_veilTask is { IsCompleted: false }) return;
                _veilTask?.GetAwaiter().GetResult();
                var cue = _definition.World[_worldIndex++];
                _pending = _worldCue!(cue, false);
                return;
            }
            if (Time >= _definition.Duration) Complete();
        }
        catch (Exception error) { Fail(error); }
    }

    // The caller covers first. This joins any in-flight restore before draining remaining cues.
    public Task FinishWorld() => _finish ??= FinishWorldCore();
    private async Task FinishWorldCore()
    {
        _paused = true; _stopping = true;
        try
        {
            if (_pending is not null) await _pending;
            if (_definition is not null)
            {
                while (_worldIndex < _definition.World.Count)
                    await _worldCue!(_definition.World[_worldIndex++], true);
                Time = _definition.Duration;
            }
            Complete();
        }
        catch (Exception error) { Fail(error); throw; }
    }
    public void Pause() => _paused = true;
    public void Abort(Exception error) => Fail(error);
    public void Clear()
    {
        // Old cancellation callbacks may start another playback before a later callback
        // reenters Clear. The outer call owns this cleanup pass, not the new playback.
        if (_clearInProgress) return;
        _clearInProgress = true;
        try
        {
            var waits = _waits.ToArray(); _waits.Clear();
            var done = _done; _done = null;
            var track = _track; _track = null; _definition = null; _worldCue = null;
            _pending = null; _veilTask = null; _covered = false;
            track?.Dispose();
            // Only detached waits belong to this operation; underlying world jobs keep running.
            foreach (var wait in waits) wait.Done.TrySetCanceled();
            done?.TrySetCanceled();
        }
        finally { _clearInProgress = false; }
    }
    public Task WaitUntil(Func<bool> ready, double timeout, string reason)
    {
        if (ready()) return Task.CompletedTask;
        var wait = new FrameWait(ready, timeout, reason); _waits.Add(wait); return wait.Done.Task;
    }
    private void ApplyCover(double time)
    {
        // Cloud artwork is owned by 49b. Until it is available the same mask envelope
        // uses the shared fog cover, preserving the hidden world-replacement boundary.
        var needsCover = false;
        var fadeIn = 0f; var fadeOut = 0f;
        for (var i = 0; i < _definition!.Masks.Count; i++)
        {
            var mask = _definition.Masks[i];
            var fullScreen = mask.Kind switch
            {
                MaskKind.Letterbox => false,
                MaskKind.CloudCover or MaskKind.Veil => true,
                _ => throw new InvalidDataException("Unknown cinematic mask."),
            };
            if (!fullScreen) continue;
            if (time >= mask.T0 && time < mask.T1 - mask.FadeOut) { needsCover = true; fadeIn = (float)mask.FadeIn; }
            if (time >= mask.T1 - mask.FadeOut && time <= mask.T1) fadeOut = (float)mask.FadeOut;
        }
        if (_covered == needsCover) return;
        _covered = needsCover;
        _veilTask = needsCover ? veil.Cover(VeilReason.Cinematic, fadeIn) : veil.Reveal(VeilReason.Cinematic, fadeOut);
    }
    private void Complete() { _paused = true; _done?.TrySetResult(); }
    private void Fail(Exception error) { _paused = true; _done?.TrySetException(error); }
    private sealed class FrameWait(Func<bool> ready, double timeout, string reason)
    {
        public Func<bool> Ready { get; } = ready;
        public double Timeout { get; } = timeout;
        public string Reason { get; } = reason;
        public double Elapsed { get; set; }
        public TaskCompletionSource Done { get; } = new();
    }
}

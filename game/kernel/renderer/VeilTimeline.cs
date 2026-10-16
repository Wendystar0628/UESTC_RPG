using Campus.Contracts;

namespace Campus.Kernel.Renderer;

/// <summary>Main-thread transition state. A failed readiness check never reveals an incomplete world.</summary>
public sealed class VeilTimeline : IVeilService
{
    private readonly VeilReadinessObservation _readiness = new();
    private TaskCompletionSource? _transition;
    private Func<bool>? _ready;
    private float _holdElapsed;
    private float _holdLimit;
    private float _start = 1f;
    private float _target = 1f;
    private float _elapsed;
    private float _duration;

    public float Opacity { get; private set; } = 1f;
    public VeilReason Reason { get; private set; } = VeilReason.Boot;
    public VeilContent Content { get; private set; } = VeilContent.Fog;
    public Exception? Failure { get; private set; }
    public bool Waiting => _readiness.Waiting;
    public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness) => _readiness.Begin(kind);
    internal void StopObservingReadiness() { if (_readiness.Stop()) Changed?.Invoke(); }
    public bool Busy => _transition is not null || _ready is not null;
    public event Action? Changed;
    public event Action<Exception>? Failed;

    public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog)
    {
        Validate(reason, seconds);
        _ = content switch { VeilContent.Fog => 0, VeilContent.Frontispiece => 1, _ => throw new ArgumentOutOfRangeException(nameof(content)) };
        Failure = null;
        _ready = null;
        Content = content;
        return Begin(reason, 1f, seconds);
    }

    public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds)
    {
        Validate(reason, maxWaitSeconds);
        ArgumentNullException.ThrowIfNull(ready);
        Reason = reason;
        _ready = ready;
        _holdElapsed = 0;
        _holdLimit = maxWaitSeconds;
        Opacity = 1f;
        _start = 1f;
        _elapsed = 0;
        if (_transition is not null && _target == 1f) Complete();
        Changed?.Invoke();
    }

    public Task Reveal(VeilReason reason, float seconds)
    {
        Validate(reason, seconds);
        if (Failure is not null) return Task.FromException(Failure);
        return Begin(reason, 0f, seconds);
    }

    public void Advance(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (_readiness.Advance(delta)) Changed?.Invoke();
        if (Failure is not null) return;
        if (_ready is not null)
        {
            try
            {
                if (_ready()) _ready = null;
                else
                {
                    _holdElapsed += (float)delta;
                    if (_holdElapsed >= _holdLimit) Fail(new TimeoutException($"Veil readiness timed out: {Reason}."));
                    return;
                }
            }
            catch (Exception error) { Fail(error); return; }
        }
        if (_transition is null) return;
        _elapsed += (float)delta;
        if (_duration == 0 || _elapsed >= _duration) Complete();
        else
        {
            Opacity = _start + (_target - _start) * StandardCurve(_elapsed / _duration);
            Changed?.Invoke();
        }
    }

    private Task Begin(VeilReason reason, float target, float seconds)
    {
        var previous = _transition;
        _transition = null;
        previous?.TrySetCanceled();
        Reason = reason;
        _start = Opacity;
        _target = target;
        _duration = seconds;
        _elapsed = 0;
        _transition = new();
        var task = _transition.Task;
        if ((seconds == 0 || _start == target) && (target == 1f || _ready is null)) Complete();
        else Changed?.Invoke();
        return task;
    }

    private void Complete()
    {
        Opacity = _target;
        var completion = _transition;
        _transition = null;
        Changed?.Invoke();
        completion?.TrySetResult();
    }

    private void Fail(Exception error)
    {
        Failure = error;
        Opacity = 1f;
        _ready = null;
        var completion = _transition;
        _transition = null;
        Changed?.Invoke();
        completion?.TrySetException(error);
        Failed?.Invoke(error);
    }

    // Cubic Bezier (0.4, 0, 0.2, 1); solve x, then evaluate y. Shared by cover and reveal.
    private static float StandardCurve(float x)
    {
        var low = 0f;
        var high = 1f;
        for (var i = 0; i < 14; i++)
        {
            var t = (low + high) * .5f;
            var one = 1f - t;
            var sample = 3f * one * one * t * .4f + 3f * one * t * t * .2f + t * t * t;
            if (sample < x) low = t; else high = t;
        }
        var u = (low + high) * .5f;
        return u * u * (3f - 2f * u);
    }

    private static void Validate(VeilReason reason, float seconds)
    {
        _ = reason switch
        {
            VeilReason.Boot => 0,
            VeilReason.Transit => 1,
            VeilReason.Teleport => 2,
            VeilReason.Season => 3,
            VeilReason.Quality => 4,
            VeilReason.Respawn => 5,
            VeilReason.Cinematic => 6,
            VeilReason.MapTravel => 7,
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };
        if (!float.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
    }
}

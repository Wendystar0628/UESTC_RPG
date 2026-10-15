using Campus.Contracts;

namespace Campus.Kernel.Stubs.AudioUi;

/// <summary>State for the D0 full-screen ColorRect; the stage advances it on the main thread.</summary>
[KernelService(typeof(IVeilService), Owner = "WP-00", IsStub = true)]
[KernelService(typeof(IVeilState), Owner = "WP-00", IsStub = true)]
public sealed class VeilServiceStub : IVeilService
{
    private TaskCompletionSource? _transition;
    private float _target;
    private float _start;
    private float _elapsed;
    private float _duration;
    private Func<bool>? _ready;
    private float _holdRemaining;

    public float Opacity { get; private set; }
    public VeilReason Reason { get; private set; } = VeilReason.Boot;
    public VeilContent Content { get; private set; } = VeilContent.Fog;
    public event Action? Changed;

    public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog)
    {
        ValidateReason(reason);
        _ = content switch
        {
            VeilContent.Fog => 0,
            VeilContent.Frontispiece => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(content))
        };
        ValidateSeconds(seconds);
        _ready = null;
        Content = content;
        return Begin(reason, 1f, seconds);
    }

    public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds)
    {
        ValidateReason(reason);
        ArgumentNullException.ThrowIfNull(ready);
        ValidateSeconds(maxWaitSeconds);
        Reason = reason;
        _ready = ready;
        _holdRemaining = maxWaitSeconds;
        Opacity = 1f;
        if (_transition is not null && _target == 1f) Complete();
        else if (_transition is not null)
        {
            _start = 1f;
            _elapsed = 0f;
        }
        Changed?.Invoke();
    }

    public Task Reveal(VeilReason reason, float seconds)
    {
        ValidateReason(reason);
        ValidateSeconds(seconds);
        return Begin(reason, 0f, seconds);
    }

    private Task Begin(VeilReason reason, float target, float seconds)
    {
        _transition?.TrySetCanceled();
        Reason = reason;
        _target = target;
        _start = Opacity;
        _elapsed = 0f;
        _duration = seconds;
        _transition = new TaskCompletionSource();
        var task = _transition.Task;
        if (seconds == 0f && (target == 1f || _ready is null)) Complete();
        else Changed?.Invoke();
        return task;
    }

    /// <summary>Must be called by the stage each frame, including while the scene is paused.</summary>
    public void Advance(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0d)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        if (_ready is not null && Opacity >= 1f)
        {
            _holdRemaining -= (float)deltaSeconds;
            if (_ready() || _holdRemaining <= 0f) _ready = null;
        }

        if (_transition is null || (_target == 0f && _ready is not null)) return;
        _elapsed += (float)deltaSeconds;
        if (_duration == 0f || _elapsed >= _duration) Complete();
        else
        {
            Opacity = _start + ((_target - _start) * (_elapsed / _duration));
            Changed?.Invoke();
        }
    }

    private void Complete()
    {
        Opacity = _target;
        var completion = _transition;
        _transition = null;
        Changed?.Invoke();
        completion?.TrySetResult();
    }

    private static void ValidateSeconds(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
    }

    private static void ValidateReason(VeilReason reason)
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
    }
}

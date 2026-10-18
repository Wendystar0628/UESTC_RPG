using Campus.Contracts;

namespace Campus.Kernel.Renderer;

/// <summary>Main-thread veil arbitration. Each holder releases only its own cover.</summary>
public sealed class VeilTimeline : IVeilService
{
    private readonly VeilReadinessObservation _readiness = new();
    private readonly List<Lease> _leases = [];
    private TaskCompletionSource? _reveal;
    private bool _animating;
    private float _start = 1f;
    private float _target = 1f;
    private float _elapsed;
    private float _duration;

    public float Opacity { get; private set; } = 1f;
    public VeilReason Reason { get; private set; } = VeilReason.Boot;
    public VeilContent Content { get; private set; } = VeilContent.Fog;
    public bool Waiting => _readiness.Waiting;
    public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness) => _readiness.Begin(kind);
    public bool Busy => _animating;
    public event Action? Changed;

    public IVeilLease Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog)
    {
        Validate(reason, seconds);
        _ = content switch { VeilContent.Fog => 0, VeilContent.Frontispiece => 1, _ => throw new ArgumentOutOfRangeException(nameof(content)) };
        var lease = new Lease(this, reason, content);
        _leases.Add(lease);
        var interrupted = _reveal;
        _reveal = null;
        Reason = reason;
        Content = content;
        Begin(1f, seconds);
        interrupted?.TrySetCanceled();
        return lease;
    }

    internal void Stop()
    {
        var leases = _leases.ToArray();
        _leases.Clear();
        _animating = false;
        var reveal = _reveal;
        _reveal = null;
        _readiness.Stop();
        foreach (var lease in leases) lease.Stop();
        reveal?.TrySetCanceled();
    }

    public void Advance(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (_readiness.Advance(delta)) Changed?.Invoke();
        if (!_animating) return;
        _elapsed += (float)delta;
        if (_duration == 0 || _elapsed >= _duration) Complete();
        else
        {
            Opacity = _start + (_target - _start) * StandardCurve(_elapsed / _duration);
            Changed?.Invoke();
        }
    }

    private void Begin(float target, float seconds)
    {
        _start = Opacity;
        _target = target;
        _duration = seconds;
        _elapsed = 0;
        _animating = true;
        if (seconds == 0 || _start == target) Complete();
        else Changed?.Invoke();
    }

    private void Complete()
    {
        Opacity = _target;
        _animating = false;
        var completion = _reveal;
        _reveal = null;
        var covered = _target == 1f ? _leases.ToArray() : [];
        Changed?.Invoke();
        foreach (var lease in covered) lease.CompleteCover();
        completion?.TrySetResult();
    }

    private Task Release(Lease lease, float seconds)
    {
        _leases.Remove(lease);
        if (_leases.Count > 0)
        {
            var current = _leases[^1];
            Reason = current.Reason;
            Content = current.Content;
            Changed?.Invoke();
            return Task.CompletedTask;
        }
        _reveal = new();
        var task = _reveal.Task;
        Begin(0f, seconds);
        return task;
    }

    private sealed class Lease(VeilTimeline owner, VeilReason reason, VeilContent content) : IVeilLease
    {
        private readonly TaskCompletionSource _covered = new();
        private Task? _released;
        private bool _active = true;
        internal VeilReason Reason => reason;
        internal VeilContent Content => content;
        public Task Covered => _covered.Task;
        public bool IsCurrent => _active && owner._leases.Count > 0 && ReferenceEquals(owner._leases[^1], this);
        internal void CompleteCover() { if (_active) _covered.TrySetResult(); }
        internal void Stop()
        {
            _active = false;
            _released ??= Task.CompletedTask;
            _covered.TrySetCanceled();
        }
        public Task Reveal(float seconds)
        {
            Validate(reason, seconds);
            if (!_active) return _released ?? Task.CompletedTask;
            _active = false;
            _released = owner.Release(this, seconds);
            _covered.TrySetCanceled();
            return _released;
        }
        public void Dispose() { if (_active) _ = Reveal(0); }
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

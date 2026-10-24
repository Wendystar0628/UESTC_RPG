using Campus.Contracts;
using Campus.Contracts.Save;

namespace Campus.Tests;

/// <summary>
/// Shared ICameraRig fake, no registration: isolated tests own this camera and its input leases.
/// Mode follows the view control when one is given, otherwise it is a plain property. Transit throws unless
/// <paramref name="transit"/> is set, in which case it records the options and hands out a pending handle.
/// </summary>
internal class TestCameraRig(IViewControl? view = null, Pose? pose = null, bool transit = false) : ICameraRig
{
    public virtual ObserverSaveSnapshot CaptureSettledObserver(MapId map) =>
        new(map, new(Mode, Pose with { FovDeg = null }, null, 1, 1), null);
    public float RollDeg => 0;
    private readonly Dictionary<InputLockReason, int> _locks = [];
    private ViewMode? _mode;
    public Pose Pose { get; private set; } = pose ?? new Pose(new(0, 30, 60), 0, -15, 70);
    public float Speed => 0;
    public ViewMode Mode { get => _mode ?? view?.Mode ?? ViewMode.Fly; set => _mode = value; }
    public ITransitHandle? ActiveTransit => Handle;
    public float EdgeFog => 0;
    public bool InputLocked => _locks.Count != 0;
    /// <summary>Total live input leases across all reasons.</summary>
    public int Locks => _locks.Values.Sum();
    public TransitOptions? Options { get; private set; }
    public TestTransitHandle? Handle { get; private set; }
    public void SetPose(Pose pose)
    {
        if (!float.IsFinite(pose.Position.X) || !float.IsFinite(pose.Position.Y) || !float.IsFinite(pose.Position.Z) ||
            !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg) ||
            pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180))
            throw new ArgumentOutOfRangeException(nameof(pose));
        Pose = pose;
    }
    public ITransitHandle Transit(Pose to, TransitOptions? options = null)
    {
        if (!transit) throw new NotSupportedException("This domain fixture does not execute camera transit.");
        Options = options;
        return Handle = new(TransitKind.Teleport);
    }
    public IDisposable LockInput(InputLockReason reason)
    {
        _locks[reason] = _locks.GetValueOrDefault(reason) + 1;
        return new Lease(this, reason);
    }
    private sealed class Lease(TestCameraRig owner, InputLockReason reason) : IDisposable
    {
        private TestCameraRig? _owner = owner;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _owner, null) is not { } camera) return;
            var count = camera._locks[reason];
            if (count == 1) camera._locks.Remove(reason); else camera._locks[reason] = count - 1;
        }
    }
}

/// <summary>A transit that stays pending until the test completes or cancels it.</summary>
internal sealed class TestTransitHandle(TransitKind kind) : ITransitHandle
{
    public TaskCompletionSource<TransitResult> Completion { get; } = new();
    public TransitKind Kind => kind;
    public float Progress => 0;
    public Task<TransitResult> Done => Completion.Task;
    public void Cancel() => Completion.TrySetResult(TransitResult.Cancelled);
}

using Campus.Contracts;

namespace Campus.Tests;

// No registration: isolated domain tests own this camera and its input leases.
internal sealed class TestCameraRig(IWorldState world) : ICameraRig
{
    private readonly Dictionary<InputLockReason, int> _locks = [];
    public Pose Pose { get; private set; } = new(new(0, 30, 60), 0, -15, 70);
    public float Speed => 0;
    public ViewMode Mode => world.ViewMode;
    public ITransitHandle? ActiveTransit => null;
    public float EdgeFog => 0;
    public bool InputLocked => _locks.Count != 0;
    public void SetPose(Pose pose)
    {
        if (!float.IsFinite(pose.Position.X) || !float.IsFinite(pose.Position.Y) || !float.IsFinite(pose.Position.Z) ||
            !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg) ||
            pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180))
            throw new ArgumentOutOfRangeException(nameof(pose));
        Pose = pose;
    }
    public ITransitHandle Transit(Pose to, TransitOptions? options = null) =>
        throw new NotSupportedException("This domain fixture does not execute camera transit.");
    public IDisposable LockInput(InputLockReason reason)
    {
        _locks[reason] = _locks.GetValueOrDefault(reason) + 1;
        return new Lease(this, reason);
    }
    public int LockCount(InputLockReason reason) => _locks.GetValueOrDefault(reason);
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

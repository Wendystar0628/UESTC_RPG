using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(ICameraRig), Owner = "WP-00", IsStub = true)]
public sealed class CameraRigStub(IWorldState world) : ICameraRig
{
    private readonly Dictionary<InputLockReason, int> _locks = [];
    private Box3? _bounds;
    public Pose Pose { get; private set; } = new(new Vector3(0, 30, 60), 0, -15, 70);
    public float Speed { get; private set; } = 30;
    public ViewMode Mode => world.ViewMode;
    public ITransitHandle? ActiveTransit => null;
    public float EdgeFog => 0;
    public bool InputLocked => _locks.Count != 0;

    public void SetPose(Pose p)
    {
        if (!float.IsFinite(p.Position.X) || !float.IsFinite(p.Position.Y) || !float.IsFinite(p.Position.Z)
            || !float.IsFinite(p.YawDeg) || !float.IsFinite(p.PitchDeg)
            || (p.FovDeg is float fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180)))
            throw new ArgumentOutOfRangeException(nameof(p));
        Pose = p;
    }

    public ITransitHandle Transit(Pose to, TransitOptions? o = null)
    {
        var kind = (o?.Prefer ?? TransitPreference.Auto) switch
        {
            TransitPreference.Auto => TransitKind.Slide,
            TransitPreference.Teleport => TransitKind.Teleport,
            _ => throw new ArgumentOutOfRangeException(nameof(o))
        };
        SetPose(to);
        return new CompletedTransit(kind);
    }

    public IDisposable LockInput(InputLockReason reason)
    {
        _ = reason switch
        {
            InputLockReason.Atlas or InputLockReason.Cinematic or InputLockReason.MapTravel or
            InputLockReason.Menu or InputLockReason.Rebind => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };
        _locks[reason] = _locks.GetValueOrDefault(reason) + 1;
        return new InputLease(this, reason);
    }

    public int LockCount(InputLockReason reason) => _locks.GetValueOrDefault(reason);

    /// <summary>The host supplies map bounds expanded by MapDescriptor.Roam.FallbackMargin.</summary>
    public void ConfigureBounds(Box3 bounds) => _bounds = bounds;

    /// <summary>Host input: local +X right, +Y up, -Z forward; angular delta is in degrees.</summary>
    public void IntegrateInput(Vector3 localMove, Vector2 lookDeltaDeg, float seconds, bool fast = false)
    {
        if (seconds < 0 || !float.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (InputLocked) return;
        float yaw = Pose.YawDeg + lookDeltaDeg.X;
        float pitch = Math.Clamp(Pose.PitchDeg + lookDeltaDeg.Y, -89, 89);
        float rad = yaw * (float.Pi / 180f);
        if (localMove.LengthSquared() > 1) localMove = Vector3.Normalize(localMove);
        Speed = fast ? 90 : 30;
        Vector3 movement = new(localMove.X * MathF.Cos(rad) - localMove.Z * MathF.Sin(rad),
            localMove.Y, localMove.X * MathF.Sin(rad) + localMove.Z * MathF.Cos(rad));
        Vector3 position = Pose.Position + movement * Speed * seconds;
        if (_bounds is Box3 bounds) position = Vector3.Clamp(position, bounds.Min, bounds.Max);
        SetPose(new(position, yaw, pitch, Pose.FovDeg));
    }

    private void Release(InputLockReason reason)
    {
        int count = _locks[reason];
        if (count == 1) _locks.Remove(reason);
        else _locks[reason] = count - 1;
    }

    private sealed class InputLease(CameraRigStub owner, InputLockReason reason) : IDisposable
    {
        private CameraRigStub? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Release(reason);
    }

    private sealed class CompletedTransit(TransitKind kind) : ITransitHandle
    {
        public TransitKind Kind => kind;
        public float Progress => 1;
        public Task<TransitResult> Done { get; } = Task.FromResult(TransitResult.Arrived);
        public void Cancel() { }
    }
}

[KernelService(typeof(IObserverState), Owner = "WP-00", IsStub = true)]
public sealed class ObserverStateStub(ICameraRig camera) : IObserverState
{
    public Vector3 Feet => camera.Pose.Position;
    public Vector3 Eye => camera.Pose.Position;
    public float YawDeg => camera.Pose.YawDeg;
    public NavSpace? Space => null;
    public ViewMode Mode => camera.Mode;
}

[KernelService(typeof(IMediumState), Owner = "WP-00", IsStub = true)]
public sealed class MediumStateStub : IMediumState
{
    public CameraMedium Current => new(MediumKind.Air);
    public MediumKind From => MediumKind.Air;
    public MediumKind To => MediumKind.Air;
    public float Blend => 1;
}

[KernelService(typeof(IInteriorDoors), Owner = "WP-00", IsStub = true)]
public sealed class InteriorDoorsStub : IInteriorDoors
{
    public DoorPhase PhaseOf(FeatureId building, string doorId) => DoorPhase.Open;
    public float OpenOf(FeatureId building, string doorId) => 1;
    public event Action<DoorEvent>? Changed { add { } remove { } }
}

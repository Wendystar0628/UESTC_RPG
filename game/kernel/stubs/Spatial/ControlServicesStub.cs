using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>D0 registration and interface-mode state only; physical keyboard routing is unavailable.</summary>
[KernelService(typeof(IInputRouter), Owner = "WP-00", IsStub = true)]
public sealed class InputRouterStub : IInputRouter
{
    private readonly HashSet<LayerRegistration> _layers = [];

    public bool InterfaceMode { get; private set; }
    public event Action<bool>? InterfaceModeChanged;

    public IDisposable RegisterLayer(string owner, int priority, Func<InputActionEvent, bool> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentNullException.ThrowIfNull(handler);
        // The registration has a lifetime, but this stub never produces or dispatches input events.
        var registration = new LayerRegistration(this);
        _layers.Add(registration);
        return registration;
    }

    public void SetInterfaceMode(bool enabled)
    {
        if (InterfaceMode == enabled) return;
        InterfaceMode = enabled;
        InterfaceModeChanged?.Invoke(enabled);
    }

    public bool TryRebind(string action, int physicalKey, out string? conflict)
    {
        conflict = "Physical input rebinding is unavailable in the D0 input-router stub.";
        return false;
    }

    public int PhysicalKey(string action) => 0;

    public void SaveBindings() => throw new NotSupportedException("Binding persistence is unavailable in the D0 input-router stub.");

    private sealed class LayerRegistration(InputRouterStub owner) : IDisposable
    {
        private InputRouterStub? _owner = owner;

        public void Dispose()
        {
            _owner?._layers.Remove(this);
            _owner = null;
        }
    }
}

/// <summary>D0 dependency placeholder only; it never activates physics or accepts a destination.</summary>
[KernelService(typeof(IWalkController), Owner = "WP-00", IsStub = true)]
public sealed class WalkControllerStub : IWalkController
{
    public Pose EyePose => default;
    public Vector3 ObserverEye => default;
    public Pose BodyPose => default;
    public Vector3 Velocity => default;
    public bool Grounded => false;
    public bool Active => false;
    public bool CollisionReady => false;
    public NavSpace? Space => null;
    public int SpeedStep { get; private set; } = 2;
    public int SafetyReturnCount => 0;
    public bool IsSettled => false;
    public Pose SettledEyePose => throw new NotSupportedException("A settled walk pose is unavailable in the D0 walk-controller stub.");

    public ModeSwitchResult Enter(Pose from) => ModeSwitchResult.Refused;
    public void Leave() { }
    public void SetLook(float yawDeg, float pitchDeg) { }
    public void SetInput(WalkInput input) { }

    public void SetSpeedStep(int step)
    {
        if (step is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(step));
        SpeedStep = step;
    }

    public bool ReturnToSafety() => false;
    public void Warp(Pose feet) => throw new NotSupportedException("Feet placement is unavailable in the D0 walk-controller stub.");
}

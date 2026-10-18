using Campus.Contracts;
using Godot;

namespace Campus.Camera;

// Input leases and the UI/capture interface mode.
public partial class ObserverCamera
{
    public IDisposable LockInput(InputLockReason reason)
    {
        _input.Clear(); _flight.Reset(); _walk?.SetInput(default);
        var lease = _locks.Acquire(reason);
        SetInterfaceMode(true);
        return lease;
    }

    public bool InterfaceMode { get; private set; } = true;
    public event Action<bool>? InterfaceModeChanged;

    public void SetInterfaceMode(bool enabled)
    {
        if (!enabled && (!IsPlaying || _locks.Locked)) enabled = true;
        bool changed = InterfaceMode != enabled;
        InterfaceMode = enabled;
        Input.MouseMode = enabled ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        if (changed) InterfaceModeChanged?.Invoke(enabled);
    }
}

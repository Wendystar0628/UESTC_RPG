using Campus.Contracts.Settings;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

// IInputRouter key-binding members; storage and validation live in InputBindings.
public partial class ObserverCamera
{
    public bool TryRebind(string action, int physicalKey, out string? conflict)
    {
        bool changed = _bindings.Rebind(action, physicalKey, out conflict);
        if (changed) { _input.Clear(); _flight.Reset(); SaveBindings(); }
        return changed;
    }
    public int PhysicalKey(string action) => action == "menu" ? (int)Key.Escape : _bindings.PhysicalKey(action);
    public void SaveBindings() { if (!_automated) _ = _bindings.Save(KernelServices.Get<ISettingsStore>()); }
}

using Campus.Contracts;

namespace Campus.Kernel.Stubs.AudioUi;

/// <summary>D0 reports only map surface flags; creates no real interface.</summary>
[KernelService(typeof(IUiSurfaceState), Owner = "WP-00", IsStub = true)]
[KernelService(typeof(IUiSurfaceSink), Owner = "WP-00", IsStub = true)]
public sealed class UiSurfaceStateStub : IUiSurfaceState, IUiSurfaceSink
{
    public UiSurface Open { get; private set; }
    public bool InterfaceMode => false;
    public string? SettingsPage => null;
    public long Rev { get; private set; }
    public event Action? Changed;

    public void Report(UiSurface surface, bool open)
    {
        if (surface is not (UiSurface.Minimap or UiSurface.Atlas or UiSurface.AtlasIndex))
            throw new ArgumentOutOfRangeException(nameof(surface), surface, "Only one map surface may be reported.");
        var next = open ? Open | surface : Open & ~surface;
        if (next == Open) return;
        Open = next;
        Rev++;
        Changed?.Invoke();
    }
}

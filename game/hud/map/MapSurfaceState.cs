using Campus.Contracts;

namespace Campus.Hud.Map;

/// <summary>Published map surfaces; controllers report their existing page and minimap visibility.</summary>
[KernelService(typeof(IUiSurfaceState))]
[KernelService(typeof(IUiSurfaceSink))]
public sealed class MapSurfaceState : IUiSurfaceState, IUiSurfaceSink
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

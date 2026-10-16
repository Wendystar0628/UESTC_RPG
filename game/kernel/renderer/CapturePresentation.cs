using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Thumbnail-only UI lease; the world, veil and attribution layers are unaffected.</summary>
public static class CapturePresentation
{
    private static CanvasLayer? _hud;
    private static bool _wasVisible;
    private static int _leases;
    public static void Bind(CanvasLayer hud)
    {
        ArgumentNullException.ThrowIfNull(hud);
        if (_leases != 0) throw new InvalidOperationException("Cannot replace the HUD during a capture.");
        _hud = hud;
    }
    public static IDisposable HideHud()
    {
        var hud = _hud ?? throw new InvalidOperationException("The stage UI layer is not ready.");
        if (_leases++ == 0) { _wasVisible = hud.Visible; hud.Visible = false; }
        return new Lease(hud);
    }
    private sealed class Lease(CanvasLayer hud) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (--_leases == 0 && GodotObject.IsInstanceValid(hud)) hud.Visible = _wasVisible;
        }
    }
}

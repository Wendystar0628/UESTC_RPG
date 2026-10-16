using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stubs.Visual;

/// <summary>The existing D0 fog rectangle, with coverage certified only after its actual viewport draw.</summary>
public partial class D0VeilPresentation : CanvasLayer, IVeilPresentation
{
    private readonly ColorRect _cover;
    private Vector2 _drawnSize;
    private bool _drawnOpaque;

    public D0VeilPresentation(Color color)
    {
        Layer = 5;
        _cover = new ColorRect { Color = color with { A = 0f }, MouseFilter = Control.MouseFilterEnum.Ignore };
    }

    public bool IsCovered => _drawnOpaque && CoversViewport() && _drawnSize == GetViewport().GetVisibleRect().Size;

    public override void _Ready()
    {
        AddChild(_cover);
        _cover.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        RenderingServer.Singleton.FramePostDraw += OnFrameDrawn;
    }

    public void Present(in VeilFrame frame)
    {
        if (!float.IsFinite(frame.Opacity)) throw new ArgumentOutOfRangeException(nameof(frame));
        // The D0 fallback has no frontispiece asset; its existing fog colour remains the cover.
        _cover.Color = _cover.Color with { A = Mathf.Clamp(frame.Opacity, 0f, 1f) };
        if (frame.Opacity < 1f) _drawnOpaque = false;
    }

    private bool CoversViewport()
    {
        if (!IsInsideTree() || !Visible || !_cover.IsVisibleInTree() || _cover.Color.A < 1f) return false;
        var viewport = GetViewport().GetVisibleRect();
        var actual = _cover.GetGlobalRect();
        return viewport.Size.X > 0f && viewport.Size.Y > 0f &&
            actual.Position.X <= viewport.Position.X && actual.Position.Y <= viewport.Position.Y &&
            actual.End.X >= viewport.End.X && actual.End.Y >= viewport.End.Y;
    }

    private void OnFrameDrawn()
    {
        _drawnOpaque = CoversViewport();
        _drawnSize = GetViewport().GetVisibleRect().Size;
    }

    public override void _ExitTree() => RenderingServer.Singleton.FramePostDraw -= OnFrameDrawn;
}

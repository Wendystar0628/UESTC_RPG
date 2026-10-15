using Godot;

namespace Campus.Hud.Map;

/// <summary>只负责画的薄层：把 _Draw 交给持有它的部件，避免为一两样静态图形各建一个 Control。</summary>
public partial class DrawHost : Control
{
    private readonly Action<DrawHost> _draw;

    public DrawHost(Action<DrawHost> draw)
    {
        _draw = draw;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw() => _draw(this);
}

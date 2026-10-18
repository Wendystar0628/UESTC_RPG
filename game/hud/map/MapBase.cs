using Godot;

namespace Campus.Hud.Map;

/// <summary>底图视口：挂 map_base 材质，每帧只写一次 instance uniform（SYS-ATLAS 的登记例外）。</summary>
public partial class MapBase : Control
{
    private static readonly StringName ViewParam = new("view");
    private static readonly StringName ViewSizeParam = new("view_size");
    private static readonly StringName PinParam = new("pin_night");
    private static readonly StringName FlagsParam = new("flags");
    private static readonly StringName ChamferParam = new("chamfer");
    private const int FlagCrosses = 1;

    private readonly bool _crosses;
    private MapView _view = new(Vector2.Zero, 1, 0);

    public MapBase(bool crosses, float chamfer = 0)
    {
        _crosses = crosses;
        Chamfer = chamfer;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public MapView View => _view;
    public float Chamfer { get; }

    public void Bind(ShaderMaterial material, bool pinNight)
    {
        Material = material;
        SetInstanceShaderParameter(PinParam, pinNight ? 1f : 0f);
        SetInstanceShaderParameter(FlagsParam, _crosses ? FlagCrosses : 0);
        SetInstanceShaderParameter(ChamferParam, Chamfer);
        SetInstanceShaderParameter(ViewSizeParam, Size);
        SetInstanceShaderParameter(ViewParam, _view.Uniform);
        QueueRedraw();
    }

    public void SetPinNight(bool pinNight) => SetInstanceShaderParameter(PinParam, pinNight ? 1f : 0f);

    public void SetView(MapView view)
    {
        _view = view;
        SetInstanceShaderParameter(ViewParam, view.Uniform);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && Material is not null) SetInstanceShaderParameter(ViewSizeParam, Size);
    }

    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), Colors.White);
}

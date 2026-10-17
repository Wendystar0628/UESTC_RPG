using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 底图视口：挂 map_base 材质时每帧只写一次 instance uniform（SYS-ATLAS 的登记例外）；材质还没取到时，
/// 按"底图未就绪"的画法用 _Draw 画校内用地一种颜色（大地图另画 100 m 十字测试点），只在视图变了才重画。
/// </summary>
public partial class MapBase : Control
{
    private static readonly StringName ViewParam = new("view");
    private static readonly StringName ViewSizeParam = new("view_size");
    private static readonly StringName PinParam = new("pin_night");
    private static readonly StringName FlagsParam = new("flags");
    private static readonly StringName ChamferParam = new("chamfer");
    private const int FlagCrosses = 1;

    private readonly MapHudContext _ctx;
    private readonly bool _crosses;
    private MapView _view = new(Vector2.Zero, 1, 0);
    private MapView _drawn;
    private bool _drawnNight;

    public MapBase(MapHudContext ctx, bool crosses, float chamfer = 0)
    {
        _ctx = ctx;
        _crosses = crosses;
        Chamfer = chamfer;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public MapView View => _view;
    public float Chamfer { get; }

    public void Bind(Material? material, bool pinNight)
    {
        Material = material;
        if (material is null) return;
        SetInstanceShaderParameter(PinParam, pinNight ? 1f : 0f);
        SetInstanceShaderParameter(FlagsParam, _crosses ? FlagCrosses : 0);
        SetInstanceShaderParameter(ChamferParam, Chamfer);
        SetInstanceShaderParameter(ViewSizeParam, Size);
        SetInstanceShaderParameter(ViewParam, _view.Uniform);
        QueueRedraw();
    }

    public void SetPinNight(bool pinNight)
    {
        if (Material is not null) SetInstanceShaderParameter(PinParam, pinNight ? 1f : 0f);
        else QueueRedraw();
    }

    public void SetView(MapView view)
    {
        _view = view;
        if (Material is not null) SetInstanceShaderParameter(ViewParam, view.Uniform);
        else
        {
            var night = _ctx.ToneFor(this).IsNight;
            if (view != _drawn || night != _drawnNight) QueueRedraw();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && Material is not null) SetInstanceShaderParameter(ViewSizeParam, Size);
    }

    public override void _Draw()
    {
        var size = Size;
        if (Material is not null)
        {
            DrawRect(new Rect2(Vector2.Zero, size), Colors.White);
            return;
        }
        var tone = _ctx.ToneFor(this);
        _drawn = _view;
        _drawnNight = tone.IsNight;
        DrawColoredPolygon(MapDraw.ChipPoints(new Rect2(Vector2.Zero, size), Chamfer), tone.Land);
        if (!_crosses) return;
        var cross = (tone.IsNight ? _ctx.Style.MapNight : _ctx.Style.MapDay)[1];
        const float Pitch = 100f;
        var a = _view.ToWorld(Vector2.Zero, size);
        var b = _view.ToWorld(size, size);
        var x0 = MathF.Floor(MathF.Min(a.X, b.X) / Pitch) * Pitch;
        var z0 = MathF.Floor(MathF.Min(a.Y, b.Y) / Pitch) * Pitch;
        var lines = new List<Vector2>();
        for (var x = x0; x <= MathF.Max(a.X, b.X) + Pitch; x += Pitch)
            for (var z = z0; z <= MathF.Max(a.Y, b.Y) + Pitch; z += Pitch)
            {
                var p = _view.ToLocal(new Vector2(x, z), size).Round() + new Vector2(0.5f, 0.5f);
                if (p.X < -4 || p.Y < -4 || p.X > size.X + 4 || p.Y > size.Y + 4) continue;
                lines.Add(p - new Vector2(3, 0)); lines.Add(p + new Vector2(4, 0));
                lines.Add(p - new Vector2(0, 3)); lines.Add(p + new Vector2(0, 4));
            }
        if (lines.Count > 0) DrawMultiline([.. lines], cross, MapDraw.Hair);
    }
}

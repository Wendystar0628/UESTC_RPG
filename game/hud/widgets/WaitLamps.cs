using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 等待灯（UI-VISUAL-COMPONENTS §4.5）：三枚过孔依次点亮再一起熄，不用进度条、不显示百分比。
/// 何时出现由雾幕服务的 <see cref="IVeilState.Waiting"/> 决定（1.5 s / 扉页 4 s 的延迟已在服务里），这里只随它显隐。
/// 不显示时不留件（<see cref="UiKit.Holders"/>）：亮起时建、淡出后释放。减少动态效果下三枚常亮，由文字陪着。
/// 开始页贴右下角，行走中四角有常驻件，改放底边居中；应用加载页不出等待灯。
/// </summary>
public partial class WaitLamps : Control
{
    private const int LampCount = 3;

    private UiKit _kit = null!;
    private IVeilState _veil = null!;
    private PanelContainer? _panel;
    private Label? _label;
    private readonly Panel[] _lamps = new Panel[LampCount];
    private int _lit = -1;
    private bool _atCorner = true;
    private float _clock, _alpha;

    public static WaitLamps Create(UiKit kit, Control parent, IVeilState veil)
    {
        var host = new WaitLamps { _kit = kit, _veil = veil, Name = "WaitLamps", MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(host);
        host.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return host;
    }

    /// <summary>由界面根每帧调用；atCorner 为真时贴右下角，否则底边居中。</summary>
    public void Tick(double delta, bool atCorner, bool suppressed = false)
    {
        Visible = !suppressed;
        var waiting = _veil.Waiting;
        if (waiting && _panel is null) Build();
        if (_panel is null) return;
        // 只在等待期间、且服务报告本次是首次编译时换成说明；淡出期间保持原样不再跳字
        if (waiting)
        {
            var text = UiKit.T(_veil.FirstShaderCompilation ? "hud.wait.first_shader" : "hud.wait.preparing");
            _label!.Text = _kit.Theme.Upper("data") ? text.ToUpperInvariant() : text;
        }
        if (atCorner != _atCorner) { _atCorner = atCorner; Place(); }

        var fade = _kit.Motion.Fade(_kit.Tokens.Ms("waitLamps", "fade"));
        var target = waiting ? 1f : 0f;
        _alpha = fade <= 0 ? target : Mathf.MoveToward(_alpha, target, (float)delta / fade);
        if (_alpha <= 0 && !waiting)
        {
            _panel.QueueFree();
            _panel = null;
            return;
        }
        _panel.Modulate = new Color(1, 1, 1, _alpha);

        _clock += (float)delta;
        SetLit(Lit());
    }

    private int Lit()
    {
        if (_kit.Motion.ReduceMotion) return LampCount;
        var period = _kit.Tokens.Ms("waitLamps", "period");
        var window = period - _kit.Tokens.Ms("waitLamps", "allOff");
        var phase = _clock % period;
        return phase >= window ? 0 : Math.Min(LampCount, (int)(phase / (window / LampCount)) + 1);
    }

    private void SetLit(int count)
    {
        if (count == _lit) return;
        _lit = count;
        for (var i = 0; i < LampCount; i++)
        {
            var on = i < count;
            _kit.Apply(_lamps[i], new UiFx(Fill: on ? "led.on" : null, Line: on ? null : "graphic", Flags: UiFlags.Circle, Pin: true), remember: false);
        }
    }

    private void Build()
    {
        var k = _kit;
        _clock = 0;
        _alpha = 0;
        _lit = -1;
        _panel = new PanelContainer
        {
            Name = "WaitLampsPanel", MouseFilter = MouseFilterEnum.Ignore,
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType, Material = k.Materials.Panel,
            Modulate = new Color(1, 1, 1, 0)
        };
        AddChild(_panel);
        k.Apply(_panel, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: k.Tokens.Chamfer("panel"), Pin: true));
        var row = k.Layout.Row(k.Layout.Margin(_panel));
        var via = k.Tokens.F("metrics", "via", "core");
        for (var i = 0; i < LampCount; i++)
        {
            _lamps[i] = k.Box(row, new UiFx(Line: "graphic", Flags: UiFlags.Circle, Pin: true));
            _lamps[i].CustomMinimumSize = new Vector2(via, via);
            _lamps[i].SizeFlagsVertical = SizeFlags.ShrinkCenter;
        }
        _label = k.Text(row, UiKit.T("hud.wait.preparing"), "data", "text.2", true, wrap: false);
        _label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        Place();
    }

    private void Place()
    {
        if (_panel is null) return;
        _panel.SetAnchorsAndOffsetsPreset(_atCorner ? LayoutPreset.BottomRight : LayoutPreset.CenterBottom);
        _panel.GrowHorizontal = _atCorner ? GrowDirection.Begin : GrowDirection.Both;
        _panel.GrowVertical = GrowDirection.Begin;
    }
}

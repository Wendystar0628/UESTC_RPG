using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 分段选择（UI-VISUAL）：外框 1 px line.control，段间发丝线；选中 = 选中底 + text.1 Medium + 底边 2 px 指示条，
/// 指示条随选中态点亮，容器子件不做位移动效；禁用段可聚焦并读出原因。
/// </summary>
public partial class Segmented : PanelContainer
{
    private readonly List<TechButton> _items = [];
    private readonly List<Panel> _bars = [];
    private int _selected = -1;

    public event Action<int>? Changed;
    public IReadOnlyList<TechButton> Items => _items;
    public int Selected => _selected;

    public static Segmented Create(UiKit kit, Control parent, string[] labels, bool pin = false,
        string font = "segment", string?[]? icons = null, int selected = -1)
    {
        var seg = new Segmented
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType,
            Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        parent.AddChild(seg);
        kit.Apply(seg, new UiFx(Line: "line.control", Pin: pin));
        var row = kit.Layout.Row(seg);
        row.ThemeTypeVariation = UiThemeBuilder.Segments;
        var group = new ButtonGroup { AllowUnpress = false };
        for (var i = 0; i < labels.Length; i++)
        {
            var button = kit.Button(row, ButtonStyle.Segment, labels[i], font, icons?[i], pin);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.Value = i;
            var index = i;
            button.Toggled += on => { if (on) seg.Select(index, notify: true); };
            seg._items.Add(button);
            var bar = kit.Box(button, new UiFx(Fill: "primary.line", Pin: pin));
            bar.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            bar.OffsetTop = -2;
            seg._bars.Add(bar);
            if (i > 0)
            {
                var line = kit.Box(button, new UiFx(Fill: "line.hair", Pin: pin));
                line.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
                line.OffsetRight = 1;
            }
        }
        seg.Select(selected, notify: false);
        return seg;
    }

    public void Select(int index, bool notify)
    {
        var next = index >= 0 && index < _items.Count ? index : -1;
        var changed = next != _selected;
        _selected = next;
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Selected = i == next;
            _items[i].SetPressedNoSignal(i == next);
            _bars[i].Visible = i == next;
        }
        if (changed && notify) Changed?.Invoke(next);
    }

    public void SetOff(int index, bool off, string? reason)
    {
        _items[index].OffReason = reason;
        _items[index].Off = off;
    }
}

/// <summary>开关（拨码，UI-VISUAL）：40×20，开 = 轨道 primary.fill、边 primary.line、拨钮右移；右侧必须写"开 / 关"。</summary>
public partial class DipSwitch : Control
{
    private UiKit _kit = null!;
    private HBoxContainer _row = null!;
    private Panel _track = null!, _knob = null!;
    private Label _label = null!;
    private TechButton _hit = null!;
    private bool _on, _pin;
    public event Action<bool>? Toggled;
    public bool On => _on;

    public override Vector2 _GetMinimumSize() => _row?.GetCombinedMinimumSize() ?? Vector2.Zero;

    public static DipSwitch Create(UiKit kit, Control parent, bool on, bool pin = false)
    {
        var sw = new DipSwitch { _kit = kit, _on = on, _pin = pin, MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(sw);
        sw._row = kit.Layout.Row(sw);
        sw._row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        sw._row.MinimumSizeChanged += sw.UpdateMinimumSize;
        var geometry = kit.Tokens.V2("metrics", "size", "switch");
        var drawing = kit.Layout.Add(sw._row, new Control { CustomMinimumSize = geometry, SizeFlagsVertical = SizeFlags.ShrinkCenter });
        sw._track = kit.Box(drawing, new UiFx(Pin: pin));
        sw._track.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        sw._knob = kit.Box(drawing, new UiFx(Fill: "surface.raised", Line: "line.control", Pin: pin));
        sw._knob.Size = kit.Tokens.V2("metrics", "size", "switchKnob");
        sw._label = kit.Text(sw._row, "", "small", "text.2", pin, wrap: false);
        sw._label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var font = kit.Theme.Font("small", out var fontSize, out _);
        sw._label.CustomMinimumSize = new Vector2(new[] { "ui.common.on", "ui.common.off" }
            .Max(key => font.GetStringSize(UiKit.T(key), fontSize: fontSize).X), 0);
        sw._hit = kit.Button(sw, ButtonStyle.Ghost, "", pin: pin);
        sw._hit.CustomMinimumSize = new Vector2(0, kit.Layout.Metric("compact"));
        sw._hit.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        sw._hit.SelfModulate = new Color(1, 1, 1, 0);
        sw._hit.Pressed += () => sw.Set(!sw._on, notify: true);
        sw._hit.FocusEntered += sw.Refresh;
        sw._hit.FocusExited += sw.Refresh;
        sw.CustomMinimumSize = new Vector2(0, kit.Layout.Metric("compact"));
        sw.Refresh();
        return sw;
    }

    public void Set(bool on, bool notify)
    {
        if (on == _on) return;
        _on = on;
        Refresh();
        if (notify) Toggled?.Invoke(on);
    }

    private void Refresh()
    {
        var focus = _hit.HasFocus(true) ? 1f : 0f;
        _kit.Apply(_track, new UiFx(Fill: _on ? "primary.fill" : "surface.sunk", Line: _on ? "primary.line" : "line.control", Pin: _pin, Focus: focus));
        var track = _kit.Tokens.V2("metrics", "size", "switch");
        var inset = (track.Y - _knob.Size.Y) / 2;
        _knob.Position = new Vector2(_on ? track.X - inset - _knob.Size.X : inset, inset);
        _label.Text = UiKit.T(_on ? "ui.common.on" : "ui.common.off");
    }
}

/// <summary>推子的轨道、刻度和帽是自绘几何；读数由 HBox 排版，宽度由端点文字推导。</summary>
public partial class Fader : Control
{
    private UiKit _kit = null!;
    private HBoxContainer _row = null!;
    private Control _drawing = null!;
    private Panel _track = null!, _fill = null!, _cap = null!, _capLine = null!;
    private readonly List<Panel> _ticks = [];
    private Label _value = null!;
    private Func<float, string> _format = v => v.ToString("0.##");
    private float _min, _max, _step, _v;
    private bool _dragging, _pin;
    public event Action<float>? Changed;
    public float Value => _v;
    public override Vector2 _GetMinimumSize() => _row?.GetCombinedMinimumSize() ?? Vector2.Zero;

    public static Fader Create(UiKit kit, Control parent, float min, float max, float step, float value,
        bool pin = false, Func<float, string>? format = null)
    {
        var f = new Fader
        {
            _kit = kit, _min = min, _max = max, _step = step, _v = value, _pin = pin,
            MouseFilter = MouseFilterEnum.Stop, FocusMode = FocusModeEnum.All,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        if (format is not null) f._format = format;
        parent.AddChild(f);
        f._row = kit.Layout.Row(f);
        f._row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        f._row.MinimumSizeChanged += f.UpdateMinimumSize;
        f._drawing = kit.Layout.Add(f._row, new Control
        {
            CustomMinimumSize = new Vector2(kit.Layout.Metric("faderTrackMinWidth"), kit.Layout.Metric("dropdown")),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        f._track = kit.Box(f._drawing, new UiFx(Fill: "line.control", Pin: pin));
        f._fill = kit.Box(f._drawing, new UiFx(Fill: "primary.line", Pin: pin));
        if (step > 0 && max > min && (max - min) / step <= 12)
            for (var i = 0; i <= (int)Math.Round((max - min) / step); i++)
                f._ticks.Add(kit.Box(f._drawing, new UiFx(Fill: "line.control", Pin: pin)));
        f._cap = kit.Box(f._drawing, new UiFx(Fill: "surface.raised", Line: "line.control", Pin: pin));
        f._capLine = kit.Box(f._drawing, new UiFx(Fill: "line.control", Pin: pin));
        f._value = kit.Text(f._row, "", "data", "text.1", pin, wrap: false);
        f._value.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        f._value.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var font = kit.Theme.Font("data", out var size, out _);
        f._value.CustomMinimumSize = new Vector2(new[] { min, max, value }.Max(v => font.GetStringSize(f._format(v), fontSize: size).X), 0);
        f._drawing.Resized += f.Layout;
        f.FocusEntered += f.Layout;
        f.FocusExited += f.Layout;
        f.Layout();
        return f;
    }

    public void Set(float value, bool notify)
    {
        var v = Math.Clamp(value, _min, _max);
        if (_step > 0) v = Math.Clamp(_min + MathF.Round((v - _min) / _step) * _step, _min, _max);
        if (MathF.Abs(v - _v) < 1e-5f) return;
        _v = v;
        Layout();
        if (notify) Changed?.Invoke(_v);
    }

    private void Layout()
    {
        var cap = _kit.Tokens.V2("metrics", "size", "fader");
        var half = cap.X / 2;
        var width = Math.Max(1, _drawing.Size.X - cap.X);
        var mid = _drawing.Size.Y / 2;
        var t = _max > _min ? (_v - _min) / (_max - _min) : 0;
        var x = half + Mathf.Round(width * t);
        _track.Position = new Vector2(half, mid);
        _track.Size = new Vector2(width, 1);
        _fill.Position = new Vector2(half, mid - 0.5f);
        _fill.Size = new Vector2(width * t, 2);
        _cap.Position = new Vector2(x - half, mid - cap.Y / 2);
        _cap.Size = cap;
        _capLine.Position = new Vector2(x - 0.5f, mid - cap.Y / 4);
        _capLine.Size = new Vector2(1, cap.Y / 2);
        for (var i = 0; i < _ticks.Count; i++)
        {
            _ticks[i].Position = new Vector2(half + width * i / Math.Max(1, _ticks.Count - 1), mid + cap.Y / 2 - 4);
            _ticks[i].Size = new Vector2(1, 5);
        }
        _value.Text = _format(_v);
        var focus = HasFocus(true) ? 1f : 0f;
        _kit.Apply(_cap, new UiFx(Fill: "surface.raised", Line: _dragging ? "primary.line" : "line.control", Pin: _pin, Focus: focus), remember: false);
    }

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb:
                _dragging = mb.Pressed;
                if (mb.Pressed) SetFromX(mb.Position.X);
                Layout();
                AcceptEvent();
                break;
            case InputEventMouseMotion mm when _dragging:
                SetFromX(mm.Position.X);
                AcceptEvent();
                break;
            default:
                var unit = _step > 0 ? _step : (_max - _min) / 20f;
                if (e.IsActionPressed("ui_left", true)) { Set(_v - unit, true); AcceptEvent(); }
                else if (e.IsActionPressed("ui_right", true)) { Set(_v + unit, true); AcceptEvent(); }
                break;
        }
    }

    private void SetFromX(float x)
    {
        var half = _kit.Tokens.V2("metrics", "size", "fader").X / 2;
        Set(_min + Math.Clamp((x - half) / Math.Max(1, _drawing.Size.X - half * 2), 0, 1) * (_max - _min), true);
    }
}

/// <summary>小件：键帽、组标题、生效标签、LED 格。</summary>
public static class Parts
{
    public static PanelContainer Keycap(UiKit kit, Control parent, string key, bool down = false, bool pin = false)
    {
        var panel = kit.Panel(parent, new UiFx(Fill: down ? "primary.fill" : "surface.raised", Line: "line.control", Pin: pin));
        var edge = kit.Layout.Metric("keycap");
        panel.CustomMinimumSize = new Vector2(edge, edge);
        panel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        var margin = kit.Layout.Add(panel, new MarginContainer(), UiThemeBuilder.SmallMargin);
        kit.Text(margin, key, "keycap", down ? "primary.ink" : "text.1", pin, wrap: false, align: HorizontalAlignment.Center);
        return panel;
    }

    /// <summary>
    /// 动作当前绑定的键名：键码取输入路由（改键后即变），左右侧取 InputMap 里同键事件的 Location，
    /// 只有绑定指明左或右时才写出侧别。
    /// </summary>
    public static string KeyLabel(IInputRouter router, string action)
    {
        var physical = router.PhysicalKey(action);
        var keys = InputMap.ActionGetEvents(action).OfType<InputEventKey>();
        var bound = physical == 0 ? keys.FirstOrDefault() : keys.FirstOrDefault(key => (int)key.PhysicalKeycode == physical);
        if (physical == 0) physical = (int)(bound?.PhysicalKeycode ?? Key.None);
        if (physical == 0) return "—";
        var name = KeyName((Key)physical);
        return bound?.Location switch
        {
            KeyLocation.Left => UiKit.T("key.side.left", ("key", name)),
            KeyLocation.Right => UiKit.T("key.side.right", ("key", name)),
            _ => name,
        };
    }

    private static string KeyName(Key code)
    {
        var text = OS.GetKeycodeString(code);
        return text switch { "Escape" => "Esc", "Period" => ".", "Space" => "Space", _ => text };
    }

    /// <summary>
    /// 常驻图标钮右下角的快捷键标，悬浮说明写"名称 · 键"（UI-NAV、UI-VISUAL-COMPONENTS）。
    /// 改键只在设置卡里做，设置卡开着时四角件隐藏；键标随所在界面重新显示时取当前绑定。
    /// </summary>
    public static Label KeyHint(UiKit kit, TechButton button, string name, Func<string> key, bool pin = false)
    {
        var hint = kit.Text(button, "", "tick", "text.2", pin, wrap: false, align: HorizontalAlignment.Right);
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        hint.GrowHorizontal = Control.GrowDirection.Begin;
        hint.GrowVertical = Control.GrowDirection.Begin;
        button.AccessibilityName = name;
        void Sync()
        {
            var text = key();
            hint.Text = text;
            button.TooltipText = name + " · " + text;
        }
        hint.VisibilityChanged += () => { if (hint.IsVisibleInTree()) Sync(); };
        Sync();
        return hint;
    }

    public static HBoxContainer GroupHeader(UiKit kit, Control parent, string text, bool pin = false)
    {
        var row = kit.Layout.Row(parent);
        var label = kit.Text(row, text, "mono-caps", "text.2", pin, wrap: false);
        label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        var line = kit.Box(row, new UiFx(Fill: "line.hair", Pin: pin));
        line.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        line.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        line.CustomMinimumSize = new Vector2(0, 1);
        return row;
    }

    public static PanelContainer Tag(UiKit kit, Control parent, string text, bool primary, bool pin = false)
    {
        var panel = kit.Panel(parent, new UiFx(Line: primary ? "primary.line" : "line.control", Pin: pin));
        panel.CustomMinimumSize = new Vector2(0, kit.Layout.Metric("tag"));
        panel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        var margin = kit.Layout.Add(panel, new MarginContainer(), UiThemeBuilder.SmallMargin);
        kit.Text(margin, text, "data", "text.2", pin, wrap: false);
        return panel;
    }
}

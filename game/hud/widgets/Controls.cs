using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 分段选择（UI-VISUAL）：外框 1 px line.control，段间发丝线；选中 = 选中底 + text.1 Medium + 底边 2 px 指示条，
/// 指示条用空间弹簧（0.22 s）滑到新项；禁用段可聚焦并读出原因；组内方向键切换。
/// </summary>
public partial class Segmented : Control
{
    private readonly List<TechButton> _items = [];
    private readonly List<float> _x = [];
    private UiKit _kit = null!;
    private Panel _bar = null!;
    private Tween? _slide;
    private int _selected = -1;

    public event Action<int>? Changed;
    public IReadOnlyList<TechButton> Items => _items;
    public int Selected => _selected;

    public static Segmented Create(UiKit kit, Control parent, Vector2 position, float[] widths, string[] labels, bool pin,
        float height = 36, string font = "segment", string?[]? icons = null, int iconMax = 16, int selected = -1)
    {
        var total = widths.Sum();
        var seg = new Segmented { _kit = kit, Position = position, Size = new Vector2(total, height), MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(seg);
        kit.Box(seg, new Rect2(0, 0, total, height), new UiFx(Line: "line.control", Pin: pin));
        var group = new ButtonGroup { AllowUnpress = false };
        float x = 0;
        for (var i = 0; i < labels.Length; i++)
        {
            if (i > 0) kit.Box(seg, new Rect2(x, 1, 1, height - 2), new UiFx(Fill: "line.hair", Pin: pin));
            var b = kit.Button(seg, ButtonStyle.Segment, labels[i], new Rect2(x + 1, 1, widths[i] - 2, height - 2), font, 6,
                icons?[i], iconMax, pin, padY: 2);
            b.ToggleMode = true;
            b.ButtonGroup = group;
            b.Value = i;
            var index = i;
            b.Toggled += on => { if (on) seg.Select(index, notify: true); };
            seg._items.Add(b);
            seg._x.Add(x);
            x += widths[i];
        }
        seg._bar = kit.Box(seg, new Rect2(1, height - 3, widths[0] - 2, 2), new UiFx(Fill: "primary.line", Pin: pin));
        seg._bar.Visible = false;
        seg.Select(selected, notify: false);
        return seg;
    }

    public void Select(int index, bool notify)
    {
        if (index < 0 || index >= _items.Count)
        {
            _selected = -1;
            foreach (var item in _items) { item.Selected = false; item.SetPressedNoSignal(false); }
            _bar.Visible = false;
            return;
        }
        var changed = index != _selected;
        _selected = index;
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Selected = i == index;
            _items[i].SetPressedNoSignal(i == index);
        }
        var target = new Vector2(_x[index] + 1, _bar.Position.Y);
        var width = _items[index].Size.X;
        var wasVisible = _bar.Visible;
        _bar.Visible = true;
        _slide?.Kill();
        var response = _kit.Tokens.F("motion", "durations", "select", "response");
        var seconds = wasVisible && _kit.Motion.Spatial ? Motion.UiEase.SpringSettle(response) : 0;
        if (seconds <= 0) { _bar.Position = target; _bar.Size = new Vector2(width, 2); }
        else
        {
            var fromX = _bar.Position.X;
            var fromW = _bar.Size.X;
            _slide = CreateTween();
            _slide.TweenMethod(Callable.From<float>(t =>
            {
                var k = Motion.UiEase.Spring(t * seconds, response);
                _bar.Position = new Vector2(fromX + (target.X - fromX) * k, target.Y);
                _bar.Size = new Vector2(fromW + (width - fromW) * k, 2);
            }), 0f, 1f, seconds);
        }
        if (changed && notify) Changed?.Invoke(index);
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
    private Panel _track = null!, _knob = null!;
    private Label _label = null!;
    private TechButton _hit = null!;
    private bool _on, _pin;

    public event Action<bool>? Toggled;
    public bool On => _on;

    public static DipSwitch Create(UiKit kit, Control parent, Vector2 position, bool on, bool pin)
    {
        var sw = new DipSwitch { _kit = kit, _on = on, _pin = pin, Position = position, Size = new Vector2(80, 20), MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(sw);
        sw._track = kit.Box(sw, new Rect2(0, 0, 40, 20), new UiFx(Pin: pin));
        sw._knob = kit.Box(sw, new Rect2(3, 3, 14, 14), new UiFx(Fill: "surface.raised", Line: "line.control", Pin: pin));
        sw._label = kit.Text(sw, "", "small", "text.2", new Vector2(48, -1), pin);
        sw._hit = kit.Button(sw, ButtonStyle.Ghost, "", new Rect2(-4, -6, 80, 32), pin: pin);
        sw._hit.SelfModulate = new Color(1, 1, 1, 0);
        sw._hit.Pressed += () => sw.Set(!sw._on, notify: true);
        sw._hit.FocusEntered += sw.Refresh;
        sw._hit.FocusExited += sw.Refresh;
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
        var focus = _hit.HasFocus() && !Input.IsMouseButtonPressed(MouseButton.Left) ? 1f : 0f;
        _kit.Apply(_track, new UiFx(Fill: _on ? "primary.fill" : "surface.sunk", Line: _on ? "primary.line" : "line.control", Pin: _pin, Focus: focus));
        _knob.Position = new Vector2(_on ? 23 : 3, 3);
        _label.Text = UiKit.T(_on ? "ui.common.on" : "ui.common.off");
    }
}

/// <summary>滑杆（推子，UI-VISUAL）：轨道 1 px，已选段 2 px primary.line，推子帽 12×20 加中线；离散档画 1×5 刻度；拖动 1:1 跟手；←→ 一档。</summary>
public partial class Fader : Control
{
    private UiKit _kit = null!;
    private Panel _fill = null!, _cap = null!, _capLine = null!;
    private Label _value = null!;
    private Func<float, string> _format = v => v.ToString("0.##");
    private float _min, _max, _step, _v, _width;
    private bool _dragging, _pin;

    public event Action<float>? Changed;
    public float Value => _v;

    public static Fader Create(UiKit kit, Control parent, Vector2 position, float width, float min, float max, float step, float value,
        bool pin, Func<float, string>? format = null)
    {
        var f = new Fader
        {
            _kit = kit,
            _min = min,
            _max = max,
            _step = step,
            _v = value,
            _width = width,
            _pin = pin,
            Position = position,
            Size = new Vector2(width, 36),
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.All,
        };
        if (format is not null) f._format = format;
        parent.AddChild(f);
        kit.Box(f, new Rect2(0, 18, width, 1), new UiFx(Fill: "line.control", Pin: pin));
        f._fill = kit.Box(f, new Rect2(0, 17.5f, 0, 2), new UiFx(Fill: "primary.line", Pin: pin));
        if (step > 0 && (max - min) / step <= 12)
        {
            var n = (int)Math.Round((max - min) / step);
            for (var i = 0; i <= n; i++)
                kit.Box(f, new Rect2(Mathf.Round(width * i / n) - (i == n ? 1 : 0), 24, 1, 5), new UiFx(Fill: "line.control", Pin: pin));
        }
        f._cap = kit.Box(f, new Rect2(0, 8, 12, 20), new UiFx(Fill: "surface.raised", Line: "line.control", Pin: pin));
        f._capLine = kit.Box(f, new Rect2(0, 13, 1, 10), new UiFx(Fill: "line.control", Pin: pin));
        f._value = kit.Text(f, "", "data", "text.1", new Vector2(width + 14, 9), pin);
        f.FocusEntered += f.Layout;
        f.FocusExited += f.Layout;
        f.Layout();
        return f;
    }

    public void Set(float value, bool notify)
    {
        var v = Math.Clamp(value, _min, _max);
        if (_step > 0) v = _min + MathF.Round((v - _min) / _step) * _step;
        if (MathF.Abs(v - _v) < 1e-5f) return;
        _v = v;
        Layout();
        if (notify) Changed?.Invoke(_v);
    }

    private void Layout()
    {
        var t = _max > _min ? (_v - _min) / (_max - _min) : 0;
        var x = Mathf.Round(_width * t);
        _fill.Size = new Vector2(x, 2);
        _cap.Position = new Vector2(x - 6, 8);
        _capLine.Position = new Vector2(x - 0.5f, 13);
        _value.Text = _format(_v);
        var focus = HasFocus() && !Input.IsMouseButtonPressed(MouseButton.Left) ? 1f : 0f;
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

    private void SetFromX(float x) => Set(_min + Math.Clamp(x / _width, 0, 1) * (_max - _min), true);
}

/// <summary>小件：键帽、组标题、生效标签、LED 格。</summary>
public static class Parts
{
    /// <summary>键帽（UI-VISUAL）：高 24、最小宽 24、1 px line.control、底边 2.5 px 键深、surface.raised；按下 = primary.fill、下沉 1.5 px。</summary>
    public static float Keycap(UiKit kit, Control parent, Vector2 at, string key, bool down = false, bool pin = false)
    {
        var probe = kit.Text(parent, key, "keycap", "text.1", Vector2.Zero, pin);
        var w = Math.Max(24, kit.Width(probe) + 14);
        probe.QueueFree();
        var sink = down ? 1.5f : 0;
        kit.Box(parent, new Rect2(at.X, at.Y + sink, w, 24 - sink), new UiFx(Fill: "line.control", Pin: pin));
        kit.Box(parent, new Rect2(at.X, at.Y + sink, w, 21.5f), new UiFx(Fill: down ? "primary.fill" : "surface.raised", Line: "line.control", Pin: pin));
        kit.Text(parent, key, "keycap", down ? "primary.ink" : "text.1", new Vector2(at.X, at.Y + sink + 2), pin, w, HorizontalAlignment.Center);
        return w;
    }

    /// <summary>组标题："mono-caps 小标签 + 发丝线"（UI-VISUAL）。</summary>
    public static void GroupHeader(UiKit kit, Control parent, Vector2 at, float width, string text, bool pin = false)
    {
        var label = kit.Text(parent, text, "mono-caps", "text.2", at, pin);
        var w = kit.Width(label) + 12;
        kit.Box(parent, new Rect2(at.X + w, at.Y + 8, width - w, 1), new UiFx(Fill: "line.hair", Pin: pin));
    }

    /// <summary>生效标签：等宽的"雾幕""重启"，发丝边；雾幕用 primary.line 边（UI-VISUAL）。</summary>
    public static float Tag(UiKit kit, Control parent, Vector2 at, string text, bool primary, bool pin = false)
    {
        var label = kit.Text(parent, text, "data", "text.2", at + new Vector2(6, 3), pin);
        var w = kit.Width(label) + 12;
        kit.Box(parent, new Rect2(at, new Vector2(w, 24)), new UiFx(Line: primary ? "primary.line" : "line.control", Pin: pin));
        return w;
    }

    /// <summary>三格负载 LED（UI-VISUAL）。</summary>
    public static void Leds(UiKit kit, Control parent, Vector2 at, int lit, int count = 3, bool pin = false)
    {
        for (var i = 0; i < count; i++)
            kit.Box(parent, new Rect2(at.X + i * 10, at.Y, 8, 8), new UiFx(Fill: i < lit ? "graphic" : null, Line: i < lit ? null : "line.control", Pin: pin));
    }
}
